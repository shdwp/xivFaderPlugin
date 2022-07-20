using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FaderPlugin {
    public sealed unsafe class Addon {
        private static readonly AtkStage* stage = AtkStage.GetSingleton();

        private static IntPtr hotbar;
        private static IntPtr crossbar;

        private static Dictionary<string, (short, short)> storedPositions = new();
        private static Dictionary<string, bool> lastState = new();

        public static bool IsAddonOpen(string name) {
            IntPtr addonPointer = Plugin.GameGui.GetAddonByName(name, 1);
            return addonPointer != IntPtr.Zero;
        }

        public static bool HasAddonStateChanged(string name) {
            bool currentState = IsAddonOpen(name);
            bool changed = false;

            if(!lastState.ContainsKey(name) || lastState[name] != currentState) {
                changed = true;
            }

            lastState[name] = currentState;

            return changed;
        }

        public static bool IsAddonFocused(string name) {
            var focusedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var focusedAddonList = &focusedUnitsList->AtkUnitEntries;

            for(var i = 0; i < focusedUnitsList->Count; i++) {
                var addon = focusedAddonList[i];
                var addonName = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));
                

                if(addonName == name) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsHudManagerOpen() {
            return IsAddonOpen("HudLayout");
        }

        public static bool HasHudManagerStateChanged() {
            return HasAddonStateChanged("HudLayout");
        }

        public static bool IsChatFocused() {
            return IsAddonFocused("ChatLog");
        }

        public static bool AreHotbarsLocked() {
            if(hotbar == IntPtr.Zero) {
                hotbar = Plugin.GameGui.GetAddonByName("_ActionBar", 1);
            }

            if(crossbar == IntPtr.Zero) {
                crossbar = Plugin.GameGui.GetAddonByName("_ActionCross", 1);
            }

            try {
                // Ccheck whether Mouse Mode or Gamepad Mode is enabled.
                var mouseModeEnabled = Marshal.ReadByte(hotbar, 0x1d6) == 0;

                if(mouseModeEnabled == true) {
                    return Marshal.ReadByte(hotbar, 0x23f) != 0;
                } else {
                    return Marshal.ReadByte(crossbar, 0x23f) != 0;
                }
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
                bool positionExists = storedPositions.TryGetValue(name, out var position);
                if(positionExists && addon->X == -9999) {
                    var (x, y) = position;
                    addon->SetPosition(x, y);
                }
            } else {
                // Store the position prior to hiding the element.
                if(addon->X != -9999) {
                    storedPositions[name] = (addon->X, addon->Y);
                }

                // Move the element off screen so it can't be interacted with.
                addon->SetPosition(-9999, -9999);
            }
        }
    }
}