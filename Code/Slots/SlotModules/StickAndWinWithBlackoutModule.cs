using System;
using System.Collections;
using System.Collections.Generic;
using Google.Apis.Json;
using UnityEngine;

/*
 * Stick and Win with Black out: If the player fully blacks out all of the independent
 * reels a visual update to the symbols occurs
 * 
 * Games: orig012
 *
 * Original Author: Shaun Peoples <speoples@zynga.com>
 */
public class StickAndWinWithBlackoutModule : SwapNormalToIndependentReelTypesBaseModule
{
	protected enum ReelRewardTypeEnum
	{
		Unknown = 0,
		Credits,
		Bonus
	}

	protected class ReelRewardData : System.IComparable<ReelRewardData>
	{
		public int reelIndex = 0; 
		public int symbolPos = 0;
		public string symbolName;
		public int multiplier = 0;
		public long rewardCreditAmount = 0; // Will contain the amount of credits that are awarded when the type is a jackpot or credit reward
		public ReelRewardTypeEnum rewardType = ReelRewardTypeEnum.Unknown; // What type of reward this reel is
		public SlotOutcome bonusOutcome; // If this is a bonus type, this will contain the bonus game outcome

		public int CompareTo(ReelRewardData other)
		{
			return reelIndex == other.reelIndex ? other.symbolPos.CompareTo(this.symbolPos) : reelIndex.CompareTo(other.reelIndex);
		}
	}

	[Tooltip("Label that displays the spin count.  Gets updated as the number of spins for this feature change.")]
	[SerializeField] private LabelWrapperComponent spinCountLabel;
	
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[SerializeField] private float preRollupDelayTime;
	[SerializeField] private float postRollupWaitTime;
	[SerializeField] private float snwScSymbolPayoutParticleTrailDelay;
	
	[Tooltip("Rollup loop used when awarding a credit value symbol")]
	[SerializeField] private string creditValueSymbolRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a credit value symbol")]
	[SerializeField] private string creditValueSymbolRollupTermKey = "stick_and_win_rollup_end";
	
	[Tooltip("Particle trail used when paying out a basic value win amount.  Will originate from the symbol and should be setup to go to the win meter.")]
	[SerializeField] private AnimatedParticleEffect valueSymbolPayoutParticleTrail;
	[SerializeField] private AnimationListController.AnimationInformationList showIndependentReelDividersAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList independentReelsShowAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList independentReelsHideAnimations;
	
