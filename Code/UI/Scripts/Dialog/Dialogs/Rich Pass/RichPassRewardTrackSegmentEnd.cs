using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RichPassRewardTrackSegmentEnd : RichPassRewardTrackSegment
{
    [SerializeField] private Transform passCompleteParent;

    private SlideController parentSlider;
    private RichPassPassCompleteBase completeComponent;

    private const string PASS_COMPLETE_NO_REWARDS_OBJ_PATH = "Features/Rich Pass/Prefabs/Instanced Prefabs/Path Assets/Path Section Item 03 - Pass Complete"; //Standard Pass Complete Animation
    private const string PASS_COMPLETE_REPEAT_CHEST_OBJ_PATH = "Features/Rich Pass/Prefabs/Instanced Prefabs/Path Assets/Path Section Item 03 - Final Chest"; //Special pass complete chest for extra rewards
    private bool hasRepeatableChest = false;
    public override void init(long requiredPoints, long currentPlayerPoints, long nextRewardPoints, RichPassCampaign.RewardTrack silverTrack, List<PassReward> silverRewards, RichPassCampaign.RewardTrack goldTrack, List<PassReward> goldRewards, SlideController slideController)
    {
        parentSlider = slideController;
        base.init(requiredPoints, currentPlayerPoints, nextRewardPoints, silverTrack, silverRewards, goldTrack, goldRewards, parentSlider);
        if (currentPlayerPoints >= requiredPoints)
        {
            loadPassCompleteObject();
        }
    }

    public void loadPassCompleteObject()
    {
        long repeatableChestPointsRequirement = CampaignDirector.richPass.getCurrentRepeatableChestRequirement();
        hasRepeatableChest = CampaignDirector.richPass.maxRepeatableRewards > 0 && repeatableChestPointsRequirement > 0 && CampaignDirector.richPass.goldTrack != null;
        hasGoldRewards = hasGoldRewards || hasRepeatableChest;
        AssetBundleManager.load(this, hasRepeatableChest ? PASS_COMPLETE_REPEAT_CHEST_OBJ_PATH : PASS_COMPLETE_NO_REWARDS_OBJ_PATH, onPassCompleteLoadSuccess, onPassCompleteLoadFailed, null, false, true, ".prefab");
    }

    private void onPassCompleteLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        GameObject passCompleteObj = NGUITools.AddChild(passCompleteParent, obj as GameObject);
        completeComponent = passCompleteObj.GetComponent<RichPassPassCompleteBase>();
        completeComponent.init(parentSlider, this);
    }
    
    private void onPassCompleteLoadFailed(string assetPath, Dict data = null)
    {
        //Harmless if failing for the non-reward version
        Debug.LogWarning("Pass Complete object failed to load: " + assetPath);
    }

    public override void unlockGoldRewards()
    {
        base.unlockGoldRewards();
        if (hasRepeatableChest && completeComponent != null)
        {
            completeComponent.unlock();
        }
    }
}
