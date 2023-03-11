using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace FaderPlugin.Config {

    public class ConfigEntry {
        public State state { get; set; }
        public Setting setting { get; set; }

        public ConfigEntry(State state, Setting setting) {
            this.state = state;
            this.setting = setting;
        }
    }

    [Serializable]
    public class Config : IPluginConfiguration {
        public event Action? OnSave;

        public int Version { get; set; } = 6;
        public Dictionary<Element, List<ConfigEntry>> elementsConfig { get; set; }
        public int DefaultDelay { get; set; } = 2000;
        public int ChatActivityTimeout { get; set; } = 5 * 1000;
        public int OverrideKey { get; set; } = 0x12;
        public bool FocusOnHotbarsUnlock { get; set; } = false;

        public void Initialize() {
            // Initialise the config.
            if(elementsConfig == null) {
                elementsConfig = new Dictionary<Element, List<ConfigEntry>>();
            }

            foreach(Element element in Enum.GetValues(typeof(Element))) {
                if(!elementsConfig.ContainsKey(element)) {
                    List<ConfigEntry> entry = new() { new ConfigEntry(State.Default, Setting.Show) };
                    elementsConfig[element] = entry;
                }
            }

            Save();
        }

        public bool DefaultDelayEnabled() {
            return this.DefaultDelay != 0;
        }

        public List<ConfigEntry> GetElementConfig(Element elementId) {
            if(!elementsConfig.ContainsKey(elementId)) {
                elementsConfig[elementId] = new();
            }

            return elementsConfig[elementId];
        }

        public void Save() {
            Plugin.PluginInterface.SavePluginConfig(this);
            OnSave?.Invoke();
        }

        public Element ConfigElementByName(string name) {
            if(name.StartsWith("JobHud")) {
                return Element.Job;
            }

            if(name.StartsWith("ChatLog")) {
                return Element.Chat;
            }

            return name switch {
                "_ActionBar" => Element.Hotbar1,
                "_ActionBar01" => Element.Hotbar2,
                "_ActionBar02" => Element.Hotbar3,
                "_ActionBar03" => Element.Hotbar4,
                "_ActionBar04" => Element.Hotbar5,
                "_ActionBar05" => Element.Hotbar6,
                "_ActionBar06" => Element.Hotbar7,
                "_ActionBar07" => Element.Hotbar8,
                "_ActionBar08" => Element.Hotbar9,
                "_ActionBar09" => Element.Hotbar10,
                "_ActionCross" => Element.CrossHotbar,
                "_ActionBarEx" => Element.PetHotbar,
                "_ActionDoubleCrossL" => Element.CrossHotbar,
                "_ActionDoubleCrossR" => Element.CrossHotbar,
                "_PartyList" => Element.PartyList,
                "_LimitBreak" => Element.LimitBreak,
                "_ParameterWidget" => Element.Parameters,
                "_Status" => Element.Status,
                "_StatusCustom0" => Element.StatusEnhancements,
                "_StatusCustom1" => Element.StatusEnfeeblements,
                "_StatusCustom2" => Element.StatusOther,
                "_CastBar" => Element.CastBar,
                "_Exp" => Element.ExperienceBar,
                "ScenarioTree" => Element.ScenarioGuide,
                "_BagWidget" => Element.InventoryGrid,
                "_MainCommand" => Element.MainMenu,
                "_NaviMap" => Element.Minimap,
                "_Money" => Element.Currency,

                "_TargetInfoMainTarget" => Element.TargetInfo,
                "_TargetInfoBuffDebuff" => Element.TargetInfo,
                "_TargetInfoCastBar" => Element.TargetInfo,
                "_TargetInfo" => Element.TargetInfo,

                _ => Element.Unknown,
            };
        }
    }
}