	[Tooltip("Rollup loop used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupTermKey = "stick_and_win_rollup_end";

	[Tooltip("Scatter symbols from normal reel name to transition into label value symbols")] 
	[SerializeField] private string normalReelScatterSymbolName;
	[Tooltip("Scatter symbols to mutate into on independent reels to show credit values")] 
	[SerializeField] private string independentReelScatterSymbolName;
	[Tooltip("Delay between symbol loop restarts")]
	[SerializeField]private float symbolLoopDelay;
	
	private JSON[] reevaluationSpinNewLockedSymbolInfo;
	private JSON[] reevaluationSpinOldLockedSymbolInfo;
	private bool isSpinMeterSet;
	private Dictionary<ReelRewardTypeEnum, List<ReelRewardData>> rewardDictionary = new Dictionary<ReelRewardTypeEnum, List<ReelRewardData>>(); // Dictionary of rewards to give out to the player based on what symbols land
	private bool isPlayingBonusGame = false;
	private Dictionary<string, long> symbolToValue = new Dictionary<string, long>(); //Dictionary that stores the scatter symbols and their associated credit value
	private bool didStartGameInitialization = false;
	private bool hasInitialRespinCountBeenSet = false;
	private bool isHandlingReelRewards = false; // Flag used to track if this module still needs to award the reel rewards (or is in the process of doing so) that will block the game from finishing a spin
	private int spinMeterStartAndResetValue = DEFAULT_SPIN_METER_START_AND_RESET_VALUE; // Store out what the spin meter start value is (this is also the reset value).  We will use this to determine when the spin counter is resetting
	private bool isBigWinShown = false;			// Need to track if this module has triggered a big win and doesn't need to trigger one anymore
	private List<TICoroutine> allLockedLoopingSymbolCoroutines = new List<TICoroutine>();
	private List<SlotSymbol> lockedLoopingSymbols = new List<SlotSymbol>();
	private bool isLoopingStandardReelSymbols = false;
	private List<TICoroutine> standardReelAreaLoopedSymbolCoroutines = new List<TICoroutine>();

	private bool hasDelayedReevaluations;
	private bool delayedReevaluationsInit;
	private bool hasBonusGame = false;
	private object originalReevals = null;
	private SlotOutcome originalOutcome = null;

	private const string JSON_REEL_LOCKING_SYMBOLS_INFO_KEY = "locked_symbols_info";
	private const string JSON_NEW_LOCKED_SYMBOLS_INFO_KEY = "new_locked_symbols_info";
	private const string JSON_OLD_LOCKED_SYMBOLS_INFO_KEY = "old_locked_symbols_info";
	private const string JSON_SPIN_METER_START_VALUE_KEY = "spin_meter_start_value";
	private const string JSON_SPIN_METER_KEY = "spin_meter";
	private const string JSON_REWARDS_KEY = "rewards";
	private const string JSON_STICK_AND_WIN_KEY = "{0}_pick_game_with_freespin_and_stick_and_win_rewards"; // needs game key appended to the front
	private const string JSON_INITIAL_SCATTER_VALUES_KEY = "sc_symbols_value";

	private const int DEFAULT_SPIN_METER_START_AND_RESET_VALUE = 1; // Default value so game would still semi function.  However if the server data to fill spinMeterStartAndResetValue does not come down an error message will be logged (since that shouldn't happen).

	private IEnumerator setSpinCountMessageText(int count)
	{
		if (spinCountLabel != null)
		{
			spinCountLabel.text = CommonText.formatNumber(count);
		}
		else
		{
			// Fallback to the built in spin panel message text if we don't have a custom label to display the count to
			if (reelGame is SlotBaseGame game)
			{
				game.setMessageText(count > 1 ? Localize.text("{0}_spins_remaining", count) : Localize.text("good_luck_last_spin"));
			}

			SpinPanel.instance.slideInPaylineMessageBox();
		}
		
		yield break;
	}

	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		if (!hasDelayedReevaluations)
		{
			yield break;
		}
		
		List<TICoroutine> independentReelIntroCoroutines = new List<TICoroutine>();
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		List<SlotSymbol> symbolsFaded = new List<SlotSymbol>();

		// Fade out the non triggering symbols and animate the triggering ones (transitioning into looping animations if they have them)
		isLoopingStandardReelSymbols = true;
		foreach (SlotSymbol currentSymbol in allVisibleSymbols)
		{
			if (currentSymbol == null)
			{
				continue;
			}
			
			if (!currentSymbol.isScatterSymbol && !currentSymbol.isBonusSymbol)
			{
				// Not scatter symbol so fade this symbol out

				// Need to check for if we've already handled fading a tall/mega symbol as part of a previous part of it
				if (symbolsFaded.Contains(currentSymbol))
				{
					continue;
				}
				
				List<SlotSymbol> allSymbolParts = currentSymbol.getAllSymbolParts();
				foreach (SlotSymbol symbol in allSymbolParts)
				{
					symbolsFaded.Add(symbol);
				}

				independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.fadeOutSymbolCoroutine(1.0f)));
			}
		}

