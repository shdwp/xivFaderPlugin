using ImGuiNET;
using System;
using System.Numerics;
using FaderPlugin.Config;

namespace FaderPlugin
{
    public class PluginUI : IDisposable
    {
        private enum OverrideKeys
        {
            Alt   = 0x12,
            Ctrl  = 0x11,
            Shift = 0x10,
        }

        private Configuration configuration;

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private Vector2      _windowSize = new Vector2(1250, 600) * ImGui.GetIO().FontGlobalScale;
        private OverrideKeys CurrentOverrideKey => (OverrideKeys)configuration.OverrideKey;

        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (!SettingsVisible)
            {
                return;
            }

            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);

            if (ImGui.Begin("Fader Plugin Configuration", ref this.settingsVisible))
            {
                ImGui.Text("Focus key:");
                ImGuiHelpTooltip("When held interface will be setup as per 'UserFocus' column.");

                if (ImGui.BeginCombo("", CurrentOverrideKey.ToString()))
                {
                    foreach (var option in Enum.GetValues(typeof(OverrideKeys)))
                    {
                        if (ImGui.Selectable(option.ToString(), option.Equals(CurrentOverrideKey)))
                        {
                            configuration.OverrideKey = (int)option;
                            configuration.Save();
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.Text("Elements matrix:");
                ImGuiHelpTooltip("Decides what to do with each interface element when under certain conditions." +
                                 "\nThis settings wouldn't interfere with whatever is configured in HUD settings simply overriding them." +
                                 "\nIf behaviour of an element already satisfy you, or if you hide the element it via HUD setting you can leave it at Skip.");

                ImGui.Separator();

                ImGui.BeginChild("##settingsMatrix");
                var columnIndex = 0;
                ImGui.Columns(Enum.GetValues(typeof(FaderState)).Length);

                var buttonSize = ImGui.CalcTextSize("Combat");
                var columnWidth = ImGui.CalcTextSize("HasEnemyTarget ?");

                ImGui.Text("");
                foreach (var element in Enum.GetValues(typeof(ConfigElementId)))
                {
                    if (element.Equals(ConfigElementId.Unknown))
                    {
                        continue;
                    }

                    ImGui.Text(element.ToString());
                    var tooltipText = TooltipForElement((ConfigElementId)element);
                    if (tooltipText != null)
                    {
                        ImGuiHelpTooltip(tooltipText);
                    }
                }

                foreach (var state in Enum.GetValues(typeof(FaderState)))
                {
                    if (state.Equals(FaderState.None))
                    {
                        continue;
                    }

                    columnIndex++;
                    ImGui.NextColumn();
                    ImGui.SetColumnWidth(columnIndex, columnWidth.X + 20f);
                    ImGui.Text(state.ToString());
                    ImGuiHelpTooltip(TooltipForState((FaderState)state));

                    foreach (var element in Enum.GetValues(typeof(ConfigElementId)))
                    {
                        var elementId = (ConfigElementId)element;
                        if (elementId == ConfigElementId.Unknown)
                        {
                            continue;
                        }

                        var stateId = (FaderState)state;
                        var setting = configuration.GetSetting(elementId, stateId);
                        var buttonId = $"##{stateId}{elementId}";

                        switch (setting)
                        {
                            case ConfigElementSetting.Skip:
                            {
                                if (ImGui.Button("Skip" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Hide);
                                }
                                break;
                            }

                            case ConfigElementSetting.Hide:
                            {
                                if (ImGui.Button("Hide" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Show);
                                }
                                break;
                            }

                            case ConfigElementSetting.Show:
                            {
                                if (ImGui.Button("Show" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Skip);
                                }
                                break;
                            }
                        }
                    }
                }

                ImGui.EndChild();
            }

            _windowSize = ImGui.GetWindowSize();
            ImGui.End();
        }

        private void ImGuiHelpTooltip(string tooltip)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private string TooltipForState(FaderState state)
        {
            return state switch
            {
                FaderState.Combat          => "In combat",
                FaderState.Crafting        => "Crafting an item",
                FaderState.Duty            => "In instanced duty",
                FaderState.Gathering       => "Gathering a node",
                FaderState.UserFocus       => "Focus button pressed",
                FaderState.HasEnemyTarget  => "Targeting an enemy",
                FaderState.HasPlayerTarget => "Targeting a player",
                FaderState.HasNPCTarget    => "Targeting a NPC",
                FaderState.Idle            => "When other conditions are not active",
                _                          => "No tooltip",
            };
        }

        private string TooltipForElement(ConfigElementId elementId)
        {
            return elementId switch
            {
                ConfigElementId.Job                 => "Job-specific UI",
                ConfigElementId.Status              => "Player status (when not split into 3 separate elements)",
                ConfigElementId.StatusEnfeeblements => "Player enfeeblements (when split into 3 separate elements)",
                ConfigElementId.StatusEnhancements  => "Player enhancements (when split into 3 separate elements)",
                ConfigElementId.StatusOther         => "Player other status (when split into 3 separate elements)",
                _                                   => null,
            };
        }

        private void UpdateSetting(ConfigElementId id, FaderState state, ConfigElementSetting setting)
        {
            configuration.SetSetting(id, state, setting);
            configuration.Save();
        }
    }
}
