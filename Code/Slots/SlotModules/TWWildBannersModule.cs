using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module intended to replace Bride01Freespins class which has been cloned a bunch and hasn't been converted to a module yet
Creates a reel covering banner when a TW symbols lands on a reel

Original Author: Scott Lepthien
Date Created: 1/19/2017
*/
public class TWWildBannersModule : SlotModule 
{
	[System.Serializable]
	protected class ReelAnimationEffects
	{
		public int targetReel = -1;
		public GameObject expandAnimatorObject; // this is the parent object for the expanding animator, which will need to be shut off once the expand animation finishes
		public AnimationListController.AnimationInformationList expandAnimationList;
		public Animator loopedOverlayAnimator;
	}

	[Header("General")]
	[SerializeField] protected List<ReelAnimationEffects> reelAnimationInfo;

	[Header("Symbol Fade and TW Slide")]
	[SerializeField] protected bool isFadingSymbolsAndSlidingTWToBottom = false;
	[SerializeField] protected float TW_SYMBOL_SLIDE_SPEED = 3.25f;
	[SerializeField] protected float SYMBOL_FADE_OUT_TIME = 0.75f;

	private List<int> twBannerActiveOnReelList = new List<int>(); // tells what reels the TW banners are active on, used to determine certain sound changes
	private int numberOfBannersExpanding = 0; // used to track how many banners are expanding, since they play at the same time