		// Play the animation to display the independent reel dividers at the same time we fade the non-scatter symbols
		if (showIndependentReelDividersAnimations.Count > 0)
		{
			independentReelIntroCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(showIndependentReelDividersAnimations)));	
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(independentReelIntroCoroutines));

		// Setup BL symbols on the base layer where the faded symbols were so we can
		// use that info when copying over to the independent reels
		foreach (SlotSymbol currentSymbol in symbolsFaded)
		{
			currentSymbol.mutateTo("BL", null, false, true);
		}
		
		// normalReelArray is the non-independent reel layer
		List<SlotSymbol> symbolsToTryToStartLooping = new List<SlotSymbol>();
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_NORMAL_REELS);
		
		for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = normalReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, LAYER_INDEX_INDEPENDENT_REELS);
			
			// Copy the visible symbols to the independent reel layer
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[symbolIndex];
				if (independentSymbol.serverName == visibleSymbols[symbolIndex].serverName)
				{
					continue;
				}

				string symbolMutationName = visibleSymbols[symbolIndex].serverName == normalReelScatterSymbolName
					? independentReelScatterSymbolName
					: visibleSymbols[symbolIndex].serverName;
				
				independentSymbol.mutateTo(symbolMutationName, null, false, true);
				symbolsToTryToStartLooping.Add(independentSymbol);
				
				// Lock in any scatter symbols that were already landed
				if (!independentSymbol.isScatterSymbol && !independentSymbol.isBonusSymbol)
				{
					continue;
				}
				
				SlotReel reelToLock = independentSymbol.reel;
				reelToLock.isLocked = true;
			}
		}
		
		// Before swapping to the independent layer, make sure that the looped symbols are at the end of a loop
		// so they should line up correctly when the loop starts on the independent reel version of the symbols
		// **
		// NOTE : Lining the animations up this way will only work if the animations are all the same length.
		// If they are different length some of them will finish first and sit stopped until all of them finish
		// and it can swap the reels over and start looping the symbols on the other layer.  One possible solution
		// to this would be to instead of doing this just terminate all the loops but store out where they were in
		// the animation and use that animation timing to play the animations on the other layer.  That is fairly
		// complicated though and may still not be completely perfect.  So for now it might be best to just use
		// loop animations that are all the same length if possible.
		isLoopingStandardReelSymbols = false;
		if (standardReelAreaLoopedSymbolCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(standardReelAreaLoopedSymbolCoroutines));
			standardReelAreaLoopedSymbolCoroutines.Clear();
		}

		// Now change what is being shown to be the independent layer
		showLayer(LAYER_INDEX_INDEPENDENT_REELS);
		
		// Now that the independent layer is showing, let's continue the looped animations on that layer
		foreach (SlotSymbol symbol in symbolsToTryToStartLooping)
		{
			tryStartLoopedLockAnimationOnSymbol(symbol);
		}

		if (independentReelsShowAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(independentReelsShowAnimations));
		}
	}
	
	private IEnumerator loopAnticipationAnimationOnStandardSymbolUntilReelTypeSwap(SlotSymbol symbol)
	{
		// This condition is just a fallback, I think for now we'll actually still just force cancel
		// the animations and kill the coroutines when the award part of the code starts
		while (isLoopingStandardReelSymbols)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
		}
		
		// Now that we are done looping put the symbol back in the default symbol state so that it appears
		// correctly when the reels are converted back to standard after the feature is over.
		symbol.mutateTo(symbol.serverName, null, false, true);
	}

	private void tryStartLoopedLockAnimationOnSymbol(SlotSymbol symbol)
	{
		bool isSymbolConvertedToLoopSymbol = symbol.tryConvertSymbolToLoopSymbol();
		if (!isSymbolConvertedToLoopSymbol)
		{
			return;
		}
		
		lockedLoopingSymbols.Add(symbol);
		allLockedLoopingSymbolCoroutines.Add(StartCoroutine(loopAnticipationAnimationOnIndependentSymbolUntilRewards(symbol)));
	}

	private IEnumerator loopAnticipationAnimationOnIndependentSymbolUntilRewards(SlotSymbol symbol)
	{
		// This condition is just a fallback, I think for now we'll actually still just force cancel
		// the animations and kill the coroutines when the award part of the code starts
		while (!isHandlingReelRewards)
		{
			if (!symbol.isAnimating)
			{
				yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());

				if (symbolLoopDelay > 0)
				{
					yield return new WaitForSeconds(symbolLoopDelay);
				}
			}
		}
	}

	private void stopAllLoopedOutcomeAnimationOnSymbols()
	{
		foreach (TICoroutine currentCoroutine in allLockedLoopingSymbolCoroutines)
		{
			if (currentCoroutine != null && !currentCoroutine.finished)
			{
				StopCoroutine(currentCoroutine);
			}
		}
		standardReelAreaLoopedSymbolCoroutines.Clear();

		foreach (SlotSymbol symbol in lockedLoopingSymbols)
		{
			symbol.haltAnimation();
			
			// Try and put the symbol into its final state here, otherwise it will be stuck on the first frame of the loop animation
			bool didConvertSymbol = symbol.tryConvertSymbolToAwardSymbol();
			if (didConvertSymbol)
			{
				continue;
			}
			
			didConvertSymbol = symbol.tryConvertSymbolToOutcomeSymbol(false);
				
			// if neither _Award or _Outcome are set for this symbol, just convert it back into a standard non _Loop version of the symbol
			if (!didConvertSymbol)
			{
				symbol.mutateTo(symbol.serverName, null, false, true);
			}
		}
		lockedLoopingSymbols.Clear();
	}
	
	protected override IEnumerator swapSymbolsBackToNormalReels()
	{
		if (!hasDelayedReevaluations)
		{
			yield break;
		}
		
		// Copy the independent reel symbols back over to the normal (non-independent) reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_NORMAL_REELS);
		for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
		{	
			SlotReel currentReel = normalReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);
			
			// Copy the visible symbols to the independent reel layer
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[symbolIndex];
				SlotSymbol currentRegularSymbol = visibleSymbols[symbolIndex];
				if (independentSymbol.serverName == currentRegularSymbol.serverName)
				{
					continue;
				}
				
				currentRegularSymbol.mutateTo(independentSymbol.serverName, null, false, true);

				LabelWrapperComponent label = independentSymbol.getDynamicLabel();
				// if this isn't a blank symbol, copy the value back onto it
				if (!currentRegularSymbol.isBlankSymbol &&  label != null)
				{
					// Check if this is a symbol with a value on it, in which case we need to transfer the value data
					currentRegularSymbol.getDynamicLabel().text = label.text;
				}
			}
		}
		
		// Turn off the reel dividers for independent reels
		if (independentReelsHideAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(independentReelsHideAnimations));
		}
		
		showLayer(LAYER_INDEX_NORMAL_REELS);
		
		// Convert all symbols on the independent reels to be BL so they are blank
		// the next time we trigger the feature.
		SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(1);
		foreach (SlotReel currentReel in independentReelArray)
		{
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;

			foreach (SlotSymbol symbol in visibleSymbols)
			{
				if (symbol.isBlankSymbol)
				{
					continue;
				}
				
				symbol.mutateTo("BL", null, false, true);
			}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (hasDelayedReevaluations)
		{
			return false;
		}
		
		JSON[] evals = reelGame.outcome.getOutcomeJsonValue<JSON[]>(JSON.getJsonArrayStatic, "reevaluations", false);
		
		foreach(JSON eval in evals)
		{
			hasBonusGame = eval.getJsonArray("bonus_game") != null;
			
			JSON[] delayedEvals = eval.getJsonArray("delayed_reevaluations");

			if (delayedEvals != null && delayedEvals.Length > 0 && delayedEvals[0] != null)
			{
				hasDelayedReevaluations = true;

				originalOutcome = reelGame.outcome;

				reelGame.outcome.getJsonObject().jsonDict.TryGetValue("reevaluations", out originalReevals);
				originalOutcome.moveDelayedReevaluationsIntoReevaluations(originalOutcome, originalReevals, false, true);
				reelGame.setOutcome(originalOutcome);
				
				delayedReevaluationsInit = true;
				
				break;
			}
		}
		
		return false;
	}

	public override bool needsToLetModuleCreateBonusGame()
	{
		return hasBonusGame;
	}

	public override bool needsToExecuteOnReevaluationSpinEnd()
	{
		return true;
	}

	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		yield return StartCoroutine(base.executeOnReevaluationPreSpin());
		
		if (!hasDelayedReevaluations)
		{
			yield break;
		}

		if (hasInitialRespinCountBeenSet)
		{
			yield break;
		}
		
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		currentReevalOutcomeJson.validateHasKey(JSON_SPIN_METER_START_VALUE_KEY);
		spinMeterStartAndResetValue = currentReevalOutcomeJson.getInt(JSON_SPIN_METER_START_VALUE_KEY, DEFAULT_SPIN_METER_START_AND_RESET_VALUE);
		yield return StartCoroutine(setSpinCountMessageText(spinMeterStartAndResetValue));
		
		extractReelRewardPrizeAwardData();
		
		SlotOutcome spinData = originalOutcome.getReevaluationSpins()[0];
		reelGame.engine.setOutcome(spinData);
		reelGame.engine.handleOutcomeBeforeSetReelSet(spinData);

		//this should swap us to independent reels if we're not already there
		if (!isUsingIndependentReels)
		{
			yield return StartCoroutine(base.activateIndependentReels());
		}
		
		hasInitialRespinCountBeenSet = true;
	}

	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		JSON lockedSymbolInfoJson = currentReevalOutcomeJson.getJSON(JSON_REEL_LOCKING_SYMBOLS_INFO_KEY);
		reevaluationSpinNewLockedSymbolInfo = lockedSymbolInfoJson.getJsonArray(JSON_NEW_LOCKED_SYMBOLS_INFO_KEY);
		reevaluationSpinOldLockedSymbolInfo = lockedSymbolInfoJson.getJsonArray(JSON_OLD_LOCKED_SYMBOLS_INFO_KEY);
		
		return true;
	}
	
	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		if (!hasDelayedReevaluations)
		{
			yield break;
		}
		
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		int spinMeterCurrentValue = currentReevalOutcomeJson.getInt(JSON_SPIN_METER_KEY, spinMeterStartAndResetValue);
		yield return StartCoroutine(setSpinCountMessageText(spinMeterCurrentValue));
		
		setValuesForSnwScSymbols();
		yield return StartCoroutine(lockInNewSymbols(reevaluationSpinNewLockedSymbolInfo));

		//return to normal reels if we're using them and we are out of reevaluationsspins
		if (!reelGame.hasReevaluationSpinsRemaining && isUsingIndependentReels)
		{
			yield return StartCoroutine(handleAllRewards());
			hasInitialRespinCountBeenSet = false;
			
			//this should swap us to independent reels if we're not already there
			yield return StartCoroutine(base.activateNormalReels());
			hasDelayedReevaluations = false;
			delayedReevaluationsInit = false;
		}
	}

	private void setValuesForSnwScSymbols()
	{
		if (reevaluationSpinOldLockedSymbolInfo != null && reevaluationSpinOldLockedSymbolInfo.Length > 0)
		{
			updateFromLockedSymbolsInfo(reevaluationSpinOldLockedSymbolInfo);
		}
	}

	private void updateFromLockedSymbolsInfo(JSON[] data)
	{
		foreach (JSON symbolInfo in data)
		{
			int reelIndex = symbolInfo.getInt("reel", -1);
			int position = symbolInfo.getInt("position", -1);
			long credits = symbolInfo.getLong("credits", -1);
			long multiplier = symbolInfo.getLong("multiplier", 1);

			if (reelIndex < 0 || position < 0 || credits < 0)
			{
				continue;
			}

			credits *= reelGame.multiplier;
			
			List<SlotSymbol> slotSymbols = reelGame.engine.getVisibleSymbolsBottomUpAt(reelIndex);
			SlotSymbol slotSymbol = slotSymbols[position];
			if (slotSymbol == null)
			{
				continue;
			}
			
			SymbolAnimator symbolAnimator = slotSymbol.getAnimator();
			if (symbolAnimator == null)
			{
				continue;
			}
			LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();
			if (symbolLabel != null)
			{
				symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credits, 1, shouldRoundUp: false);
			}
		}
	}
	
	private IEnumerator lockInNewSymbols(JSON[] newLockedSymbolJsonData)
	{
		List<TICoroutine> symbolAnimationCoroutines = new List<TICoroutine>();

		foreach (JSON currentSymbolLockJson in newLockedSymbolJsonData)
		{
			int reelIndex = currentSymbolLockJson.getInt("reel", -1);
			int position = currentSymbolLockJson.getInt("position", -1);

			// Might need bottom up symbols here to make this work as expected!
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);

			// Invert the position value because this is an independent reel position
			// not visible symbols indices which is what we are going to use it for
			position = (independentVisibleSymbols.Length - 1) - position;

			SlotSymbol symbolToLock = independentVisibleSymbols[position];
			
			// Animate the outcome on the lock symbol to play its locking animation
			symbolAnimationCoroutines.Add(StartCoroutine(symbolToLock.playAndWaitForAnimateOutcome()));
			
			bool isSymbolConvertedToLoopSymbol = symbolToLock.tryConvertSymbolToLoopSymbol();
			if (!isSymbolConvertedToLoopSymbol)
			{
				continue;
			}
		
			lockedLoopingSymbols.Add(symbolToLock);
			
			SlotReel reelToLock = symbolToLock.reel;
			reelToLock.isLocked = true;
		}
		
		foreach (TICoroutine currentCoroutine in allLockedLoopingSymbolCoroutines)
		{
			if (currentCoroutine != null && !currentCoroutine.finished)
			{
				StopCoroutine(currentCoroutine);
			}
		}
		allLockedLoopingSymbolCoroutines.Clear();

		foreach (SlotSymbol symbol in lockedLoopingSymbols)
		{
			symbol.haltAnimation();
			allLockedLoopingSymbolCoroutines.Add(StartCoroutine(loopAnticipationAnimationOnIndependentSymbolUntilRewards(symbol)));
		}

		if (symbolAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolAnimationCoroutines));
		}
	}
	
	private IEnumerator handleAllRewards()
	{
		// Cancel all the looped animations here as we are about to award
		stopAllLoopedOutcomeAnimationOnSymbols();
	
		isHandlingReelRewards = true;
	
		extractReelRewardPrizeAwardData();
		
		// Now that we've extracted the reward data we will try to reward each thing in a set order
		// but only if we have data to present to the player.
		
		// Handle the credit rewards first
		if (rewardDictionary.ContainsKey(ReelRewardTypeEnum.Credits) && rewardDictionary[ReelRewardTypeEnum.Credits].Count > 0)
		{
			yield return StartCoroutine(handleCreditRewards());
		}
		
		yield return StartCoroutine(handleBonusGameRewards());
		
		isHandlingReelRewards = false;
	}

	// Handle symbols that award just standard credit values
	private IEnumerator handleCreditRewards()
	{
		long totalCredits = 0;
		
		float rollupTime = valueSymbolPayoutParticleTrail.translateTime * rewardDictionary.Count;
		bool isSkippingRollup = rollupTime == -1;
		
		List<TICoroutine> creditRewardCoroutines = new List<TICoroutine>();

		foreach (ReelRewardData creditReward in rewardDictionary[ReelRewardTypeEnum.Credits])
		{
			totalCredits += creditReward.rewardCreditAmount;
		}
		
		creditRewardCoroutines.Add(StartCoroutine(payoutSymbolsAsyncCoroutine()));
		
		creditRewardCoroutines.Insert(0, StartCoroutine(rollupWinnings(totalCredits, bonusGameRollupLoopKey,
			bonusGameRollupTermKey, rollupTime, isSkippingRollup)));
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(creditRewardCoroutines));
	}
	
	private IEnumerator payoutSymbolsAsyncCoroutine()
	{
		foreach (ReelRewardData creditReward in rewardDictionary[ReelRewardTypeEnum.Credits])
		{
			// Have a short delay between each particle trails, but they shouldn't wait for each other to finish before shooting the next one
			StartCoroutine(payoutSymbolValue(creditReward, creditValueSymbolRollupLoopKey, creditValueSymbolRollupTermKey, valueSymbolPayoutParticleTrail, null));
			
			yield return new WaitForSeconds(snwScSymbolPayoutParticleTrailDelay);
		}
	}
	
	// Handle bonus game rewards from symbols
	private IEnumerator handleBonusGameRewards()
	{
		SlotBaseGame baseGame = reelGame as SlotBaseGame;

		if (!hasDelayedReevaluations)
		{
			yield break;
		}

		bool hasFreeSpins =  originalOutcome.moveDelayedReevaluationsIntoReevaluations(originalOutcome, originalReevals, true, false);
		
		if (!hasFreeSpins)
		{
			yield break;
		}

		ReelGame.activeGame.setOutcome(originalOutcome);
		
		FreeSpinsOutcome freespinsOutcome = null;
		SlotOutcome bonusGameOutcome = SlotOutcome.getBonusGameOutcome(originalOutcome, "orig012_freespin");
		if (bonusGameOutcome != null)
		{
			JSON[] bonusBaseOutcome = bonusGameOutcome.getJsonObject().getJsonArray("outcomes");
			if (bonusBaseOutcome.Length > 0)
			{
				JSON[] baseReevaluations = bonusBaseOutcome[0].getJsonArray("reevaluations");
				if (baseReevaluations.Length > 0)
				{
					bonusGameOutcome = new SlotOutcome(baseReevaluations[0]);
				}
			}

			freespinsOutcome = new FreeSpinsOutcome(bonusGameOutcome);
			freespinsOutcome.paytable = BonusGamePaytable.findPaytable("free_spin", bonusGameOutcome.getBonusGamePayTableName()); //Paytable name is still inside the baseoutcome and not the nested bonus outcomes
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = freespinsOutcome;
		}

		BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = freespinsOutcome;
		
		baseGame.createBonus(originalOutcome);

		// Trigger bonus via BonusGameManager, since we are dealing with very nested bonus outcomes
		if (baseGame.isDoingFreespinsInBasegame())
		{
			// if a bonus triggers when we are doing freespins in base, we need to stack the bonus so the freespins will be restored
			BonusGameManager.instance.showStackedBonus(isHidingSpinPanelOnPopStack:false);
		}
		else
		{
			BonusGameManager.instance.show();
		}
		isPlayingBonusGame = true;

		// wait until the bonus game is over before proceeding
		while (isPlayingBonusGame)
		{
			yield return null;
		}

		// Payout the bonus game win amount now that we are back in the base game
		long bonusPayout = BonusGameManager.instance.finalPayout;
		BonusGameManager.instance.finalPayout = 0;

		// Check if the bonus actually paid anything out (it is possible for the bonus to not award)
		if (bonusPayout > 0)
		{
			yield return StartCoroutine(reelGame.rollupCredits(0,
				bonusPayout,
				ReelGame.activeGame.onPayoutRollup,
				isPlayingRollupSounds: true,
				specificRollupTime: 0.0f,
				shouldSkipOnTouch: true,
				allowBigWin: false,
				isAddingRollupToRunningPayout: true,
				rollupOverrideSound: bonusGameRollupLoopKey,
				rollupTermOverrideSound: bonusGameRollupTermKey));

			// Base game, go ahead and pay this out right now
			reelGame.addCreditsToSlotsPlayer(bonusPayout, "blackout prog jackpot stick and win bonus game award", shouldPlayCreditsRollupSound: false);
		}
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return hasDelayedReevaluations;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		// Mark the bonus game complete so that handleBonusGameRewards can continue
		isPlayingBonusGame = false;
		yield break;
	}

	// Payout a value or jackpot symbol
	private IEnumerator payoutSymbolValue(ReelRewardData currentReward, string rollupOverrideSound, string rollupTermOverrideSound, AnimatedParticleEffect particleEffect, Transform particleEffectStartLocation)
	{
		SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(currentReward.reelIndex, 1);
		// Need to invert since symbolPos is based on independent reel position and not visible symbol index
		int visibleSymbolsIndex = (independentVisibleSymbols.Length - 1) - currentReward.symbolPos;
		SlotSymbol symbol = independentVisibleSymbols[visibleSymbolsIndex];

		if (symbol == null || symbol.isBlankSymbol)
		{
			yield break;
		}
		
		symbol.tryConvertSymbolToAwardSymbol();
		symbol.animateOutcome();

		if (particleEffect == null)
		{
			yield break;
		}
		
		// If a specific start location is not passed, then the particle effect will start from the symbol location
		if (particleEffectStartLocation == null)
		{
			particleEffectStartLocation = symbol.gameObject.transform;
		}

		yield return StartCoroutine(particleEffect.animateParticleEffect(particleEffectStartLocation));
	}

	private IEnumerator rollupWinnings(long creditsAwarded, string rollupOverrideSound, string rollupTermOverrideSound, float rollupTime = 0.0f, bool isSkippingRollup = false)
	{
		if (preRollupDelayTime > 0)
		{
			yield return new WaitForSeconds(preRollupDelayTime);
		}
		
		if (!isSkippingRollup)
		{
			yield return StartCoroutine(reelGame.rollupCredits(0,
				creditsAwarded,
				null,
				true,
				0.0f,
				true,
				false,
				true,
				rollupOverrideSound: rollupOverrideSound,
				rollupTermOverrideSound: rollupTermOverrideSound));
		}
		else
		{
			// Just add the values without rolling up
			reelGame.onPayoutRollup(creditsAwarded);
			yield return StartCoroutine(reelGame.onEndRollup(false, true));
		}

		reelGame.addCreditsToSlotsPlayer(creditsAwarded, "blackout stick and win symbol value award", shouldPlayCreditsRollupSound: false);

		if (postRollupWaitTime > 0)
		{
			yield return new TIWaitForSeconds(postRollupWaitTime);
		}
	}

	// Add a reward to the reward dictionary
	private void addRewardToRewardDictionary(ReelRewardData reward)
	{
		if (!rewardDictionary.ContainsKey(reward.rewardType))
		{
			rewardDictionary.Add(reward.rewardType, new List<ReelRewardData>());
		}

		rewardDictionary[reward.rewardType].Add(reward);
	}

	// Store out the reward data for what the player will be rewarded with
	// once the feature spinning is complete
	private void extractReelRewardPrizeAwardData()
	{
		rewardDictionary.Clear();

		JSON[] rewardDataJsonArray = null;
		foreach (SlotOutcome reevalSpinOutcome in reelGame.reevaluationSpins)
		{
			JSON currentReevalOutcomeJson = reevalSpinOutcome.getJsonObject();
			rewardDataJsonArray = currentReevalOutcomeJson.getJsonArray(JSON_REWARDS_KEY);
		}

		if (rewardDataJsonArray == null)
		{
			return;
		}
		
		for (int i = 0; i < rewardDataJsonArray.Length; i++)
		{
			JSON currentRewardDataJson = rewardDataJsonArray[i];
			string outcomeType = currentRewardDataJson.getString("outcome_type", "");
			ReelRewardData reward;

			switch (outcomeType)
			{
				case "symbol_credit":
					JSON[] symbolCreditValueDataJsonArray = currentRewardDataJson.getJsonArray("outcomes");
					foreach (JSON currentCreditValueDataJson in symbolCreditValueDataJsonArray)
					{
						reward = new ReelRewardData
						{
							rewardType = ReelRewardTypeEnum.Credits,
							reelIndex = currentCreditValueDataJson.getInt("reel", 0),
							symbolPos = currentCreditValueDataJson.getInt("pos", 0),
							symbolName = currentCreditValueDataJson.getString("symbol", ""),
							multiplier = currentCreditValueDataJson.getInt("multiplier", 1),
							rewardCreditAmount = currentCreditValueDataJson.getLong("credits", 0L)
						};

						reward.rewardCreditAmount *= reward.multiplier;
						// Factor in the multiplier (since the credit values aren't multiplied)
						reward.rewardCreditAmount *= reelGame.multiplier;
						addRewardToRewardDictionary(reward);
					}
					break;
			}
		}

		// Sort the rewards in each section (since the server doesn't force an order on them)
		foreach (KeyValuePair<ReelRewardTypeEnum, List<ReelRewardData>> kvp in rewardDictionary)
		{
			kvp.Value.Sort();
		}
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbol.isScatterSymbol)
		{
			return true;
		}
		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (!hasDelayedReevaluations)
		{
			return;
		}

		if (!didStartGameInitialization)
		{
			JSON[] modifierJSON = SlotBaseGame.instance.modifierExports;

			string stickAndWinDataKey = string.Format(JSON_STICK_AND_WIN_KEY, GameState.game.keyName);

			JSON stickAndWinJson = null;
			foreach (JSON mod in modifierJSON)
			{
				if (mod.hasKey(stickAndWinDataKey))
				{
					stickAndWinJson = mod.getJSON(stickAndWinDataKey);
					break; //Don't need to keep looping through the JSON once we have information we need
				}
			}
	
			if (stickAndWinJson != null)
			{
				if (!stickAndWinJson.hasKey(JSON_INITIAL_SCATTER_VALUES_KEY))
				{
					return;
				}

				JSON[] values = stickAndWinJson.getJsonArray(JSON_INITIAL_SCATTER_VALUES_KEY);
				foreach (JSON value in values)
				{
					if (value.hasKey("symbol")) //Check for the key before adding it into the dictionary
					{
						symbolToValue.Add(value.getString("symbol", ""), value.getLong("credits", 0));
					}
				}
			}
			else
			{
				Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
			}

			didStartGameInitialization = true;
		}
		
		if (didStartGameInitialization)
		{
			 setSymbolLabel(symbol);
		}
	}

	private void setSymbolLabel(SlotSymbol symbol)
	{
		if (symbolToValue.Count <= 0)
		{
			return;
		}

		//Only set the label on Scatter symbols that are in our dictionary.
		//If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
		long symbolCreditValue = 0;
		if (!symbolToValue.TryGetValue(symbol.serverName, out symbolCreditValue))
		{
			return;
		}

		SymbolAnimator symbolAnimator = symbol.getAnimator();
		if (symbolAnimator == null)
		{
			return;
		}

		LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

		if (symbolLabel != null)
		{
			symbolLabel.text =
				CreditsEconomy.multiplyAndFormatNumberAbbreviated(symbolCreditValue * reelGame.multiplier, 1,
					shouldRoundUp: false);
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogWarning(
				"BlackoutProgressiveJackpotStickAndWinModule.setSymbolLabel() - Unable to find LabelWrapperComponent on symbol which should have a value shown on it, symbol: " +
				symbol.serverName, symbol.gameObject);
#endif
		}
	}

	public override bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return !isHandlingReelRewards;
	}

	public override bool isAllowingShowNonBonusOutcomesToSetIsSpinComplete()
	{
		// showNonBonusOutcomes should never be able to mark the spin complete
		// since the original reevaluation spin that triggered the bonus game
		// via the feature will ultimately unlock the game once the feature
		// coroutine is done
		return false;
	}

	public override bool isModuleHandlingBigWin()
	{
		return isHandlingReelRewards;
	}

	public override IEnumerator executeTriggerBigWinBeforeSpinEnd()
	{
		isBigWinShown = true;
		// NOTE: We don't need to check if it is over the threshold because that is checked before executeTriggerBigWinBeforeSpinEnd() is called
		float rollupTime = SlotUtils.getRollupTime(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), SlotBaseGame.instance.betAmount);
		yield return StartCoroutine(SlotBaseGame.instance.forceTriggerBigWin(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), rollupTime));
	}
}
