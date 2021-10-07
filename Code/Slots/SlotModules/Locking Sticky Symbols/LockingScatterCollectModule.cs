using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// Locking Scatter Collect Module is a type of freespin game in which as symbols
// land, they lock in place. When all the symbols lock in place, the game is won.
// The game ends when either all the symbols lock into place, or the player runs
// out of freespins.

// Symbol Locking is handled by LockingSymbolBaseModule, but rollup, granting freespins,
// and animating trails is all handled here.

// Optionally a ModularChallengeGame can be activated when the game is won to add
// additional rewards to the total awarded credits.

// games : marilyn02, elvis03
// author : nick saito <nsaito@zynga.com>

public class LockingScatterCollectModule : LockingSymbolBaseModule
{
	[System.Serializable]
	public class ScatterJackpotRollupAnimationInfo
	{
		public string scatterJackpotSymbolName;
		public AnimationListController.AnimationInformationList scatterJackpotCelebrationOnAnimations;
		public AnimationListController.AnimationInformationList scatterJackpotCelebrationOffAnimations;
		public string jackpotRollupLoopSound = "";
		public string jackpotRollupTermSound = "";
		public AudioListController.AudioInformationList jackpotWinStartedSounds;
		[Tooltip("Determines how high a priority the sounds for this scatter symbol are with regards to others since they play at the same time. (Higher number means more priority to be played).")]
		public int soundPriority = 0;
	}

	#region configuration
	[Header("Locking Scatter Collect Settings")]
	[SerializeField] private LabelWrapperComponent numberOfSymbolsLockedLabel;
	[SerializeField] private ModularChallengeGame modularChallengeGame;
	[SerializeField] private float numberOfSymbolsLockedIncrementDelay = 0.0f;
	[SerializeField] private AnimationListController.AnimationInformationList animationsToPlayOnSymbolsLockedValueChanged;
	[SerializeField] private AnimationListController.AnimationInformationList animationsToPlayOnAllSymbolsLocked;
	[SerializeField] private List<AnimationListController.AnimationInformationList> pipAcquiredAnimationList;
	[Tooltip("If you want one set of sounds to trigger for all pips acquired regardless of number, use this sound list instead of putting them in pipAcquiredAnimationList.")]
	[SerializeField] private AudioListController.AudioInformationList pipAcquiredSoundsList;

	[Header("RollUp Settings")]
	[SerializeField] private bool rollupEachSpin;
	[SerializeField] private float rollupEachSpinTime = 0.0f;
	[SerializeField] private float singleRollupTime = 0.0f;
	[SerializeField] private float postRollupWait;
	[SerializeField] private string rollupFanfareAudioKey = "";
	[SerializeField] private bool shouldLoopSymbolAnimationsOnRollup = false;
	[SerializeField] private AnimatedParticleEffect rollupParticleEffect;

	[Header("Animated Particle Effect (New)")]
	[SerializeField] private AnimatedParticleEffect sparkleTrailParticleEffect;
	[SerializeField] private AudioListController.AudioInformationList sparkleTrailsCompleteSounds;
	[SerializeField] private bool shouldResetTrailCompleteSoundsOnStart;

	[Header("Retrigger Trail Settings")]
	[SerializeField] private GameObject sparkleTrailPrefab;
	[SerializeField] private Camera sparkleTrailCamera;
	[SerializeField] private Camera symbolCamera;
	[SerializeField] private Camera uiCamera;
	[SerializeField] private iTween.EaseType sparkleTrailEasyType;
	[SerializeField] private float startRetriggerDelay;
	[SerializeField] private float sparkleTrailAnimationDelay;
	[SerializeField] private float sparkleTrailAnimationTime;
	[SerializeField] private float sparkleTrailDestroyDelay;
	[SerializeField] private float sparkleTrailZPosition;
	[SerializeField] private bool staggerRetriggerTrails = true;
	[SerializeField] private float staggerRetriggerTime = 0.2f;
	[SerializeField] private float staggeredMeterBurstZOffset = 1.0f;
	[SerializeField] private float sparkleBurstAnimationTime = 0.0f;
	[SerializeField] private AudioListController.AudioInformationList sparkleTrailTravelSounds;
	[SerializeField] private bool shouldResetTrailTravelSoundsOnStart;
	[SerializeField] private bool shouldResetTrailTravelSoundsEachSpin;
	[SerializeField] private float delayAfterPlayingSparkleTrails = 0.5f;

