using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module to play a specific AnimationList on a round start based on the pick game award value
 *
 * Author: Xueer Zhu
 * Date: July 1st, 2021
 */
public class ChallengeGameAnimateOnStartRoundByPickAwardValueModule : ChallengeGameAnimateOnStartRoundModule
{
    [SerializeField] private List<PickRewardAnimationInfoList> transitionByPickAward;
    [SerializeField] private ModularPickingGameVariant pickingVariantParent;
    
    // Executes the defined animation list on round start
    public override IEnumerator executeOnRoundStart()
    {
        PickRewardTypeEnum pickRewardType = PickRewardTypeEnum.PickOne;
        List<ModularChallengeGameOutcomeEntry> outcome = pickingVariantParent.getPickOutcomeList(0);
        switch (outcome.Count)
        {
            case 1:
                pickRewardType = PickRewardTypeEnum.PickOne;
                break;
            case 2:
                pickRewardType = PickRewardTypeEnum.PickTwo;
                break;
            case 3:
                pickRewardType = PickRewardTypeEnum.PickThree;
                break;
            default:
                pickRewardType = PickRewardTypeEnum.Blackout;
                break;
        }
        
        StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInformation));
        yield return StartCoroutine(
            AnimationListController.playListOfAnimationInformation(
                getAnimationInfoForPickAwardValueInList(pickRewardType, transitionByPickAward)));
    }
    
    private AnimationListController.AnimationInformationList getAnimationInfoForPickAwardValueInList(PickRewardTypeEnum pickRewardType, List<PickRewardAnimationInfoList> targetList)
    {
        PickRewardAnimationInfoList matchedList = targetList.Find(pickAnimation => (pickAnimation.pickRewardType == pickRewardType));
        if (matchedList != null)
        {
            return matchedList.animationInfoList;
        }
        
        return null;
    }
    
    [System.Serializable]
    public class PickRewardAnimationInfoList
    {
        public PickRewardTypeEnum pickRewardType;
        public AnimationListController.AnimationInformationList animationInfoList;
    }
    
    public enum PickRewardTypeEnum
    {
        PickOne,
        PickTwo,
        PickThree,
        Blackout
    }
}
