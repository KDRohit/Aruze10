using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module is used for progressive scatter symbol jackpots where the player personally
// increases their jackpot totals by spinning the game
//
// games : gen78, gen79, gen80, gen85, bettie02
//
public class ProgressiveScatterJackpotsModule : SlotModule
{
	//Used to ensure that the labels, animations, and jackpot data are properly tied together without the hassle of needing to index a bunch of seperate lists
	[System.Serializable]
	private class JackpotContainer
	{
		//We will want a way to match mutation data since the jackpots may not come down in order or with numerically parsable keys in the future
		public string jackpotKeyValue = "";
		//Related animations for the jackpot ui elements
		public AnimationListController.AnimationInformationList selectedAnimations;
		public AnimationListController.AnimationInformation activatedAnimationState;
		public AnimationListController.AnimationInformationList winIntroAnimations;
		public AnimationListController.AnimationInformationList winOutroAnimations;
		public AnimationListController.AnimationInformationList deselectedAnimations;
		//The label that holds current value of the jackpot
		public LabelWrapperComponent jackpotLabel;
		// Tells if the jackpotLabel should be displayed abbreviated
		public bool isAbbreviatingJackpotLabel;
		//The labels that hold the bet threshhold value
		public LabelWrapperComponent minBetLabel;
		//Data variables to be updated on each spin and on the initial game load
		[HideInInspector] public long minQualifyingBet;
		[HideInInspector] public long baseJackpotPayout;
		[HideInInspector] public long personalContributionAmount;
		[HideInInspector] public bool activated = false;
	}

	[SerializeField] private List<JackpotContainer> jackpotContainers = new List<JackpotContainer>();

	[SerializeField] private List<AudioListController.AudioInformationList> jackpotSymbolLandingSounds = new List<AudioListController.AudioInformationList>();
	[SerializeField] private AudioListController.AudioInformationList jackpotSymbolWinSounds = new AudioListController.AudioInformationList();

	[SerializeField] bool animateScatterOnRollbackEnd;

	private const float PRE_WIN_ANIMATIONS_DELAY = 0.5f;

	//The tracked mutation
	private StandardMutation progressiveScatterJackpotMutation = null;
	private int numberOfJackpotSymbolsThisSpin = 0;
	private bool isRollingUp = false;
	//Use to ensure MinQualifyingBetSet has been calculated.
	[HideInInspector] private bool isDataInitalized = false;