	[Header("Retrigger Meter Burst Settings")]
	[SerializeField] private GameObject meterBurstPrefab;
	[SerializeField] private Camera meterBurstCamera;
	[SerializeField] private float meterBurstZPosition;
	[SerializeField] private AudioListController.AudioInformationList meterBurstArriveSounds;
	[SerializeField] private bool shouldResetMeterBurstArriveSoundsOnStart;
	[SerializeField] private bool shouldResetMeterBurstArriveSoundsEachSpin;

	[Header("Scatter Jackpot Symbol Settings")]
	[SerializeField] private bool isRolllingUpScatterJackpotSymbolsAlone;
	[SerializeField] private ScatterJackpotRollupAnimationInfo[] scatterJackpotRollupAnimationInfo;

	[SerializeField] private string majorJackpotRollupLoopSound = "rollup_sw_major_loop";
	[SerializeField] private string majorJackpotRollupTermSound = "rollup_sw_major_end";
	[SerializeField] private string majorJackpotWonVOSound = "sw_major_vo";
	#endregion

	#region private vars
	private int numberOfSymbolsLocked = 0;
	private int totalWagerMultiplierAwarded = 0;
	private long totalCreditsAwarded = 0L;
	private int numParticlesAnimating = 0;

	private int scatterJackpotWagerMultiplierAwarded = 0;
	private long scatterJackpotTotalCreditsAwarded = 0L;

	private bool hasRollupFinished = false;
	private bool symbolsDonePlaying = true;
	private float totalStaggeredMeterBurstZOffset = 0f;
	private List<SlotSymbol> allStickySymbols = new List<SlotSymbol>();         // Keep track of all the sticky symbols so we can animate them all at the end. 
	private List<SlotSymbol> newStickySymbols = new List<SlotSymbol>();         // Keep track of new sticky symbols each spin so we can animate them.
	private List<SlotSymbol> allScatterJackpotSymbols = new List<SlotSymbol>(); // If isRolllingUpScatterJackpotSymbolsAlone is enabled then Scatter Jackpot symbols will be stored here rather than in allStickySymbols
	private List<SlotSymbol> newScatterJackpotSymbols = new List<SlotSymbol>(); // If isRolllingUpScatterJackpotSymbolsAlone is enabled then new Scatter Jackpot symbols will be stored here rather than in newStickySymbols
	#endregion

