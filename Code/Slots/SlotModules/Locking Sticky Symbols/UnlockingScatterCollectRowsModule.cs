using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

//
// Unlocking Scatter Collect Rows Module is a type of freespin game in which some of the
// rows on the reels are locked and any symbol that lands on them doesn't count to the
// winnings. As SC symbols land, they lock in place and contribute to unlocking the locked
// row. Landing an SC symbol also adds more freespin attempts.
//
// When a locked row unlocks, the SC symbols there are collected and are added to the
// winnings
//
// author : nick saito <nsaito@zynga.com>
// date : July 11, 2019
// games : gen86
//
public class UnlockingScatterCollectRowsModule : LockingSymbolBaseModule
{
	[Header("Unlocking Rows Settings")]
	[SerializeField] private List<LockedRowData> lockedRowData;
	[SerializeField] private float staggeredRowUnlockDelay = 0.0f;

	[Header("Retrigger Freespins Settings")]
	[SerializeField] private AnimatedParticleEffect retriggerParticleEffect;
	[SerializeField] private float retriggerCompleteDelay = 0.0f;

	[Header("RollUp Settings")]
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float regularSymbolRollupTime = 0.0f;
	[Tooltip("If zero this will auto calculate the length of the rollup, if -1 then the rollup will be skipped and credits value updated right away.  Otherwise will use the time specified")]
	[SerializeField] private float jackpotSymbolRollupTime = 0.0f;
	[SerializeField] private float postRollupWaitTime;
	[SerializeField] private string rollupFanfareAudioKey = "";
	[SerializeField] private string rollupOverrideSound = "rollup_jackpot_loop";
	[SerializeField] private string rollupTermOverrideSound = "rollup_jackpot_end";

	[SerializeField] private AnimatedParticleEffect rollupParticleEffect;
	[SerializeField] private AnimatedParticleEffect sparkleTrailParticleEffect;

	// How we should animate the jackpots when they rollup
	[SerializeField] private List<JackpotAnimationData> jackpotAnimationData;

	// Transform data to different symbols based on bet level or tier
	[SerializeField] private ProgressiveJackpotSymbolReplacementData[] symbolReplacementData;

	// The tier that the player started freespins with
	private ProgressiveJackpotSymbolReplacementData currentTierSymbolReplacementData;

	// the tier key, for example hir_gen86_blackout_tier1, use to set the currentTier
	private string progressiveJackpotKey;

	// keep a list of SC symbols that still need to be collected by row so we can collect them when
	// a row unlocks
	private Dictionary<int, List<int>> uncollectedLockedSymbolByRow = new Dictionary<int, List<int>>();

	private ReevaluationUnlockAreaAndResetFreespin reevaluationUnlockAreaAndResetFreespin;
	private List<TICoroutine> lockSymbolsCoroutineList = new List<TICoroutine>();

	// The list of what symbols are worth in credits is buried somewhere in one of the reevaluations, so
	// save a list of this by going through all the reevaluations in the outcome to find it.
	private Dictionary<string, ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout>
		scatterSymbolPayouts;

	private bool didInit = false;
	private bool shouldStopPlaying = true;
	private ProgressiveJackpot progressiveJackpot = null;
	private ModifierExportUnlockAreaAndResetFreespinSlotModule.ModifierExportUnlockAreaAndResetFreespin modifierExportUnlockAreaAndResetFreespin = null;

	private bool hasGameEnded
	{
		get { return (FreeSpinGame.instance.numberOfFreespinsRemaining <= 0); }
	}

	// Look for the basegame ModifierExportUnlockAreaAndResetFreespinSlotModule since that contains
	// data we need to use for the freespins.  Note: Because of this we can never have this game
	// allow gifted spins.
	private ModifierExportUnlockAreaAndResetFreespinSlotModule.ModifierExportUnlockAreaAndResetFreespin getDataFromBaseGameModifierExportUnlockAreaAndResetFreespinSlotModule()
	{
		for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
		{
			ModifierExportUnlockAreaAndResetFreespinSlotModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as ModifierExportUnlockAreaAndResetFreespinSlotModule;
			if (module != null)
			{
				return module.modifierExportUnlockAreaAndResetFreespin;
			}
		}
		
		return null;
	}

