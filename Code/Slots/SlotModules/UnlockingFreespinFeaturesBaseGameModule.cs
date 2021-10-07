using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class to handle new feature for elvis04 where the amount you bet in the basegame determines
what set of unlockable features are enabled when you go into a freespins game.

Creation Date: 9/4/2019
Original Author: Scott Lepthien
*/
public class UnlockingFreespinFeaturesBaseGameModule : SlotModule 
{
	private enum BetDisplayType
	{
		Lowest = 0,			// Will select the min bet amount for each tier to show
		Middle,				// Will select the middle bet amount for each tier to show
		ClosestToDefault 	// Will select bet values to show from each tier which are closest to the default bet amount we'd normally use
	}
	
	[System.Serializable]
	private class UnlockingFreespinFeatureData
	{
		[Tooltip("Tier number from the way the tier is setup on the server, this is used to auto build the name in the form: <game_key>_freespin_tier_<progressiveTierNumber>")]
		[SerializeField] public int featureTierNumber;
		[Tooltip("The pressable button that will select this tier on the bet selector")]
		[SerializeField] public UIButtonMessage betSelectorButton;
		[Tooltip("Animations played when the bet selector is shown and a specific tier is to be highlighted to indicate it is the ideal one for the player")]
		[SerializeField] public AnimationListController.AnimationInformationList featureTierHighlightedIntroAnimations;
		[Tooltip("Animations played when this tier is selected and the game is shown")]
		[SerializeField] public AnimationListController.AnimationInformationList featureTierSelectedAnimations;
		[Tooltip("Animations played when the base game is shown again after a bonus or big win, ensuring that the correct states for what is unlocked are still shown")]
		[SerializeField] public AnimationListController.AnimationInformationList onBaseGameReenabledAnimations;
		[Tooltip("Animations played when the player pressed the bet up button and increases their bet amount to this tier")]
		[SerializeField] public AnimationListController.AnimationInformationList betIncreasedToTierAnimations;
		[Tooltip("Animations played when the player pressed the bet down button and decreases their bet amount to this tier")]
		[SerializeField] public AnimationListController.AnimationInformationList betDecreasedToTierAnimations;
		[Tooltip("Controls if the bet amounts are shown as full numbers, or abbreviated to keep them short.")]
		[SerializeField] private bool isAbbreviatingBetAmounts = true;
		[Tooltip("Labels that show the bet amount for this tier (supports more than one label if we need to show it in more than one place)")]
		[SerializeField] private LabelWrapperComponent[] betAmountLabels;
		[Tooltip("Labels that show the min bet amount required for this tier (supports more than one label if we need to show it in more than one place)")]
		[SerializeField] private LabelWrapperComponent[] minBetForTierLabels;
		[Tooltip("Animations for showing that this tier is disabled because the player can't bet high enough for it")]
		[SerializeField] private AnimationListController.AnimationInformationList disableFeatureTierSelectionAnimations;
		[Tooltip("Animations for showing that this tier is enabled and can be selected (unused for now since we should only need to disable ones that can't be selected since the player can't open this bet selector again in this game type)'")]
		[SerializeField] private AnimationListController.AnimationInformationList enableFeatureTierSelectionAnimations;

		[System.NonSerialized] public long minQualifyingAmount; // This will comes down in the started info 
		[System.NonSerialized] public long maxQualifyingAmount; // This comes down in the started info
		
		private BetDisplayType betDisplayType;
		[System.NonSerialized] public long betAmountShown = 0; // Amount shown in the label

		private string featureTierKeyName;
		public string getFeatureTierKeyName()
		{
			if (string.IsNullOrEmpty(featureTierKeyName))
			{
				// build the name using the progressiveTierNumber and the game key
				featureTierKeyName = GameState.game.keyName + "_freespin_tier_" + featureTierNumber;
			}

			return featureTierKeyName;
		}

		public void init(BetDisplayType betDisplayType, long minQualifyingAmount, long maxQualifyingAmount)
		{
			this.betDisplayType = betDisplayType;
			this.minQualifyingAmount = minQualifyingAmount;
			this.maxQualifyingAmount = maxQualifyingAmount;
			
			// make sure we've created the progressiveKeyName by this point
			if (string.IsNullOrEmpty(featureTierKeyName))
			{
				featureTierKeyName = getFeatureTierKeyName();
			}
			
			betAmountShown = getBetAmountToShowForTier(betDisplayType);
			
			if (!canPlayerBetForThisTier())
			{
				betSelectorButton.enabled = false;
				// set betAmountShown to be the min value for that range
				betAmountShown = getBetAmountToShowForTier(BetDisplayType.Lowest);
				RoutineRunner.instance.StartCoroutine(playDisableTierAnimations());
			}
			
			updateBetAmountLabels();
			updateMinBetForTierLabels();
		}
		
