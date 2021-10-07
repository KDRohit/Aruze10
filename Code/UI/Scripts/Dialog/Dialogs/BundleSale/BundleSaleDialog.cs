using UnityEngine;
using Com.Scheduler;
using System.Collections.Generic;
using Code.UI.Scripts.Dialog.Dialogs.BundleSale;
using Com.HitItRich.Feature.BundleSale;

using TMPro;
using Object = UnityEngine.Object;

public class BundleSaleDialog : DialogBase
{
    private const string DAILY_BONUS_PREFAB_PATH = "Features/Bundle Sale/Prefabs/Instanced Prefabs/Bundle Sale Perk Item - Daily Bonus";
    private const string TRIPLE_XP_PREFAB_PATH = "Features/Bundle Sale/Prefabs/Instanced Prefabs/Bundle Sale Perk Item - Triple XP";
    private const string UNLOCK_ALL_GAMES_PREFAB_PATH = "Features/Bundle Sale/Prefabs/Instanced Prefabs/Bundle Sale Perk Item - Unlock All Games";
        
    
    [SerializeField] private LabelWrapperComponent coins;
    [SerializeField] private LabelWrapperComponent buyPrice;
    [SerializeField] private LabelWrapperComponent buyValue;
    [SerializeField] private LabelWrapperComponent title;
    [SerializeField] private LabelWrapperComponent clock;
    [SerializeField] private GameObject saleObjectParent_0;
    [SerializeField] private GameObject saleObjectParent_1;
    [SerializeField] private GameObject saleObjectParent_2;
    [SerializeField] private ClickHandler buyButtonHandler;
    [SerializeField] private AnimationListController.AnimationInformationList oneItemAnimation;
    [SerializeField] private AnimationListController.AnimationInformationList twoItemAnimation;
    [SerializeField] private AnimationListController.AnimationInformationList threeItemAnimation;
    
    
    private CreditPackage purchaseItem = null;
    private GameTimerRange saleTimer = null;
    private Dictionary<string, string> itemKeyMap = new Dictionary<string, string>()
    {
        {"unlock_all_games", "unlock_all_games"},
        {"xp_multiplier","triple_xp"},
        {"daily_bonus_reduced_timer","daily_bonus"}
    };
    