	// When the game first starts, we get the saved user data from the server from the modifier_exports
	// ModifierExportUnlockAreaAndResetFreespinSlotModule gets the modifier_export data on the basegame and keeps
	// it static so we can get it in the freespin game.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		modifierExportUnlockAreaAndResetFreespin = getDataFromBaseGameModifierExportUnlockAreaAndResetFreespinSlotModule();
		return modifierExportUnlockAreaAndResetFreespin != null;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		initilizeLockedRows();
		initScatterSymbolPayouts();
		initProgressiveJackpot();
		setCurrentTierSymbolReplacementData(progressiveJackpotKey);
		initScatterSymbolsOnReel();
		didInit = true;
		yield break;
	}

	// Sets the number of SC symbols need to unlock each row from the server data
	private void initilizeLockedRows()
	{
		foreach (ReevaluationUnlockAreaAndResetFreespin.UnlockAreaAndResetLockedRowsInfo lockedRow in
			modifierExportUnlockAreaAndResetFreespin.lockedRows)
		{
			LockedRowData myLockedRowData = getLockedRowData(lockedRow.index);
			myLockedRowData.numToUnlock = lockedRow.unlockInitialNeed;
		}
	}

	// Look for the basegame BuiltInProgressiveJackpotBaseGameModule since that had to be used to
	// determine what bet level and by that what progressive jackpot we qualify for.
	private void initProgressiveJackpot()
	{
		for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
		{
			BuiltInProgressiveJackpotBaseGameModule module =
				SlotBaseGame.instance.cachedAttachedSlotModules[i] as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				progressiveJackpotKey = module.getCurrentJackpotTierKey();
			}
		}
	}

	// Sets the current tier from bet level so that we can transform the symbols as needed.
	private void setCurrentTierSymbolReplacementData(string progressiveJackpotTierKey)
	{
		foreach (ProgressiveJackpotSymbolReplacementData replacementData in symbolReplacementData)
		{
			if (replacementData.jackpotTierName == progressiveJackpotTierKey)
			{
				currentTierSymbolReplacementData = replacementData;
				return;
			}
		}
	}

	// Create a map of symbolName to the symbol's credit value so we can quickly
	// access it for rolling up. These values are found from the modifier exports.
	private void initScatterSymbolPayouts()
	{
		scatterSymbolPayouts =
			new Dictionary<string, ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout>();

		foreach (ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout scatterSymbolPayout in
			modifierExportUnlockAreaAndResetFreespin.scatterSymbolPayouts)
		{
			scatterSymbolPayouts.Add(scatterSymbolPayout.symbolName, scatterSymbolPayout);
		}

		// hack to make progressive jackpot work when collecting the values
		JSON progJackpotWonJson = SlotBaseGame.instance.outcome.getProgressiveJackpotWinJson();
		if (progJackpotWonJson != null)
		{
			ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout jackpotScatterSymbolPayout =
				new ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout();

			jackpotScatterSymbolPayout.symbolName = "SCJ";
			jackpotScatterSymbolPayout.credits = progJackpotWonJson.getLong("running_total", 0);
			jackpotScatterSymbolPayout.isProgressiveJackpot = true;
			scatterSymbolPayouts.Add("SCJ", jackpotScatterSymbolPayout);
		}
	}

	// Update the credit labels on all the slot symbols when the game starts
	private void initScatterSymbolsOnReel()
	{
		foreach (SlotSymbol slotSymbol in reelGame.engine.getAllVisibleSymbols())
		{
			setSymbolLabel(slotSymbol);
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		yield return StartCoroutine(base.executeOnPreSpin());
	}

	// get the reevaluation before reels stop spinning
	public override bool needsToExecutePreReelsStopSpinning()
	{
		reevaluationUnlockAreaAndResetFreespin = getReevaluationUnlockAreaAndResetFreespin();

		if (reevaluationUnlockAreaAndResetFreespin == null ||
		    reevaluationUnlockAreaAndResetFreespin.lockedRows == null ||
		    reevaluationUnlockAreaAndResetFreespin.lockedRows.Count <= 0)
		{
			return false;
		}

		return true;
	}

	// just before the reels stop spinning process all the data so we have replacement symbols,
	// new locking symbol data, and the correct jackpot symbols for the current tier.
	public override IEnumerator executePreReelsStopSpinning()
	{
		updateReplacementSymbols();
		transformSymbolLockJSON();
		replaceProgressiveJackpotSymbolNamesInNewStickySymbols();
		populateNewStickySymbols();
		yield break;
	}

	// In gen86 replacement symbols are sent down in mutation AND reevaluation  So we need to manually update
	// reelGame replacementSymbols. Normally reelGame handles this by just getting it through a mutation, but
	// something on server requires it to be sent down in more than one place.
	private void updateReplacementSymbols()
	{
		JSON[] reevaluationArray = reelGame.outcome.getArrayReevaluations();
		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();

		for (int i = 0; i < reevaluationArray.Length; i++)
		{
			JSON reevaluationJSON = reevaluationArray[i];
			JSON replaceData = reevaluationJSON.getJSON("replace_symbols");

			if (replaceData != null)
			{
				foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict(
					"normal_symbols"))
				{
					// Check and see if mega and normal have the same values.
					if (normalReplaceInfo.Value == currentTierSymbolReplacementData.symbolServerName)
					{
						normalReplacementSymbolMap.Add(normalReplaceInfo.Key,
							currentTierSymbolReplacementData.symbolName);
					}
					else
					{
						normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
					}
				}
			}
		}

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null)
		{
			for (int i = 0; i < reelGame.mutationManager.mutations.Count; i++)
			{
				MutationBase mutation = reelGame.mutationManager.mutations[i];

				if (mutation.type == "symbol_replace_multi")
				{
					StandardMutation replaceSymbolMutation = mutation as StandardMutation;
					foreach (KeyValuePair<string, string> normalReplaceInfo in replaceSymbolMutation
						.normalReplacementSymbolMap)
					{
						if (!normalReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key))
						{
							if (normalReplaceInfo.Value == currentTierSymbolReplacementData.symbolServerName)
							{
								normalReplacementSymbolMap.Add(normalReplaceInfo.Key,
									currentTierSymbolReplacementData.symbolName);
							}
							else
							{
								normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
							}
						}
					}
				}
			}
		}

		if (!normalReplacementSymbolMap.IsEmpty())
		{
			reelGame.engine.setReplacementSymbolMap(normalReplacementSymbolMap, null, isApplyingNow: true);
		}
	}

	// The data for locking symbols in gen86 is sent down in a reevaluation,
	// so we need to change that into a mutator which is what LockingSymbolBaseModule uses.
	// Otherwise we would need add branching code in many places to handle this and the data
	// structure is otherwise identical.
	private void transformSymbolLockJSON()
	{
		// ==== Change Mutation to Reeval to get new_stickies
		JSON[] reevaluationArray = reelGame.outcome.getArrayReevaluations();

		for (int i = 0; i < reevaluationArray.Length; i++)
		{
			ReevaluationBase baseReevaluation = new ReevaluationBase(reevaluationArray[i]);
			if (baseReevaluation.type == "discard_spin_lock_symbols")
			{
				convertReevaluationToMutationAndAddToMutationList(reevaluationArray[i].ToString(),
					"discard_spin_lock_symbols", "symbols_lock_fake_spins_mutator");
			}
		}
	}

	// Helper method to convert a reeval to a mutation
	private void convertReevaluationToMutationAndAddToMutationList(string reevaluationAsString, string originalKey,
		string newKey)
	{
		string mutationAsString = reevaluationAsString.Replace(originalKey, newKey);
		JSON mutationJSON = new JSON(mutationAsString);
		StandardMutation newMutation = new StandardMutation(mutationJSON);
		reelGame.mutationManager.mutations.Add(newMutation);
	}

	// As the reels are stopping, lock symbols in place
	public override bool needsToExecuteOnSpecificReelStop(SlotReel slotReel)
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		foreach (StandardMutation stickyMutation in stickyMutationsList)
		{
			foreach (var replacementCell in stickyMutation.replacementCells)
			{
				if (replacementCell.reelIndex == slotReel.reelID - 1)
				{
					return true;
				}
			}
		}

		return false;
	}

	// As the reels are stopping, lock symbols in place and update row unlock counts.
	public override IEnumerator executeOnSpecificReelStop(SlotReel slotReel)
	{
		lockSymbolsCoroutineList.Add(StartCoroutine(lockSymbolsOnReel(slotReel)));
		yield break;
	}

	private IEnumerator lockSymbolsOnReel(SlotReel slotReel)
	{
		int reelIndex = slotReel.reelID - 1;
		yield return StartCoroutine(lockLandedSymbols(reelIndex));
		playAnimationsForNewLockedSymbols(slotReel);
		collectSymbolsFromReel(slotReel);
	}

	// Changes the SCJ symbols to the correct type of jackpot such as SCJ_Major so we
	// have the right jackpot to match the tier
	private void replaceProgressiveJackpotSymbolNamesInNewStickySymbols()
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		for (int i = 0; i < stickyMutationsList.Count; i++)
		{
			StandardMutation mutation = stickyMutationsList[i];

			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int reelID = 0; reelID < reelGame.getReelRootsLength(); reelID++)
			{
				for (int position = 0;
					position < reelGame.engine.getVisibleSymbolsCountAt(reelArray, reelID, -1);
					position++)
				{
					// Check and see if that visible symbol needs to be changed into something.
					if (!string.IsNullOrEmpty(mutation.triggerSymbolNames[reelID, position]))
					{
						if (mutation.triggerSymbolNames[reelID, position] ==
						    currentTierSymbolReplacementData.symbolServerName)
						{
							mutation.triggerSymbolNames[reelID, position] = currentTierSymbolReplacementData.symbolName;
						}
					}
				}
			}
		}
	}

	// Runs through the reel, collects any new SC symbols, updates the lock counts, and
	// removes the collected symbol from the list.
	// We can optionally play animations and sounds as the symbols are collected here.
	private void collectSymbolsFromReel(SlotReel slotReel, bool playAnimations = false)
	{
		// collect all the locked symbols from this reel
		int symbolIndex = getUncollectedRowIndexFromReel(slotReel.reelID);
		int numSymbolsUnlocked = 0;

		while (symbolIndex >= 0)
		{
			if (playAnimations)
			{
				SlotSymbol slotSymbol = getSlotSymbolAt(slotReel.reelID, symbolIndex);

				if (slotSymbol != null)
				{
					StartCoroutine(playSymbolLockAnimation(slotSymbol));
					StartCoroutine(playSymbolLandingSound(slotSymbol.serverName));
				}
			}

			symbolIndex = getUncollectedRowIndexFromReel(slotReel.reelID);
			numSymbolsUnlocked++;
		}

		updateLockCounts(numSymbolsUnlocked);
	}

	// When the reels stop, design wants to play animations for newly locked symbols
	// even when they are locked rows. So we have a separate method here for playing
	// animations on newly locked symbols for a slotReel.
	private void playAnimationsForNewLockedSymbols(SlotReel slotReel)
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count > 0)
		{
			for (int i = 0; i < stickyMutationsList.Count; i++)
			{
				StandardMutation stickyMutation = stickyMutationsList[i];

				int reelIndex = slotReel.reelID - 1;

				for (int symbolPosition = 0; symbolPosition < stickyMutation.triggerSymbolNames.GetLength(1); symbolPosition++)
				{
					if (!string.IsNullOrEmpty(stickyMutation.triggerSymbolNames[reelIndex, symbolPosition]))
					{
						SlotSymbol slotSymbol = getSlotSymbolAt(slotReel.reelID, symbolPosition);
						StartCoroutine(playSymbolLockAnimation(slotSymbol));
						StartCoroutine(playSymbolLandingSound(slotSymbol.serverName));
					}
				}
			}
		}
	}

	private SlotSymbol getSlotSymbolAt(int reelID, int symbolIndex)
	{
		foreach (SlotSymbol slotSymbol in currentStickySymbols)
		{
			if (slotSymbol.reel.reelID == reelID && slotSymbol.visibleSymbolIndexBottomUp == (symbolIndex + 1))
			{
				return slotSymbol;
			}
		}

		return null;
	}

	private void updateLockCounts(int numSymbolsUnlocked)
	{
		if (numSymbolsUnlocked <= 0)
		{
			return;
		}

		foreach (LockedRowData lockedRow in lockedRowData)
		{
			if (lockedRow.isLocked)
			{
				int currentNum = lockedRow.numToUnlock;
				StartCoroutine(AnimationListController.playListOfAnimationInformation(lockedRow.labelValueChangedAnimations));
				lockedRow.numToUnlock -= numSymbolsUnlocked;
			}
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	// Collect any extra symbols from rows that were unlocked and
	// award extra freespins here at the end, and check if the game
	// is over so we can do all the rollup magic.
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// wait for all the symbols to be locked before proceeding
		if (lockSymbolsCoroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(lockSymbolsCoroutineList));
			lockSymbolsCoroutineList.Clear();
		}

		// unlock rows
		yield return StartCoroutine(unlockRowsStaggered());

		// award freespins
		if (reevaluationUnlockAreaAndResetFreespin.extraSpinsAwarded > 0)
		{
			yield return StartCoroutine(retriggerParticleEffect.animateParticleEffect());
			reelGame.numberOfFreespinsRemaining += reevaluationUnlockAreaAndResetFreespin.extraSpinsAwarded;
			yield return new WaitForSeconds(retriggerCompleteDelay);
		}

		if (hasGameEnded)
		{
			yield return StartCoroutine(endGame());
		}
	}

	protected IEnumerator unlockRowsStaggered()
	{
		foreach (LockedRowData rowData in lockedRowData)
		{
			if (rowData.isLocked && rowData.numToUnlock == 0)
			{
				// unlock the row
				rowData.isLocked = false;
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rowData.unlockAnimations));

				// collect symbols
				SlotReel[] slotReels = reelGame.engine.getReelArray();
				foreach (SlotReel slotReel in slotReels)
				{
					collectSymbolsFromReel(slotReel, playAnimations:true);
				}

				if (staggeredRowUnlockDelay > 0f)
				{
					yield return new WaitForSeconds(staggeredRowUnlockDelay);
				}
			}
		}
	}

	// Go through each row an collect all the winnings
	private IEnumerator endGame()
	{
		int maxRows = getMaxRows();
		SlotReel[] slotReels = reelGame.engine.getReelArray();

		for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
		{
			for (int reelID = 1; reelID <= slotReels.Length; reelID++)
			{
				bool isRowUnlocked = this.isUnlockedRow(rowIndex, slotReels[reelID - 1].visibleSymbols.Length);
				if (isRowUnlocked)
				{
					SlotSymbol slotSymbol = getSlotSymbolAt(reelID, rowIndex);
					if (slotSymbol != null)
					{
						long creditsAwarded = getCreditsAwarded(slotSymbol.serverName);
						StartCoroutine(playSymbolCollectCreditAnimation(slotSymbol));

						JackpotAnimationData jackpotAnimData = getJackpotData(slotSymbol.serverName);
						if (jackpotAnimData != null && jackpotAnimData.winLoop != null)
						{
							StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.winLoop));
						}

						yield return StartCoroutine(
							sparkleTrailParticleEffect.animateParticleEffect(slotSymbol.transform));
						
						// Determine if this is a jackpot or a regular rollup, to determine how quick to make
						// the rollup
						float rollupTime = regularSymbolRollupTime;
						bool isSkippingRollup = false;
						if (jackpotAnimData != null)
						{
							rollupTime = jackpotSymbolRollupTime;
						}

						if (rollupTime == -1)
						{
							isSkippingRollup = true;
						}
						
						yield return StartCoroutine(rollupWinnings(creditsAwarded, rollupFanfareAudioKey, rollupTime, rollupOverrideSound, rollupTermOverrideSound, isSkippingRollup));

						if (jackpotAnimData != null && jackpotAnimData.outroAnimation != null)
						{
							yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotAnimData.outroAnimation));
						}
					}
				}
			}
		}
	}

	private JackpotAnimationData getJackpotData(string jackpotName)
	{
		foreach (JackpotAnimationData jad in jackpotAnimationData)
		{
			if (jad.jackpotSymbolName == jackpotName)
			{
				return jad;
			}
		}

		return null;
	}

	private int getMaxRows()
	{
		int maxRows = 0;
		SlotReel[] slotReels = reelGame.engine.getReelArray();
		foreach (SlotReel slotReel in slotReels)
		{
			if (slotReel.visibleSymbols.Length > maxRows)
			{
				maxRows = slotReel.visibleSymbols.Length;
			}
		}

		return maxRows;
	}

	private long getCreditsAwarded(string symbolName)
	{
		if (symbolName == currentTierSymbolReplacementData.symbolName)
		{
			symbolName = currentTierSymbolReplacementData.symbolServerName;
		}

		if (!String.IsNullOrEmpty(symbolName) && scatterSymbolPayouts[symbolName] != null)
		{
			ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout payoutInfo = scatterSymbolPayouts[symbolName];
			long creditsAwarded = payoutInfo.credits;

			if (!payoutInfo.isProgressiveJackpot)
			{
				// Only non-progressive amounts are multiplied by the game multiplier
				creditsAwarded *= reelGame.multiplier;
			}

			return creditsAwarded;
		}

		return 0;
	}

	private IEnumerator rollupWinnings(long creditsAwarded, string rollupFanfareSound = "", float rollupTime = 0.0f,
		string rollupOverrideSound = "", string rollupTermOverrideSound = "", bool isSkippingRollup = false)
	{
		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += creditsAwarded;

		if (!string.IsNullOrEmpty(rollupFanfareSound))
		{
			Audio.playSoundMapOrSoundKey(rollupFanfareSound);
		}

		if (rollupParticleEffect != null)
		{
			yield return StartCoroutine(rollupParticleEffect.animateParticleEffect());
		}

		if (!isSkippingRollup)
		{
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
		}
		else
		{
			// set the value to the ending value right away
			BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		}

		if (postRollupWaitTime > 0)
		{
			yield return new TIWaitForSeconds(postRollupWaitTime);
		}

		if (rollupParticleEffect != null)
		{
			rollupParticleEffect.stopAllParticleEffects();
		}
	}

	private LockedRowData getLockedRowData(int rowIndex)
	{
		foreach (LockedRowData lrd in lockedRowData)
		{
			if (lrd.rowIndex == rowIndex)
			{
				return lrd;
			}
		}

		return null;
	}

	public ReevaluationUnlockAreaAndResetFreespin getReevaluationUnlockAreaAndResetFreespin()
	{
		JSON[] reevaluationArray = reelGame.outcome.getArrayReevaluations();

		if (reevaluationArray == null || reevaluationArray.Length <= 0)
		{
			return null;
		}

		for (int i = 0; i < reevaluationArray.Length; i++)
		{
			ReevaluationBase baseReevaluation = new ReevaluationBase(reevaluationArray[i]);
			if (baseReevaluation.type == "unlock_area_and_reset_freespin")
			{
				return new ReevaluationUnlockAreaAndResetFreespin(reevaluationArray[i]);
			}
		}

		return null;
	}

	// Creates a list of symbols that were locked on the last spin, so they
	// can be animated by playAnimationsForNewLockedSymbols and have particle
	// trails blast to the spin panel.
	private void populateNewStickySymbols()
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		foreach (StandardMutation stickyMutation in stickyMutationsList)
		{
			foreach (var replacementCell in stickyMutation.replacementCells)
			{
				if (!uncollectedLockedSymbolByRow.ContainsKey(replacementCell.symbolIndex))
				{
					uncollectedLockedSymbolByRow.Add(replacementCell.symbolIndex, new List<int>());
				}

				uncollectedLockedSymbolByRow[replacementCell.symbolIndex].Add(replacementCell.reelIndex);
			}
		}
	}

	// Gets the index of an uncollected symbol from a reel
	private int getUncollectedRowIndexFromReel(int reelID)
	{
		int reelIndex = reelID - 1;
		SlotReel[] slotReels = reelGame.engine.getReelArray();
		SlotReel slotReel = slotReels[reelIndex];

		int numberOfRows = slotReel.visibleSymbols.Length;

		for (int rowIndex = 0; rowIndex < numberOfRows; rowIndex++)
		{
			bool isRowUnlocked = isUnlockedRow(rowIndex, numberOfRows);
			if (isRowUnlocked && uncollectedLockedSymbolByRow.ContainsKey(rowIndex) &&
				uncollectedLockedSymbolByRow[rowIndex].Contains(reelIndex))
			{
				uncollectedLockedSymbolByRow[rowIndex].Remove(reelIndex);
				return rowIndex;
			}
		}

		return -1;
	}

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (symbol.isScatterSymbol)
		{
			return true;
		}

		return false;
	}

	// Sets the credit value of an SC symbol after it is setup
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		if (didInit)
		{
			setSymbolLabel(symbol);
		}
	}

	// Sets the credit value of an SC symbol after it is setup
	private void setSymbolLabel(SlotSymbol symbol)
	{
		if (!symbol.isScatterSymbol)
		{
			return;
		}

		foreach (ModifierExportUnlockAreaAndResetFreespinSlotModule.ScatterSymbolPayout scatterSymbolPayout in
			modifierExportUnlockAreaAndResetFreespin.scatterSymbolPayouts)
		{
			if (symbol.serverName == scatterSymbolPayout.symbolName) // and the correct reel and position
			{
				SymbolAnimator symbolAnimator = symbol.getAnimator();
				if (symbolAnimator != null)
				{
					symbolAnimator.activate(symbol.isFlattenedSymbol);
					LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

					if (symbolLabel != null)
					{
						symbolLabel.text =
							CreditsEconomy.multiplyAndFormatNumberAbbreviated(
								scatterSymbolPayout.credits * reelGame.multiplier, 0, shouldRoundUp: false);
					}
				}
			}
		}
	}

	private bool isUnlockedRow(int rowIndex, int numberOfRows)
	{
		bool isUnlockedRow = false;

		if (rowIndex < (numberOfRows - lockedRowData.Count))
		{
			// this row is one of the bottom unlocked rows for sure
			return true;
		}

		// This might be a locked row, so check it's data to see if has been unlocked yet.
		// Map the rowIndex to an index in our lockedRowData
		int lockedRowIndex = rowIndex - lockedRowData.Count;
		if (lockedRowIndex < lockedRowData.Count && !lockedRowData[lockedRowIndex].isLocked)
		{
			return true;
		}

		return false;
	}

	private IEnumerator playSymbolLockAnimation(SlotSymbol symbol)
	{
		symbol.mutateToUnflattenedVersion();
		yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
		symbol.mutateToFlattenedVersion();
	}

	private IEnumerator playSymbolCollectCreditAnimation(SlotSymbol symbol)
	{
		symbol.mutateToUnflattenedVersion();
		yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		symbol.mutateToFlattenedVersion();
	}

	[System.Serializable]
	public class LockedRowData
	{
		public int rowIndex;
		public AnimationListController.AnimationInformationList unlockAnimations;
		public AnimationListController.AnimationInformationList labelValueChangedAnimations;
		public LabelWrapperComponent unlockTextLabel;

		private bool _isLocked = true;

		public bool isLocked
		{
			get { return _isLocked; }
			set { _isLocked = value; }
		}

		public int numToUnlock
		{
			get { return _numToUnlock; }
			set
			{
				_numToUnlock = value;

				if (_numToUnlock < 0)
				{
					_numToUnlock = 0;
				}

				unlockTextLabel.text = CommonText.formatNumber(_numToUnlock);
			}
		}

		private int _numToUnlock;
	}

	// Animations to play when a jackpot symbol is being collected in the rollup
	[System.Serializable]
	public class JackpotAnimationData
	{
		public string jackpotSymbolName;
		public AnimationListController.AnimationInformationList winLoop;
		public AnimationListController.AnimationInformationList outroAnimation;
	}

	// This allows us to transform SCJ symbols to SCJ_Major / Huge / Grand / Mega
	// so that we can have the correct symbol on the reels and awards the correct
	// amount of credits when they win.
	[System.Serializable]
	public class ProgressiveJackpotSymbolReplacementData
	{
		public string symbolName;
		public string symbolServerName;
		public string jackpotTierName;
	}
}
