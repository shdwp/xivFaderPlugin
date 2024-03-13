using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FaderPlugin.Config;
using FaderPlugin.Windows;
using Lumina.Excel.GeneratedSheets;

namespace FaderPlugin {
    public class Plugin : IDalamudPlugin {
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
        [PluginService] public static IKeyState KeyState { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static ITargetManager TargetManager { get; set; } = null!;
        [PluginService] public static IDataManager Data { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;

        public Plugin() {;
            LoadConfig(out config);
            config.OnSave += UpdateAddonVisibility;

            configurationWindow = new ConfigurationWindow(config);
            windowSystem.AddWindow(configurationWindow);

            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            CommandManager.AddHandler(commandName, new CommandInfo(FaderCommandHandler) {
                HelpMessage = "Opens settings\n't' toggles whether it's enabled.\n'on' enables the plugin\n'off' disables the plugin."
            });

            foreach(State state in Enum.GetValues(typeof(State))) {
                stateMap[state] = state == State.Default;
            }

            // We don't want a looping timer, only once
            idleTimer.AutoReset = false;
            idleTimer.Elapsed += (_, _) => {
                hasIdled = true;
            };
            idleTimer.Start();

            chatActivityTimer.Elapsed += (_, _) => {
                hasChatActivity = false;
            };

            ChatGui.ChatMessage += OnChatMessage;

            if (config.DefaultDelay == 0) config.DefaultDelay = 2000; // recover from previous misconfiguration
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

        private void FaderCommandHandler(string s, string arguments)
        {
            switch (arguments.Trim())
            {
                case "t" or "toggle":
                    enabled = !enabled;
                    ChatGui.Print($"Fader plugin {(enabled ? "enabled" : "disabled")}.");
                    break;
                case "on":
                    enabled = true;
                    ChatGui.Print($"Fader plugin enabled.");
                    break;
                case "off":
                    enabled = false;
                    ChatGui.Print($"Fader plugin disabled.");
                    break;
                case "":
                    configurationWindow.IsOpen = true;
                    break;
            }
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            // Don't trigger chat for non-standard chat channels.
            if (!Constants.ActiveChatTypes.Contains(type)
                && (!config.ImportantActivity || !Constants.ImportantChatTypes.Contains(type))
                && (!config.EmoteActivity || !Constants.EmoteChatTypes.Contains(type)))
                return;

            hasChatActivity = true;
            chatActivityTimer.Stop();
            chatActivityTimer.Interval = config.ChatActivityTimeout;
            chatActivityTimer.Start();
        }

        private void OnFrameworkUpdate(IFramework framework) {
            if(!IsSafeToWork())
                return;

            stateChanged = false;

            // User Focus
            UpdateStateMap(State.UserFocus, KeyState[config.OverrideKey] || (config.FocusOnHotbarsUnlock && !Addon.AreHotbarsLocked()));

            // Key Focus
            UpdateStateMap(State.AltKeyFocus, KeyState[(int) Constants.OverrideKeys.Alt]);
            UpdateStateMap(State.CtrlKeyFocus, KeyState[(int) Constants.OverrideKeys.Ctrl]);
            UpdateStateMap(State.ShiftKeyFocus, KeyState[(int) Constants.OverrideKeys.Shift]);

            // Chat Focus
            UpdateStateMap(State.ChatFocus, Addon.IsChatFocused());

            // Chat Activity
            UpdateStateMap(State.ChatActivity, hasChatActivity);

            // Combat
            UpdateStateMap(State.Combat, Condition[ConditionFlag.InCombat]);

            // Weapon Unsheathed
            UpdateStateMap(State.WeaponUnsheathed, Addon.IsWeaponUnsheathed());

            // In Sanctuary (e.g Cities, Aetheryte Villages)
            UpdateStateMap(State.InSanctuary, Addon.InSanctuary());

            // Island Sanctuary
            var inIslandSanctuary = Data.GetExcelSheet<TerritoryType>()!.GetRow(ClientState.TerritoryType)!.TerritoryIntendedUse == 49;
            UpdateStateMap(State.IslandSanctuary, inIslandSanctuary);

            var target = TargetManager.Target;
            if (target != null)
            {
                // Enemy Target
                UpdateStateMap(State.EnemyTarget, target.ObjectKind == ObjectKind.BattleNpc);

                // Player Target
                UpdateStateMap(State.PlayerTarget, target.ObjectKind == ObjectKind.Player);

                // NPC Target
                UpdateStateMap(State.NPCTarget, target.ObjectKind == ObjectKind.EventNpc);
            }

            // Crafting
            UpdateStateMap(State.Crafting, Condition[ConditionFlag.Crafting]);

            // Gathering
            UpdateStateMap(State.Gathering, Condition[ConditionFlag.Gathering]);

            // Mounted
            UpdateStateMap(State.Mounted, Condition[ConditionFlag.Mounted] || Condition[ConditionFlag.Mounted2]);

            // Duty
            var boundByDuty = Condition[ConditionFlag.BoundByDuty] || Condition[ConditionFlag.BoundByDuty56] || Condition[ConditionFlag.BoundByDuty95];
            UpdateStateMap(State.Duty, !inIslandSanctuary && boundByDuty);

            // Only update display state if a state has changed.
            if(stateChanged || hasIdled || Addon.HasAddonStateChanged("HudLayout")) {
                UpdateAddonVisibility();

                // Always set Idled to false to prevent looping
                hasIdled = false;

                // Only start idle timer if there was a state change
                if(stateChanged && config.DefaultDelayEnabled) {
                    // If idle transition is enabled reset the idle state and start the timer.
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
            if(!IsSafeToWork())
                return;

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

                    if (elementConfig.FirstOrDefault(entry => stateMap[entry.state]) is { } configEntry &&
                        (configEntry.state != State.Default || !config.DefaultDelayEnabled || hasIdled))
                    {
                        setting = configEntry.setting;
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