    //for stats
    private bool purchasedCausedClose = false;
    public override void init()
    {
        purchasedCausedClose = false;
        BundleSaleFeature feature = BundleSaleFeature.instance;
        List<BundleSaleFeature.BundleItem> bundleItems = dialogArgs.getWithDefault(D.OPTION, new List<BundleSaleFeature.BundleItem>()) as List<BundleSaleFeature.BundleItem>; 
        purchaseItem = dialogArgs.getWithDefault(D.OPTION1, null) as CreditPackage;
        if (feature != null && bundleItems != null && bundleItems.Count >0 && purchaseItem != null) 
        {
            switch (bundleItems.Count)
            {
                default:
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(oneItemAnimation));
                    break;
                case 2:
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(twoItemAnimation));
                    break;
                case 3:
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(threeItemAnimation));
                    break;
            }
            
            for(int index = 0; index < bundleItems.Count; index++ ) 
            {
                BundleSaleFeature.BundleItem item = bundleItems[index];
                if (itemKeyMap.TryGetValue(item.buffType, out string itemKey))
                {
                    populateSaleItems(item.getTitle(), item.buffDuration, itemKey, index);
                }
            }
            
            SafeSet.labelText(coins.labelWrapper,  CreditsEconomy.convertCredits(purchaseItem.purchasePackage.totalCredits(bonusPercent: BundleSaleFeature.instance.saleBonusPercent)));
            SafeSet.labelText(buyPrice.labelWrapper, Localize.text(feature.salePreText) + " " + purchaseItem.purchasePackage.priceLocalized);
            SafeSet.labelText(buyValue.labelWrapper, feature.badgeText);
            SafeSet.labelText(title.labelWrapper, feature.saleTitle);
            if (buyButtonHandler != null)
            {
                buyButtonHandler.registerEventDelegate(onBuyClick);
            }

            if (clock != null)
            {
                int endTime = BundleSaleFeature.instance.getSaleEndTime();
                if (BundleSaleFeature.instance.isTimerVisible && endTime > GameTimer.currentTime)
                {
                    clock.gameObject.SetActive(true);
                    saleTimer = new GameTimerRange(GameTimer.currentTime, endTime);
                    saleTimer.registerLabel(clock.labelWrapper, GameTimerRange.TimeFormat.REMAINING, true);
                    saleTimer.registerFunction(onSaleTimerExpires);
                }
                else
                {
                    clock.gameObject.SetActive(false);
                }
            }
        }
        
        StatsManager.Instance.LogCount("dialog", "bundle_sale", "dialog", "starter_pack", ExperimentWrapper.BundleSale.bundleId, "view");
    }

    private void onSaleTimerExpires(Dict args = null, GameTimerRange originalTimer = null)
    {
        saleTimer.removeFunction(onSaleTimerExpires);
        Dialog.close(this);
    }

    private void onBuyClick(Dict args)
    {
        BundleSaleFeature.instance.doPurchase();
    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        //callback from new economy manager.
        //tell the bundle sale feature the purchase succeeded and close the dialog
        BundleSaleFeature.instance.onPurchaseSucceeded();
        purchasedCausedClose = true;
        return PurchaseSuccessActionType.closeDialog;
    }

    public GameObject getParent(int index)
    {
        GameObject parent = null;
        switch (index)
        {
            case 0:
                parent = saleObjectParent_0;
                break;
            case 1:
                parent = saleObjectParent_1;
                break;
            case 2:
                parent = saleObjectParent_2;
                break;
        }

        return parent;
    }

    private void populateSaleItems(string title,int duration,string saleItem,int index)
    {
        GameObject parent = getParent(index);
        
        if (parent == null)
        {
            Debug.LogError("No parent object for index: " + index);
            return;
        }
        
        Dict args = Dict.create(D.DATA, parent, D.TIME, duration, D.TITLE, title);
       
        switch (saleItem)
        {
            case "daily_bonus":
                AssetBundleManager.load(DAILY_BONUS_PREFAB_PATH, onLoadPowerup, onLoadAssetFailure, args, isSkippingMapping:true, fileExtension:".prefab");
                break;
            case "triple_xp":
                AssetBundleManager.load(TRIPLE_XP_PREFAB_PATH, onLoadPowerup, onLoadAssetFailure, args, isSkippingMapping:true, fileExtension:".prefab");
                break;
            case "unlock_all_games":
                AssetBundleManager.load(UNLOCK_ALL_GAMES_PREFAB_PATH, onLoadPowerup, onLoadAssetFailure, args, isSkippingMapping:true, fileExtension:".prefab");
                break;
        }
    }
    
    private void onLoadPowerup(string assetPath, object loadedObj, Dict data = null)
    {
        if (data == null)
        {
            Debug.LogError("No data passed into load callback");
            return;
        }
        
        GameObject parent = data[D.DATA] as GameObject;
        int duration = (int)data[D.TIME];
        string titleText = data[D.TITLE] as string;
        
        GameObject powerupOverlayObj = NGUITools.AddChild(parent != null ? parent.transform : this.gameObject.transform, loadedObj as GameObject);
        if (powerupOverlayObj == null)
        {
            Debug.LogError("prefab broken");
        }
        else
        {
            BundleSaleItemHelper helper = powerupOverlayObj.GetComponent<BundleSaleItemHelper>();
            if (helper != null)
            {
                helper.setText(titleText, duration);
            }
        }
    }
    
    private static void onLoadAssetFailure(string assetPath, Dict data = null)
    {
        Debug.LogError(string.Format("Failed to load asset at {0}", assetPath));
    }

    public override void close()
    {
        if (buyButtonHandler != null)
        {
            buyButtonHandler.unregisterEventDelegate(onBuyClick);
        }
        if (clock != null)
        {
            if (saleTimer != null)
            {
                saleTimer.removeFunction(onSaleTimerExpires);
                saleTimer.removeLabel(clock.labelWrapper);
                saleTimer = null;
            }
        }
        string closeStatString = "close";
        if (purchasedCausedClose)
        {
            closeStatString = "cta";
        }
        StatsManager.Instance.LogCount("dialog", "bundle_sale", "dialog", "starter_pack", closeStatString, "click");
    }

    public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
    {
        Scheduler.addDialog("bundle_sale_dialog", args, priority);
    }
}
