using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module made for orig004 base game feature.  Where when enough scatter symbols are landed the reels
 * swap to independent reels and perform respins until the player runs out of spins.  Landed symbols
 * can award credits, fixed jackpots, or multiple types of bonus games.  If all symbol locations become
 * locked for a blackout, then the progressive jackpot that the player currently qualifies for is awarded.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 8/27/2020
 */
public class BlackoutProgressiveJackpotStickAndWinModule : SwapNormalToIndependentReelTypesOnReevalModule
{
	public enum ReelRewardTypeEnum
	{
		Unknown = 0,
		Credits,
		Bonus,
		Jackpot,
		ProgressiveJackpot
	}

	public enum JackpotTypeEnum
	{
		Unknown = 0,
		Mini,
		Minor,
		Major,
		Pjp
	}

	protected class ReelRewardData : System.IComparable<ReelRewardData>
	{
		public int reelIndex = 0; // Reel location for the awarding symbol (PJP blackout reward will not set this)
		public int symbolPos = 0; // Symbol location for the awarding symbol (PJP blackout reward will not set this)
		public string symbolName = "";
		public long rewardCreditAmount = 0; // Will contain the amount of credits that are awarded when the type is a jackpot or credit reward
		public ReelRewardTypeEnum rewardType = ReelRewardTypeEnum.Unknown; // What type of reward this reel is
		public JackpotTypeEnum jackpotType = JackpotTypeEnum.Unknown; // If this rewardType is Jackpot, this will contain the jackpot type so we can award the correct one (like "mini")
		public SlotOutcome bonusOutcome; // If this is a bonus type, this will contain the bonus game outcome

		public int CompareTo(ReelRewardData other)
		{
			if (this.reelIndex == other.reelIndex)
			{
				// If reel index is the same sort by the position from high to low
				return other.symbolPos.CompareTo(this.symbolPos);
			}
			else
			{
				// Sort by the reel index low to high
				return this.reelIndex.CompareTo(other.reelIndex);
			}
		}
	}

	[System.Serializable]
	protected class JackpotAnimationData
	{
		public JackpotTypeEnum jackpotType;
		[Tooltip("Animations played when a jackpot symbol value is awarded (probably want this to go to a looped state that will end when jackpotWonFinishedIdleAnims is played)")]
		public AnimationListController.AnimationInformationList celebrateJackpotWinAnims;
		[Tooltip("Animations to cancel the effects turned on with celebrateJackpotWinAnims.")]
		public AnimationListController.AnimationInformationList jackpotWonFinishedIdleAnims;
		[Tooltip("Particle trail used when paying out a jackpot value.  Control starting location by using jackpotPayoutParticleTrailStartLocation (this allows the same particle effect to be used for multiple jackpots).")]
		public AnimatedParticleEffect jackpotPayoutParticleTrail;
		[Tooltip("Starting location for the jackpotPayoutParticleTrail. This should for instance be somewhere on the value UI.  If jackpotPayoutParticleTrail is set but this is left null, then the particle trail will originate from the jackpot symbol.")]
		public Transform jackpotPayoutParticleTrailStartLocation;
	}

	[Tooltip("Animations played when the spin counter is displayed after the feature is triggered")]
	[SerializeField] private AnimationListController.AnimationInformationList displaySpinCounterAnims;
	[Tooltip("Animations played when the spin counter value is reset from a new symbol locking in")]
	[SerializeField] private AnimationListController.AnimationInformationList spinCounterResetValueAnims;
	[Tooltip("Animations played when the spin counter is hidden.  After the player runs out of spin.")]
	[SerializeField] private AnimationListController.AnimationInformationList hideSpinCounterAnims;
	[Tooltip("Label that displays the spin count.  Gets updated as the number of spins for this feature change.")]
	[SerializeField] private LabelWrapperComponent spinCountLabel;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[SerializeField] private float postRollupWaitTime;
	
	[Tooltip("Rollup loop used when awarding the progressive jackpot")]
	[SerializeField] private string progressiveJackpotRollupLoopKey = "progressive_jackpot_rollup_loop";
	[Tooltip("Rollup term used when awarding the progressive jackpot")]
	[SerializeField] private string progressiveJackpotRollupTermKey = "progressive_jackpot_rollup_end";

