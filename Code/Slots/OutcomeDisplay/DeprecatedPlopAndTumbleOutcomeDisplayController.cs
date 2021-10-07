using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Extension of OutcomeDisplayController which includes legacy code required for the old version of tumble games to function
// this version of OutcomeDisplayController is only intended for use by those old tumble games
public class DeprecatedPlopAndTumbleOutcomeDisplayController : OutcomeDisplayController
{
	private TumbleOutcomeCoroutine _tumbleOutcomeCoroutine; //Callback happens after rollup but before rollup is treated as done, if tumble outcome data exists in outcome.
	public TICoroutine tumbleRoutine;

	protected bool shouldDoNormaleRollupFlow = true; // control used by DeprecatedPlopAndTumbleOutcomeDisplayController to skip the normal rollup flow
	
	// onOutcomeDisplayed - callback from a display module when it has completed its display.
	protected override void onOutcomeDisplayed(OutcomeDisplayBaseModule displayModule, SlotOutcome outcome)
	{
		base.onOutcomeDisplayed(displayModule, outcome);

		if (FreeSpinGame.instance != null && FreeSpinGame.instance.isLegacyTumbleGame)
		{
			(FreeSpinGame.instance as TumbleFreeSpinGame).isReadyForSymbolAnimations();
		}
		else if (SlotBaseGame.instance != null && SlotBaseGame.instance.isLegacyTumbleGame)
		{
			(SlotBaseGame.instance as TumbleSlotBaseGame).isReadyForSymbolAnimations();
		}
	}

	// for tumble slot games, we just want to play 1 outcome before removing these symbols
	public void playOneOutcome(int outcomeIndex)
	{		
		startOutcome(_loopedOutcomes[outcomeIndex]);
	}
	
	/// Callback for the completion of the payline cascade.  Proceeds to the next step of looping through all the paylines.
	protected override void onPaylineCascadeDone()
	{
		if (_isAutoSpinMode && !slotEngine.isFreeSpins && SlotBaseGame.instance.shouldSkipPayboxDisplay)
		{
			// cancel pre win stuff, otherwise the game will become locked
			preWinShowOutcome = null;
			StartCoroutine(startPayoutRollup());
		}
		else
		{
			if ((SlotBaseGame.instance != null && !SlotBaseGame.instance.isLegacyTumbleGame) || (FreeSpinGame.instance != null && !FreeSpinGame.instance.isLegacyTumbleGame))
			{
				setState(DisplayState.LoopDisplay);
			}
			else
			{
				// for tumble games, we don't go through looped outcomes, we just show the cascade once and then
				// do the tumble stuff. Must speed up rollup or else rollup takes forever while nothing is happening on screen
				setState(DisplayState.JustDoRollup);
				if (FreeSpinGame.instance != null && FreeSpinGame.instance.isLegacyTumbleGame)
				{
					(FreeSpinGame.instance as TumbleFreeSpinGame).isReadyForSymbolAnimations();
				}
				else if (SlotBaseGame.instance != null && SlotBaseGame.instance.isLegacyTumbleGame)
				{
					(SlotBaseGame.instance as TumbleSlotBaseGame).isReadyForSymbolAnimations();
				}
			}
		}
	}

