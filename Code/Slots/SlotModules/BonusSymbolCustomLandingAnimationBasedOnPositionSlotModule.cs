using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays a custom animation based off the reel / row that a specific symbol lands on.
public class BonusSymbolCustomLandingAnimationBasedOnPositionSlotModule : SlotModule
{
    [Tooltip("Symbol for which animation is played.")]
    [SerializeField] private string symbolServerName = "WD";
    [Tooltip("Animations to play on reel end rollback")]
    [SerializeField] public List<LandingAnimations> landingAnimationList ;
    [SerializeField] private bool shouldBlockNextSpin = false;

    [System.Serializable]public class LandingAnimations
    {
        public int reel;
        public int row;
        public AnimationListController.AnimationInformationList landingAnimations;
    }

    public override bool needsToExecuteOnReelEndRollback(SlotReel stoppingReel)
    {
        SlotSymbol[] finalSymbols = stoppingReel.visibleSymbols;
        for (int i = 0; i < finalSymbols.Length; i++)
        {
            if (symbolServerName == finalSymbols[i].serverName)
            {
                return true;
            }
        }

        return false;
    }

    public override IEnumerator executeOnReelEndRollback(SlotReel reel)
    {
        SlotSymbol[] finalSymbols = reel.visibleSymbols;
        for (int i = 0; i < finalSymbols.Length; i++)
        {
            if (symbolServerName == finalSymbols[i].serverName)
            {
                int reelNumber = reel.reelID - 1;
                int row = i;
                foreach (LandingAnimations item in landingAnimationList)
                {
                    if(item.reel == reelNumber && item.row == row)
                    {
                        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(item.landingAnimations));
                    }
                }
            }
        }
    }

    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        return true;
    }

}

