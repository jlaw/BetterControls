using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;

namespace BetterControls
{
    public class InternalKeymap
    {
        public Dictionary<Keys, Buttons> KeyToButtonMap { get; }
        public Dictionary<Buttons, Keys> ButtonToKeyMap { get; }
        public Dictionary<Keys, Keys> KeyToKeyMap { get; }
        public Dictionary<Buttons, Buttons> ButtonToButtonMap { get; }

        public InternalKeymap(
            Dictionary<Keys, Buttons> ktb,
            Dictionary<Buttons, Keys> btk,
            Dictionary<Keys, Keys> ktk,
            Dictionary<Buttons, Buttons> btb)
        {
            KeyToButtonMap = ktb;
            ButtonToKeyMap = btk;
            KeyToKeyMap = ktk;
            ButtonToButtonMap = btb;
        }
    }

    public class InputPatch
    {
        private static IMonitor _monitor;
        private static InternalKeymap _globalMap;
        private static InternalKeymap _map;

        private static DynamicMethod _originalKeyboardGetStateMethod;
        private static DynamicMethod _originalGamePadGetStateMethod;
        private static DynamicMethod _originalMouseGetStateMethod;

        private static KeyboardState _currKeyboardState;
        private static MouseState _currMouseState;
        private static GamePadState _currGamePadState;

        private static readonly MethodInfo GetVirtualButtonMethod =
            AccessTools.Method(typeof(GamePadState), "GetVirtualButtons");

        public static void Initialize(string id, IMonitor monitor, InternalKeymap globalMap)
        {
            // Initialize
            _monitor = monitor;
            _globalMap = globalMap;
            var harmony = HarmonyInstance.Create(id);

            // Get refs to all the methods we need to patch
            var keyboardGetStateMethod = AccessTools.Method(typeof(Keyboard), nameof(Keyboard.GetState));
            var mouseGetStateMethod =
                AccessTools.Method(typeof(Mouse), nameof(Mouse.GetState));
            var gamepadGetStateMethod = AccessTools.Method(typeof(GamePad), nameof(GamePad.GetState),
                new[] { typeof(int), typeof(GamePadDeadZone) });
            var updateMethod = AccessTools.Method(typeof(Game1), "Update");
            MethodInfo[] methods = { keyboardGetStateMethod, mouseGetStateMethod, gamepadGetStateMethod, updateMethod };

            // Ensure that none of them have been patched
            foreach (var method in methods)
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null)
                {
                    continue;
                }

                foreach (var patch in info.Postfixes)
                {
                    _monitor.Log($"{info} is already patched by {patch.owner}", LogLevel.Error);
                    throw new NotImplementedException();
                }
            }

            // Get a ref to the original GetState methods
            _originalKeyboardGetStateMethod = harmony.Patch(keyboardGetStateMethod);
            _originalGamePadGetStateMethod = harmony.Patch(gamepadGetStateMethod);
            _originalMouseGetStateMethod = harmony.Patch(mouseGetStateMethod);

            // Patch all the methods
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

