public class StatsRichPass
{
    public static void logLobbyIconClick()
    {
        StatsManager.Instance.LogCount(
            counterName:"lobby",
            kingdom: "rich_pass_icon",
            genus: "click"
        );
    }
    
    public static void logInGameUIClick()
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"daily_challenge_icon",
            genus: "click"
        );
    }

    public static void logFeatureDialogStateView(string state)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"feature",
            klass: state,
            genus: "view"
        );
    }

    public static void logFeatureDialogStateClick(string state)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"navigate",
            klass: state,
            genus: "click"
        );
    }

    public static void logUpgradeToGoldDialog(string genus, string source)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"upgrade_dialog",
            klass:source,
            genus: genus
        );
    }

    public static void logNewSeasonalChallenges(string genus)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"new_seasonal_challenges",
            genus: genus
        );
    }
    public static void logFeatureDialogGoldUpgrade()
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"gold_upgrade",
            genus: "click"
        );
    }
    
    public static void logInfoClick()
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "rich_pass",
            phylum:"watch_video",
            genus: "click"
        );
    }
}
