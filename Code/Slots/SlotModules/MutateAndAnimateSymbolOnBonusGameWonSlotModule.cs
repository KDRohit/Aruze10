using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Mutate Wild symbol to play Bonus trigger animation and then mutate them back in Aruze05.
public class MutateAndAnimateSymbolOnBonusGameWonSlotModule : SlotModule
{
    [SerializeField] private string symbolNameToMutateTo = "BN";
    [SerializeField] private string symbolNameToMutateFrom = "WD";
	[SerializeField] private AudioListController.AudioInformationList soundsToPlayBeforeSymbolAnimations;
    List<TICoroutine> runningCoroutines = new List<TICoroutine>();
    List<SlotSymbol> mutatedSymbolsList = new List<SlotSymbol>();

    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        if (reelGame.outcome.hasBonusGame())
        {
            return true;
        }
        return false;
    }

    public override IEnumerator executeOnReelsStoppedCallback()
    {
        if (soundsToPlayBeforeSymbolAnimations.Count > 0)
        {
            yield return StartCoroutine(AudioListController.playListOfAudioInformation(soundsToPlayBeforeSymbolAnimations));
        }

        SlotReel[] reelArray = reelGame.engine.getReelArray();

        foreach (SlotReel reel in reelArray)
        {
            foreach (SlotSymbol symbol in reel.visibleSymbols)
            {
                if (symbol.serverName == symbolNameToMutateFrom)
                {
                    // Found a symbol to change.
                    mutateAndPlaySymbolAnimationOn(symbol);
                }
            }

        }

        yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

        foreach (SlotSymbol symbol in mutatedSymbolsList)
        {
            symbol.mutateTo(symbolNameToMutateFrom);
        }
        mutatedSymbolsList.Clear();
        runningCoroutines.Clear();
    }

    private void mutateAndPlaySymbolAnimationOn(SlotSymbol symbol)
    {
        symbol.mutateTo(symbolNameToMutateTo);
        mutatedSymbolsList.Add(symbol);
        runningCoroutines.Add(StartCoroutine(symbol.playAndWaitForAnimateOutcome()));
    }

}
