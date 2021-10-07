using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/**
Class to handle features like Elvis03 which has a built in progressive jackpot.

Creation Date: 4/24/2018
Original Author: Scott Lepthien
*/
public class BuiltInProgressiveJackpotBaseGameModule : SlotModule 
{
	public enum BetDisplayType
	{
		Lowest = 0,			// Will select the min bet amount for each tier to show
		Middle,				// Will select the middle bet amount for each tier to show
		Closest_To_Default // Will select bet values to show from each teir which are closest to the default bet amount we'd normally use
	}

	[System.Serializable]
	public class BuiltInProgressiveJackpotTierData
	{
		[Tooltip("Tier number from the way the tier is setup in SCAT/Admin tool, not this is used to auto build the name in the form: hir_<gameKey>_blackout_tier_<progressiveTierNumber>")]
		[SerializeField] public int progressiveTierNumber;
		[Tooltip("Lables which will be registered to show the jackpot value")]
		[SerializeField] private LabelWrapperComponent[] valueLabels;
		[Tooltip("Lables which will show the suggested bet amount for a tier")]
		[SerializeField] private LabelWrapperComponent[] betAmountLabels;
		[Tooltip("Controls if the bet amounts are shown as full numbers, or abbreviated to keep them short.")]
		[SerializeField] private bool isAbbreviatingBetAmounts = true;
		[Tooltip("Button for this tier that when pressed will select it.")]
		[SerializeField] public UIButtonMessage button;
		[Tooltip("Animations played when the bet selector is shown and a specific tier is to be highlighted")]
		[SerializeField] public AnimationListController.AnimationInformationList tierHighlightedIntroAnimations;
		[Tooltip("Animations played when this tier button is pressed and selected")]
		[SerializeField] public AnimationListController.AnimationInformationList tierSelectedAnimations;
		[Tooltip("Animations played when the player pressed the bet up button and increases their bet amount to this tier")]
		[SerializeField] public AnimationListController.AnimationInformationList betIncreasedToTierAnimations;
		[Tooltip("Animations played when the player pressed the bet down button and decreases their bet amount to this tier")]
		[SerializeField] public AnimationListController.AnimationInformationList betDecreasedToTierAnimations;
		[Tooltip("Animations played when this tier needs to be disabled because the player can't bet at any amount for it.")]
		[SerializeField] public AnimationListController.AnimationInformationList disableTierAnimations;
		[Tooltip("Animations played when this tier needs to be enabled because a player can makea a valid bet amount for it.")]
		[SerializeField] public AnimationListController.AnimationInformationList enableTierAnimations;
		[Tooltip("Animations to correctly reset things like the text display at the top after the base game is turned on again after being disabled")]
		[SerializeField] public AnimationListController.AnimationInformationList onBaseGameReenabledAnimations;

		private ProgressiveJackpot progressiveJackpot = null;
		[System.NonSerialized] public long minQualifyingAmount; // This will comes down in the started info 
		private long maxQualifyingAmount; // This comes down in the started info

		[System.NonSerialized] public long betAmountOverride = -1; // used if isCopyingCurrentWagerToDisplayForTier is enabled to track an override value to be used instead of the standard betAmountShown
		private BetDisplayType betDisplayType;
		private long _betAmountShown = 0;
		public long betAmountShown
		{
			get
			{
				if (betAmountOverride != -1)
				{
					return betAmountOverride;
				}
				else
				{
					return _betAmountShown;
				}
			}
			private set
			{
				_betAmountShown = value;
			}
		} // Amount shown in the label

		private string progressiveKeyName;
		public string getProgressiveKeyName()
		{
			if (string.IsNullOrEmpty(progressiveKeyName))
			{
				// build the name using the progressiveTierNumber and the game key
				progressiveKeyName = "hir_" + GameState.game.keyName + "_blackout_tier" + progressiveTierNumber;
			}

			return progressiveKeyName;
		}

		public ProgressiveJackpot getProgressiveJackpot()
		{
			return progressiveJackpot;
		}

		public void init(BetDisplayType betDisplayType, long minQualifyingAmount, long maxQualifyingAmount)
		{
			this.betDisplayType = betDisplayType;

			string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);
			long absMinQualifyingAmount = SlotsWagerSets.getAbsMinBuiltInProgressiveWager(wagerSet, minQualifyingAmount, true);
			this.minQualifyingAmount = minQualifyingAmount > absMinQualifyingAmount ? minQualifyingAmount : absMinQualifyingAmount;
			this.maxQualifyingAmount = maxQualifyingAmount;

