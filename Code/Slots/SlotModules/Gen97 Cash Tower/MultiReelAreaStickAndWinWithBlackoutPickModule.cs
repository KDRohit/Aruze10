using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module made for the gen97 Super Bonus Freespins.  Handles multiplier values which increase
 * when a reel section gets blacked out.  And playing a multiplier pick bonus at the end assuming
 * the player got at least one blackout.
 * NOTE: This game does not actually have separated reel areas (the whole game is a large 9 tall
 * by 5 wide set of independent reels, with each 3x5 section being treated as if it was a unique
 * section).
 *
 * Original Creator: Scott Lepthien
 * Creation Date: 2/12/2020
 */
public class MultiReelAreaStickAndWinWithBlackoutPickModule : SlotModule
{
	private const string JSON_KEY_CASH_TOWER = "_cash_tower";
	private const string JSON_KEY_SC_SYMBOLS_VALUE = "symbol_values";
	private const string JSON_KEY_SYMBOL = "symbol";
	private const string JSON_KEY_CREDITS = "credits";
	private const string SUPER_BONUS_TYPE = "super_bonus";
	private const int REEL_SECTION_HEIGHT = 3; // Can look into changing this in the future, or pulling it from somewhere like the server data instead of having it hardcoded
	
	[Tooltip("Link to the multiplier pick game bonus presenter")]
	[SerializeField] private BonusGamePresenter pickGameBonusPresenter;
	[Tooltip("Link to the multiplier pick game object")]
	[SerializeField] private ModularChallengeGame pickGame;
	[Tooltip("Labels at the top that show the player what multiplier values they could win if they play the multiplier pick game from getting a blackout")]
	[SerializeField] private LabelWrapperComponent[] multiplierLabels;
	[Tooltip("Delay before multipliers are updated in order to allow their label change to be synced with the animations being played form the ReelAreaBlackoutAnimData entry")]
	[SerializeField] private float delayBeforeUpdatingMultipliers = 0.0f;
	[Tooltip("Data that contains the sequences of animations to play when a specific area of the reel is blacked out.  This could include reel area effects, and should include the multipliers at the top being updated.")]
	[SerializeField] private ReelAreaBlackoutAnimData[] blackoutAnimDataArray;
	[Tooltip("Audio that should play when the game is ended and the rollup phase is beginning.")]
	[SerializeField] private AudioListController.AudioInformationList endGamePayoutPhaseStartSounds;
	[Tooltip("Additional delay after a rollup is complete that a coroutine will continue to block")]
	[SerializeField] private float postRollupWaitTime;
	[Tooltip("Particle effect used when each symbol is paid out, going from the symbol to the win meter")]
	[SerializeField] private AnimatedParticleEffect symbolPayoutSparkleTrailParticleEffect;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified.  Used when the player gets a blackout.")]
	[SerializeField] private float blackoutSymbolRollupTime = 0.0f;
	
	// Dictionary that stores the scatter symbols and their associated credit value
	private Dictionary<string, long> scatterSymbolInitValues = new Dictionary<string, long>();
	private Dictionary<string, long> scatterSymbolOutcomeValues = new Dictionary<string, long>();
	
	private List<SlotSymbol> lockedSymbolList = new List<SlotSymbol>();
	
