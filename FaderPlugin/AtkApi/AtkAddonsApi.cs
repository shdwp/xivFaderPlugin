using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FaderPlugin.AtkApi
{
    public sealed unsafe class AtkAddonsApi
    {
        private AtkStage* stage;

        public AtkAddonsApi()
        {
            this.stage = AtkStage.GetSingleton();
        }

        public void UpdateAddonVisibility(Func<string, bool?> predicate)
        {
            var loadedUnitsList = &this.stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
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
