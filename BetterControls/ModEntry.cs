using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace BetterControls
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private object _inputState;
        private MethodInfo _overrideButtonMethod;
        private IClickableMenu _activeMenu;

        private readonly Dictionary<SButton, SButton> _remapOverworld = new Dictionary<SButton, SButton>
        {
            {SButton.DPadUp,          SButton.B},               //     ChestAnywhere
            {SButton.DPadLeft,        SButton.None},
            {SButton.DPadDown,        SButton.Tab},             //     Shift Toolbar
            {SButton.DPadRight,       SButton.K},               //     GeodeInfo
            //{SButton.ControllerA,     SButton.ControllerA},   // Default: Check/Do Action
            //{SButton.ControllerB,     SButton.ControllerB},   // Default: Inventory
            //{SButton.ControllerX,     SButton.ControllerX},   // Default: Use Tool
            //{SButton.ControllerY,     SButton.ControllerY},   // Default: Crafting
            //{SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Left Tool (Default: Shift Toolbar)
            //{SButton.RightShoulder,   SButton.RightTrigger},    //     Select Right Tool (Default: Shift Toolbar)
            //{SButton.LeftTrigger,     SButton.LeftTrigger},   // Default: Select Left Tool
            //{SButton.RightTrigger,    SButton.C},             //     Use Tool (Default: Select Right Tool)
            //{SButton.ControllerBack,  SButton.ControllerBack},// Default: Journal
            {SButton.ControllerStart, SButton.N},               //     Pause/TimeSpeed (Default: Menu)
            {SButton.LeftStick,       SButton.M},               //     Map (Default: nothing)
            {SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };

        private readonly Dictionary<SButton, SButton> _remapInMenu = new Dictionary<SButton, SButton>
        {
            {SButton.ControllerY,     SButton.Q},               //     OrganizeShortcut: StackToChest
            //{SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Previous Tab
            //{SButton.RightShoulder,   SButton.RightTrigger},    //     Select Next Tab
            {SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // get SMAPI's input handler
            _inputState = helper.Input.GetType().GetField("InputState", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(helper.Input);

            // get OverrideButton method
            _overrideButtonMethod = _inputState?.GetType().GetMethod("OverrideButton");

            helper.Events.Display.MenuChanged += this.OnEnterMenu;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Remap a button on the keyboard, controller, or mouse.</summary>
        /// <param name="oldButton">The old button.</param>
        /// <param name="newButton">The new button.</param>
        /// <param name="isDown">The state of new button.</param>
        private void RemapButton(SButton oldButton, SButton newButton, bool isDown)
        {
            // suppress the old button
            //_overrideButtonMethod.Invoke(_inputState, new object[] { oldButton, false });
            this.Helper.Input.Suppress(oldButton);

            _overrideButtonMethod.Invoke(_inputState, new object[] { newButton, isDown });
        }

        /// <summary>Raised after the player opens a menu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnEnterMenu(object sender, MenuChangedEventArgs e)
        {
            //this.Monitor.Log($"{Game1.player.Name} opened {e.NewMenu}.", LogLevel.Debug);
            _activeMenu = e.NewMenu;
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // get state of button
            //SButtonState state = this.Helper.Input.GetState(e.Button);
            //bool isDown = (state == SButtonState.Pressed || state == SButtonState.Held);
            bool isDown = e.IsDown(e.Button);

            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // remap overworld bindings
            if (_activeMenu == null)
            {
                if (_remapOverworld.ContainsKey(e.Button))
                    RemapButton(e.Button, _remapOverworld[e.Button], isDown);
            }
            // remap in-menu bindings
            else
            {
                if (_remapInMenu.ContainsKey(e.Button))
                    RemapButton(e.Button, _remapInMenu[e.Button], isDown);
            }

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }
    }
}