	[Tooltip("Rollup loop used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupTermKey = "stick_and_win_rollup_end";
	
	[Tooltip("Rollup loop used when awarding a credit value symbol")]
	[SerializeField] private string creditValueSymbolRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a credit value symbol")]
	[SerializeField] private string creditValueSymbolRollupTermKey = "stick_and_win_rollup_end";
	
	[Tooltip("Rollup loop used when awarding a jackpot symbol")]
	[SerializeField] private string jackpotSymbolRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a jackpot symbol")]
	[SerializeField] private string jackpotSymbolRollupTermKey = "stick_and_win_rollup_end";
	
	[Tooltip("Animations played for transitioning into the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList bonusGameTransitionAnimations;
	[Tooltip("Used to hide and handle stuff in the base game once the start call has been done on the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList postBonusGameStartAnimations;
	[Tooltip("Animations that will play when returning to the base game from the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList returnFromBonusGameAnimations;
	[Tooltip("Controls if the top overlay UI should be faded with the transition")]
	[SerializeField] private bool SHOULD_FADE_OVERLAY_WITH_TRANSITION = true;
	[Tooltip("Controls the fade out time of the top overlay UI")]
	[SerializeField] private float OVERLAY_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Controls the spin panel should be faded with the transition")]
	[SerializeField] private bool shouldFadeSpinPanel = true;
	[Tooltip("Controls the fade out time of the spin panel")]
	[SerializeField] private float SPIN_PANEL_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Defines animations played when a jackpot value begins and ends awarding")]
	[SerializeField] private JackpotAnimationData[] jackpotAnimationData;
	[Tooltip("Particle trail used when paying out a basic value win amount.  Will originate from the symbol and should be setup to go to the win meter.")]
	[SerializeField] private AnimatedParticleEffect valueSymbolPayoutParticleTrail;
	[Tooltip("Particle trail used when paying out a bonus game win value.  Will originate from the symbol that triggered the bonus and should be setup to go to the win meter.")]
	[SerializeField] private AnimatedParticleEffect bonusGamePayoutParticleTrail;
	[Tooltip("Animations for showing reel dividers when the feature is triggered and the game swaps to independent reels")]
	[SerializeField] private AnimationListController.AnimationInformationList showIndependentReelDividersAnimations;
	[Tooltip("Animations that play right when the reels are swapped to Independent Reels. This can be used if something needs to happen at exactly the same time as the reel swap occurs.")]
	[SerializeField] private AnimationListController.AnimationInformationList independentReelsShownAnimations;
	[Tooltip("Animations for hiding reel dividers when the feature is over and the game returns to standard reels")]
	[SerializeField] private AnimationListController.AnimationInformationList hideIndependentReelDividersAnimations;
	[Tooltip("Audio that should play when the feature is ended and the rollup phase is beginning.")]
	[SerializeField] private AudioListController.AudioInformationList featurePayoutPhaseStartSounds;
	[Tooltip("Audio that should play when a new symbol is being locked in during the stick and win feature.")]
	[SerializeField] private AudioListController.AudioInformationList newSymbolLockedSounds;

	private JSON[] reevaluationSpinNewLockedSymbolInfo;
	private bool isSpinMeterSet;
	private Dictionary<ReelRewardTypeEnum, List<ReelRewardData>> rewardDictionary = new Dictionary<ReelRewardTypeEnum, List<ReelRewardData>>(); // Dictionary of rewards to give out to the player based on what symbols land
	private bool isPlayingBonusGame = false;
	private Dictionary<string, long> symbolToValue = new Dictionary<string, long>(); //Dictionary that stores the scatter symbols and their associated credit value
	private bool didStartGameInitialization = false;
	private bool hasInitialRespinCountBeenSet = false;
	private bool isHandlingReelRewards = false; // Flag used to track if this module still needs to award the reel rewards (or is in the process of doing so) that will block the game from finishing a spin
	private Dictionary<JackpotTypeEnum, JackpotAnimationData> jackpotAnimDictionary = new Dictionary<JackpotTypeEnum,JackpotAnimationData>(); // Dictionary to lookup animation data for a given jackpot
	private BuiltInProgressiveJackpotBaseGameModule progressiveJackpotModule = null; // Stores a link to the progressive jackpot module so that we can use it to control the hooked up labels (whcih will be simpler then duplicating a lot of code)
	private int spinMeterStartAndResetValue = DEFAULT_SPIN_METER_START_AND_RESET_VALUE; // Store out what the spin meter start value is (this is also the reset value).  We will use this to determine when the spin counter is resetting
	private bool isBigWinShown = false;			// Need to track if this module has triggered a big win and doesn't need to trigger one anymore
	private bool wasFeatureTriggered = false;	// Tracks if the feature was triggered or not so that the big win knows if it should be overridden at the end of the spin (only happens for spins that trigger the feature)
	private List<TICoroutine> allLockedLoopingSymbolCoroutines = new List<TICoroutine>();
	private List<SlotSymbol> lockedLoopingSymbols = new List<SlotSymbol>();
	private bool isLoopingStandardReelSymbols = false;
	private List<TICoroutine> standardReelAreaLoopedSymbolCoroutines = new List<TICoroutine>();

	public delegate IEnumerator LoopedAnimationDelegate(SlotSymbol symbol);

	private const string JSON_REEL_LOCKING_SYMBOLS_INFO_KEY = "locked_symbols_info";
	private const string JSON_NEW_LOCKED_SYMBOLS_INFO_KEY = "new_locked_symbols_info";
	private const string JSON_SPIN_METER_START_VALUE_KEY = "spin_meter_start_value";
	private const string JSON_SPIN_METER_KEY = "spin_meter";
	private const string JSON_REWARDS_KEY = "rewards";
	private const string JSON_STICK_AND_WIN_KEY = "{0}_stick_and_win_no_prize_meters"; // needs game key appended to the front
	private const string JSON_MINI_JACKPOT_VALUE_KEY = "mini";
	private const string JSON_MINOR_JACKPOT_VALUE_KEY = "minor";
	private const string JSON_MAJOR_JACKPOT_VALUE_KEY = "major";
	private const string JSON_INITIAL_SCATTER_VALUES_KEY = "sc_symbols_value";

	private const int DEFAULT_SPIN_METER_START_AND_RESET_VALUE = 1; // Default value so game would still semi function.  However if the server data to fill spinMeterStartAndResetValue does not come down an error message will be logged (since that shouldn't happen).

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Get the starting values for the Scatter symbols in case we have any on the starting reelset and for the first spin of the game.
		// Have to read the base game values since that is the only place where the started data is stored
		// and the started data for freespins also lives in that data (not an issue for progressive games
		// since the freespins can't be gifted anyways)
		JSON[] modifierJSON = SlotBaseGame.instance.modifierExports;

		string stickAndWinDataKey = string.Format(JSON_STICK_AND_WIN_KEY, GameState.game.keyName);

		JSON stickAndWinJson = null;
		for (int i = 0; i < modifierJSON.Length; i++)
		{
			if (modifierJSON[i].hasKey(stickAndWinDataKey))
			{
				stickAndWinJson = modifierJSON[i].getJSON(stickAndWinDataKey);
				break; //Don't need to keep looping through the JSON once we have information we need
			}
		}

		if (stickAndWinJson != null)
		{
			setScatterValuesOnStart(stickAndWinJson);
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}

		foreach (JackpotAnimationData animData in jackpotAnimationData)
		{
			if (!jackpotAnimDictionary.ContainsKey(animData.jackpotType))
			{
				jackpotAnimDictionary[animData.jackpotType] = animData;
			}
			else
			{
				Debug.LogError("BlackoutProgressiveJackpotStickAndWinModule.executeOnSlotGameStarted() - Found duplicate anim data defined for animData.jackpotType = " + animData.jackpotType);
			}
		}

		// Look for the basegame BuiltInProgressiveJackpotBaseGameModule so we can use that to control the progressive labels it already manages
		// Rather than having to rebuild tons of code for handling progressive jackpots inside this module.
		progressiveJackpotModule = reelGame.getBuiltInProgressiveJackpotBaseGameModule();

		didStartGameInitialization = true;
		yield break;
	}

	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	// Base class already overrides the needs to execute and make it brute
	public override IEnumerator executeOnPreSpin()
	{
		wasFeatureTriggered = false;
		isBigWinShown = false;
		yield return StartCoroutine(base.executeOnPreSpin());
	}

	// Parse data about what the scatters are worth
	private void setScatterValuesOnStart(JSON stickAndWinJson)
	{
		if (stickAndWinJson.hasKey(JSON_INITIAL_SCATTER_VALUES_KEY))
		{
			JSON[] values = stickAndWinJson.getJsonArray(JSON_INITIAL_SCATTER_VALUES_KEY);
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].hasKey("symbol")) //Check for the key before adding it into the dictionary
				{
					symbolToValue.Add(values[i].getString("symbol", ""), values[i].getLong("credits", 0));
				}
			}
		}
	}

	private IEnumerator setSpinCountMessageText(int count)
	{
		if (hasInitialRespinCountBeenSet && count == spinMeterStartAndResetValue)
		{
			// Play animations for the spin count resetting
			if (spinCounterResetValueAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(spinCounterResetValueAnims));
			}
		}

		if (spinCountLabel != null)
		{
			spinCountLabel.text = CommonText.formatNumber(count);
		}
		else
		{
			// Fallback to the built in spin panel message text if we don't have a custom label to display the count to
			if (reelGame is SlotBaseGame)
			{
				if (count > 1)
				{
					((SlotBaseGame) reelGame).setMessageText(Localize.text("{0}_spins_remaining", count));
				}
				else
				{
					((SlotBaseGame) reelGame).setMessageText(Localize.text("good_luck_last_spin"));
				}
			}

			SpinPanel.instance.slideInPaylineMessageBox();
		}
	}

	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		wasFeatureTriggered = true;

		// Fade out non-scatters symbols
		List<TICoroutine> independentReelIntroCoroutines = new List<TICoroutine>();
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		List<SlotSymbol> symbolsFaded = new List<SlotSymbol>();

		// Fade out the non triggering symbols and animate the triggering ones (transitioning into looping animations if they have them)
		isLoopingStandardReelSymbols = true;
		for (int i = 0; i < allVisibleSymbols.Count; i++)
		{
			SlotSymbol currentSymbol = allVisibleSymbols[i];

			if (currentSymbol != null)
			{
				if (!currentSymbol.isScatterSymbol && !currentSymbol.isBonusSymbol)
				{
					// Not scatter symbol so fade this symbol out

					// Need to check for if we've already handled fading a tall/mega symbol as part of a previous part of it
					if (!symbolsFaded.Contains(currentSymbol))
					{
						List<SlotSymbol> allSymbolParts = currentSymbol.getAllSymbolParts();
						for (int k = 0; k < allSymbolParts.Count; k++)
						{
							symbolsFaded.Add(allSymbolParts[k]);
						}

						independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.fadeOutSymbolCoroutine(1.0f)));
					}
				}
				else
				{
					// Play outcome anims on SC symbols here to indicate that the feature is starting and those symbols are locking
					independentReelIntroCoroutines.Add(StartCoroutine(playAndWaitForOutcomeAnimationForStandardReelsIntoLoopAnimation(currentSymbol)));
				}
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
		for (int i = 0; i < symbolsFaded.Count; i++)
		{
			SlotSymbol currentSymbol = symbolsFaded[i];
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
				if (independentSymbol.serverName != visibleSymbols[symbolIndex].serverName)
				{
					independentSymbol.mutateTo(visibleSymbols[symbolIndex].serverName, null, false, true);
					symbolsToTryToStartLooping.Add(independentSymbol);
					
					// Lock in any scatter symbols that were already landed
					if (independentSymbol.isScatterSymbol || independentSymbol.isBonusSymbol)
					{
						SlotReel reelToLock = independentSymbol.reel;
						reelToLock.isLocked = true;
					}
				}
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

		if (independentReelsShownAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(independentReelsShownAnimations));
		}
	}
	
	// Function that blocks while an outcome animation is played on a trigger symbol of the standard reels.
	// Once that blocking is done it checks if the symbol has a _Loop version to play and spawns a new coroutine
	// to manage that until the reels are swapped to independent reels (where the loop will be continued by the symbols
	// on the independent reels).
	private IEnumerator playAndWaitForOutcomeAnimationForStandardReelsIntoLoopAnimation(SlotSymbol symbol)
	{
		// Play the sounds for the new symbol locking
		if (newSymbolLockedSounds.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(newSymbolLockedSounds));
		}
		yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		
		// Trigger the loop on the symbol which will wait until the reel type swap to terminate
		bool isSymbolConvertedToLoopSymbol = symbol.tryConvertSymbolToLoopSymbol();
		if (isSymbolConvertedToLoopSymbol)
		{
			standardReelAreaLoopedSymbolCoroutines.Add(StartCoroutine(loopAnticipationAnimationOnStandardSymbolUntilReelTypeSwap(symbol)));
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
		if (isSymbolConvertedToLoopSymbol)
		{
			lockedLoopingSymbols.Add(symbol);
			allLockedLoopingSymbolCoroutines.Add(StartCoroutine(loopAnticipationAnimationOnIndependentSymbolUntilRewards(symbol)));
		}
	}

	private IEnumerator loopAnticipationAnimationOnIndependentSymbolUntilRewards(SlotSymbol symbol)
	{
		// This condition is just a fallback, I think for now we'll actually still just force cancel
		// the animations and kill the coroutines when the award part of the code starts
		while (!isHandlingReelRewards)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
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
			if (!didConvertSymbol)
			{
				didConvertSymbol = symbol.tryConvertSymbolToOutcomeSymbol(false);
				
				// if neither _Award or _Outcome are set for this symbol, just convert it back into a standard non _Loop version of the symbol
				if (!didConvertSymbol)
				{
					symbol.mutateTo(symbol.serverName, null, false, true);
				}
			}
		}
		lockedLoopingSymbols.Clear();
	}
	
	protected override IEnumerator swapSymbolsBackToNormalReels()
	{
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
				if (independentSymbol.serverName != currentRegularSymbol.serverName)
				{
					currentRegularSymbol.mutateTo(independentSymbol.serverName, null, false, true);
					
					// if this isn't a blank symbol, copy the value back onto it
					if (!currentRegularSymbol.isBlankSymbol)
					{
						// Check if this is a symbol with a value on it, in which case we need to transfer the value data
						setSymbolLabel(currentRegularSymbol);
					}
				}
			}
		}
		
