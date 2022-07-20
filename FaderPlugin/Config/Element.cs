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

        Job,
        CastBar,
        ExperienceBar,
        InventoryGrid,
        Currency,
        ScenarioGuide,
        QuestLog,
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
        public static string GetElementName(Element element) {
            switch(element) {
                case Element.Hotbar1:
                    return "Hotbar 1";
                case Element.Hotbar2:
                    return "Hotbar 2";
                case Element.Hotbar3:
                    return "Hotbar 3";
                case Element.Hotbar4:
                    return "Hotbar 4";
                case Element.Hotbar5:
                    return "Hotbar 5";
                case Element.Hotbar6:
                    return "Hotbar 6";
                case Element.Hotbar7:
                    return "Hotbar 7";
                case Element.Hotbar8:
                    return "Hotbar 8";
                case Element.Hotbar9:
                    return "Hotbar 9";
                case Element.Hotbar10:
                    return "Hotbar 10";
                case Element.CrossHotbar:
                    return "Cross Hotbar";
                case Element.CastBar:
                    return "Cast Bar";
                case Element.ExperienceBar:
                    return "Experience Bar";
                case Element.InventoryGrid:
                    return "Inventory Grid";
                case Element.ScenarioGuide:
                    return "Scenario Guide";
                case Element.QuestLog:
                    return "Quest Log";
                case Element.MainMenu:
                    return "Main Menu";
                case Element.TargetInfo:
                    return "Target Info";
                case Element.PartyList:
                    return "Party List";
                case Element.LimitBreak:
                    return "Limit Break";
                case Element.StatusEnhancements:
                    return "Status Enhancements";
                case Element.StatusEnfeeblements:
                    return "Status Enfeeblements";
                case Element.StatusOther:
                    return "Status Other";
                default:
                    return element.ToString();
            }
        }

        public static string[] GetAddonName(Element element) {
            return element switch {
                Element.Hotbar1 => new string[] { "_ActionBar" },
                Element.Hotbar2 => new string[] { "_ActionBar01" },
                Element.Hotbar3 => new string[] { "_ActionBar02" },
                Element.Hotbar4 => new string[] { "_ActionBar03" },
                Element.Hotbar5 => new string[] { "_ActionBar04" },
                Element.Hotbar6 => new string[] { "_ActionBar05" },
                Element.Hotbar7 => new string[] { "_ActionBar06" },
                Element.Hotbar8 => new string[] { "_ActionBar07" },
                Element.Hotbar9 => new string[] { "_ActionBar08" },
                Element.Hotbar10 => new string[] { "_ActionBar09" },
                Element.CrossHotbar => new string[] { "_ActionCross", "_ActionDoubleCrossL", "_ActionDoubleCrossR" },
                Element.PetHotbar => new string[] { "_ActionBarEx" },
                Element.Job => new string[] {
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
                    "JobHudDNC0",
                    "JobHudBLM0",
                    "JobHudSMN0",
                    "JobHudSMN0",
                    "JobHudRDM0"
                },
                Element.PartyList => new string[] { "_PartyList" },
                Element.LimitBreak => new string[] { "_LimitBreak" },
                Element.Parameters => new string[] { "_ParameterWidget" },
                Element.Status => new string[] { "_Status" },
                Element.StatusEnhancements => new string[] { "_StatusCustom0" },
                Element.StatusEnfeeblements => new string[] { "_StatusCustom1" },
                Element.StatusOther => new string[] { "_StatusCustom2" },
                Element.CastBar => new string[] { "_CastBar" },
                Element.ExperienceBar => new string[] { "_Exp" },
                Element.ScenarioGuide => new string[] { "ScenarioTree" },
                Element.InventoryGrid => new string[] { "_BagWidget" },
                Element.QuestLog => new string[] { },
                Element.MainMenu => new string[] { "_MainCommand" },
                Element.Chat => new string[] { "ChatLog", "ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3" },
                Element.Minimap => new string[] { "_NaviMap" },
                Element.Currency => new string[] { "_Money" },
                Element.TargetInfo => new string[] { "_TargetInfoMainTarget", "_TargetInfoBuffDebuff", "_TargetInfoCastBar", "_TargetInfo" },
                Element.Unknown => new string[] { },
                _ => new string[] { },
            };
        }
    }
}