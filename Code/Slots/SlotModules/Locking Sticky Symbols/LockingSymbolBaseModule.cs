using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockingSymbolBaseModule : SlotModule
{
	[Header("Locking Symbol Settings")]
	[SerializeField] protected string nameOfSymbolCollected;

	[Tooltip("Should stickies be attached to reels instead of to a Sticky Symbols parent which isn't attached to the reels?")]
	[SerializeField] protected bool attachStickiesToReels = false;

	[Tooltip("Controls if sticky symbols play one at a time or all at once, ignored if stickies played on specific reel stops")]
	[SerializeField] protected bool isStaggeringStickySymbols = true;
	[SerializeField] protected bool mutateSymbolsBeforeSettingUpStickies = true;
	[SerializeField] protected bool hasLockingSymbolRevealAnimation = false;
	[SerializeField] protected float lockingSymbolRevealAnimationLength;
	[SerializeField] protected List<AnimationListController.AnimationInformationList> rewardAnimations;

	[Tooltip("Use this when you want the sticky symbols to have different animations then the final symbol which is part of paylines.")]
	[SerializeField] protected string STICKY_SYMBOL_NAME_POSTFIX = "";
	[SerializeField] protected string SYMBOL_ANIMATION_SOUND_KEY;
	[SerializeField] protected string STICKY_SYMBOL_LOCKING_SOUND_KEY;
	[SerializeField] protected int stickySoundCountPerReelMax = -1;
	[SerializeField] protected string SYMBOL_REVEAL_POSTFIX = "_Reveal";

	[Tooltip("Use to offset sticky symbols on the z axis so that symbol still underneath are hidden")]
	[SerializeField] protected float stickySymbolZOffset = 0.0f;

	[Tooltip("Use to move sticky symbols in a certain position/row to specified Layer")]
	[SerializeField] protected List<SymbolCustomLayerMapItem> stickySymbolLayerMap; // stickySymbolLayerMap[position/row] = Layer
	
	[Header("Locking Symbol Sounds")]
	[SerializeField] protected List<SymbolSoundInfo> symbolLandingSounds;
	[SerializeField] protected bool playMutationLandingSoundsOnReelStopping;

	protected GameObject stickySymbolsParent;
	protected List<SlotSymbol> currentStickySymbols = new List<SlotSymbol>();
	protected List<int> stickySoundCountPerReel = new List<int>();

	protected int symbolsCollected;

	public override void Awake()
	{
		base.Awake();
		if (stickySymbolsParent == null && !attachStickiesToReels)
		{
			stickySymbolsParent = new GameObject();
			stickySymbolsParent.name = "Sticky Symbols";
			stickySymbolsParent.transform.parent = this.transform;
			stickySymbolsParent.transform.localScale = Vector3.one;
			stickySymbolsParent.layer = Layers.ID_SLOT_OVERLAY;
		}

		StartCoroutine(loopAnimateAllStickySymbols());
	}

	protected virtual IEnumerator loopAnimateAllStickySymbols()
	{
		yield break;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (attachStickiesToReels)
		{
			// show all the stickies by revealing all of the ones attached to the reels
			for (int i = 0; i < currentStickySymbols.Count; i++)
			{
				SlotSymbol symbol = currentStickySymbols[i];
				symbol.getAnimator().activate(symbol.isFlattenedSymbol);
			}
		}
		else
		{
			// show all stickies by showing the parent that contains them all
			stickySymbolsParent.SetActive(true);
		}

		stickySoundCountPerReel.Clear();
		while (stickySoundCountPerReel.Count < reelGame.getReelRootsLength())
		{
			stickySoundCountPerReel.Add(0);
		}

		yield return null;
	}

	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return stoppingReel.isStopping && playMutationLandingSoundsOnReelStopping;
	}

	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		StartCoroutine(playLandingSounds(stoppingReel));
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return stoppedReel.isStopped && !playMutationLandingSoundsOnReelStopping;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		yield return StartCoroutine(playLandingSounds(stoppedReel));
	}

	protected virtual IEnumerator playLandingSounds(SlotReel reel)
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count > 0)
		{
			List<TICoroutine> landingSoundCoroutines = new List<TICoroutine>();

			for (int i = 0; i < stickyMutationsList.Count; i++)
			{
				StandardMutation stickyMutation = stickyMutationsList[i];

				int reelIndex = reel.reelID - 1;

				for (int symbolPosition = 0; symbolPosition < stickyMutation.triggerSymbolNames.GetLength(1); symbolPosition++)
				{
					if (!string.IsNullOrEmpty(stickyMutation.triggerSymbolNames[reelIndex, symbolPosition]))
					{
						landingSoundCoroutines.Add(StartCoroutine(
							playSymbolLandingSound(stickyMutation.triggerSymbolNames[reelIndex, symbolPosition])));
					}
				}
			}

			if (landingSoundCoroutines.Count > 0)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(landingSoundCoroutines));
			}
		}
	}

	public virtual IEnumerator lockLandedSymbols(int reelID = -1)
	{
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count == 0)
		{
			// make sure we still convert the stickies we already have, even if there aren't new ones
			if (currentStickySymbols != null && currentStickySymbols.Count > 0)
			{
				yield return RoutineRunner.instance.StartCoroutine(mutateBaseSymbols(reelID));
			}
		}
		else
		{
			if (mutateSymbolsBeforeSettingUpStickies)
			{
				yield return StartCoroutine(mutateBaseSymbols(reelID));
			}

			if (rewardAnimations != null && rewardAnimations.Count > 0)
			{
				yield return StartCoroutine(playRewardAnimations());

				int totalSymbolsCollectedInMutations = 0;

				for (int i = 0; i < stickyMutationsList.Count; i++)
				{
					totalSymbolsCollectedInMutations += stickyMutationsList[i].numberOfSymbolsCollected;
				}

				if (totalSymbolsCollectedInMutations > 0 && totalSymbolsCollectedInMutations != symbolsCollected)
				{
					Debug.LogError("Number of symbols collected does not match server data. Please investigate.");
				}
			}

			yield return StartCoroutine(setUpStickySymbols(stickyMutationsList, reelID));
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

	protected virtual IEnumerator mutateBaseSymbols(int specificReelID = -1)
	{
		for (int i = 0; i < currentStickySymbols.Count; i++)
		{
			SlotSymbol symbol = currentStickySymbols[i];

			if (specificReelID > -1 && symbol.reel.reelID != (specificReelID + 1))
			{
				continue;
			}

			int reelID = symbol.reel.reelID - 1;
			int position;
			if (reelGame.engine.reelSetData.isIndependentReels)
			{
				// if we're using independent reels, we need to reference the reel's position 
				// since the symbol's index will always be relative
				position = symbol.reel.position;
			}
			else
			{
				position = symbol.visibleSymbolIndex;
			}

			SlotSymbol symbolToMutate = reelGame.engine.getVisibleSymbolsAt(reelID)[position];

			string symbolName = symbol.serverName;
			if (STICKY_SYMBOL_NAME_POSTFIX != "")
			{
				symbolName = symbolName.Replace(STICKY_SYMBOL_NAME_POSTFIX, "");
			}

			if (symbolToMutate.name != symbolName)
			{
				symbolToMutate.mutateTo(symbolName, null, false, true); // Skip the animation.
			}

			if (attachStickiesToReels)
			{
				// if the sticky is attached to the reels hide it now that we've replaced it on the reels
				symbol.getAnimator().deactivate();
			}
		}

		if (!attachStickiesToReels && specificReelID<0)
		{
			// just turn off the parent of all the sticky symbols
			stickySymbolsParent.SetActive(false);
		}

		yield break;
	}

	protected IEnumerator playRewardAnimations()
	{
		foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (symbol.shortServerName == nameOfSymbolCollected)
			{
				symbol.animateOutcome();
				if (!string.IsNullOrEmpty(SYMBOL_ANIMATION_SOUND_KEY))
				{
					Audio.playSoundMapOrSoundKey(SYMBOL_ANIMATION_SOUND_KEY);
				}
				yield return new TIWaitForSeconds(symbol.info.customAnimationDurationOverride);
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardAnimations[symbolsCollected]));
				symbolsCollected++;
			}
		}
	}

	protected virtual IEnumerator setUpStickySymbols(List<StandardMutation> stickyMutationsList, int specificReelID = -1)
	{
		if (stickyMutationsList.Count == 0)
		{
			Debug.LogError("Trying to set up sticky symbols with no mutation!");
			yield break;
		}

		TICoroutine stickySymbolCoroutine = null;

		for (int i = 0; i < stickyMutationsList.Count; i++)
		{
			StandardMutation mutation = stickyMutationsList[i];

			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int reelID = 0; reelID < reelGame.getReelRootsLength(); reelID++)
			{
				if (specificReelID > -1 && specificReelID != reelID)
				{
					continue;
				}

				for (int position = 0; position < reelGame.engine.getVisibleSymbolsCountAt(reelArray, reelID, -1); position++)
				{
					// Check and see if that visible symbol needs to be changed into something.
					if (!string.IsNullOrEmpty(mutation.triggerSymbolNames[reelID, position]))
					{
						if (specificReelID < 0 && isStaggeringStickySymbols)
						{
							// play and wait on each one
							yield return StartCoroutine(changeSymbolToSticky(reelID, position, mutation.triggerSymbolNames[reelID, position]));
						}
						else
						{
							// start them all at the same time
							stickySymbolCoroutine = StartCoroutine(changeSymbolToSticky(reelID, position, mutation.triggerSymbolNames[reelID, position]));
						}
					}
				}
			}
		}

		if (stickySymbolCoroutine != null)
		{
			// wait for the last coroutine to finish if isStaggeringStickySymbols is FALSE
			yield return stickySymbolCoroutine;
		}

		yield return RoutineRunner.instance.StartCoroutine(mutateBaseSymbols(specificReelID));
	}

	protected virtual IEnumerator changeSymbolToSticky(int reelID, int position, string name)
	{
		if (!(reelGame.engine.reelSetData.isIndependentReels && !reelGame.engine.reelSetData.isHybrid))
		{
			position = reelGame.engine.getVisibleSymbolsAt(reelID).Length - 1 - position;
		}

		if (hasLockingSymbolRevealAnimation)
		{
			StartCoroutine(playLockingSymbolRevealAnimation(reelID, position, name + SYMBOL_REVEAL_POSTFIX));
			yield return new TIWaitForSeconds(lockingSymbolRevealAnimationLength - 0.2f);
		}

		// Make a new symbol.
		SlotSymbol newSymbol = new SlotSymbol(reelGame);
		SlotSymbol[] visibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelID);
		SlotSymbol symbolToSticky = visibleSymbols[position];
		int newSymbolIndex = symbolToSticky.index; // index will be different from position if on independent reel
		SlotReel reel = symbolToSticky.reel;

		// Setup symbol with updated index
		newSymbol.setupSymbol(name + STICKY_SYMBOL_NAME_POSTFIX, newSymbolIndex, reel);

		// We need to set the local position here for independent reel games since engine.getSlotReelAt is returning
		// the actual reel object that is in that position, but the server is sending us a relative position. 
		// Calculate the sticky symbol offset based on the symbol info of both the new symbol and the target symbol
		SlotSymbol targetSymbol = reelGame.engine.getVisibleSymbolsAt(reelID)[position];
		Vector3 offset = Vector3.zero;

		if (newSymbol.info != null)
		{
			offset = newSymbol.info.positioning;
			if (targetSymbol.info != null)
			{
				offset = targetSymbol.info.positioning - offset;
				offset.z += stickySymbolZOffset;
			}
		}

		newSymbol.transform.localPosition = targetSymbol.transform.localPosition - offset;
		newSymbol.gameObject.name = "sticky_" + name + " (" + reelID + ", " + position + ")";
		newSymbol.debugName = name;

		if (attachStickiesToReels)
		{
			newSymbol.transform.parent = reel.getReelGameObject().transform;
		}
		else
		{
			newSymbol.transform.parent = stickySymbolsParent.transform;
		}

		if (stickySymbolLayerMap != null && stickySymbolLayerMap.Count > 0)
		{
			setStickySymbolLayer(newSymbol, reelID, position);
		}
		else
		{
			CommonGameObject.setLayerRecursively(newSymbol.gameObject, Layers.ID_SLOT_OVERLAY);
		}
		
		currentStickySymbols.Add(newSymbol);
		yield return null;
	}

	// Set this sticky symbol to the proper layer based on the provided map.
	// 'stickySymbolLayerMap' is an array of ints the represents a row. Each
	// row should be on it's own layer so that the symbols can be masked by
	// the row underneath it.
	//
	// Very useful if you have not standard layer masks setup to hide symbols
	// in independent reel games.
	// Used in marilyn02
	private void setStickySymbolLayer(SlotSymbol stickySymbol, int reelId, int position)
	{
		foreach(SymbolCustomLayerMapItem layerMap in stickySymbolLayerMap)
		{
			if(layerMap.symbolReelPosition == position)
			{
				CommonGameObject.setLayerRecursively(stickySymbol.gameObject, layerMap.layerId);
				return;
			}
		}
		
		// if no layer is set above, put it on default layer
		CommonGameObject.setLayerRecursively(stickySymbol.gameObject, Layers.ID_SLOT_OVERLAY);
	}

	protected IEnumerator playLockingSymbolRevealAnimation(int reelID, int position, string name)
	{
		if (!string.IsNullOrEmpty(STICKY_SYMBOL_LOCKING_SOUND_KEY) && (stickySoundCountPerReelMax < 0 || stickySoundCountPerReel[reelID] < stickySoundCountPerReelMax))
		{
			Audio.playSoundMapOrSoundKey(STICKY_SYMBOL_LOCKING_SOUND_KEY);

			stickySoundCountPerReel[reelID]++;
		}

		SlotSymbol newSymbol = new SlotSymbol(reelGame);
		newSymbol.setupSymbol(name, position, reelGame.engine.getSlotReelAt(reelID, position));
		// We need to set the local position here for independent reel games since engine.getSlotReelAt is returning
		// the actual reel object that is in that position, but the server is sending us a relative position. 
		newSymbol.transform.localPosition = reelGame.engine.getVisibleSymbolsAt(reelID)[position].transform.localPosition;
		CommonGameObject.setLayerRecursively(newSymbol.gameObject, Layers.ID_SLOT_OVERLAY);
		newSymbol.animator.playOutcome(newSymbol);
		yield return new TIWaitForSeconds(lockingSymbolRevealAnimationLength);
		Destroy(newSymbol.gameObject);
	}

	protected virtual IEnumerator playSymbolLandingSound(string symbolName)
	{
		SymbolSoundInfo symbolSoundInfo = getSymbolSoundInfo(symbolName);
		if(symbolSoundInfo != null)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(symbolSoundInfo.landingSounds));
		}
	}

	protected SymbolSoundInfo getSymbolSoundInfo(string symbolName)
	{
		foreach(SymbolSoundInfo soundInfo in symbolLandingSounds)
		{
			if(soundInfo.symbolName == symbolName)
			{
				return soundInfo;
			}
		}

		return null;
	}

	[System.Serializable]
	public class SymbolSoundInfo
	{
		public string symbolName;
		public AudioListController.AudioInformationList landingSounds;
	}

	[System.Serializable]
	public class SymbolCustomLayerMapItem
	{
		public int symbolReelPosition;
		public int layerId;
	}
}


