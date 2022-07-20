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
                case State.ChatFocus:
                    return "Chat Focus";
                case State.UserFocus:
                    return "User Focus";
                default:
                    return state.ToString();
            }
        }
    }


}