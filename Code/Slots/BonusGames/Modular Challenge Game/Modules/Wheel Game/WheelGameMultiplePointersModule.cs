using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;

public class WheelGameMultiplePointersModule : WheelGameModule
{
    [SerializeField] private AnimationListController.AnimationInformationList wheelPointerAnimations;
    [SerializeField] private float showPointerDelay;
    [SerializeField] private float movePointerToWheelDelay;

    [Header("Animation and Data")]
    [SerializeField] private AnimationAndAudioSet pointerOnSet;
    [SerializeField] private AnimationAndAudioSet movedPointerToWheelSet;
    [SerializeField] private AnimationAndAudioSet pointerRoundEndSet;
    [SerializeField] private AnimationAndAudioSet movedPointerToWheelOffSet;

    [System.Serializable]
    public class AnimationAndAudioSet
    {
        public string animationName;
        public float animationDelay;
        public AudioListController.AudioInformationList audioInfos;
    }
    
    // Enable round init override
    public override bool needsToExecuteOnRoundInit()
    {
        return true;
    }

    public override bool needsToExecuteOnRoundStart()
    {
        return true;
    }

    public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
    {
        return true;
    }

    public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
    {
        if (string.IsNullOrEmpty(movedPointerToWheelOffSet.animationName))
        {
            yield break;
        }
        
        for (int i = 0; i < wheelPointerAnimations.Count; ++i)
        {
            //pointer has been moved to the wheel and is visible, so let's turn it off at round end
            if (wheelPointerAnimations.animInfoList[i].ANIMATION_NAME == movedPointerToWheelSet.animationName)
            {
                wheelPointerAnimations.animInfoList[i].ANIMATION_NAME = movedPointerToWheelOffSet.animationName;

                yield return StartCoroutine(AudioListController.playListOfAudioInformation(movedPointerToWheelSet.audioInfos));

                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wheelPointerAnimations));
            }
        }
    }

    public override IEnumerator executeOnRoundStart()
    {
        string gameName = wheelRoundVariantParent.getVariantGameDataName();
			
        SlotOutcome bonusGameSlotOutcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, gameName);
			
        int additionalPointersCount = -1;
        ReadOnlyCollection<SlotOutcome> outcomes = bonusGameSlotOutcome.getSubOutcomesReadOnly();
        foreach (SlotOutcome slotOutcome in outcomes)
        {
            additionalPointersCount = slotOutcome.getOverrideCredits() > 0 ? additionalPointersCount + 1 : additionalPointersCount;
        }

        if (additionalPointersCount < 0)
        {
            Debug.LogError("Error occured while processing slot outcomes for multiple wheel pointers!");
            yield break;
        }
 
        if (showPointerDelay > 0)
        {
            yield return new WaitForSeconds(showPointerDelay);
        }
        
        for (int i = 0; i < wheelPointerAnimations.Count; ++i)
        {
            //don't turn on pointers that have already been disabled because they weren't picked
            if (wheelPointerAnimations.animInfoList[i].ANIMATION_NAME == pointerRoundEndSet.animationName)
            {
                continue;
            }
            
            AnimationAndAudioSet animAudioSet = i < additionalPointersCount ? pointerOnSet : pointerRoundEndSet;

            yield return StartCoroutine(AudioListController.playListOfAudioInformation(animAudioSet.audioInfos));

            wheelPointerAnimations.animInfoList[i].ANIMATION_NAME = animAudioSet.animationName;

            if (animAudioSet.animationDelay > 0)
            {
                yield return new WaitForSeconds(animAudioSet.animationDelay);
            }
            
            yield return StartCoroutine(AnimationListController.playAnimationInformation(wheelPointerAnimations.animInfoList[i]));
        }

        if (movePointerToWheelDelay > 0)
        {
            yield return new WaitForSeconds(movePointerToWheelDelay);
        }
            
        //animate the move to wheel part of the pointers
        for (int i = 0; i < wheelPointerAnimations.Count; ++i)
        {
            if (wheelPointerAnimations.animInfoList[i].ANIMATION_NAME != pointerOnSet.animationName)
            {
                continue;
            }

            wheelPointerAnimations.animInfoList[i].ANIMATION_NAME = movedPointerToWheelSet.animationName;

            yield return StartCoroutine(AudioListController.playListOfAudioInformation(movedPointerToWheelSet.audioInfos));
            
            if (movedPointerToWheelSet.animationDelay > 0)
            {
                yield return new WaitForSeconds(movedPointerToWheelSet.animationDelay);
            }
            
            yield return StartCoroutine(AnimationListController.playAnimationInformation(wheelPointerAnimations.animInfoList[i]));
        }
    }
}
