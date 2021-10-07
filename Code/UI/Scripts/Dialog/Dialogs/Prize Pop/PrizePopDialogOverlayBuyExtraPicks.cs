using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Rewardables;
using PrizePop;

public class PrizePopDialogOverlayBuyExtraPicks : PrizePopDialogOverlay
{
    [SerializeField] private LabelWrapperComponent extraPicksLabel;
    [SerializeField] private LabelWrapperComponent creditsLabel;
    [SerializeField] private LabelWrapperComponent priceLabel;
    [SerializeField] private GameObject bonusValueObject;
    [SerializeField] private LabelWrapperComponent bonusValueLabel;
    [SerializeField] private ObjectSwapper endingSoonSwapper;

    private const string INTRO_SOUND = "BuyExtraPrizePopCommon";
    private string statsKlass = "";
    private string packageName = "";

    public static PrizePopDialogOverlayBuyExtraPicks instance;
    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        parentDialog = parent;
        closeButton.registerEventDelegate(closeClicked, overlayArgs);
        ctaButton.registerEventDelegate(ctaClicked, overlayArgs);
        if (overlayArgs != null)
        {
            statsKlass = (string) overlayArgs.getWithDefault(D.TYPE, "");
        }

        Audio.play(INTRO_SOUND);
        extraPicksLabel.text = string.Format("{0} Extra Chances", CommonText.formatNumber(PrizePopFeature.instance.currentPackagePicks)); //Need this value from login data
        PurchasablePackage currentPackage = PrizePopFeature.instance.getCurrentPackage();
        if (currentPackage != null)
        {
            creditsLabel.text = CreditsEconomy.convertCredits(currentPackage.totalCredits());
            priceLabel.text = string.Format("Buy Now {0}", currentPackage.getLocalizedPrice());
            packageName = currentPackage.keyName;
        }
        endingSoonSwapper.setState(PrizePopFeature.instance.isEndingSoon() ? "ends_soon" : "default");
        bonusValueObject.SetActive(false); //TODO: Need to get this data from server
        StatsPrizePop.logViewBuyPicks(statsKlass, packageName);
    }

    protected override void closeClicked(Dict args = null)
    {
        StatsPrizePop.logCloseBuyPicks(statsKlass, packageName);
        bool shouldClose = args == null || (bool) args.getWithDefault(D.CLOSE, true);
        if (shouldClose)
        {
            Dialog.close();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected override void ctaClicked(Dict args = null)
    {
        if (parentDialog.type.keyName != "prize_pop_overlay")
        {
            Destroy(gameObject);
        }
        
#if !UNITY_EDITOR
        PrizePopFeature.instance.purchaseExtraPick();
#else
        PrizePopAction.devAddPicks(PrizePopFeature.instance.currentPackagePicks);
#endif
    }

    public void onEndingSoon()
    {
        closeClicked();
    }

    private void OnDestroy()
    {
        instance = null;
    }
}
