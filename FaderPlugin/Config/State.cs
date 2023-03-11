using System.Collections.Generic;

namespace FaderPlugin.Config {
    public enum State {
        None,
        Default,
        Duty,
        EnemyTarget,
        PlayerTarget,
        NPCTarget,
        Crafting,
        Gathering,
        Mounted,
        Combat,
        WeaponUnsheathed,
        ChatFocus,
        UserFocus,
        ChatActivity,
    }

    public static class StateUtil {
        public static string GetStateName(State state)
        {
            return state switch
            {
                State.EnemyTarget => "Enemy Target",
                State.PlayerTarget => "Player Target",
                State.NPCTarget => "NPC Target",
                State.WeaponUnsheathed => "Weapon Unsheathed",
                State.ChatActivity => "Chat Activity",
                State.ChatFocus => "Chat Focus",
                State.UserFocus => "User Focus",
                _ => state.ToString()
            };
        }

        public static readonly List<State> orderedStates = new() {
            State.None,
            State.Default,
            State.Duty,
            State.EnemyTarget,
            State.PlayerTarget,
            State.NPCTarget,
            State.Crafting,
            State.Gathering,
            State.Mounted,
            State.Combat,
            State.WeaponUnsheathed,
            State.ChatActivity,
            State.ChatFocus,
            State.UserFocus,
        };
    }


}