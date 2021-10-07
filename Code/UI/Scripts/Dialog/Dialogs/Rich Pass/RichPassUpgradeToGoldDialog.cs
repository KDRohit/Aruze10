 using System.Collections;
 using UnityEngine;
 using TMPro;
 using System.Collections.Generic;
 using Com.Scheduler;

 public class RichPassUpgradeToGoldDialog : DialogBase
{
    [SerializeField] private LabelWrapperComponent headerLabel;
    [SerializeField] private LabelWrapperComponent bodyLabel;
    [SerializeField] private ButtonHandler buyGoldButton;
    [SerializeField] private ButtonHandler checkItOutButton;
    [SerializeField] private UITexture background;

    private string source = "";
    private bool elite = false;

    private const string RP_GOLD_UPGRADE_HEADER_LOC = "rp_gold_upgrade_header";
    private const string RP_GOLD_UPGRADE_BODY_LOC = "rp_gold_upgrade_body";
    private const string RP_UNLOCK_GOLD_LOC = "rp_unlock_gold";

    public override void init()
    {
        int dollarValue = ExperimentWrapper.RichPass.passValueAmount;
        RichPassPackage goldPackage = CampaignDirector.richPass.getCurrentPackage();
        string goldPassCost = goldPackage != null && goldPackage.purchasePackage != null
            ? goldPackage.purchasePackage.getLocalizedPrice()
            : "";
        int unclaimedRewardCount = CampaignDirector.richPass.getNumberOfUnclaimedRewards(true);
        SafeSet.gameObjectActive(bodyLabel.gameObject, unclaimedRewardCount > 0);
        bodyLabel.text = Localize.text(RP_GOLD_UPGRADE_BODY_LOC, unclaimedRewardCount);
        elite = (bool) dialogArgs.getWithDefault(D.CUSTOM_INPUT, false);
        if (!elite)
        {
            headerLabel.text = Localize.text(RP_GOLD_UPGRADE_HEADER_LOC, CommonText.formatNumber(dollarValue));
            if (buyGoldButton != null)
            {
                buyGoldButton.text = Localize.text(RP_UNLOCK_GOLD_LOC, goldPassCost);
                buyGoldButton.registerEventDelegate(buyGoldClicked);
            }
        }
        else
        {
            StatsManager.Instance.LogCount(
                counterName: "dialog",
                kingdom: "elite",
                phylum:"richpass_grant",
                family: ExperimentWrapper.RichPass.experimentData.variantName,
                genus:"view"
            );
            if (checkItOutButton != null)
            {
                checkItOutButton.registerEventDelegate(checkOutEliteGold);

            }
        }
   
        source = (string) dialogArgs.getWithDefault(D.DATA, "");
        StatsRichPass.logUpgradeToGoldDialog("view", source);
        downloadedTextureToUITexture(background, 0);
    
    }

    private void checkOutEliteGold(Dict args = null)
    {
        Dialog.close(this);
    }
    
    private void buyGoldClicked(Dict args = null)
    {
        Audio.play("ButtonBuyRichPass");
        StatsRichPass.logUpgradeToGoldDialog("upgrade", source);
        CampaignDirector.richPass.purchasePackage();
    }

    public override void onCloseButtonClicked(Dict args = null)
    {
        base.onCloseButtonClicked(args);
        StatsRichPass.logUpgradeToGoldDialog("close", source);
    }

    public override void close()
    {
        if (checkItOutButton != null)
        {
            checkItOutButton.unregisterEventDelegate(checkOutEliteGold);

        }

        if (elite)
        {
            elite = false;
            StatsManager.Instance.LogCount(
                counterName: "dialog",
                kingdom: "elite",
                phylum:"richpass_grant",
                family: ExperimentWrapper.RichPass.experimentData.variantName,
                genus:"click"
            );
            CampaignDirector.richPass.upgradePass("gold");
        }
    }

    public static void showDialog(string source, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.MEDIUM, bool elite = false)
    {
        if (CampaignDirector.richPass != null && CampaignDirector.richPass.isEnabled)
        {
            bool isBundled =
                !string.IsNullOrEmpty(
                    AssetBundleManager.getBundleNameForResource(CampaignDirector.richPass.dialogBackgroundPath));
            if (elite)
            {
                Dict args = Dict.create(D.DATA, source, D.CUSTOM_INPUT, elite);
                Dialog.instance.showDialogAfterDownloadingTextures("rich_pass_upgrade_to_gold_from_elite_dialog",
                    CampaignDirector.richPass.dialogBackgroundPath, args, isExplicitPath: isBundled,
                    priorityType: priority, skipBundleMapping: isBundled);

            }
            else
            {
                Dialog.instance.showDialogAfterDownloadingTextures("rich_pass_upgrade_to_gold_dialog",
                    CampaignDirector.richPass.dialogBackgroundPath, Dict.create(D.DATA, source),
                    isExplicitPath: isBundled, priorityType: priority, skipBundleMapping: isBundled);
            }
        }
    }
}