	protected override void OnEnable()
	{
		base.OnEnable();
		resetAnimationStates();
	}

	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}

	public override void executeOnBigWinEnd()
	{
		resetAnimationStates();
		reInitializeJackpotData();
	}

	private void resetAnimationStates()
	{
		for (int i = 0; i < jackpotContainers.Count; i++)
		{
			if (jackpotContainers[i].activated)
			{
				StartCoroutine(AnimationListController.playAnimationInformation(jackpotContainers[i].activatedAnimationState));
			}
			else
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].deselectedAnimations));
			}
		}
	}

	// When the game first starts, we get the saved user data from the server from the modifier_exports
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return reelGame.modifierExports != null;
	}

	// Get the players startup data so that we can initialize the free spin meters
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		JSON personalJackpotModifierExportJSON = getPersonalJackpotModifierExportJSON();

		if (personalJackpotModifierExportJSON == null)
		{
			yield break;
		}

		List<string> jackpotKeys = personalJackpotModifierExportJSON.getKeyList();

		foreach (string jackpotKey in jackpotKeys)
		{
			JackpotContainer jackpotContainer = getJackpotContainerWithKey(jackpotKey);

			if (jackpotContainer != null)
			{
				JSON jackpotJSON = personalJackpotModifierExportJSON.getJSON(jackpotKey);
				yield return StartCoroutine(initializeJackpotContainer(jackpotContainer, jackpotJSON));
			}
		}

		isDataInitalized = true;
	}

	private void reInitializeJackpotData()
	{
		string jackpotKey = reelGame.outcome.getPersonalJackpotKey();

		if (string.IsNullOrEmpty(jackpotKey))
		{
			return;
		}

		ReevaluationPersonalJackpotReevaluator personalJackpotReeval = null;
		
		JSON[] arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		foreach (JSON reeval in arrayReevaluations)
		{
			string reevalType = reeval.getString("type", "");
			if (reevalType != "personal_jackpot_reevaluator")
			{
				continue;
			}
			
			personalJackpotReeval =  new ReevaluationPersonalJackpotReevaluator(reeval);
			break;
		}

		if (personalJackpotReeval == null)
		{
			return;
		}
		
		JackpotContainer jackpotContainer = getJackpotContainerWithKey(jackpotKey);

		ReevaluationPersonalJackpotReevaluator.PersonalJackpot personalJackpot = personalJackpotReeval.personalJackpotList.Find(x => (x.name == jackpotKey));

		if (jackpotContainer == null || personalJackpot == null)
		{
			return;
		}
		
		jackpotContainer.personalContributionAmount = personalJackpot.contributionAmount;
		jackpotContainer.baseJackpotPayout = personalJackpot.basePayout;

		if (jackpotContainer.jackpotLabel != null)
		{
			jackpotContainer.jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainer);
		}
	}

	// Personal Jackpot is buried in a with a key that varies by game, but we can find it
	// using the type within the JSON.
	private JSON getPersonalJackpotModifierExportJSON()
	{
		foreach (JSON modifierExportJSON in reelGame.modifierExports)
		{
			List<string> modifierExportKeys = modifierExportJSON.getKeyList();

			foreach (string key in modifierExportKeys)
			{
				if (!string.IsNullOrEmpty(key))
				{
					JSON modifierExportContentJSON = modifierExportJSON.getJSON(key);

					if (modifierExportContentJSON != null && modifierExportContentJSON.getString("type", "") == "personal_jackpot")
					{
						return modifierExportContentJSON;
					}

					JSON[] modifierExportContentJSONArray = modifierExportJSON.getJsonArray(key);

					if (modifierExportContentJSONArray != null)
					{
						foreach (JSON mod in modifierExportContentJSONArray)
						{
							if (mod.getString("type", "") == "fixed" || mod.getString("type", "") == "pjp")
							{
								return mod;
							}
						}
					}
				}
			}
		}

		return null;
	}
	
	private IEnumerator initializeJackpotContainer(JackpotContainer jackpotContainer, JSON jackpotJSON)
     	{
     		long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);

     		jackpotContainer.minQualifyingBet = long.Parse(jackpotJSON.getString("min_qualifying_bet", ""));
     		jackpotContainer.personalContributionAmount = long.Parse(jackpotJSON.getString("contrib_amount", ""));
     		jackpotContainer.baseJackpotPayout = long.Parse(jackpotJSON.getString("base_payout", ""));

     		if (jackpotContainer.jackpotLabel != null)
     		{
     			//From the design doc:
     			//JACKPOT DISPLAY = (JACKPOT BASE VALUE x WAGER MULTIPLIER) + CONTRIBUTION BUCKET
     			//this also needed the economy multiplier attached to it
     			jackpotContainer.jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainer);
     		}

     		if (jackpotContainer.minBetLabel != null)
     		{
     			//round up to nearest bet
     			long minBet = jackpotContainer.minQualifyingBet;

     			for (int betIndex = 0; betIndex < allBetAmounts.Length; ++betIndex)
     			{
     				if (allBetAmounts[betIndex] >= minBet)
     				{
     					minBet = allBetAmounts[betIndex];
     					break;
     				}
     			}

     			jackpotContainer.minBetLabel.text =
     				CommonText.formatNumberAbbreviated(minBet * CreditsEconomy.economyMultiplier);
     		}

     		//Play the animation for the defualt states of the jackpot ui
     		if (reelGame.currentWager >= jackpotContainer.minQualifyingBet)
     		{
     			jackpotContainer.activated = true;
     			yield return StartCoroutine(
     				AnimationListController.playAnimationInformation(jackpotContainer.activatedAnimationState));
     		}
     		else if (reelGame.currentWager < jackpotContainer.minQualifyingBet)
     		{
     			jackpotContainer.activated = false;
     			yield return StartCoroutine(
     				AnimationListController.playListOfAnimationInformation(jackpotContainer.deselectedAnimations));
     		}
     	}

	private JackpotContainer getJackpotContainerWithKey(string jackpotKey)
	{
		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			if (jackpotContainer.jackpotKeyValue == jackpotKey)
			{
				return jackpotContainer;
			}
		}
		return null;
	}

	//Update the jackpot ui when coming back from a freespins game
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		long betAmount = 0;
		if (reelGame.isFreeSpinGame())
		{
			if (SlotBaseGame.instance != null)
			{
				copyValuesFromBasegame();
			}
			else
			{
				getValuesForGiftedFreespinsGame();
			}

			betAmount = reelGame.slotGameData.baseWager * FreeSpinGame.instance.multiplier;
			for (int i = 0; i < jackpotContainers.Count; i++)
			{
				jackpotContainers[i].jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainers[i]);
				if (betAmount >= jackpotContainers[i].minQualifyingBet && !jackpotContainers[i].activated)
				{
					jackpotContainers[i].activated = true;
					StartCoroutine(AnimationListController.playAnimationInformation(jackpotContainers[i].activatedAnimationState));
				}
				else if (betAmount < jackpotContainers[i].minQualifyingBet && jackpotContainers[i].activated)
				{
					jackpotContainers[i].activated = false;
					StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].deselectedAnimations));
				}
			}
		}
	}

	//Update the active jackpots on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return true;
	}
	public override void executeOnWagerChange(long currentWager)
	{
		if (isDataInitalized)
		{
			int qualifingMinBetIndex = -1;
			long currentMin = 0;
			for (int i = 0; i < jackpotContainers.Count; i++)
			{
				if (currentMin <= jackpotContainers[i].minQualifyingBet)
				{
					if (currentWager >= jackpotContainers[i].minQualifyingBet)
					{
						qualifingMinBetIndex = i;
						currentMin = jackpotContainers[i].minQualifyingBet;
					}
				}
			}

			//bool animationAudioPlayed = false;
			for (int i = 0; i < jackpotContainers.Count; i++)
			{
				jackpotContainers[i].jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainers[i]);
				if (currentWager >= jackpotContainers[i].minQualifyingBet && !jackpotContainers[i].activated)
				{
					jackpotContainers[i].activated = true;
					StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].selectedAnimations, null, qualifingMinBetIndex == i));

				}
				else if (currentWager < jackpotContainers[i].minQualifyingBet && jackpotContainers[i].activated)
				{
					jackpotContainers[i].activated = false;
					StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].deselectedAnimations));
				}
			}
		}
	}

	//Update the Jackpot balances once we get the spin outcome back
	public override bool needsToExecutePreReelsStopSpinning()
	{
		reelGame.mutationManager.setMutationsFromOutcome(reelGame.outcome.getJsonObject(), true);

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null && reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				if (baseMutation.type == "personal_jackpot_reevaluator")
				{
					progressiveScatterJackpotMutation = baseMutation as StandardMutation;
				}
			}
		}
		return (progressiveScatterJackpotMutation != null);
	}
	public override IEnumerator executePreReelsStopSpinning()
	{
		//Update the jackpot ui items
		for (int i = 0; i < jackpotContainers.Count; i++)
		{
			jackpotContainers[i].personalContributionAmount = progressiveScatterJackpotMutation.progressiveScatterJackpots[i].jackpotBalance;
			if (jackpotContainers[i].jackpotLabel != null)
			{
				jackpotContainers[i].jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainers[i]);
			}
		}
		yield break;
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return (animateScatterOnRollbackEnd && jackpotSymbolLandingSounds.Count > 0);
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		yield return StartCoroutine(animateScatterSymbols(reel));
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return (!animateScatterOnRollbackEnd && jackpotSymbolLandingSounds.Count > 0);
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		yield return StartCoroutine(animateScatterSymbols(stoppedReel));
	}

	public IEnumerator animateScatterSymbols(SlotReel reel)
	{
		bool hasScatterSymbolLanded = false;

		//If we have a scatter symbol we want to play sound
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			//Since all the balls that drop are considered scatter symbols we need to check for the trigger joker specifically
			if (reel.visibleSymbols[i].isScatterSymbol)
			{
				numberOfJackpotSymbolsThisSpin++;
				reel.visibleSymbols[i].animateAnticipation();
				hasScatterSymbolLanded = true;
			}
		}

		if (hasScatterSymbolLanded)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotSymbolLandingSounds[numberOfJackpotSymbolsThisSpin - 1]));
		}
	}

	//Award Jackpot once reels are finished spinning
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return (progressiveScatterJackpotMutation != null && progressiveScatterJackpotMutation.creditsAwarded > 0);
	}
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//slight pause to allow for a more natural timing
		yield return new TIWaitForSeconds(PRE_WIN_ANIMATIONS_DELAY);

		isRollingUp = true;

		foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (symbol.isScatterSymbol)
			{
				StartCoroutine(playScatterSymbolAnimation(symbol));
			}
		}

		//Play outcome sounds
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotSymbolWinSounds));

		for (int i = 0; i < jackpotContainers.Count; i++)
		{
			if (jackpotContainers[i].jackpotKeyValue == progressiveScatterJackpotMutation.jackpotKey)
			{
				//Run through the list of win animations
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].winIntroAnimations));
			}
		}

		//Add credit to player and roll up winnings
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.addCreditsToSlotsPlayer(progressiveScatterJackpotMutation.creditsAwarded, "spin outcome", shouldPlayCreditsRollupSound: false);
		}
		Overlay.instance.top.updateCredits(false); //Rollup the top overlay to the new player amount

		//Wait for the rollup to finish animating
		yield return StartCoroutine(reelGame.rollupCredits(0,
			progressiveScatterJackpotMutation.creditsAwarded,
			ReelGame.activeGame.onPayoutRollup,
			isPlayingRollupSounds: true,
			specificRollupTime: 0.0f,
			shouldSkipOnTouch: true,
			allowBigWin: false));

		isRollingUp = false;

		//Play jackpot outro animations
		for (int i = 0; i < jackpotContainers.Count; i++)
		{
			if (jackpotContainers[i].jackpotKeyValue == progressiveScatterJackpotMutation.jackpotKey)
			{
				//Run through the list of win animations
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotContainers[i].winOutroAnimations));

				//Reset jackpot label to the base amount
				if (jackpotContainers[i].jackpotLabel != null)
				{
					//Have to make sure to set this back to 0 so on wager change it doesn't repopulate with the old value.
					jackpotContainers[i].personalContributionAmount = 0;
					jackpotContainers[i].jackpotLabel.text = getProgressiveJackpotLabelText(jackpotContainers[i]);
				}
			}
		}
	}

	private IEnumerator playScatterSymbolAnimation(SlotSymbol symbol)
	{
		while (isRollingUp)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		}
	}

	//Reset the win tracking variables
	public override bool needsToExecuteOnPreSpin()
	{
		return (progressiveScatterJackpotMutation != null);
	}
	public override IEnumerator executeOnPreSpin()
	{
		progressiveScatterJackpotMutation = null;
		numberOfJackpotSymbolsThisSpin = 0;
		yield return null;
	}

	// Helper function to get the same module attached to the base game
	private ProgressiveScatterJackpotsModule getBaseGameModule()
	{
		if (SlotBaseGame.instance != null)
		{
			for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
			{
				ProgressiveScatterJackpotsModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as ProgressiveScatterJackpotsModule;
				if (module != null)
				{
					return module;
				}
			}
		}

		return null;
	}

	// Copy game init values over to freespins
	private void copyValuesFromBasegame()
	{
		ProgressiveScatterJackpotsModule baseGameModule = getBaseGameModule();
		if (baseGameModule == null)
		{
			return;
		}

		long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);

		for (int i = 0; i < jackpotContainers.Count; i++)
		{
			if (baseGameModule.jackpotContainers.Count >= i)
			{
				jackpotContainers[i].minQualifyingBet = baseGameModule.jackpotContainers[i].minQualifyingBet;
				jackpotContainers[i].baseJackpotPayout = baseGameModule.jackpotContainers[i].baseJackpotPayout;
				jackpotContainers[i].personalContributionAmount = baseGameModule.jackpotContainers[i].personalContributionAmount;
			}

			if (jackpotContainers[i].minBetLabel != null)
			{
				long minBet = jackpotContainers[i].minQualifyingBet;

				for (int j = 0; i < allBetAmounts.Length; ++j)
				{
					if (allBetAmounts[j] >= minBet)
					{
						minBet = allBetAmounts[j];
						break;
					}
				}
				jackpotContainers[i].minBetLabel.text = CommonText.formatNumberAbbreviated(minBet * CreditsEconomy.economyMultiplier);
			}
		}
	}

	// in normal freespins we get values from basegame, but for gifted freespins, values are in reelInfo
	private void getValuesForGiftedFreespinsGame()
	{
		JSON[] reelInfo = reelGame.reelInfo;
		if (reelInfo == null)
		{
			return;
		}

		foreach (JSON json in reelInfo)
		{
			if (json.getString("type", "") == "freespin_background")
			{
				JSON personalJackpots = json.getJSON("personal_jackpot");
				if (personalJackpots == null)
				{
					break;
				}
				
				List<string> jackpotKeys = personalJackpots.getKeyList();
				foreach (string jackpotKey in jackpotKeys)
				{
					JackpotContainer jackpotContainer = getJackpotContainerWithKey(jackpotKey);
					if (jackpotContainer != null)
					{
						JSON jackpotJSON = personalJackpots.getJSON(jackpotKey);
						StartCoroutine(initializeJackpotContainer(jackpotContainer, jackpotJSON));
					}
				}				
			}
		}
	}

	private string getProgressiveJackpotLabelText(JackpotContainer jackpotContainer)
	{
		if (jackpotContainer != null)
		{
			long totalProgressiveAmount = (jackpotContainer.baseJackpotPayout * reelGame.multiplier) + jackpotContainer.personalContributionAmount;
			if (jackpotContainer.isAbbreviatingJackpotLabel)
			{
				return CreditsEconomy.multiplyAndFormatNumberAbbreviated(totalProgressiveAmount);
			}
			else
			{
				return CreditsEconomy.convertCredits(totalProgressiveAmount);
			}
		}
		else
		{
			Debug.LogError("ProgressiveScatterJackpotsModule.getProgressiveJackpotLabelText() - jackpotContainer was null!");
			return "";
		}
	}
}
