using System;
using System.Collections.Generic;
using System.Timers;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using FaderPlugin.Config;
using FFXIVClientStructs;

namespace FaderPlugin {
    public class Plugin : IDalamudPlugin {
        public string Name => "Fader Plugin";

        private readonly Config.Config config;
        private readonly PluginUI ui;

        private readonly Dictionary<State, bool> stateMap = new();
        private bool stateChanged = false;

        // Idle State
        private Timer idleTimer;
        private bool hasIdled = false;

        // Chat State
        private Timer chatActivityTimer;
        private bool hasChatActivity;
        private List<XivChatType> activeChatTypes = new() { XivChatType.Say, XivChatType.Party, XivChatType.Alliance, XivChatType.Yell, XivChatType.Shout, XivChatType.FreeCompany, XivChatType.TellIncoming, XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8 };

        private readonly string commandName = "/pfader";
        private bool enabled = true;

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] public static KeyState KeyState { get; set; }
        [PluginService] public static Framework Framework { get; set; }
        [PluginService] public static ClientState ClientState { get; set; }
        [PluginService] public static Condition Condition { get; set; }
        [PluginService] public static CommandManager CommandManager { get; set; }
        [PluginService] public static ChatGui ChatGui { get; set; }
        [PluginService] public static GameGui GameGui { get; set; }
        [PluginService] public static TargetManager TargetManager { get; set; }

        public Plugin() {;
            LoadConfig(out config);
            config.OnSave += UpdateAddonVisibility;

            ui = new PluginUI(config);

            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () => ui.SettingsVisible = true;

            CommandManager.AddHandler(commandName, new CommandInfo(FaderCommandHandler) {
                HelpMessage = "Opens settings, /pfader t toggles whether it's enabled."
            });

            foreach(State state in Enum.GetValues(typeof(State))) {
                stateMap[state] = state == State.Default;
            }

            idleTimer = new();
            idleTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
                hasIdled = true;
            };

            chatActivityTimer = new();
            chatActivityTimer.Elapsed += (object sender, ElapsedEventArgs e) => {
                hasChatActivity = false;
            };

            ChatGui.ChatMessage += OnChatMessage;
        }

        private void LoadConfig(out Config.Config config) {
            var existingConfig = PluginInterface.GetPluginConfig();

            if(existingConfig?.Version == 6) {
                config = existingConfig as Config.Config;
            } else {
                config = new();
            }

            config.Initialize();
        }

        public void Dispose() {
            config.OnSave -= UpdateAddonVisibility;
            ui.Dispose();
            Framework.Update -= OnFrameworkUpdate;
            CommandManager.RemoveHandler(commandName);
            ChatGui.ChatMessage -= OnChatMessage;
            UpdateAddonVisibility(true);
            idleTimer.Dispose();
            chatActivityTimer.Dispose();
        }

        private void FaderCommandHandler(string s, string arguments) {
            arguments = arguments.Trim();
            if(arguments == "t" || arguments == "toggle") {
                enabled = !enabled;
                var state = enabled ? "enabled" : "disabled";
                ChatGui.Print($"Fader plugin {state}.");
            } else if(arguments == "") {
                ui.SettingsVisible = true;
            }
        }
        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            if(!activeChatTypes.Contains(type)) {
                // Don't trigger chat for non-standard chat channels.
                return;
            }

            hasChatActivity = true;
            chatActivityTimer.Stop();
            chatActivityTimer.Interval = config.ChatActivityTimeout;
            chatActivityTimer.Start();
        }

        private void OnFrameworkUpdate(Framework framework) {
            if(!IsSafeToWork()) {
                return;
            }

            stateChanged = false;

            // User Focus
            UpdateStateMap(State.UserFocus, KeyState[config.OverrideKey] || (config.FocusOnHotbarsUnlock && !Addon.AreHotbarsLocked()));

            // Chat Focus
            UpdateStateMap(State.ChatFocus, Addon.IsChatFocused());

            // Chat Activity
            UpdateStateMap(State.ChatActivity, hasChatActivity);

            // Combat
            UpdateStateMap(State.Combat, Condition[ConditionFlag.InCombat]);

            var target = TargetManager?.Target;

            // Enemy Target
            UpdateStateMap(State.EnemyTarget, target?.ObjectKind == ObjectKind.BattleNpc);

            // Player Target
            UpdateStateMap(State.PlayerTarget, target?.ObjectKind == ObjectKind.Player);

            // NPC Target
            UpdateStateMap(State.NPCTarget, target?.ObjectKind == ObjectKind.EventNpc);

            // Crafting 
            UpdateStateMap(State.Crafting, Condition[ConditionFlag.Crafting]);

            // Gathering 
            UpdateStateMap(State.Gathering, Condition[ConditionFlag.Gathering]);

            // Mounted 
            UpdateStateMap(State.Mounted, Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2]);

            // Duty
            UpdateStateMap(State.Duty, Condition[ConditionFlag.BoundByDuty]);

            // Only update display state if a state has changed.
            if(stateChanged || hasIdled || Addon.HasAddonStateChanged("HudLayout")) {
                UpdateAddonVisibility();

                if(config.DefaultDelayEnabled()) {
                    // If idle transition is enabled reset the idle state and start the timer.
                    hasIdled = false;
                    idleTimer.Stop();
                    idleTimer.Interval = config.DefaultDelay;
                    idleTimer.Start();
                }
            }
        }

        private void UpdateStateMap(State state, bool value) {
            if(stateMap[state] != value) {
                stateMap[state] = value;
                stateChanged = true;
            }
        }

        private void UpdateAddonVisibility() {
            UpdateAddonVisibility(false);
        }

        private void UpdateAddonVisibility(bool forceShow) {
            if(!IsSafeToWork()) {
                return;
            }

            forceShow = !enabled || forceShow || Addon.IsHudManagerOpen();

            foreach(Element element in Enum.GetValues(typeof(Element))) {
                string[] addonNames = ElementUtil.GetAddonName(element);

                if(addonNames.Length == 0) {
                    continue;
                }

                Setting setting = Setting.Unknown;

                if(forceShow) {
                    setting = Setting.Show;
                }

                if(setting == Setting.Unknown) {
                    List<ConfigEntry> elementConfig = config.GetElementConfig(element);

                    foreach(ConfigEntry entry in elementConfig) {
                        if(stateMap[entry.state]) {
                            // If the state is default and idle is enabled, only set the setting if the state has idled.
                            if(entry.state != State.Default || !config.DefaultDelayEnabled() || hasIdled) {
                                setting = entry.setting;
                            }
                            break;
                        }
                    }
                }

                if(setting == Setting.Unknown) {
                    continue;
                }

                foreach(string addonName in addonNames) {
                    Addon.SetAddonVisibility(addonName, setting == Setting.Show);
                }
            }
        }

        /// <summary>
        /// Returns whether it is safe for the plugin to perform work, dependent on whether the game is on a login or loading screen.
        /// </summary>
        private bool IsSafeToWork() {
            return !Condition[ConditionFlag.BetweenAreas] && ClientState.IsLoggedIn;
        }
    }
}