namespace FaderPlugin.Config;

public enum Element {
    Unknown,

    Hotbar1,
    Hotbar2,
    Hotbar3,
    Hotbar4,
    Hotbar5,
    Hotbar6,
    Hotbar7,
    Hotbar8,
    Hotbar9,
    Hotbar10,
    CrossHotbar,
    PetHotbar,
    ContextActionHotbar,

    Job,
    CastBar,
    ExperienceBar,
    InventoryGrid,
    Currency,
    ScenarioGuide,
    QuestLog,
    DutyList,
    ServerInfo,
    IslekeepIndex,
    MainMenu,
    Chat,
    Minimap,
    Nameplates,

    TargetInfo,
    PartyList,
    LimitBreak,
    Parameters,
    Status,
    StatusEnhancements,
    StatusEnfeeblements,
    StatusOther,
}

public static class ElementUtil {
    public static string GetElementName(Element element)
    {
        return element switch
        {
            Element.Hotbar1 => "Hotbar 1",
            Element.Hotbar2 => "Hotbar 2",
            Element.Hotbar3 => "Hotbar 3",
            Element.Hotbar4 => "Hotbar 4",
            Element.Hotbar5 => "Hotbar 5",
            Element.Hotbar6 => "Hotbar 6",
            Element.Hotbar7 => "Hotbar 7",
            Element.Hotbar8 => "Hotbar 8",
            Element.Hotbar9 => "Hotbar 9",
            Element.Hotbar10 => "Hotbar 10",
            Element.CrossHotbar => "Cross Hotbar",
            Element.PetHotbar => "Pet Hotbar",
            Element.ContextActionHotbar => "Context Action Hotbar",
            Element.CastBar => "Cast Bar",
            Element.ExperienceBar => "Experience Bar",
            Element.InventoryGrid => "Inventory Grid",
            Element.ScenarioGuide => "Scenario Guide",
            Element.IslekeepIndex => "Islekeep's Index",
            Element.DutyList => "Duty List",
            Element.ServerInfo => "Server Information",
            Element.MainMenu => "Main Menu",
            Element.TargetInfo => "Target Info",
            Element.PartyList => "Party List",
            Element.LimitBreak => "Limit Break",
            Element.StatusEnhancements => "Status Enhancements",
            Element.StatusEnfeeblements => "Status Enfeeblements",
            Element.StatusOther => "Status Other",
            _ => element.ToString()
        };
    }

    public static string[] GetAddonName(Element element) {
        return element switch {
            Element.Hotbar1 => ["_ActionBar"],
            Element.Hotbar2 => ["_ActionBar01"],
            Element.Hotbar3 => ["_ActionBar02"],
            Element.Hotbar4 => ["_ActionBar03"],
            Element.Hotbar5 => ["_ActionBar04"],
            Element.Hotbar6 => ["_ActionBar05"],
            Element.Hotbar7 => ["_ActionBar06"],
            Element.Hotbar8 => ["_ActionBar07"],
            Element.Hotbar9 => ["_ActionBar08"],
            Element.Hotbar10 => ["_ActionBar09"],
            Element.CrossHotbar =>
            [
                "_ActionCross",
                "_ActionDoubleCrossL",
                "_ActionDoubleCrossR"
            ],
            Element.ContextActionHotbar => ["_ActionContents"],
            Element.PetHotbar => ["_ActionBarEx"],
            Element.Job =>
            [
                "JobHudPLD0",
                "JobHudWAR0",
                "JobHudDRK0", "JobHudDRK1",
                "JobHudGNB0",
                "JobHudWHM0",
                "JobHudACN0", "JobHudSCH0",
                "JobHudAST0",
                "JobHudGFF0", "JobHudGFF1",
                "JobHudMNK0", "JobHudMNK1",
                "JobHudDRG0",
                "JobHudNIN0", "JobHudNIN1v70",
                "JobHudSAM0", "JobHudSAM1",
                "JobHudRRP0", "JobHudRRP1",
                "JobHudBRD0",
                "JobHudMCH0",
                "JobHudDNC0", "JobHudDNC1",
                "JobHudBLM0", "JobHudBLM1",
                "JobHudSMN0", "JobHudSMN1",
                "JobHudRDM0",
                "JobHudRPM0", "JobHudRPM1",
                "JobHudRDB0", "JobHudRDB1",
            ],
            Element.PartyList => ["_PartyList"],
            Element.LimitBreak => ["_LimitBreak"],
            Element.Parameters => ["_ParameterWidget"],
            Element.Status => ["_Status"],
            Element.StatusEnhancements => ["_StatusCustom0"],
            Element.StatusEnfeeblements => ["_StatusCustom1"],
            Element.StatusOther => ["_StatusCustom2"],
            Element.CastBar => ["_CastBar"],
            Element.ExperienceBar => ["_Exp"],
            Element.ScenarioGuide => ["ScenarioTree"],
            Element.InventoryGrid => ["_BagWidget"],
            Element.DutyList => ["_ToDoList"],
            Element.ServerInfo => ["_DTR"],
            Element.IslekeepIndex => ["MJIHud"],
            Element.MainMenu => ["_MainCommand"],
            Element.Chat =>
            [
                "ChatLog",
                "ChatLogPanel_0",
                "ChatLogPanel_1",
                "ChatLogPanel_2",
                "ChatLogPanel_3"
            ],
            Element.Minimap => ["_NaviMap"],
            Element.Currency => ["_Money"],
            Element.TargetInfo =>
            [
                "_TargetInfoMainTarget",
                "_TargetInfoBuffDebuff",
                "_TargetInfoCastBar",
                "_TargetInfo"
            ],
            Element.Unknown => [],
            _ => [],
        };
    }
}