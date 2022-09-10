using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using FaderPlugin.Config;
using ImGuiNET;

namespace FaderPlugin {
    public class PluginUI : IDisposable {
        private enum OverrideKeys {
            Alt = 0x12,
            Ctrl = 0x11,
            Shift = 0x10,
        }

        private readonly Config.Config config;
        private List<ConfigEntry> selectedConfig;
        private readonly List<Element> selectedElements;

        private bool settingsVisible = false;

        public bool SettingsVisible {
            get { return settingsVisible; }
            set { settingsVisible = value; }
        }

        private Vector2 _windowSize = new Vector2(730, 670) * ImGui.GetIO().FontGlobalScale;
        private OverrideKeys CurrentOverrideKey => (OverrideKeys)config.OverrideKey;
        private HttpClient _httpClient = new HttpClient();
        private string _noticeString;
        private string _noticeUrl;

        public PluginUI(Config.Config configuration) {
            this.config = configuration;
            this.selectedElements = new();

            DownloadAndParseNotice();
        }

        private void DownloadAndParseNotice() {
            try {
                var stringAsync = _httpClient.GetStringAsync("https://shdwp.github.io/ukraine/xiv_notice.txt");
                stringAsync.Wait();
                var strArray = stringAsync.Result.Split('|');

                if((uint)strArray.Length > 0U) {
                    _noticeString = strArray[0];
                }

                if(strArray.Length <= 1) {
                    return;
                }

                _noticeUrl = strArray[1];

                if(!(_noticeUrl.StartsWith("http://") || _noticeUrl.StartsWith("https://"))) {
                    PluginLog.Warning($"Received invalid noticeUrl {_noticeUrl}, ignoring");
                    _noticeUrl = null;
                }
            } catch(Exception ex) {
            }
        }

        private void DisplayNotice() {
            if(_noticeString == null) {
                return;
            }

            ImGui.PushStyleColor((ImGuiCol)0, ImGuiColors.DPSRed);
            ImGuiHelpers.SafeTextWrapped(_noticeString);

            if(_noticeUrl != null) {
                if(ImGui.Button(_noticeUrl)) {
                    try {
                        Process.Start(new ProcessStartInfo {
                            FileName = _noticeUrl,
                            UseShellExecute = true
                        });
                    } catch(Exception ex) {
                    }
                }
            }

            ImGui.PopStyleColor();
        }

        public void Dispose() {
        }

        public void Draw() {
            if(!SettingsVisible) {
                return;
            }

            DrawSettingsWindow();
        }

        public void DrawSettingsWindow() {
            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);

