using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Maps animations to groupId data from wheel paytable entries id
 */
public class WheelGameInitSlicesFromDataModule :  WheelGameModule
{
    [SerializeField] private List<AnimationListController.AnimationInformationList> wheelSliceAnimations;
    
    private Dictionary<long, int> winIDtoAnimationIndexMap;
    private AnimationListController.AnimationInformationList currentSpinAnimations; 
    
    [SerializeField] private List<NameToAnimationMapping> nameToAnimationMappings;
    private Dictionary<string, string> nameToAnimationMap;
    
    [System.Serializable]
    public class NameToAnimationMapping
    {
        public string nameId;
        public string animationName;
    }
   
    // Enable round init override
    public override bool needsToExecuteOnRoundInit()
    {
        return true;
    }

    // Executes on round init & populate the wheel values
    public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
    {
        base.executeOnRoundInit(roundParent, wheel);
        
        setupWheelSlices();
    }
    
    private void setupWheelSlices()
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

    private AnimationListController.AnimationInformationList getAnimationsForWinID(long winId)
    {
        int index = winIDtoAnimationIndexMap[winId];
        return wheelSliceAnimations[index];
    }
    
    public override bool needsToExecuteOnSpin()
    {
        //get the outcome and only execute on spin if we have a set of animations for this winID
        WheelOutcome wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
        WheelPick entry = wheelOutcome.lookAtNextEntry();
        currentSpinAnimations = getAnimationsForWinID(entry.winID);
         
        return currentSpinAnimations != null;
    }

    public override IEnumerator executeOnSpin()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentSpinAnimations));
    }
}