            harmony.Patch(
                original: updateMethod,
                postfix: new HarmonyMethod(typeof(InputPatch), nameof(InputPatch.Game1_Update_Prefix))
            );
        }

        public static InternalKeymap ParseKeymap(Keymap keymap) {
            Dictionary<Keys, Buttons> keysToButtons = new Dictionary<Keys, Buttons>();
            Dictionary<Buttons, Keys> buttonsToKeys = new Dictionary<Buttons, Keys>();
            Dictionary<Keys, Keys> keysToKeys = new Dictionary<Keys, Keys>();
            Dictionary<Buttons, Buttons> buttonsToButtons = new Dictionary<Buttons, Buttons>();

            foreach (var entry in keymap)
            {
                // Ensure the mapping is valid
                if (!Enum.TryParse(entry.Key, out SButton from) ||
                    !Enum.TryParse(entry.Value, out SButton to))
                {
                    _monitor.Log($"Invalid mapping: {entry.Key} -> {entry.Value}", LogLevel.Error);
                    continue;
                }

                if (from.TryGetKeyboard(out var fromKey))
                {
                    if (to.TryGetKeyboard(out var toKey))
                    {
                        keysToKeys[fromKey] = toKey;
                    }
                    else if (to.TryGetController(out var toButton))
                    {
                        keysToButtons[fromKey] = toButton;
                    }
                    else
                    {
                        _monitor.Log($"Unsupported type", LogLevel.Warn);
                    }
                }
                else if (from.TryGetController(out var fromButton))
                {
                    if (to.TryGetKeyboard(out var toKey))
                    {
                        buttonsToKeys[fromButton] = toKey;
                    }
                    else if (to.TryGetController(out var toButton))
                    {
                        buttonsToButtons[fromButton] = toButton;
                    }
                    else
                    {
                        _monitor.Log($"Unsupported type", LogLevel.Warn);
                    }
                }
                else
                {
                    _monitor.Log($"Unsupported type", LogLevel.Warn);
                }
            }

            return new InternalKeymap(keysToButtons, buttonsToKeys, keysToKeys, buttonsToButtons);
        }

        public static void SetMap(InternalKeymap map)
        {
            _map = map;
        }

        public static void Game1_Update_Prefix()
        {
            // Cache state results
            var keyboardState = (KeyboardState) _originalKeyboardGetStateMethod.Invoke(null, null);
            var mouseState = (MouseState) _originalMouseGetStateMethod.Invoke(null, null);
            var gamepadState = (GamePadState)new GamePadState(); //.Invoke(null, new object[] { 0, GamePadDeadZone.None });

            Buttons oldButtons = (Buttons) GetVirtualButtonMethod.Invoke(gamepadState, new object[] { });

            List<Keys> newKeys = new List<Keys>(); // Additive
            Buttons newButtons = oldButtons; // Subtractive

            // Process each keymap
            InternalKeymap[] maps = { _map, _globalMap };
            foreach (var map in maps)
            {
                // Map button -> *
                foreach (var entry in map.ButtonToKeyMap)
                {
                    if ((entry.Key & oldButtons) == entry.Key)
                    {
                        newButtons &= ~entry.Key;
                        newKeys.Add(entry.Value);
                    }
                }
                foreach (var entry in map.ButtonToButtonMap)
                {
                    if ((entry.Key & oldButtons) == entry.Key)
                    {
                        newButtons &= ~entry.Key;
                        newButtons |= entry.Value;
                    }
                }

                // Map keyboard -> *
                foreach (var key in keyboardState.GetPressedKeys())
                {
                    if (map.KeyToKeyMap.ContainsKey(key))
                    {
                        newKeys.Add(_map.KeyToKeyMap[key]);
                    }
                    else if (map.KeyToButtonMap.ContainsKey(key))
                    {
                        newButtons |= map.KeyToButtonMap[key];
                    }
                    else
                    {
                        newKeys.Add(key);
                    }
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

            _currKeyboardState = new KeyboardState(newKeys.Distinct().ToArray());
            _currMouseState = mouseState;
            _currGamePadState = new GamePadState(
                gamepadState.ThumbSticks,
                gamepadState.Triggers,
                new GamePadButtons(newButtons),
                new GamePadDPad(newDPadUp, newDPadDown, newDPadLeft, newDPadRight)
            );
        }

        public static KeyboardState Keyboard_GetState_Postfix(KeyboardState __result)
        {
            return _currKeyboardState;
        }

        public static MouseState Mouse_GetState_Postfix(MouseState __result)
        {
            // FIXME: WE DON'T CURRENTLY PROCESS THE GAMEWINDOW CASE
            return _currMouseState;
        }

        public static GamePadState GamePad_GetState_Postfix(GamePadState __result)
        {
            // FIXME: THIS ASSUMES THERE IS ONLY ONE GAMEPAD
            return _currGamePadState;
        }
    }
}
