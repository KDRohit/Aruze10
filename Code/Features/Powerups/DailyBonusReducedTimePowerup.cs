
public class DailyBonusReducedTimePowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.UNCOMMON; 
        aliasKeys = new System.Collections.Generic.List<string>
        {
            "dailybonus_"
        };
        name = BUNDLE_SALE_DAILY_BONUS;
        uiPrefabName = "PowerUp Icon Daily Bonus Item";
        isDisplayablePowerup = false;
    }

    public override bool canPerformAction
    {
        get { return SlotsPlayer.instance.dailyBonusTimer.isExpired; }
    }
}