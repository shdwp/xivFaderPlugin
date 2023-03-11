using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FaderPlugin.Config;
using ImGuiNET;

using static FaderPlugin.Constants;

namespace FaderPlugin.Windows;

public class ConfigurationWindow : Window, IDisposable
{
    private readonly Config.Config config;
    private List<ConfigEntry> selectedConfig = new();
    private readonly List<Element> selectedElements = new();

    private OverrideKeys CurrentOverrideKey => (OverrideKeys) config.OverrideKey;

    private HttpClient httpClient = new();
    private string noticeString = string.Empty;
    private string noticeUrl = string.Empty;

    public ConfigurationWindow(Config.Config config) : base("Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(730, 670),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.config = config;

        DownloadAndParseNotice();
    }

    public void Dispose() { }

    public override void Draw()
    {
        if(ImGui.CollapsingHeader("Notice", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DisplayNotice();
        }

        ImGuiHelpers.ScaledDummy(5, 0);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5, 0);

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
                config.DefaultDelay = (int) TimeSpan.FromSeconds(Math.Round(idleDelay, 1)).TotalMilliseconds;
                config.Save();
            }
        }

        ImGuiHelpTooltip("Amount of time it takes to go back to the `Idle` column.");

        ImGui.Text("Chat activity timeout:");
        ImGui.SameLine();
        var chatActivityTimeout = (int) TimeSpan.FromMilliseconds(config.ChatActivityTimeout).TotalSeconds;
        ImGui.SetNextItemWidth(170);
        if(ImGui.SliderInt("##chat_activity_timeout", ref chatActivityTimeout, 1, 20, "%d seconds")) {
            config.ChatActivityTimeout = (int) TimeSpan.FromSeconds(chatActivityTimeout).TotalMilliseconds;
            config.Save();
        }

        ImGuiHelpers.SafeTextWrapped("Hint: you can select multiple elements to be edited at the same time. Configuration of the element that was selected first will override the rest.");

        ImGuiHelpers.ScaledDummy(5, 0);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5, 0);

        ImGui.Columns(2, "columns", false);

        var buttonWidth = ImGui.CalcTextSize("Status Enfeeblements   ?").X + 10;

        ImGui.SetColumnWidth(0, buttonWidth + 40);

        ImGui.BeginChild("##elementsList");
        foreach(Element element in Enum.GetValues(typeof(Element))) {
            if(ShouldIgnoreElement(element)) {
                continue;
            }

            string buttonText = ElementUtil.GetElementName(element);
            string tooltipText = TooltipForElement(element);
            if(tooltipText != string.Empty) {
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

            if(tooltipText != string.Empty) {
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
                            if(state is State.None or State.Default) {
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
            Element.Chat => "Should be always visible if focused, albeit feature can be buggy with some configurations",
            Element.PetHotbar => "Pet and mount actions",
            Element.Job => "Job-specific UI",
            Element.Status => "Player status (when not split into 3 separate elements)",
            Element.StatusEnfeeblements => "Player enfeeblements (when split into 3 separate elements)",
            Element.StatusEnhancements => "Player enhancements (when split into 3 separate elements)",
            Element.StatusOther => "Player other status (when split into 3 separate elements)",
            _ => string.Empty,
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

    private void DownloadAndParseNotice() {
        try {
            var stringAsync = httpClient.GetStringAsync("https://shdwp.github.io/ukraine/xiv_notice.txt");
            stringAsync.Wait();
            var strArray = stringAsync.Result.Split('|');

            if((uint)strArray.Length > 0U) {
                noticeString = strArray[0];
            }

            if(strArray.Length <= 1) {
                return;
            }

            noticeUrl = strArray[1];

            if(!(noticeUrl.StartsWith("http://") || noticeUrl.StartsWith("https://"))) {
                PluginLog.Warning($"Received invalid noticeUrl {noticeUrl}, ignoring");
                noticeUrl = string.Empty;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void DisplayNotice() {
        if(noticeString == string.Empty) {
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DPSRed);
        ImGuiHelpers.SafeTextWrapped(noticeString);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGold);
        if(noticeUrl != string.Empty) {
            if(ImGui.Button(noticeUrl)) {
                try {
                    Dalamud.Utility.Util.OpenLink(noticeUrl);
                } catch {
                    // ignored
                }
            }
        }
        ImGui.PopStyleColor();
    }
}