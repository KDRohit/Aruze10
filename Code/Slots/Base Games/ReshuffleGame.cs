using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// The base class that gets used for all games that have a reshuffle feature that needs to be triggered, such as shark01 or dead01.
public class ReshuffleGame : SlotBaseGame
{

	private const float RUMBLE_TIME_WITHOUT_PAYLINE = 1.0f;

	protected override void Awake()
	{
		base.Awake();
		_outcomeDisplayController.setBonusPoolCoroutine(bonusAfterRollup);
	}

	// Starts the screen shaking if there is going to be a reshuffle of the screen with a tornado.
	protected override void reelsStoppedCallback()
	{
		JSON[] reevalJSONs = _outcome.getArrayReevaluations();
		bool hasReshufflewithoutOutcome = false;
		if (reevalJSONs.Length > 0)
		{
			playPrereshuffleEffects();
			if (_outcome.getSubOutcomesReadOnly().Count == 0)
			{
				hasReshufflewithoutOutcome = true;
				StartCoroutine(startBonusAfterRollupWithDelay(RUMBLE_TIME_WITHOUT_PAYLINE));
			}
		}
		// We have to do this weird check here because bonusAfterRollup won't get called unless there is a roll up. And we don't want to call the
		// reels stopped twice.
		if (!hasReshufflewithoutOutcome)
		{
			base.reelsStoppedCallback();
		}
	}

	// Function that gets called before the reshuffle effects get started. Usefully for playing audio or shaking the screen.
	protected virtual void playPrereshuffleEffects()
	{
	}

	protected virtual IEnumerator startBonusAfterRollupWithDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		yield return StartCoroutine(bonusAfterRollup(null, 0, 0, null, false, null));
	}

	// Handles the tornado stuff that happens after the spin has been evaluated one time.
	// None of these parameters are used.
	protected virtual IEnumerator bonusAfterRollup(JSON bonusPoolsJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate)
	{
		// setup the rollup to continue from where it was at before the reshuffle
		long amountAlreadyAwarded = basePayout + bonusPayout;
		runningPayoutRollupValue = amountAlreadyAwarded;
		runningPayoutRollupAlreadyPaidOut = amountAlreadyAwarded;

		JSON[] reevalJSONs = _outcome.getArrayReevaluations();
		JSON reevalJSON = null;
		if (reevalJSONs != null && reevalJSONs.Length != 0)
		{
			// Force terminate the big win (if showing) so that it isn't going while the reshuffle happens, 
			// since it normally isn't cancelled until outcomeDisplayController.finalizeRollup() 
			// is called which doesn't happen until lower down
			yield return StartCoroutine(forceEndBigWinEffect(isDoingContinueWhenReady: false));

			// Clear the current outcome so it's not animating during the tornado.
			clearOutcomeDisplay();
			foreach (JSON json in reevalJSONs)
 			{
 				List<List<string>> newMatrix = json.getStringListList("reevaluated_matrix");
 				if (newMatrix.Count > 0)
 				{
 					reevalJSON = json;
					yield return StartCoroutine(startReshuffle(newMatrix));
					break;
				}
 			}

 			// Always update the _outcome, this will either be used below to force show the display if
 			// we already showed paylines, or in base.ReelsStoppedCallback if no paylines have been dispalyed yet
			_outcome = new SlotOutcome(reevalJSON);
			
			if (_outcomeDisplayController.rollupsRunning.Count > 0)
			{
				// Show new paylines and roll up the new winnings.
				// The new winnings are in addition to the base payout,
				// even though some reevaluated paylines are the same as the original payout.
				bool autoSpinMode = hasAutoSpinsRemaining;
				_outcomeDisplayController.displayOutcome(_outcome, autoSpinMode);
				
				// Wait for the reevaluation rollup to start.
				// This is necessary because previous outcome paylines need
				// to fade and the new paylines need to appear
				// before the new rollup starts, and we need to wait
				// until the rollup starts before we show the big win,
				// otherwise it sits at 0 until the rollup starts (a couple of seconds).
				while (_outcomeDisplayController.rollupsRunning.Count == 1)
				{
					yield return null;
				}

				// Wait for the reevaluation rollup to finish, which is rollup 2 (index 1).
				int rollupIndex = _outcomeDisplayController.rollupsRunning.Count - 1;
				while (_outcomeDisplayController.rollupsRunning[rollupIndex])
				{
					yield return null;
				}
				// We're finally done with this whole outcome.
				yield return StartCoroutine(_outcomeDisplayController.finalizeRollup());
			}
			else
			{
				// We need to call the reelsStoppedCallback now because we skipped it earlier because there was no orignal paylines that would have called bonusAfterRollup Naturally.
				base.reelsStoppedCallback();
			}
		}
	}

	// Gets played when the reshuffle is supposed to be starting. This is where moving all of the symbols should play out.
	protected virtual IEnumerator startReshuffle(List<List<string>> newSymbolMatrix)
	{
		yield return null;
	}
}
