using System.Timers;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin;
using Dalamud.Interface;
using Dalamud.IoC;
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

        [PluginService] private DalamudPluginInterface PluginInterface { get; set; }
        [PluginService] private KeyState               KeyState        { get; set; }
        [PluginService] private Framework              Framework       { get; set; }
        [PluginService] private ClientState            ClientState     { get; set; }
        [PluginService] private Condition              Condition       { get; set; }


        public Plugin()
        {
            Resolver.Initialize();

            this.atkAddonsApi = new AtkApi.AtkAddonsApi();

            this.configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(PluginInterface);

            this.pendingTimer = new Timer();
            this.pendingTimer.Interval = this.configuration.IdleTransitionDelay;

            this.maintanceTimer = new Timer();
            this.maintanceTimer.Interval = 1000;
            this.maintanceTimer.AutoReset = true;

            this.ui = new PluginUI(this.configuration);

            this.pendingTimer.Elapsed += TransitionToPendingState;
            this.maintanceTimer.Elapsed += UpdateAddonVisibilityBasedOnCurrentState;

            Framework.Update += OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += this.ui.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += () => this.ui.SettingsVisible = true;
        }

        public void Dispose()
        {
            this.ui.Dispose();
            PluginInterface.Dispose();

            this.pendingTimer.Elapsed -= TransitionToPendingState;
            this.pendingTimer.Dispose();

            this.maintanceTimer.Elapsed -= UpdateAddonVisibilityBasedOnCurrentState;
            this.maintanceTimer.Dispose();

            Framework.Update -= OnFrameworkUpdate;
            this.atkAddonsApi.UpdateAddonVisibility(_ => true);
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (this.KeyState[this.configuration.OverrideKey])
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
                currentState == state
                || state == FaderState.Crafting
                || state == FaderState.Gathering
                || state == FaderState.Combat
                || state == FaderState.HasEnemyTarget
                || state == FaderState.HasPlayerTarget
                || state == FaderState.HasNPCTarget
                || state == FaderState.UserFocus
            )
            {
                pendingTimer.Stop();
                pendingState = FaderState.None;

                MoveToState(state);
            }
            else if (pendingState != state)
            {
                pendingTimer.Stop();
                pendingState = state;
                pendingTimer.Start();
            }
        }

        private void TransitionToPendingState(object sender, ElapsedEventArgs e)
        {
            pendingTimer.Stop();
            MoveToState(pendingState);
            pendingState = FaderState.None;
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
                return this.configuration.ShouldDisplayElement(addonName, this.currentState) switch
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
