using System.Collections.Generic;
using StardewModdingAPI;

namespace BetterControls
{
    public class ModConfig
    {
        public Dictionary<string, KeymapString> Keymaps { get; set; }

        public ModConfig()
        {
            Keymaps = new Dictionary<string, KeymapString>
            {
                {
                    "Global", new KeymapString
                    {
                        {"Up", "DPadUp"}, //     Up
                        {"Down", "DPadDown"}, //     Down
                        {"Left", "DPadLeft"}, //     Left
                        {"Right", "DPadRight"}, //     Right
                        {"Space", "ControllerA"}, //     Select
                        {"Enter", "ControllerA"}, //     Select
                        {SButton.PageUp.ToString(), "LeftTrigger"}, //     Select Previous Tab/Left Tool
                        {SButton.PageDown.ToString(), "RightTrigger"}, //     Select Next Tab/Right Tool
                        {"LeftShoulder", "LeftTrigger"}, //     Select Previous Tab/Left Tool
                        {"RightShoulder", "RightTrigger"}, //     Select Next Tab/Right Tool
                        {"LeftStick", "M"}, //     Map (Default: nothing)
                        {"RightStick", "F1"}, //     LookupAnything (Default: chat/emoji)
                    }
                },
                {
                    "Overworld", new KeymapString
                    {
                        {"DPadUp", "B"}, //     ChestAnywhere
                        {"DPadLeft", "None"},
                        {"DPadDown", "Tab"}, //     Shift Toolbar
                        {"DPadRight", "K"}, //     GeodeInfo
                        //{"ControllerA",     "ControllerA"},   // Default: Check/Do Action
                        //{"ControllerB",     "ControllerB"},   // Default: Inventory
                        //{"ControllerX",     "ControllerX"},   // Default: Use Tool
                        //{"ControllerY",     "ControllerY"},   // Default: Crafting
                        //{"LeftTrigger",     "LeftTrigger"},   // Default: Select Left Tool
                        {"RightTrigger", "ControllerX"}, //     Use Tool (Default: Select Right Tool)
                        //{"ControllerBack",  "ControllerBack"},// Default: Journal
                        {"ControllerStart", "N"}, //     Pause/TimeSpeed (Default: Menu)
                    }
                },
                {
                    "GameMenu", new KeymapString
                    {
                    }
                },
                {
                    "ItemGrabMenu", new KeymapString
                    {
                        {"LeftTrigger", "LeftShoulder"}, //     ChestAnywhere: Select Previous Tab
                        {"RightTrigger", "RightShoulder"}, //     ChestAnywhere: Select Next Tab
                        {"ControllerY", "Q"}, //     OrganizeShortcut: StackToChest
                    }
                },
                {
                    "TitleMenu", new KeymapString
                    {
                    }
                }
            };
        }
    }

    public class Keymap : Dictionary<SButton, SButton>
    {
    }

    public class KeymapString : Dictionary<string, string>
    {
    }
}