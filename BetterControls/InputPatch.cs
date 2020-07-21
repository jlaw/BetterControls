﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;

namespace BetterControls
{
    public class InputPatch
    {
        private static IMonitor _monitor;
        private static Keymap _globalMap;
        private static Keymap _map;
        private static KeyboardState _prevKeyboardState;
        private static GamePadState _prevGamePadState;
        private static KeyboardState _pendingKeyState;
        private static GamePadState _pendingButtonState;
        //private static MouseState curMouseState;
        private static readonly MethodInfo GetVirtualButtonMethod =
            AccessTools.Method(typeof(GamePadState), "GetVirtualButtons");

        // call this method from your Entry class
        public static void Initialize(string id, IMonitor monitor, Keymap globalMap)
        {
            _monitor = monitor;
            _globalMap = globalMap;
            var harmony = HarmonyInstance.Create(id);

            var keyboardGetStateMethod = AccessTools.Method(typeof(Keyboard), nameof(Keyboard.GetState));
            var mouseGetStateMethod =
                AccessTools.Method(typeof(Mouse), nameof(Mouse.GetState), new[] {typeof(GameWindow)});
            var gamepadGetStateMethod = AccessTools.Method(typeof(GamePad), nameof(GamePad.GetState),
                new[] {typeof(int), typeof(GamePadDeadZone)});
            MethodInfo[] methods = {keyboardGetStateMethod, mouseGetStateMethod, gamepadGetStateMethod};

            foreach (var method in methods)
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null)
                {
                    continue;
                }

                foreach (var patch in info.Postfixes)
                {
                    _monitor.Log($"{info} is already patched by {patch.owner}");
                    throw new NotImplementedException();
                }
            }