			// make sure we've created the progressiveKeyName by this point
			if (string.IsNullOrEmpty(progressiveKeyName))
			{
				progressiveKeyName = getProgressiveKeyName();
			}

			progressiveJackpot = ProgressiveJackpot.find(progressiveKeyName);

			if (progressiveJackpot == null)
			{
				Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.BuiltInProgressiveJackpotTierData.init() - Couldn't find progressiveKeyName = " + progressiveKeyName);
				return;
			}

			registerProgressiveJackpotLabels();

			betAmountShown = getBetAmountToShowForTier(betDisplayType);

			/*Debug.Log("BuiltInProgressiveJackpotTierData.init() - minQualifyingAmount = " + minQualifyingAmount 
				+ "; maxQualifyingAmount = " + maxQualifyingAmount
				+ "; defaultBetAmount = " + GameState.game.getDefaultBetValue()
				+ "; betAmountShown = " + betAmountShown
				+ "; betDisplayType = " + betDisplayType);*/

			if (!canPlayerBetForThisTier())
			{
				button.enabled = false;
				// set betAmountShown to be the min value for that range
				betAmountShown = getBetAmountToShowForTier(BetDisplayType.Lowest);
				RoutineRunner.instance.StartCoroutine(playDisableTierAnimations());
			}

			updateBetAmountLabels();
		}

		// Register the labels so they auto update with the progressive value.
		public void registerProgressiveJackpotLabels()
		{
			if (progressiveJackpot != null)
			{
				for (int i = 0; i < valueLabels.Length; i++)
				{
					progressiveJackpot.registerLabel(valueLabels[i]);
				}
			}
		}

		// Unregister the labels so they don't auto update with the progressive value
		private void unregisterValueLabelsFromProgressiveJackpot()
		{
			if (progressiveJackpot != null)
			{
				for (int i = 0; i < valueLabels.Length; i++)
				{
					progressiveJackpot.unregisterLabel(valueLabels[i]);
				}
			}
		}
		
		// Update all of the labels that are showing the jackpot amount to what the amount being won
		// is shown.  Should call registerProgressiveJackpotLabels() after the award is over so that the
		// labels turn back into tickers again.
		public void setProgressiveJackpotValueLabelsToJackpotWinAmount(long amount)
		{
			unregisterValueLabelsFromProgressiveJackpot();
		
			for (int i = 0; i < valueLabels.Length; i++)
			{
				valueLabels[i].text = CreditsEconomy.convertCredits(amount);
			}
		}

		public long getMaxQualifyingAmount()
		{
			return maxQualifyingAmount;
		}

		// Handles cases where the player has leveled up and has now unlocked a valid bet value for a previously
		// locked out tier
		public void checkIfTierShouldBeEnabled()
		{
			if (!button.enabled)
			{
				if (canPlayerBetForThisTier())
				{
					// the player can now bet at this tier so we need to enable it and setup the data for it
					betAmountShown = getBetAmountToShowForTier(betDisplayType);
					button.enabled = true;
					updateBetAmountLabels();
					RoutineRunner.instance.StartCoroutine(playEnableTierAnimations());
				}
				else
				{
					// ensure that the button stays in the disabled state, since it may
					// be knocked out of this state when returning from a bonus game
					RoutineRunner.instance.StartCoroutine(playDisableTierAnimations());
				}
			}
		}

