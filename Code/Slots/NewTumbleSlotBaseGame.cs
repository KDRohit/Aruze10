using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewTumbleSlotBaseGame : SlotBaseGame
{
	private bool isBigWinHappening = false;

	protected override void onBigWinNotification(long payout, bool isSettingStartingAmountToPayout = false)
	{
		isBigWinHappening = true;
		base.onBigWinNotification(payout, isSettingStartingAmountToPayout);
	}

	protected override void setEngine()
	{
		engine = new TumbleSlotEngine(this);
	}

	// this game doesn't need swipeable reels, so don't do anything
	protected override void setSwipeableReels()
	{}

	protected override IEnumerator doReelsStopped(bool isAllowingContinueWhenReadyToEndSpin = true)
	{
		// Tell the bigwin to stop immediately if there are no more reevaluation spins (tumble outcomes)
		if (bigWinEffect != null)
		{
			if (reevaluationSpinsRemaining == 0) 
			{
				isBigWinHappening = false;

				if (_outcomeDisplayController.rollupsRunning.Count == 0)
				{
					// there aren't anymore rollups going, so we need to hide the big win here since it isn't going to hide when a rollup finishes
					yield return StartCoroutine(bigWinEffect.endBigWin());
				}
			}
		}

		// Do normal reel stops if there are no reevaluation spins (tumble outcomes)
		if (currentReevaluationSpin == null && !outcome.isBonus)
		{
			yield return StartCoroutine(base.doReelsStopped());
		}
		// Process the outcome of reevaluation spins (tumble outcomes) if they exist
		else if (currentReevaluationSpin != null)
		{
			SlotOutcome currentOutcome = currentReevaluationSpin;
			currentOutcome.processBonus();

			if (currentOutcome.isBonus)
			{
				if (!playFreespinsInBasegame || !currentOutcome.isGifting)
				{
					bool shouldModuleCreateBonus = false;
					foreach (SlotModule module in cachedAttachedSlotModules)
					{
						if (module.needsToLetModuleCreateBonusGame())
						{
							shouldModuleCreateBonus = true;
						}
					}
					// handle the pre bonus created modules, needed for some transitions
					// Don't handle the pre bonus here if we want to create a portal first. 
					if (banners.Length <= 0)
					{
						foreach (SlotModule module in cachedAttachedSlotModules)
						{
							if (module.needsToExecuteOnPreBonusGameCreated())
							{
								yield return StartCoroutine(module.executeOnPreBonusGameCreated());
							}
						}
					}
					if (!shouldModuleCreateBonus)
					{
						createBonus();
					}
				}
			}
			else
			{
				yield return StartCoroutine(base.doReelsStopped());
			}
		}

		yield return null;
	}

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		if (currentReevaluationSpin == null)
		{
			StartCoroutine(doReelsStopped());
		}
	}

	public override bool isBigWinBlocking
	{
		get
		{
			return false;
		}
	}

	// Overriding this because we don't want to do the big win effect changes that the base version does
	public override IEnumerator onEndRollup(bool isAllowingContinueWhenReady, bool isAddingRollupToRunningPayout = true)
	{
		if (isAddingRollupToRunningPayout)
		{
			moveLastPayoutIntoRunningPayoutRollupValue();
		}
		else
		{
			lastPayoutRollupValue = 0;
		}

		if (bigWinEffect != null)
		{
			// if we are rolling up and we are on the second to last reevaluation then that means that there are no
			// more payouts because the last reevaluation means that no more symbols will payout and vanish
			if (reevaluationSpinsRemaining == 1) 
			{
				isBigWinHappening = false;
			}

			if (!isBigWinHappening)
			{
				yield return StartCoroutine(bigWinEffect.endBigWin());
			}
		}
		
		if (isAllowingContinueWhenReady)
		{
			yield return StartCoroutine(waitForModulesAfterPaylines(true));
			// If our effect is null, let's not have it stop progression. Just a safety check.
			yield return StartCoroutine(continueWhenReady());
		}
		
		// since the tumble game will payout the running rollup as it tumbles we need to make sure that we track what is already paid out
		runningPayoutRollupAlreadyPaidOut = runningPayoutRollupValue;
	}
}