            harmony.Patch(
                original: keyboardGetStateMethod,
                postfix: new HarmonyMethod(typeof(InputPatch), nameof(InputPatch.Keyboard_GetState_Postfix))
            );
            harmony.Patch(
                original: mouseGetStateMethod,
                postfix: new HarmonyMethod(typeof(InputPatch), nameof(InputPatch.Mouse_GetState_Postfix))
            );
            harmony.Patch(
                original: gamepadGetStateMethod,
                postfix: new HarmonyMethod(typeof(InputPatch), nameof(InputPatch.GamePad_GetState_Postfix))
            );
        }

        public static void SetMap(Keymap map)
        {
            _map = _globalMap;
            foreach (var binding in map)
            {
                _map[binding.Key] = binding.Value;
            }
        }

        public static KeyboardState Keyboard_GetState_Postfix(KeyboardState __result)
        {
            // process pending keys
            if (_pendingKeyState.GetPressedKeys().Any())
            {
                _monitor.Log($"Pending Keys: {_pendingKeyState.GetPressedKeys()}", LogLevel.Debug);
                _prevKeyboardState = _pendingKeyState;
                _pendingKeyState = new KeyboardState();
                return _prevKeyboardState;
            }

            // skip remap if nothing is pressed
            if (!__result.GetPressedKeys().Any())
            {
                _pendingButtonState = new GamePadState();
                _prevKeyboardState = new KeyboardState();
                return _prevKeyboardState;
            }

            List<Keys> curKeys = new List<Keys>();
            curKeys.AddRange(__result.GetPressedKeys());

            List<Keys> noremapKeys = new List<Keys>(curKeys.ToArray());

            List<Keys> newKeys = new List<Keys>();
            Buttons newButtons = 0;

            foreach (var key in curKeys)
            {
                foreach (var entry in _map.Where(entry => entry.Key == key.ToSButton()))
                {
                    // remove key from list of noremaps
                    noremapKeys.Remove(key);

                    if (entry.Value.TryGetKeyboard(out var toKey))
                        newKeys.Add(toKey);
                    else if (entry.Value.TryGetController(out var toButton))
                        newButtons |= toButton;
                    else
                        _monitor.Log($"No such key: {entry.Value}");
                    break;
                }
            }

            // update DPad states if they were remapped
            var newDPadUp = (newButtons & Buttons.DPadUp) == Buttons.DPadUp
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadDown = (newButtons & Buttons.DPadDown) == Buttons.DPadDown
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadLeft = (newButtons & Buttons.DPadLeft) == Buttons.DPadLeft
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadRight = (newButtons & Buttons.DPadRight) == Buttons.DPadRight
                ? ButtonState.Pressed
                : ButtonState.Released;

            _pendingButtonState = new GamePadState(
                new GamePadThumbSticks(),
                new GamePadTriggers(),
                new GamePadButtons(newButtons),
                new GamePadDPad(newDPadUp, newDPadDown, newDPadLeft, newDPadRight)
            );

            newKeys.AddRange(noremapKeys.ToArray());
            _prevKeyboardState = new KeyboardState(newKeys.Distinct().ToArray());
            return _prevKeyboardState;
        }

        public static void Mouse_GetState_Postfix(MouseState __result)
        {
        }

        public static GamePadState GamePad_GetState_Postfix(GamePadState __result)
        {
            Buttons pendingButtons = (Buttons) GetVirtualButtonMethod.Invoke(_pendingButtonState, new object[] { });
            if (pendingButtons != 0)
            {
                _monitor.Log($"Pending Buttons: {pendingButtons}", LogLevel.Debug);
                _prevGamePadState = _pendingButtonState;
                _pendingButtonState = new GamePadState();
                return _prevGamePadState;
            }

            // skip remap if nothing changed
            if (_prevGamePadState == __result)
            {
                _pendingKeyState = new KeyboardState();
                return _prevGamePadState;
            }

            // get a copy of current button states (Buttons, ThumbSticks, DPad)
            Buttons curButtons = (Buttons) GetVirtualButtonMethod.Invoke(__result, new object[] { });

            // copy current button states
            Buttons newButtons = curButtons;

            // create a list to hold any button->key mappings
            List<Keys> newKeys = new List<Keys>();

            // go through each mapping
            //   clear button state only if button was originally pressed
            foreach (var entry in _map)
            {
                Buttons fromButton;
                if (entry.Key.TryGetController(out fromButton) && entry.Value.TryGetController(out var toButton))
                {
                    newButtons &= (newButtons & curButtons & fromButton) == fromButton ? ~fromButton : newButtons;
                    newButtons |= (curButtons & fromButton) == fromButton ? toButton : newButtons;
                }
                else if (entry.Key.TryGetController(out fromButton) && entry.Value.TryGetKeyboard(out var newKey))
                {
                    newButtons &= (newButtons & curButtons & fromButton) == fromButton ? ~fromButton : newButtons;
                    if ((curButtons & fromButton) == fromButton)
                        newKeys.Add(newKey);
                }
            }

            if (__result.IsConnected)
                _monitor.Log($"{curButtons} -> {newButtons}", LogLevel.Debug);

            // update DPad states if they were remapped
            var newDPadUp = (newButtons & Buttons.DPadUp) == Buttons.DPadUp
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadDown = (newButtons & Buttons.DPadDown) == Buttons.DPadDown
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadLeft = (newButtons & Buttons.DPadLeft) == Buttons.DPadLeft
                ? ButtonState.Pressed
                : ButtonState.Released;
            var newDPadRight = (newButtons & Buttons.DPadRight) == Buttons.DPadRight
                ? ButtonState.Pressed
                : ButtonState.Released;

            // save key mappings to be processed by Keyboard_GetState_Postfix
            _pendingKeyState = new KeyboardState(newKeys.ToArray());

            _prevGamePadState = new GamePadState(
                __result.ThumbSticks,
                __result.Triggers,
                new GamePadButtons(newButtons),
                new GamePadDPad(newDPadUp, newDPadDown, newDPadLeft, newDPadRight)
            );
            return _prevGamePadState;
        }
    }
}
