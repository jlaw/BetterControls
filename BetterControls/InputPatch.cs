using System;
using System.Collections.Generic;
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
        private static Dictionary<Tuple<Type, Type>, KeyMap> map;
        // private static Dictionary<Type, string> pressed;

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

        public static void SetMap(Dictionary<Tuple<Type, Type>, KeyMap> map)
        {
            InputPatch.map = map;
        }

        public static void Keyboard_GetState_Postfix(KeyboardState __result)
        {

        }

        public static void Mouse_GetState_Postfix(MouseState __result)
        {

        }

        public static GamePadState GamePad_GetState_Postfix(GamePadState __result)
        {
            FieldInfo fieldinfo = AccessTools.Field(typeof(GamePadButtons), "buttons");
            Buttons origButtons = (Buttons)fieldinfo.GetValue(__result.Buttons);
            Buttons newButtons = origButtons;

            // Just handle gamepad -> gamepad mappings for now
            var key = Tuple.Create(typeof(GamePadState), typeof(GamePadState));
            if (!map.ContainsKey(key))
            {
                return __result;
            }
            foreach (var entry in map[key])
            {
                var fromFI = AccessTools.Field(typeof(Buttons), entry.Key);
                var toFI = AccessTools.Field(typeof(Buttons), entry.Value);
                var from = (Buttons) fromFI.GetRawConstantValue();
                var to = (Buttons) toFI.GetRawConstantValue();

                // this currently sets to = from is set, but it should actually be mirroring
                // to -> from and unsetting to (making sure not to trash any keys that have already been rebound)
                // ex: a -> b and b -> a
                if ((newButtons & from) == from) {
                    newButtons &= ~from;
                    newButtons |= to;
                }
            }

            return new GamePadState(
                __result.ThumbSticks,
                __result.Triggers,
                new GamePadButtons(newButtons),
                __result.DPad
            );
        }
    }

    public class KeyMap : Dictionary<string, string> { }
}