		// Update the bet amount labels with whatever betAmountShown is, which is either a pre-set value
		// or an override which should be what the current player is betting
		private void updateBetAmountLabels()
		{
			for (int i = 0; i < betAmountLabels.Length; i++)
			{
				if (isAbbreviatingBetAmounts)
				{
					betAmountLabels[i].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(betAmountShown);
				}
				else
				{
					betAmountLabels[i].text = CreditsEconomy.convertCredits(betAmountShown);
				}
			}
		}

		// Update labels that tell how much is required to bet in order to qualify for a tier
		private void updateMinBetForTierLabels()
		{
			// Need to convert minQualifyingAmount into a valid wager amount (since it may not
			// map exactly to the wager set currently in use).
			long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);
			long minBetAmountToDisplay = findWagerValueClosestToPassedValue(minQualifyingAmount, allBetAmounts);
			
			for (int i = 0; i < minBetForTierLabels.Length; i++)
			{
				if (isAbbreviatingBetAmounts)
				{
					minBetForTierLabels[i].text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(minBetAmountToDisplay);
				}
				else
				{
					minBetForTierLabels[i].text = CreditsEconomy.convertCredits(minBetAmountToDisplay);
				}
			}
		}
		
		// Get the bet amount to show for this tier
		public long getBetAmountToShowForTier(BetDisplayType betDisplayType)
		{
			// Get the wager set info for this game since we'll need that to determine what we'll show
			long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);
			
