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
        ChatFocus,
        UserFocus,
        ChatActivity,
    }

    public static class StateUtil {
        public static string GetStateName(State state) {
            switch(state) {
                case State.EnemyTarget:
                    return "Enemy Target";
                case State.PlayerTarget:
                    return "Player Target";
                case State.NPCTarget:
                    return "NPC Target";
                case State.ChatActivity:
                    return "Chat Activity";
                case State.ChatFocus:
                    return "Chat Focus";
                case State.UserFocus:
                    return "User Focus";
                default:
                    return state.ToString();
            }
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
            State.ChatActivity,
            State.ChatFocus,
            State.UserFocus,
        };
    }


}