	private bool didStartGameInitialization = false;
	private bool isAnySectionBlackedOut = false;
	
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Tell bonus game presenter we aren't going back to the base game after this, since
		// we need to return to the picking game for freespins instead.
		BonusGamePresenter.instance.isReturningToBaseGameWhenDone = false;
	
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
						scatterSymbolInitValues.Add(symbolName, symbolCredit);
					}
				}
			}
			
			// Need to handle displaying these multiplier values at the top disabled.
			int[] multiplierInitValues = reelGame.freeSpinsOutcomes.getTopMultiplierInitValues();
			updateMultiplierLabels(multiplierInitValues);
		}
		else
		{
			Debug.LogWarning("Starting Information not found. Check the reel set data JSON.");
		}

		didStartGameInitialization = true;
		
		yield break;
	}
	
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();
		
		if (stickyMutationsList.Count > 0)
		{
			lockIneNewStickySymbols(stickyMutationsList);
			updateFreespinCount(stickyMutationsList);
		}
		
		// Check for blackouts in reevaluations, and flag that we've had at least
		// one blackout if we do have one.  Also update the multipliers at the top to their
		// new values.
		JSON superBonusFreespinReevaluationJson = getSuperBonusFreespinsReevaluationJson();
		if (superBonusFreespinReevaluationJson != null)
		{
			JSON[] blackoutSectionsJson = superBonusFreespinReevaluationJson.getJsonArray("blackout_sections");
			if (blackoutSectionsJson != null && blackoutSectionsJson.Length > 0)
			{
				// Loop through each blackout section (since multiple could occur on a single spin)
				// and present them to the player.
				List<TICoroutine> runningCoroutineList = new List<TICoroutine>();
				foreach (JSON sectionJson in blackoutSectionsJson)
				{
					// The sections will always come down once they are acquired, but this reward
					// change will only come down when the reward is first encountered 
					JSON topRewardChangesJson = sectionJson.getJSON("top_reward_changes");
					if (topRewardChangesJson != null)
					{
						int sectionRowMin = sectionJson.getInt("row_min", 0);
						ReelAreaBlackoutAnimData animData = getReelAreaBlackoutAnimDataForRowMin(sectionRowMin);

						if (isAnySectionBlackedOut)
						{
							// Play the increase flow of animations
							if (animData.multipliersIncreasedAnims.Count > 0)
							{
								runningCoroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.multipliersIncreasedAnims)));
							}
						}
						else
						{
							// Play the unlock flow of animations
							if (animData.multipliersUnlockedAnims.Count > 0)
							{
								runningCoroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.multipliersUnlockedAnims)));
							}
						}
					
						JSON[] afterRewardsJson = topRewardChangesJson.getJsonArray("after");
						
						// NOTE : If reward types were to ever change we'd need to update how we do things here
						int[] newMultiplierValues = new int[afterRewardsJson.Length];
						for (int i = 0; i < afterRewardsJson.Length; i++)
						{
							newMultiplierValues[i] = afterRewardsJson[i].getInt("multiplier", 0);
						}

						// Update the multiplier values
						if (delayBeforeUpdatingMultipliers > 0.0f)
						{
							runningCoroutineList.Add(StartCoroutine(updateMultiplierLabelsAfterDelay(delayBeforeUpdatingMultipliers, newMultiplierValues)));
						}
						else
						{
							updateMultiplierLabels(newMultiplierValues);
						}

						yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutineList));
						runningCoroutineList.Clear();

						// Flag that at least one section has been blacked out now
						isAnySectionBlackedOut = true;
					}
				}
			}
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
		
		// @todo : Will need to play sounds for symbols landing
		yield break;
	}
	
	private void updateFreespinCount(List<StandardMutation> stickyMutationsList)
	{
		int numberOfSpinsAdded = 0;
		foreach (StandardMutation mutation in stickyMutationsList)
		{
			numberOfSpinsAdded += mutation.numberOfFreeSpinsAwarded;
		}

		if (numberOfSpinsAdded > 0)
		{
			reelGame.numberOfFreespinsRemaining += numberOfSpinsAdded;
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
	
		// First add the symbol values to the win meter with effects.
		// Want to pay out each reel section one at a time, so need
		// to group each section and then pay them out one at a time.
		Dictionary<int, List<SlotSymbol>> symbolsByReelSection = new Dictionary<int, List<SlotSymbol>>();
		List<List<SlotReel>> independentReelArray = reelGame.engine.independentReelArray;
		for (int reelIndex = 0; reelIndex < independentReelArray.Count; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex);

			int currentSection = 0;
			for (int i = 0; i < visibleSymbols.Length; i++)
			{
				if (i > 0 && i % REEL_SECTION_HEIGHT == 0)
				{
					currentSection++;
				}

				if (!symbolsByReelSection.ContainsKey(currentSection))
				{
					symbolsByReelSection.Add(currentSection, new List<SlotSymbol>());
				}
				
				symbolsByReelSection[currentSection].Add(visibleSymbols[i]);
			}
		}

		// Now that sections are grouped together we can pay them out
		// one at a time.
		for (int i = 0; i < symbolsByReelSection.Count; i++)
		{
			List<SlotSymbol> currentSectionVisibileSymbols = symbolsByReelSection[i];
			foreach (SlotSymbol symbol in currentSectionVisibileSymbols)
			{
				if (symbol != null && !symbol.isBlankSymbol)
				{
					symbol.animateOutcome();
				
					if (symbolPayoutSparkleTrailParticleEffect != null)
					{
						yield return StartCoroutine(symbolPayoutSparkleTrailParticleEffect.animateParticleEffect(symbol.transform));
					}
				
					// Allow rollups to be instant if we want (happened in another scatter game gen86 freespins)
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
		}

		// If any section was blacked out, then we should be playing a bonus game here
		if (isAnySectionBlackedOut)
		{
			// Check for blackout, and trigger picking game if black out occured.
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
				
				WheelOutcome pickGameWheelOutcome = new WheelOutcome(new SlotOutcome(pickGameOutcomeJson));
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
				
				// Now that we've finished the picking game we should clear the bonus flag
				// on the SlotOutcome so that the game doesn't try to handle it again
				reelGame.outcome.isChallenge = false;
				
				// Clear the payout from BonusGameManager, since we don't want the ReelGame to actually pay this out again
				// since we are having the pickGame roll it up directly to the win meter of the ReelGame already.
				BonusGameManager.instance.finalPayout = 0;
				
				// Force the music off so that it doesn't get saved and restored by the summary dialog
				Audio.switchMusicKeyImmediate("");
			}
			else
			{
				Debug.LogError("MultiReelAreaStickAndWinWIthBlackoutPickModule.endGame() - challengeGame was null!");
			}
		}
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

	// executeAfterSymbolSetup() section
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
	
	// Control if the reel game wants the Overlay and SpinPanel turned back on when returning from a bonus
	// you may want to skip that step if for instance eyou have a transition that will do it for you
	public override bool isEnablingOverlayWhenBonusGameEnds()
	{
		// This super bonus goes back to a freespin count pick game when done, so we
		// don't want the overlay to turn back on.
		return false;
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
						SlotReel slotReel = reelGame.engine.getSlotReelAt(reelID, position);
						
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
								Debug.LogError("MultiReelAreaStickAndWinWIthBlackoutPickModule.lockIneNewStickySymbols() - Trying to lock a reel that has a null symbol on it!");
							}
						}
						else
						{
							Debug.LogWarning("MultiReelAreaStickAndWinWIthBlackoutPickModule.lockIneNewStickySymbols() - Trying to lock already locked reel, just skipping this.");
						}
					}
				}
			}
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
	
	private void creditLabelUpdate(LabelWrapperComponent label, long credit, bool isCreditAlreadyMultiplied)
	{
		if (label == null)
		{
			return;
		}

		long creditMultiplied = credit;

		if (!isCreditAlreadyMultiplied)
		{
			if (BonusGameManager.instance.betMultiplierOverride != -1)
			{
				creditMultiplied = credit * BonusGameManager.instance.betMultiplierOverride;
			}
			else
			{
				creditMultiplied = credit * reelGame.multiplier;
			}
		}

		label.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(credit: creditMultiplied, decimalPoints: 1, shouldRoundUp: false);
	}

	// Update the multiplier labels after a delay (in case their change should be timed with the playing of an animation)
	private IEnumerator updateMultiplierLabelsAfterDelay(float delay, int[] multiplierValues)
	{
		if (delay > 0.0f)
		{
			yield return new TIWaitForSeconds(delay);
		}

		updateMultiplierLabels(multiplierValues);
	}

	// Update the multiplier labels that will be presented to the player during the blackout multiplier pick game
	private void updateMultiplierLabels(int[] multiplierValues)
	{
		if (multiplierValues.Length != multiplierLabels.Length)
		{
			Debug.LogError("MultiReelAreaStickAndWinWIthBlackoutPickModule.updateMultiplierLabels() - Number of multiplierValues: " + multiplierValues.Length + "; does not match the number of multiplierLabels = " + multiplierLabels.Length);
		}
		else
		{
			for (int i = 0; i < multiplierLabels.Length; i++)
			{
				multiplierLabels[i].text = CommonText.formatNumber(multiplierValues[i]) + "X";
			}
		}
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
				if (BonusGameManager.instance.betMultiplierOverride != -1)
				{
					symbolCreditValue *= BonusGameManager.instance.betMultiplierOverride;
				}
				else
				{
					symbolCreditValue *= reelGame.multiplier;
				}
			}

			return symbolCreditValue;
		}
		else
		{
			Debug.LogError("StickAndWinWithBlackoutPickModule.getCreditValueForScatterSymbol() - Unable to find symbol.serverName = " + symbol.serverName + "; in lookup dictionaries!");
			return 0;
		}
	}
	
	// Extract the pick game JSON that we need to run the picking game for the multiplier
	private JSON getPickGameBonusJson()
	{
		// Seems like this is getting called even on reeevaluation spins where
		// we don't need to update all this data again
		if (reelGame.outcome != null)
		{
			JSON[] reevals = reelGame.outcome.getArrayReevaluations();
			for (int i = 0; i < reevals.Length; i++)
			{
				JSON currentReevalJson = reevals[i];
				string reevalType = currentReevalJson.getString("type", "");
				
				if (reevalType == SUPER_BONUS_TYPE)
				{
					// Cache out the reward info
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
		
		Debug.LogError("StickAndWinWIthBlackoutPickModule.getPickGameBonusJson() - Unable to find bonus game reward, returning NULL!");
		return null;
	}

	private JSON getSuperBonusFreespinsReevaluationJson()
	{
		JSON[] reevaluationsArrayJson = reelGame.outcome.getArrayReevaluations();
		foreach (JSON json in reevaluationsArrayJson)
		{
			string type = json.getString("type", "");
			if (type == SUPER_BONUS_TYPE)
			{
				return json;
			}
		}

		return null;
	}
	
	private int _numberOfReelSlots = 0;
	private int numberOfReelSlots
	{
		get
		{
			if (_numberOfReelSlots <= 0)
			{
				SlotReel[] reels = reelGame.engine.getAllSlotReels();
				foreach (SlotReel slotReel in reels)
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
			return (didLockAllSymbols || FreeSpinGame.instance.numberOfFreespinsRemaining <= 0);
		}
	}

	/// <summary>
	/// Get the animation data for a given reel area.  Basically even though our spin logic
	/// treats this as one giant game, it is actually treated as sections for the purpose
	/// of blackout logic.
	/// </summary>
	/// <param name="rowMin">This is the starting value for the reel row where a reel section starts, it is matched against values in our data list.</param>
	private ReelAreaBlackoutAnimData getReelAreaBlackoutAnimDataForRowMin(int rowMin)
	{
		foreach (ReelAreaBlackoutAnimData data in blackoutAnimDataArray)
		{
			if (rowMin == data.rowMin)
			{
				return data;
			}
		}

		Debug.LogWarning("MultiReelAreaStickAndWinWithBlackoutPickModule.getReelAreaBlackoutAnimDataForRowMin() - Unable to find data for rowMin = " + rowMin);
		return null;
	}

	[System.Serializable]
	private class ReelAreaBlackoutAnimData
	{
		[Tooltip("Links up to server data which also contains a row_min field which tells the starting row for the blacked out section")]
		public int rowMin;
		[Tooltip("Animations played when the first section of the reels blacks out, which unlocks and displays the multipliers as active")]
		public AnimationListController.AnimationInformationList multipliersUnlockedAnims;
		[Tooltip("Animations played for any section that blacks out after the first, these show the multiplier values increasing")] 
		public AnimationListController.AnimationInformationList multipliersIncreasedAnims;
	}
}
