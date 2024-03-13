using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FaderPlugin {
    public static unsafe class Addon {
        private static readonly AtkStage* stage = AtkStage.GetSingleton();

        private static IntPtr hotbar;
        private static IntPtr crossbar;

        private static Dictionary<string, (short, short)> storedPositions = new();
        private static Dictionary<string, bool> lastState = new();

        private static bool IsAddonOpen(string name) {
            IntPtr addonPointer = Plugin.GameGui.GetAddonByName(name, 1);
            return addonPointer != IntPtr.Zero;
        }

        public static bool HasAddonStateChanged(string name) {
            bool currentState = IsAddonOpen(name);
            bool changed = !lastState.ContainsKey(name) || lastState[name] != currentState;

            lastState[name] = currentState;

            return changed;
        }

        private static bool IsAddonFocused(string name) {
            foreach (var addon in stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList.EntriesSpan)
            {
                if (addon.Value == null || addon.Value->Name == null)
                    continue;

                if (MemoryHelper.EqualsZeroTerminatedString(name, (nint) addon.Value->Name))
                    return true;
            }

            return false;
        }

        public static bool IsHudManagerOpen() {
            return IsAddonOpen("HudLayout");
        }

        public static bool HasHudManagerStateChanged() {
            return HasAddonStateChanged("HudLayout");
        }

        public static bool IsChatFocused()
        {
            // Check for ChatLogPanel_[0-3] as well to prevent chat from disappearing while user is scrolling through logs via controller input
            return IsAddonFocused("ChatLog")
                   || IsAddonFocused("ChatLogPanel_0")
                   || IsAddonFocused("ChatLogPanel_1")
                   || IsAddonFocused("ChatLogPanel_2")
                   || IsAddonFocused("ChatLogPanel_3");
        }

        public static bool AreHotbarsLocked() {
            if(hotbar == IntPtr.Zero) {
                hotbar = Plugin.GameGui.GetAddonByName("_ActionBar", 1);
            }

            if(crossbar == IntPtr.Zero) {
                crossbar = Plugin.GameGui.GetAddonByName("_ActionCross", 1);
            }

            try {
                // Check whether Mouse Mode or Gamepad Mode is enabled.
                var mouseModeEnabled = Marshal.ReadByte(hotbar, 0x1d6) == 0;

                if(mouseModeEnabled) {
                    return Marshal.ReadByte(hotbar, 0x23f) != 0;
                }
                return Marshal.ReadByte(crossbar, 0x23f) != 0;
            } catch(AccessViolationException) {
                return true;
            }
        }

        public static void SetAddonVisibility(string name, bool isVisible) {
            IntPtr addonPointer = Plugin.GameGui.GetAddonByName(name, 1);
            if(addonPointer == IntPtr.Zero) {
                return;
            }

            AtkUnitBase* addon = (AtkUnitBase*)addonPointer;

            if(isVisible) {
                // Restore the elements position on screen.
                if (storedPositions.TryGetValue(name, out var position) && (addon->X == -9999 || addon->Y == -9999))
                {
                    var (x, y) = position;
                    addon->SetPosition(x, y);
                }
            } else {
                // Store the position prior to hiding the element.
                if(addon->X != -9999 && addon->Y != -9999) {
                    storedPositions[name] = (addon->X, addon->Y);
                }

                // Move the element off screen so it can't be interacted with.
                addon->SetPosition(-9999, -9999);
            }
        }

        public static bool IsWeaponUnsheathed()
        {
            return UIState.Instance()->WeaponState.IsUnsheathed;
        }

        public static bool InSanctuary()
        {
            return GameMain.IsInSanctuary();
        }
    }
}