		// Play the disable animations if this tier can't be bet at
		private IEnumerator playDisableTierAnimations()
		{
			if (disableTierAnimations.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(disableTierAnimations));
			}
		}

		// Play the enable animations if this tier can be bet at
		private IEnumerator playEnableTierAnimations()
		{
			if (enableTierAnimations.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(enableTierAnimations));
			}
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

		// Use an override bet amount for this tier and update the labels
		public void setBetAmountOverride(long betAmount)
		{
			betAmountOverride = betAmount;
			updateBetAmountLabels();
		}

		// Turn off an override bet amount for this tier and update the labels
		public void clearBetAmountOverride()
		{
			if (betAmountOverride != -1)
			{
				betAmountOverride = -1;
				updateBetAmountLabels();
			}
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

		// Cleanup function for when the parent is destroyed, mostly to unregister labels from Progressive jackpot stuff
		public void cleanup()
		{
			unregisterValueLabelsFromProgressiveJackpot();
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
					case BetDisplayType.Closest_To_Default:
						// find the bet amount nearest the default we would use on the SmartBetSelector
						return findWagerValueClosestToPassedValue(GameState.game.getDefaultBetValue(), allBetAmounts);
					default:
						Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.getBetAmountToShowForTier() - Unknown betDisplayType = " + betDisplayType);
						return 0;
				}
			}
			else
			{
				return 0;
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
	}

	[Tooltip("List of data for animations and data associated with each one of the progressive jackpots that this game uses.")]
	[SerializeField] private BuiltInProgressiveJackpotTierData[] progressiveList;
	[Tooltip("Controls how the bet level for each tier is determined, default is to just find the valid bet in the tier which is closest to the default bet amount we'd suggest the player to bet.")]
	[SerializeField] private BetDisplayType betDisplayType = BetDisplayType.Closest_To_Default;
	[Tooltip("Controls if the current wager value when reopening the bet selector is used for the tier the player was playing at.  See betAmountOverride in BuiltInProgressiveJackpotTierData.")]
	[SerializeField] private bool isCopyingCurrentWagerToDisplayForTier = false;
	[Tooltip("Used to extract the jackpot info from the started event data, final key used will be <keyName>_jackpot_fs.")]
	[SerializeField] private string slotStartedJackpotDataJsonKeyExtension = "_jackpots_fs";
	[Tooltip("Sounds played when the info button is pressed and before the intro anims for the bet selector play")]
	[SerializeField] public AudioListController.AudioInformationList jackpotInfoButtonSounds;
	[Tooltip("Hide top nav and spin panel with slide when bet selector is enabled")]
	[SerializeField] private bool isFadingOverlayPanels;
	[Tooltip("Time to fade spin and overlay panels")]
	[SerializeField] private float fadeAnimationTime;

	// Events
	[Tooltip("Event that triggers when the bet selector is shown")]
	public UnityEvent betSelectorShowEvent;
	[Tooltip("Event that triggers when the bet selector is hidden when a tier is selected")]
	public UnityEvent betSelectorHideEvent;

	private SlotBaseGame baseGame = null;
	private BuiltInProgressiveJackpotTierData currentTierData = null;
	private TICoroutine wagerChangeCoroutine = null; // Used to cancel a coroutine that is already happening to start a new one, since the player could change wagers quickly
	private TICoroutine baseGameEnabledAnimationCoroutine = null; // Used to cancel coroutine that ensures game is in correct animation state when it is enabled again on the off chance the game is quickly enabled multiple times
	private bool isInputEnabledForInfoButton = false;
	private bool isInputEnabledForBetButtons = true;
	private bool areOverlayPanelsFadedOut = false;

	public static BuiltInProgressiveJackpotTierData getCurrentTierData()
	{
		foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules)
		{
			BuiltInProgressiveJackpotBaseGameModule bar = module as BuiltInProgressiveJackpotBaseGameModule;
			if (bar != null)
			{
				return bar.currentTierData;
			}
		}
		return null;
	}

	protected override void OnDestroy()
	{
		for (int i = 0; i < progressiveList.Length; i++)
		{
			progressiveList[i].cleanup();
		}

		base.OnDestroy();
	}

	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		if (isFadingOverlayPanels)
		{
			// Start with everything hidden, fading out here is too soon for some devices
			// because spin panel may still have alpha 0 at this stage while the game loads
			if (!SlotBaseGame.instance.isVipRevampGame)
			{
				SpinPanel.instance.hidePanels();
			}
			Overlay.instance.top.show(false);
		}

		baseGame = reelGame as SlotBaseGame;

		if (baseGame == null)
		{
			Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.executeOnSlotGameStarted() - reelGame was not a SlotBaseGame, "
				+ "this shouldn't happen, if you are looking for what to hookup for a freespins game with this feature "
				+ "you want BuiltInProgressiveJackpotFreespinsModule.  Destroying this module.");
			Destroy(this);
			yield break;
		}

		// Locate the jackpot data in the started info so we can fill in the min/max qualifying values
		JSON jackpotInfoJSON = getJackpotInfoJSON(reelSetDataJson);

		if (jackpotInfoJSON == null)
		{
			Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.executeOnSlotGameStarted() - Unable to find JSON entry in started info for: " + GameState.game.keyName + slotStartedJackpotDataJsonKeyExtension);
			Destroy(this);
			yield break;
		}

