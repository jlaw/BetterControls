using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley.Menus;

namespace BetterControls
{
    public class InputPatch
    {
        private static IMonitor Monitor;
        private static KeyMap map;
        private static GamePadState curGamePadState;
        private static KeyboardState curKeyboardState;
        private static MouseState curMouseState;

        // call this method from your Entry class
        public static bool Initialize(string id, IMonitor monitor)
        {
            Monitor = monitor;
            var harmony = HarmonyInstance.Create(id);

            var keyboardGetStateMethod = AccessTools.Method(typeof(Keyboard), nameof(Keyboard.GetState));
            var mouseGetStateMethod = AccessTools.Method(typeof(Mouse), nameof(Mouse.GetState), new Type[] { typeof(GameWindow) });
            var gamepadGetStateMethod = AccessTools.Method(typeof(GamePad), nameof(GamePad.GetState), new Type[] { typeof(int), typeof(GamePadDeadZone) });
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
                    Monitor.Log($"{info.ToString()} is already patched by {patch.owner}");
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
            InputPatch.map = map;
        }

        public static KeyboardState Keyboard_GetState_Postfix(KeyboardState __result)
        {
            if (__result.GetPressedKeys().Any())
            {
                curKeyboardState = __result;
                return __result;
            }
            return curKeyboardState;
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

            // this currently sets to = from is set, but it should actually be mirroring
            // to -> from and unsetting to (making sure not to trash any keys that have already been rebound)
            // ex: a -> b and b -> a
            foreach (var entry in map)
            {
                Buttons fromButton;
                if (entry.Key.TryGetController(out oldButton) && entry.Value.TryGetController(out var newButton))
                {
                    newButtons &= ((curButtons & oldButton) == oldButton) ? ~oldButton : newButtons;
                    newButtons |= ((curButtons & oldButton) == oldButton) ? newButton : 0;
                }
                if (entry.Key.TryGetController(out oldButton) && entry.Value.TryGetKeyboard(out var newKey))
                {
                    newButtons &= ((curButtons & oldButton) == oldButton) ? ~oldButton : newButtons;
                    if ((curButtons & oldButton) == oldButton)
                        newKeys.Add(newKey);
                }
            }

            curKeyboardState = new KeyboardState(newKeys.ToArray());
            
            curGamePadState = new GamePadState(
                __result.ThumbSticks,
                __result.Triggers,
                new GamePadButtons(newButtons),
                __result.DPad
            );
            return curGamePadState;
        }
    }

    public class KeyMap : Dictionary<SButton, SButton> { }
}
