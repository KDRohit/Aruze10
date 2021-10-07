using System.Collections;
using Com.Scheduler;
using UnityEngine;

public class RichPassRewardIconChest : RichPassRewardIcon
{
    [SerializeField] private ButtonHandler collectButton;
    [SerializeField] private Transform chestParent;
    [SerializeField] private AnimationListController.AnimationInformationList collectButtonAnim; //Play these anims after chest plays open animation

    private CommonAnimatedTieredChest collectedChest;

    private long creditsAmount = 0;
    private string cardPackId = "";
    private bool chestFailed = false;
    private bool chestIntroFinished = false;

    private JSON cardData;

    public override IEnumerator playAnticipations()
    {
        yield return StartCoroutine(base.playAnticipations());
        parentSlideController.preventScrolling();
        CommonAnimatedTieredChest.loadChest(reward.rarity, chestLoadSuccess, chestLoadFailed);
    }
    
    private void chestLoadFailed(string assetPath, Dict data = null)
    {
        chestFailed = true;
        Debug.LogWarning("Failed to load chest: " + assetPath);
    }

    private void chestLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        if (chestParent == null || chestParent.gameObject == null)
        {
            return;
        }
        
        SafeSet.gameObjectActive(chestParent.gameObject, true);
        GameObject chestObj = NGUITools.AddChild(chestParent, obj as GameObject);
        if (chestObj == null)
        {
            Debug.LogError("Unable to add child object");
            return;
        }
        
        collectedChest = chestObj.GetComponent<CommonAnimatedTieredChest>();
        if (collectedChest != null)
        {
            collectedChest.initChest("", "", creditsAmount, !string.IsNullOrEmpty(cardPackId));
            StartCoroutine(playChestIntro());            
        }
        else
        {
            Debug.LogWarning("Invalid chest object -- missing animatedTieredChest Script");
        }

    }
    
    private IEnumerator playChestIntro()
    {
        chestIntroFinished = true;
        yield return StartCoroutine(collectedChest.playChestIntro());
    }
    
    public override void rewardClaimSuccess(JSON data)
    {
        if (data == null)
        {
            Debug.LogError("invalid reward data");
            parentSlideController.enableScrolling();
            return;
        }
        
        JSON[] chestContentsData = data.getJsonArray("rewardables");
        for (int i = 0; i < chestContentsData.Length; i++)
        {
            if (chestContentsData[i] == null)
            {
                Debug.LogError("invalid chest item at index: " + i);
                continue;
            }
            
            string type = chestContentsData[i].getString("reward_type", "");
            switch (type)
            {
                case "coin":
                    creditsAmount += chestContentsData[i].getLong("value", 0);
                    break;
                case "collectible_pack":
                    cardPackId = chestContentsData[i].getString("pack_key", "");
                    break;
                default:
                    Debug.LogWarning("Unexpected chest reward: " + type);
                    break;
            }
        }

        //Re-init the chest with the actual info from the server once we have it
        if (collectedChest != null)
        {
            collectedChest.initChest("", "", creditsAmount, !string.IsNullOrEmpty(cardPackId), cardPackId);
        }

        StartCoroutine(waitToOpenChest());
    }
    
    private void collectClicked(Dict args = null)
    {
        Audio.play("CollectCoinsChestRichPass");
        SlotsPlayer.addNonpendingFeatureCredits(creditsAmount, "richPassChest");
        creditsAmount = 0;
        StartCoroutine(playChestOutro());
    }
    
    private IEnumerator playChestOutro()
    {
        //Do rollup/add credits to player and queue card pack
        yield return StartCoroutine(collectedChest.coinParticle.animateParticleEffect());
        Audio.play("ChestExitRichPass");
        yield return StartCoroutine(collectedChest.playChestOutro());

        closeChestOverlay();
    }

    private void closeChestOverlay()
    {
        chestParent.gameObject.SetActive(false);
        if (collectedChest != null)
        {
            Destroy(collectedChest.gameObject);
        }
        parentSlideController.enableScrolling();
        if (cardData != null)
        {
            Collectables.claimPackDropNow(cardData, SchedulerPriority.PriorityType.IMMEDIATE);
        }
    }
    

    private IEnumerator waitToOpenChest()
    {
        while (!chestIntroFinished && !chestFailed)
        {
            //Need to wait for the chest to actually load & play its intro 
            yield return null;
        }
        
        if (!chestFailed)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(collectButtonAnim));
            yield return StartCoroutine(collectedChest.playOpenChest());
            collectButton.registerEventDelegate(collectClicked);
        }
        else
        {
            //Award the coins if the chest failed to load to prevent a desync
            SlotsPlayer.addNonpendingFeatureCredits(creditsAmount, "richPassChestLoadFailed");
            creditsAmount = 0;
            parentSlideController.enableScrolling();
        }
    }

    public override void rewardClaimFailed()
    {
        //Close instantly if the claim failed for some reason
        closeChestOverlay();
    }

    public override void packDropRecieved(JSON data)
    {
        if (Collectables.isActive())
        {
            Collectables.Instance.unRegisterForPackDrop(packDropRecieved, "rich_pass");
        }
        cardData = data;
    }
}