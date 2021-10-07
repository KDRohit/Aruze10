using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Module to link a list of animations for the animated wheel to a winId to trigger on win.
// On spin, the animation from the wheelSliceAnimations is looked up from the winID and played
//
// Author : Shaun Peoples <speoples@zynga.com>
// Date : Sept 27th, 2019
//
// games : bettie02
public class WheelGameMapIdsToSpinAnimations : WheelGameModule 
{
    [SerializeField] private List<AnimationListController.AnimationInformationList> wheelSliceAnimations;
    private Dictionary<long, int> winIDtoAnimationIndexMap;
    private AnimationListController.AnimationInformationList currentSpinAnimations; 

    // Enable round init override
    public override bool needsToExecuteOnRoundInit()
    {
        return true;
    }

    // Executes on round init & populate the wheel values
    public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
    {
        base.executeOnRoundInit(roundParent, wheel);
        mapWinIdsToAnimations();
    }

    private void mapWinIdsToAnimations()
    {
        // generate an ordered outcome list from the wins & leftovers
        List<ModularChallengeGameOutcomeEntry> wheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);
        winIDtoAnimationIndexMap = new Dictionary<long, int>();

        if (wheelEntryList.Count != wheelSliceAnimations.Count)
        {
            Debug.LogErrorFormat("wheelEntryList ({0}) from data != wheelSliceAnimations ({1}) set in prefab", wheelEntryList.Count, wheelSliceAnimations.Count);
        }

        for (int i = 0; i < wheelSliceAnimations.Count; i++)
        {
            if (wheelSliceAnimations[i] != null)
            {
                winIDtoAnimationIndexMap.Add(wheelEntryList[i].winID, i);
            }
        }
    }

    private AnimationListController.AnimationInformationList GetAnimationsForWinID(long winID)
    {
        int index = winIDtoAnimationIndexMap[winID];
        return wheelSliceAnimations[index];
    }
    
    public override bool needsToExecuteOnSpin()
    {
        //get the outcome and only execute on spin if we have a set of animations for this winID
        WheelOutcome wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
        WheelPick entry = wheelOutcome.lookAtNextEntry();
        currentSpinAnimations = GetAnimationsForWinID(entry.winID);
         
        return currentSpinAnimations != null;
    }

    public override IEnumerator executeOnSpin()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentSpinAnimations));
    }
}

