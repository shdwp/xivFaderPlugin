using System;
using System.Collections.Generic;
using System.Timers;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using FaderPlugin.Config;
using FFXIVClientStructs;

namespace FaderPlugin {
    public class Plugin : IDalamudPlugin {
        public string Name => "Fader Plugin";

        private readonly Configuration config;
        private readonly PluginUI ui;
        private readonly AtkApi.AtkAddonsApi atkAddonsApi;

        private readonly Dictionary<State, bool> stateMap = new();
        private bool stateChanged = false;
        private Timer idleTimer;
        private bool hasIdled = false;

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

        public Plugin() {
            Resolver.Initialize();

            atkAddonsApi = new AtkApi.AtkAddonsApi(GameGui);

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
        }

        private void LoadConfig(out Configuration config) {
            var existingConfig = PluginInterface.GetPluginConfig();

            if(existingConfig?.Version == 5) {
                config = existingConfig as Configuration;
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
            atkAddonsApi.UpdateAddonVisibility(_ => true);
            idleTimer.Dispose();
        }

        private void FaderCommandHandler(string s, string arguments) {
            arguments = arguments.Trim();
            if(arguments == "t" || arguments == "toggle") {
                enabled = !enabled;
                var state = enabled ? "enabled" : "disabled";
                ChatGui.Print($"Fader plugin {state}.");
            } else if(arguments == "dbg") {
                atkAddonsApi.UpdateAddonVisibility((name) => {
                    ChatGui.Print(name);
                    return null;
                });
            } else if(arguments == "") {
                ui.SettingsVisible = true;
            }
        }

        private void OnFrameworkUpdate(Framework framework) {
            if(!IsSafeToWork()) {
                return;
            }

            stateChanged = false;

            // User Focus
            UpdateStateMap(State.UserFocus, KeyState[config.OverrideKey] || (config.FocusOnHotbarsUnlock && !atkAddonsApi.AreHotbarsLocked()));

            // Chat Focus
            UpdateStateMap(State.ChatFocus, atkAddonsApi.IsChatFocused());

            // Combat
            UpdateStateMap(State.Combat, Condition[ConditionFlag.InCombat]);

            // Enemy Target
            UpdateStateMap(State.EnemyTarget, ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.BattleNpc);

            // Player Target
            UpdateStateMap(State.PlayerTarget, ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.Player);

            // NPC Target
            UpdateStateMap(State.NPCTarget, ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.EventNpc);

            // Gathering 
            UpdateStateMap(State.Gathering, Condition[ConditionFlag.Gathering]);

            // Gathering 
            UpdateStateMap(State.Crafting, Condition[ConditionFlag.Crafting]);

            // Duty
            UpdateStateMap(State.Duty, Condition[ConditionFlag.BoundByDuty]);

            // Only update display state if a state has changed.
            if(stateChanged || hasIdled) {
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
            if(!IsSafeToWork()) {
                return;
            }

            atkAddonsApi.UpdateAddonVisibility(addonName => {
                if(!enabled) {
                    return true;
                }

                Element element = config.ConfigElementByName(addonName);
                if(element == Element.Unknown) {
                    return null;
                }

                List<ConfigEntry> elementConfig = config.GetElementConfig(element);

                Setting setting = Setting.Unknown;

                foreach(ConfigEntry entry in elementConfig) {
                    if(stateMap[entry.state]) {
                        // If the state is default and idle is enabled, only set the setting if the state has idled.
                        if(entry.state != State.Default || !config.DefaultDelayEnabled() || hasIdled) {
                            setting = entry.setting;
                        }
                        break;
                    }
                }

                return setting switch {
                    Setting.Show => true,
                    Setting.Hide => false,
                    _ => null,
                };
            });
        }

        /// <summary>
        /// Returns whether it is safe for the plugin to perform work, dependent on whether the game is on a login or loading screen.
        /// </summary>
        private bool IsSafeToWork() {
            return !Condition[ConditionFlag.BetweenAreas] && ClientState.IsLoggedIn;
        }
    }
}