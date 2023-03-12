using System.Collections.Generic;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace FaderPlugin;

public static class Constants
{
    public static readonly List<XivChatType> ActiveChatTypes = new()
    {
        XivChatType.Say,
        XivChatType.Party,
        XivChatType.Alliance,
        XivChatType.Yell,
        XivChatType.Shout,
        XivChatType.FreeCompany,
        XivChatType.TellIncoming,
        XivChatType.Ls1,
        XivChatType.Ls2,
        XivChatType.Ls3,
        XivChatType.Ls4,
        XivChatType.Ls5,
        XivChatType.Ls6,
        XivChatType.Ls7,
        XivChatType.Ls8,
        XivChatType.CrossLinkShell1,
        XivChatType.CrossLinkShell2,
        XivChatType.CrossLinkShell3,
        XivChatType.CrossLinkShell4,
        XivChatType.CrossLinkShell5,
        XivChatType.CrossLinkShell6,
        XivChatType.CrossLinkShell7,
        XivChatType.CrossLinkShell8
    };

    public enum OverrideKeys {
        Alt = 0x12,
        Ctrl = 0x11,
        Shift = 0x10,
    }

    // TODO delete this in the next update
    public static SeString BuildMigrationMessage(string element)
    {
        return new SeStringBuilder()
            .AddUiForeground(570)
            .AddText("Fader Plugin: ")
            .AddUiForegroundOff()
            .AddUiForeground(34)
            .AddText(element)
            .AddUiForegroundOff()
            .AddText(" has been migrated after the previous update broke its config. ")
            .AddText("If in the last update the condition was intentionally set to be ")
            .AddUiForeground(34)
            .AddText("'IslandSanctuary' or 'WeaponUnsheathed'")
            .AddUiForegroundOff()
            .AddText(" so has this been reverted too, please change it back in your config.")
            .BuiltString;
    }
}