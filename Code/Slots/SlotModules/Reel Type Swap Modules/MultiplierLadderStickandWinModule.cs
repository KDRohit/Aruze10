using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module made for elvira07 Stick and Win feature (exists in base and free spins).
 * Where when enough scatter symbols are landed,
 * the reels swap to independent reels and perform re-spins until the player runs out of spins or all symbol spaces are filled.
 * Feature rewards multipliers according to landed Scatter symbol count. Such reward tiers are presented to the player through ladder VFX. 
 * This class also handles a nested picking game that rewards multiplier / extra snw spins.
 * 
 * Original Author: Xueer Zhu <xzhu@zynga.com>
 * Date: 06/11/2021
 */
public class MultiplierLadderStickandWinModule : SwapNormalToIndependentReelTypesOnReevalModule
{
	[Header("Feature Components"), Space(5)]
	[Tooltip("Label that displays the spin count.  Gets updated as the number of spins for this feature change.")]
	[SerializeField] private LabelWrapperComponent spinCountLabel;
	
	[Header("Pick Game Components"), Space(5)]
	[Tooltip("Picking game bonus presenter used in free spins version of this feature.")]
	[SerializeField] private BonusGamePresenter pickGameBonusPresenter;
	[Tooltip("Picking game nested in free spins version of this feature.")]
	[SerializeField] private ModularChallengeGame pickGame;
	
	[Header("VFX Data"), Space(5)]
	[Tooltip("Contains animation data for rungs on ladder")]
	[SerializeField] private List<Rung> rungsData;
	
	// reel swap
	[Tooltip("Animations that play right when the reels are swapped to Independent Reels. This can be used if something needs to happen at exactly the same time as the reel swap occurs.")]
	[SerializeField] private AnimationListController.AnimationInformationList independentReelsShownAnimations;
	[Tooltip("Animations for hiding reel dividers when the feature is over and the game returns to standard reels")]
	[SerializeField] private AnimationListController.AnimationInformationList hideIndependentReelDividersAnimations;
	
	// special spin panel and spin counter
	[Tooltip("Controls the normal spin panel should be faded out before entering snw feature")]
	[SerializeField] private bool shouldSwapOutNormalSpinPanel = true; 
	[Tooltip("Controls the slide out time of the normal spin panel during its swap to snw version spin panel")]
	[SerializeField] private float NORMAL_SPIN_PANEL_EXIT_TIME = 0.25f;
	[Tooltip("Controls the slide in time of the special spin panel during its swap to snw version spin panel")]
	[SerializeField] private float SPECIAL_SPIN_PANEL_ENTER_TIME = 0.25f;
	[Tooltip("Animations played when the spin counter is displayed after the feature is triggered")]
	[SerializeField] private AnimationListController.AnimationInformationList displaySpinCounterAnims;
	[Tooltip("Animations played when the spin counter value is reset from a new symbol locking in")]
	[SerializeField] private AnimationListController.AnimationInformationList spinCounterResetValueAnims;
	[Tooltip("Animations played when the spin counter is hidden.  After the player runs out of spin.")]
	[SerializeField] private AnimationListController.AnimationInformationList hideSpinCounterAnims;
	
	// ladder
	[Tooltip("Particle trail used when unlocking a new rung on ladder.  Will originate from the symbol and should be setup to go to the target rung.")]
	[SerializeField] private AnimatedParticleEffect ladderRungUnlockParticleTrail;
	[Tooltip("Controls long the animation is for activating one rung on the ladder")]
	[SerializeField] private float RUNG_ACTIVATE_DURATION = 0.3f;
	[Tooltip("Controls long the animation is for turning off one rung on the ladder")]
	[SerializeField] private float RUNG_TURN_OFF_DURATION = 0.15f;

	// payout
	[Tooltip("Particle trail used when paying out a basic value win amount.  Will originate from the won multiplier rung on the ladder and should be setup to go to the win meter.")]
	[SerializeField] private AnimatedParticleEffect valueMultiplierPayoutParticleTrail;
	[Tooltip("Particle trail used when multiplying player's bet amount.  Will originate from the bet amount box and should be setup to go to the jackpot.")]
	[SerializeField] private AnimatedParticleEffect betAmountToMultiplierParticleTrail;
	[Tooltip("Animations played when we change the multiplier to credit value by multiplying the bet amount.")]
	[SerializeField] private AnimationListController.AnimationInformationList changeMultiplierToCreditAnims;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[SerializeField] private float postRollupWaitTime;
	
	[Tooltip("Rollup loop used when awarding a multiplier value")]
	[SerializeField] private string multiplierValueRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a multiplier value")]
	[SerializeField] private string multiplierValueRollupTermKey = "stick_and_win_rollup_end";
	
