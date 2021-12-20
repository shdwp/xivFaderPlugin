using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FaderPlugin.AtkApi
{
    public sealed unsafe class AtkAddonsApi
    {
        private readonly AtkStage* _stage;
        private readonly GameGui   _gameGui;

        private IntPtr _addon;

        public AtkAddonsApi(GameGui gameGui)
        {
            _gameGui = gameGui;
            this._stage = AtkStage.GetSingleton();
        }

        public bool AreHotbarsLocked()
        {
            if (_addon == IntPtr.Zero)
            {
                _addon = _gameGui.GetAddonByName("_ActionBar", 1);
            }

            try
            {

                return Marshal.ReadByte(_addon, 0x23f) != 0;
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

        public void UpdateAddonVisibility(Func<string, bool?> predicate)
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
                    if (value.HasValue)
                    {
                        if (value.Value)
                        {
                            if (addon->UldManager.NodeListCount == 0)
                            {
                                addon->UldManager.UpdateDrawNodeList();
                            }
                        }
                        else
                        {
                            if (addon->UldManager.NodeListCount != 0)
                            {
                                addon->UldManager.NodeListCount = 0;
                            }
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