		// Turn off the reel dividers for independent reels
		if (hideIndependentReelDividersAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideIndependentReelDividersAnimations));
		}
		
		showLayer(LAYER_INDEX_NORMAL_REELS);
		
		// Convert all symbols on the independent reels to be BL so they are blank
		// the next time we trigger the feature.
		SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(1);
		for (int reelIndex = 0; reelIndex < independentReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = independentReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;

			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				if (!visibleSymbols[symbolIndex].isBlankSymbol)
				{
					visibleSymbols[symbolIndex].mutateTo("BL", null, false, true);
				}
			}
		}

		yield break;
	}
	
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		yield return StartCoroutine(animateAnticipationOnTriggerSymbols(reel));
	}
	
	public IEnumerator animateAnticipationOnTriggerSymbols(SlotReel reel)
	{
		// If we have a scatter symbol we want to play sound
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			SlotSymbol currentSymbol = reel.visibleSymbols[i];
		
			if (currentSymbol.isScatterSymbol || currentSymbol.isBonusSymbol)
			{
				currentSymbol.animateAnticipation();
			}
		}
		
		yield break;
	}

	// executeOnReevaluationPreSpin() section
	// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
	// and triggers before the reels begin spinning
	public override IEnumerator executeOnReevaluationPreSpin()
	{
		yield return StartCoroutine(base.executeOnReevaluationPreSpin());

		if (!hasInitialRespinCountBeenSet)
		{
			JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
			currentReevalOutcomeJson.validateHasKey(JSON_SPIN_METER_START_VALUE_KEY);
			spinMeterStartAndResetValue = currentReevalOutcomeJson.getInt(JSON_SPIN_METER_START_VALUE_KEY, DEFAULT_SPIN_METER_START_AND_RESET_VALUE);
			yield return StartCoroutine(setSpinCountMessageText(spinMeterStartAndResetValue));

			// Show the spin counter now that the initial value is set to display on it
			if (displaySpinCounterAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(displaySpinCounterAnims));
			}
			
			hasInitialRespinCountBeenSet = true;
		}
	}

	// executeOnReevaluationReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		// Extract the info we need to use for displaying how the current spin will function
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		JSON lockedSymbolInfoJson = currentReevalOutcomeJson.getJSON(JSON_REEL_LOCKING_SYMBOLS_INFO_KEY);
		reevaluationSpinNewLockedSymbolInfo = lockedSymbolInfoJson.getJsonArray(JSON_NEW_LOCKED_SYMBOLS_INFO_KEY);
		// If there are reevaluations then we want to do this.
		return true;
	}

	private IEnumerator lockInNewSymbols(JSON[] newLockedSymbolJsonData)
	{
		List<TICoroutine> symbolAnimationCoroutines = new List<TICoroutine>();
	
		for (int i = 0; i < newLockedSymbolJsonData.Length; i++)
		{
			JSON currentSymbolLockJson = newLockedSymbolJsonData[i];
			int reelIndex = currentSymbolLockJson.getInt("reel", -1);
			int position = currentSymbolLockJson.getInt("position", -1);

			// Might need bottom up symbols here to make this work as expected!
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, 1);

			// Invert the position value because this is an independent reel position
			// not visible symbols indices which is what we are going to use it for
			position = (independentVisibleSymbols.Length - 1) - position;

			SlotSymbol symbolToLock = independentVisibleSymbols[position];
			
			// Play the sounds for the new symbol locking
			if (newSymbolLockedSounds.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(newSymbolLockedSounds));
			}
			
			// Animate the outcome on the lock symbol to play its locking animation
			symbolAnimationCoroutines.Add(StartCoroutine(symbolToLock.playAndWaitForAnimateOutcome()));
			
			tryStartLoopedLockAnimationOnSymbol(symbolToLock);
			
			SlotReel reelToLock = symbolToLock.reel;
			reelToLock.isLocked = true;
		}

		if (symbolAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolAnimationCoroutines));
		}
	}
	
	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		int spinMeterCurrentValue = currentReevalOutcomeJson.getInt(JSON_SPIN_METER_KEY, spinMeterStartAndResetValue);
		yield return StartCoroutine(setSpinCountMessageText(spinMeterCurrentValue));
	
		// Handle locking in new symbols
		if (reevaluationSpinNewLockedSymbolInfo != null && reevaluationSpinNewLockedSymbolInfo.Length > 0)
		{
			yield return StartCoroutine(lockInNewSymbols(reevaluationSpinNewLockedSymbolInfo));
		}

		// If this is the last reevaluation spin then we need to handle the payout now
		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			// Allow for audio changes as we switch to the payout phase of the feature
			if (featurePayoutPhaseStartSounds.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(featurePayoutPhaseStartSounds));
			}
		
			yield return StartCoroutine(handleAllRewards());
			// reset this for the next time the feature is triggered
			hasInitialRespinCountBeenSet = false;
			
			// Hide the spin counter here
			if (hideSpinCounterAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideSpinCounterAnims));
			}
		}
	}

	// Main function for handling all types of rewards possible with this feature in the correct order
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
		
		// Next handle jackpot rewards
		if (rewardDictionary.ContainsKey(ReelRewardTypeEnum.Jackpot) && rewardDictionary[ReelRewardTypeEnum.Jackpot].Count > 0)
		{
			yield return StartCoroutine(handleJackpotRewards());
		}
		
		// Next handle the Progressive Jackpot (if a blackout happened and we got data that it should award)
		if (rewardDictionary.ContainsKey(ReelRewardTypeEnum.ProgressiveJackpot) && rewardDictionary[ReelRewardTypeEnum.ProgressiveJackpot].Count > 0)
		{
			yield return StartCoroutine(handleProgressiveJackpotReward());
		}

		// Finally award the bonus games won, one at a time
		if (rewardDictionary.ContainsKey(ReelRewardTypeEnum.Bonus) && rewardDictionary[ReelRewardTypeEnum.Bonus].Count > 0)
		{
			yield return StartCoroutine(handleBonusGameRewards());
		}
		
		isHandlingReelRewards = false;
	}

	// Handle symbols that award just standard credit values
	private IEnumerator handleCreditRewards()
	{
		foreach (ReelRewardData creditReward in rewardDictionary[ReelRewardTypeEnum.Credits])
		{
			yield return StartCoroutine(payoutSymbolValue(creditReward, creditValueSymbolRollupLoopKey, creditValueSymbolRollupTermKey, valueSymbolPayoutParticleTrail, null));
		}
	}

	// Handle symbols that award jackpot credit values
	private IEnumerator handleJackpotRewards()
	{
		foreach (ReelRewardData jackpotReward in rewardDictionary[ReelRewardTypeEnum.Jackpot])
		{
			JackpotAnimationData jackpotAnimData = getJackpotAnimationDataForJackpot(jackpotReward.jackpotType);

			if (jackpotAnimData != null && jackpotAnimData.celebrateJackpotWinAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.celebrateJackpotWinAnims));
			}
			
			if (jackpotAnimData != null)
			{
				yield return StartCoroutine(payoutSymbolValue(jackpotReward, jackpotSymbolRollupLoopKey, jackpotSymbolRollupTermKey, jackpotAnimData.jackpotPayoutParticleTrail, jackpotAnimData.jackpotPayoutParticleTrailStartLocation));
			}
			else
			{
				yield return StartCoroutine(payoutSymbolValue(jackpotReward, jackpotSymbolRollupLoopKey, jackpotSymbolRollupTermKey, null, null));
			}

			if (jackpotAnimData != null && jackpotAnimData.jackpotWonFinishedIdleAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.jackpotWonFinishedIdleAnims));
			}
		}
	}
	
	// Handle progressive jackpot award (if the player blacks out the reels)
	private IEnumerator handleProgressiveJackpotReward()
	{
		foreach (ReelRewardData progJackpotReward in rewardDictionary[ReelRewardTypeEnum.ProgressiveJackpot])
		{
			JackpotAnimationData jackpotAnimData = getJackpotAnimationDataForJackpot(progJackpotReward.jackpotType);

			if (jackpotAnimData != null && jackpotAnimData.celebrateJackpotWinAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.celebrateJackpotWinAnims));
			}
			
			// Extract the progressive jackpot amount, and award that
			JSON progJackpotWonJson = SlotBaseGame.instance.outcome.getProgressiveJackpotWinJson();
			if (progJackpotWonJson != null)
			{
				long finalJackpotCreditAward = progJackpotWonJson.getLong("running_total", 0);
					
				// Set the constantly rolling up progressive value label to the final value won as we are about to pay it out
				if (progressiveJackpotModule != null)
				{
					progressiveJackpotModule.setProgressiveJackpotValueLabelsToJackpotWinAmount(finalJackpotCreditAward);
				}
					
				yield return StartCoroutine(reelGame.rollupCredits(0, 
					finalJackpotCreditAward, 
					ReelGame.activeGame.onPayoutRollup, 
					isPlayingRollupSounds: true,
					specificRollupTime: 0.0f,
					shouldSkipOnTouch: true,
					allowBigWin: false,
					isAddingRollupToRunningPayout: true,
					rollupOverrideSound: progressiveJackpotRollupLoopKey,
					rollupTermOverrideSound: progressiveJackpotRollupTermKey));
					
				yield return StartCoroutine(reelGame.onEndRollup(false));
					
				// Base game, go ahead and pay this out right now
				reelGame.addCreditsToSlotsPlayer(finalJackpotCreditAward, "blackout prog jackpot stick and win progressive jackpot award", shouldPlayCreditsRollupSound: false);
					
				// Reregister the jackpot labels to show the rolling up progressive amount
				if (progressiveJackpotModule != null)
				{
					progressiveJackpotModule.registerProgressiveJackpotLabels();
				}
			}
			
			if (jackpotAnimData != null && jackpotAnimData.jackpotWonFinishedIdleAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.jackpotWonFinishedIdleAnims));
			}
		}
	}
	
	// Handle bonus game rewards from symbols
	private IEnumerator handleBonusGameRewards()
	{
		 SlotBaseGame baseGame = reelGame as SlotBaseGame;
		
		foreach (ReelRewardData bonusReward in rewardDictionary[ReelRewardTypeEnum.Bonus])
		{
			// First animate the symbol so we know what is awarding the bonus that we are about to trigger
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(bonusReward.reelIndex, 1);
			// Need to invert since symbolPos is based on independent reel position and not visible symbol index
			int visibleSymbolsIndex = (independentVisibleSymbols.Length - 1) - bonusReward.symbolPos;
			SlotSymbol symbol = independentVisibleSymbols[visibleSymbolsIndex];
			
			// Check if there is an _Award symbol to swap to before animating the symbol for the award
			symbol.tryConvertSymbolToAwardSymbol();
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		
			// We need to set this here, because we may have loaded multiple freespins but can only track
			// one at a time.  So it will have the last freespins processed, not the actual one we might
			// need to go into.
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusReward.bonusOutcome);
			string bonusGameName = bonusReward.bonusOutcome.getBonusGame();
			BonusGame thisBonusGame = BonusGame.find(bonusGameName);
			BonusGameManager.instance.summaryScreenGameName = bonusGameName;
			BonusGameManager.instance.isGiftable = thisBonusGame.gift;
			
			// Handle the transition stuff like fading off the UI panels and playing transition animations
			yield return StartCoroutine(playBonusGameTransitionAnimations());

			baseGame.createBonus(bonusReward.bonusOutcome);

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

			yield return StartCoroutine(playPostBonusGameStartAnimations());

			// wait until the bonus game is over before proceeding
			while (isPlayingBonusGame)
			{
				yield return null;
			}

			// Handle transition back stuff
			yield return StartCoroutine(playReturnFromBonusGameAnimations());

			// Payout the bonus game win amount now that we are back in the base game
			long bonusPayout = BonusGameManager.instance.finalPayout;
			BonusGameManager.instance.finalPayout = 0;

			// Check if the bonus actually paid anything out (it is possible for the bonus to not award)
			if (bonusPayout > 0)
			{
				if (bonusGamePayoutParticleTrail != null)
				{
					yield return StartCoroutine(bonusGamePayoutParticleTrail.animateParticleEffect(symbol.gameObject.transform));
				}

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
	}

	// Play the bonus game transition animations
	private IEnumerator playBonusGameTransitionAnimations()
	{
		List<TICoroutine> bonusGameIntroCoroutines = new List<TICoroutine>();

		if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
		{
			bonusGameIntroCoroutines.Add(StartCoroutine(fadeOutOverlay()));
		}

		if (shouldFadeSpinPanel)
		{
			bonusGameIntroCoroutines.Add(StartCoroutine(fadeOutSpinPanel()));
		}

		// Do bonus game transition anims before starting the bonus
		if (bonusGameTransitionAnimations.Count > 0)
		{
			bonusGameIntroCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusGameTransitionAnimations)));
		}

		// Wait for all of the bonus intro coroutines that can trigger together to finish
		if (bonusGameIntroCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(bonusGameIntroCoroutines));
		}
	}

	// Play animations after the bonus game transition has happened and the bonus game is actually being shown.
	// Does things like restores the UI, as well as allows additional animations to be played to control the state of stuff.
	private IEnumerator playPostBonusGameStartAnimations()
	{
		if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
		{
			Overlay.instance.fadeInNow();
		}

		// Restore spin panel stuff so that it appears in the bonus game
		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.restoreAlpha();
		}

		if (postBonusGameStartAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(postBonusGameStartAnimations));
		}
	}

	// Handles stuff for the UI and playing animations that need to occur when the game returns from a bonus game.
	private IEnumerator playReturnFromBonusGameAnimations()
	{
		if (SHOULD_FADE_OVERLAY_WITH_TRANSITION)
		{
			Overlay.instance.top.show(true);
		}

		if (shouldFadeSpinPanel)
		{
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame())
			{
				SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
			}
			else
			{
				SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
			}
		}

		// Play return from bonus anims
		if (returnFromBonusGameAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(returnFromBonusGameAnimations));
		}
	}

	// executeOnBonusGameEnded() section
	// functions here are called by the SlotBaseGame onBonusGameEnded() function
	// usually used for reseting transition stuff
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
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

		if (symbol != null && !symbol.isBlankSymbol)
		{
			symbol.tryConvertSymbolToAwardSymbol();
			symbol.animateOutcome();

			if (particleEffect != null)
			{
				// If a specific start location is not passed, then the particle effect will start from the symbol location
				if (particleEffectStartLocation == null)
				{
					particleEffectStartLocation = symbol.gameObject.transform;
				}

				yield return StartCoroutine(particleEffect.animateParticleEffect(particleEffectStartLocation));
			}

			float rollupTime = regularSymbolRollupTime;

			bool isSkippingRollup = (rollupTime == -1) ? true : false;
			yield return StartCoroutine(rollupWinnings(currentReward.rewardCreditAmount, rollupOverrideSound, rollupTermOverrideSound, rollupTime, isSkippingRollup));
		}
	}

	private IEnumerator rollupWinnings(long creditsAwarded, string rollupOverrideSound, string rollupTermOverrideSound, float rollupTime = 0.0f, bool isSkippingRollup = false)
	{
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

		reelGame.addCreditsToSlotsPlayer(creditsAwarded, "blackout prog jackpot stick and win symbol value award", shouldPlayCreditsRollupSound: false);

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

		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		JSON[] rewardDataJsonArray = currentReevalOutcomeJson.getJsonArray(JSON_REWARDS_KEY);

		for (int i = 0; i < rewardDataJsonArray.Length; i++)
		{
			JSON currentRewardDataJson = rewardDataJsonArray[i];
			string outcomeType = currentRewardDataJson.getString("outcome_type", "");
			ReelRewardData reward;

			switch (outcomeType)
			{
				case "symbol_credit":
					JSON[] symbolCreditValueDataJsonArray = currentRewardDataJson.getJsonArray("outcomes");
					for (int creditRewardIndex = 0; creditRewardIndex < symbolCreditValueDataJsonArray.Length; creditRewardIndex++)
					{
						JSON currentCreditValueDataJson = symbolCreditValueDataJsonArray[creditRewardIndex];

						reward = new ReelRewardData();
						reward.rewardType = ReelRewardTypeEnum.Credits;
						reward.reelIndex = currentCreditValueDataJson.getInt("reel", 0);
						reward.symbolPos = currentCreditValueDataJson.getInt("pos", 0);
						reward.symbolName = currentCreditValueDataJson.getString("symbol", "");
						reward.rewardCreditAmount = currentCreditValueDataJson.getLong("credits", 0L);
						// Factor in the multiplier (since the credit values aren't multiplied)
						reward.rewardCreditAmount *= reelGame.multiplier;
						addRewardToRewardDictionary(reward);
					}
					break;

				case "jackpot":
					reward = new ReelRewardData();
					reward.jackpotType = getJackpotTypeFromString(currentRewardDataJson.getString("type", ""));

					if (reward.jackpotType == JackpotTypeEnum.Pjp)
					{
						reward.rewardType = ReelRewardTypeEnum.ProgressiveJackpot;
					}
					else
					{
						reward.rewardType = ReelRewardTypeEnum.Jackpot;
					}

					reward.reelIndex = currentRewardDataJson.getInt("reel", 0);
					reward.symbolPos = currentRewardDataJson.getInt("pos", 0);
					reward.rewardCreditAmount = currentRewardDataJson.getLong("credits", 0L);
					// Factor in the multiplier (since the credit values aren't multiplied)
					reward.rewardCreditAmount *= reelGame.multiplier;
					addRewardToRewardDictionary(reward);
					break;

				case "bonus_game":
					reward = new ReelRewardData();
					reward.rewardType = ReelRewardTypeEnum.Bonus;
					reward.reelIndex = currentRewardDataJson.getInt("reel", 0);
					reward.symbolPos = currentRewardDataJson.getInt("pos", 0);
					reward.bonusOutcome = new SlotOutcome(currentRewardDataJson);
					reward.bonusOutcome.processBonus();
					addRewardToRewardDictionary(reward);
					break;
			}
		}

		// Sort the rewards in each section (since the server doesn't force an order on them)
		foreach (KeyValuePair<ReelRewardTypeEnum, List<ReelRewardData>> kvp in rewardDictionary)
		{
			kvp.Value.Sort();
		}
	}

	// Using the jackpot string sent down by the server convert it into our JackpotTypeEnum
	private JackpotTypeEnum getJackpotTypeFromString(string jackpotTypeStr)
	{
		switch (jackpotTypeStr)
		{
			case "mini":
				return JackpotTypeEnum.Mini;

			case "minor":
				return JackpotTypeEnum.Minor;

			case "major":
				return JackpotTypeEnum.Major;

			case "pjp":
				return JackpotTypeEnum.Pjp;

			default:
				Debug.LogError("BlackoutProgressiveJackpotStickAndWinModule.getJackpotTypeFromString() - Unknown jackpot type: " + jackpotTypeStr);
				return JackpotTypeEnum.Unknown;
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
		if (didStartGameInitialization)
		{
			setSymbolLabel(symbol);
		}
	}

	private void setSymbolLabel(SlotSymbol symbol)
	{
		if (symbolToValue.Count > 0)
		{
			//Only set the label on Scatter symbols that are in our dictionary.
			//If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
			long symbolCreditValue = 0;
			if (symbolToValue.TryGetValue(symbol.serverName, out symbolCreditValue))
			{
				SymbolAnimator symbolAnimator = symbol.getAnimator();
				if (symbolAnimator != null)
				{
					LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

					if (symbolLabel != null)
					{
						symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(symbolCreditValue * reelGame.multiplier, 1, shouldRoundUp: false);
					}
					else
					{
#if UNITY_EDITOR
						Debug.LogError("BlackoutProgressiveJackpotStickAndWinModule.setSymbolLabel() - Unable to find LabelWrapperComponent on symbol which should have a value shown on it, symbol: " + symbol.serverName, symbol.gameObject);		
#endif
					}
				}
			}
		}
	}

	// special function which hopefully shouldn't be used by a lot of modules
	// but this will allow for the game to not continue when the reels stop during
	// special features.  This is required for the rhw01 type of game with the
	// SC feature which does respins which shouldn't allow the game to unlock
	// even on the last spin since the game should unlock when it returns to the
	// normal game state.
	public override bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return !isHandlingReelRewards;
	}

	// Very similar to onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	// however this blocks the spin being marked complete when returning from a bonus
	// game.  Can be useful for features like got01 where the feature can trigger
	// multiple bonuses that all need to resolve before the spin is actually complete
	public override bool isAllowingShowNonBonusOutcomesToSetIsSpinComplete()
	{
		// showNonBonusOutcomes should never be able to mark the spin complete
		// since the original reevaluation spin that triggered the bonus game
		// via the feature will ultimately unlock the game once the feature
		// coroutine is done
		return false;
	}

	// Function for handling the top overlay fade
	private IEnumerator fadeOutOverlay()
	{
		TICoroutine overlayFadeCorotuine = StartCoroutine(Overlay.instance.fadeOut(OVERLAY_TRANSITION_FADE_TIME));

		while (overlayFadeCorotuine != null && !overlayFadeCorotuine.finished)
		{
			yield return null;
		}

		Overlay.instance.top.show(false);
	}

	// Function for handling the spin panel fade
	private IEnumerator fadeOutSpinPanel()
	{
		TICoroutine spinPanelFadeCoroutine = StartCoroutine(SpinPanel.instance.fadeOut(SPIN_PANEL_TRANSITION_FADE_TIME));

		while (spinPanelFadeCoroutine != null && !spinPanelFadeCoroutine.finished)
		{
			yield return null;
		}

		SpinPanel.instance.hidePanels();
	}

	// Get the animation data for a specific animation type
	private JackpotAnimationData getJackpotAnimationDataForJackpot(JackpotTypeEnum jackpotType)
	{
		JackpotAnimationData animData = null;
		jackpotAnimDictionary.TryGetValue(jackpotType, out animData);
		return animData;
	}

	// needsToTriggerBigWinBeforeSpinEnd() section
	// allows the big win to be delayed, by returning true from isModuleHandlingBigWin
	// the big win will then be custom triggered by the module when executeTriggerBigWinBeforeSpinEnd is called from continueWhenReady
	public override bool isModuleHandlingBigWin()
	{
		// controls if the big win should be delayed
		// NOTE: This needs to return false at some point after return true once a module determines the big win can occur, otherwise big wins will not trigger
		return isHandlingReelRewards;
	}

	public override bool needsToTriggerBigWinBeforeSpinEnd()
	{
		// The big win should not show until the reward display is over
		if (isModuleHandlingBigWin())
		{
			return false;
		}

		// Make sure we only show the big win once
		if (isBigWinShown)
		{
			return false;
		}

		return wasFeatureTriggered;
	}

	public override IEnumerator executeTriggerBigWinBeforeSpinEnd()
	{
		isBigWinShown = true;
		// Launch a big win that rolls up everything won from zero
		// NOTE: We don't need to check if it is over the threshold because that is checked before executeTriggerBigWinBeforeSpinEnd() is called
		float rollupTime = SlotUtils.getRollupTime(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), SlotBaseGame.instance.betAmount);
		yield return StartCoroutine(SlotBaseGame.instance.forceTriggerBigWin(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), rollupTime));
	}
}
