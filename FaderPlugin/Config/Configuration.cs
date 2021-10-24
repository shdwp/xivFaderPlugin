using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FaderPlugin.Config
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public Dictionary<FaderState, Dictionary<ConfigElementId, ConfigElementSetting>> ElementsTable { get; set; }

        public long IdleTransitionDelay { get; set; } = 2000;

        public int OverrideKey { get; set; } = 0x12;

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            if (ElementsTable == null)
            {
                ElementsTable = new Dictionary<FaderState, Dictionary<ConfigElementId, ConfigElementSetting>>();
                ElementsTable[FaderState.Idle] = new Dictionary<ConfigElementId, ConfigElementSetting>
                {
                    { ConfigElementId.ActionBar01, ConfigElementSetting.Hide },
                    { ConfigElementId.ActionBar02, ConfigElementSetting.Hide },
                    { ConfigElementId.ActionBar03, ConfigElementSetting.Hide },
                    { ConfigElementId.ActionBar04, ConfigElementSetting.Hide },
                    { ConfigElementId.CrossHotbar, ConfigElementSetting.Hide },
                    { ConfigElementId.Parameters, ConfigElementSetting.Hide },
                    { ConfigElementId.TargetInfo, ConfigElementSetting.Hide },
                    { ConfigElementId.StatusEnhancements, ConfigElementSetting.Hide },
                    { ConfigElementId.Job, ConfigElementSetting.Hide },
                };

                ElementsTable[FaderState.Combat] = new Dictionary<ConfigElementId, ConfigElementSetting>
                {
                    { ConfigElementId.ActionBar01, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar02, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar03, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar04, ConfigElementSetting.Show },
                    { ConfigElementId.CrossHotbar, ConfigElementSetting.Show },
                    { ConfigElementId.Parameters, ConfigElementSetting.Show },
                    { ConfigElementId.TargetInfo, ConfigElementSetting.Show },
                    { ConfigElementId.StatusEnhancements, ConfigElementSetting.Show },
                    { ConfigElementId.Job, ConfigElementSetting.Show },
                };

                ElementsTable[FaderState.HasEnemyTarget] = new Dictionary<ConfigElementId, ConfigElementSetting>
                {
                    { ConfigElementId.ActionBar01, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar02, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar03, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar04, ConfigElementSetting.Show },
                    { ConfigElementId.CrossHotbar, ConfigElementSetting.Show },
                    { ConfigElementId.Parameters, ConfigElementSetting.Show },
                    { ConfigElementId.TargetInfo, ConfigElementSetting.Show },
                    { ConfigElementId.StatusEnhancements, ConfigElementSetting.Show },
                    { ConfigElementId.Job, ConfigElementSetting.Show },
                };

                ElementsTable[FaderState.UserFocus] = new Dictionary<ConfigElementId, ConfigElementSetting>
                {
                    { ConfigElementId.ActionBar01, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar02, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar03, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar04, ConfigElementSetting.Show },
                    { ConfigElementId.CrossHotbar, ConfigElementSetting.Show },
                    { ConfigElementId.Parameters, ConfigElementSetting.Show },
                    { ConfigElementId.TargetInfo, ConfigElementSetting.Show },
                    { ConfigElementId.StatusEnhancements, ConfigElementSetting.Show },
                    { ConfigElementId.Job, ConfigElementSetting.Show },
                };

                ElementsTable[FaderState.Crafting] = new Dictionary<ConfigElementId, ConfigElementSetting>
                {
                    { ConfigElementId.ActionBar01, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar02, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar03, ConfigElementSetting.Show },
                    { ConfigElementId.ActionBar04, ConfigElementSetting.Show },
                    { ConfigElementId.CrossHotbar, ConfigElementSetting.Show },
                    { ConfigElementId.Parameters, ConfigElementSetting.Show },
                    { ConfigElementId.TargetInfo, ConfigElementSetting.Hide },
                    { ConfigElementId.StatusEnhancements, ConfigElementSetting.Show },
                    { ConfigElementId.Job, ConfigElementSetting.Show },
                };
            }

            Save();
        }

        public ConfigElementSetting ShouldDisplayElement(string name, FaderState state)
        {
            return GetSetting(ConfigElementByName(name), state);
        }

        public ConfigElementSetting GetSetting(ConfigElementId id, FaderState state)
        {
            if (ElementsTable.TryGetValue(state, out var table))
            {
                if (table.TryGetValue(id, out var setting))
                {
                    return setting;
                }
            }

            return ConfigElementSetting.Skip;
        }

        public void SetSetting(ConfigElementId id, FaderState state, ConfigElementSetting setting)
        {
            if (!ElementsTable.TryGetValue(state, out var table))
            {
                table = new Dictionary<ConfigElementId, ConfigElementSetting>();
                ElementsTable[state] = table;
            }

            table[id] = setting;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }

        private ConfigElementId ConfigElementByName(string name)
        {
            if (name.StartsWith("JobHud"))
            {
                return ConfigElementId.Job;
            }

            return name switch
            {
                "_ActionBar"            => ConfigElementId.ActionBar01,
                "_ActionBar01"          => ConfigElementId.ActionBar02,
                "_ActionBar02"          => ConfigElementId.ActionBar03,
                "_ActionBar03"          => ConfigElementId.ActionBar04,
                "_ActionBar04"          => ConfigElementId.ActionBar05,
                "_ActionBar05"          => ConfigElementId.ActionBar06,
                "_ActionBar06"          => ConfigElementId.ActionBar07,
                "_ActionBar07"          => ConfigElementId.ActionBar08,
                "_ActionBar08"          => ConfigElementId.ActionBar09,
                "_ActionBar09"          => ConfigElementId.ActionBar10,
                "_ActionCross"          => ConfigElementId.CrossHotbar,
                "_ActionDoubleCrossL"   => ConfigElementId.CrossHotbar,
                "_ActionDoubleCrossR"   => ConfigElementId.CrossHotbar,
                "_PartyList"            => ConfigElementId.PartyList,
                "_LimitBreak"           => ConfigElementId.LimitBreak,
                "_ParameterWidget"      => ConfigElementId.Parameters,
                "_TargetInfo"           => ConfigElementId.TargetInfo,
                "_TargetInfoBuffDebuff" => ConfigElementId.TargetInfo,
                "_TargetInfoCastBar"    => ConfigElementId.TargetInfo,
                "_TargetInfoMainTarget" => ConfigElementId.TargetInfo,
                "_Status"               => ConfigElementId.Status,
                "_StatusCustom0"        => ConfigElementId.StatusEnhancements,
                "_StatusCustom1"        => ConfigElementId.StatusEnfeeblements,
                "_StatusCustom2"        => ConfigElementId.StatusOther,

                _ => ConfigElementId.Unknown,
            };
        }
    }
}