	private const string TRIGGER_LAND_SOUND_KEY = "trigger_symbol";
	private const string TRIGGER_SYMBOL = "TW";
	private const string BANNER_COVERED_WILD_REPLACEMENT = "WD"; 

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return twBannerActiveOnReelList.Contains(stoppingReel.reelID);
	}
	
	public override void executeOnSpecificReelStopping(SlotReel stoppingReel)
	{
		// Don't play the stop sound for reels with the TW banners already on them.
		stoppingReel.shouldPlayReelStopSound = false;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return !twBannerActiveOnReelList.Contains(stoppedReel.reelID);
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		for (int i = 0; i < stoppedReel.visibleSymbols.Length; i++)
		{
			SlotSymbol symbol = stoppedReel.visibleSymbols[i];

			if (symbol.serverName == TRIGGER_SYMBOL)
			{
				symbol.animateAnticipation();
				if (Audio.canSoundBeMapped(TRIGGER_LAND_SOUND_KEY))
				{
					Audio.play(Audio.soundMap(TRIGGER_LAND_SOUND_KEY));
				}
				break;
			}
		}

		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// if we have banners already active we need to convert anything under them to TW and if we have a new TW symbol we'll need to expand that out
		return twBannerActiveOnReelList.Count > 0 || doesAnyReelContainTW();
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		numberOfBannersExpanding = 0;

		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotReel reel = reelArray[reelIndex];

			if (twBannerActiveOnReelList.Contains(reel.reelID))
			{
				// this banner has already expanded we need to convert everything under it to WD symbols
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					reel.visibleSymbols[symbolIndex].mutateTo(BANNER_COVERED_WILD_REPLACEMENT, null, false, true);
					// tell the symbols to not animate, incase they would be visible around the edges or have SymbolLayerReorganizer scripts on them
					reel.visibleSymbols[symbolIndex].skipAnimationsThisOutcome();
				}
			}
			else
			{
				for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
				{
					SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

					if (symbol.serverName == TRIGGER_SYMBOL)
					{
						StartCoroutine(expandWildBannerOnReel(reel, numberOfBannersExpanding == 0));
						numberOfBannersExpanding++;
					}
				}
			}
		}

		while (numberOfBannersExpanding > 0)
		{
			// wait for all the banners to finish expanding
			yield return null;
		}
	}

	// Handles the expanding of the wild banner
	private IEnumerator expandWildBannerOnReel(SlotReel reel, bool shouldPlaySounds)
	{
		twBannerActiveOnReelList.Add(reel.reelID);

		Vector3 originalTwSymbolPos = Vector3.zero;
		SlotSymbol twSymbol = null;

		if (isFadingSymbolsAndSlidingTWToBottom)
		{
			// perform symbol fade and TW slide to bottom gated via a flag
			for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

				if (symbol.serverName != TRIGGER_SYMBOL)
				{
					StartCoroutine(symbol.fadeOutSymbolCoroutine(SYMBOL_FADE_OUT_TIME));
				}
				else
				{
					// store the symbol we need to move to the bottom
					twSymbol = symbol;
					originalTwSymbolPos = twSymbol.gameObject.transform.localPosition;
				}
			}

			yield return new TIWaitForSeconds(SYMBOL_FADE_OUT_TIME);

			Vector3 currentPosition = twSymbol.gameObject.transform.localPosition - twSymbol.info.positioning;
			Vector3 targetPosition = reel.getSymbolPositionForSymbolAtIndex(twSymbol.reel.reelData.visibleSymbols - 1, 0, isUsingVisibleSymbolIndex: true, isLocal: true);

			Vector3 positionDelta = targetPosition - currentPosition;
			float timeToMove = Mathf.Abs(positionDelta.y / TW_SYMBOL_SLIDE_SPEED);

			float elapsedTime = 0.0f;
			while (elapsedTime < timeToMove)
			{
				twSymbol.getAnimator().positioning = currentPosition + (elapsedTime / timeToMove) * positionDelta;
				yield return null;
				elapsedTime += Time.deltaTime;
			}

			twSymbol.getAnimator().positioning = targetPosition;
		}

		// find the animation for this reel if it exists
		ReelAnimationEffects effectsForReel = getBannerAnimationEffectsForReel(reel);

		if (effectsForReel != null)
		{
			if (twSymbol != null)
			{
				// temporarily hide the TW symbol if it was slid down so that we don't see two copies during the animation
				twSymbol.gameObject.SetActive(false);
			}

			effectsForReel.expandAnimatorObject.SetActive(true);

			// play the banner reveal
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(effectsForReel.expandAnimationList, null, shouldPlaySounds));

			// if we have a looped animator switch to that and hide the expand object,
			// otherwise we are assuming that the expand animation will have a direct transition 
			// into the looped state in the same animator, so we'll continue to show that
			if (effectsForReel.loopedOverlayAnimator != null)
			{
				effectsForReel.loopedOverlayAnimator.gameObject.SetActive(true);
				effectsForReel.expandAnimatorObject.SetActive(false);
			}
		}
		else
		{
			Debug.LogError("TWWildBannersModule.expandWildBannerOnReel() - No ReelAnimationEffects defined for reel with index = " + (reel.reelID - 1));
		}

		// if we faded and moved the symbols then we should restore them here
		if (isFadingSymbolsAndSlidingTWToBottom)
		{
			for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

				if (symbol.serverName != TRIGGER_SYMBOL)
				{
					// make the symbols visible again now that they are coverd by the banner
					symbol.fadeSymbolInImmediate();
				}
				else
				{
					// put the TW symbol back where it came from
					symbol.gameObject.SetActive(true);
					symbol.gameObject.transform.localPosition = originalTwSymbolPos;
				}
			}
		}

		// convert all the symbols under the banner to WD
		for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
		{
			reel.visibleSymbols[symbolIndex].mutateTo(BANNER_COVERED_WILD_REPLACEMENT, null, false, true);
			// tell the symbols to not animate, incase they would be visible around the edges or have SymbolLayerReorganizer scripts on them
			reel.visibleSymbols[symbolIndex].skipAnimationsThisOutcome();
		}

		numberOfBannersExpanding--;
	}

	// Get the banner effects for the passed in reel
	private ReelAnimationEffects getBannerAnimationEffectsForReel(SlotReel reel)
	{
		// find the animation for this reel if it exists
		for (int i = 0; i < reelAnimationInfo.Count; i++)
		{
			if (reelAnimationInfo[i].targetReel == reel.reelID - 1)
			{
				return reelAnimationInfo[i];
			}
		}

		return null;
	}

	// Tells if any reel has a TW symbol
	private bool doesAnyReelContainTW()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotReel reel = reelArray[reelIndex];

			for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = reel.visibleSymbols[symbolIndex];

				if (symbol.serverName == TRIGGER_SYMBOL)
				{
					return true;
				}
			}
		}

		return false;
	}
}
