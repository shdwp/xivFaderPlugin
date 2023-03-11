using System;
using System.Collections.Generic;
using System.Linq;
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
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FaderPlugin.Config;
using FaderPlugin.Windows;

namespace FaderPlugin {
    public class Plugin : IDalamudPlugin {
        public string Name => "Fader Plugin";

        private readonly Config.Config config;
        private readonly ConfigurationWindow configurationWindow;
        private readonly WindowSystem windowSystem = new("Fader");

        private readonly Dictionary<State, bool> stateMap = new();
        private bool stateChanged;

        // Idle State
        private readonly Timer idleTimer = new();
        private bool hasIdled;

        // Chat State
        private readonly Timer chatActivityTimer = new();
        private bool hasChatActivity;

        // Commands
        private readonly string commandName = "/pfader";
        private bool enabled = true;

        [PluginService] public static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] public static KeyState KeyState { get; set; } = null!;
        [PluginService] public static Framework Framework { get; set; } = null!;
        [PluginService] public static ClientState ClientState { get; set; } = null!;
        [PluginService] public static Condition Condition { get; set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; set; } = null!;
        [PluginService] public static ChatGui ChatGui { get; set; } = null!;
        [PluginService] public static GameGui GameGui { get; set; } = null!;
        [PluginService] public static TargetManager TargetManager { get; set; } = null!;

        public Plugin() {;
            LoadConfig(out config);
            config.OnSave += UpdateAddonVisibility;

            configurationWindow = new ConfigurationWindow(config);
            windowSystem.AddWindow(configurationWindow);

            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            CommandManager.AddHandler(commandName, new CommandInfo(FaderCommandHandler) {
                HelpMessage = "Opens settings, /pfader t toggles whether it's enabled."
            });

            foreach(State state in Enum.GetValues(typeof(State))) {
                stateMap[state] = state == State.Default;
            }

            idleTimer.Elapsed += (_, _) => {
                hasIdled = true;
            };

            chatActivityTimer.Elapsed += (_, _) => {
                hasChatActivity = false;
            };

            ChatGui.ChatMessage += OnChatMessage;
        }

        private void LoadConfig(out Config.Config configuration) {
            var existingConfig = PluginInterface.GetPluginConfig();

            if(existingConfig is { Version: 6 }) {
                configuration = (Config.Config) existingConfig;
            } else {
                configuration = new Config.Config();
            }

            configuration.Initialize();
        }

        public void Dispose() {
            config.OnSave -= UpdateAddonVisibility;
            Framework.Update -= OnFrameworkUpdate;
            CommandManager.RemoveHandler(commandName);
            ChatGui.ChatMessage -= OnChatMessage;
            UpdateAddonVisibility(true);

            idleTimer.Dispose();
            chatActivityTimer.Dispose();

            configurationWindow.Dispose();
            windowSystem.RemoveWindow(configurationWindow);
        }

        private void DrawUI()
        {
            windowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            configurationWindow.IsOpen = true;
        }

        private void FaderCommandHandler(string s, string arguments) {
            arguments = arguments.Trim();
            if(arguments is "t" or "toggle") {
                enabled = !enabled;
                ChatGui.Print($"Fader plugin {(enabled ? "enabled" : "disabled")}.");
            } else if(arguments == "")
            {
                configurationWindow.IsOpen = true;
            }
        }
        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            if(!Constants.ActiveChatTypes.Contains(type)) {
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

            // Weapon Unsheathed
            UpdateStateMap(State.WeaponUnsheathed, Addon.IsWeaponUnsheathed());

            var target = TargetManager.Target;

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

                if(setting == Setting.Unknown)
                {
                    List<ConfigEntry> elementConfig = config.GetElementConfig(element);

                    foreach (ConfigEntry entry in elementConfig.Where(entry => stateMap[entry.state]))
                    {
                        // If the state is default and idle is enabled, only set the setting if the state has idled.
                        if(entry.state != State.Default || !config.DefaultDelayEnabled() || hasIdled) {
                            setting = entry.setting;
                        }
                        break;
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
        /// Returns whether it is safe for the plugin to perform work,
        /// dependent on whether the game is on a login or loading screen.
        /// </summary>
        private bool IsSafeToWork() {
            return !Condition[ConditionFlag.BetweenAreas] && ClientState.IsLoggedIn;
        }
    }
}