using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using FeatureOrchestrator;
using PrizePop;
using UnityEngine;

public class PurchasePerksPanel : MonoBehaviour
{
    public ButtonHandler openDrawerButton;

    [SerializeField] private ObjectSwapper panelSwapper; //Expanded object when cycler is clicked on
    [SerializeField] private bool usePanelBacking; //Expanded object when cycler is clicked on
    [SerializeField] private Transform cyclerParent;
    [SerializeField] private PurchasePerksDrawer perksDrawer; //Expanded object when cycler is clicked on

    private List<PerkType> perksToShow;
    private Dictionary<PerkType, PurchasePerksIcon> loadedPerkIcons = new Dictionary<PerkType, PurchasePerksIcon>();
    private CreditPackage creditPackage;
    private string statKingdom = "";

    private PurchasePerksIcon currentCyclingIcon;

    private BuyCreditsOptionNewHIR buyPageOptionParent;
    private bool isPurchase = false;
    
    private const string PERKS_ICON_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Purchase Perks/Prefabs/Instanced Prefabs/Drawer Item Types/Perk Item {0}.prefab";

    public enum PerkType
    {
        Card_Pack,
        Casino_Empire_Dice,
        Elite_Gift,
        Elite_Points,
        Pet_Treat,
        Pinata,
        Power_Ups,
        PrizePop_Extras,
        Vip_Points,
        XP_Boost_Triple,
        None
    };

    private static readonly Dictionary<string, PerkType> typeMap = new Dictionary<string, PerkType>()
    {
        {"card_pack", PerkType.Card_Pack},
        {"elite_gift", PerkType.Elite_Gift},
        {"elite_points", PerkType.Elite_Points},
        {"pinata", PerkType.Pinata},
        {"power_ups", PerkType.Power_Ups},
        {"vip_points", PerkType.Vip_Points},
        {"prize_pop_extras", PerkType.PrizePop_Extras},
        {"triple_xp_buff", PerkType.XP_Boost_Triple},
        {"pet_treat", PerkType.Pet_Treat},
        {"bg_dice", PerkType.Casino_Empire_Dice},
    };


    public void init(int index, CreditPackage package, string statName, List<PerkType> perks, ClickHandler.onClickDelegate closeButtonDelegate = null, PurchasePerksCycler perksCycler = null, BuyCreditsOptionNewHIR buyPageOption = null, PurchaseFeatureData.Type purchaseType = PurchaseFeatureData.Type.NONE)
    {
        if (perksCycler != null)
        {
            perksCycler.panelsToCycle.Add(this);
        }
        
        buyPageOptionParent = buyPageOption;
        perksToShow = perks;
        creditPackage = package;
        statKingdom = statName;

        int activePerks = 0;
        
        //Load icons for cycling perks
        for (int i = 0; i < perksToShow.Count; i++)
        {
            if (isTypeActiveForPackage(perksToShow[i], package, index, purchaseType))
            {
                activePerks++;
                SkuResources.loadFromMegaBundleWithCallbacks(this, string.Format(PERKS_ICON_PREFAB_PATH, perksToShow[i].ToString()), iconLoadSuccess, iconLoadFailed, Dict.create(D.DATA, i, D.TYPE, perksToShow[i], D.INDEX, index));
            }
        }

        if (panelSwapper != null)
        {
            if (activePerks <= 1)
            {
                if (usePanelBacking)
                {
                    panelSwapper.setState(PurchasePerksIconContainer.SwapperStates.DETACHED_WITH_BACKING_STATE);
                }
                else
                {
                    panelSwapper.setState(PurchasePerksIconContainer.SwapperStates.DETACHED_WITHOUT_BACKING_STATE);
                }
            }
            else
            {
                if (usePanelBacking)
                {
                    panelSwapper.setState(PurchasePerksIconContainer.SwapperStates.ATTACHED_WITH_BACKING_STATE);
                }
                else
                {
                    panelSwapper.setState(PurchasePerksIconContainer.SwapperStates.ATTACHED_WITHOUT_BACKING_STATE);
                }
            }
        }

        openDrawerButton.registerEventDelegate(openDrawerClicked);
        if (closeButtonDelegate != null)
        {
            perksDrawer.closeButton.registerEventDelegate(closeButtonDelegate, Dict.create(D.INDEX, index));
        }

        if (perksDrawer != null && perksDrawer.closeButton != null)
        {
            perksDrawer.closeButton.registerEventDelegate(closeDrawerClicked);
        }
    }

