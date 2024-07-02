using System.Collections.Generic;

namespace FaderPlugin.Config;

// Do not change the values of existing states, it will break configs
public enum State {
    None = 0,
    Default = 1,
    Duty = 2,
    EnemyTarget = 3,
    PlayerTarget = 4,
    NPCTarget = 5,
    Crafting = 6,
    Gathering = 7,
    Mounted = 8,
    Combat = 9,
    WeaponUnsheathed = 10,
    IslandSanctuary = 11,
    ChatFocus = 12,
    UserFocus = 13,
    ChatActivity = 14,
    AltKeyFocus = 15,
    CtrlKeyFocus = 16,
    ShiftKeyFocus = 17,
    InSanctuary = 18,
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
            State.InSanctuary => "In Sanctuary",
            State.IslandSanctuary => "Island Sanctuary",
            State.ChatActivity => "Chat Activity",
            State.ChatFocus => "Chat Focus",
            State.UserFocus => "User Focus",
            State.AltKeyFocus => "ALT Key Focus",
            State.CtrlKeyFocus => "CTRL Key Focus",
            State.ShiftKeyFocus => "SHIFT Key Focus",
            _ => state.ToString()
        };
    }

    public static readonly List<State> orderedStates =
    [
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
        State.InSanctuary,
        State.IslandSanctuary,
        State.ChatActivity,
        State.ChatFocus,
        State.UserFocus,
        State.AltKeyFocus,
        State.CtrlKeyFocus,
        State.ShiftKeyFocus
    ];
}