using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/**
 * Class to handle the tpir01 Cliffhanger bonus.  This bonus has a picking game where revealed
 * options either advance a character along a prize ladder or multiply the ladder values and provide
 * an additional pick.  Any time the character moves the player is provided an offer that they can collect
 * or decline (in which case the game will continue).  The game can end if the character falls off the end of
 * the prize ladder (in which case the player wins the lowest value).
 *
 * Creation Date: 1/21/2021
 * Original Author: Scott Lepthien
 */
public class ModularCliffhangerGameVariant : ModularPickingWithOffersGameVariant
{
	[SerializeField] private CliffhangerCharacterObject character;
	[SerializeField] private CliffHangerSectionData[] sectionsData;
	[Tooltip("Use this if you want the multipliers to upgrade staggered from each other, if set to zero then all multipliers will upgrade at the same time")]
	[SerializeField] private float multiplierUpgradeStaggerTime = 0.0f;
	[Tooltip("Animations played to celebrate the value that the player is being awarded when this game is finished.  Purely animation driven, so you can put a looped animation start in here and then have a delayed idle state to cancel the loop after whatever duration we want to play.")]
	[SerializeField] private AnimationListController.AnimationInformationList celebrateCreditAwardAnims;
	[Tooltip("An array of labels that will be updated any time the multiplier being awarded to the player is updated. (In tpir01 this was used for the offer signs which explain what multiplier you would win if you accept the offer).")]
	[SerializeField] private LabelWrapperComponent[] currentMultiplierLabels;
	[Tooltip("An array of labels that will show what the possible range of meter values that can be revealed in the game are.")]
	[SerializeField] private LabelWrapperComponent[] meterRevealValueRangeLabels;

	private int currentMeterValue = 0; // Tracks where on the meter the player is
	private long baseWinValue; // The base value which is multiplied by the multipliers in the section data to determine what each section is currently worth
	private long gameOverMultiplier; // Multiplier used if the player gets a gameover by going past all cliffhanger sections
	private CliffHangerSectionData currentSection; // Just store the current section for easy access (that way we don't have to search for it each time we need to look at data from it)
	private int cachedTotalMeterLength = 0;
	private bool _isCliffhangerEndedInGameover = false;
	private int minPotentialMeterRevealValue = 0; // Used to display to the player what the possible range of meter value reveals are
	private int maxPotentialMeterRevealValue = 0; // Used to display to the player what the possible range of meter value reveals are

	public bool isCliffhangerEndedInGameover
	{
		get { return _isCliffhangerEndedInGameover; }
	}
	
