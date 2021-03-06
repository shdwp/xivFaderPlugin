using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FaderPlugin.AtkApi
{
    public sealed unsafe class AtkAddonsApi
    {
        private readonly AtkStage* _stage;
        private readonly GameGui _gameGui;

        private IntPtr _hotbar;
        private IntPtr _crossbar;

        private Dictionary<string, (short, short)> storedPositions = new Dictionary<string, (short, short)>();

        public AtkAddonsApi(GameGui gameGui)
        {
            _gameGui = gameGui;
            _stage = AtkStage.GetSingleton();
        }

        public bool AreHotbarsLocked()
        {
            if (_hotbar == IntPtr.Zero)
            {
                _hotbar = _gameGui.GetAddonByName("_ActionBar", 1);
            }

            if (_crossbar == IntPtr.Zero)
            {
                _crossbar = _gameGui.GetAddonByName("_ActionCross", 1);
            }

            try
            {
                // here we check whether Mouse Mode or Gamepad Mode is enabled
                var mouseModeEnabled = Marshal.ReadByte(_hotbar, 0x1d6) == 0;

                if (mouseModeEnabled == true)
                {
                    return Marshal.ReadByte(_hotbar, 0x23f) != 0;
                }
                else
                {
                    return Marshal.ReadByte(_crossbar, 0x23f) != 0;
                }
            }
            catch (AccessViolationException)
            {
                return true;
            }
        }

        public bool IsChatFocused()
        {
            return CheckIfFocused("ChatLog");
        }

        public void UpdateAddonVisibility(Func<string, bool?> predicate, bool moveElementOffscreen = false)
        {
            var loadedUnitsList = &this._stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            var addonList = &loadedUnitsList->AtkUnitEntries;

            for (var i = 0; i < loadedUnitsList->Count; i++)
            {
                var addon = addonList[i];
                var name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));

                if (name != null)
                {
                    var value = predicate(name);
                    if (value == true)
                    {
                        if (!moveElementOffscreen)
                        {
                            if (addon->UldManager.NodeListCount == 0)
                            {
                                addon->UldManager.UpdateDrawNodeList();
                            }
                        }
                        else
                        {
                            // Restore the elements position on screen.
                            bool positionExists = this.storedPositions.TryGetValue(name, out var position);
                            if (positionExists && addon->X == -9999)
                            {
                                var (x, y) = position;
                                addon->SetPosition(x, y);
                            }
                        }
                    }
                    else if (value == false)
                    {
                        if (!moveElementOffscreen)
                        {
                            if (addon->UldManager.NodeListCount != 0)
                            {
                                addon->UldManager.NodeListCount = 0;
                            }
                        }
                        else
                        {
                            // Store the position prior to hiding the element.
                            if (addon->X != -9999)
                            {
                                this.storedPositions[name] = (addon->X, addon->Y);
                            }

                            // Move the element off screen so it can't be interacted with.
                            addon->SetPosition(-9999, -9999);
                        }
                    }
                }
            }
        }

        public bool CheckIfFocused(string name)
        {
            var focusedUnitsList = &this._stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var focusedAddonList = &focusedUnitsList->AtkUnitEntries;

            for (var i = 0; i < focusedUnitsList->Count; i++)
            {
                var addon = focusedAddonList[i];
                var addonName = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));

                if (addonName == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}