    public void initConfirmationPerks(PurchaseFeatureData.Type purchaseType, string packageName, RewardPurchaseOffer offer = null)
    {
        int buyPageIndex = -1;
        
        if (purchaseType == PurchaseFeatureData.Type.BUY_PAGE && ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
        {
            for (int i = 0; i < ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList.Count; i++)
            {
                if (packageName == ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList[i].packageName)
                {
                    PurchasablePackage fallbackPackage = PurchasablePackage.find(ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList[i].packageName);
                    creditPackage = new CreditPackage(fallbackPackage, ExperimentWrapper.FirstPurchaseOffer.firstPurchaseOffersList[i].bonusPercent, false); 
                    
                    // Using same logic from getEligiblePerks(List<FirstPurchaseOfferData>, List<CreditPackage>) to set the collectableDropKeyName 
                    creditPackage.collectableDropKeyName =  PurchaseFeatureData.BuyPage.creditPackages.Count > i ? PurchaseFeatureData.BuyPage.creditPackages[i].collectableDropKeyName : "";
                }
            }
        }
        else if (purchaseType == PurchaseFeatureData.Type.PRIZE_POP)
        {
            PurchaseFeatureData purchaseData = PurchaseFeatureData.find(purchaseType);
            for (int i = 0; i < purchaseData.prizePopPackages.Count; i++)
            {
                if (purchaseData.prizePopPackages[i].purchasePackage != null && purchaseData.prizePopPackages[i].purchasePackage.keyName == PrizePopFeature.instance.packageKey)
                {
                    creditPackage = new CreditPackage(purchaseData.prizePopPackages[i].purchasePackage, 0, false);
                }
            }
        }
        else if (purchaseType == PurchaseFeatureData.Type.NONE)
        {
            //If purchase has an unknown source just create a package from the basic purchase package data
            //Can use this to still setup some perk types
            PurchasablePackage purchasePackage = PurchasablePackage.find(packageName);
            if (purchasePackage == null)
            {
                return;
            }
            creditPackage = new CreditPackage(purchasePackage, 0, false); 
        }
        else
        {
            PurchaseFeatureData purchaseData = PurchaseFeatureData.find(purchaseType);
            if (purchaseData != null)
            {
                for (int i = 0; i < purchaseData.creditPackages.Count; i++)
                {
                    if (purchaseData.creditPackages[i].purchasePackage.keyName == packageName)
                    {
                        creditPackage = purchaseData.creditPackages[i];
                        if (purchaseType == PurchaseFeatureData.Type.BUY_PAGE)
                        {
                            buyPageIndex = i;
                        }

                        break;
                    }
                }
            }
        }


        int activePerks = 0;
        isPurchase = true;
        if (creditPackage != null)
        {
            foreach (PerkType type in System.Enum.GetValues(typeof(PerkType)))
            {
                if (type == PerkType.None)
                {
                    continue;
                }

                if (isTypeActiveForPackage(type, creditPackage, buyPageIndex, purchaseType, offer))
                {
                    activePerks++;
                    SkuResources.loadFromMegaBundleWithCallbacks(this,
                        string.Format(PERKS_ICON_PREFAB_PATH, type.ToString()), staticIconLoadSuccess, iconLoadFailed,
                        Dict.create(D.DATA, 0, D.TYPE, type, D.INDEX, buyPageIndex, D.PAYLOAD, offer));
                }
            }
        }
    }

    private void openDrawerClicked(Dict args)
    {
        if (buyPageOptionParent != null)
        {
            buyPageOptionParent.onDrawerOpen();
            string packageName = creditPackage != null && creditPackage.purchasePackage != null ? creditPackage.purchasePackage.keyName : "";
            StatsManager.Instance.LogCount(
                counterName:"dialog", 
                kingdom:statKingdom, 
                phylum:packageName,
                klass:"drawer",
                genus:"click"
                );
        }
        perksDrawer.gameObject.SetActive(true);
        perksDrawer.checkBounds();
    }

    public void closeDrawerClicked(Dict args)
    {
        if (buyPageOptionParent != null)
        {
            buyPageOptionParent.onDrawerClose();
        }
        perksDrawer.gameObject.SetActive(false);
    }

    private void iconLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject iconObj = NGUITools.AddChild(cyclerParent, obj as GameObject);
        PerkType perkType = (PerkType)args.getWithDefault(D.TYPE, PerkType.None);
        int index = (int)args.getWithDefault(D.INDEX, -1);
        PurchasePerksIcon icon = iconObj.GetComponent<PurchasePerksIcon>();
        if (icon != null)
        {
            icon.init(creditPackage, index, isPurchase, null);
        }
        
        if (perkType != PerkType.None)
        {
            loadedPerkIcons.Add(perkType, icon);
        }

        if (perksDrawer != null)
        {
            perksDrawer.addObjectToGrid(iconObj, PurchasePerksIconContainer.SwapperStates.DETACHED_WITHOUT_BACKING_STATE);
        }
        
        //Might need to adjust this, possible show all and don't do this
        if ((int)args.getWithDefault(D.DATA, -1) == 0)
        {
            currentCyclingIcon = icon;
        }

        iconObj.SetActive((int)args.getWithDefault(D.DATA, -1) == 0); //Only set the first item active from the start
    }

