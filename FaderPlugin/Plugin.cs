using System.Timers;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using Dalamud.IoC;
using Dalamud.Logging;
using FaderPlugin.Config;
using FFXIVClientStructs;

namespace FaderPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Fader Plugin";

        private readonly Configuration       _configuration;
        private readonly PluginUI            _ui;
        private readonly AtkApi.AtkAddonsApi _atkAddonsApi;

        private FaderState currentState = FaderState.None;
        private FaderState pendingState = FaderState.None;
        private Timer      pendingTimer;
        private Timer      maintanceTimer;

        private string commandName = "/pfader";
        private bool   enabled     = true;

        [PluginService] private DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] private KeyState               KeyState        { get; set; }
        [PluginService] private Framework              Framework       { get; set; }
        [PluginService] private ClientState            ClientState     { get; set; }
        [PluginService] private Condition              Condition       { get; set; }
        [PluginService] private CommandManager         CommandManager  { get; set; }
        [PluginService] private ChatGui                ChatGui         { get; set; }
        [PluginService] private GameGui                GameGui         { get; set; }

        public Plugin()
        {
            Resolver.Initialize();

            this._atkAddonsApi = new AtkApi.AtkAddonsApi(GameGui);

            this._configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this._configuration.Initialize(PluginInterface);
            this._configuration.OnSaved += OnConfigurationSaved;

            this.pendingTimer = new Timer();
            this.pendingTimer.Interval = this._configuration.IdleTransitionDelay;

            this.maintanceTimer = new Timer();
            this.maintanceTimer.Interval = 1000;
            this.maintanceTimer.AutoReset = true;
            this.maintanceTimer.Start();

            this._ui = new PluginUI(this._configuration);

            this.pendingTimer.Elapsed += TransitionToPendingState;
            this.maintanceTimer.Elapsed += UpdateAddonVisibilityBasedOnCurrentState;

            this.Framework.Update += OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += this._ui.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += () => this._ui.SettingsVisible = true;

            this.CommandManager.AddHandler(commandName, new CommandInfo(FaderCommandHandler)
            {
                HelpMessage = "Opens settings, /pfader t toggles whether it's enabled."
            });
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(commandName);

            this._ui.Dispose();
            this.PluginInterface.Dispose();

            this.pendingTimer.Elapsed -= TransitionToPendingState;
            this.pendingTimer.Dispose();

            this.maintanceTimer.Elapsed -= UpdateAddonVisibilityBasedOnCurrentState;
            this.maintanceTimer.Dispose();

            this.Framework.Update -= OnFrameworkUpdate;
            this._atkAddonsApi.UpdateAddonVisibility(_ => true);
        }

        private void FaderCommandHandler(string s, string arguments)
        {
            arguments = arguments.Trim();
            if (arguments == "t" || arguments == "toggle")
            {
                enabled = !enabled;
                var state = enabled ? "enabled" : "disabled";
                this.ChatGui.Print($"Fader plugin {state}.");
            }
            else if (arguments == "dbg")
            {
                this._atkAddonsApi.UpdateAddonVisibility((name) =>
                {
                    this.ChatGui.Print(name);
                    return null;
                });
            }
            else if (arguments == "")
            {
                this._ui.SettingsVisible = true;
            }
        }

        private void OnConfigurationSaved()
        {
            this.pendingTimer.Interval = this._configuration.IdleTransitionDelay;
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (this.Condition[ConditionFlag.BetweenAreas] || !this.ClientState.IsLoggedIn)
            {
                // client weirds out if you mess with addons during loading
                return;
            }

            if (this.KeyState[this._configuration.OverrideKey])
            {
                ScheduleTransition(FaderState.UserFocus);
            }
            else if (this._configuration.FocusOnHotbarsUnlock && !this._atkAddonsApi.AreHotbarsLocked())
            {
                ScheduleTransition(FaderState.UserFocus);
            }
            else if (this._atkAddonsApi.IsChatFocused())
            {
                ScheduleTransition(FaderState.ChatFocus);
            }
            else if (this.Condition[ConditionFlag.InCombat])
            {
                ScheduleTransition(FaderState.Combat);
            }
            else if (this.ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.BattleNpc)
            {
                ScheduleTransition(FaderState.HasEnemyTarget);
            }
            else if (this.ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.Player)
            {
                ScheduleTransition(FaderState.HasPlayerTarget);
            }
            else if (this.ClientState.LocalPlayer?.TargetObject?.ObjectKind == ObjectKind.EventNpc)
            {
                ScheduleTransition(FaderState.HasNPCTarget);
            }
            else if (this.Condition[ConditionFlag.Gathering])
            {
                ScheduleTransition(FaderState.Gathering);
            }
            else if (this.Condition[ConditionFlag.Crafting])
            {
                ScheduleTransition(FaderState.Crafting);
            }
            else if (this.Condition[ConditionFlag.BoundByDuty])
            {
                ScheduleTransition(FaderState.Duty);
            }
            else
            {
                ScheduleTransition(FaderState.Idle);
            }
        }

        private void ScheduleTransition(FaderState state)
        {
            if (
                currentState != state
                &&
                (state == FaderState.Crafting
                 || state == FaderState.Gathering
                 || state == FaderState.Combat
                 || state == FaderState.HasEnemyTarget
                 || state == FaderState.HasPlayerTarget
                 || state == FaderState.HasNPCTarget
                 || state == FaderState.ChatFocus
                 || state == FaderState.UserFocus)
            )
            {
                PluginLog.Debug($"Immediate transition from {currentState} to {state}");

                pendingTimer.Stop();
                MoveToState(state);
            }
            else if (pendingState != state)
            {
                PluginLog.Debug($"Pending transition from {currentState} to {state}");

                pendingTimer.Stop();
                pendingState = state;
                pendingTimer.Start();
            }
        }

        private void TransitionToPendingState(object sender, ElapsedEventArgs e)
        {
            if (pendingState != currentState)
            {
                pendingTimer.Stop();
                MoveToState(pendingState);
            }
        }

        private void MoveToState(FaderState newState)
        {
            this.currentState = newState;

            UpdateAddonVisibilityBasedOnCurrentState();
        }

        private void UpdateAddonVisibilityBasedOnCurrentState(object sender, ElapsedEventArgs e)
        {
            UpdateAddonVisibilityBasedOnCurrentState();
        }

        private void UpdateAddonVisibilityBasedOnCurrentState()
        {
            if (this.Condition[ConditionFlag.BetweenAreas] || !this.ClientState.IsLoggedIn)
            {
                // client weirds out if you mess with addons during loading
                return;
            }

            this._atkAddonsApi.UpdateAddonVisibility(addonName =>
            {
                var element = this._configuration.ConfigElementByName(addonName);
                if (element == ConfigElementId.Unknown)
                {
                    return null;
                }

                if (!enabled)
                {
                    return true;
                }

                var value = this._configuration.GetSetting(element, this.currentState);
                return value switch
                {
                    ConfigElementSetting.Show => true,
                    ConfigElementSetting.Hide => false,
                    ConfigElementSetting.Skip => null,
                    _                         => null,
                };
            });
        }
    }
}