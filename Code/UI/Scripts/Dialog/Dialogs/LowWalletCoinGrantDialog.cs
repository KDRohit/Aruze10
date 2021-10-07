using Com.Scheduler;
using UnityEngine;

public class LowWalletCoinGrantDialog : DialogBase
{
    [SerializeField] private LabelWrapperComponent creditsLabel;
    [SerializeField] private ClickHandler collectButton;

    private long creditsToAdd;
    public override void init()
    {
        creditsToAdd = (long)dialogArgs.getWithDefault(D.VALUE, 0);
        creditsLabel.text = CreditsEconomy.convertCredits(creditsToAdd);
        
        collectButton.registerEventDelegate(collectClicked);
        logStat("view");
    }

    private void collectClicked(Dict args)
    {
        logStat("click");
        SlotsPlayer.addNonpendingFeatureCredits(creditsToAdd, "ooc_low_wallet", false);
        Dialog.close();
    }

    public override void close()
    {
        logStat("close");
    }

    private void logStat(string genus)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: "ooc_low_wallet",
            phylum:"low_wallet",
            genus: genus
        );
    }

    public static void registerEventDelegates()
    {
        Server.registerEventDelegate("low_wallet_coins_granted", showDialog, true);
    }
    

    private static void showDialog(JSON data)
    {
        JSON grantData = data.getJSON("grant_data");
        if (grantData != null)
        {
            long creditsToAdd = grantData.getLong("value", 0);
            if (creditsToAdd > 0)
            {
                Scheduler.addDialog("low_wallet_grant", Dict.create(D.VALUE, creditsToAdd), SchedulerPriority.PriorityType.HIGH);
            }
        }
    }
}