	// Coroutine that runs the rollup of player credits, starting from 0 and ending with the number of credits won this spin.
	protected override IEnumerator startPayoutRollup()
	{
		JSON[] tumbleOutcomeJson = _rootOutcome.getTumbleOutcomes();
		bool shouldDoTumbleOutcomeCoroutine = (_tumbleOutcomeCoroutine != null && tumbleOutcomeJson != null && tumbleOutcomeJson.Length > 0);

		if (shouldDoTumbleOutcomeCoroutine)
		{
			shouldDoNormaleRollupFlow = false;
		}
		else
		{
			shouldDoNormaleRollupFlow = true;
		}

		long payout = _basePayout;
		long bonusPayout = BonusGameManager.instance.finalPayout;

		if (BonusGameManager.instance.multiBonusGamePayout > 0 && FreeSpinGame.instance == null) // multiBonusGamePayout is only used for rollups in the base game
		{
			bonusPayout = BonusGameManager.instance.multiBonusGamePayout;
			BonusGameManager.instance.multiBonusGamePayout = 0;
		}

		// Clear the bonus payout for next time.
		BonusGameManager.instance.finalPayout = 0;
		
		rollupsRunning.Add(true);
				
		if (_isAutoSpinMode && !slotEngine.isFreeSpins && SlotBaseGame.instance.shouldSkipPayboxDisplay)
		{
			// Go ahead and turn off the paylines while we're doing the rollup, in autospins modes.
			setState(DisplayState.Off);
		}
				
		if (slotEngine.progressivesHit > slotEngine.progressiveThreshold && GameState.game.keyName.Contains("wow"))
		{
			JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.
			if (progressivePoolsJSON != null && progressivePoolsJSON.Length > 0)
			{
				Debug.Log("Progressives Hit!: " + SlotsPlayer.instance.progressivePools.getPoolCredits("wow_fs_any_" + slotEngine.progressivesHit, multiplier, false));
				//payout += SlotsPlayer.instance.progressivePools.getPoolCredits("wow_fs_any_" + slotEngine.progressivesHit, multiplier, true);				
				payout += SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[slotEngine.progressivesHit - 5].getString("key_name", ""), multiplier, true);
			}
		}

		// Add the last bonus game's winnings to the total.
		payout += bonusPayout;
		
		JSON bonusPoolsJson = _rootOutcome.getBonusPools();
		bool shouldDoBonusPoolCoroutine = (_bonusPoolCoroutine != null && bonusPoolsJson != null);
		
		long rollupStart = 0L; // where to begin this payout. (Note that ReelGame will adjust this by adding it to runningPayoutRollupValue)
		long rollupEnd = payout; // this is the total amount won during this spin action (or in the case of Freespins, all the spins combined)
		long payoutWon = payout - rollupStart; // this is the amount won during this payout only

		// Show a big win if we're over the threshold and *not* going to play through a re-evaluation feature
		bool isDoingFreeSpinsInBase = ReelGame.activeGame.isDoingFreespinsInBasegame();
		bool isActiveGameBaseGame = (ReelGame.activeGame is SlotBaseGame) && !isDoingFreeSpinsInBase;
		bool isGoingToLaunchFreeSpinsInBase = (ReelGame.activeGame.playFreespinsInBasegame && ReelGame.activeGame.outcome.isBonus && ReelGame.activeGame.outcome.isGifting);
		bool shouldBigWin = isActiveGameBaseGame 
			&& !isGoingToLaunchFreeSpinsInBase
			&& ReelGame.activeGame.isOverBigWinThreshold(payout + ReelGame.activeGame.getCurrentRunningPayoutRollupValue()) 
			&& (!ReelGame.activeGame.hasReevaluationSpinsRemaining || (ReelGame.activeGame.hasReevaluationSpinsRemaining && ReelGame.activeGame.outcome.getTumbleOutcomes().Length > 0));

		// If a free spins game, don't add these credits to the player's total.
		// Also ignore them if we are doing free spins in base, as both base and freespins winnings will rollup when the player comes back from freespins
		bool isGoingToDoFreespinsInBase = SlotBaseGame.instance != null && SlotBaseGame.instance.playFreespinsInBasegame && SlotBaseGame.instance.outcome.hasFreespinsBonus();
		if (!slotEngine.isFreeSpins && !isGoingToDoFreespinsInBase)
		{
			// Add the winnings to the player's credits (including multi bonus games that they triggered and played), 
			// and less any amount that is in runningPayoutRollupValue that was already awarded, see NewTumbleSlotBaseGame
			long amountToAward = payoutWon + ReelGame.activeGame.getCurrentRunningPayoutRollupValue() - ReelGame.activeGame.getRunningPayoutRollupAlreadyPaidOut();

			SlotsPlayer.addCredits(amountToAward, "spin outcome", false);
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null && Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule != null)
			{
				(Overlay.instance.jackpotMystery.tokenBar as RoyalRushCollectionModule).updateSprintScore(payoutWon);
			}
			// if we have reevaluations then we need to mark this as already paid out, 
			// so that the next payout doesn't include it, but the value will still be
			// part of the running total in the win box
			if (ReelGame.activeGame.hasReevaluationSpinsRemaining)
			{
				ReelGame.activeGame.incrementRunningPayoutRollupAlreadyPaidOutBy(amountToAward);
			}

