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
            {SButton.DPadUp,          SButton.B},               //     ChestAnywhere
            {SButton.DPadLeft,        SButton.None},
            {SButton.DPadDown,        SButton.Tab},             //     Shift Toolbar
            {SButton.DPadRight,       SButton.K},               //     GeodeInfo
            //{SButton.ControllerA,     SButton.ControllerX},   // Default: Check/Do Action
            //{SButton.ControllerB,     SButton.ControllerB},   // Default: Inventory
            //{SButton.ControllerX,     SButton.ControllerA},   // Default: Use Tool
            //{SButton.ControllerY,     SButton.ControllerY},   // Default: Crafting
            {SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Left Tool (Default: Shift Toolbar)
            {SButton.RightShoulder,   SButton.RightTrigger},    //     Select Right Tool (Default: Shift Toolbar)
            //{SButton.LeftTrigger,     SButton.LeftTrigger},   // Default: Select Left Tool
            //{SButton.RightTrigger,    SButton.C},             //     Use Tool (Default: Select Right Tool)
            //{SButton.ControllerBack,  SButton.ControllerBack},// Default: Journal
            {SButton.ControllerStart, SButton.N},               //     Pause/TimeSpeed (Default: Menu)
            {SButton.LeftStick,       SButton.M},               //     Map (Default: nothing)
            {SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };

        // keymap for main menu
        private readonly KeyMap _mapGameMenu = new KeyMap
        {
            {SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Previous Tab
            {SButton.RightShoulder,   SButton.RightTrigger},    //     Select Next Tab
            {SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };

        // keymap for chests
        private readonly KeyMap _mapItemGrabMenu = new KeyMap
        {
            {SButton.LeftTrigger,    SButton.LeftShoulder},     //     ChestAnywhere: Select Previous Tab
            {SButton.RightTrigger,   SButton.RightShoulder},    //     ChestAnywhere: Select Next Tab
            {SButton.ControllerY,     SButton.Q},               //     OrganizeShortcut: StackToChest
            {SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };
        
        // keymap for title menu
        private readonly KeyMap _mapTitleMenu = new KeyMap
        {
            {SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Previous Tab
            {SButton.RightShoulder,   SButton.RightTrigger},    //     Select Next Tab
        };


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

            InputPatch.SetMap(_mapTitleMenu);
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
                    InputPatch.SetMap(_mapOverworld);
                    break;
                case GameMenu _:
                    InputPatch.SetMap(_mapGameMenu);
                    break;
                case ItemGrabMenu _:
                    InputPatch.SetMap(_mapItemGrabMenu);
                    break;
                case TitleMenu _:
                    InputPatch.SetMap(_mapTitleMenu);
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