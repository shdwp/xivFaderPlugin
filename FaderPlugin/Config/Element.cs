namespace FaderPlugin.Config {
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
                Element.Hotbar1 => new[] { "_ActionBar" },
                Element.Hotbar2 => new[] { "_ActionBar01" },
                Element.Hotbar3 => new[] { "_ActionBar02" },
                Element.Hotbar4 => new[] { "_ActionBar03" },
                Element.Hotbar5 => new[] { "_ActionBar04" },
                Element.Hotbar6 => new[] { "_ActionBar05" },
                Element.Hotbar7 => new[] { "_ActionBar06" },
                Element.Hotbar8 => new[] { "_ActionBar07" },
                Element.Hotbar9 => new[] { "_ActionBar08" },
                Element.Hotbar10 => new[] { "_ActionBar09" },
                Element.CrossHotbar => new[] {
                    "_ActionCross",
                    "_ActionDoubleCrossL",
                    "_ActionDoubleCrossR"
                },
                Element.ContextActionHotbar => new [] {"_ActionContents"},
                Element.PetHotbar => new[] { "_ActionBarEx" },
                Element.Job => new[] {
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
                    "JobHudNIN0", "JobHudNIN1",
                    "JobHudSAM0", "JobHudSAM1",
                    "JobHudRRP0", "JobHudRRP1",
                    "JobHudBRD0",
                    "JobHudMCH0",
                    "JobHudDNC0", "JobHudDNC1",
                    "JobHudBLM0",
                    "JobHudSMN0", "JobHudSMN1",
                    "JobHudRDM0"
                },
                Element.PartyList => new[] { "_PartyList" },
                Element.LimitBreak => new[] { "_LimitBreak" },
                Element.Parameters => new[] { "_ParameterWidget" },
                Element.Status => new[] { "_Status" },
                Element.StatusEnhancements => new[] { "_StatusCustom0" },
                Element.StatusEnfeeblements => new[] { "_StatusCustom1" },
                Element.StatusOther => new[] { "_StatusCustom2" },
                Element.CastBar => new[] { "_CastBar" },
                Element.ExperienceBar => new[] { "_Exp" },
                Element.ScenarioGuide => new[] { "ScenarioTree" },
                Element.InventoryGrid => new[] { "_BagWidget" },
                Element.DutyList => new[] { "_ToDoList" },
                Element.ServerInfo => new[] { "_DTR" },
                Element.IslekeepIndex => new [] { "MJIHud" },
                Element.MainMenu => new[] { "_MainCommand" },
                Element.Chat => new[]
                {
                    "ChatLog",
                    "ChatLogPanel_0",
                    "ChatLogPanel_1",
                    "ChatLogPanel_2",
                    "ChatLogPanel_3"
                },
                Element.Minimap => new[] { "_NaviMap" },
                Element.Currency => new[] { "_Money" },
                Element.TargetInfo => new[]
                {
                    "_TargetInfoMainTarget",
                    "_TargetInfoBuffDebuff",
                    "_TargetInfoCastBar",
                    "_TargetInfo"
                },
                Element.Unknown => new string[] { },
                _ => System.Array.Empty<string>(),
            };
        }
    }
}