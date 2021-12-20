using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.Colors;
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
                // @TODO: remove on net core transition
                var grey = new Vector4(0.9f, 0.9f, 0.9f, 1f);
                var green = new Vector4(0f, 0.8f, 0.13f, 1f);
                var red = new Vector4(1f, 0f, 0f, 1f);

                ImGui.Text("User Focus key:");
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

                var focusOnHotbarsUnlock = configuration.FocusOnHotbarsUnlock;
                if (ImGui.Checkbox("##focus_on_unlocked_bars", ref focusOnHotbarsUnlock))
                {
                    this.configuration.FocusOnHotbarsUnlock = focusOnHotbarsUnlock;
                    this.configuration.Save();
                }
                ImGui.SameLine();
                ImGui.Text("Always User Focus when hotbars are unlocked");
                ImGuiHelpTooltip("When hotbars are unlocked always setup to the UserFocus column.");

                var idleDelay = (float)TimeSpan.FromMilliseconds(configuration.IdleTransitionDelay).TotalSeconds;
                ImGui.Text("Idle transition delay:");
                ImGui.SameLine();
                if (ImGui.SliderFloat("##idle_delay", ref idleDelay, 0.1f, 15f))
                {
                    this.configuration.IdleTransitionDelay = (long)TimeSpan.FromSeconds(idleDelay).TotalMilliseconds;
                    this.configuration.Save();
                }
                ImGuiHelpTooltip("Amount of time it takes to go back to the `Idle` column.");

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

                    /*
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, green);
                    if (ImGui.Button("o##show_all_" + state))
                    {

                    }
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, red);
                    if (ImGui.Button("x##hide_all_" + state))
                    {

                    }
                    ImGui.PopStyleColor();

                    ImGui.PushStyleColor(ImGuiCol.Text, grey);
                    ImGui.SameLine();
                    if (ImGui.Button("~##skip_all_" + state))
                    {

                    }
                    ImGui.PopStyleColor();
                    */

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
                                ImGui.PushStyleColor(ImGuiCol.Text, grey);
                                if (ImGui.Button("skip" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Hide);
                                }
                                ImGui.PopStyleColor();
                                break;
                            }

                            case ConfigElementSetting.Hide:
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, red);
                                if (ImGui.Button("hide" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Show);
                                }
                                ImGui.PopStyleColor();
                                break;
                            }

                            case ConfigElementSetting.Show:
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, green);
                                if (ImGui.Button("show" + buttonId, buttonSize))
                                {
                                    UpdateSetting(elementId, stateId, ConfigElementSetting.Skip);
                                }
                                ImGui.PopStyleColor();
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
                ConfigElementId.Chat                => "Should be always visible if focused, albeit feature can be buggy with some configurations",
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