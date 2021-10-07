using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Base class for a stick and win style game implemented in a way where it will be played as freespins in base
 * using a second layer that has independent reels setup for it.  If the player fully blacks out all of the independent
 * reels a picking game is triggered that awards a multiplier.
 *
 * First used by gen97 Cash Tower
 *
 * Creation Date: 1/22/2020
 * Original Author: Scott Lepthien
 */
public class StickAndWinWithBlackoutPickModule : SwapNormalToIndependentReelTypesBaseModule
{
	private const string JSON_KEY_CASH_TOWER = "_cash_tower";
	private const string JSON_KEY_SC_SYMBOLS_VALUE = "symbol_values";
	private const string JSON_KEY_SYMBOL = "symbol";
	private const string JSON_KEY_CREDITS = "credits";
	
	[SerializeField] private BonusGamePresenter pickGameBonusPresenter;
	[SerializeField] private ModularChallengeGame pickGame;
	[SerializeField] private float postRollupWaitTime;
	[SerializeField] private AnimatedParticleEffect symbolPayoutSparkleTrailParticleEffect;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified.  Used when the player gets a blackout.")]
	[SerializeField] private float blackoutSymbolRollupTime = 0.0f;
	[Tooltip("Labels that show the player what multiplier values they could win if they play the multiplier pick game from getting a blackout")]
	[SerializeField] private LabelWrapperComponent[] multiplierLabels;
	[Tooltip("Audio that should play when the game is ended and the rollup phase is beginning.")]
	[SerializeField] private AudioListController.AudioInformationList endGamePayoutPhaseStartSounds;

	[Header("Super Bonus")] [Tooltip("If this is on, the game is assumed to have a Super Bonus that can trigger before this freespin module happens.")] 
	[SerializeField] private bool isHandlingSuperBonus = true;
	[Tooltip("Animation to play if we are going to show the super bonus amount when freespins starts, which will be awarded at the end of the game")] 
	[SerializeField] private AnimationListController.AnimationInformationList showSuperBonusValueAnims;
	[Tooltip("Animation to play when returning to base game after freespins to ensure if the Super Bonus value was shown, it is now hidden.")] 
	[SerializeField] private AnimationListController.AnimationInformationList hideSuperBonusValueAnims;
	[Tooltip("The label that will display the super bonus value which will award when the freespins is over.")]
	[SerializeField] private LabelWrapperComponent superBonusValueText;
	[Tooltip("Object that is moved in order to show how filled in the Super Bonus bar is.")] 
	[SerializeField] private GameObject superBonusBarMover;
	[Tooltip("Point for the bar mover where the bar appears unfilled.  Will interpolate between this and barFilledPoint to display how filled in the bar is.")]
	[SerializeField] private Vector3 barUnfilledPoint;
	[Tooltip("Point for the bar mover where the bar appears fully filled.  Will interpolate between this and barUnfilledPoint to display how filled in the bar is.")]
	[SerializeField] private Vector3 barFilledPoint;
	[Tooltip("Animations to play when the super bonus is awarded to the player, should probably go to a loop state which can be cancelled by onEndRollupSuperBonusAnims")] 
	[SerializeField] private AnimationListController.AnimationInformationList awardSuperBonusIntroAnims;
	[Tooltip("Animations to play when the super bonus is done rolling up")] 
	[SerializeField] private AnimationListController.AnimationInformationList onEndRollupSuperBonusAnims;
	[Tooltip("Amount of time to use when rolling up the Super Bonus win amount")] 
	[SerializeField] private float superBonusPayoutRollupTime = 4.0f;
	[Tooltip("Bonus game where Super Bonus bar is changed, this will be checked to ensure that Super Bonus bar and win meter are updated when returning from this bonus.")] 
	[SerializeField] private string superBonusAccumulationBonusGameName = "{0}_pickem";
	[Tooltip("Particle effect played before the awardSuperBonusIntroAnims happen, whether it blocks or not will determine how it plays with those anims.")]
	[SerializeField] private AnimatedParticleEffect superBonusAwardSparkleTrailParticleEffect;

	private bool didStartGameInitialization = false;
	private long superBonusWinAmount = 0; // @todo : Verify that this is working correctly to grab the Super Bonus value and store it out
	private List<SlotSymbol> lockedSymbolList = new List<SlotSymbol>();
	
	// Dictionary that stores the scatter symbols and their associated credit value
	private Dictionary<string, long> scatterSymbolInitValues = new Dictionary<string, long>();
	private Dictionary<string, long> scatterSymbolOutcomeValues = new Dictionary<string, long>();
	