	// Custom init for picking game specifics
	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame parentGame)
	{
		base.init(outcome, roundIndex, parentGame);

		// @todo : Determine if this is fine or if we want this data to actually be included in the picking game portion of the outcome
		baseWinValue = BonusGameManager.currentBonusGameOutcome.getWager();

		// Get the paytable for this game and extract out the info about the segments so we can use
		// it to initialize the visible sections.
		JSON cliffhangerPaytableJson = BonusGamePaytable.findPaytable(outcome.getPayTableName());
		List<CliffhangerSegmentPaytableData> cliffhangerPaytableSegmentData = extractCliffhangerSegmentDataFromPaytable(cliffhangerPaytableJson.getJsonArray("ladders")[0]);
		
		// Handle extracting the meter value range and then updating the labels with it
		extractMinAndMaxMeterPoolValuesFromPaytable(cliffhangerPaytableJson.getJsonArray("pools"));
		updateMeterRevealValueRangeLabels();
		
		// Double check that the number of visible segments setup matches the number of segments from the data
		if (sectionsData.Length != cliffhangerPaytableSegmentData.Count)
		{
			Debug.LogError($"ModularCliffhangerGameVariant.init() - Visible segment sectionsData.Length: {sectionsData.Length} does not match the server paytable cliffhangerPaytableSegmentData.Count = {cliffhangerPaytableSegmentData.Count}");
		}
		else
		{
			for (int i = 0; i < sectionsData.Length; i++)
			{
				sectionsData[i].init(cliffhangerPaytableSegmentData[i], baseWinValue);
			}

			// Set the currentSection now that the section data is loaded in
			currentSection = getSectionDataForMeterValue(currentMeterValue);
			
			// Update this label right away so that it is set during some of the intro anims
			updateCliffhangerWinAmount();
			
			// Update these labels so they are set for the first time the signs are displayed (since
			// it only gets updated when moving to a new section, but you can get the signs in the first section
			// if you don't move very far).
			updateCurrentMultiplierLabels();
		}
	}

	// Function to extract the min and max values that can be won inside of the pools that award the meter values.
	// These can be used to display the range of possible values to the player.
	private void extractMinAndMaxMeterPoolValuesFromPaytable(JSON[] poolsJsonArray)
	{
		minPotentialMeterRevealValue = Int32.MaxValue;
		maxPotentialMeterRevealValue = Int32.MinValue;
	
		foreach (JSON poolJson in poolsJsonArray)
		{
			JSON[] itemsJson = poolJson.getJsonArray("items");
			foreach (JSON itemJson in itemsJson)
			{
				int meterValue = itemJson.getInt("meter_value", 0);
				// Just double check for missing data, since a meter value of zero doesn't make sense
				if (meterValue != 0)
				{
					if (meterValue < minPotentialMeterRevealValue)
					{
						minPotentialMeterRevealValue = meterValue;
					}

					if (meterValue > maxPotentialMeterRevealValue)
					{
						maxPotentialMeterRevealValue = meterValue;
					}
				}
			}
		}
	}

	// Updates any labels that are supposed to be displaying what the potential range is for meter values that can be revealed from picks are
	// NOTE: Should be called after extractMinAndMaxMeterPoolValuesFromPaytable() so the range values are stored
	private void updateMeterRevealValueRangeLabels()
	{
		string rangeString = string.Format("{0}-{1}", CommonText.formatNumber(minPotentialMeterRevealValue), CommonText.formatNumber(maxPotentialMeterRevealValue));
		foreach (LabelWrapperComponent label in meterRevealValueRangeLabels)
		{
			label.text = rangeString;
		}
	}

	// Function to extract the segment data from the paytable so we can put it into the data structure used
	// by this bonus game on the client
	private List<CliffhangerSegmentPaytableData> extractCliffhangerSegmentDataFromPaytable(JSON cliffhangerLadderJson)
	{
		int totalLength = cliffhangerLadderJson.getInt("total_length", 0);
		gameOverMultiplier = cliffhangerLadderJson.getInt("default_multiplier", 0);
		
		// Due to how the data is structured we need to match up all of the upgrades for a given
		// segment section first so that we can ultimately build the set of data we want.
		Dictionary<int, List<JSON>> segmentUpgrades = new Dictionary<int, List<JSON>>();
		
		// Go through all the segments and sort them into our segmentUpgrades structure
		JSON[] segmentsArrayJson = cliffhangerLadderJson.getJsonArray("segments");
		foreach (JSON segmentJson in segmentsArrayJson)
		{
			int startPosition = segmentJson.getInt("start_position", 0);
			
			// If the start position is 1 we want to actually extend it to 0.
			// The server doesn't count being on the meter until you are at 1
			// but on the client we still want to count and display the value for
			// the first section when the player has not moved yet.
			if (startPosition == 1)
			{
				startPosition = 0;
			}
			
			if (!segmentUpgrades.ContainsKey(startPosition))
			{
				segmentUpgrades[startPosition] = new List<JSON>();
			}
			
			segmentUpgrades[startPosition].Add(segmentJson);
		}
		
		List<CliffhangerSegmentPaytableData> outputDataList = new List<CliffhangerSegmentPaytableData>();
		
		// Now all the upgrades should be grouped together and we can start making our final data structure
		foreach (KeyValuePair<int, List<JSON>> kvp in segmentUpgrades)
		{
			CliffhangerSegmentPaytableData segmentData = new CliffhangerSegmentPaytableData();
			segmentData.minSectionValue = kvp.Key;
			segmentData.multiplierUpgrades = new long[kvp.Value.Count];

			foreach (JSON segmentJson in kvp.Value)
			{
				int upgradeLevel = segmentJson.getInt("upgrade_level", 0);
				long multiplier = segmentJson.getLong("multiplier", 0);
				segmentData.multiplierUpgrades[upgradeLevel] = multiplier;
			}
			
			outputDataList.Add(segmentData);
		}
		
		// Now we need to sort the segmentData, that way we can set the upper range of each segment
		// which isn't included directly in the data, but can easily be inferred once the data is sorted
		outputDataList.Sort();
		
		// Now initialize the max value for each segment based on the segment that comes after.
		// With the max for the last segment being totalLength extracted above.
		for (int i = 0; i < outputDataList.Count; i++)
		{
			int nextIndex = i + 1;
			if (nextIndex < outputDataList.Count)
			{
				outputDataList[i].maxSectionValue = outputDataList[nextIndex].minSectionValue - 1;
			}
			else
			{
				// This is the final segment, so set it to the totalLength - 1
				outputDataList[i].maxSectionValue = totalLength - 1;
			}
		}

		return outputDataList;
	}

	public IEnumerator addToMeterValue(int valueToAddToMeter)
	{
		currentMeterValue += valueToAddToMeter;

		if (isMeterValueHigherThanAllSections(currentMeterValue))
		{
			// Need to trigger a gameover anime sequence and award the player the gameover value
			yield return StartCoroutine(triggerGameOver());
		}
		else
		{
			CliffHangerSectionData prevSection = currentSection;
			currentSection = getSectionDataForMeterValue(currentMeterValue);
			
			// Check for a section change and update the win amount if we've moved into a new section
			if (prevSection != currentSection)
			{
				updateCliffhangerWinAmount();
				
				// Need to update the offer sign multiplier text here
				updateCurrentMultiplierLabels();
			}
			
			// Move the character along the path
			// @todo : May need to think about if there should be a way to speed this up (and how that will work with code,
			// like I assume if we do it will trigger off tap to skip, but we'll have to determine how to move the character
			// faster, maybe just do all the coroutines but speed them up).
			yield return StartCoroutine(animateAndTweenCharacterAlongMeter(currentMeterValue));
		}
	}
	
	// Function to call in order to move the character along the meter
	protected IEnumerator animateAndTweenCharacterAlongMeter(int newSegment)
	{
		// @todo : If anim wants the segments to light up while the character is on one, we'll have to add some addition stuff to handle disabling the currently lit one
		// and lighting the new one when the character is done moving
	
		float movementTime = character.movementTime;
		yield return StartCoroutine(AnimationListController.playRandomizedAnimationInformationList(character.randomizedCharacterMovingAnimList));

		// now we tween the object to the new location
		Vector3 pointDifference = character.endPoint - character.startPoint;
		float newSegmentPercent = newSegment / (float)(getTotalMeterLength() - 1);

		Vector3 newSegmentPosition = character.startPoint + (pointDifference * newSegmentPercent);

		if (movementTime != -1.0f)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(character.movingObject, iTween.Hash("position", newSegmentPosition, "time", movementTime, "islocal", true, "easetype", iTween.EaseType.linear)));
		}
		else
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(character.movingObject, iTween.Hash("position", newSegmentPosition, "speed", character.movementSpeed, "islocal", true, "easetype", iTween.EaseType.linear)));
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(character.endMovingAnimList));
	}

	private IEnumerator triggerGameOver()
	{
		_isCliffhangerEndedInGameover = true;
		
		// Need to zero out the pick meter here (since no more picks can be made)
		picksRemainingLabel.text = CommonText.formatNumber(0);
		
		long gameOverCredits = baseWinValue * gameOverMultiplier;
		winLabel.text = CreditsEconomy.convertCredits(gameOverCredits);
		
		// Send a server message saying that the "completed" the game on the final entry (which should indicate to the server that they got the gameover)
		// Note we send index + 1 here because the pickIndex hasn't been incremented yet and is 1-based, so to send the current index for the gameover reveal
		// we need it to be incremented by 1
		sendOfferCompleteActionToServer(pickIndex + 1);
	
		// First play the animation of the character falling off the end
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(character.characterFallsOffEndAnimList));
		
		// Award the gameover multiplier to the player
		yield return StartCoroutine(awardAndCelebrateCreditValue(gameOverCredits));
		
		// Need to transfer any remaining picks to leftovers (since we want to make sure
		// that we show the player everything they could have gotten)
		transferPicksToLeftovers(true);

		// Force terminate the game here (since the server may have still sent additional reveals
		// which would make the game think that it could still be played).
		yield return StartCoroutine(roundEnd());
	}

	// Get the section for a passed meter value.
	// NOTE: Should check isMeterValueHigherThanAllSections() before calling this to make sure there will be a valid section
	private CliffHangerSectionData getSectionDataForMeterValue(int passedMeterValue)
	{
		foreach (CliffHangerSectionData section in sectionsData)
		{
			if (section.isMeterValueInSection(passedMeterValue))
			{
				return section;
			}
		}

		Debug.LogError("ModularCliffhangerGameVariant.getSectionDataForMeterValue() - Unable to find section for passedMeterValue = " + passedMeterValue);
		return null;
	}

	// Add the length of each section together in order to determine the total length of all sections
	private int getTotalMeterLength()
	{
		if (cachedTotalMeterLength == 0)
		{
			cachedTotalMeterLength = 0;
			foreach (CliffHangerSectionData section in sectionsData)
			{
				cachedTotalMeterLength += section.getSectionLength();
			}
		}

		return cachedTotalMeterLength;
	}

	// Tells if a meter value is so high that it is beyond all sections (in which case this will lead to a gameover)
	private bool isMeterValueHigherThanAllSections(int passedMeterValue)
	{
		foreach (CliffHangerSectionData section in sectionsData)
		{
			if (section.isMeterValueInSection(passedMeterValue))
			{
				return false;
			}
		}

		return true;
	}
	
	// A function for upgrading all the section that can be called when the reveal module for the additional pick and upgrade occurs
	public IEnumerator upgradeSectionMultipliers(int upgradeIndexIncrease)
	{
		List<TICoroutine> upgradeCoroutines = new List<TICoroutine>();
		foreach (CliffHangerSectionData section in sectionsData)
		{
			// Track the upgrades in a list so we can make sure that all upgrades complete before proceeding
			if (section == currentSection)
			{
				// Handle the special case where we will upgrade the section and update the win amount as soon as the update is complete
				upgradeCoroutines.Add(StartCoroutine(upgradeSectionAndWinAmount(section, upgradeIndexIncrease)));
			}
			else
			{
				upgradeCoroutines.Add(StartCoroutine(section.upgradeMultiplier(upgradeIndexIncrease, baseWinValue)));
			}
			
			// Check if we want to stagger the upgrades
			if (multiplierUpgradeStaggerTime > 0.0f)
			{
				yield return new TIWaitForSeconds(multiplierUpgradeStaggerTime);
			}
		}
		
		// Make sure all the upgrades are complete
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(upgradeCoroutines));
	}

	// Special function used when the current section is being upgraded so that
	// the win amount label is upgraded as soon as the multiplier upgrade animation
	// is finished.
	private IEnumerator upgradeSectionAndWinAmount(CliffHangerSectionData section, int upgradeIndexIncrease)
	{
		yield return StartCoroutine(section.upgradeMultiplier(upgradeIndexIncrease, baseWinValue));
		
		// Need to update the offer sign multiplier text here
		updateCurrentMultiplierLabels();
		
		// Update the win amount to reflect the updated multipliers
		// Do this when we upgrade the section the player is currently in
		updateCliffhangerWinAmount();
	}

	// Update any labels that are hooked up that show the player what the current multiplier they would win if they stopped is
	private void updateCurrentMultiplierLabels()
	{
		if (currentSection != null)
		{
			foreach (LabelWrapperComponent label in currentMultiplierLabels)
			{
				label.text = CommonText.formatNumber(currentSection.getCurrentMultiplier());
			}
		}
		else
		{
			Debug.LogError("ModularCliffhangerGameVariant.updateCurrentMultiplierLabels() - currentSection was not set when this function was called, this shouldn't happen.");
		}
	}

	// Function that hooks into after the pick index has been increased
	// When this happens we need to determine what to do next based on if the player has picks remaining or not.
	public IEnumerator handlePlayerChoiceAfterReveal()
	{
		// Check if the player is out of picks (in which case we need to auto accept the current offer)
		if (isRoundOver())
		{
			yield return StartCoroutine(collectOfferCoroutine());
		}
		else
		{
			// Need to show the player the offer UI so they can make a selection
			yield return StartCoroutine(showOfferButtons());
		}
	}
	
	// Plays a set of animations that celebrate the value being awarded over a set duration (since we don't actually rollup in this game)
	// And then put the value in the location where it will be awarded to the player
	private IEnumerator awardAndCelebrateCreditValue(long winAmount)
	{
		// NOTE: It is assumed that labels with the win amount have already been set before this is called
		
		// Go ahead and award the value into the win amount for this bonus
		BonusGamePresenter.instance.currentPayout = winAmount;
		
		// Then play the win celebration animations (these are in place of a rollup, since the win amount is already fully shown)
		if (celebrateCreditAwardAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(celebrateCreditAwardAnims));
		}
	}
	
	// Derived class should implement this (since the actual workings of the specific game implementing this class
	// and what animations it wants to play, will be dictated by how that game works).
	protected override IEnumerator awardOfferToPlayer()
	{
		yield return StartCoroutine(awardAndCelebrateCreditValue(currentSection.getSectionCreditWinAmount()));
	}
	
	// Update the labels to initial states when the round starts
	protected override void updateLabelsOnRoundStart()
	{
		// Overriding this so that we can change what the initial win label gets set to, since it should start
		// at the value the player could win for the first segment.
		updateCliffhangerWinAmount();
		
		if (multiplierLabel != null)
		{
			refreshMultiplierLabel();
		}
		
		if (jackpotLabel != null)
		{
			refreshJackpotLabel();
		}
	}

	// Update the win amount displayed for the player for where they are currently located on the cliffhanger path
	private void updateCliffhangerWinAmount()
	{
		if (winLabel != null)
		{
			winLabel.text = CreditsEconomy.convertCredits(currentSection.getSectionCreditWinAmount());
		}
	}

	// Tells if the character has moved along the meter at all yet.  Currently used to determine if the player
	// should be presented a choice after an upgrade.  Basically the player will get to make a choice after an
	// upgrade reveal, but only if the character has moved at least once.
	public bool hasCharacterEverMoved()
	{
		return currentMeterValue > 0;
	}

	// Represents the paytable data and minipulates it into a format that can be used for
	// CliffHangerSectionData
	private class CliffhangerSegmentPaytableData : IComparable<CliffhangerSegmentPaytableData>
	{
		public int minSectionValue; // The starting value for this section of the meter
		public int maxSectionValue; // The final value for this section of the meter
		public long[] multiplierUpgrades; // Stores the multiplier progression which increases when an additional pick reveal is found during the pick game

		public int CompareTo(CliffhangerSegmentPaytableData other)
		{
			if (other == null)
			{
				return 1;
			}

			return this.minSectionValue.CompareTo(other.minSectionValue);
		}
	}

	// This is the class used to visually display the data we get for the segments from the server
	[System.Serializable]
	private class CliffHangerSectionData
	{
		[Tooltip("Holds the label for this section, which gets updated if the section is upgraded")]
		[SerializeField] private LabelWrapperComponent multiplierLabel;
		[Tooltip("List of animations played when the multiplier is upgraded")]
		[SerializeField] private AnimationListController.AnimationInformationList multiplierUpgradeAnimations;
		private CliffhangerSegmentPaytableData paytableData; // Stores the data extracted from the paytable about this section, like minSectionValue, maxSectionValue, and multiplierUpgrades
		private int currentUpgradeIndex = 0; // Tracks what the upgrade value of this section is
		private long sectionCreditWinAmount;
		
		public void init(CliffhangerSegmentPaytableData paytableData, long baseWinValue)
		{
			this.paytableData = paytableData;

			sectionCreditWinAmount = baseWinValue * getCurrentMultiplier();

			updateMultiplierLabel();
		}

		public bool isMeterValueInSection(int passedMeterValue)
		{
			return passedMeterValue >= paytableData.minSectionValue && passedMeterValue <= paytableData.maxSectionValue;
		}

		public IEnumerator upgradeMultiplier(int upgradeIndexIncrease, long baseWinValue)
		{
			// Check for out of bounds before doing the upgrade
			if (currentUpgradeIndex + upgradeIndexIncrease >= paytableData.multiplierUpgrades.Length)
			{
				Debug.LogWarning("CliffHangerSectionData.upgradeMultiplier() - Upgraded index of " + (currentUpgradeIndex + upgradeIndexIncrease) + "; will exceed multiplierUpgardes.Length = " + paytableData.multiplierUpgrades.Length + "; skipping upgrade!");
			}
			else
			{
				currentUpgradeIndex += upgradeIndexIncrease;
				sectionCreditWinAmount = baseWinValue * getCurrentMultiplier();

				// Play the list of upgrade animations, for now I think just
				// setting a shorter delay then the animation should work if 
				// animation wants the label to change in the middle of the animation
				// (since these animations don't need to block for their full duration).
				if (multiplierUpgradeAnimations.Count > 0)
				{
					yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierUpgradeAnimations));
				}

				updateMultiplierLabel();
			}
		}

		public long getCurrentMultiplier()
		{
			return paytableData.multiplierUpgrades[currentUpgradeIndex];
		}

		public long getSectionCreditWinAmount()
		{
			return sectionCreditWinAmount;
		}

		public int getSectionLength()
		{
			return (paytableData.maxSectionValue - paytableData.minSectionValue) + 1;
		}

		public void updateMultiplierLabel()
		{
			if (multiplierLabel != null)
			{
				multiplierLabel.text = CommonText.formatNumber(getCurrentMultiplier());
			}
		}
	}
	
	// Class that defines how the cliffhanger character object functions as it progresses along the meter
	[System.Serializable]
	public class CliffhangerCharacterObject
	{
		[Tooltip("Object that will me moved between startPoint and endPoint")]
		public GameObject movingObject;
		[Tooltip("Movement speed used by iTween.MoveTo instead of time so that the movement speed is the same regardless of distance")]
		public float movementSpeed = 1;
		[Tooltip("Use this if you want the movement to use a time duration instead of a set speed (might make sense if movements are always the same distance and need to sync with animations)")]
		public float movementTime = -1.0f;
		[Tooltip("The starting point for where the object will move between")]
		public Vector3 startPoint;
		[Tooltip("The ending point for where the object will move between")]
		public Vector3 endPoint;
		[Tooltip("Randomized Animation list of animations to be played when the movingObject is moved along the path")]
		public AnimationListController.RandomizedAnimationInformationLists randomizedCharacterMovingAnimList;
		[Tooltip("Animation list of animations to be played when the movingObject stops moving")]
		public AnimationListController.AnimationInformationList endMovingAnimList;
		[Tooltip("Animation list of animations to be played when the movingObject goes past the end of the meter")]
		public AnimationListController.AnimationInformationList characterFallsOffEndAnimList;
	}
}
