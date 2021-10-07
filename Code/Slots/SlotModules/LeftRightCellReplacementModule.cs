using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Takes care of left/right cell replacement games (e.g. pawn01)
public class LeftRightCellReplacementModule : SlotModule
{
	[System.Serializable]
	public class ReplacementMajorSymbolInfo
	{
		public string symbolTemplateName;
		public string landSymbolName;
	}

	[System.Serializable]
	public class ReplacementSymbolInfo
	{
		public string symbolTemplateName;

		[System.NonSerialized] public GameObjectCacher cache;
	}

	[System.Serializable]
	public class ReplacementSymbolInfoPair
	{
		public string outcomeSymbolName;
		public string mutateToSymbolTemplateName;

		public ReplacementSymbolInfo left;
		public ReplacementSymbolInfo right;
	}

	// In some games the symbols underneath the replacements can be rather large and 'peek' out from underneath.
	//  This will hide them when cell replacement happens.
	[SerializeField] private bool shouldHideUnderlyingSymbols = false;
	[SerializeField] private List<ReplacementMajorSymbolInfo> replacementMajors;
	[SerializeField] private List<ReplacementSymbolInfoPair> replacementCellInfoPair;
	[SerializeField] private float timeBetweenMinorMutationEffects = 0.0f;
	[SerializeField] private AudioListController.AudioInformationList symbolInitSoundList;
	[SerializeField] private AudioListController.AudioInformationList symbolActivateSoundList;
	[SerializeField] private AudioListController.AudioInformationList regularWildPopulateSoundList;
	[SerializeField] private AudioListController.AudioInformationList twoTimesWildPopulateSoundList;
	[SerializeField] private string symbolNameForTwoTimesWildPopulateSound = "W2"; // used to figure out if the two times sound should be played instead of the standard sound

	// This list contains the actual major symbols which we are performing various animations on.
	private List<SlotSymbol> animatingMajorSymbols = new List<SlotSymbol>();
	
	// Keeps track of wheather or not we have left-right wilds in this game.
	private bool hasSymbolLeftRightWilds = false;
	
	// Guard to prevent loop in 'needsToExecuteOnReelsStoppedCallback' to run more than once.
	private bool needsToCheckForLeftRightWilds = true;

	private int numberOfSpecificReelStopCoroutinesRunning = 0;

	public override void Awake()
	{
		base.Awake();
		
		// Init the left right symbols from template data.
		for (int i = 0; i < replacementCellInfoPair.Count; i++)
		{
			SymbolInfo leftSymbolInfo = reelGame.findSymbolInfo(replacementCellInfoPair[i].left.symbolTemplateName);
			if (leftSymbolInfo == null)
			{
				Debug.LogError("No symbol template named '" + replacementCellInfoPair[i].left.symbolTemplateName + "'!");
			}
			SymbolInfo rightSymbolInfo = reelGame.findSymbolInfo(replacementCellInfoPair[i].right.symbolTemplateName);
			if (rightSymbolInfo == null)
			{
				Debug.LogError("No symbol template named '" + replacementCellInfoPair[i].right.symbolTemplateName + "'!");
			}
			replacementCellInfoPair[i].left.cache = new GameObjectCacher(this.gameObject, leftSymbolInfo.symbolPrefab);
			replacementCellInfoPair[i].right.cache = new GameObjectCacher(this.gameObject, rightSymbolInfo.symbolPrefab);
		}
	}

