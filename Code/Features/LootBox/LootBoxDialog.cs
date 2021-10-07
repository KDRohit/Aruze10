using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using TMPro;
using Com.Rewardables;
using Com.States;
using Zynga.Core.Util;

public class LootBoxDialog : DialogBase
{
    [SerializeField]
    private Transform chestContainerTransform;
    [SerializeField]
    private Transform gridTransform;
    [SerializeField]
    private UICenteredGrid centeredGridScript;
    [Header("Prefabs")]
    [SerializeField] 
    private GameObject bronzeChestPrefab;
    [SerializeField] 
    private GameObject commonChestPrefab;
    [SerializeField] 
    private GameObject epicChestPrefab;
    [SerializeField]
    private GameObject goldChestPrefab;
    [SerializeField]
    private GameObject silverChestPrefab;
    [SerializeField]
    private GameObject awardCardItemPrefab;
    [Header("Animations")]
    [SerializeField]
    private AnimationListController.AnimationInformationList introAnimList;
    [SerializeField]
    private AnimationListController.AnimationInformationList outroAnimList;
    [SerializeField]
    private Animator dialogAnimator;
    [SerializeField]
    private float TIME_BETWEEN_AWARD_ITEMS = 0.04f;
    [SerializeField]
    private float TIME_BEFORE_AWARD_ITEMS = 3.2f;
    [Header("Arrows")]
    [SerializeField]
    private GameObject leftPageArrow;
    [SerializeField]
    private GameObject rightPageArrow;
    [Header("Header Texts")]
    [SerializeField] 
    private MultiLabelWrapperComponent headerText;
    [SerializeField]
    private MultiLabelWrapperComponent headerSubText;
    [Header("Misc")]
    [SerializeField]
    private TextMeshPro tmpCoinAward;

    private const string CONGRATS_TEXT = "loot_box_congrats_{0}";
    private const string HEADER_TEXT = "loot_box_header";
    
    public override void init()
    {
        if (closeButtonHandler != null)
        {
            closeButtonHandler.registerEventDelegate(onCloseButtonClicked);
        }
        
        // Get parameters
        if (dialogArgs == null)
        {
            Dialog.close(this);
        }

        LootBoxRewardBundle rewardLootBox = dialogArgs.getWithDefault(D.OPTION, null) as LootBoxRewardBundle;
        
        if (rewardLootBox == null || rewardLootBox.LootBoxRewardItems == null || 
            rewardLootBox.LootBoxRewardItems.IsEmpty() || string.IsNullOrEmpty(rewardLootBox.Rarity))
        {
            Dialog.close(this);
        }
        
        displayItems(rewardLootBox);
        
        
        // Log stat
        StatsManager.Instance.LogCount(
            counterName: "lootbox",
            kingdom: rewardLootBox.Rarity,
            genus: TRACK_CLASS_VIEW);
    }

