using UnityEngine;
using Com.Scheduler;
using TMPro;

public class RichPassSummaryDialog : DialogBase
{
    [SerializeField] private LabelWrapperComponent bodyLabel;
    [SerializeField] private ButtonHandler ctaButton;
    [SerializeField] private UITexture background;

    private const string RP_GOLD_UPGRADE_BODY_LOC = "rp_gold_upgrade_header";

    public override void init()
    {
        Audio.play("MOTDRichPass");
        int dollarValue = ExperimentWrapper.RichPass.passValueAmount;
        bodyLabel.text = Localize.text(RP_GOLD_UPGRADE_BODY_LOC, CommonText.formatNumber(dollarValue));
        if (EliteManager.hasGoldFromUpgrade)
        {
            ctaButton.registerEventDelegate(checkOut);
        }
        else
        {
            ctaButton.registerEventDelegate(ctaClicked);

        }
        downloadedTextureToUITexture(background, 0);
        
    }

    private void checkOut(Dict args = null)
    {
        Dialog.close(this);
    }
    private void ctaClicked(Dict args = null)
    {
        Audio.play("ButtonConfirm");
        RichPassFeatureDialog.showDialog(CampaignDirector.richPass, SchedulerPriority.PriorityType.HIGH);
        Dialog.close();
    }

    public override void close()
    {
        if (EliteManager.hasGoldFromUpgrade)
        {
            RichPassUpgradeToGoldDialog.showDialog("gold_game", SchedulerPriority.PriorityType.IMMEDIATE,true);
            ctaButton.unregisterEventDelegate(checkOut);
        }
        else
        {
            ctaButton.unregisterEventDelegate(ctaClicked);

        }
    }

    public static bool showDialog(string motdKey = "")
    {
        bool isBundled = !string.IsNullOrEmpty(AssetBundleManager.getBundleNameForResource(CampaignDirector.richPass.dialogBackgroundPath));
        Dialog.instance.showDialogAfterDownloadingTextures("rich_pass_summary_dialog", CampaignDirector.richPass.dialogBackgroundPath, Dict.create(D.MOTD_KEY, motdKey), isExplicitPath:isBundled, skipBundleMapping:isBundled);
        return true;
    }
    
    public override void onCloseButtonClicked(Dict args = null)
    {
        base.onCloseButtonClicked(args);
        Audio.play("ButtonConfirm");
    }
}
