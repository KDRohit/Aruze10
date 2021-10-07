/*
 * FreeSpinBattleModeModule.cs
 * This module handles the Battle Mode mutation during the freespins game.
 * This module retrieves the data from the MutationManager and triggers good/wild animations based on the outcome.
 * It can also create a picking game from the loot data sent by the server on a win outcome.
 * 
 * Original author - Abhishek Singh
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeSpinBattleModeModule : SlotModule
{
	[SerializeField] private int totalPipCount = 3;
	[SerializeField] private string winOutcomeSkinName;
	[SerializeField] private string loseOutcomeSkinName;
	[SerializeField] private AnimationListController.AnimationInformationList[] goodPipAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList[] badPipAnimations;
	[SerializeField] private string GOOD_WILD_SYMBOL_NAME = "GW";
	[SerializeField] private AnimationListController.AnimationInformationList goodWildOverlayAnimations;
	[SerializeField] private string BAD_WILD_SYMBOL_NAME = "BW";
	[SerializeField] private AnimationListController.AnimationInformationList badWildOverlayAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList overlayEndAnimations;
	[SerializeField] private bool isOverridingOverlayAnimationDuration = false;
	[SerializeField] private bool isOverridingSymbolAnticipationDuration = false;
	[SerializeField] private float DELAY_BEFORE_WILD_POPULATE = 0.65f;
	[SerializeField] private float DELAY_BETWEEN_WILD_POPULATE = 0.35f;
	[SerializeField] private float DELAY_AFTER_WILD_POPULATE = 0.75f;
	[SerializeField] private string WILD_POPULATE_MUTATION_SUFFIX = "_Mutate"; //  a symbol suffix for interim wild mutations
	[SerializeField] private AnimationListController.AnimationInformationList gameWinAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList gameLoseAnimations;
	[SerializeField] private bool isWinLeadingToPickGame;
	[SerializeField] private ModularChallengeGame pickingGame;
	[SerializeField] private string summaryFanfareGood = "freespin_summary_fanfare";
	[SerializeField] private string summaryFanfareBad = "freespin_summary_fanfare_v2";
	[SerializeField] private float FADE_TIME = 0.5f;
	[SerializeField] private List<GameObject> objectsToFade;
	[SerializeField] private List<LabelWrapperComponent> extraWinLabels;

	[SerializeField] private AudioListController.AudioInformationList goodSymbolAudio;
	[SerializeField] private AudioListController.AudioInformationList badSymbolAudio;
	
	// Wild mutation related parameters
	private StandardMutation wildPopulateMutation;
	private List<Reveal> pickGameItems;
	private int goodWildCount = 0;
	private int badWildCount = 0;
	private bool goodWildWon = false;
	private bool winOutcome = false;
	private bool battleWildAnimationDone = false;
	private int battleWildMutationPlaying = 0;
	private const float SPIN_PANEL_SLIDE_TIME = 0.25f;
	

	public override void Awake()
	{
		base.Awake();

		if (winOutcomeSkinName == "" || loseOutcomeSkinName == "")
		{
			Debug.LogError("Outcome skin name(s) not specified in " + this.GetType().Name + " - Destroying script.");
			Destroy(this);
		}
		if (isWinLeadingToPickGame && pickingGame == null)
		{
			Debug.LogError("Picking game not defined for the win outcome in " + this.GetType().Name + " - Destroying script.");
			Destroy(this);
		}
	}

	// Reset mutation variable before every spin
	public override bool needsToExecuteOnPreSpin()
	{
		wildPopulateMutation = null;
		return false;
	}

	// Ensure reels are evaluated after every spin
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		reelGame.mutationManager.setMutationsFromOutcome(reelGame.outcome.getJsonObject());
		return (reelGame.mutationManager.mutations.Count > 0);
	}

	// Check if the spin resulted in battle mode and evaluate result
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// do one loop through to make sure we have the wild population data before we do the actual battle part
		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];

			if (mutation.type == "symbol_rise_and_fall")
			{
				wildPopulateMutation = mutation as StandardMutation;
			}
		}

		for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
		{
			MutationBase mutation = reelGame.mutationManager.mutations[i];
			
			if (mutation.type == "free_spin_battle_mode")
			{
				StandardMutation battleMutation = mutation as StandardMutation;
				goodWildWon = battleMutation.didWin;
				winOutcome = (battleMutation.reveals != null) ? true : false;
				if (winOutcome)
				{
					pickGameItems = battleMutation.reveals;
				}

				// Trigger handling of the special Wild symbol
				yield return StartCoroutine(mutateSpecialWilds());

				// Update the pips
				if (goodWildWon)
				{
					if (goodWildCount >= 0 && goodWildCount < goodPipAnimations.Length)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(goodPipAnimations[goodWildCount]));
					}
					else
					{
						Debug.LogError("Index out of range. Check if animation is defined for good pip count: " + goodWildCount);
					}
					goodWildCount++;
				}
				else
				{
					if (badWildCount >= 0 && badWildCount < badPipAnimations.Length)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(badPipAnimations[badWildCount]));
					}
					else
					{
						Debug.LogError("Index out of range. Check if animation is defined for bad pip count: " + badWildCount);
					}
					badWildCount++;
				}
			}
		}

		// Verify if end-game conditions are met
		evaluateResult();
	}

	// Delegate needed for call to symbol anticipation animation
	private void battleWildAnimationCompleted(SlotSymbol sender)
	{
		battleWildAnimationDone = true;
	}

	private IEnumerator mutateSpecialWilds()
	{
		battleWildAnimationDone = false;
		List<SlotSymbol> allVisibleSymbols = reelGame.engine.getAllVisibleSymbols();
		AnimationListController.AnimationInformationList overlayAnimations = goodWildWon ? goodWildOverlayAnimations : badWildOverlayAnimations;
		TICoroutine animationCoroutine = null;

		foreach (SlotSymbol symbol in allVisibleSymbols)
		{
			if (symbol.serverName == GOOD_WILD_SYMBOL_NAME || symbol.serverName == BAD_WILD_SYMBOL_NAME)
			{
				// Save the symbol aticipation duration to be restored before it's cached
				float originalAnimationDuration = symbol.info.customAnimationDurationOverride;

				// Calculate the time required for all the WD symbols to be populated
				if (wildPopulateMutation != null)
				{
					float wildPopulateDuration = DELAY_BEFORE_WILD_POPULATE + (DELAY_BETWEEN_WILD_POPULATE * (wildPopulateMutation.twMutatedSymbolList.Count + 1));

					// Change duration of animations
					if (overlayAnimations != null && isOverridingOverlayAnimationDuration)
					{
						foreach (AnimationListController.AnimationInformation anim in overlayAnimations.animInfoList)
						{
							anim.durationOverride = wildPopulateDuration;
						}
					}

					if (isOverridingSymbolAnticipationDuration)
					{
						symbol.info.customAnimationDurationOverride = Mathf.Max(originalAnimationDuration, wildPopulateDuration);
					}
				}

				// play symbol audio list for good / bad wild landing
				if (symbol.serverName == GOOD_WILD_SYMBOL_NAME)
				{
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(goodSymbolAudio));
				}
				else
				{
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(badSymbolAudio));	
				}
				
				symbol.animateAnticipation(battleWildAnimationCompleted);

				if (overlayAnimations != null)
				{
					animationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(overlayAnimations));
				}

				// If the special wilds were first or last symbol (respectively) on reel, then WD mutations are not needed
				if (wildPopulateMutation != null)
				{
					yield return new TIWaitForSeconds(DELAY_BEFORE_WILD_POPULATE);
					yield return StartCoroutine(activateWilds());
					yield return new TIWaitForSeconds(DELAY_AFTER_WILD_POPULATE);
				}

				// Wait until the special wild or overlay animations have finished animating
				while (!battleWildAnimationDone || battleWildMutationPlaying > 0 ||
					(animationCoroutine != null && !animationCoroutine.isFinished))
				{
					yield return null;
				}
				
				// Reset the symbol animation duration
				symbol.info.customAnimationDurationOverride = originalAnimationDuration;

				// Play overlay end animations
				if (overlayEndAnimations != null)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(overlayEndAnimations));
				}

				// Currently, only looks for the first BW/GW symbol. If there are multiple GW/BW symbols in future games, this
				// logic would have to change along with how the mutation data is populated in MutationManager.cs
				break;
			}
		}
	}

	// The WD symbols that populate as the special object moves across the reel
	private IEnumerator activateWilds()
	{
		//Populate wilds in the middle reel
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (StandardMutation.ReplacementCell replacementCell in wildPopulateMutation.twMutatedSymbolList)
		{
			SlotSymbol symbol = reelArray[replacementCell.reelIndex].visibleSymbolsBottomUp[replacementCell.symbolIndex];
			yield return new TIWaitForSeconds(DELAY_BETWEEN_WILD_POPULATE);

			// if we've defined an interim symbol, mutate to it first and queue up the final outcome mutation
			if (string.IsNullOrEmpty(WILD_POPULATE_MUTATION_SUFFIX))
			{
				symbol.mutateTo(replacementCell.replaceSymbol);
			}
			else
			{
				battleWildMutationPlaying++;
				symbol.mutateTo(replacementCell.replaceSymbol + WILD_POPULATE_MUTATION_SUFFIX);
				StartCoroutine(finishMutationEffect(symbol, replacementCell));
			}
		}
	}

	private IEnumerator finishMutationEffect(SlotSymbol symbol, StandardMutation.ReplacementCell replacementCell )
	{
		// wait for the mutation animation to finish, then mutate to the final symbol
		while (symbol.animator.isAnimating)
		{
			yield return null;
		}
		symbol.mutateTo(replacementCell.replaceSymbol);
		battleWildMutationPlaying--;
	}

	private void evaluateResult()
	{
		if (goodWildCount >= totalPipCount || badWildCount >= totalPipCount)
		{
			string outcomeName = winOutcome ? winOutcomeSkinName : loseOutcomeSkinName;
			PlayerPrefsCache.SetString(string.Format(Prefs.GAME_BACKGROUND_SKIN, GameState.game.keyName), outcomeName);

			// Trigger end of freespins
			reelGame.numberOfFreespinsRemaining = 0;
		}
	}

	// Enable round end transitions
	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return (gameWinAnimations != null || gameLoseAnimations != null);
	}

	// Executes the defined animation on round end
	public override IEnumerator executeOnFreespinGameEnd()
	{
		// Clear the outcome to stop the looping since we loop freespin outcomes now
		ReelGame.activeGame.outcomeDisplayController.clearOutcome();

		// ensure that we don't continue to cycle paylines while fading out
		if (ReelGame.activeGame != null && ReelGame.activeGame.outcomeDisplayController != null)
		{
			while (ReelGame.activeGame.outcomeDisplayController.getCurrentState() != OutcomeDisplayController.DisplayState.Off)
			{
				yield return null;
			}			
		}
		
		// If we have winboxes we should update their display values
		if (extraWinLabels != null && extraWinLabels.Count > 0)
		{
			for (int i = 0; i < extraWinLabels.Count; i++)
			{
				extraWinLabels[i].text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}
		}

		// Hide the spin panel
		RoutineRunner.instance.StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, SPIN_PANEL_SLIDE_TIME, false));
		SpinPanel.instance.showFeatureUI(false);

		// prior to fade, flatten symbols to avoid animations overriding alpha
		for (int reelIndex = 0; reelIndex < reelGame.engine.getReelRootsLength(); reelIndex++)
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (!symbol.isFlattenedSymbol)
				{
					symbol.mutateToFlattenedVersion(null, false, true, false);					
				}
			}
		}
		
		float elapsedTime = 0.0f;
		while (elapsedTime < FADE_TIME)
		{
			elapsedTime += Time.deltaTime;
			foreach(GameObject objectToFade in objectsToFade)
			{
				CommonGameObject.alphaGameObject(objectToFade, 1 - (elapsedTime / FADE_TIME));
			}
			yield return null;
		}

		foreach (GameObject objectToFade in objectsToFade)
		{
			if (objectToFade != null)
			{
				CommonGameObject.alphaGameObject(objectToFade, 0.0f);
			}
			else
			{
				Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
			}
		}

		AnimationListController.AnimationInformationList animInfoList = winOutcome ? gameWinAnimations : gameLoseAnimations;
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animInfoList));

		if (winOutcome)
		{
			BonusGamePresenter.instance.FREESPIN_SUMMARY_FANFARE = summaryFanfareGood; // override final summary
		}
		else
		{
			BonusGamePresenter.instance.FREESPIN_SUMMARY_FANFARE = summaryFanfareBad;
		}

		if (winOutcome && isWinLeadingToPickGame)
		{
			yield return StartCoroutine(startBonusPickGame());
		}
	}

	// Creates a pick game if the battle result was a win
	private IEnumerator startBonusPickGame()
	{
		// Create a PickemOutcome to store our result from the freespin battle mode
		PickemOutcome pickGameOutcome = new PickemOutcome();
		pickGameOutcome.entries = new List<PickemPick>();
		pickGameOutcome.reveals = new List<PickemPick>();

		// Manually populate the data in PickemOutcome as the server does not send data for the pick game separately
		foreach (Reveal revealItem in pickGameItems)
		{
			// Create PickemPick item
			PickemPick pick = new PickemPick();
			if (revealItem.multiplier)
			{
				// FIXME: Manual parsing because server multiplier data has 'x' character in it. Use commented version later
				// pick.multiplier = revealItem.value;
				pick.multiplier = 2;
			}
			else
			{
				pick.baseCredits = revealItem.value;
			}

			// Ensure all multipliers are accounted for since we only receive the raw base value from server data
			pick.credits = pick.baseCredits * BonusGameManager.instance.currentMultiplier * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;

			// Add to our PickemOutcome
			if (revealItem.selected)
			{
				pickGameOutcome.entries.Add(pick);
			}
			else
			{
				pickGameOutcome.reveals.Add(pick);
			}
		}

		// Convert our outcome to ModularChallengeGameOutcome
		ModularChallengeGameOutcome modularPickGameOutcome = new ModularChallengeGameOutcome(pickGameOutcome);

		List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
		variantOutcomeList.Add(modularPickGameOutcome);
		pickingGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
		pickingGame.init();
		pickingGame.gameObject.SetActive(true);

		// Wait till this pick game feature is over before continuing
		while (BonusGamePresenter.instance.isGameActive)
		{
			yield return null;
		}
	}
}
 