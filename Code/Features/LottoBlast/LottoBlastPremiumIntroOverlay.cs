using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LottoBlastPremiumIntroOverlay : MonoBehaviour
{
    [SerializeField] ButtonHandler buyButtonHandler;

    [SerializeField] private AnimationListController.AnimationInformationList animInfo;
    [SerializeField] private AnimationListController.AnimationInformationList introAnimInfo;
    [SerializeField] private AudioListController.AudioInformationList audioInfo;

    public TextMeshPro buyButtonText;
    [SerializeField] protected PurchasePerksPanel perksPanel;
    [SerializeField] protected GameObject perksPanelParent;

    private bool purchaseSucceeded = false;

    private CreditPackage creditPackage;
    // Start is called before the first frame update
    void Start()
    {
        buyButtonHandler.registerEventDelegate(onBuyButtonClicked);
        
        creditPackage = LottoBlastMinigameDialog.instance.creditPackage;
        if (creditPackage != null)
        {
            List<PurchasePerksPanel.PerkType> cyclingPerks = PurchasePerksPanel.getEligiblePerksForPackage(creditPackage, -1);
            PurchasePerksCycler perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, Mathf.Min(ExperimentWrapper.BuyPageDrawer.maxItemsToRotate, cyclingPerks.Count));
            perksPanel.init(-1, creditPackage, StatsLottoBlast.KINGDOM, cyclingPerks, perksCycler: perksCycler);
            perksCycler.startCycling();
        }
    }

    public void show()
    {
        buyButtonHandler.enabled = true;
        buyButtonText.text = "For " + LottoBlastMinigameDialog.instance.premiumBuyButtonText;
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimInfo));
    }

    private void playOutro()
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(animInfo));
    }

    public void showPurchaseSucceededMode()
    {
        buyButtonHandler.enabled = true;
        purchaseSucceeded = true;
        perksPanelParent.SetActive(false);
        buyButtonText.text = "START";
    }

    private void onBuyButtonClicked(Dict args = null)
    {
        buyButtonHandler.enabled = false;
        if (purchaseSucceeded)
        {
            LottoBlastMinigameDialog.instance.startPremiumGameRoutine();
            
            playOutro();
        }
        else
        {
            StartCoroutine(AudioListController.playListOfAudioInformation(audioInfo));
            LottoBlastMinigameDialog.instance.attemptPremiumGamePurchase();
            StatsLottoBlast.logBuyPremiumGame("lotto_blast_premium");
        }
    }

}
