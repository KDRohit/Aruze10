using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Similar to CumulativeBonusModule but this handles games like wonka04 that have multiple bonuses it can trigger instead of a portal
the data is a bit different which is why we aren't inheriting from CumulativeBonusModule directly.  The major difference between the
two, is that in zynga04 which uses CumulativeBonusModule you collect 5 different symbols, in this one for wonka04 you need to collect
the same symbol (TR1, TR2, or TR3) a certain number of times and then you trigger the bonus which corresponds to that symbol.
NOTE: This class can only handle triggering one bonus game at a time, modification would be required to support the ability for multiple
bonus games to trigger one after another.

Original Author: Scott Lepthien
Creation Date: October 11, 2017
*/
public class MultiCumulativeBonusGameModule : SlotModule 
{
	// Class for handling the tracking and animated parts of each bonus that is being tracked
	// by the cumulative module
	[System.Serializable] private class MultiCumulativeBonusGameTracker
	{
		public string symbolName = "";
		[SerializeField] private int numSymbolsRequired;
		// These animation are intended to go to the unselected state for an animation, used when an icon isn't acquired yet
		[SerializeField] private List<AnimationListController.AnimationInformationList> symbolNotAcquiredAnimations = new List<AnimationListController.AnimationInformationList>();
		// These animations are intended to go to the loop/static acquired state directly rather than playing the full acquired animaiton sequence
		[SerializeField] private List<AnimationListController.AnimationInformationList> symbolAlreadyAcquiredAnimations = new List<AnimationListController.AnimationInformationList>();
		// The following animations triggers when you acquire a bonus symbol from the reels
		[SerializeField] private List<AnimationListController.AnimationInformationList> symbolAcquiredAnimations = new List<AnimationListController.AnimationInformationList>();
		private int numSymbolsAcquired = 0;

		// Returns the number of symbols required to trigger this tracker
		public int getNumSymbolsRequired()
		{
			return numSymbolsRequired;
		}

		// Returns the number of acquired symbols
		public int getNumSymbolsAcquired()
		{
			return numSymbolsAcquired;
		}

		// Increment the number of symbols acquired
		public void incrementNumSymbolsAcquired(int incrementValue)
		{
			if (numSymbolsAcquired < numSymbolsRequired && (numSymbolsAcquired + incrementValue) <= numSymbolsRequired)
			{
				numSymbolsAcquired += incrementValue;
			}
			else
			{
				Debug.LogError("MultiCumulativeBonusGameModule.MultiCumulativeBonusGameTracker.incrementNumSymbolsAcquired() - Trying to increment by incrementValue = " + incrementValue 
					+ "; but numSymbolsAcquired would exceed numSymbolsRequired!");
			}
		}

		// Set the value of the number of symbols acquired for this tracker
		// This should probably only be used when first entering the game to set the starting value
		public void setNumSymbolsAcquiredStartingValue(int startingNumAcquired)
		{
			if (startingNumAcquired < numSymbolsRequired)
			{
				numSymbolsAcquired = startingNumAcquired;
			}
			else
			{
				Debug.LogError("MultiCumulativeBonusGameModule.MultiCumulativeBonusGameTracker.setNumSymbolsAcquiredStartingValue() - startingNumAcquired = " + startingNumAcquired 
					+ "; startingNumAcquired would exceed numSymbolsRequired!  Reseting to 0!");
				resetNumSymbolsAcquired();
			}
		}

		// Reset the tracked number of symbols acquired
		public void resetNumSymbolsAcquired()
		{
			numSymbolsAcquired = 0;
		}

		// Plays already acquired animations for all of the symbols which have already been acquired
		// and the not acquired animations for any that aren't acquired.
		public IEnumerator updateAnimations()
		{
			for (int i = 0; i < numSymbolsRequired; i++)
			{
				if (numSymbolsAcquired > 0 && i <= numSymbolsAcquired - 1)
				{
					// symbol is already acquired
					if (i >= 0 && i < symbolAlreadyAcquiredAnimations.Count)
					{
						yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(symbolAlreadyAcquiredAnimations[i]));
					}
				}
				else
				{
					// this symbol is not acquired yet, make sure it is doing that animation
					if (i >= 0 && i < symbolNotAcquiredAnimations.Count)
					{
						yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(symbolNotAcquiredAnimations[i]));
					}
				}
			}
		}

		// Plays the acquired animations for a specific 0-based index
		public IEnumerator playAcquiredAnimationForIndex(int symbolAcquiredIndex)
		{
			if (symbolAcquiredIndex >= 0 && symbolAcquiredIndex < symbolAcquiredAnimations.Count)
			{
				yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(symbolAcquiredAnimations[symbolAcquiredIndex]));
			}
		}
	}

	[SerializeField] private List<MultiCumulativeBonusGameTracker> bonusGameTrackers = new List<MultiCumulativeBonusGameTracker>();

	private bool isCumulativeDataLoaded = false;		// Ensure we don't refresh the active bonus symbols before the data loads for it
	private Dictionary<int, string> awardedSymbols;		// Tracks the symbols awarded this spin, for now should only contain a single symbol, the int is the reel it landed on (parsed from anticipation data)
	private long betMultiplierOverride = -1;
	private bool isStartingFeatureAfterRollup = false; // This will be used for the base game respin feature, since we want to delay the accumulation until after the payout
	private bool isRespinFeatureActive = false; // Tells if a respin feature is active, since we will need to reset the accumulator when the respins end

	private const string BONUS_SYMBOLS_DATA_KEY = "cumulative_bonus_symbols";
	private const string BONUS_SYMBOL_ACCUMULATION_MULTI_TYPE_NAME = "bonus_symbol_accumulation_multi";

	// Get the bonus tracker info for a specific bonus symbol
	private MultiCumulativeBonusGameTracker getTrackerForBonusSymbol(string bonusSymbolName)
	{
		for (int i = 0; i < bonusGameTrackers.Count; i++)
		{
			if (bonusGameTrackers[i].symbolName == bonusSymbolName)
			{
				return bonusGameTrackers[i];
			}
		}

		Debug.LogError("MultiCumulativeBonusGameModule.getTrackerForBonusSymbol() - bonusSymbolName = " + bonusSymbolName + "; No tracker for this symbol, returning NULL!");
		return null;
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		if (isCumulativeDataLoaded)
		{
			updateAllTrackerAnimations();
		}
	}

	// Updates the animations on the trackers to correctly display the number of bonus symbols already acquired
	private void updateAllTrackerAnimations()
	{
		for (int i = 0; i < bonusGameTrackers.Count; i++)
		{
			StartCoroutine(bonusGameTrackers[i].updateAnimations());
		}
	}