    private void staticIconLoadSuccess(string path, Object obj, Dict args)
    {
        int index = (int)args.getWithDefault(D.INDEX, -1);
        RewardPurchaseOffer offer = (RewardPurchaseOffer) args.getWithDefault(D.PAYLOAD, null);
        perksDrawer.addObjectToGrid(obj as GameObject, PurchasePerksIconContainer.SwapperStates.DETACHED_WITH_BACKING_STATE, creditPackage, index, isPurchase, offer);
    }

    public void swapPerks(int fromIndex, int toIndex)
    {
        if (gameObject.activeSelf && gameObject.activeInHierarchy)
        {
            StartCoroutine(fadePanels(fromIndex, toIndex));    
        }
        
    }

    private IEnumerator fadePanels(int fromIndex, int toIndex)
    {
        if (this == null || gameObject == null || perksToShow == null || perksToShow.Count == 0)
        {
            yield break;
        }

        PerkType fromPerk = perksToShow.Count > fromIndex ? perksToShow[fromIndex] : perksToShow[0];
        PerkType toPerk = perksToShow.Count > toIndex ? perksToShow[toIndex] : perksToShow[0];
        PurchasePerksIcon fromPanelObj;
        if (loadedPerkIcons.TryGetValue(fromPerk, out fromPanelObj))
        {
            StartCoroutine(CommonGameObject.fadeGameObjectTo(fromPanelObj.gameObject, 1.0f, 0.0f, 0.5f, false));
        }
        
        PurchasePerksIcon toPanelObj;
        if (loadedPerkIcons.TryGetValue(toPerk, out toPanelObj))
        {
            toPanelObj.gameObject.SetActive(true);
            currentCyclingIcon = toPanelObj;
            StartCoroutine(CommonGameObject.fadeGameObjectTo(toPanelObj.gameObject, 0.0f, 1.0f, 0.5f, false));
        }
        else
        {
            currentCyclingIcon = null;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (this == null || gameObject == null)
        {
            yield break;
        }
        
        if (fromPanelObj != null && fromPanelObj.gameObject != null)
        {
            fromPanelObj.gameObject.SetActive(false);
        }
    }

    private void iconLoadFailed(string path, Dict args)
    {
        Debug.LogWarning("Failed to load prefab at path: " + path);
    }

    public void dimCurrentIcon()
    {
        if (currentCyclingIcon != null)
        {
            currentCyclingIcon.setToDimmedVisuals();
        }
    }

    public void restoreCurrentIcon()
    {
        if (currentCyclingIcon != null)
        {
            currentCyclingIcon.setToNormalVisuals();
        }
    }

    #region Static Functions
    
    public static List<PerkType> getEligiblePerks(PurchaseFeatureData purchaseData)
    {
        if (purchaseData != null && purchaseData.creditPackages != null)
        {
            return getEligiblePerksForPackages(purchaseData.creditPackages, purchaseData.type);
        }

        return new List<PerkType>();
    }
    
    public static List<PerkType> getEligiblePerks(List<FirstPurchaseOfferData> purchaseData, List<CreditPackage> normalBuyCreditPackages)
    {
        
        if (purchaseData != null)
        {
            List<CreditPackage> fpoPackages = new List<CreditPackage>();
            for (int packageIndex = 0; packageIndex < purchaseData.Count; packageIndex++)
            {
                PurchasablePackage fpoPackage = PurchasablePackage.find(purchaseData[packageIndex].packageName);
                if (fpoPackage != null)
                {
                    CreditPackage newPackage = new CreditPackage(fpoPackage, purchaseData[packageIndex].bonusPercent, false);
                    newPackage.collectableDropKeyName = normalBuyCreditPackages.Count > packageIndex ? normalBuyCreditPackages[packageIndex].collectableDropKeyName : "";
                    fpoPackages.Add(newPackage);
                    
                }
            }
            return getEligiblePerksForPackages(fpoPackages);
        }

        return new List<PerkType>();
    }

    public static List<PerkType> getEligiblePerksForPackages(List<CreditPackage> packages, PurchaseFeatureData.Type purchaseType = PurchaseFeatureData.Type.NONE)
    {
        List<PerkType> results = new List<PerkType>();
        string[] possiblePerks = ExperimentWrapper.BuyPageDrawer.priorityList;

        for (int i = 0; i < possiblePerks.Length; i++)
        {
            PerkType typeToCheck = getTypeFromString(possiblePerks[i]);
            
            if (typeToCheck == PerkType.Vip_Points)
            {
                results.Add(typeToCheck);
                continue;
            }

            if (typeToCheck != PerkType.None && packages != null)
            {
                for (int packageIndex = 0; packageIndex < packages.Count; packageIndex++)
                {
                    if (isTypeActiveForPackage(typeToCheck, packages[packageIndex], packageIndex, purchaseType))
                    {
                        results.Add(typeToCheck);
                        break; //Only need to add to the list once for all the packages in the PurchaseFeatureData
                    }
                }
            }
        }
        
        return results;
    }
    
    public static List<PerkType> getEligiblePerksForPackage(CreditPackage package, int packageIndex)
    {
        List<PerkType> results = new List<PerkType>();
        string[] possiblePerks = ExperimentWrapper.BuyPageDrawer.priorityList;

        for (int i = 0; i < possiblePerks.Length; i++)
        {
            PerkType typeToCheck = getTypeFromString(possiblePerks[i]);
            
            if (typeToCheck == PerkType.Vip_Points)
            {
                results.Add(typeToCheck);
                continue;
            }

            if (typeToCheck != PerkType.None && isTypeActiveForPackage(typeToCheck, package, packageIndex, PurchaseFeatureData.Type.NONE))
            {
                results.Add(typeToCheck);
            }
        }
        
        return results;
    }

    private static bool isTypeActiveForPackage(PerkType type, CreditPackage package, int packageIndex, PurchaseFeatureData.Type purchaseType, RewardPurchaseOffer purchaseRewardable = null)
    {
        if (package == null)
        {
            return false;
        }
        
        switch (type)
        {
            case PerkType.Card_Pack:
                return Collectables.isActive() && 
                       ((!string.IsNullOrEmpty(package.collectableDropKeyName) && package.collectableDropKeyName != "nothing" && !CollectablePack.isWildCardPack(package.collectableDropKeyName)) || 
                        (purchaseRewardable != null && !string.IsNullOrEmpty(purchaseRewardable.cardPackKey) && purchaseRewardable.cardPackKey != "nothing" && !CollectablePack.isWildCardPack(purchaseRewardable.cardPackKey)));
            
            case PerkType.Elite_Gift:
                return EliteManager.isActive && EliteManager.hasActivePass && !EliteManager.showLobbyTransition;
            
            case PerkType.Elite_Points:
                int points = package.purchasePackage.priceTier * EliteManager.elitePointsPerDollar;
                return EliteManager.isActive && points > 0;
            
            case PerkType.Pinata:
                PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
                if (campaign != null && campaign.isEarlyEndActive && purchaseType == PurchaseFeatureData.Type.BUY_PAGE)
                {
                    int bonusAmount = campaign.getPostPurchaseChallengeBonus(packageIndex);
                    return bonusAmount > 0;
                }
                break;
            
            case PerkType.Power_Ups:
                //TODO implement once packages are setup to handle powerups
                return false;
            
            case PerkType.Vip_Points:
                if (package != null && package.purchasePackage != null)
                {
                    return package.purchasePackage.vipPoints() > 0;
                }
                else
                {
                    return false;
                }
            case PerkType.PrizePop_Extras:
                return PrizePopFeature.instance != null && package.purchasePackage == PrizePopFeature.instance.getCurrentPackage();
            case PerkType.XP_Boost_Triple:
                bool showLottoBlast = ExperimentWrapper.LevelLotto.isInExperiment 
                                      && ExperimentWrapper.LevelLotto.showBuffOnPackages != null
                                      && FeatureOrchestrator.Orchestrator.activeFeaturesToDisplay.Contains(ExperimentWrapper.LevelLotto.experimentName)
                                      && purchaseType != PurchaseFeatureData.Type.POPCORN_SALE;
                
                if (showLottoBlast && packageIndex >= 0 && ExperimentWrapper.LevelLotto.showBuffOnPackages != null && ExperimentWrapper.LevelLotto.showBuffOnPackages.Length > packageIndex)
                {
                    return ExperimentWrapper.LevelLotto.showBuffOnPackages[packageIndex];
                }

                break;
            case PerkType.Pet_Treat:
                return VirtualPetsFeature.instance != null && 
                       VirtualPetsFeature.instance.isEnabled && 
                       VirtualPetsFeature.instance.isPackageEligibleForTreat(package);
            
            case PerkType.Casino_Empire_Dice:
                //Testing
                return purchaseRewardable != null && purchaseRewardable.boardGameDice > 0;
        }
        return false;
    }

    private static PerkType getTypeFromString(string type)
    {
        PerkType result;
        if (typeMap.TryGetValue(type, out result))
        {
            return result;
        }
        
#if UNITY_EDITOR
        Debug.LogWarningFormat("Buy Page Drawer Perk Type {0} not available", type);
#endif
        return PerkType.None;
    }
    
    #endregion
}