            if(ImGui.Begin("Fader Plugin Configuration", ref settingsVisible)) {
                DisplayNotice();

                ImGui.Text("User Focus key:");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(195);
                if(ImGui.BeginCombo("", CurrentOverrideKey.ToString())) {
                    foreach(var option in Enum.GetValues(typeof(OverrideKeys))) {
                        if(ImGui.Selectable(option.ToString(), option.Equals(CurrentOverrideKey))) {
                            config.OverrideKey = (int)option;
                            config.Save();
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGuiHelpTooltip("When held interface will be setup as per 'UserFocus' column.");

                var focusOnHotbarsUnlock = config.FocusOnHotbarsUnlock;
                if(ImGui.Checkbox("##focus_on_unlocked_bars", ref focusOnHotbarsUnlock)) {
                    config.FocusOnHotbarsUnlock = focusOnHotbarsUnlock;
                    config.Save();
                }

                ImGui.SameLine();
                ImGui.Text("Always User Focus when hotbars are unlocked");
                ImGuiHelpTooltip("When hotbars or crossbars are unlocked always setup to the UserFocus column.");

                var idleDelay = (float)TimeSpan.FromMilliseconds(config.DefaultDelay).TotalSeconds;
                ImGui.Text("Default delay:");
                ImGui.SameLine();
                bool defaultDelayEnabled = config.DefaultDelayEnabled();
                if(ImGui.Checkbox("##default_delay_enabled", ref defaultDelayEnabled)) {
                    config.DefaultDelay = config.DefaultDelayEnabled() ? 0 : 2000;
                    config.Save();
                }
                if(defaultDelayEnabled) {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(170);
                    if(ImGui.SliderFloat("##default_delay", ref idleDelay, 0.1f, 15f, "%.1f seconds")) {
                        config.DefaultDelay = (long)TimeSpan.FromSeconds(Math.Round(idleDelay, 1)).TotalMilliseconds;
                        config.Save();
                    }
                }

                ImGuiHelpTooltip("Amount of time it takes to go back to the `Idle` column.");

                ImGui.Text("Chat activity timeout:");
                ImGui.SameLine();
                var chatActivityTimeout = (int)TimeSpan.FromMilliseconds(config.ChatActivityTimeout).TotalSeconds;
                ImGui.SetNextItemWidth(170);
                if(ImGui.SliderInt("##chat_activity_timeout", ref chatActivityTimeout, 1, 20, "%d seconds")) {
                    config.ChatActivityTimeout = (long)TimeSpan.FromSeconds(chatActivityTimeout).TotalMilliseconds;
                    config.Save();
                }

                ImGui.Text("Hint: you can select multiple elements to be edited at the same time. Configuration of the element that was selected first will override the rest.");

                ImGui.Separator();

                ImGui.Columns(2, "columns", false);

                var buttonWidth = ImGui.CalcTextSize("Status Enfeeblements   ?").X + 10;

                ImGui.SetColumnWidth(0, buttonWidth + 40);

                ImGui.BeginChild("##elementsList");
                foreach(Element element in Enum.GetValues(typeof(Element))) {
                    if(ShouldIgnoreElement(element)) {
                        continue;
                    }

                    var buttonText = ElementUtil.GetElementName(element);
                    string tooltipText = TooltipForElement(element);
                    if(tooltipText != null) {
                        buttonText += "   ?";
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));

                    Vector4? desiredButtonColor = null;

                    if(selectedElements.Contains(element)) {
                        desiredButtonColor = ImGuiColors.HealerGreen;
                    }

                    if(desiredButtonColor.HasValue) {
                        ImGui.PushStyleColor(ImGuiCol.Button, desiredButtonColor.Value);
                    }

                    if(ImGui.Button(buttonText, new Vector2(buttonWidth, 0))) {
                        if(!ImGui.IsKeyDown(ImGuiKey.ModCtrl)) {
                            selectedElements.Clear();
                        }

                        if(!selectedElements.Any()) {
                            selectedConfig = config.GetElementConfig(element);
                        }

                        if(!selectedElements.Contains(element)) {
                            selectedElements.Add(element);
                        } else {
                            selectedElements.Remove(element);
                        }
                    }

                    if(tooltipText != null) {
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(tooltipText);
                        }
                    }

                    ImGui.PopStyleVar();

                    if(desiredButtonColor.HasValue) {
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndChild();

                ImGui.NextColumn();

                // Config for the selected elements.
                if(selectedElements.Any()) {
                    string elementName = ElementUtil.GetElementName(selectedElements.First());
                    if(selectedElements.Count > 1) {
                        elementName += " & others";
                    }

                    ImGui.Text($"{elementName} Configuration");


                    if(selectedElements.Count > 1) {
                        if(ImGui.Button($"Sync selected to {selectedElements.First()}")) {
                            SaveSelectedElementsConfig();
                        }
                    }

                    // Config for each condition.
                    for(int i = 0; i < selectedConfig.Count; i++) {
                        var elementState = selectedConfig[i].state;
                        var elementSetting = selectedConfig[i].setting;

                        // State
                        ImGui.SetNextItemWidth(200);
                        if(elementState == State.Default) {
                            ImGui.Text(StateUtil.GetStateName(elementState));
                            ImGui.SameLine();
                            // Because item width doesn't work on text. Remove when reimplemented with tables.
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 159);
                        } else {
                            if(ImGui.BeginCombo($"##{elementName}-{i}-state", StateUtil.GetStateName(elementState))) {
                                foreach(State state in StateUtil.orderedStates) {
                                    if(state == State.None || state == State.Default) {
                                        continue;
                                    }
                                    if(ImGui.Selectable(StateUtil.GetStateName(state))) {
                                        selectedConfig[i].state = state;
                                        SaveSelectedElementsConfig();
                                    }
                                }
                                ImGui.EndCombo();
                            }
                            ImGui.SameLine();
                        }

                        // Setting
                        ImGui.SetNextItemWidth(200);
                        if(ImGui.BeginCombo($"##{elementName}-{i}-setting", elementSetting.ToString())) {
                            foreach(Setting setting in Enum.GetValues(typeof(Setting))) {
                                if(setting == Setting.Unknown) {
                                    continue;
                                }

                                if(ImGui.Selectable(setting.ToString())) {
                                    selectedConfig[i].setting = setting;
                                    SaveSelectedElementsConfig();
                                }
                            }
                            ImGui.EndCombo();
                        }

                        if(elementState == State.Default) {
                            continue;
                        }

                        // Up
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        if(ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}##{elementName}-{i}-up")) {
                            if(i > 0) {
                                var swap1 = selectedConfig[i - 1];
                                var swap2 = selectedConfig[i];

                                if(swap1.state != State.Default && swap2.state != State.Default) {
                                    selectedConfig[i] = swap1;
                                    selectedConfig[i - 1] = swap2;

                                    SaveSelectedElementsConfig();
                                }
                            }
                        }

                        // Down
                        ImGui.SameLine();
                        if(ImGui.Button($"{FontAwesomeIcon.ArrowDown.ToIconString()}##{elementName}-{i}-down")) {
                            if(i < selectedConfig.Count - 1) {
                                var swap1 = selectedConfig[i + 1];
                                var swap2 = selectedConfig[i];

                                if(swap1.state != State.Default && swap2.state != State.Default) {
                                    selectedConfig[i] = swap1;
                                    selectedConfig[i + 1] = swap2;

                                    SaveSelectedElementsConfig();
                                }
                            }
                        }

                        // Delete
                        ImGui.SameLine();
                        if(ImGui.Button($"{FontAwesomeIcon.TrashAlt.ToIconString()}##{elementName}-{i}-delete")) {
                            selectedConfig.RemoveAt(i);

                            SaveSelectedElementsConfig();
                        }
                        ImGui.PopFont();
                    }

                    ImGui.PushFont(UiBuilder.IconFont);
                    if(ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##{elementName}-add")) {
                        // Add the new state then swap it with the existing default state.
                        selectedConfig.Add(new(State.None, Setting.Hide));
                        var swap1 = selectedConfig[^1];
                        var swap2 = selectedConfig[^2];

                        selectedConfig[^2] = swap1;
                        selectedConfig[^1] = swap2;

                        SaveSelectedElementsConfig();
                    }
                    ImGui.PopFont();
                }

                _windowSize = ImGui.GetWindowSize();
                ImGui.End();
            }
        }

        private void SaveSelectedElementsConfig() {
            foreach(var element in selectedElements) {
                config.elementsConfig[element] = selectedConfig;
            }

            config.Save();
        }

        private void ImGuiHelpTooltip(string tooltip) {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "?");
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip(tooltip);
            }
        }

        private string TooltipForElement(Element elementId) {
            return elementId switch {
                Element.Chat =>
                    "Should be always visible if focused, albeit feature can be buggy with some configurations",
                Element.PetHotbar => "Pet and mount actions",
                Element.Job => "Job-specific UI",
                Element.Status => "Player status (when not split into 3 separate elements)",
                Element.StatusEnfeeblements => "Player enfeeblements (when split into 3 separate elements)",
                Element.StatusEnhancements => "Player enhancements (when split into 3 separate elements)",
                Element.StatusOther => "Player other status (when split into 3 separate elements)",
                _ => null,
            };
        }

        private bool ShouldIgnoreElement(Element elementId) {
            return elementId switch {
                Element.QuestLog => true,
                Element.Nameplates => true,
                Element.Unknown => true,
                _ => false,
            };
        }
    }
}