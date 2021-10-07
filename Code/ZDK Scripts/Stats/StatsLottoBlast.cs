using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StatsLottoBlast
{
    private const string COUNTER = "dialog";
    public const string KINGDOM = "lotto_blast";

    public static void logFeatureDialogAction(int startLevel, int endLevel, string action)
    {
        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: "feature_view",
            klass: startLevel.ToString(),
            family: endLevel.ToString(),
            genus: action
        );
    }

    public static void logFeatureDialogPurchaseOverlayAction(string action)
    {
		StatsManager.Instance.LogCount
		(
			counterName: COUNTER,
			kingdom: KINGDOM,
			phylum: "extra_dialog",
			genus: action
	    );	
    }

    public static void logBuyPremiumGame(string phylum)
    {
        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: phylum,
            genus: "click"
        );
    }
    
    public static void logBuyBuff()
    {
        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: "lotto_blast_buy_powerup",
            genus: "click"
        );
    }
    
    public static void logConfirmationDialogView()
    {
        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: "are_you_sure",
            genus: "view"
        );	
    }
    
    public static void logConfirmationDialogClose()
    {
        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: "are_you_sure",
            genus: "close"
        );	
    }

    public static void logCollectReward(string phylum, int ball1Multiplier, int ball2Multiplier, long seedValue, long amount)
    {
        string family = "";
        if (ball2Multiplier > 0)
        {
            family = ball2Multiplier.ToString();
        }

        seedValue = CreditsEconomy.multipliedCredits(seedValue);
        amount = CreditsEconomy.multipliedCredits(amount);

        StatsManager.Instance.LogCount
        (
            counterName: COUNTER,
            kingdom: KINGDOM,
            phylum: phylum,
            klass: ball1Multiplier.ToString(),
            family: family,
            milestone: seedValue.ToString(),
            val: amount,
            genus: "click"
        );
    }

}