	protected int currentSuperBarFillAmount = 0;		// What amount the Super Bonus bar is already filled.  This will be increased via a bonus game, but will be tracked in this class.
	protected int _totalBarFillAmount = 0;				// Total number  that currentSuperBarFillAmount must reach in order to trigger a Super Bonus
	public int totalBarFillAmount
	{
		get { return _totalBarFillAmount; }
		private set { _totalBarFillAmount = value; }
	}

	public override void Awake()
	{
		base.Awake();

		superBonusAccumulationBonusGameName = string.Format(superBonusAccumulationBonusGameName, GameState.game.keyName);
	}

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (!didStartGameInitialization)
		{
			// PARSE DATA, GET MODIFIERS
			JSON[] modifierData = SlotBaseGame.instance.modifierExports;

			string reSpinDataKey = GameState.game.keyName + JSON_KEY_CASH_TOWER;

			JSON featureInitData = null;
			for (int i = 0; i < modifierData.Length; i++)
			{
				if (modifierData[i].hasKey(reSpinDataKey))
				{
					featureInitData = modifierData[i].getJSON(reSpinDataKey);
					break;
				}
			}

			if (featureInitData != null)
			{
				if (featureInitData.hasKey(JSON_KEY_SC_SYMBOLS_VALUE))
				{
					JSON[] values = featureInitData.getJsonArray(JSON_KEY_SC_SYMBOLS_VALUE);
					for (int i = 0; i < values.Length; i++)
					{
						if (values[i].hasKey(JSON_KEY_SYMBOL)) //Check for the key before adding it into the dictionary
						{
							string symbolName = values[i].getString(JSON_KEY_SYMBOL, "");
							long symbolCredit = values[i].getLong(JSON_KEY_CREDITS, 0);

							if (scatterSymbolInitValues.ContainsKey(symbolName))
							{
								scatterSymbolInitValues[symbolName] = symbolCredit;
							}
							else
							{
								scatterSymbolInitValues.Add(symbolName, symbolCredit);
							}
						}
					}
				}

				// Get the super bonus meter info
				currentSuperBarFillAmount = featureInitData.getInt("super_bonus_meter", 0);
				_totalBarFillAmount = featureInitData.getInt("super_bonus_meter_size", 0);
			}
			else
			{
				Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
			}

			didStartGameInitialization = true;

			// Make sure the game is using normal reels when it starts
			base.executeOnSlotGameStartedNoCoroutine(reelSetDataJson);
		}
	}
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return !isUsingIndependentReels;
	}

	public override IEnumerator executeOnPreSpin()
	{
		// need to clear this out on each new spin in the base game to make
		// sure it is 0 unless it is set after a super bonus is completed
		superBonusWinAmount = 0;
		yield break;
	}
	
	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return StartCoroutine((updateSuperBonusBarFill()));
	}

	// executeOnContinueToBasegameFreespins() section
	// functions in this section are executed when SlotBaseGame.continueToBasegameFreespins() is called to start freespins in base
	// NOTE: These modules will trigger right at the start of the transition to freespins in base, before the spin panel is changed and the game is fully ready to start freespining
	public override bool needsToExecuteOnContinueToBasegameFreespins()
	{
		return true;
	}

	public override IEnumerator executeOnContinueToBasegameFreespins()
	{
		// Grab and set the multiplier values for the pick game (if they exist)
		int[] multiplierInitValues = reelGame.freeSpinsOutcomes.getTopMultiplierInitValues();
		if (multiplierInitValues.Length > 0)
		{
			updateMultiplierLabels(multiplierInitValues);
		}

		// Make sure we cleanup stuff from the last time we ran freespins
		lockedSymbolList.Clear();

		yield return StartCoroutine(activateIndependentReels());
	}
	
	// executeOnBonusGameEnded() section
	// functions here are called by the SlotBaseGame onBonusGameEnded() function
	// usually used for reseting transition stuff
	public override bool needsToExecuteOnBonusGameEnded()
	{
		// Only want to trigger this when returning from the freespin count pick
		return BonusGameManager.instance.bonusGameName == superBonusAccumulationBonusGameName;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		if (isHandlingSuperBonus)
		{
			// Need to show the bar updated with whatever the current counter is
			yield return StartCoroutine(updateSuperBonusBarFill());
		
			// Handle swapping the top bar to what it should be displaying
			if (superBonusWinAmount > 0)
			{
				superBonusValueText.text = CreditsEconomy.convertCredits(superBonusWinAmount);
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(showSuperBonusValueAnims));
			}
		}
	}
	
	// executeOnReturnToBasegameFreespins is the inverse of executeOnContinueToBaseGameFreespins()
	// these will trigger right at the start of the transition from freespins back to base, before spin panel transitions and any big win starts
	public override bool needsToExecuteOnReturnToBasegameFreespins()
	{
		return true;
	}

	public override IEnumerator executeOnReturnToBasegameFreespins()
	{
		yield return StartCoroutine(activateNormalReels());
	}
	
	protected override IEnumerator swapSymbolsBackToNormalReels()
	{
		// Copy the independent reel symbols back over to the normal (non-independent) reels
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(0);
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
						// check for outcome first since values may be changed here
						Dictionary<string, long> scatterSymbolValues = null;
						if (scatterSymbolOutcomeValues.ContainsKey(currentRegularSymbol.serverName))
						{
							scatterSymbolValues = scatterSymbolOutcomeValues;
						}
						else if (scatterSymbolInitValues.ContainsKey(currentRegularSymbol.serverName))
						{
							scatterSymbolValues = scatterSymbolInitValues;
						}
						
						creditLabelUpdate(currentRegularSymbol.getDynamicLabel(), scatterSymbolValues[currentRegularSymbol.serverName], false);
					}
				}
			}
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

		// Disable the super bonus value if that is showing
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideSuperBonusValueAnims));
	}
	
	protected override IEnumerator swapSymbolsToIndependentReels()
	{
		// Fade out non-scatters symbols
		List<TICoroutine> independentReelIntroCoroutines = new List<TICoroutine>();
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		List<SlotSymbol> symbolsFaded = new List<SlotSymbol>();

		for (int i = 0; i < allVisibleSymbols.Count; i++)
		{
			SlotSymbol currentSymbol = allVisibleSymbols[i];

			if (currentSymbol != null)
			{
				if (!currentSymbol.isScatterSymbol)
				{
					// Not scatter symbol so fade this symbol out
					symbolsFaded.Add(currentSymbol);
					independentReelIntroCoroutines.Add(StartCoroutine(currentSymbol.fadeOutSymbolCoroutine(1.0f)));
				}
			}
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
				if (independentSymbol.serverName != visibleSymbols[symbolIndex].serverName)
				{
					independentSymbol.mutateTo(visibleSymbols[symbolIndex].serverName, null, false, true);
					
					// Lock in any scatter symbols that were already landed
					if (independentSymbol.isScatterSymbol)
					{
						SlotReel reelToLock = independentSymbol.reel;
						reelToLock.isLocked = true;
						lockedSymbolList.Add(independentSymbol);
					}
				}
			}
		}

		// Now change what is being shown to be the independent layer
		showLayer(LAYER_INDEX_INDEPENDENT_REELS);
	}
	
	// executeAfterSymbolSetup() secion
	// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (didStartGameInitialization && symbol.isScatterSymbol)
		{
			Dictionary<string, long> scatterSymbolValues = null;

			// check for outcome first since values may be changed here
			if (scatterSymbolOutcomeValues.ContainsKey(symbol.serverName))
			{
				scatterSymbolValues = scatterSymbolOutcomeValues;
			}
			else if (scatterSymbolInitValues.ContainsKey(symbol.serverName))
			{
				scatterSymbolValues = scatterSymbolInitValues;
			}

			// not all scatter symbols have labels to update (zynga06 - ribboned pumpkins)
			if (scatterSymbolValues != null)
			{
				// Only set the label on Scatter symbols that are in our dictionary. 
				// If its a Scatter symbol without a credits value then it's the Scatter that awards the jackpot.
				creditLabelUpdate(symbol.getDynamicLabel(), scatterSymbolValues[symbol.serverName], false);
			}
		}
	}
	
	private void creditLabelUpdate(LabelWrapperComponent label, long credit, bool isCreditAlreadyMultiplied)
	{
		if (label == null)
		{
			return;
		}

		long creditMultiplied = credit;

		if (!isCreditAlreadyMultiplied)
		{
			creditMultiplied = credit * reelGame.multiplier;
		}

		label.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credit: creditMultiplied, decimalPoints: 1, shouldRoundUp: false);
	}

	// Determine the value of the passed in symbol using a name lookup into our dictionary of symbol values
	private long getCreditValueForScatterSymbol(SlotSymbol symbol, bool isApplyingMultiplier)
	{
		Dictionary<string, long> scatterSymbolValues = null;
		
		// check for outcome first since values may be changed here
		if (scatterSymbolOutcomeValues.ContainsKey(symbol.serverName))
		{
			scatterSymbolValues = scatterSymbolOutcomeValues;
		}
		else if (scatterSymbolInitValues.ContainsKey(symbol.serverName))
		{
			scatterSymbolValues = scatterSymbolInitValues;
		}

		if (scatterSymbolValues != null)
		{
			long symbolCreditValue = scatterSymbolValues[symbol.serverName];
			if (isApplyingMultiplier)
			{
				symbolCreditValue *= reelGame.multiplier;
			}

			return symbolCreditValue;
		}
		else
		{
			Debug.LogError("StickAndWinWithBlackoutPickModule.getCreditValueForScatterSymbol() - Unable to find symbol.serverName = " + symbol.serverName + "; in lookup dictionaries!");
			return 0;
		}
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return reelGame.isDoingFreespinsInBasegame();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();
		
		if (stickyMutationsList.Count > 0)
		{
			lockIneNewStickySymbols(stickyMutationsList);
		}

		if (isGameEnded)
		{
			yield return StartCoroutine(endGame());
		}
	}
	
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		yield return StartCoroutine(animateScatterSymbols(reel));
	}
	
	public IEnumerator animateScatterSymbols(SlotReel reel)
	{
		// If we have a scatter symbol we want to play sound
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			if (reel.visibleSymbols[i].isScatterSymbol)
			{
				reel.visibleSymbols[i].animateAnticipation();
			}
		}
		
		// @todo : Will probably need sounds for base when triggering feature (see IndependentStackingRewardsModule.cs for example of handling SC trigger sounds).
		// @todo : Will need sounds for Standard Freespins when symbols are simply landing
		yield break;
	}
	
	// Get the sticky mutation that will be used for this spin
	protected List<StandardMutation> getCurrentStickyMutations()
	{
		List<StandardMutation> mutationList = new List<StandardMutation>();

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				MutationBase mutation = reelGame.mutationManager.mutations[i];

				if (mutation.type == "free_spin_trigger_wild_locking"
					|| mutation.type == "symbol_locking_with_freespins"
					|| mutation.type == "symbol_locking_with_scatter_wins"
					|| mutation.type == "symbol_locking_with_mutating_symbols"
					|| mutation.type == "sliding_sticky_symbols"
					|| mutation.type == "symbol_locking"
					|| mutation.type == "symbol_locking_multi_payout"
					|| mutation.type == "symbol_locking_multi_payout_jackpot"
					|| mutation.type == "symbols_lock_fake_spins_mutator")
				{
					mutationList.Add(mutation as StandardMutation);
				}
			}
		}

		return mutationList;
	}
	
	// Creates a list of symbols that were locked on the last spin, so they 
	// can be animated by playAnimationsForNewLockedSymbols and have particle
	// trails blast to the spin panel.
	private void lockIneNewStickySymbols(List<StandardMutation> stickyMutationsList)
	{
		for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
		{
			StandardMutation stickyMutation = stickyMutationsList[mutationIndex];

			for (int reelID = 0; reelID < stickyMutation.triggerSymbolNames.GetLength(0); reelID++)
			{
				for (int position = 0; position < stickyMutation.triggerSymbolNames.GetLength(1); position++)
				{
					if (!string.IsNullOrEmpty(stickyMutation.triggerSymbolNames[reelID, position]))
					{
						SlotReel slotReel = reelGame.engine.getSlotReelAt(reelID, position, LAYER_INDEX_INDEPENDENT_REELS);
						
						if (!slotReel.isLocked)
						{
							// Lock this reel so it doesn't spin anymore
							slotReel.isLocked = true;
						
							SlotSymbol symbol = slotReel.visibleSymbols[0];

							if (symbol != null)
							{
								lockedSymbolList.Add(symbol);
							}
							else
							{
								Debug.LogError("StickAndWinWithBlackoutPickModule.lockIneNewStickySymbols() - Trying to lock a reel that has a null symbol on it!");
							}
						}
					}
				}
			}
		}
	}
	
	// Rollup the symbols into the win meter
	// Then determine if a blackout occured in which case we need to trigger the picking game
	protected IEnumerator endGame()
	{
		// Make sure we cancel any remaining spins
		if (reelGame.numberOfFreespinsRemaining > 0)
		{
			reelGame.numberOfFreespinsRemaining = 0;
		}
		
		// Allow for audio changes as we switch to the payout phase of the game
		if (endGamePayoutPhaseStartSounds.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(endGamePayoutPhaseStartSounds));
		}

		// First add the symbol values to the win meter with effects
		List<SlotSymbol> independentVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		foreach (SlotSymbol symbol in independentVisibleSymbols)
		{
			if (symbol != null && !symbol.isBlankSymbol)
			{
				symbol.animateOutcome();
			
				if (symbolPayoutSparkleTrailParticleEffect != null)
				{
					yield return StartCoroutine(symbolPayoutSparkleTrailParticleEffect.animateParticleEffect(symbol.transform));
				}
				
				float rollupTime = regularSymbolRollupTime;
				if (didLockAllSymbols)
				{
					rollupTime = blackoutSymbolRollupTime;
				}
				
				bool isSkippingRollup = (rollupTime == -1) ? true : false;
				long creditsAwarded = getCreditValueForScatterSymbol(symbol, true);
				yield return StartCoroutine(rollupWinnings(creditsAwarded, rollupTime, isSkippingRollup));
			}
		}

		if (didLockAllSymbols)
		{
			// Check for blackout, and trigger picking game if black out occured.
			SlotOutcome pickGameOutcome = getPickGameBonusOutcome();
			
			if (pickGame != null)
			{
				if (pickGameBonusPresenter != null)
				{
					long currentReelGamePayout = BonusGamePresenter.instance.currentPayout;

					// Make sure that the multiplier pick doesn't clear the summary screen name
					pickGameBonusPresenter.isKeepingSummaryScreenGameName = true;
					pickGameBonusPresenter.gameObject.SetActive(true);
					BonusGameManager.instance.swapToPassedInBonus(pickGameBonusPresenter, false, isHidingSpinPanelOnPopStack:false);
					pickGameBonusPresenter.init(isCheckingReelGameCarryOverValue:true);

					// Copy the current reel game winnings into the multiplier pick so that
					// it has the value it will multiply by to get the final credits.
					pickGameBonusPresenter.currentPayout = currentReelGamePayout;
					
					// Also clear out the current payout from the reelGame so that when the bonus game
					// does its rollup it gets added correctly
					reelGame.setRunningPayoutRollupValue(0);
				}

				List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
				
				WheelOutcome pickGameWheelOutcome = new WheelOutcome(pickGameOutcome);
				// Convert our outcome to ModularChallengeGameOutcome
				ModularChallengeGameOutcome pickGameModularBonusOutcome = new ModularChallengeGameOutcome(pickGameWheelOutcome);

				// since each variant will use the same outcome we need to add as many outcomes as there are variants setup
				for (int m = 0; m < pickGame.pickingRounds[0].roundVariants.Length; m++)
				{
					variantOutcomeList.Add(pickGameModularBonusOutcome);
				}

				pickGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
				pickGame.init();
				pickGame.gameObject.SetActive(true);

				// wait till this challenge game is over before continuing
				while (pickGameBonusPresenter.isGameActive)
				{
					yield return null;
				}
				
				pickGame.reset();
				
				// Now that we've finished the picking game we should clear the bonus flag
				// on the SlotOutcome so that the game doesn't try to handle it again
				reelGame.outcome.isChallenge = false;
				
				// Clear the payout from BonusGameManager, since we don't want the ReelGame to actually pay this out again
				// since we are having the pickGame roll it up directly to the win meter of the ReelGame already.
				BonusGameManager.instance.finalPayout = 0;
			}
			else
			{
				Debug.LogError("StickAndWinWithBlackoutPickModule.endGame() - challengeGame was null!");
			}
		}
		
		// If the player has a super bonus amount, then we need to roll that up
		if (isHandlingSuperBonus && superBonusWinAmount > 0)
		{
			if (superBonusAwardSparkleTrailParticleEffect != null)
			{
				yield return StartCoroutine(superBonusAwardSparkleTrailParticleEffect.animateParticleEffect());
			}
		
			if (awardSuperBonusIntroAnims != null && awardSuperBonusIntroAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(awardSuperBonusIntroAnims));
			}

			yield return StartCoroutine(reelGame.rollupCredits(0, 
				superBonusWinAmount, 
				ReelGame.activeGame.onPayoutRollup, 
				isPlayingRollupSounds: true,
				specificRollupTime: superBonusPayoutRollupTime,
				shouldSkipOnTouch: true,
				allowBigWin: false,
				isAddingRollupToRunningPayout: true));

			if (onEndRollupSuperBonusAnims != null && onEndRollupSuperBonusAnims.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onEndRollupSuperBonusAnims));
			}
		}
	}

	// Extract the pick game JSON that we need to run the picking game for the multiplier
	private SlotOutcome getPickGameBonusOutcome()
	{
		// Seems like this is getting called even on reeevaluation spins where
		// we don't need to update all this data again
		if (reelGame.outcome != null && reelGame.outcome.isChallenge)
		{
			return reelGame.outcome.getBonusGameInOutcomeDepthFirst();
		}
		
		Debug.LogError("StickAndWinWIthBlackoutPickModule.getPickGameBonusOutcome() - Unable to find bonus game, returning NULL!");
		return null;
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

	private bool didLockAllSymbols
	{
		get
		{
			return lockedSymbolList.Count >= numberOfReelSlots;
		}
	}
	
	private bool isGameEnded
	{
		get
		{
			// @todo : Can't use FreeSpinGame.instance here because this is freespins in base
			return (didLockAllSymbols || reelGame.numberOfFreespinsRemaining <= 0);
		}
	}
	
	private IEnumerator rollupWinnings(long creditsAwarded, float rollupTime = 0.0f, bool isSkippingRollup = false)
	{
		BonusGamePresenter.instance.currentPayout += creditsAwarded;

		if (!isSkippingRollup)
		{
			yield return StartCoroutine(reelGame.rollupCredits(creditsAwarded, 
			null, 
			true, 
			0.0f, 
			true, 
			false, 
			true));
		}
		else
		{
			// Just add the values without rolling up
			reelGame.onPayoutRollup(creditsAwarded);
			yield return StartCoroutine(reelGame.onEndRollup(false, true));
		}

		if (postRollupWaitTime > 0)
		{
			yield return new TIWaitForSeconds(postRollupWaitTime);
		}
	}

	// Updates the Super Bonus bar display for how filled in it should be, and then plays the animations to display it.
	private IEnumerator updateSuperBonusBarFill()
	{
		Vector3 pointDifference = barFilledPoint - barUnfilledPoint;
		float newSegmentPercent = currentSuperBarFillAmount / (float)(_totalBarFillAmount);
		superBonusBarMover.transform.localPosition = barUnfilledPoint + (pointDifference * newSegmentPercent);
		yield break;
	}
	
	// Function to check and ensure that the passed starting value for the super bonus bar matches what
	// was shown in the base game.
	public void verifySuperBonusBarFillAmountMatches(int passedValue)
	{
		if (currentSuperBarFillAmount != passedValue)
		{
			Debug.LogError("StickAndWinWithBlackoutPickModule.verifySuperBonusBarFillAmountMatches() - Base game bar value of currentSuperBarFillAmount = " + currentSuperBarFillAmount + "; did not match bonus value of passedValue = " + passedValue);
		}
	}

	// Public function so that other games that modify this value can tell this module about changes
	public void incrementSuperBarFillAmount(int amountToAdd)
	{
		currentSuperBarFillAmount += amountToAdd;
		if (currentSuperBarFillAmount > _totalBarFillAmount)
		{
			currentSuperBarFillAmount = _totalBarFillAmount;
		}
	}

	// Reset the Super Bonus bar fill to the starting point (doesn't update the visual, that is only done
	// if updateSuperBonusBarFillAndShow() is called)
	public void resetSuperBarFillAmount()
	{
		currentSuperBarFillAmount = 0;
	}

	public void setSuperBonusWinAmount(long winAmount)
	{
		superBonusWinAmount = winAmount;
	}
	
	// We need to not create a bonus game for the multiplier pick that is part of freespins
	// since that is already part of this module, and the picking game prefab setup for 
	// slot resource map is for the Freespins Count pick
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
	
	// Control if the reel game wants the Overlay and SpinPanel turned back on when returning from a bonus
	// you may want to skip that step if for instance eyou have a transition that will do it for you
	public override bool isEnablingOverlayWhenBonusGameEnds()
	{
		// When the pick bonus ends we don't want the overlay to enable, that should
		// only come back on when the freespins in base ends
		if (isUsingIndependentReels)
		{
			return false;
		}
		else
		{
			return true;
		}
	}
	
	private void updateMultiplierLabels(int[] multiplierValues)
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
}
