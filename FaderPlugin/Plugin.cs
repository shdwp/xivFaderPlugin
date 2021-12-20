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

        private Configuration       configuration;
        private PluginUI            ui;
        private AtkApi.AtkAddonsApi atkAddonsApi;

        public  string AssemblyLocation { get => assemblyLocation; set => assemblyLocation = value; }
        private string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

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

            this.atkAddonsApi = new AtkApi.AtkAddonsApi(GameGui);

            this.configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(PluginInterface);
            this.configuration.OnSaved += OnConfigurationSaved;

            this.pendingTimer = new Timer();
            this.pendingTimer.Interval = this.configuration.IdleTransitionDelay;

            this.maintanceTimer = new Timer();
            this.maintanceTimer.Interval = 250;
            this.maintanceTimer.AutoReset = true;
            this.maintanceTimer.Start();

            this.ui = new PluginUI(this.configuration);

            this.pendingTimer.Elapsed += TransitionToPendingState;
            this.maintanceTimer.Elapsed += UpdateAddonVisibilityBasedOnCurrentState;

            this.Framework.Update += OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += this.ui.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += () => this.ui.SettingsVisible = true;

            this.CommandManager.AddHandler(commandName, new CommandInfo(FaderCommandHandler)
            {
                HelpMessage = "Opens settings, /pfader t toggles whether it's enabled."
            });
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler(commandName);

            this.ui.Dispose();
            this.PluginInterface.Dispose();

            this.pendingTimer.Elapsed -= TransitionToPendingState;
            this.pendingTimer.Dispose();

            this.maintanceTimer.Elapsed -= UpdateAddonVisibilityBasedOnCurrentState;
            this.maintanceTimer.Dispose();

            this.Framework.Update -= OnFrameworkUpdate;
            this.atkAddonsApi.UpdateAddonVisibility(_ => true);
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
                this.atkAddonsApi.UpdateAddonVisibility((name) =>
                {
                    this.ChatGui.Print(name);
                    return null;
                });
            }
            else if (arguments == "")
            {
                this.ui.SettingsVisible = true;
            }
        }

        private void OnConfigurationSaved()
        {
            this.pendingTimer.Interval = this.configuration.IdleTransitionDelay;
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (this.Condition[ConditionFlag.BetweenAreas] || !this.ClientState.IsLoggedIn)
            {
                // client weirds out if you mess with addons during loading
                return;
            }

            if (this.KeyState[this.configuration.OverrideKey])
            {
                ScheduleTransition(FaderState.UserFocus);
            }
            else if (this.configuration.FocusOnHotbarsUnlock && !this.atkAddonsApi.AreHotbarsLocked())
            {
                ScheduleTransition(FaderState.UserFocus);
            }
            else if (this.Condition[ConditionFlag.BoundByDuty])
            {
                ScheduleTransition(FaderState.Duty);
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

            this.atkAddonsApi.UpdateAddonVisibility(addonName =>
            {
                if (!enabled)
                {
                    return true;
                }

                var element = this.configuration.ConfigElementByName(addonName);
                if (element == ConfigElementId.Chat && this.atkAddonsApi.CheckIfFocused("ChatLog"))
                {
                    return true;
                }

                var value = this.configuration.GetSetting(element, this.currentState);
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