	#region properties
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
			return numberOfSymbolsLocked >= numberOfReelSlots;
		}
	}

	private bool isGameEnded
	{
		get
		{
			return (didLockAllSymbols || FreeSpinGame.instance.numberOfFreespinsRemaining <= 0);
		}
	}
	#endregion

	#region slotmodule overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return shouldResetTrailTravelSoundsOnStart || shouldResetMeterBurstArriveSoundsOnStart || shouldResetTrailCompleteSoundsOnStart;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		resetSoundCollection(sparkleTrailsCompleteSounds, shouldResetTrailCompleteSoundsOnStart);
		resetSoundCollection(sparkleTrailTravelSounds, shouldResetTrailTravelSoundsOnStart);
		resetSoundCollection(meterBurstArriveSounds, shouldResetMeterBurstArriveSoundsOnStart);
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	// When the reel stops, it's time to lock the symbols, handle awarded freespins,
	// and do a credit rollup if required.
	// We also allow resetting sound collections so they play from the beginning as they are
	// used in the handling of freespin awarding.
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		resetSoundCollection(sparkleTrailTravelSounds, shouldResetTrailTravelSoundsEachSpin);
		resetSoundCollection(meterBurstArriveSounds, shouldResetMeterBurstArriveSoundsEachSpin);
		newStickySymbols.Clear();
		newScatterJackpotSymbols.Clear();

		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count > 0)
		{
			yield return StartCoroutine(handleExtraSpinsAwarded(stickyMutationsList));
			yield return new WaitForSeconds(delayAfterPlayingSparkleTrails);
			yield return StartCoroutine(handleRollupEachSpin(stickyMutationsList));
		}

		if (isGameEnded)
		{
			yield return StartCoroutine(endGame());
		}
	}

	// Awards freespins, updates the number of symbols that are locked in position,
	// and tracks wager multiplier and credits awarded
	protected IEnumerator handleExtraSpinsAwarded(List<StandardMutation> stickyMutationsList)
	{
		int totalNumberOfFreeSpinsAwarded = 0;
		int mutationsTotalMultiplierAwarded = 0;
		long mutationsTotalCreditsAwarded = 0;
		int mutationsScatterJackpotTotalMultiplierAwarded = 0;
		long mutationsScatterJackpotTotalCreditsAwarded = 0;
		for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
		{
			StandardMutation mutation = stickyMutationsList[mutationIndex];
			totalNumberOfFreeSpinsAwarded += mutation.numberOfFreeSpinsAwarded;
			if (isRolllingUpScatterJackpotSymbolsAlone && mutation.type == "symbol_locking_multi_payout_jackpot")
			{
				mutationsScatterJackpotTotalMultiplierAwarded += mutation.creditsMultiplier;
				mutationsScatterJackpotTotalCreditsAwarded += mutation.creditsAwarded;
			}
			else
			{
				mutationsTotalMultiplierAwarded += mutation.creditsMultiplier;
				mutationsTotalCreditsAwarded += mutation.creditsAwarded;
			}
		}

		if (totalNumberOfFreeSpinsAwarded > 0)
		{
			populateNewStickySymbols(stickyMutationsList);

			int prevNumberOfSymbolsLocked = numberOfSymbolsLocked;
			numberOfSymbolsLocked += totalNumberOfFreeSpinsAwarded;
			totalWagerMultiplierAwarded += mutationsTotalMultiplierAwarded;
			totalCreditsAwarded += mutationsTotalCreditsAwarded;
			scatterJackpotWagerMultiplierAwarded += mutationsScatterJackpotTotalMultiplierAwarded;
			scatterJackpotTotalCreditsAwarded += mutationsScatterJackpotTotalCreditsAwarded;

			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnSymbolsLockedValueChanged));

			if (pipAcquiredAnimationList != null && pipAcquiredAnimationList.Count > 0)
			{
				yield return StartCoroutine(playAcquiredPipAnimation(prevNumberOfSymbolsLocked));
			}

			if (numberOfSymbolsLockedLabel != null)
			{
				yield return new WaitForSeconds(numberOfSymbolsLockedIncrementDelay); //Need a little extra delay to sync animation with number increment.
				numberOfSymbolsLockedLabel.text = numberOfSymbolsLocked.ToString();
			}

			// Skip doing the animations to award additional spins if we've already locked all the symbols in and are going to end the game
			if (!didLockAllSymbols)
			{
				yield return StartCoroutine(doRetriggerAnimations(stickyMutationsList));
			}
		}
	}

	// Play the animations for the acquired pips, for now will play all of the ones acquired during the spin together
	private IEnumerator playAcquiredPipAnimation(int prevNumberOfSymbolsLocked)
	{
		List<TICoroutine> pipAnimationCoroutinesList = new List<TICoroutine>();

		if (pipAcquiredSoundsList.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(pipAcquiredSoundsList));
		}

		for (int i = prevNumberOfSymbolsLocked; i < numberOfSymbolsLocked; i++)
		{
			if (i < pipAcquiredAnimationList.Count)
			{
				if (pipAcquiredAnimationList[i].Count > 0)
				{
					pipAnimationCoroutinesList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(pipAcquiredAnimationList[i])));
				}
			}
			else
			{
				Debug.LogError("LockingScatterCollectModule.playAcquiredPipAnimation() - i = " + i + " is out of range of pipAcquiredAnimationList!");
			}
		}

		if (pipAnimationCoroutinesList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(pipAnimationCoroutinesList));
		}
		else
		{
			yield break;
		}
	}

	// Animates symbols and performs rolling up of credits if anything was won on the spin
	protected IEnumerator handleRollupEachSpin(List<StandardMutation> stickyMutationsList)
	{
		long mutationsTotalCreditsAwarded = 0;
		long mutationsScatterJackpotTotalCreditsAwarded = 0;
		for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
		{
			StandardMutation mutation = stickyMutationsList[mutationIndex];

			if (isRolllingUpScatterJackpotSymbolsAlone && mutation.type == "symbol_locking_multi_payout_jackpot")
			{
				// separate out the scatter jackpot value so we can roll that up alone
				mutationsScatterJackpotTotalCreditsAwarded += mutation.creditsAwarded;
			}
			else
			{
				mutationsTotalCreditsAwarded += mutation.creditsAwarded;
			}
		}

		if (rollupEachSpin && mutationsTotalCreditsAwarded > 0)
		{
			// Divide the rollupEachSpinTime time in two if we are going to be rolling up the scatter jackpot
			// winnings separately
			float rollupTime = rollupEachSpinTime;
			if (isRolllingUpScatterJackpotSymbolsAlone)
			{
				rollupTime = rollupTime / 2.0f;
			}

			hasRollupFinished = false;
			long creditsAwarded = mutationsTotalCreditsAwarded * reelGame.multiplier;
			playAnimationsForLockedSymbols(newStickySymbols);
			yield return StartCoroutine(rollupWinnings(creditsAwarded, rollupFanfareAudioKey, rollupTime));
			hasRollupFinished = true;

			if (isRolllingUpScatterJackpotSymbolsAlone)
			{
				// make sure the other symbols are done playing before doing the next part
				while (!symbolsDonePlaying)
				{
					yield return null;
				}

				hasRollupFinished = false;
				// handle the separate rollup for the scatter jackpot symbols
				long scatterJackpotCreditsAwarded = mutationsScatterJackpotTotalCreditsAwarded * reelGame.multiplier;
				playAnimationsForLockedSymbols(newScatterJackpotSymbols);

				// Turn on scatter jackpot celebration animaitons
				yield return StartCoroutine(playScatterJackpotCelebrationOnAnimations(newScatterJackpotSymbols));

				// Get the sound data we'll use, since we can only use one set of data, even though we are doing both
				// jackpots together
				ScatterJackpotRollupAnimationInfo soundData = getScatterJackpotRollupAnimationInfoWithHighestPriority(newScatterJackpotSymbols);
				string rollupLoopSound = "";
				string rollupTermSound = "";
				if (soundData != null)
				{
					if (soundData.jackpotWinStartedSounds.Count > 0)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(soundData.jackpotWinStartedSounds));
					}
					rollupLoopSound = soundData.jackpotRollupLoopSound;
					rollupTermSound = soundData.jackpotRollupTermSound;
				}

				yield return StartCoroutine(rollupWinnings(scatterJackpotCreditsAwarded, rollupFanfareAudioKey, rollupTime, rollupLoopSound, rollupTermSound));
				// Turn off scatter jackpot celebration animaitons
				yield return StartCoroutine(playScatterJackpotCelebrationOffAnimations(newScatterJackpotSymbols));
				hasRollupFinished = true;
			}

			while (!symbolsDonePlaying)
			{
				yield return null;
			}
		}
	}

	// Used to check if we have ScatterJackpotRollupAnimationInfo for the passed symbol
	// and if so we will add these symbols to the list of ones to be animated separately
	// if the isRolllingUpScatterJackpotSymbolsAlone is true
	private bool hasScatterJackpotRollupAnimationInforForSymbolName(string symbolName)
	{
		for (int i = 0; i < scatterJackpotRollupAnimationInfo.Length; i++)
		{
			if (scatterJackpotRollupAnimationInfo[i].scatterJackpotSymbolName == symbolName)
			{
				return true;
			}
		}

		return false;
	}

	// Find matching ScatterJackpotRollupAnimationInfo for passed symbol name
	// Returns null if a matching entry isn't found
	private ScatterJackpotRollupAnimationInfo getScatterJackpotRollupAnimationInfoForSymbolName(string symbolName)
	{
		for (int i = 0; i < scatterJackpotRollupAnimationInfo.Length; i++)
		{
			ScatterJackpotRollupAnimationInfo currentInfo = scatterJackpotRollupAnimationInfo[i];
			if (currentInfo.scatterJackpotSymbolName == symbolName)
			{
				return currentInfo;
			}
		}

		Debug.LogError("LockingScatterCollectModule.getScatterJackpotRollupAnimationInfoForSymbolName() - Couldn't find ScatterJackpotRollupAnimationInfo for symbolName = " + symbolName);
		return null;
	}

	// Returns the ScatterJackpotRollupAnimationInfo with the highest sound priorty to be played 
	private ScatterJackpotRollupAnimationInfo getScatterJackpotRollupAnimationInfoWithHighestPriority(List<SlotSymbol> lockedJackpotSymbols)
	{
		HashSet<string> uniqueNames = getUniqueScatterJackpotSymbolNamesInList(lockedJackpotSymbols);
		ScatterJackpotRollupAnimationInfo highestSoundPriortyData = null;

		foreach (string name in uniqueNames)
		{
			ScatterJackpotRollupAnimationInfo currentInfo = getScatterJackpotRollupAnimationInfoForSymbolName(name);
			if (currentInfo != null)
			{
				if (highestSoundPriortyData == null || (currentInfo.soundPriority > highestSoundPriortyData.soundPriority))
				{
					highestSoundPriortyData = currentInfo;
				}
			}
		}

		return highestSoundPriortyData;
	}

	// Play the scatter jackpot celebration on animations
	private IEnumerator playScatterJackpotCelebrationOnAnimations(List<SlotSymbol> lockedJackpotSymbols)
	{
		HashSet<string> uniqueNames = getUniqueScatterJackpotSymbolNamesInList(lockedJackpotSymbols);
		foreach (string name in uniqueNames)
		{
			ScatterJackpotRollupAnimationInfo currentInfo = getScatterJackpotRollupAnimationInfoForSymbolName(name);
			if (currentInfo != null)
			{
				if (currentInfo.scatterJackpotCelebrationOnAnimations.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentInfo.scatterJackpotCelebrationOnAnimations));
				}
			}
		}
	}

	// Play the scatter jackpot celebration off animations
	private IEnumerator playScatterJackpotCelebrationOffAnimations(List<SlotSymbol> lockedJackpotSymbols)
	{
		HashSet<string> uniqueNames = getUniqueScatterJackpotSymbolNamesInList(lockedJackpotSymbols);
		foreach (string name in uniqueNames)
		{
			ScatterJackpotRollupAnimationInfo currentInfo = getScatterJackpotRollupAnimationInfoForSymbolName(name);
			if (currentInfo != null)
			{
				if (currentInfo.scatterJackpotCelebrationOffAnimations.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentInfo.scatterJackpotCelebrationOffAnimations));
				}
			}
		}
	}

	// Determine the list of unique scatter jackpot symbol names, these will be used
	private HashSet<string> getUniqueScatterJackpotSymbolNamesInList(List<SlotSymbol> lockedJackpotSymbols)
	{
		HashSet<string> uniqueSymbols = new HashSet<string>();
		for (int i = 0; i < lockedJackpotSymbols.Count; i++)
		{
			string serverName = lockedJackpotSymbols[i].serverName;
			if (!uniqueSymbols.Contains(serverName))
			{
				uniqueSymbols.Add(serverName);
			}
		}

		return uniqueSymbols;
	}

	// Ends the game by removing any extra freespins.
	// Also does any final rollup for the freespin part of this game.
	protected IEnumerator endGame()
	{
		// Force the freespins to end if there are any left.
		FreeSpinGame.instance.numberOfFreespinsRemaining = 0;

		// players should win at least their wager
		if (totalWagerMultiplierAwarded <= 0)
		{
			totalWagerMultiplierAwarded = 1;
		}

		// We need to do the rollup here at the end if it wasn't handled for each spin.
		if (!rollupEachSpin)
		{
			// Divide the rollupEachSpinTime time in two if we are going to be rolling up the scatter jackpot
			// winnings separately
			float rollupTime = singleRollupTime;
			if (isRolllingUpScatterJackpotSymbolsAlone)
			{
				rollupTime = rollupTime / 2.0f;
			}

			hasRollupFinished = false;
			long finalCreditsAwarded = totalCreditsAwarded * reelGame.multiplier;
			playAnimationsForLockedSymbols(allStickySymbols);
			yield return StartCoroutine(rollupWinnings(finalCreditsAwarded, rollupFanfareAudioKey, singleRollupTime));
			hasRollupFinished = true;

			if (isRolllingUpScatterJackpotSymbolsAlone)
			{
				// make sure the other symbols are done playing before doing the next part
				while (!symbolsDonePlaying)
				{
					yield return null;
				}

				hasRollupFinished = false;
				// handle the separate rollup for the scatter jackpot symbols
				long scatterJackpotCreditsAwarded = scatterJackpotTotalCreditsAwarded * reelGame.multiplier;
				playAnimationsForLockedSymbols(allScatterJackpotSymbols);

				// Turn on scatter jackpot celebration animaitons
				yield return StartCoroutine(playScatterJackpotCelebrationOnAnimations(allScatterJackpotSymbols));

				// Get the sound data we'll use, since we can only use one set of data, even though we are doing both
				// jackpots together
				ScatterJackpotRollupAnimationInfo soundData = getScatterJackpotRollupAnimationInfoWithHighestPriority(allScatterJackpotSymbols);
				string rollupLoopSound = "";
				string rollupTermSound = "";
				if (soundData != null)
				{
					if (soundData.jackpotWinStartedSounds.Count > 0)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(soundData.jackpotWinStartedSounds));
					}
					rollupLoopSound = soundData.jackpotRollupLoopSound;
					rollupTermSound = soundData.jackpotRollupTermSound;
				}

				yield return StartCoroutine(rollupWinnings(scatterJackpotCreditsAwarded, rollupFanfareAudioKey, rollupTime, rollupLoopSound, rollupTermSound));
				// Turn off scatter jackpot celebration animaitons
				yield return StartCoroutine(playScatterJackpotCelebrationOffAnimations(allScatterJackpotSymbols));
				hasRollupFinished = true;
			}
		}

		//call the base reelGame to have the currentPayout carry over for the pickgame
		reelGame.onPayoutRollup(BonusGamePresenter.instance.currentPayout);
	}

	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return true;
	}

	public override IEnumerator executeOnFreespinGameEnd()
	{
		if (didLockAllSymbols && modularChallengeGame != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnAllSymbolsLocked));
			yield return StartCoroutine(startModularChallengeGame());
		}
	}

	// Creates a list of symbols that were locked on the last spin, so they 
	// can be animated by playAnimationsForNewLockedSymbols and have particle
	// trails blast to the spin panel.
	private void populateNewStickySymbols(List<StandardMutation> stickyMutationsList)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

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
						// Lock this reel so it doesn't spin anymore
						slotReel.isLocked = true;

						SlotSymbol symbol = slotReel.visibleSymbols[0];

						if (symbol != null)
						{
							if (!isRolllingUpScatterJackpotSymbolsAlone || !hasScatterJackpotRollupAnimationInforForSymbolName(symbol.serverName))
							{
								// newStickySymbols is cleared each spin so we know which ones are new
								newStickySymbols.Add(symbol);
								// allStickySymbols keeps track of all the symbols that are stickied.
								allStickySymbols.Add(symbol);
							}
							else
							{
								// dealing with scatter jackpot separately
								// newScatterJackpotSymbols is cleared each spin so we know which ones are new
								newScatterJackpotSymbols.Add(symbol);
								// allScatterJackpotSymbols keeps track of all the scatter jackpot symbols that are stickied.
								allScatterJackpotSymbols.Add(symbol);
							}
						}
					}
				}
			}
		}
	}
	#endregion

	#region grant freespins
	// Retrigger Animations will animate particle trails from newly locked symbols
	// to the spin meter and fire of a meter burst.
	protected virtual IEnumerator doRetriggerAnimations(List<StandardMutation> stickyMutationsList)
	{
		yield return new WaitForSeconds(startRetriggerDelay);

		totalStaggeredMeterBurstZOffset = 0f;

		if (uiCamera == null)
		{
			uiCamera = BonusGameManager.instance.GetComponentInParent<Camera>();
		}

		Vector3 spinCountScreenPosition = uiCamera.WorldToScreenPoint(BonusSpinPanel.instance.spinCountLabel.transform.position);
		Vector3 sparkleTrailEndPosition = sparkleTrailCamera.ScreenToWorldPoint(spinCountScreenPosition);
		sparkleTrailEndPosition.z = sparkleTrailZPosition;

		if (sparkleTrailParticleEffect != null)
		{
			// If we are using the new AnimatedParticleEffect we need to keep track of how many
			// particles we need to wait for to complete animating
			numParticlesAnimating = newStickySymbols.Count + newScatterJackpotSymbols.Count;
		}

		List<TICoroutine> particleTrailCoroutineList = new List<TICoroutine>();
		for (int i = 0; i < newStickySymbols.Count; i++)
		{
			// Add stagger if we've already started other trails
			if (i != 0)
			{
				if (staggerRetriggerTime > 0.0f)
				{
					yield return new WaitForSeconds(staggerRetriggerTime);
				}
			}

			particleTrailCoroutineList.Add(StartCoroutine(doParticleTrailEffectFromSymbol(newStickySymbols[i], sparkleTrailEndPosition)));
		}

		for (int i = 0; i < newScatterJackpotSymbols.Count; i++)
		{
			// Add stagger if we've already started other trails
			if ((i != 0 || particleTrailCoroutineList.Count > 0))
			{
				if (staggerRetriggerTime > 0.0f)
				{
					yield return new WaitForSeconds(staggerRetriggerTime);
				}
			}

			particleTrailCoroutineList.Add(StartCoroutine(doParticleTrailEffectFromSymbol(newScatterJackpotSymbols[i], sparkleTrailEndPosition)));
		}

		// Wait for all the particle trails and effects to actually finish
		if (particleTrailCoroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(particleTrailCoroutineList));
		}


		// If we are using the new AnimatedParticleEffect we need to wait for all the particles to
		// finish animating.
		if (sparkleTrailParticleEffect != null)
		{
			while (numParticlesAnimating > 0)
			{
				yield return null;
			}

			if (sparkleTrailsCompleteSounds != null && sparkleTrailsCompleteSounds.Count > 0)
			{
				yield return StartCoroutine(AudioListController.playListOfAudioInformation(sparkleTrailsCompleteSounds));
			}
		}
	}

	// Do the sparkle trail and meter burst from a newly locked symbol to the freespins meter
	private IEnumerator doParticleTrailEffectFromSymbol(SlotSymbol slotSymbol, Vector3 sparkleTrailEndPosition)
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		coroutineList.Add(StartCoroutine(animateParticleTrailToEndPosition(slotSymbol, sparkleTrailEndPosition)));
		coroutineList.Add(StartCoroutine(doMeterBurst()));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	// When a particle animation is complete it should send us an event that we handle here.
	public void handleParticleEventCompleted()
	{
		numParticlesAnimating--;
	}

	// Function to handle animating the particle trail to the end position, used so that
	// we can trigger the burst while the trail is still traveling to mimic the way this
	// class used to handle this display flow
	private IEnumerator animateParticleTrailToEndPosition(SlotSymbol slotSymbol, Vector3 sparkleTrailEndPosition)
	{
		if (sparkleTrailParticleEffect != null)
		{
			yield return StartCoroutine(sparkleTrailParticleEffect.animateParticleEffect(slotSymbol.transform));
		}
		else
		{
			Vector3 symbolPosition = slotSymbol.transform.position;
			Vector3 symbolScreenPosition = symbolCamera.WorldToScreenPoint(symbolPosition);
			Vector3 startPosition = sparkleTrailCamera.ScreenToWorldPoint(symbolScreenPosition);
			startPosition.z = sparkleTrailZPosition;

			GameObject sparkleEffect = CommonGameObject.instantiate(sparkleTrailPrefab, Vector3.zero, Quaternion.identity, gameObject.transform) as GameObject;
			sparkleEffect.transform.SetPositionAndRotation(startPosition, Quaternion.identity);
			sparkleEffect.SetActive(true);

			List<ITIYieldInstruction> trailInstructions = new List<ITIYieldInstruction>();
			trailInstructions.Add(new TITweenYieldInstruction(iTween.MoveTo(sparkleEffect,
					iTween.Hash("position", sparkleTrailEndPosition,
					"delay", sparkleTrailAnimationDelay,
					"time", sparkleTrailAnimationTime,
					"easetype", sparkleTrailEasyType))));
			trailInstructions.Add(StartCoroutine(AudioListController.playListOfAudioInformation(sparkleTrailTravelSounds)));
			yield return StartCoroutine(Common.waitForITIYieldInstructionsToEnd(trailInstructions));

			if (sparkleTrailDestroyDelay > 0.0f)
			{
				yield return new WaitForSeconds(sparkleTrailDestroyDelay);
			}

			Destroy(sparkleEffect);
		}
	}

	protected IEnumerator doMeterBurst()
	{
		if (sparkleTrailParticleEffect == null)
		{
			yield return new WaitForSeconds(sparkleTrailAnimationDelay + sparkleTrailAnimationTime);

			if (uiCamera == null)
			{
				uiCamera = BonusGameManager.instance.GetComponentInParent<Camera>();
			}

			Vector3 spinCountScreenPosition = uiCamera.WorldToScreenPoint(BonusSpinPanel.instance.spinCountLabel.transform.position);
			Vector3 meterBurstPosition = meterBurstCamera.ScreenToWorldPoint(spinCountScreenPosition);
			meterBurstPosition.z = meterBurstZPosition + totalStaggeredMeterBurstZOffset;
			totalStaggeredMeterBurstZOffset += staggeredMeterBurstZOffset;

			GameObject meterBurstEffect = CommonGameObject.instantiate(meterBurstPrefab, Vector3.zero, Quaternion.identity, gameObject.transform) as GameObject;
			meterBurstEffect.transform.SetPositionAndRotation(meterBurstPosition, Quaternion.identity);
			meterBurstEffect.SetActive(true);

			yield return StartCoroutine(AudioListController.playListOfAudioInformation(meterBurstArriveSounds));

			reelGame.numberOfFreespinsRemaining++;

			if (sparkleBurstAnimationTime > 0.0f)
			{
				yield return new WaitForSeconds(sparkleBurstAnimationTime);
			}

			if (sparkleTrailDestroyDelay > 0.0f)
			{
				yield return new WaitForSeconds(sparkleTrailDestroyDelay);
			}

			Destroy(meterBurstEffect);
		}
	}

	public void incrementNumberOfFreespinsRemaining()
	{
		++reelGame.numberOfFreespinsRemaining;
	}

	protected void resetSoundCollection(AudioListController.AudioInformationList soundEffects, bool shouldResetSoundEffects)
	{
		if (!shouldResetSoundEffects)
		{
			return;
		}
		else
		{
			foreach (AudioListController.AudioInformation info in soundEffects.audioInfoList)
			{
				Audio.resetCollectionBySoundMapOrSoundKey(info.SOUND_NAME);
			}
		}
	}
	#endregion

	#region rollup
	private IEnumerator rollupWinnings(long creditsAwarded, string rollupFanfareSound = "", float rollupTime = 0.0f, string rollupOverrideSound = "", string rollupTermOverrideSound = "")
	{
		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += creditsAwarded;

		if (!rollupFanfareSound.IsNullOrWhiteSpace())
		{
			Audio.play(rollupFanfareSound);
		}

		if (rollupParticleEffect != null)
		{
			yield return StartCoroutine(rollupParticleEffect.animateParticleEffect());
		}

		yield return StartCoroutine(SlotUtils.rollup(
			start: currentWinnings,
			end: currentWinnings + creditsAwarded,
			tmPro: BonusSpinPanel.instance.winningsAmountLabel,
			playSound: true,
			specificRollupTime: rollupTime,
			shouldSkipOnTouch: true,
			shouldBigWin: false,
			rollupOverrideSound: rollupOverrideSound,
			rollupTermOverrideSound: rollupTermOverrideSound));

		yield return new TIWaitForSeconds(postRollupWait);

		if (rollupParticleEffect != null)
		{
			rollupParticleEffect.stopAllParticleEffects();
		}
	}

	private void playAnimationsForLockedSymbols(List<SlotSymbol> symbolList)
	{
		for (int i = 0; i < symbolList.Count; i++)
		{
			if (symbolList[i].isScatterSymbol)
			{
				StartCoroutine(playSymbolAnimation(symbolList[i]));
			}
		}
	}

	private IEnumerator playSymbolAnimation(SlotSymbol symbol)
	{
		symbolsDonePlaying = false;
		yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		while (shouldLoopSymbolAnimationsOnRollup && !hasRollupFinished)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		}
		symbolsDonePlaying = true;
	}
	#endregion

	#region challenge game	
	// Creates a pick game if the battle result was a win
	private IEnumerator startModularChallengeGame()
	{
		PickemOutcome pickGameOutcome = getPickOutcome();

		if (pickGameOutcome != null)
		{
			//add any extra credits awarded
			totalCreditsAwarded += pickGameOutcome.jackpotFinalValue;
			totalCreditsAwarded *= pickGameOutcome.finalMultiplier;

			// Convert our outcome to ModularChallengeGameOutcome
			ModularChallengeGameOutcome modularPickGameOutcome = new ModularChallengeGameOutcome(pickGameOutcome);
			List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();
			variantOutcomeList.Add(modularPickGameOutcome);
			modularChallengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
			modularChallengeGame.init();
			LabelWrapperComponent labelWrapperComponent = BonusSpinPanel.instance.winningsAmountLabel.gameObject.AddComponent<LabelWrapperComponent>();
			modularChallengeGame.pickingRounds[0].roundVariants[0].winLabel = labelWrapperComponent;
			modularChallengeGame.gameObject.SetActive(true);

			// Wait till this pick game feature is over before continuing
			while (BonusGamePresenter.instance.isGameActive)
			{
				yield return null;
			}
		}
	}

	// this needs to be better
	private PickemOutcome getPickOutcome()
	{
		SlotOutcome currentOutcome = reelGame.getCurrentOutcome();

		if (currentOutcome != null)
		{
			if (currentOutcome.hasSubOutcomes())
			{
				ReadOnlyCollection<SlotOutcome> subOutcomes = currentOutcome.getSubOutcomesReadOnly();

				if (subOutcomes != null && subOutcomes.Count > 0)
				{
					if (subOutcomes[0].getOutcomeType() == SlotOutcome.OutcomeTypeEnum.BONUS_GAME && subOutcomes[0].hasSubOutcomes())
					{
						ReadOnlyCollection<SlotOutcome> bonusGameSubOutcomes = subOutcomes[0].getSubOutcomesReadOnly();

						if (bonusGameSubOutcomes.Count > 0 && bonusGameSubOutcomes[0].getOutcomeType() == SlotOutcome.OutcomeTypeEnum.PICKEM)
						{
							return new PickemOutcome(subOutcomes[0]);
						}
					}
				}
			}
		}

		return null;
	}

	#endregion
}
