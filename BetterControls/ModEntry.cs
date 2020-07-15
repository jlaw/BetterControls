using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using xTile.Dimensions;
using StardewValley.Menus;

namespace BetterControls
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        internal class ModHooksWrapper : ModHooks
        {
            private readonly ModHooks _existingHooks;
            private static ModEntry _mod;

            internal static ModHooksWrapper CreateWrapper(ModEntry mod)
            {
                _mod = mod;

                try
                {
                    FieldInfo hooksField = _mod.Helper.Reflection.GetField<ModHooks>(typeof(Game1), "hooks").FieldInfo;
                    ModHooksWrapper wrapper = new ModHooksWrapper((ModHooks)hooksField.GetValue(null));
                    hooksField.SetValue(null, wrapper);
                    _mod.Monitor.Log($"Successfully wrapped Game1.hooks!", LogLevel.Debug);
                    return wrapper;
                }
                catch (Exception e)
                {
                    _mod.Monitor.Log($"Failed to wrap Game1.hooks: {e.Message}", LogLevel.Debug);
                }

                return null;
            }

            private ModHooksWrapper(ModHooks existingHooks)
            {
                _existingHooks = existingHooks;
            }

            public override void OnGame1_PerformTenMinuteClockUpdate(Action action)
            {
                this._existingHooks.OnGame1_PerformTenMinuteClockUpdate(action);
            }

            public override void OnGame1_NewDayAfterFade(Action action)
            {
                this._existingHooks.OnGame1_NewDayAfterFade(action);
            }

            public override void OnGame1_ShowEndOfNightStuff(Action action)
            {
                this._existingHooks.OnGame1_ShowEndOfNightStuff(action);
            }

            public override void OnGame1_UpdateControlInput(ref KeyboardState keyboardState, ref MouseState mouseState, ref GamePadState gamePadState, Action action)
            {
                _mod.Monitor.Log($"Original gamepad state: {gamePadState.Buttons.ToString()}.", LogLevel.Debug);
                _mod.Monitor.Log($"Original gamepad state: {gamePadState.GetHashCode().ToString("X")}.", LogLevel.Debug);

                // get the state of all buttons
                Buttons origButtons = (Buttons) (gamePadState.Buttons.GetHashCode() | gamePadState.DPad.GetHashCode());
                _mod.Monitor.Log($"Original button hash: {origButtons.ToString("X")}", LogLevel.Debug);

                // remap buttons according to dictionary
                Buttons newButtons = 0;
                foreach (var buttonMap in _remapGamePad)
                    newButtons |= ((origButtons & buttonMap.Key) == buttonMap.Key) ? buttonMap.Value : 0;
                _mod.Monitor.Log($"New button hash: {newButtons.ToString("X")}", LogLevel.Debug);

                // set new button states
                GamePadButtons newGamePadButtons = new GamePadButtons(newButtons);

                // set new DPad states
                ButtonState newDPadUpState    = (newButtons & Buttons.DPadUp)    == Buttons.DPadUp    ? ButtonState.Pressed : ButtonState.Released;
                ButtonState newDPadDownState  = (newButtons & Buttons.DPadDown)  == Buttons.DPadDown  ? ButtonState.Pressed : ButtonState.Released;
                ButtonState newDPadLeftState  = (newButtons & Buttons.DPadLeft)  == Buttons.DPadLeft  ? ButtonState.Pressed : ButtonState.Released;
                ButtonState newDPadRightState = (newButtons & Buttons.DPadRight) == Buttons.DPadRight ? ButtonState.Pressed : ButtonState.Released;
                GamePadDPad newGamePadDPad = new GamePadDPad(newDPadUpState, newDPadDownState, newDPadLeftState, newDPadRightState);

                // pass new states to existing hooks
                gamePadState = new GamePadState(gamePadState.ThumbSticks, gamePadState.Triggers, newGamePadButtons, newGamePadDPad);
                //_mod.Monitor.Log($"New gamepad state: {gamePadState.ToString()}.", LogLevel.Debug);
                _mod.Monitor.Log($"New button state: {gamePadState.GetHashCode().ToString("X")}", LogLevel.Debug);
                this._existingHooks.OnGame1_UpdateControlInput(ref keyboardState, ref mouseState, ref gamePadState, action);
            }

            public override void OnGameLocation_ResetForPlayerEntry(GameLocation location, Action action)
            {
                this._existingHooks.OnGameLocation_ResetForPlayerEntry(location, action);
            }

            public override bool OnGameLocation_CheckAction(GameLocation location, Location tileLocation, Rectangle viewport, Farmer who, Func<bool> action)
            {
                return this._existingHooks.OnGameLocation_CheckAction(location, tileLocation, viewport, who, action);
            }

            public override FarmEvent OnUtility_PickFarmEvent(Func<FarmEvent> action)
            {
                return this._existingHooks.OnUtility_PickFarmEvent(action);
            }

            private readonly Dictionary<Buttons, Buttons> _remapGamePad = new Dictionary<Buttons, Buttons>
            {
                {Buttons.DPadUp,        Buttons.DPadDown},
                {Buttons.DPadDown,      Buttons.DPadUp},
                {Buttons.DPadLeft,      Buttons.DPadRight},
                {Buttons.DPadRight,     Buttons.DPadLeft},
                {Buttons.A,             Buttons.X},
                {Buttons.B,             Buttons.B},
                {Buttons.X,             Buttons.A},
                {Buttons.Y,             Buttons.Y},
                {Buttons.Start,         Buttons.Start},
                {Buttons.Back,          Buttons.Back},
                {Buttons.BigButton,     Buttons.BigButton},
                {Buttons.LeftStick,     Buttons.LeftStick},
                {Buttons.RightStick,    Buttons.RightStick},
                {Buttons.LeftShoulder,  Buttons.LeftShoulder},
                {Buttons.RightShoulder, Buttons.RightShoulder},
                {Buttons.LeftTrigger,   Buttons.LeftTrigger},
                {Buttons.RightTrigger,  Buttons.RightTrigger},
            }; 
        }

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
            ModHooksWrapper.CreateWrapper(this);

            //// get SMAPI's input handler
            //_inputState = helper.Input.GetType().GetField("InputState", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(helper.Input);

            //// get OverrideButton method
            //_overrideButtonMethod = _inputState?.GetType().GetMethod("OverrideButton");

            //helper.Events.Display.MenuChanged += this.OnEnterMenu;
            //helper.Events.Input.ButtonPressed += this.OnButtonPressed;
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