	// Search the major replacement symbol list for the replacement info with the given symbol template name.
	private ReplacementMajorSymbolInfo findReplacementMajorSymbolInfo(string symbolTemplateName)
	{
		for (int i = 0; i < replacementMajors.Count; i++)
		{
			// We use 'Contains' since the name at this point may be decorated.
			if (symbolTemplateName.Contains(replacementMajors[i].symbolTemplateName))
			{
				return replacementMajors[i];
			}
		}
		return null;
	}

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}

	private IEnumerator playFinishAnimationAndCleanup()
	{
		// Stop land coroutines, mutate symbols to their landing version, and animate the landing outcome.
		// NOTE: This code assumes the reels are stopping left to right. Other orders may make this not execute correctly.
		//	This shouldn't be a problem for most games, but it may need to be improved should future games require this
		//	support.
		for (int i = 0; i < animatingMajorSymbols.Count; i++)
		{
			// Need to make sure the animation is finished
			while (animatingMajorSymbols[i].isAnimatorDoingSomething)
			{
				yield return null;
			}

			// Remove "_Land" from the name if it is there, since that will cause the lookup to fail
			string serverName = animatingMajorSymbols[i].serverName;
			serverName = serverName.Replace(SlotSymbol.LAND_SYMBOL_POSTFIX, "");

			ReplacementMajorSymbolInfo replacementInfo = findReplacementMajorSymbolInfo(serverName);
			animatingMajorSymbols[i].mutateTo(replacementInfo.symbolTemplateName);
		}
		animatingMajorSymbols.Clear();
		yield break;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		numberOfSpecificReelStopCoroutinesRunning++;

		List<SlotSymbol> symbolList = stoppedReel.symbolList;
		for (int symbolIndex = 0; symbolIndex < symbolList.Count; symbolIndex++)
		{
			if (symbolList[symbolIndex].isTallSymbolPart && symbolList[symbolIndex].isWhollyOnScreen)
			{
				ReplacementMajorSymbolInfo info = findReplacementMajorSymbolInfo(symbolList[symbolIndex].serverName);
				if (info != null)
				{
					// Mutate the symbol to the land symbol
					symbolList[symbolIndex].mutateTo(info.landSymbolName);

					// Store the animating symbol, info, and coroutine
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(symbolInitSoundList));
					yield return StartCoroutine(symbolList[symbolIndex].playAndWaitForAnimateAnticipation());
					animatingMajorSymbols.Add(symbolList[symbolIndex]);
				}
				break;
			}
		}

		numberOfSpecificReelStopCoroutinesRunning--;
		yield break;
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// Do this only once
		if (needsToCheckForLeftRightWilds)
		{
			// Scrub through the mutations and see if it is the right kind of data.
			if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
				reelGame.mutationManager.mutations.Count > 0)
			{
				foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
				{
					if (baseMutation.type == "symbol_left_right_wild")
					{
						StandardMutation mutation = baseMutation as StandardMutation;
						hasSymbolLeftRightWilds = true;
						break;
					}
				}
			}
			needsToCheckForLeftRightWilds = false;
		}
		return hasSymbolLeftRightWilds;
	}

	private ReplacementSymbolInfoPair findPopInfoPair(string outcomeSymbolName)
	{
		for (int i = 0; i < replacementCellInfoPair.Count; i++)
		{
			if (replacementCellInfoPair[i].outcomeSymbolName == outcomeSymbolName)
			{
				return replacementCellInfoPair[i];
			}
		}
		return null;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Wait for the reel stop coroutines to finish animating the anticipations on the tall symbols
		while (numberOfSpecificReelStopCoroutinesRunning > 0)
		{
			yield return null;
		}

		if (reelGame.mutationManager != null && reelGame.mutationManager.mutations != null &&
			reelGame.mutationManager.mutations.Count > 0)
		{
			foreach (MutationBase baseMutation in reelGame.mutationManager.mutations)
			{
				StandardMutation currentMutation = baseMutation as StandardMutation;
				if (currentMutation.type == "symbol_left_right_wild")
				{
					if (currentMutation.twTriggeredSymbolList != null && currentMutation.leftRightWildMutateSymbolList != null)
					{
						for (int cellIndex = 0; cellIndex < currentMutation.twTriggeredSymbolList.Count; cellIndex++)
						{
							foreach (StandardMutation.ReplacementCell item in
								currentMutation.leftRightWildMutateSymbolList[cellIndex])
							{
								int triggerReel = currentMutation.twTriggeredSymbolList[cellIndex].reelIndex;
								int triggerSymbolIndex = currentMutation.twTriggeredSymbolList[cellIndex].symbolIndex - 1;
								SlotSymbol triggerSymbol = reelGame.engine.getVisibleSymbolsBottomUpAt(triggerReel)[triggerSymbolIndex];
								yield return StartCoroutine(AudioListController.playListOfAudioInformation(symbolActivateSoundList));
								if (!triggerSymbol.isAnimatorDoingSomething)
								{
									triggerSymbol.animateOutcome();
								}

								int reel = item.reelIndex;
								int row = item.symbolIndex;
								SlotSymbol symbol = reelGame.engine.getVisibleSymbolsBottomUpAt(reel)[row];
								ReplacementSymbolInfoPair infoPair = findPopInfoPair(item.replaceSymbol);

								AudioListController.AudioInformationList symbolAudioList = regularWildPopulateSoundList;
								if (infoPair.outcomeSymbolName == symbolNameForTwoTimesWildPopulateSound)
								{
									symbolAudioList = twoTimesWildPopulateSoundList;
								} 

								if (triggerReel < reel)
								{
									StartCoroutine(spawnMinorSymbols(symbol, infoPair, spawnRight : true, row: row, symbolSounds: symbolAudioList));
								}
								else
								{
									StartCoroutine(spawnMinorSymbols(symbol, infoPair, spawnRight : false, row: row, symbolSounds: symbolAudioList));
								}
								yield return new TIWaitForSeconds(timeBetweenMinorMutationEffects);
							}
						}
					}
				}
			}
		}
		yield return StartCoroutine(playFinishAnimationAndCleanup());
	}

	private IEnumerator waitForMutatedSymbolAnimationThenRelease(GameObjectCacher cache, SlotSymbol symbol,
		GameObject mutatingSymbol, float animLength, string mutateTo)
	{
		yield return new TIWaitForSeconds(animLength);
		symbol.mutateTo(mutateTo);
		cache.releaseInstance(mutatingSymbol);
	}

	private IEnumerator spawnMinorSymbols(SlotSymbol symbol, ReplacementSymbolInfoPair infoPair, bool spawnRight, int row, AudioListController.AudioInformationList symbolSounds)
	{
		ReplacementSymbolInfo info = spawnRight ? infoPair.right : infoPair.left;
		SymbolInfo mutateInfo = reelGame.findSymbolInfo(info.symbolTemplateName);
		GameObject mutatingSymbol = spawnMutationSymbol(symbol, info.cache, mutateInfo, row);
		mutatingSymbol.SetActive(true);
		if (shouldHideUnderlyingSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		yield return StartCoroutine(AudioListController.playListOfAudioInformation(symbolSounds));

		StartCoroutine (
			waitForMutatedSymbolAnimationThenRelease (
				info.cache,
				symbol,
				mutatingSymbol,
				mutateInfo.customAnimationDurationOverride,
				infoPair.mutateToSymbolTemplateName
			)
		);
	}

	private GameObject spawnMutationSymbol(SlotSymbol symbol, GameObjectCacher mutationObjectCache, SymbolInfo mutateInfo,
		int row)
	{
		GameObject mutatingSymbol = mutationObjectCache.getInstance();
		mutatingSymbol.transform.parent = symbol.reel.getReelGameObject().transform;
		mutatingSymbol.transform.localScale = mutateInfo.scaling;
		mutatingSymbol.transform.localPosition = new Vector3 (
			mutateInfo.positioning.x,
			mutateInfo.positioning.y + (row * reelGame.symbolVerticalSpacingLocal),
			mutatingSymbol.transform.localPosition.z
		);
		return mutatingSymbol;
	}
}
