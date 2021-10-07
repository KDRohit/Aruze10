using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

//
// This module will count how many special symbols have landed as the reels stop, and activate
// an animation that represents the current count.
//
// Before each spin, this count resets and the animations are deactivated
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Jan 29th, 2020
// games : billions02
//
public class AnimateSpecialSymbolLandingCountEachReelStopModule : SlotModule
{
	[Tooltip("Special symbols that will trigger an increase in the number of speical symbols landed.")]
	[SerializeField] private List<string> symbolNames;

	[Tooltip("Animations to play as special symbols land.")]
	[SerializeField] private List<CountUpAnimations> countUpAnimations;

	// Keep track of how many special symbols landed so we can animate the correct amount things
	private int totalNumberOfSpecialSymbols;

	// Number Of New Special Symbols Found on the reel so we can animate just that many more.
	private int numberOfNewSpecialSymbolsFound;

	// Make a map of special symbol names for fast lookup on every reel
	private Dictionary<string, bool> specialSymbolMap = new Dictionary<string, bool>();

	// Create a queue of animations to play as special symbols land to activate
	private Queue<AnimationListController.AnimationInformationList> animationInformationListQueue = new Queue<AnimationListController.AnimationInformationList>();

	// The coroutine that animates items from the animationInformationListQueue
	private TICoroutine playAnimationCoroutine;

	private void Start()
	{
		initSpecialSymbolMap();
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		totalNumberOfSpecialSymbols = 0;
		animationInformationListQueue.Clear();
		yield break;
	}

	// Reset things if there are special symbols on the reels
	public override bool needsToExecuteOnPreSpin()
	{
		return totalNumberOfSpecialSymbols > 0;
	}

	// Before the the reels start spinning, make sure all the things are
	// set to their deactivated state.
	public override IEnumerator executeOnPreSpin()
	{
		// When playing the activating animations we activate them in ascending order. Here we are deactivating,
		// so we played them in reverse which is why we are counting backwards.
		for (int i = totalNumberOfSpecialSymbols - 1; i >= 0; i--)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(countUpAnimations[i].deactivateAnimations));
		}

		totalNumberOfSpecialSymbols = 0;
		animationInformationListQueue.Clear();
	}

	// Count how many special symbols are found on this reel so we can tell if we need to execute
	// for this reel. Also keep track of the total number of special symbols here since we have it.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		numberOfNewSpecialSymbolsFound = 0;
		foreach (SlotSymbol slotSymbol in stoppedReel.visibleSymbols)
		{
			if (specialSymbolMap.ContainsKey(slotSymbol.serverName))
			{
				numberOfNewSpecialSymbolsFound++;
			}
		}

		return numberOfNewSpecialSymbolsFound > 0;
	}

	// Play number of animations to match the numSpecialSymbols
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		for (int i = 0; i < numberOfNewSpecialSymbolsFound; i++)
		{
			int nextAnimationIndex = totalNumberOfSpecialSymbols + i;

			if (nextAnimationIndex < countUpAnimations.Count)
			{
				animationInformationListQueue.Enqueue(countUpAnimations[nextAnimationIndex].activateAnimations);
			}
		}

		// Count the total number of special symbols so we know how many extra to animate for the next reel.
		totalNumberOfSpecialSymbols += numberOfNewSpecialSymbolsFound;

		// Clamp the number of special symbols count to the max number of animations.
		totalNumberOfSpecialSymbols = Mathf.Clamp(totalNumberOfSpecialSymbols, 0, countUpAnimations.Count);

		if (playAnimationCoroutine == null || playAnimationCoroutine.isFinished)
		{
			// The playAnimationCoroutine plays until the animationQueue is empty. If the animations are really
			// quick or instant the queue could empty and the coroutine ends. In that case we may need to restart
			// the coroutine to empty the animationQueue again.
			playAnimationCoroutine = StartCoroutine(playAnimationsFromList());
		}

		yield break;
	}

	private IEnumerator playAnimationsFromList()
	{
		while (!animationInformationListQueue.IsEmpty())
		{
			AnimationListController.AnimationInformationList animationInformationList = animationInformationListQueue.Dequeue();
			yield return (StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInformationList)));
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return totalNumberOfSpecialSymbols > 0;
	}

	// Block until our activation animations are complete
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (!animationInformationListQueue.IsEmpty())
		{
			yield return null;
		}
	}

	// Create a map of symbols that increase the count for quick lookup as each reel stops.
	private void initSpecialSymbolMap()
	{
		foreach (string symbolName in symbolNames)
		{
			specialSymbolMap.Add(symbolName, true);
		}
	}

	// Container to specify animations to play for each state of a count up animation
	[System.Serializable]
	public class CountUpAnimations
	{
		[Tooltip("Animations to play when a special symbol lands and activates this.")]
		public AnimationListController.AnimationInformationList activateAnimations;

		[Tooltip("Animations to play when a spin starts and we deactivate this.")]
		public AnimationListController.AnimationInformationList deactivateAnimations;
	}
}