	[Tooltip("Animations played for transitioning into the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList bonusGameTransitionAnimations;
	[Tooltip("Used to hide and handle stuff in the base game once the start call has been done on the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList postBonusGameStartAnimations;
	[Tooltip("Animations that will play when returning to the base game from the bonus game")]
	[SerializeField] private AnimationListController.AnimationInformationList returnFromBonusGameAnimations;
	[Tooltip("Controls if the top overlay UI should be faded with the transition")]
	[SerializeField] private bool shouldFadeOverlayWithTransition = true;
	[Tooltip("Controls the fade out time of the top overlay UI")]
	[SerializeField] private float OVERLAY_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Controls the spin panel should be faded with the transition")]
	[SerializeField] private bool shouldFadeSpinPanel = true;
	[Tooltip("Controls the fade out time of the spin panel")]
	[SerializeField] private float SPIN_PANEL_TRANSITION_FADE_TIME = 0.25f;
	[Tooltip("Rollup loop used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupLoopKey = "stick_and_win_rollup_loop";
	[Tooltip("Rollup term used when awarding a bonus game payout")]
	[SerializeField] private string bonusGameRollupTermKey = "stick_and_win_rollup_end";
	[Tooltip("Particle trail used when paying out a bonus game win value.  Will originate from the symbol that triggered the bonus and should be setup to go to the win meter.")]
	[SerializeField] private AnimatedParticleEffect bonusGamePayoutParticleTrail;
	
	[Tooltip("Labels that show the player what multiplier values they could win if they play the multiplier pick game triggered from snw in free spins")]
	[SerializeField] private LabelWrapperComponent[] multiplierLabels;
	
	[Header("SFX Data"), Space(5)]
	[Tooltip("Audio that should play when a new symbol is being locked in during the stick and win feature.")]
	[SerializeField] private AudioListController.AudioInformationList newSymbolLockedSounds;
	[Tooltip("Audio that should play when the a new rung is unlocked on the ladder.")]
	[SerializeField] private AudioListController.AudioInformationList ladderRungUnlockSounds;
	[Tooltip("Audio that should play when the feature is ended and the rollup phase is beginning.")]
	[SerializeField] private AudioListController.AudioInformationList featurePayoutPhaseStartSounds;

	private Dictionary<int, int> scatterNumberToMultiplierValue = new Dictionary<int, int>();
	private List<TICoroutine> standardReelAreaLoopedSymbolCoroutines = new List<TICoroutine>();
	
	private List<SlotSymbol> lockedLoopingSymbols = new List<SlotSymbol>();
	private List<TICoroutine> allLockedLoopingSymbolCoroutines = new List<TICoroutine>();
	
	private List<SlotSymbol> reevaluationSpinNewLockedSymbols = new List<SlotSymbol>();
	private string ladderTriggerSymbolName = ""; // used to find trigger symbols, from which we grab the starting locations of particle effect that triggers ladder advance
	
	private List<SlotSymbol> lockedSymbolList = new List<SlotSymbol>();  // used to track blackout status
	
	private Ladder ladder;
	private JSON[] reevaluationSpinNewLockedSymbolInfo;
	private JSON[] reevaluationSpinOldLockedSymbolInfo;
	private Dictionary<ReelRewardTypeEnum, List<ReelRewardData>> rewardDictionary = new Dictionary<ReelRewardTypeEnum, List<ReelRewardData>>(); 
	private bool hasInitialLadderBeenSet; // Flag used to track if we have activated the starting tier on the ladder
	private bool isHandlingReelRewards; // Flag used to track if this module still needs to award the reel rewards (or is in the process of doing so) that will block the game from finishing a spin
	private bool hasInitialRespinCountBeenSet;
	private bool isPlayingBonusGame;
	private bool isBigWinShown;	// Need to track if this module has triggered a big win and doesn't need to trigger one anymore
	private bool wasFeatureTriggered;    // Tracks if the feature was triggered or not so that the big win knows if it should be overridden at the end of the spin (only happens for spins that trigger the feature)
	private bool isLoopingStandardReelSymbols;
	private bool hasPlayedBonusGame;

	private int spinMeterStartAndResetValue = DEFAULT_SPIN_METER_START_AND_RESET_VALUE;
	
	private const string JSON_STICK_AND_WIN_WITH_LADDER_KEY = "{0}_stick_and_win_with_ladder";
	private const string JSON_MULTIPLIER_LADDER_VALUES_KEY = "ladder_multipliers";
	private const string JSON_SPIN_METER_START_VALUE_KEY = "spin_meter_start_value";
	private const string JSON_REEL_LOCKING_SYMBOLS_INFO_KEY = "locked_symbols_info";
	private const string JSON_NEW_LOCKED_SYMBOLS_INFO_KEY = "new_locked_symbols_info";
	private const string JSON_OLD_LOCKED_SYMBOLS_INFO_KEY = "old_locked_symbols_info";
	private const string JSON_LOCKED_SYMBOLS_NAME_KEY = "to_symbol";
	private const string JSON_LOCKED_SYMBOLS_REEL_KEY = "reel";
	private const string JSON_LOCKED_SYMBOLS_POSITION_KEY = "position";
	private const string JSON_SPIN_METER_KEY = "spin_meter";
	private const string JSON_REWARDS_KEY = "rewards";

	private const int DEFAULT_SPIN_METER_START_AND_RESET_VALUE = 1; // Default value so game would still semi function.  However if the server data to fill spinMeterStartAndResetValue does not come down an error message will be logged (since that shouldn't happen).
	
	// --------------------------
	// SECTION: OnSlotGameStarted
	// --------------------------
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Set up stick and win data from modifier exports data
		JSON[] modifierJSON = SlotBaseGame.instance.modifierExports;
		
		JSON stickAndWinJson = null;
		string stickAndWinLadderDataKey = string.Format(JSON_STICK_AND_WIN_WITH_LADDER_KEY, GameState.game.keyName);
		for (int i = 0; i < modifierJSON.Length; i++)
		{
			if (modifierJSON[i].hasKey(stickAndWinLadderDataKey))
			{
				stickAndWinJson = modifierJSON[i].getJSON(stickAndWinLadderDataKey);
				break;
			}
		}
		if (stickAndWinJson != null)
		{
			setScatterMultiplierValuesOnStart(stickAndWinJson);
		}
		else
		{
			Debug.LogWarning("Starting stick and win multiplier ladder not found. Check the reel set data JSON.");
		}
		
		// Initialize ladder feature data
		initLadder();

		yield break;
	}

	private void setScatterMultiplierValuesOnStart(JSON stickAndWinJson)
	{
		if (stickAndWinJson.hasKey(JSON_MULTIPLIER_LADDER_VALUES_KEY))
		{
			JSON[] values = stickAndWinJson.getJsonArray(JSON_MULTIPLIER_LADDER_VALUES_KEY);
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].hasKey("sc_number")) 
				{
					scatterNumberToMultiplierValue.Add(values[i].getInt("sc_number", 0), values[i].getInt("multiplier", 0));
				}
			}
		}
	}

	private void initLadder()
	{
		ladder = new Ladder(rungsData);
		if (reelGame.currentWager == 0)
		{
			ladder.setupRungScCountToMultiplierData(scatterNumberToMultiplierValue,BonusGameManager.currentBonusGameOutcome.getWager());
		}
		else
		{
			ladder.setupRungScCountToMultiplierData(scatterNumberToMultiplierValue, reelGame.currentWager);
		}
	}

	// --------------------------
	// SECTION: executeOnWagerChange
	// --------------------------
	// scale credits on ladder relative to the wager
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return true;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		if (ladder != null)
		{
			ladder.updateCreditLabels(currentWager);
		}
	}
	
	// --------------------------
	// SECTION: OnPreSpin
	// --------------------------
	public override IEnumerator executeOnPreSpin()
	{
		wasFeatureTriggered = false;
		isBigWinShown = false;
		
		// Call activateNormalReels() in base 
		yield return StartCoroutine(base.executeOnPreSpin());
		
		// Resets ladder back to no active tier with all rungs turned off 
		if (hasInitialLadderBeenSet)
		{
			yield return StartCoroutine(ladder.resetAsyncCoroutine());
			hasInitialLadderBeenSet = false;
		}
		
		// Grab and set the multiplier values for the pick game (if they exist)
		if (reelGame.isFreeSpinGame() || reelGame.isDoingFreespinsInBasegame())
		{
			int[] multiplierInitValues = reelGame.freeSpinsOutcomes.getTopMultiplierInitValues();
			if (multiplierInitValues.Length > 0)
			{
				updatePickMultiplierLabels(multiplierInitValues);
			}

			// Make sure we cleanup stuff from the last time we ran freespins
			lockedSymbolList.Clear();
		}
	}
	
	// --------------------------
	// SECTION: OnReevaluationPreSpin
	// --------------------------
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnReevaluationPreSpin()
	{
		// Activate the ladder, set initial starting tier with that rung set to active 
		if (!hasInitialLadderBeenSet)
		{
			JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
			JSON lockedSymbolInfoJson = currentReevalOutcomeJson.getJSON(JSON_REEL_LOCKING_SYMBOLS_INFO_KEY);
			reevaluationSpinOldLockedSymbolInfo = lockedSymbolInfoJson.getJsonArray(JSON_OLD_LOCKED_SYMBOLS_INFO_KEY);
			int landedScCount = reevaluationSpinOldLockedSymbolInfo.Length;
			
			// Because we know there is only one kind of ladder trigger symbol, we only need to grab and store it once.
			ladderTriggerSymbolName = reevaluationSpinOldLockedSymbolInfo[0].getString(JSON_LOCKED_SYMBOLS_NAME_KEY,"");

			yield return StartCoroutine(handleLadderAsyncCoroutine(landedScCount));
			
			hasInitialLadderBeenSet = true;
		}
		
		// Call swapSymbolsToIndependentReels() in base
		yield return StartCoroutine(base.executeOnReevaluationPreSpin()); 
		
		// Swap normal spin panel to use special snick and win version spin panel
		yield return StartCoroutine(swapSpinPanelFromNormalToSpecialVersionAsyncCoroutine()); 
		
		// Set snw spin count label
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
	
	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		wasFeatureTriggered = true;
		
		List<TICoroutine> independentReelIntroCoroutines = new List<TICoroutine>();
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();

		for (int i = 0; i < allVisibleSymbols.Count; i++)
		{
			SlotSymbol currentSymbol = allVisibleSymbols[i];

			if (currentSymbol != null)
			{
				if (!currentSymbol.isScatterSymbol)
				{
					// Fade out non-SC symbols
					independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.fadeOutSymbolCoroutine(1.0f)));
				}
				else
				{
					// Play outcome anims on SC symbols 
					independentReelIntroCoroutines.Add(StartCoroutine(playAndWaitForOutcomeAnimationForStandardReelsIntoLoopAnimation(currentSymbol)));
				}
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(independentReelIntroCoroutines));
		
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
					// We should only mutate the ones that are SC, and keep the rest symbol BL on independent layer as they were faded to be BL before the reel swap
					if (!visibleSymbols[symbolIndex].isScatterSymbol)
					{
						continue;	
					}
					
					independentSymbol.mutateTo(visibleSymbols[symbolIndex].serverName, null, false, true);
					symbolsToTryToStartLooping.Add(independentSymbol);
					
					// Lock in any scatter symbols that were already landed
					if (independentSymbol.isScatterSymbol || independentSymbol.isBonusSymbol)
					{
						SlotReel reelToLock = independentSymbol.reel;
						reelToLock.isLocked = true;
						lockedSymbolList.Add(independentSymbol);
					}
				}
			}
		}
		
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

	private IEnumerator swapSpinPanelFromNormalToSpecialVersionAsyncCoroutine()
	{
		if (shouldSwapOutNormalSpinPanel)
		{
			yield return StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, NORMAL_SPIN_PANEL_EXIT_TIME, false));
			yield return StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, SPECIAL_SPIN_PANEL_ENTER_TIME, false));
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
	
	// --------------------------
	// SECTION: OnReevaluationReelsStopped
	// --------------------------
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
		List<TICoroutine> symbolToLadderCoroutines = new List<TICoroutine>();
		
		if (reevaluationSpinNewLockedSymbolInfo != null && reevaluationSpinNewLockedSymbolInfo.Length > 0)
		{
			// Set ladder position according to locked SC count
			int landedScCount = reevaluationSpinOldLockedSymbolInfo.Length + reevaluationSpinNewLockedSymbolInfo.Length;
			
			// Find all newly landed SC symbols, used to trigger ladder particle effects 
			reevaluationSpinNewLockedSymbols.Clear();
			foreach (var triggerSymbolJSON in reevaluationSpinNewLockedSymbolInfo)
			{
				int reel = triggerSymbolJSON.getInt(JSON_LOCKED_SYMBOLS_REEL_KEY, 0);
				int position = triggerSymbolJSON.getInt(JSON_LOCKED_SYMBOLS_POSITION_KEY, 0);
				
				List<SlotSymbol> slotSymbols = reelGame.engine.getVisibleSymbolsBottomUpAt(reel);
				SlotSymbol slotSymbol = slotSymbols[position];
				reevaluationSpinNewLockedSymbols.Add(slotSymbol);
				lockedSymbolList.Add(slotSymbol);
			}
			
			symbolToLadderCoroutines.Add(StartCoroutine(handleLadderAsyncCoroutine(landedScCount)));
			symbolToLadderCoroutines.Add(StartCoroutine(lockInNewSymbols(reevaluationSpinNewLockedSymbolInfo)));
			
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolToLadderCoroutines));
		}
		
		// Update spin count label
		JSON currentReevalOutcomeJson = reelGame.currentReevaluationSpin.getJsonObject();
		int spinMeterCurrentValue = currentReevalOutcomeJson.getInt(JSON_SPIN_METER_KEY, spinMeterStartAndResetValue);
		yield return StartCoroutine(setSpinCountMessageText(spinMeterCurrentValue));
		
		// If this reeval outcome has rewards, we need to handle snw to bonus transition now. As reeval after this could be due to extra snw pick rewards
		JSON[] rewardsJson = currentReevalOutcomeJson.getJsonArray("rewards");
		if (rewardsJson != null && rewardsJson.Length > 0)
		{
			if (reelGame.isFreeSpinGame() || reelGame.isDoingFreespinsInBasegame())
			{
				yield return StartCoroutine(tryStartPickInSNWGame());
				
				// If there is spins left after coming back from bonus, it must rewarded additional ones
				if (reelGame.hasReevaluationSpinsRemaining)
				{
					yield return StartCoroutine(setSpinCountMessageText(ReelGame.activeGame.reevaluationSpinsRemaining));
				}
			}
		}
		
		// If this is the last reevaluation spin then we need to handle the payout now
		if (!reelGame.hasReevaluationSpinsRemaining)
		{
			yield return StartCoroutine(setSpinCountMessageText(0));
			// Allow for audio changes as we switch to the payout phase of the feature
			if (featurePayoutPhaseStartSounds.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(featurePayoutPhaseStartSounds));
			}
			// Celebrate the multiplier rung won anim
			yield return StartCoroutine(handleAllRewards());
			yield return StartCoroutine(ladder.animateLadderWinOutroAsyncCoroutine());
			
			hasInitialRespinCountBeenSet = false;

			// Hide the spin counter here
			if (hideSpinCounterAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideSpinCounterAnims));
			}
		}
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

	private IEnumerator handleLadderAsyncCoroutine(int landedScCount)
	{
		// Turn off rung that is currently active
		yield return StartCoroutine(playTurnOffAnimationForActiveRungAsyncCoroutine());
		
		// Unlock rungs while advancing ladder to the highest tier
		yield return StartCoroutine(playActivationAnimationForUnlockedTiersAsyncCoroutine(landedScCount));
	}

	private IEnumerator playActivationAnimationForUnlockedTiersAsyncCoroutine(int landedScCount)
	{
		// get all SC symbols that are origins for animated particle effects
		List<SlotSymbol> triggerScSymbols = new List<SlotSymbol>();

		if (hasInitialLadderBeenSet)
		{
			foreach (SlotSymbol slotSymbol in reevaluationSpinNewLockedSymbols)
			{
				if (ladderTriggerSymbolName == slotSymbol.serverName) 
				{
					triggerScSymbols.Add(slotSymbol);
				}
			}
		}
		else
		{
			foreach (var triggerSymbolJSON in reevaluationSpinOldLockedSymbolInfo)
			{
				int reel = triggerSymbolJSON.getInt(JSON_LOCKED_SYMBOLS_REEL_KEY, 0);
				int position = triggerSymbolJSON.getInt(JSON_LOCKED_SYMBOLS_POSITION_KEY, 0);
				
				List<SlotSymbol> slotSymbols = reelGame.engine.getVisibleSymbolsBottomUpAt(reel);
				SlotSymbol slotSymbol = slotSymbols[position];
				reevaluationSpinNewLockedSymbols.Add(slotSymbol);
				triggerScSymbols.Add(slotSymbol);
			}
		}
		
		int highestRungIndex = ladder.getHighestRungIndexByScCount(landedScCount);
		
		List<TICoroutine> scToHighestRungParticleEffects = new List<TICoroutine>();
		
		foreach (SlotSymbol triggerScSymbol in triggerScSymbols)
		{
			scToHighestRungParticleEffects.Add(StartCoroutine(playActivationAnimationForUnlockedRungAsyncCoroutine(highestRungIndex, triggerScSymbol))); 
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(scToHighestRungParticleEffects));
	}

	private IEnumerator playActivationAnimationForUnlockedRungAsyncCoroutine(int highestRungIndex, SlotSymbol triggerSymbol = null)
	{
		// Shoot a particle trail from new locked SC to the highest unlocked rung
		if (triggerSymbol == null)
		{
			yield return StartCoroutine(ladder.unlockTargetRungAsyncCoroutine(highestRungIndex, ladderRungUnlockParticleTrail, ladderRungUnlockSounds));
		}
		else
		{
			yield return StartCoroutine(ladder.unlockTargetRungAsyncCoroutine(highestRungIndex, ladderRungUnlockParticleTrail, ladderRungUnlockSounds, triggerSymbol.transform));
		}
	}
	
	private IEnumerator playTurnOffAnimationForActiveRungAsyncCoroutine()
	{
		int current = ladder.getCurrentTierIndex();
		if (current != -1)
		{
			yield return StartCoroutine(ladder.turnOffCurrentRung(current));
		}
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield break;
	}

	private bool didBlackout
	{
		// blackout occurs if all symbols are unlocked for this feature
		get
		{
			return lockedSymbolList.Count >= numberOfReelSlots;
		}
	}
	
	private int _numberOfReelSlots = 0;
	private int numberOfReelSlots
	{
		get
		{
			if (_numberOfReelSlots <= 0)
			{
				SlotReel[] independentReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_INDEPENDENT_REELS);
				foreach (SlotReel slotReel in independentReelArray)
				{
					_numberOfReelSlots += slotReel.visibleSymbols.Length;
				}
			}

			return _numberOfReelSlots;
		}
	}

	protected IEnumerator tryStartPickInSNWGame()
	{
		// Check for triggering picking game conditions: that is
		// if black out does not occur, and picking game has not been played.
		if (!didBlackout && !hasPlayedBonusGame)
		{
			JSON pickGameOutcomeJson = getPickGameBonusJson();
			
			if (pickGame != null)
			{
				if (pickGameBonusPresenter != null)
				{
					long currentReelGamePayout = BonusGamePresenter.instance.currentPayout;
				
					// Set this so that we don't try and go back to the base game when this multiplier pick is over
					pickGameBonusPresenter.isReturningToBaseGameWhenDone = false;
					
					// Make sure that the multiplier pick doesn't clear the summary screen name
					pickGameBonusPresenter.isKeepingSummaryScreenGameName = true;
				
					BonusGameManager.instance.swapToPassedInBonus(pickGameBonusPresenter, false, isHidingSpinPanelOnPopStack:false);
					pickGameBonusPresenter.gameObject.SetActive(true);
					pickGameBonusPresenter.init(isCheckingReelGameCarryOverValue:true);
					
					// Copy the current reel game winnings into the multiplier pick so that
					// it has the value it will multiply by to get the final credits.
					pickGameBonusPresenter.currentPayout = currentReelGamePayout;
					
					// Also clear out the current payout from the reelGame so that when the bonus game
					// does its rollup it gets added correctly
					reelGame.setRunningPayoutRollupValue(0);
				}

				List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
				
				ModularChallengeGameOutcome pickGameModularBonusOutcome = new ModularChallengeGameOutcome(new SlotOutcome(pickGameOutcomeJson));

				// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
				for (int m = 0; m < pickGame.pickingRounds[0].roundVariants.Length; m++)
				{
					variantOutcomeList.Add(pickGameModularBonusOutcome);
				}

				pickGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);

				pickGame.init();
				hasPlayedBonusGame = true;
				pickGame.gameObject.SetActive(true);

				// wait till this challenge game is over before continuing
				while (pickGameBonusPresenter.isGameActive)
				{
					yield return null;
				}
				
				pickGame.reset();

				// Now that we've finished the picking game we should clear the bonus flag
				// on the SlotOutcome so that the game doesn't try to handle it again
				// reelGame.outcome.isChallenge = false;
				
				// Clear the payout from BonusGameManager, since we don't want the ReelGame to actually pay this out again
				// since we are having the pickGame roll it up directly to the win meter of the ReelGame already.
				BonusGameManager.instance.finalPayout = 0;
			}
			else
			{
				Debug.LogError("MultiplierLadderStickAndWinModule.endFreeSpinsStickAndWinGame() - challengeGame was null!");
			}
		}
	}
	
	// Extract the pick game JSON that we need to run the picking game for the multiplier/snw spins
	private JSON getPickGameBonusJson()
	{
		if (reelGame.outcome != null)
		{
			JSON[] reevals = reelGame.outcome.getArrayReevaluations();
			for (int i = 0; i < reevals.Length; i++)
			{
				JSON currentReevalJson = reevals[i];
				if (currentReevalJson.getJsonArray("rewards") != null)
				{
					JSON[] rewardsJson = currentReevalJson.getJsonArray("rewards");
					if (rewardsJson != null && rewardsJson.Length > 0)
					{
						for (int k = 0; k < rewardsJson.Length; k++)
						{
							JSON currentRewardJson = rewardsJson[k];
							string rewardType = currentRewardJson.getString("outcome_type", "");
							if (rewardType == "bonus_game")
							{
								return currentRewardJson;
							}
						}
					}
				}
			}
		}
		
		Debug.LogError("MultiplierLadderStickAndWinModule.getPickGameBonusOutcome() - Unable to find bonus game, returning NULL!");
		return null;
	}

	// Main function for handling all types of rewards possible with this feature in the correct order
	private IEnumerator handleAllRewards()
	{
		// Cancel all the looped animations here as we are about to award
		stopAllLoopedOutcomeAnimationOnSymbols();
	
		isHandlingReelRewards = true;

		extractReelRewardPrizeAwardData();
		
		// Handle the credit rewards first
		if (rewardDictionary.ContainsKey(ReelRewardTypeEnum.Credits) && rewardDictionary[ReelRewardTypeEnum.Credits].Count > 0)
		{
			yield return StartCoroutine(handleCreditRewards());
		}
		
		yield return StartCoroutine(handleBonusGameRewards());

		isHandlingReelRewards = false;
	}
	
	private IEnumerator handleCreditRewards()
	{
		foreach (ReelRewardData creditReward in rewardDictionary[ReelRewardTypeEnum.Credits])
		{
			// change multiplier to credit amount on the ladder label
			Rung win = ladder.getRungAtIndex(ladder.getCurrentTierIndex());
			yield return StartCoroutine(payoutLadderMultiplierValue(creditReward, multiplierValueRollupLoopKey, multiplierValueRollupTermKey, 
				valueMultiplierPayoutParticleTrail, win));
		}
	}
	
	private IEnumerator handleBonusGameRewards()
	{
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
				case "end_reward":
					reward = new ReelRewardData();
					reward.rewardType = ReelRewardTypeEnum.Credits;
					reward.reelIndex = currentRewardDataJson.getInt("reel", 0);
					reward.symbolPos = currentRewardDataJson.getInt("pos", 0);
					reward.symbolName = currentRewardDataJson.getString("symbol", "");
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
	
	// Payout a credit value or multiplier won
	private IEnumerator payoutLadderMultiplierValue(ReelRewardData currentReward, string rollupOverrideSound, string rollupTermOverrideSound, AnimatedParticleEffect particleEffect, Rung win)
	{
		Transform particleEffectJackpotLocation = win.rungTransform;
		
		yield return StartCoroutine(ladder.animateLadderWinLoopAsyncCoroutine());
		
		// push credit value to win box
		if (particleEffect != null)
		{
			yield return StartCoroutine(particleEffect.animateParticleEffect(particleEffectJackpotLocation));
		}

		float rollupTime = regularSymbolRollupTime;

		bool isSkippingRollup = (rollupTime == -1) ? true : false;
		
		yield return StartCoroutine(rollupWinnings(currentReward.rewardCreditAmount, rollupOverrideSound, rollupTermOverrideSound, rollupTime, isSkippingRollup));
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

		if (!reelGame.hasFreespinGameStarted)
		{
			reelGame.addCreditsToSlotsPlayer(creditsAwarded, "multiplier ladder stick and win award", shouldPlayCreditsRollupSound: false);
		}

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
	
	// --------------------------
	// SECTION: OnBonusGameEnded
	// --------------------------
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
	
	public override bool onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin()
	{
		return !isHandlingReelRewards;
	}
	
	public override bool isAllowingShowNonBonusOutcomesToSetIsSpinComplete()
	{
		return false;
	}
	
	// --------------------------
	// SECTION: TriggerBigWinBeforeSpinEnd
	// --------------------------
	// allows the big win to be delayed, by returning true from isModuleHandlingBigWin
	// the big win will then be custom triggered by the module when executeTriggerBigWinBeforeSpinEnd is called from continueWhenReady
	public override bool isModuleHandlingBigWin()
	{
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
		float rollupTime = SlotUtils.getRollupTime(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), SlotBaseGame.instance.betAmount);
		yield return StartCoroutine(SlotBaseGame.instance.forceTriggerBigWin(ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), rollupTime));
	}
	
	// --------------------------
	// SECTION: BonusGame
	// --------------------------
	public override bool needsToLetModuleCreateBonusGame()
	{
		// We need to skip creation of a bonus if this is the multiplier pick for the freespins
		// (since this module handles displaying and running that itself)
		if (isUsingIndependentReels && reelGame.outcome.isChallenge)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	private void updatePickMultiplierLabels(int[] multiplierValues)
	{
		if (multiplierValues.Length != multiplierLabels.Length)
		{
			Debug.LogError("StickAndWinWithBlackoutPickModule.updateMultiplierLabels() - Number of multiplierValues: " + multiplierValues.Length + "; does not match the number of multiplierLabels = " + multiplierLabels.Length);
		}
		else
		{
			for (int i = 0; i < multiplierLabels.Length; i++)
			{
				multiplierLabels[i].text = CommonText.formatNumber(multiplierValues[i]) + "X";
			}
		}
	}
	
	// ---------------
	// LADDER AND RUNG
	// ---------------
	private class Ladder
	{
		private List<Rung> rungs;
		private int currentTierIndex;
		
		public Ladder(List<Rung> rungs)
		{
			validateRungsData(rungs);
			if (rungs != null)
			{
				this.rungs = rungs;
			}
			currentTierIndex = -1;
		}

		public void updateCreditLabels(long wager)
		{
			foreach (var rung in rungs)
			{
				rung.setCreditLabelText(wager);
			}
		}
		
		public void setupRungScCountToMultiplierData(Dictionary<int, int> scNumberToMultiplier, long wager)
		{
			int rungIndex = 0;
			if (rungs.Count != scNumberToMultiplier.Count)
			{
				Debug.LogError("MultiplierLadderStickandWinModule.cs ladder data doesn't match between client and server.");
			}
			foreach (KeyValuePair<int, int> rungScToMultiplierPair in scNumberToMultiplier)
			{
				rungs[rungIndex].setValue(rungScToMultiplierPair, wager);
				rungIndex++;
			}
		}
		
		public IEnumerator resetAsyncCoroutine()
		{
			// Make sure all rungs are off
			foreach (Rung rung in rungs)
			{
				yield return RoutineRunner.instance.StartCoroutine(rung.turnOffAsyncCoroutine());
			}
			
			currentTierIndex = -1;
		}

		public IEnumerator unlockTargetRungAsyncCoroutine(int targetRungIndex, AnimatedParticleEffect particleEffect, AudioListController.AudioInformationList unlockSounds, Transform triggerSymbolLocation = null)
		{
			Rung targetRung = getRungAtIndex(targetRungIndex);
			if (targetRung != null)
			{
				advanceToTargetRung(targetRungIndex);
				yield return RoutineRunner.instance.StartCoroutine(targetRung.unlockAsyncCoroutine(particleEffect, unlockSounds, triggerSymbolLocation, targetRung.rungTransform));
			}
		}
		
		public IEnumerator turnOffCurrentRung(int index)
		{
			if (rungs[index].isActive)
			{
				yield return RoutineRunner.instance.StartCoroutine(rungs[index].turnOffAsyncCoroutine());
			}
		}
		
		public IEnumerator animateLadderWinLoopAsyncCoroutine()
		{
			yield return RoutineRunner.instance.StartCoroutine(rungs[currentTierIndex].animateWinIntroAsyncCoroutine());
		}
		
		public IEnumerator animateLadderWinOutroAsyncCoroutine()
		{
			yield return RoutineRunner.instance.StartCoroutine(rungs[currentTierIndex].animateWinOutroAsyncCoroutine());
		}
		
		public int getCurrentTierIndex()
		{
			return currentTierIndex;
		}
		
		public int getHighestRungIndexByScCount(int landedSc = 0)
		{
			int rungIndex = 0;
			for (int i = 0; i < rungs.Count; i++)
			{
				if (rungs[i].scCountToUnlock <= landedSc)
				{
					rungIndex = i;
				}
				else if (rungs[i].scCountToUnlock > landedSc && i > rungIndex)
				{
					return rungIndex;
				}
			}
			return rungIndex;
		}
		
		public Rung getRungAtIndex(int index)
		{
			return rungs[index];
		}

		private void validateRungsData(List<Rung> rungsData)
		{
			for (int i = 0; i < rungsData.Count; i++)
			{
				if (rungsData[i].rungTransform == null)
				{
#if UNITY_EDITOR
					Debug.LogError("rung Transform not set for Rung at Tier: " + i);
#endif
				}
				
				if (rungsData[i].multiplierLabel == null)
				{
#if UNITY_EDITOR
					Debug.LogError("multiplier Label not set for Rung at Tier: " + i);
#endif
				}

				if (rungsData[i].activate == null || rungsData[i].idle == null )
				{
#if UNITY_EDITOR
					Debug.LogError("rung animation data not set for Rung at Tier: " + i);
#endif
				}
			}
		}

		private void advanceToTargetRung(int targetRungindex)
		{
			currentTierIndex = targetRungindex;
		}
	}
	
	[Serializable]
	public class Rung
	{
		[Tooltip("End location for particle trail that fly from newly locked sc symbol to rung")]
		public Transform rungTransform;
		[Tooltip("Label that show the player what multiplier value they could win if they land enough sc symbol to climb to reach this tier as top of ladder")]
		public MultiLabelWrapperComponent multiplierLabel;
		[Tooltip("Animation to unlock a rung")]
		public AnimationListController.AnimationInformationList activate;
		[Tooltip("Animation to restore a rung to its inactive state")]
		public AnimationListController.AnimationInformationList idle;
		[Tooltip("Win loop state of this rung")]
		public AnimationListController.AnimationInformationList winStart;
		[Tooltip("Win outro state of this rung")]
		public AnimationListController.AnimationInformationList winEnd;

		public bool isActive { get; private set; }
		public int scCountToUnlock { get; private set; }
		public int multiplierValue { get; private set; }
		
		public void setValue(KeyValuePair<int, int> rungScToMultiplierPair, long wager)
		{
			scCountToUnlock = rungScToMultiplierPair.Key;
			multiplierValue = rungScToMultiplierPair.Value;
			setCreditLabelText(wager);
			isActive = false;
		}

		public IEnumerator unlockAsyncCoroutine(AnimatedParticleEffect particleEffect, AudioListController.AudioInformationList unlockSounds, Transform triggerSymbolLocation = null, Transform rungLocation = null)
		{
			isActive = true;
			yield return RoutineRunner.instance.StartCoroutine(animateUnlockAsyncCoroutine(particleEffect, unlockSounds, triggerSymbolLocation, rungLocation));
		}
		
		public IEnumerator turnOffAsyncCoroutine()
		{
			if (isActive)
			{
				isActive = false;
				yield return RoutineRunner.instance.StartCoroutine(animateTurnOffAsyncCoroutine());
			}
		}

		public void setTransform(Transform rungTransform)
		{
			this.rungTransform = rungTransform;
		}
		
		public void setMultiplierLabelText(MultiLabelWrapperComponent multiplierLabel)
		{
			this.multiplierLabel = multiplierLabel;
			this.multiplierLabel.text = CommonText.formatNumber(multiplierValue) + "X";
		}

		public void setCreditLabelText(long wager)
		{
			string abbreviatedText = CreditsEconomy.multiplyAndFormatNumberAbbreviated(wager * multiplierValue, 2, shouldRoundUp: false);
			multiplierLabel.text = abbreviatedText;		
		}

		public void convertMultiplierToCreditLabelText(long credits)
		{
			string abbreviatedText = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credits, 2, shouldRoundUp: false);
			multiplierLabel.text = abbreviatedText;
		}

		public void convertCreditToMultiplierLabelText()
		{
			multiplierLabel.text = CommonText.formatNumber(multiplierValue) + "X";;
		}
		
		public void setAnimationData(AnimationListController.AnimationInformationList activate, AnimationListController.AnimationInformationList idle)
		{
			this.activate = activate;
			this.idle = idle;
		}

		private IEnumerator animateUnlockAsyncCoroutine(AnimatedParticleEffect particleEffect, AudioListController.AudioInformationList unlockSounds, Transform particleStartLocation, Transform particleEndLocation)
		{
			if (particleStartLocation != null && particleStartLocation != null)
			{
				yield return RoutineRunner.instance.StartCoroutine(particleEffect.animateParticleEffect(particleStartLocation, particleEndLocation));
			}
			
			if (unlockSounds != null && unlockSounds.audioInfoList.Count > 0)
			{
				RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(unlockSounds));
			}

			if (activate.Count > 0)
			{
				RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(activate));
			}
		}
		
		private IEnumerator animateTurnOffAsyncCoroutine()
		{
			if (idle.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(idle));
			}
		}
		
		public IEnumerator animateWinIntroAsyncCoroutine()
		{
			if (winStart.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(winStart));
			}        
		}
		
		public IEnumerator animateWinOutroAsyncCoroutine()
		{
			if (winEnd.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(winEnd));
			}
		}
	}
	
	// -------------
	// REWARDS DATA
	// -------------
	public enum ReelRewardTypeEnum
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
		public long rewardCreditAmount = 0; 
		public ReelRewardTypeEnum rewardType = ReelRewardTypeEnum.Unknown; 
		public SlotOutcome bonusOutcome; 

		public int CompareTo(ReelRewardData other)
		{ 
			if (reelIndex == other.reelIndex)
			{
				return other.symbolPos.CompareTo(symbolPos);
			}
			return reelIndex.CompareTo(other.reelIndex);
		}
	}
}