		#if RWR
			if (SlotsPlayer.instance.getIsRWRSweepstakesActive() &&
			   (GameState.game != null) && GameState.game.isRWRSweepstakes)
			{
				if (SpinPanel.instance.rwrSweepstakesMeter != null)
				{
					SpinPanel.instance.rwrSweepstakesMeter.addCount(payoutWon);
				}
			}
		#endif
			if (shouldBigWin && !shouldDoBonusPoolCoroutine)
			{
				// Only do this if we're not going to do a coroutine after the rollup,
				// because that coroutine is responsible for starting the big win if necessary.
				NotificationAction.sendJackpotNotifications(GameState.currentStateName);
				foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
				{
					if(module.needsToExecuteOnPreBigWin())
					{
						yield return StartCoroutine(module.executeOnPreBigWin());
					}
				}
				_bigWinNotificationCallback(payoutWon, false);
			}
		}
		
		if (VirtualPetRespinOverlayDialog.instance != null)
		{
			yield return StartCoroutine(VirtualPetRespinOverlayDialog.instance.playPaylinesCelebration());
		}

		if (shouldDoNormaleRollupFlow)
		{
			if ((SlotBaseGame.instance != null && SlotBaseGame.instance.isLegacyPlopGame) || (FreeSpinGame.instance != null && FreeSpinGame.instance.isLegacyPlopGame))
			{
				float rollupTime = PlopSlotBaseGame.CALCULATED_TIME_PER_CLUSTER * getNumClusterWins();
				if (shouldBigWin)
				{
					rollupTime *= 2.0f;
				}
			
				bool shouldPlayRollupSounds = true;
				if (ReelGame.activeGame is TumbleSlotBaseGame)
				{
					TumbleSlotBaseGame tumbleSlotBase = ReelGame.activeGame as TumbleSlotBaseGame;
					if (shouldBigWin && tumbleSlotBase.playRollupSoundsWithBigWinAnimation)
					{
						// don't play big win rollup sounds here even if a big win was triggered, 
						// we want to sync the sounds with the animation instead of the rollup
						// the rollup sounds will be triggered with the call to onBigWinNotification
						shouldPlayRollupSounds = false;
					}
				}
				rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, _payoutDelegate, playSound: shouldPlayRollupSounds, specificRollupTime: rollupTime, shouldBigWin: shouldBigWin));
				ReelGame.activeGame.doModulesOnPaylines(hasDisplayedOutcomes(), rollupRoutine);

			}
			else
			{
				if (shouldBigWin)
				{
					rollupStart = 0;
				}
								
				if (_subOutcomes != null && _subOutcomes.Count > 0)
				{
					//The sound only plays if its the first payline that animates 
					SlotOutcome firstPaylineOutcome = _subOutcomes[0];
					if (firstPaylineOutcome != null)
					{
						List<SlotSymbol> lineSymbols = null;
						if (firstPaylineOutcome.getPayLine() != "")
						{
							Payline line = Payline.find(firstPaylineOutcome.getPayLine());
							PaylineOutcomeDisplayModule paylineModule = outcomeDisplayModules[SlotOutcome.OutcomeTypeEnum.LINE_WIN] as PaylineOutcomeDisplayModule;
							lineSymbols = paylineModule.getSymbolsInPayLine(firstPaylineOutcome, line);
						}

						// @note : Removed rollup delay stuff from this class since no old plop/tumble games will ever be using it
					}
				}

				rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, _payoutDelegate, shouldBigWin: shouldBigWin));
				ReelGame.activeGame.doModulesOnPaylines(hasDisplayedOutcomes(), rollupRoutine);
			}

			// @todo - Big win/SlotPlayer rollup here
			yield return rollupRoutine;

			if (rollupsRunning.Count > 0)
			{
				rollupsRunning[rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
			}
			else
			{
				Debug.LogError("We should definitely have a rollup running here.");
			}
		}

		// handle bonus pool coroutine or tumble coroutines
		yield return StartCoroutine(handleSpecialOutcomeCoroutineCallbackTypes(bonusPayout, shouldBigWin, rollupStart, rollupEnd));

		// Tumble games have a different flow which results in hasFreespinGameStarted to return false which makes ReelGame.activeGame = null
		// in gifted freespins (because SlotBaseGame is null). Therefore, we need to use SlotBaseGame.instance and FreespinGame.instance
		if (FreeSpinGame.instance != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(FreeSpinGame.instance.checkModulesAtBonusPoolCoroutineStop(bonusPayout, _basePayout));
		}
		else if (SlotBaseGame.instance != null)
		{
			yield return RoutineRunner.instance.StartCoroutine(SlotBaseGame.instance.checkModulesAtBonusPoolCoroutineStop(bonusPayout, _basePayout));
		}
		if (rollupsRunning.Count == 1)
		{
			yield return RoutineRunner.instance.StartCoroutine(finalizeRollup());
		}
	}

	// function for handling bonus pool coroutines and tumble coroutines in the DeprecatedPlopAndTumbleOutcomeDisplayController
	protected override IEnumerator handleSpecialOutcomeCoroutineCallbackTypes(long bonusPayout, bool shouldBigWin, long rollupStart, long rollupEnd)
	{
		JSON bonusPoolsJson = _rootOutcome.getBonusPools();
		bool shouldDoBonusPoolCoroutine = (_bonusPoolCoroutine != null && bonusPoolsJson != null);

		JSON[] tumbleOutcomeJson = _rootOutcome.getTumbleOutcomes();
		bool shouldDoTumbleOutcomeCoroutine = (_tumbleOutcomeCoroutine != null && tumbleOutcomeJson != null && tumbleOutcomeJson.Length > 0);

		if (shouldDoBonusPoolCoroutine)
		{
			// Some slots like Elvira can have a bonus rollup after showing the first rollup.
			// This coroutine function is responsible for doing any special features and animations,
			// as well as its own rollup or whatever it needs to do.
			// We only call this for the first concurrent rollup, since this coroutine can start other rollups and cause an infinite loop.
			yield return StartCoroutine(_bonusPoolCoroutine(bonusPoolsJson, _basePayout, bonusPayout, _payoutDelegate, shouldBigWin, _bigWinNotificationCallback));
		}
		else if (shouldDoTumbleOutcomeCoroutine)
		{
			// Some slots like zynga01 (FarmVille2) have tumble outcomes that should be shown/processed
			// after the first outcome
			// We only call this for the first concurrent rollup, since this coroutine can start other rollups and cause an infinite loop.
			tumbleRoutine = StartCoroutine(_tumbleOutcomeCoroutine(tumbleOutcomeJson, _basePayout, bonusPayout, _payoutDelegate, shouldBigWin, _bigWinNotificationCallback, rollupStart, rollupEnd));
			yield return tumbleRoutine;
		}
	}

	// Pauses the tumble coroutine.
	public void pauseTumble()
	{
		if (tumbleRoutine != null)
		{
			tumbleRoutine.paused = true;
		}
	}

	// Resumes the tumble coroutine.
	public void resumeTumble()
	{
		if (tumbleRoutine != null)
		{
			tumbleRoutine.paused = false;
		}
	}

	/// Sets a coroutine callback that gets called after the rollups but before the endRollupDelegate is called,
	/// to extend post-rollback features to a game.
	public void setTumbleOutcomeCoroutine(TumbleOutcomeCoroutine callback)
	{
		_tumbleOutcomeCoroutine = callback;
	}
}

public delegate IEnumerator TumbleOutcomeCoroutine(JSON[] tumbleOutcomeJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd);
