using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Module to link a list of animations for the animated wheel to a winId to trigger on win.
// On spin End, the animation from the wheelSliceAnimations is looked up from the winID and played
// Also depending on bonus wheel type the final win lables are populated here.
// Author : Punit Dabas
//
// games : aruze05
public class WheelGameEndSequence : WheelGameModule
{
    [SerializeField] private List<AnimationListController.AnimationInformationList> wheelSliceAnimations;
    [SerializeField] private LabelWrapperComponent multiplierText;
    [SerializeField] private LabelWrapperComponent winText;

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

    private AnimationListController.AnimationInformationList getAnimationsForWinID(long winID)
    {
        int index = winIDtoAnimationIndexMap[winID];
        return wheelSliceAnimations[index];
    }

    public override bool needsToExecuteOnSpinComplete()
    {
        ////get the outcome and only execute on spin if we have a set of animations for this winID
        ModularChallengeGameOutcomeEntry entry = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).lookAtNextEntry();
        currentSpinAnimations = getAnimationsForWinID(entry.winID);
        if(entry.multiplier > 0)
        {
            long basePay = SlotBaseGame.instance.outcomeDisplayController.calculateBasePayout(ReelGame.activeGame.outcome);
            long win =  basePay * (entry.multiplier-1);
            BonusGamePresenter.instance.currentPayout += win;
            
            if(multiplierText != null && winText != null)
            {
                multiplierText.text = CommonText.formatNumber(entry.multiplier) + " X " + (CreditsEconomy.convertCredits(basePay));
                winText.text = CreditsEconomy.convertCredits(basePay * entry.multiplier);
            }
        }
        else if(entry.credits > 0)
        {
            long win = entry.credits ;

            if(winText != null)
            {
                winText.text = CreditsEconomy.convertCredits(win);
            }
            BonusGamePresenter.instance.currentPayout += win;
        }
        else if(ReelGame.activeGame.hasFreespinGameStarted)
        {
            int numberOfFreeSpins = entry.wheelPick.spins;
            ReelGame.activeGame.numberOfFreespinsRemaining += numberOfFreeSpins;
        }
        return currentSpinAnimations != null;
    }

    public override IEnumerator executeOnSpinComplete()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentSpinAnimations));
    }
}

