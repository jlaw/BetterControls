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
        private static IMonitor Monitor;
        private static KeyMap _map;
        private static GamePadState curGamePadState;
        private static KeyboardState curKeyboardState;
        //private static MouseState curMouseState;

        // call this method from your Entry class
        public static bool Initialize(string id, IMonitor monitor)
        {
            Monitor = monitor;
            var harmony = HarmonyInstance.Create(id);

            var keyboardGetStateMethod = AccessTools.Method(typeof(Keyboard), nameof(Keyboard.GetState));
            var mouseGetStateMethod = AccessTools.Method(typeof(Mouse), nameof(Mouse.GetState), new[] { typeof(GameWindow) });
            var gamepadGetStateMethod = AccessTools.Method(typeof(GamePad), nameof(GamePad.GetState), new[] { typeof(int), typeof(GamePadDeadZone) });
            MethodInfo[] methods = { keyboardGetStateMethod, mouseGetStateMethod, gamepadGetStateMethod };

            foreach (var method in methods)
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null)
                {
                    continue;
                }
                foreach (var patch in info.Postfixes)
                {
                    Monitor.Log($"{info} is already patched by {patch.owner}");
                    return false;
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

            return true;
        }

        public static void SetMap(KeyMap map)
        {
            InputPatch._map = map;
        }

        public static KeyboardState Keyboard_GetState_Postfix(KeyboardState __result)
        {
            //List<Keys> keys = new List<Keys>();
            //keys.AddRange(curKeyboardState.GetPressedKeys());
            //keys.AddRange(__result.GetPressedKeys());
            //return new KeyboardState(keys.Distinct().ToArray());
            return (__result.GetPressedKeys().Any()) ? __result : curKeyboardState;
        }

        public static void Mouse_GetState_Postfix(MouseState __result)
        {
        }

        public static GamePadState GamePad_GetState_Postfix(GamePadState __result)
        {
            // get a copy of current button states (Buttons, ThumbSticks, DPad)
            MethodInfo virtualButtonMethod = AccessTools.Method(typeof(GamePadState), "GetVirtualButtons");
            Buttons curButtons = (Buttons)virtualButtonMethod.Invoke(__result, new object[] {});

            // start new button and key states with no remaps
            Buttons newButtons = curButtons;
            List<Keys> newKeys = new List<Keys>();

            // go through each mapping and process
            //   clearing button state only if button was originally pressed
            foreach (var entry in _map)
            {
                Buttons fromButton;
                if (entry.Key.TryGetController(out fromButton) && entry.Value.TryGetController(out var toButton))
                {
                    newButtons &= ((newButtons & curButtons & fromButton) == fromButton) ? ~fromButton : newButtons;
                    newButtons |= ((curButtons & fromButton) == fromButton) ? toButton : newButtons;
                }
                else if (entry.Key.TryGetController(out fromButton) && entry.Value.TryGetKeyboard(out var newKey))
                {
                    newButtons &= ((newButtons & curButtons & fromButton) == fromButton) ? ~fromButton : newButtons;
                    if ((curButtons & fromButton) == fromButton)
                        newKeys.Add(newKey);
                }
            }

            // update DPad states if they were remapped
            var newDPadUp    = (newButtons & Buttons.DPadUp)    == Buttons.DPadUp    ? ButtonState.Pressed : ButtonState.Released;
            var newDPadDown  = (newButtons & Buttons.DPadDown)  == Buttons.DPadDown  ? ButtonState.Pressed : ButtonState.Released;
            var newDPadLeft  = (newButtons & Buttons.DPadLeft)  == Buttons.DPadLeft  ? ButtonState.Pressed : ButtonState.Released;
            var newDPadRight = (newButtons & Buttons.DPadRight) == Buttons.DPadRight ? ButtonState.Pressed : ButtonState.Released;

            // save key mappings to be processed Keyboard_GetState_Postfix
            curKeyboardState = new KeyboardState(newKeys.ToArray());

            curGamePadState = new GamePadState(
                __result.ThumbSticks,
                __result.Triggers,
                new GamePadButtons(newButtons),
                new GamePadDPad(newDPadUp, newDPadDown, newDPadLeft, newDPadRight)
            );
            return curGamePadState;
        }
    }

    public class KeyMap : Dictionary<SButton, SButton> { }
}
