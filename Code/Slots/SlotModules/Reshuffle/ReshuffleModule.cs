using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReshuffleModule : SlotModule 
{
	[SerializeField] private float RUMBLE_TIME_WITHOUT_PAYLINE = 1.0f;
	protected bool isReshuffleHappening = false;

	// Delay if pre-reeval outcome is enough to be a bigwin
	// NOTE: Module will play bigwin when we finish reshuffling (in bonusAfterRollup), 
	// where it will call reelGame.outcomeDisplayController.displayOutcome()
	public override bool isModuleHandlingBigWin()
	{
		JSON[] reevalJSONs = reelGame.outcome.getArrayReevaluations();
		if (reevalJSONs != null && reevalJSONs.Length != 0)
		{	
			return true;
		}
		else
		{
			return false;
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		JSON[] reevalJSONs = reelGame.outcome.getArrayReevaluations();
		if (reevalJSONs.Length > 0)
		{
			if (reelGame.outcome.getSubOutcomesReadOnly().Count == 0)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(startBonusAfterRollupWithDelay(RUMBLE_TIME_WITHOUT_PAYLINE));
	}
	
// end sections

// executeOnStartPayoutRollupEnd() section
// functions in this section are accessed by OutcomeDisplayController.startPayoutRollup()	
	public override bool needsToExecuteOnStartPayoutRollupEnd(long bonusPayout, long basePayout)
	{
		JSON[] reevalJSONs = reelGame.outcome.getArrayReevaluations();
		if (reevalJSONs.Length > 0)
		{
			if (reelGame.outcome.getSubOutcomesReadOnly().Count != 0)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnStartPayoutRollupEnd(long bonusPayout, long basePayout)
	{
		// setup the rollup to continue from where it was at before the reshuffle starts
		long amountAlreadyAwarded = basePayout + bonusPayout;
		reelGame.setRunningPayoutRollupValue(amountAlreadyAwarded);

		yield return StartCoroutine(bonusAfterRollup());
	}

// end sections

	private IEnumerator startBonusAfterRollupWithDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		yield return StartCoroutine(bonusAfterRollup());
	}

	// Handles the tornado stuff that happens after the spin has been evaluated one time.
	protected virtual IEnumerator bonusAfterRollup()
	{
		JSON[] reevalJSONs = reelGame.outcome.getArrayReevaluations();
		JSON reevalJSON = null;
		if (reevalJSONs != null && reevalJSONs.Length != 0)
		{
			// Clear the current outcome so it's not animating during the tornado.
			reelGame.outcomeDisplayController.clearOutcome();
			foreach (JSON json in reevalJSONs)
			{
				List<List<string>> newMatrix = json.getStringListList("reevaluated_matrix");
				if (newMatrix.Count > 0)
				{
					reevalJSON = json;
					isReshuffleHappening = true;
					yield return RoutineRunner.instance.StartCoroutine(onEnterReshuffle());
					yield return RoutineRunner.instance.StartCoroutine(startReshuffle(newMatrix));
					yield return RoutineRunner.instance.StartCoroutine(onExitReshuffle());
					break;
				}
			}

			// the 'yield return RoutineRunner.instance.StartCoroutine() call above is not yielding correctly
			// causing the game to continue incorrectly. Temporary solution is to wait until this boolean is set.
			while (isReshuffleHappening)
			{
				yield return null;
			}

			// Always update the _outcome, this will either be used below to force show the display if
 			// we already showed paylines, or in the ReelGame ReelsStoppedCallback if no paylines have been displayed yet
			SlotOutcome reevalOutcome = new SlotOutcome(reevalJSON);
 			reelGame.setOutcomeNoExtraProcessing(reevalOutcome);

			if (reelGame.outcomeDisplayController.rollupsRunning.Count > 0)
			{
				// Show new paylines and roll up the new winnings.
				// The new winnings are in addition to the base payout,
				// even though some reevaluated paylines are the same as the original payout.
				//
				// If the new outcome's payout is enough to trigger a bigwin, 
				// outcomeDisplayController will handle displaying the bigwin
				bool autoSpinMode = reelGame.hasAutoSpinsRemaining;
				reelGame.outcomeDisplayController.displayOutcome(reelGame.outcome, autoSpinMode);
				
				// Wait for the reevaluation rollup to start.
				// This is necessary because previous outcome paylines need
				// to fade and the new paylines need to appear
				// before the new rollup starts, and we need to wait
				// until the rollup starts before we show the big win,
				// otherwise it sits at 0 until the rollup starts (a couple of seconds).
				while (reelGame.outcomeDisplayController.rollupsRunning.Count == 1)
				{
					yield return null;
				}
				
				// Wait for the reevaluation rollup to finish, which is rollup 2 (index 1).
				int rollupIndex = reelGame.outcomeDisplayController.rollupsRunning.Count - 1;
				if (rollupIndex > 0)
				{
					while (reelGame.outcomeDisplayController.rollupsRunning[rollupIndex])
					{
						yield return null;
					}
					// We're finally done with this whole outcome.
					yield return StartCoroutine(reelGame.outcomeDisplayController.finalizeRollup());
				}
			}
		}
	}
	
	// Gets played before reshuffle starts. Any animations and/or sounds that happen before the shuffle should play here.
	protected virtual IEnumerator onEnterReshuffle()
	{
		yield break;
	}

	// Gets played after reshuffle ends. Any animations and/or sounds that happen after the shuffle should play here.
	protected virtual IEnumerator onExitReshuffle()
	{
		yield break;
	}

	// Gets played when the reshuffle is supposed to be starting. This is where moving all of the symbols should play out.
	protected virtual IEnumerator startReshuffle(List<List<string>> newSymbolMatrix)
	{
		yield return null;
	}

}