#if UNITY_EDITOR
		// Do a validation check to make sure that we have data setup for all of the jackpots this game uses
		List<string> allJackpotKeys = jackpotInfoJSON.getKeyList();
		for (int i = 0; i < allJackpotKeys.Count; i++)
		{
			string key = allJackpotKeys[i];
			// the keys will include a "type" field which we want to skip checking
			if (key == "type")
			{
				continue;
			}

			if (!doesProgressiveListContainKey(key))
			{
				Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.executeOnSlotGameStarted() - progressiveList is missing an entry for progressive key: " + allJackpotKeys[i]);
			}
		}
#endif

		for (int i = 0; i < progressiveList.Length; i++)
		{
			BuiltInProgressiveJackpotTierData progressive = progressiveList[i];
			JSON progressiveJsonData = jackpotInfoJSON.getJSON(progressive.getProgressiveKeyName());

			if (progressiveJsonData == null)
			{
				Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.executeOnSlotGameStarted() - Unable to find JSON entry for progressive: " + progressive.getProgressiveKeyName());
				Destroy(this);
				yield break;
			}

			long minQualifyingAmount = progressiveJsonData.getLong("min_qualifying_bet", 0);
			long maxQualifyingAmount = progressiveJsonData.getLong("max_qualifying_bet", 0);

			progressiveList[i].init(betDisplayType, minQualifyingAmount, maxQualifyingAmount);
		}

		SwipeableReel.canSwipeToSpin = false;

		// Determine the tier which is closest to the default bet amount we'd assign the player
		// (i.e. the one they'd be at when loading into the game if we weren't showing this bet selector)
		currentTierData = getTierNearestDefaultBet();

		if (currentTierData != null && currentTierData.tierHighlightedIntroAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.tierHighlightedIntroAnimations));
		}

		yield break;
	}

	// Check if the progressive list contains the passed in key
	private bool doesProgressiveListContainKey(string progressiveKey)
	{
		for (int i = 0; i < progressiveList.Length; i++)
		{
			string currentEntryKey = progressiveList[i].getProgressiveKeyName();
			if (progressiveKey == currentEntryKey)
			{
				return true;
			}
		}

		return false;
	}

	// Extract the jackpot info from the game started JSON
	private JSON getJackpotInfoJSON(JSON reelSetDataJson)
	{
		JSON[] modifiers = reelSetDataJson.getJsonArray("modifier_exports");

		JSON jackpotInfoJSON = null;
		for (int i = 0;i < modifiers.Length; i++)
		{
			jackpotInfoJSON = modifiers[i].getJSON(GameState.game.keyName + slotStartedJackpotDataJsonKeyExtension);
			if (jackpotInfoJSON != null)
			{
				return jackpotInfoJSON;
			}
		}

		return null;
	}

	// Callback for UIButtonMessage to call when a jackpot entry is pressed
	// NOTE: In order for this to work correctly the object with the button message
	// must be setup in the BuiltInProgressiveJackpotTierData
	private void jackpotEntryPressed(GameObject jackpotObject)
	{
		if (isInputEnabledForBetButtons)
		{
			betSelectorHideEvent.Invoke();
			isInputEnabledForBetButtons = false;
			StartCoroutine(jackpotEntryPressedCoroutine(jackpotObject));

			if (isFadingOverlayPanels)
			{
				StartCoroutine(fadeInOverlayPanels());
			}
		}
	}

	private IEnumerator fadeInOverlayPanels()
	{
		if (!areOverlayPanelsFadedOut)
		{
			// overlay panels were only hidden at this point. So show them and
			// fade them out first so the alpha maps are correct.
			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
			Overlay.instance.top.show(true);
			yield return StartCoroutine(fadeOutOverlayPanels(0f));
		}

		SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		Overlay.instance.top.show(true);
		StartCoroutine(Overlay.instance.fadeIn(fadeAnimationTime));
		StartCoroutine(SpinPanel.instance.fadeIn(fadeAnimationTime));
		areOverlayPanelsFadedOut = false;
	}

	// Coroutine to handle stuff when the Callback from UIButtonMessage is triggered
	// on a jackpot entry being pressed
	private IEnumerator jackpotEntryPressedCoroutine(GameObject jackpotObject)
	{
		currentTierData = getTierDataForPressedJackpotEntry(jackpotObject);

		baseGame.setInitialBetAmount(currentTierData.betAmountShown);

		if (currentTierData.tierSelectedAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.tierSelectedAnimations));
		}

		// Reset any overrides we setup when the bet selector was opened
		if (isCopyingCurrentWagerToDisplayForTier)
		{
			for (int i = 0; i < progressiveList.Length; i++)
			{
				progressiveList[i].clearBetAmountOverride();
			}
		}

		SwipeableReel.canSwipeToSpin = true;
		isInputEnabledForInfoButton = true;
	}
	
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	// Function intended to be used by automation systems in order to randomly select a bet tier and
	// get the game through the bet selector and into a state where it can be spun.
	public IEnumerator automateBetSelection()
	{
		if (isInputEnabledForBetButtons)
		{
			yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(currentTierData.button.gameObject));

			// After pressing a jackpot button, we want to wait until the animations are done and the game is playable
			while (!isInputEnabledForInfoButton)
			{
				yield return null;
			}
		}
	}