    private void displayItems(LootBoxRewardBundle rewardLootBox)
    {
        string rarity = rewardLootBox.Rarity;

        if (headerSubText != null && headerText != null)
        {
            headerText.text = rarity + " " + Localize.text(HEADER_TEXT);
            // Hide header sub text based on UI/UX suggestion.
            headerSubText.gameObject.SetActive(false);
        }

        GameObject goLootBoxChest = null;

        switch (rarity.ToLower())
        {
            case "gold":
                goLootBoxChest = (GameObject) CommonGameObject.instantiate(goldChestPrefab, chestContainerTransform);
                break;
            case "silver":
                goLootBoxChest = (GameObject) CommonGameObject.instantiate(silverChestPrefab, chestContainerTransform);
                break;
            case "bronze":
                goLootBoxChest = (GameObject) CommonGameObject.instantiate(bronzeChestPrefab, chestContainerTransform);
                break;
            case "epic":
                goLootBoxChest = (GameObject) CommonGameObject.instantiate(epicChestPrefab, chestContainerTransform);
                break;
            default:
                goLootBoxChest = (GameObject) CommonGameObject.instantiate(commonChestPrefab, chestContainerTransform);
                break;
        }

        List<LootBoxRewardItem> lootBoxRewardItems = rewardLootBox.LootBoxRewardItems;
        // Display award items
        PrizeChestAwardCardItem[] awardItemScripts = new PrizeChestAwardCardItem[lootBoxRewardItems.Count];
        for (int i = 0; i < lootBoxRewardItems.Count; i++)
        {
            if (lootBoxRewardItems[i].rewardItemType == LootBoxRewardItem.LootRewardItemType.coin)
            {
                tmpCoinAward.SetText(CreditsEconomy.convertCredits(lootBoxRewardItems[i].addedValue));
            }
            else
            {
                GameObject goItem = (GameObject)CommonGameObject.instantiate(awardCardItemPrefab, gridTransform);
                awardItemScripts[i] = goItem.GetComponent<PrizeChestAwardCardItem>();
                if (awardItemScripts[i] != null)
                {
                    switch (lootBoxRewardItems[i].rewardItemType)
                    {
                        case LootBoxRewardItem.LootRewardItemType.cardPack:
                            awardItemScripts[i].setIcon(PrizeChestAwardCardItem.PrizeChestAwardCardItemTypes.cardPack, lootBoxRewardItems[i].keyName, lootBoxRewardItems[i].description);
                            break;
                        case LootBoxRewardItem.LootRewardItemType.elite:
                            awardItemScripts[i].setIcon(PrizeChestAwardCardItem.PrizeChestAwardCardItemTypes.elite, "", lootBoxRewardItems[i].description);
                            break;
                        case LootBoxRewardItem.LootRewardItemType.powerup:
                            awardItemScripts[i].setIcon(PrizeChestAwardCardItem.PrizeChestAwardCardItemTypes.powerup, lootBoxRewardItems[i].keyName, lootBoxRewardItems[i].description);
                            break;
                        case LootBoxRewardItem.LootRewardItemType.richPass:
                            awardItemScripts[i].setIcon(PrizeChestAwardCardItem.PrizeChestAwardCardItemTypes.richPass, "", lootBoxRewardItems[i].description);
                            break;
                        case LootBoxRewardItem.LootRewardItemType.pets:
                            awardItemScripts[i].setIcon(PrizeChestAwardCardItem.PrizeChestAwardCardItemTypes.pets, "", lootBoxRewardItems[i].description);
                            break;
                    }

                }
            }
        }
        
        centeredGridScript.reposition();
        removePageArrows();
        
        StartCoroutine(startIntroAnimations(goLootBoxChest, awardItemScripts));
    }
    
    public override void close()
    {
        
    }

    private void removePageArrows()
    {
        leftPageArrow.SetActive(false);
        rightPageArrow.SetActive(false);
    }
    
    private IEnumerator startIntroAnimations(GameObject goLootBoxChest, PrizeChestAwardCardItem[] awardItemScripts)
    {
        if (goLootBoxChest == null)
        {
            yield break;
        }

        LootBoxChest lootBoxChest = goLootBoxChest.GetComponent<LootBoxChest>();

        // Play intro animation for this dialog itself
        StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimList));

        // Then play Chest animation when chest is enabled.
        for (int i = 0; i < 10; i++) //Wait a few frames for an animation to enable the chest object
        {
            if (goLootBoxChest.activeInHierarchy)
            {
                break;
            }
            else
            {
                yield return null;
            }
        }

        if (lootBoxChest != null && goLootBoxChest.activeInHierarchy)
        {
            lootBoxChest.startIntroAnimation();

            yield return new WaitForSeconds(TIME_BEFORE_AWARD_ITEMS);
            
            // After a while, play other award animations.
            for (int i = 0; i < awardItemScripts.Length; i++)
            {
                yield return new WaitForSeconds(TIME_BETWEEN_AWARD_ITEMS);
                if (awardItemScripts[i] != null)
                {
                    awardItemScripts[i].startIntroAnimation();
                }
            }
        }
    }
    
    public IEnumerator playDialogOutro()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimList));
        Dialog.close(this);
    }

    private void updateOutroState(string state)
    {
        StartCoroutine(playDialogOutro());
    }
    
    public override void onCloseButtonClicked(Dict args = null)
    {
        updateOutroState("chest_outro");
    }
    
    public void OnDestroy()
    {
        // Unregister button callbacks
        if (closeButtonHandler != null)
        {
            closeButtonHandler.unregisterEventDelegate(onCloseButtonClicked);
        }
    }
    
    public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
    {
        Scheduler.addDialog("loot_box_dialog", args, priority);
    }
}
