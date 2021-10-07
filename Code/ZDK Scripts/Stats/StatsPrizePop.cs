public class StatsPrizePop
{
    public static void logViewBuyPicks(string source, string packageName)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:"prize_pop_offer",
            klass:source,
            family:packageName,
            genus: "view"
        );
    }
    
    public static void logCloseBuyPicks(string source, string packageName)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:"prize_pop_offer",
            klass:source,
            family:packageName,
            genus: "click"
        );
    }
    
    public static void logViewBoardDialog(int stageNumber, bool activeBonus, bool manualOpen, int remainingObjects)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:stageNumber.ToString(),
            klass:manualOpen ? "manual" : "auto",
            family:activeBonus ? "on" : "off",
            genus: "view",
            milestone:remainingObjects.ToString()
        );
    }
    
    public static void logCloseBoardDialog(int stageNumber, bool activeBonus, bool manualOpen, int remainingObjects)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:stageNumber.ToString(),
            klass:manualOpen ? "manual" : "auto",
            family:activeBonus ? "on" : "off",
            genus: "view",
            milestone:remainingObjects.ToString()
        );
    }

    public static void logItemPick(string type, string value, int remainingObjects)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:"object_click",
            klass:type,
            family:value,
            genus: "click",
            milestone:remainingObjects.ToString()
        );
    }

    public static void logOverlayView(string type)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:type,
            genus: "view"
        );
    }
    
    public static void logOverlayClose(string type)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum:type,
            genus: "close"
        );
    }

    public static void logShowVideo(bool autoPlayed)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "prize_pop",
            phylum: autoPlayed ? "primary" : "question_mark",
            klass: "watch_video",
            genus: "click"
        );
    }
}