// executeOnSlotGameStartedNoCoroutine() section
// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return reelSetDataJson != null;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		JSON bonusSymbolsJson = reelSetDataJson.getJSON(BONUS_SYMBOLS_DATA_KEY);

		if (bonusSymbolsJson != null)
		{
			List<string> symbolNameList = bonusSymbolsJson.getKeyList();

			for (int i = 0; i < symbolNameList.Count; i++)
			{
				string currentSymbolName = symbolNameList[i];
				int numSymbolsAcquired = bonusSymbolsJson.getInt(currentSymbolName, 0);
				MultiCumulativeBonusGameTracker tracker = getTrackerForBonusSymbol(currentSymbolName);
				if (tracker != null)
				{
					tracker.setNumSymbolsAcquiredStartingValue(numSymbolsAcquired);
				}
			}
		}
		else
		{
			// seems like if we don't get the data we have to assume that no progress is made on any of the bonuses
			// @TODO : See if server can fix this, going to put in error logging here again and then hopefully
			// we can update server to always send the values down and then the error log will go away.
			Debug.LogError("MultiCumulativeBonusGameModule.executeOnSlotGameStartedNoCoroutine() - Acquired symbol counts weren't sent down with the started event!");
			
			for (int i = 0; i < bonusGameTrackers.Count; i++)
			{
				bonusGameTrackers[i].resetNumSymbolsAcquired();
			}
		}

		updateAllTrackerAnimations();
		isCumulativeDataLoaded = true;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Search for a reevaluation of the type that would award bonus game progress
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();

		if (reevaluations != null && reevaluations.Length > 0)
		{
			for (int i = 0; i < reevaluations.Length; i++)
			{
				JSON reevaluation = reevaluations[i];
				string reevalType = reevaluation.getString(SlotOutcome.FIELD_TYPE, "");

				if (reevalType == BONUS_SYMBOL_ACCUMULATION_MULTI_TYPE_NAME)
				{
					// Get the symbol that was actually triggered and store it
					awardedSymbols = reelGame.outcome.getAnticipationSymbols();
					betMultiplierOverride = reevaluation.getLong(SlotOutcome.FIELD_BET_MULTIPLIER, -1);

					// check if we have respins and if so then we will delay until after the rollup is complete
					SlotOutcome currentReevalOutcome = new SlotOutcome(reevaluation);
					currentReevalOutcome.setParentOutcome(reelGame.outcome);
					List<SlotOutcome> reevaluationSpins = currentReevalOutcome.getReevaluationSpins();
					if (reevaluationSpins.Count > 0)
					{
						// a base game respin feature was awarded, need to plug this into the reelGame and trigger
						// the acquire animations after the base game rolls up
						isStartingFeatureAfterRollup = true;
						reelGame.reevaluationSpins = reevaluationSpins;
						reelGame.reevaluationSpinsRemaining = reevaluationSpins.Count;

						return false;
					}
					else
					{
						// this is a bonus or isn't the award of a base game respin feature
						return true;
					}
				}
			}
		}

		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		yield return StartCoroutine(playAcquiredAnimation());
	}

	// Check the number of acquired symbols against the number required for a tracker
	// to determine if a bonus/feature is triggering when it shouldn't be.
	// However making this only a warning because of the way the server works
	// with cheats to award the bonus right away the number will be off in those cases.
	private void checkForTrackerAwardingAtWrongTime(MultiCumulativeBonusGameTracker tracker)
	{
		int numSymbolsAcquired = tracker.getNumSymbolsAcquired();
		int numSymbolsRequired = tracker.getNumSymbolsRequired();
		if (numSymbolsAcquired != numSymbolsRequired)
		{
			Debug.LogWarning("MultiCumulativeBonusGameModule.playAcquiredAnimation() - numSymbolsAcquired = " + numSymbolsRequired + "; did not equal numSymbolsRequired = " + numSymbolsRequired
				+ "; but a feature/bonus was triggered, this might be due to using a cheat, but could mean something is not working right.");
		}
	}

	private IEnumerator playAcquiredAnimation()
	{
		foreach (KeyValuePair<int, string> kvp in awardedSymbols)
		{
			MultiCumulativeBonusGameTracker tracker = getTrackerForBonusSymbol(kvp.Value);
			if (tracker != null)
			{
				tracker.incrementNumSymbolsAcquired(1);
				yield return StartCoroutine(tracker.playAcquiredAnimationForIndex(tracker.getNumSymbolsAcquired() - 1));

				if (reelGame.outcome.isBonus)
				{
					checkForTrackerAwardingAtWrongTime(tracker);
					tracker.resetNumSymbolsAcquired();

					if (betMultiplierOverride != -1)
					{
						BonusGameManager.instance.betMultiplierOverride = betMultiplierOverride;
						betMultiplierOverride = -1;
					}
				}
				else if (isRespinFeatureActive)
				{
					checkForTrackerAwardingAtWrongTime(tracker);
					tracker.resetNumSymbolsAcquired();

					// Need to use an override for the multiplier for this base game feature
					if (betMultiplierOverride != -1)
					{
						reelGame.reevaluationSpinMultiplierOverride = betMultiplierOverride;
						betMultiplierOverride = -1;
					}
				}
			}
		}
	}

// needsToExecuteDuringContinueWhenReady() section
// called from continueWhenReady() after all wins are paid out but before the game is unlocked
	public override bool needsToExecuteDuringContinueWhenReady()
	{
		return isStartingFeatureAfterRollup || (isRespinFeatureActive && !reelGame.hasReevaluationSpinsRemaining);
	}

	public override IEnumerator executeDuringContinueWhenReady()
	{
		if (isStartingFeatureAfterRollup)
		{
			// Show the acquire for a respin feature
			isStartingFeatureAfterRollup = false;
			isRespinFeatureActive = true;
			yield return StartCoroutine(playAcquiredAnimation());
		}
		else if (isRespinFeatureActive && !reelGame.hasReevaluationSpinsRemaining)
		{
			// Reset the accumulator now that the respins are complete
			updateAllTrackerAnimations();
			isRespinFeatureActive = false;
		}
	}
	
// executeOnBigWinEnd() section
// Functions here are executed after the big win has been removed from the screen.
	public override bool needsToExecuteOnBigWinEnd()
	{
		return true;
	}
	
	public override void executeOnBigWinEnd()
	{
		if (isCumulativeDataLoaded)
		{
			updateAllTrackerAnimations();
		}
	}
}
