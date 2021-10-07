using System.Collections.Generic;

public class UnlockAllGamesPowerup : PowerupBase
{
    public const string POWER_UNLOCK_ALL_GAMES_PAGE_KEY  = "powerup_unlockAllGames";

    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.COMMON;
       aliasKeys = new List<string>
        {
            "unlock_all_games_"
        };
       name = POWER_UNLOCK_ALL_GAMES_PAGE_KEY;
       isDisplayablePowerup = false;
    }
    public override void apply(int totalTime, int durationRemaining)
    {
        base.apply(totalTime, durationRemaining);
        int currentTime = GameTimer.currentTime; 
        UnlockAllGamesFeature.instance.addTimeRange(currentTime,currentTime+durationRemaining,UnlockAllGamesFeature.Source.Powerup);
        if (MainLobby.instance != null && !Loading.isLoading)
        {
            MainLobby.refresh(null);
        }
    }

    public override void remove(Dict args = null, GameTimerRange sender = null)
    {
        base.remove(args, sender);
        UnlockAllGamesFeature.instance.stopTimer(UnlockAllGamesFeature.Source.Powerup);
     
    }

}