			if (allBetAmounts.Length > 0)
			{
				switch (betDisplayType)
				{
					case BetDisplayType.Lowest:
						// find the lowest value near minQualifyingAmount
						return findWagerValueClosestToPassedValue(minQualifyingAmount, allBetAmounts);
					case BetDisplayType.Middle:
						// find the bet amount nearest the middle of minQualifyingAmount and maxQualifyingAmount
						long midPoint = minQualifyingAmount + ((maxQualifyingAmount - minQualifyingAmount) / 2);
						return findWagerValueClosestToPassedValue(midPoint, allBetAmounts);
					case BetDisplayType.ClosestToDefault:
						// find the bet amount nearest the default we would use on the SmartBetSelector
						return findWagerValueClosestToPassedValue(GameState.game.getDefaultBetValue(), allBetAmounts);
					default:
						Debug.LogError("UnlockingFreespinFeaturesBaseGameModule.getBetAmountToShowForTier() - Unknown betDisplayType = " + betDisplayType);
						return 0;
				}
			}
			else
			{
				return 0;
			}
		}

		// Play the disable animations if this tier can't be bet at
		private IEnumerator playDisableTierAnimations()
		{
			if (disableFeatureTierSelectionAnimations.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(disableFeatureTierSelectionAnimations));
			}
		}

		// Play the enable animations if this tier can be bet at
		private IEnumerator playEnableTierAnimations()
		{
			if (enableFeatureTierSelectionAnimations.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(enableFeatureTierSelectionAnimations));
			}
		}

		// Determine if the player can bet at this tier
		private bool canPlayerBetForThisTier()
		{
			long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);
			string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);

			for (int i = 0; i < allBetAmounts.Length; i++)
			{
				long wagerValue = allBetAmounts[i];

				if (wagerValue < minQualifyingAmount)
				{
					// skip values that are less than the minQualifyingAmount
					continue;
				}

				if (SlotsWagerSets.isAbleToWager(wagerSet, wagerValue))
				{
					// The player can bet at the minimum amount for the tier
					return true;
				}
				else
				{
					// The player can't bet at the minimum amount for the tier
					return false;
				}
			}

			// A valid bet couldn't be found so just return that we can't bet for this tier
			return false;
		}
		
		// Find a wager value that is closest to the passed wager value
		private long findWagerValueClosestToPassedValue(long passedValue, long[] allBetAmounts)
		{
			long betAmount = -1;
			long bestDifference = long.MaxValue;
			string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);
			for (int i = 0; i < allBetAmounts.Length; i++)
			{
				long wagerValue = allBetAmounts[i];

				if (wagerValue < minQualifyingAmount)
				{
					// skip values that are less than the minQualifyingAmount
					continue;
				}

				if (wagerValue > maxQualifyingAmount || !SlotsWagerSets.isAbleToWager(wagerSet, wagerValue))
				{
					// we've reached the upper range of qualifying amounts 
					// or what the player is able to wager so just return what we have
					if (betAmount == -1)
					{
						// we hadn't found a valid bet amount so just return the wagerValue we were on
						return wagerValue;
					}
					else
					{
						return betAmount;
					}
				}

				long currentDistance = CommonMath.abs(passedValue - wagerValue);
				if (currentDistance > bestDifference)
				{
					// the distance is increasing again which means we are getting further
					// from the closest value which means we have the closest value, so return it
					return betAmount;
				}
				else
				{
					bestDifference = currentDistance;
					betAmount = wagerValue;
				}
			}

			return betAmount;
		}
		
		// Determine if a passed wager value is between minQualifyingAmount and maxQualifyingAmount
		// and if it is, then that wager value is part of this tier
		public bool isWagerValueInTier(long wagerValue)
		{
			if (wagerValue >= minQualifyingAmount && wagerValue <= maxQualifyingAmount)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
	
	[Tooltip("List of data for animations and data associated with each one of the freespin feature tiers that this game has.")]
	[SerializeField] private UnlockingFreespinFeatureData[] freespinUnlockingFeatureDataList;
	[Tooltip("Used to extract the freespin feature tier info from the started event data, final key used will be <keyName>_freespin_tiers.")]
	[SerializeField] private string slotStartedFreespinFeaturesDataJsonKeyExtension = "_freespin_tiers";
	[Tooltip("Controls how the bet level for each tier is determined, default is to just find the valid bet in the tier which is closest to the default bet amount we'd suggest the player to bet.")]
	[SerializeField] private BetDisplayType betDisplayType = BetDisplayType.ClosestToDefault;

	private SlotBaseGame baseGame = null;
	private UnlockingFreespinFeatureData currentTierData = null;
	private bool isBetSelectorInputEnabled = true; // Bet selector will be showing by default when the game launches (leaving false for now till I have art hooked up)
	private bool isBetSelectorHidden = false;
	private TICoroutine baseGameEnabledAnimationCoroutine = null; // Used to cancel coroutine that ensures game is in correct animation state when it is enabled again on the off chance the game is quickly enabled multiple times
	private TICoroutine wagerChangeCoroutine = null; // Used to cancel a coroutine that is already happening to start a new one, since the player could change wagers quickly
	
	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		baseGame = reelGame as SlotBaseGame;
		
		if (baseGame == null)
		{
			Debug.LogError("UnlockingFreespinFeaturesBaseGameModule.executeOnSlotGameStarted() - reelGame was not a SlotBaseGame, "
							+ "this shouldn't happen, if you are looking for what to hookup for a freespins game with this feature "
							+ "you want UnlockingFreespinFeaturesFreespinModule.  Destroying this module.");
			Destroy(this);
			yield break;
		}
		
		JSON freespinFeaturesInfoJSON = getFreespinFeaturesInfoJSON(reelSetDataJson);

		if (freespinFeaturesInfoJSON == null)
		{
			Debug.LogError("UnlockingFreespinFeaturesBaseGameModule.executeOnSlotGameStarted() - Unable to find JSON entry in started info for: " + GameState.game.keyName + slotStartedFreespinFeaturesDataJsonKeyExtension);
			Destroy(this);
			yield break;
		}

#if UNITY_EDITOR
		// Do a validation check to make sure that we have data setup for all of the jackpots this game uses
		List<string> allFeatureTierKeys = freespinFeaturesInfoJSON.getKeyList();
		for (int i = 0; i < allFeatureTierKeys.Count; i++)
		{
			string key = allFeatureTierKeys[i];
			// the keys will include a "type" field which we want to skip checking
			if (key == "type")
			{
				continue;
			}
		
			if (!doesFreespinUnlockingFeatureDataListContainFeatureKey(key))
			{
				Debug.LogError("UnlockingFreespinFeaturesBaseGameModule.executeOnSlotGameStarted() - freespinUnlockingFeatureDataList is missing an entry for feature key: " + allFeatureTierKeys[i]);
			}
		}
#endif

		for (int i = 0; i < freespinUnlockingFeatureDataList.Length; i++)
		{
			UnlockingFreespinFeatureData progressive = freespinUnlockingFeatureDataList[i];
			JSON progressiveJsonData = freespinFeaturesInfoJSON.getJSON(progressive.getFeatureTierKeyName());

			if (progressiveJsonData == null)
			{
				Debug.LogError("UnlockingFreespinFeaturesBaseGameModule.executeOnSlotGameStarted() - Unable to find JSON entry for feature: " + progressive.getFeatureTierKeyName());
				Destroy(this);
				yield break;
			}

			long minQualifyingAmount = progressiveJsonData.getLong("min_qualifying_bet", 0);
			long maxQualifyingAmount = progressiveJsonData.getLong("max_qualifying_bet", 0);

			freespinUnlockingFeatureDataList[i].init(betDisplayType, minQualifyingAmount, maxQualifyingAmount);
		}

		SwipeableReel.canSwipeToSpin = false;
		
		// Determine the tier which is closest to the default bet amount we'd assign the player 
		// (i.e. the one they'd be at when loading into the game if we weren't showing this bet selector)
		currentTierData = getTierNearestDefaultBet();
		
		if (currentTierData.featureTierHighlightedIntroAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.featureTierHighlightedIntroAnimations));
		}

		yield break;
	}
	
	// Update the active features on wager change
	public override bool needsToExecuteOnWagerChange(long currentWager)
	{		
		return true;
	}

	public override void executeOnWagerChange(long currentWager)
	{
		if (currentTierData == null)
		{
			return;
		}

		// Update the top jackpot as the player changes their value
		if (wagerChangeCoroutine != null)
		{
			StopCoroutine(wagerChangeCoroutine);
		}

		wagerChangeCoroutine = StartCoroutine(handleExecuteOnWagerChangeCoroutine(currentWager));
	}
	
	// Handle running animations that need to happen when the wager changes in order to show the correct
	// jackpot on top of the game
	private IEnumerator handleExecuteOnWagerChangeCoroutine(long currentWager)
	{
		UnlockingFreespinFeatureData tierData = getTierForWagerValue(currentWager);

		if (tierData == null)
		{
			Debug.LogWarning("could not find tier data for current wager " + currentWager);
			yield break;
		}

		if (tierData != currentTierData)
		{
			UnlockingFreespinFeatureData prevTierData = currentTierData;
			currentTierData = tierData;

			if (currentTierData.minQualifyingAmount > prevTierData.minQualifyingAmount)
			{
				if (currentTierData.betIncreasedToTierAnimations.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.betIncreasedToTierAnimations));
				}
			}
			else
			{
				if (currentTierData.betDecreasedToTierAnimations.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.betDecreasedToTierAnimations));
				}
			}
		}
	}
	
	// Get the tier that is closest to the default bet amount
	private UnlockingFreespinFeatureData getTierNearestDefaultBet()
	{
		UnlockingFreespinFeatureData tier = null;
		long defaultWager = GameState.game.getDefaultBetValue();
		long bestDifference = long.MaxValue;
		string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);

		for (int i = 0; i < freespinUnlockingFeatureDataList.Length; i++)
		{
			long wagerAmountForTier = freespinUnlockingFeatureDataList[i].betAmountShown;

			// check if we can wager at the amount for this tier
			if (SlotsWagerSets.isAbleToWager(wagerSet, wagerAmountForTier))
			{
				long currentDifference = CommonMath.abs(defaultWager - wagerAmountForTier);
				if (currentDifference < bestDifference)
				{
					bestDifference = currentDifference;
					tier = freespinUnlockingFeatureDataList[i];
				}
			}
		}

		return tier;
	}
	
	// Callback for UIButtonMessage to call when a jackpot entry is pressed
	// NOTE: In order for this to work correctly the object with the button message 
	// must be setup in the UnlockingFreespinFeatureData
	private void featureTierEntryPressed(GameObject jackpotObject)
	{
		if (isBetSelectorInputEnabled)
		{
			isBetSelectorInputEnabled = false;
			StartCoroutine(featureTierEntryPressedCoroutine(jackpotObject));
		}
	}
	
	// Coroutine to handle stuff when the Callback from UIButtonMessage is triggered
	// on a freespin feature entry being pressed
	private IEnumerator featureTierEntryPressedCoroutine(GameObject jackpotObject)
	{
		currentTierData = getTierDataForPressedJackpotEntry(jackpotObject);

		baseGame.setInitialBetAmount(currentTierData.betAmountShown);
		
		if (currentTierData.featureTierSelectedAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.featureTierSelectedAnimations));
		}

		SwipeableReel.canSwipeToSpin = true;
		isBetSelectorHidden = true;
	}
	
	// Get the tier for the passed wager value, used to get the new tier when the player
	// changes the bet amount using the bet selection buttons on the SpinPanel
	private UnlockingFreespinFeatureData getTierForWagerValue(long wagerValue)
	{
		long maxQualifyingAmount = 0;
		int maxQualifyingAmountIndex = 0;

		for (int i = 0; i < freespinUnlockingFeatureDataList.Length; i++)
		{
			if (freespinUnlockingFeatureDataList[i].isWagerValueInTier(wagerValue))
			{
				return freespinUnlockingFeatureDataList[i];
			}

			// Store which progress tier has the highest amount. We do it like this to guarantee
			// we get the highest value in case the list is unordered.
			if (freespinUnlockingFeatureDataList[i].maxQualifyingAmount > maxQualifyingAmount)
			{
				maxQualifyingAmount = freespinUnlockingFeatureDataList[i].maxQualifyingAmount;
				maxQualifyingAmountIndex = i;
			}
		}

		// If the players bet is so high that it exceeds that maximum returned by the server,
		// send back the high tier from the progressive list.
		if (wagerValue >= maxQualifyingAmount)
		{
			return freespinUnlockingFeatureDataList[maxQualifyingAmountIndex];
		}

		return null;
	}
	
	// Get the corresponding UnlockingFreespinFeatureData for a jackpot entry that was pressed
	private UnlockingFreespinFeatureData getTierDataForPressedJackpotEntry(GameObject jackpotObject)
	{
		for (int i = 0; i < freespinUnlockingFeatureDataList.Length; i++)
		{
			if (freespinUnlockingFeatureDataList[i].betSelectorButton.gameObject == jackpotObject)
			{
				return freespinUnlockingFeatureDataList[i];
			}
		}

		return null;
	}
	
	// Extract the jackpot info from the game started JSON
	private JSON getFreespinFeaturesInfoJSON(JSON reelSetDataJson)
	{
		JSON[] modifiers = reelSetDataJson.getJsonArray("modifier_exports");

		JSON jackpotInfoJSON = null;
		for (int i = 0;i < modifiers.Length; i++)
		{
			jackpotInfoJSON = modifiers[i].getJSON(GameState.game.keyName + slotStartedFreespinFeaturesDataJsonKeyExtension);
			if (jackpotInfoJSON != null)
			{
				return jackpotInfoJSON;
			}
		}

		return null;
	}
	
	// Check if the freespin features list contains the passed in feature key
	private bool doesFreespinUnlockingFeatureDataListContainFeatureKey(string featureKey)
	{
		for (int i = 0; i < freespinUnlockingFeatureDataList.Length; i++)
		{
			string currentEntryKey = freespinUnlockingFeatureDataList[i].getFeatureTierKeyName();
			if (featureKey == currentEntryKey)
			{
				return true;
			}
		}

		return false;
	}

	public override bool needsToExecuteOnDoSpecialOnBonusGameEnd()
	{
		return (currentTierData != null && currentTierData.onBaseGameReenabledAnimations.Count > 0);
	}

	public override void executeOnDoSpecialOnBonusGameEnd()
	{
		// Make sure we cancel any previous enable animations coroutine that might still be running
		// before firing off the new one
		if (baseGameEnabledAnimationCoroutine != null)
		{
			StopCoroutine(baseGameEnabledAnimationCoroutine);
		}

		baseGameEnabledAnimationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.onBaseGameReenabledAnimations));
	}

	// executeOnShowSlotBaseGame() section
	// Functions here are executed when the base game is restored after being hidden by events like BigWin effect or full screen dialogs,
	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return (currentTierData != null && currentTierData.onBaseGameReenabledAnimations.Count > 0);
	}

	public override void executeOnShowSlotBaseGame()
	{
		// Make sure we cancel any previous enable animations coroutine that might still be running
		// before firing off the new one
		if (baseGameEnabledAnimationCoroutine != null)
		{
			StopCoroutine(baseGameEnabledAnimationCoroutine);
		}

		baseGameEnabledAnimationCoroutine = StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.onBaseGameReenabledAnimations));
	}
	
	// isBlockingWebGLKeyboardInputForSlotGame() section
	// Used to block the game from accepting WebGL game input
	// for instance if the game has a psuedo dialog like elvis03
	// where we have a bet selector that goes over the game and
	// should prevent interaction with the game behind it, this
	// way the player can't use the space bar to spin or arrow keys
	// to change bet amount while this dialog is up
	public override bool isBlockingWebGLKeyboardInputForSlotGame()
	{
		return !isBetSelectorHidden;
	}
	
	// isModulePreventingBaseGameAudioFade() section
	// Used to prevent audio from fading out after a set amount of time
	// can be used for stuff like elvis03 bet selector where sound should
	// not fade until the player has picked a bet and is actually into the 
	// base game
	public override bool isModulePreventingBaseGameAudioFade()
	{
		return !isBetSelectorHidden;
	}
}