#endif

	// Callback for UIButtonMessage to call when the info button next to the jackpot
	// is pressed once the player is in the game proper.
	private void jackpotInfoButtonPressed(GameObject jackpotInfoBtnObject)
	{
		if (isInputEnabledForInfoButton && !baseGame.isGameBusy)
		{
			betSelectorShowEvent.Invoke();
			SwipeableReel.canSwipeToSpin = false;
			isInputEnabledForInfoButton = false;
			StartCoroutine(jackpotInfoButtonPressedCoroutine(jackpotInfoBtnObject));

			if (isFadingOverlayPanels)
			{
				StartCoroutine(fadeOutOverlayPanels(fadeAnimationTime));
			}
		}
	}

	private IEnumerator fadeOutOverlayPanels(float fadeTime)
	{
		areOverlayPanelsFadedOut = true;
		TICoroutine topOverlayCoroutine = RoutineRunner.instance.StartCoroutine(Overlay.instance.fadeOut(fadeTime));
		TICoroutine spinPanelFadeCoroutine = RoutineRunner.instance.StartCoroutine(SpinPanel.instance.fadeOut(fadeTime));

		while (!topOverlayCoroutine.finished && !spinPanelFadeCoroutine.finished)
		{
			yield return null;
		}

		SpinPanel.instance.hidePanels();
		Overlay.instance.top.show(false);
	}

	// Coroutine version of jackpotInfoButtonPressed used to handle animations
	// which trigger when the info button is pressed
	private IEnumerator jackpotInfoButtonPressedCoroutine(GameObject jackpotInfoBtnObject)
	{
		if (isCopyingCurrentWagerToDisplayForTier && currentTierData != null)
		{
			currentTierData.setBetAmountOverride(SlotBaseGame.instance.betAmount);
		}

		// Make sure we enable any tiers that have been unlocked by leveling up
		for (int i = 0; i < progressiveList.Length; i++)
		{
			progressiveList[i].checkIfTierShouldBeEnabled();
		}
		
		// Play any extra anims/sounds the info button wants to do before the intro anims trigger
		// some games might need this to play an open sound
		if (jackpotInfoButtonSounds.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(jackpotInfoButtonSounds));
		}

		if (currentTierData != null && currentTierData.tierHighlightedIntroAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.tierHighlightedIntroAnimations));
		}

		isInputEnabledForBetButtons = true;
	}

	// Get the corresponding BuiltInProgressiveJackpotTierData for a jackpot entry that was pressed
	private BuiltInProgressiveJackpotTierData getTierDataForPressedJackpotEntry(GameObject jackpotObject)
	{
		for (int i = 0; i < progressiveList.Length; i++)
		{
			if (progressiveList[i].button.gameObject == jackpotObject)
			{
				return progressiveList[i];
			}
		}

		return null;
	}

	// Get the tier that is closest to the default bet amount
	private BuiltInProgressiveJackpotTierData getTierNearestDefaultBet()
	{
		BuiltInProgressiveJackpotTierData tier = null;
		long defaultWager = GameState.game.getDefaultBetValue();
		long bestDifference = long.MaxValue;
		string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);

		for (int i = 0; i < progressiveList.Length; i++)
		{
			long wagerAmountForTier = progressiveList[i].betAmountShown;

			// check if we can wager at the amount for this tier
			if (SlotsWagerSets.isAbleToWager(wagerSet, wagerAmountForTier))
			{
				long currentDifference = CommonMath.abs(defaultWager - wagerAmountForTier);
				if (currentDifference < bestDifference)
				{
					bestDifference = currentDifference;
					tier = progressiveList[i];
				}
			}
		}

		return tier;
	}

	// Get the tier for the passed wager value, used to get the new tier when the player
	// changes the bet amount using the bet selection buttons on the SpinPanel
	private BuiltInProgressiveJackpotTierData getTierForWagerValue(long wagerValue)
	{
		long maxQualifyingAmount = 0;
		int maxQualifyingAmountIndex = 0;

		for (int i = 0; i < progressiveList.Length; i++)
		{
			if (progressiveList[i].isWagerValueInTier(wagerValue))
			{
				return progressiveList[i];
			}

			// Store which progress tier has the highest amount. We do it like this to guarantee
			// we get the highest value in case the list is unordered.
			if (progressiveList[i].getMaxQualifyingAmount() > maxQualifyingAmount)
			{
				maxQualifyingAmount = progressiveList[i].getMaxQualifyingAmount();
				maxQualifyingAmountIndex = i;
			}
		}

		// If the players bet is so high that it exceeds that maximum returned by the server,
		// send back the high tier from the progressive list.
		if (wagerValue >= maxQualifyingAmount)
		{
			return progressiveList[maxQualifyingAmountIndex];
		}

		return null;
	}

	//Update the active jackpots on wager change
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

	// Returns the progressive jackpot tier key which the player is currently playing for
	public string getCurrentJackpotTierKey()
	{
		if (currentTierData != null)
		{
            return currentTierData.getProgressiveKeyName();
		}
		else
		{
			Debug.LogError("BuiltInProgressiveJackpotBaseGameModule.getCurrentJackpotTierKey() - currentTierData was null so couldn't determine progressiveKeyName");
			return "";
		}
	}

	// Handle running animations that need to happen when the wager changes in order to show the correct
	// jackpot on top of the game
	private IEnumerator handleExecuteOnWagerChangeCoroutine(long currentWager)
	{
		BuiltInProgressiveJackpotTierData tierData = getTierForWagerValue(currentWager);

		if (tierData == null)
		{
			Debug.LogWarning("could not find tier data for current wager " + currentWager);
			yield break;
		}

		if (tierData != currentTierData)
		{
			BuiltInProgressiveJackpotTierData prevTierData = currentTierData;
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

	// executeOnBonusGameEnded() section
	// functions here are called by the SlotBaseGame onBonusGameEnded() function
	// usually used for reseting transition stuff
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return (currentTierData != null && currentTierData.onBaseGameReenabledAnimations.Count > 0);
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentTierData.onBaseGameReenabledAnimations));
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
	
	// Allow an outside module that is interacting with the progressive data to reregister the labels after changing them before an award.
	// Typical usage should be to set the won value using setProgressiveJackpotValueLabelsToJackpotWinAmount() and then call this function
	// after the award is over to turn the labels back into tickers.
	public void registerProgressiveJackpotLabels()
	{
		if (currentTierData != null)
		{
			currentTierData.registerProgressiveJackpotLabels();
		}
	}

	// Update all of the labels that are showing the jackpot amount to what the amount being won
	// is shown.  Should call registerProgressiveJackpotLabels() after the award is over so that the
	// labels turn back into tickers again.
	public void setProgressiveJackpotValueLabelsToJackpotWinAmount(long amount)
	{
		if (currentTierData != null)
		{
			currentTierData.setProgressiveJackpotValueLabelsToJackpotWinAmount(amount);
		}
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
		return !isInputEnabledForInfoButton;
	}
	
// isModulePreventingBaseGameAudioFade() section
// Used to prevent audio from fading out after a set amount of time
// can be used for stuff like elvis03 bet selector where sound should
// not fade until the player has picked a bet and is actually into the 
// base game
	public override bool isModulePreventingBaseGameAudioFade()
	{
		return !isInputEnabledForInfoButton;
	}
}
