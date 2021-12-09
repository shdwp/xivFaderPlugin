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

            return Marshal.ReadByte(_addon, 0x23f) != 0;
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
                    if (value == true)
                    {
                        if (addon->UldManager.NodeListCount == 0)
                        {
                            addon->UldManager.UpdateDrawNodeList();
                        }
                    }
                    else if (value == false)
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
}