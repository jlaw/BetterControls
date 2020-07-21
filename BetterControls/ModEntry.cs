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
        private readonly Dictionary<string, Keymap> _keymaps = new Dictionary<string, Keymap>();
        private ModConfig _config;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // try reading config.json
            try
            {
                _config = helper.ReadConfig<ModConfig>();
                if (_config.Keymaps != null)
                {
                    // iterate through each keymap group
                    foreach (var keymapGroup in _config.Keymaps)
                    {
                        Keymap newKeymap = new Keymap();
                        
                        // convert strings to SButtons
                        foreach (var keymap in keymapGroup.Value)
                        {
                            if (Enum.TryParse(keymap.Key, out SButton from) &&
                                Enum.TryParse(keymap.Value, out SButton to))
                            {
                                newKeymap.Add(from, to);
                            }
                        }

                        // put it all together
                        _keymaps.Add(keymapGroup.Key, newKeymap);
                    }
                }
            }
            catch (Exception e)
            {
                Monitor.Log($"Error: {e}", LogLevel.Error);
            }

            // try to initialize the input patcher
            try
            {
                InputPatch.Initialize(ModManifest.UniqueID, Monitor, _keymaps["Global"]);
            }
            catch (Exception e)
            {
                Monitor.Log($"Error: {e}", LogLevel.Error);
            }

            // set default keymap to TitleMenu, since that is the first screen
            InputPatch.SetMap(_keymaps["TitleMenu"]);
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
                    InputPatch.SetMap(_keymaps["Overworld"]);
                    break;
                case GameMenu _:
                case JunimoNoteMenu _: // Community Center Menu
                    InputPatch.SetMap(_keymaps["GameMenu"]);
                    break;
                case ItemGrabMenu _:
                    InputPatch.SetMap(_keymaps["ItemGrabMenu"]);
                    break;
                case TitleMenu _:
                    InputPatch.SetMap(_keymaps["TitleMenu"]);
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