using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace BetterControls
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private readonly KeyMap _mapOverworld = new KeyMap
        {

        };
            
        // keymap for main menu
        private readonly KeyMap _mapGameMenu = new KeyMap
        {

        };

        // keymap for chests
        private readonly KeyMap _mapItemGrabMenu = new KeyMap
        {

        };
        
        // keymap for title menu
        private readonly KeyMap _mapTitleMenu = new KeyMap
        {

        };
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Try to initialize the input patcher
            if (!InputPatch.Initialize(this.ModManifest.UniqueID, Monitor))
            {
                return;
            }

            Config = Helper.ReadConfig<ModConfig>();

            InputPatch.SetMap(Config.KeyMaps.TitleMenu);
            helper.Events.Display.MenuChanged += this.OnEnterMenu;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
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
                case BobberBar _:
                    InputPatch.SetMap(Config.KeyMaps.GameMenu);
                    break;
                case GameMenu _:
                case JunimoNoteMenu _:
                    InputPatch.SetMap(Config.KeyMaps.GameMenu);
                    break;
                case ItemGrabMenu _:
                    InputPatch.SetMap(Config.KeyMaps.ItemGrabMenu);
                    break;
                case TitleMenu _:
                    InputPatch.SetMap(Config.KeyMaps.TitleMenu);
                    break;
                default:
                    InputPatch.SetMap(new KeyMap());
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