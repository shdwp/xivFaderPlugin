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
        public bool DefaultDelayEnabled { get; set; } = true;
        public int DefaultDelay { get; set; } = 2000;
        public int ChatActivityTimeout { get; set; } = 5 * 1000;
        public int OverrideKey { get; set; } = 0x12;
        public bool FocusOnHotbarsUnlock { get; set; } = false;
        public bool EmoteActivity { get; set; } = false;
        public bool ImportantActivity { get; set; } = false;

        public void Initialize() {
            // Initialise the config.
            if (elementsConfig == null) {
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
    }
}