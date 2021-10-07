using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for playing animation that correlated to a mapped groupId value and animation name
 * sequentially through the AnimationInformationList
 *
 * Used By: billions02
 */
public class PickingGamePlayAnimationMatchingPickedGroupIdInSequence : PickingGameRevealModule
{
    [SerializeField] protected AnimationListController.AnimationInformationList pickTriggeredAnimations;
    [SerializeField] protected List<LabelWrapperComponent> labels; 
    private int pickMatchIndex;

    [SerializeField] private List<GroupIdAnimationName> groupIdToAnimationNames;
    private Dictionary<string, GroupIdAnimationName> groupIdToPickDataLookup;
    private List<string> incrementingIds = new List<string>();

    [System.Serializable]
    private class GroupIdAnimationName
    {
        public string groupId;
        public string animationName;
        public string animationGrayName;
    }

    public override bool needsToExecuteOnRoundInit()
    {
        return pickTriggeredAnimations.Count > 0;
    }

    public override void executeOnRoundInit(ModularChallengeGameVariant round)
    {
        pickMatchIndex = 0;
        
        groupIdToPickDataLookup = new Dictionary<string, GroupIdAnimationName>();
        foreach (var v in groupIdToAnimationNames)
        {
            groupIdToPickDataLookup.Add(v.groupId, v);
        }
        
        base.executeOnRoundInit(round);
    }

    public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
    {
        if (pickMatchIndex >= pickTriggeredAnimations.Count)
        {
            return false;
        }
    
        if (pickTriggeredAnimations.animInfoList[pickMatchIndex] != null && groupIdToPickDataLookup.ContainsKey(pickData.groupId))
        {
            pickTriggeredAnimations.animInfoList[pickMatchIndex].ANIMATION_NAME = groupIdToPickDataLookup[pickData.groupId].animationName;
        }

        bool alreadyUsed = incrementingIds.Contains(pickData.groupId);
        if (!alreadyUsed)
        {
            incrementingIds.Add(pickData.groupId);
        }

        ModularChallengeGameOutcomeEntry pickOutcome = pickingVariantParent.getCurrentPickOutcome();

        if (labels != null && pickMatchIndex < labels.Count)
        {
            labels[pickMatchIndex].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(pickOutcome.credits, decimalPoints: 2, shouldRoundUp: false);
        }
        
        return !alreadyUsed && groupIdToPickDataLookup.ContainsKey(pickData.groupId) && pickTriggeredAnimations.animInfoList.Count > 0 && pickTriggeredAnimations.animInfoList[pickMatchIndex] != null;
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        if (pickMatchIndex >= pickTriggeredAnimations.Count)
        {
            Debug.LogError("Animation on pickTriggeredAnimations is out of range for index: " + pickMatchIndex);
            yield break;
        }
        
        AnimationListController.AnimationInformation animInfo = pickTriggeredAnimations.animInfoList[pickMatchIndex];
        pickMatchIndex++;
        
        yield return StartCoroutine(AnimationListController.playAnimationInformation(animInfo));
    }
    
    public override bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
    {
        return pickMatchIndex < pickTriggeredAnimations.Count;
    }
    
    public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
    {
        if (pickMatchIndex >= pickTriggeredAnimations.Count)
        {
            Debug.LogError("Animation on pickTriggeredAnimations is out of range for index: " + pickMatchIndex);
            yield break;
        }
        
        if (pickTriggeredAnimations.animInfoList[pickMatchIndex] != null)
        {
            pickTriggeredAnimations.animInfoList[pickMatchIndex].ANIMATION_NAME = groupIdToAnimationNames[pickMatchIndex].animationGrayName;
        }

        AnimationListController.AnimationInformation animInfo = pickTriggeredAnimations.animInfoList[pickMatchIndex];
        pickMatchIndex++;
        
        yield return StartCoroutine(AnimationListController.playAnimationInformation(animInfo));
    }
}
