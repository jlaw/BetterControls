using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace BetterControls
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private Keymap globalKeymap = new Keymap();
        private Dictionary<string, Keymap> allKeymaps = new Dictionary<string, Keymap>();
        private ModConfig _config;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            try
            {
                _config = helper.ReadConfig<ModConfig>();
                if (_config.Keymaps != null)
                {
                    foreach (var keymapGroup in _config.Keymaps)
                    {
                        if (keymapGroup.Key == "Global")
                        {
                            foreach (var keymap in keymapGroup.Value)
                            {
                                if (Enum.TryParse(keymap.Key, out SButton from) &&
                                    Enum.TryParse(keymap.Value, out SButton to))
                                    globalKeymap.Add(from, to);
                            }
                        }
                        else
                        {
                            Keymap temp = new Keymap();
                            foreach (var keymap in keymapGroup.Value)
                            {
                                if (Enum.TryParse(keymap.Key, out SButton from) &&
                                    Enum.TryParse(keymap.Value, out SButton to))
                                    temp.Add(from, to);
                            }

                            allKeymaps.Add(keymapGroup.Key, temp);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Monitor.Log($"Error: {e}", LogLevel.Error);
            }
            
            // Try to initialize the input patcher
            try
            {
                InputPatch.Initialize(ModManifest.UniqueID, Monitor, globalKeymap);
            }
            catch (Exception e)
            {
                Monitor.Log($"Error: {e}", LogLevel.Error);
                return;
            }

            InputPatch.SetMap(allKeymaps["TitleMenu"]);
            helper.Events.Display.MenuChanged += OnEnterMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;
        }

        /// <summary>Raised after the player opens a menu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnEnterMenu(object sender, MenuChangedEventArgs e)
        {
            Monitor.Log($"OldMenu: {e.OldMenu} NewMenu: {e.NewMenu}", LogLevel.Debug);

            switch (e.NewMenu)
            {
                case null:
                case BobberBar _: // Fishing
                    InputPatch.SetMap(allKeymaps["Overworld"]);
                    break;
                case GameMenu _:
                case JunimoNoteMenu _: // Community Center Menu
                    InputPatch.SetMap(allKeymaps["GameMenu"]);
                    break;
                case ItemGrabMenu _:
                    InputPatch.SetMap(allKeymaps["ItemGrabMenu"]);
                    break;
                case TitleMenu _:
                    InputPatch.SetMap(allKeymaps["TitleMenu"]);
                    break;
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            Monitor.Log($"Button Pressed: {e.Button}", LogLevel.Debug);
        }
        
        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            Monitor.Log($"Button Released: {e.Button}", LogLevel.Debug);
        }
    }
}