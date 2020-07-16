using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace BetterControls
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private IClickableMenu _activeMenu;

        private readonly Dictionary<(Type, Type), KeyMap> _remapOverworld = new Dictionary<(Type, Type), KeyMap>
        {
            {
                (typeof(GamePadState), typeof(GamePadState)),
                new KeyMap
                {
                    { nameof(GamePadState.Buttons.B), nameof(GamePadState.Buttons.A) },
                }
            },
            //{SButton.DPadUp,          SButton.B},               //     ChestAnywhere
            //{SButton.DPadLeft,        SButton.None},
            //{SButton.DPadDown,        SButton.Tab},             //     Shift Toolbar
            //{SButton.DPadRight,       SButton.K},               //     GeodeInfo
            //{SButton.ControllerA,     SButton.ControllerA},   // Default: Check/Do Action
            //{SButton.ControllerB,     SButton.ControllerB},   // Default: Inventory
            //{SButton.ControllerX,     SButton.ControllerX},   // Default: Use Tool
            //{SButton.ControllerY,     SButton.ControllerY},   // Default: Crafting
            //{SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Left Tool (Default: Shift Toolbar)
            //{SButton.RightShoulder,   SButton.RightTrigger},    //     Select Right Tool (Default: Shift Toolbar)
            //{SButton.LeftTrigger,     SButton.LeftTrigger},   // Default: Select Left Tool
            //{SButton.RightTrigger,    SButton.C},             //     Use Tool (Default: Select Right Tool)
            //{SButton.ControllerBack,  SButton.ControllerBack},// Default: Journal
            //{SButton.ControllerStart, SButton.N},               //     Pause/TimeSpeed (Default: Menu)
            //{SButton.LeftStick,       SButton.M},               //     Map (Default: nothing)
            //{SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
        };

        private readonly Dictionary<(Type, Type), KeyMap> _remapInMenu = new Dictionary<(Type, Type), KeyMap>
        {
            //{SButton.ControllerY,     SButton.Q},               //     OrganizeShortcut: StackToChest
            //{SButton.LeftShoulder,    SButton.LeftTrigger},     //     Select Previous Tab
            //{SButton.RightShoulder,   SButton.RightTrigger},    //     Select Next Tab
            //{SButton.RightStick,      SButton.F1},              //     LookupAnything (Default: chat/emoji)
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

            helper.Events.Display.MenuChanged += this.OnEnterMenu;
        }

        /// <summary>Raised after the player opens a menu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnEnterMenu(object sender, MenuChangedEventArgs e)
        {
            InputPatch.SetMap(_remapInMenu);
        }
    }
}