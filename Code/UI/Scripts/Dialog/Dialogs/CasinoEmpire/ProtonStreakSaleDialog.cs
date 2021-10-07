using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using FeatureOrchestrator;
using UnityEngine;

public class ProtonStreakSaleDialog : DialogBase
{
    [SerializeField] private UITexture bgTexture;
    [SerializeField] private GameObject purchseOfferPrefab;
    [SerializeField] private UIGrid offerGrid;
    [SerializeField] Transform coinExplosion;
    [SerializeField] private AnimationListController.AnimationInformationList purchaseCompleteAnimList;
    [SerializeField] private LabelWrapperComponent durationLabel;
    [SerializeField] private ProtonDialogComponentButton closeButton;
    [SerializeField] private ProtonDialogComponentButton ctaButton;
    [SerializeField] private LabelWrapperComponent footerLabel;
    [SerializeField] private string DIALOG_OPEN_AUDIO = "";
    [SerializeField] private string DIALOG_CLOSE_AUDIO = "";

    private List<RewardPurchaseOffer> purchaseOffers = new List<RewardPurchaseOffer>();
    private ShowDialogComponent parentComponent;
    private string iconBasePath = "";
    private ProgressCounter offersCounter;
    
    private List<ProtonStreakSaleDialogOfferNode> createdNodes = new List<ProtonStreakSaleDialogOfferNode>();
    private string[] packKeys;

    private int streakIndex = 0;
    private GameTimerRange eventTimer;
    
    private string statKingdom = "";
    private string statPhylum = "";

    public override void init()
    {
        economyTrackingName = type.keyName;
        if (downloadedTextureToUITexture(bgTexture, 0))
        {
            bgTexture.gameObject.SetActive(true);
        }
        parseDialogArgs();
        createOfferObjects();
        logStat("view");
    }
    
    private void createOfferObjects()
    {
        for (int i = 0; i < purchaseOffers.Count; i++)
        {
            GameObject offerInstance = NGUITools.AddChild(offerGrid.transform, purchseOfferPrefab);
            ProtonStreakSaleDialogOfferNode offerNode = offerInstance.GetComponent<ProtonStreakSaleDialogOfferNode>();
            if (offerNode != null)
            {
                createdNodes.Add(offerNode);
                
                CreditPackage package = new CreditPackage(purchaseOffers[i].package.purchasePackage, purchaseOffers[i].package.bonus, false);
                package.collectableDropKeyName = i < packKeys.Length && packKeys[i] != null ? packKeys[i] : "";
                List<PurchasePerksPanel.PerkType> cyclingPerks = PurchasePerksPanel.getEligiblePerksForPackage(package, -1);
                PurchasePerksCycler perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, cyclingPerks.Count);
                perksCycler.startCycling();
                offerNode.init(package, purchaseOffers[i],i, streakIndex, cyclingPerks, perksCycler, statKingdom, statPhylum, iconBasePath);
            }
        }
        
        StartCoroutine(playNodeIntros());
    }

    private IEnumerator playNodeIntros()
    {
        for (int i = 0; i < createdNodes.Count; i++)
        {
            yield return StartCoroutine(createdNodes[i].playIntro());
        }
    }

    public override void close()
    {
        logStat("close");
        eventTimer.removeFunction(onEventEnd);
    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        return PurchaseSuccessActionType.leaveDialogOpenAndShowThankYouDialog;
    }

    protected override void onShow()
    {
        int newIndex = (int)offersCounter.currentValue;
        if (newIndex != streakIndex)
        {
            //Unlock the next package or close the dialog once we've gone through all of them
            if (newIndex > offersCounter.completeValue)
            {
                Dialog.close(this);
            }
            else
            {
                streakIndex = newIndex;
                coinExplosion.position = createdNodes[streakIndex - 1].buttonPostion;
                for (int i = 0; i < createdNodes.Count; i++)
                {
                    StartCoroutine(createdNodes[i].updateState(streakIndex));
                }
            }
        }
    }

    private void parseDialogArgs()
    {
        parentComponent = (ShowDialogComponent)dialogArgs.getWithDefault(D.DATA, null);
        if (parentComponent != null)
        {
            object[] purchaseOfferObjects = (object[])parentComponent.jsonData.jsonDict["packageOffers"];
            for (int i = 0; i < purchaseOfferObjects.Length; i++)
            {
                if (purchaseOfferObjects[i] is RewardPurchaseOffer offerReward)
                {
                    purchaseOffers.Add(offerReward);
                }
            }
            
            object[] cardPackKeysObjects = (object[])parentComponent.jsonData.jsonDict["cardPackKeys"];
            packKeys = new string[cardPackKeysObjects.Length];
            for (int i = 0; i < cardPackKeysObjects.Length; i++)
            {
                packKeys[i] = cardPackKeysObjects[i] as string;
            }

            iconBasePath = parentComponent.jsonData.getString("offerIconBasePath", "");
            offersCounter = parentComponent.jsonData.jsonDict["offerCounter"] as ProgressCounter;
            streakIndex = (int)offersCounter.currentValue;
            statKingdom = parentComponent.jsonData.getString("kingdom", "");
            statPhylum = parentComponent.jsonData.getString("phylum", "");

            TimePeriod featureTimer = parentComponent.jsonData.jsonDict["timePeriod"] as TimePeriod;
            if (featureTimer != null)
            {
                eventTimer = featureTimer.durationTimer;
                eventTimer.registerLabel(durationLabel.labelWrapper, format:GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT, keepCurrentText: true);
                eventTimer.registerFunction(onEventEnd);
            }

            closeButton.registerToParentComponent(parentComponent);
            ctaButton.registerToParentComponent(parentComponent);

            string footerLocalization = parentComponent.jsonData.getString("footerText", "");
            footerLabel.text = Localize.text(footerLocalization);
        }
    }

    private void logStat(string genus)
    {
        StatsManager.Instance.LogCount(
            counterName: "dialog",
            kingdom: statKingdom,
            phylum: statPhylum,
            genus: genus
        );
    }

    private void onEventEnd(Dict args, GameTimerRange caller)
    {
        //Close dialog instantly when event ends
        Dialog.close(this);
    }
    
    protected override void playOpenSound()
    {
	    Audio.play(DIALOG_OPEN_AUDIO);
    }
    
    public override void playCloseSound()
    {
	    Audio.play(DIALOG_CLOSE_AUDIO);
    }
}
