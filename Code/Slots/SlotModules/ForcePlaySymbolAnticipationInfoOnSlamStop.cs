using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForcePlaySymbolAnticipationInfoOnSlamStop : SlotModule
{
	private Dictionary<int, string> anticipationInfo;
	private int numberOfPlayingAnticipationAnimations;
	
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return true;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		anticipationInfo = reelGame.outcome.getAnticipationSymbols();
		yield break;
	}
	
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return reelGame.engine.isSlamStopPressed && anticipationInfo != null && anticipationInfo.ContainsKey(reel.reelID);
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			if (symbol.serverName == anticipationInfo[reel.reelID])
			{
				numberOfPlayingAnticipationAnimations++;
				symbol.animateAnticipation(onAnticipationAnimationDone);
			}
		}
		yield break;
	}
	
	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		anticipationInfo = null;
		numberOfPlayingAnticipationAnimations = 0;
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return numberOfPlayingAnticipationAnimations > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (numberOfPlayingAnticipationAnimations > 0)
		{
			yield return null;
		}
	}

	private void onAnticipationAnimationDone(SlotSymbol symbol)
	{
		numberOfPlayingAnticipationAnimations--;
	}
}
