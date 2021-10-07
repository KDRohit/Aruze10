using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module to handle the mega symbol anticipation animation for Aruze04

Original Author: Scott Lepthien
Creation Date: January 7, 2018
*/
public class Aruze04MegaSymbolAnticipationModule : SlotModule 
{
	[System.Serializable]
	public class SymbolAnticipationData
	{
		public string symbolName;
		public float reelStopDelay;
		public string reelStopSoundKey;
		public string anticipateSoundKey;
		public GameObject reelAnticipationObj;
		public AnimationListController.AnimationInformationList playAnimationList;
		public AnimationListController.AnimationInformationList idleAnimationList;
	}

	[System.Serializable]
	public class ReplacementToSymbolAnticipationData
	{
		public string megaReplacementSymbolName;
		public SymbolAnticipationData[] symbolsAnticipateDataList;
	}

	[SerializeField] private ReplacementToSymbolAnticipationData[] replacementSymbolToAnticipationDataList;

	private bool isShowingAnticipation = false;
	private SymbolAnticipationData currentAnticipateData = null;
	private PlayingAudio anticipationSound = null;
	private bool wasAnticipationShownThisSpin = false;
	private bool isCurrentAnticipateDataCachedForSpin = false;

// executeOnPreSpinNoCoroutine() section
// Functions here are executed during the startSpinCoroutine but do not spawn a coroutine
	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		wasAnticipationShownThisSpin = false;
		isCurrentAnticipateDataCachedForSpin = false;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return isShowingAnticipation;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (currentAnticipateData != null)
		{
			yield return StartCoroutine(turnOffReelAnticipation(currentAnticipateData));
			currentAnticipateData = null;
		}
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return reelGame.currentReevaluationSpin == null && shouldAnticipate();
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (isShowingAnticipation && isReelWithAnticipation(stoppedReel))
		{
			if (reelGame.engine.isSlamStopPressed)
			{
				// We've slam stopped, so we should cancel doing anything further, 
				// and should cancel the animationSound if it is playing
				// cancel anticipation sound
				if (anticipationSound != null)
				{
					Audio.stopSound(anticipationSound, 0);
					anticipationSound = null;
				}
			}

			// turn off the anticipation
			if (currentAnticipateData != null)
			{
				yield return StartCoroutine(turnOffReelAnticipation(currentAnticipateData));
				currentAnticipateData = null;
			}
		}
		else if (stoppedReel.reelID == 1 && stoppedReel.layer == 1)
		{
			// this is the first reel stopping, so we should turn on the anticipation now
			SymbolAnticipationData symbolAnticipateData = getSymbolAnticipationDataForCurrentMegaSymbol();
			if (symbolAnticipateData != null)
			{
				// start the anticipation sound if it is set
				if (!string.IsNullOrEmpty(symbolAnticipateData.anticipateSoundKey))
				{
					anticipationSound = Audio.playSoundMapOrSoundKey(symbolAnticipateData.anticipateSoundKey);
				}

				yield return StartCoroutine(turnOnReelAnticipation(symbolAnticipateData));
			}
		}

		yield break;
	}

	// Check if this is the mega reel in aruze04
	private bool isReelWithAnticipation(SlotReel stoppedReel)
	{
		int reelIndex = stoppedReel.reelID - 1;
		int reelLayer = stoppedReel.layer;
		if (reelIndex == 1 && reelLayer == 1)
		{
			return true;
		}

		return false;
	}

// Replace the reel timing value entirely, rather than adding to it
	public override bool shouldReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		// only want to adjust when not doing reevals
		if (reelGame.currentReevaluationSpin == null && shouldAnticipate())
		{
			for (int i = 0; i < reelsForStopIndex.Count; i++)
			{
				SlotReel stopReel = reelsForStopIndex[i];

				// Verify that one of the stopping reels is the mega reel for aruze04
				if (stopReel.reelID - 1 == 1 && stopReel.layer == 1)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override float getReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		SymbolAnticipationData currentReelAnticipationData = getSymbolAnticipationDataForCurrentMegaSymbol();
		return currentReelAnticipationData.reelStopDelay;
	}

	// Get the SymbolAnticipationData for the symbol which should be anticipating
	// returns null if it can't find the data
	private SymbolAnticipationData getSymbolAnticipationDataForCurrentMegaSymbol()
	{
		if (!isCurrentAnticipateDataCachedForSpin)
		{
			Dictionary<string, string> megaReplacementSymbolMap = getMegaReplacementSymbolMapForLayer(1);

			for (int i = 0; i < replacementSymbolToAnticipationDataList.Length; i++)
			{
				ReplacementToSymbolAnticipationData replacementAnticipateData = replacementSymbolToAnticipationDataList[i];

				if (megaReplacementSymbolMap.ContainsKey(replacementAnticipateData.megaReplacementSymbolName))
				{
					string megaReplacementSymbolName = megaReplacementSymbolMap[replacementAnticipateData.megaReplacementSymbolName];
					for (int k = 0; k < replacementAnticipateData.symbolsAnticipateDataList.Length; k++)
					{
						SymbolAnticipationData currentSymbolData = replacementAnticipateData.symbolsAnticipateDataList[k];
						if (currentSymbolData.symbolName == megaReplacementSymbolName)
						{
							currentAnticipateData = currentSymbolData;
							isCurrentAnticipateDataCachedForSpin = true;
							return currentSymbolData;
						}
					}
				}
			}

			// no anticipation data found, so we will just set it null and say that it is cached
			isCurrentAnticipateDataCachedForSpin = true;
			currentAnticipateData = null;
		}

		return currentAnticipateData;
	}

	// Get the mega symbol replacement map
	private Dictionary<string, string> getMegaReplacementSymbolMapForLayer(int layer)
	{
		JSON[] mutationInfo = reelGame.outcome.getMutations();

		Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();

		// Check all mutations for possible replace symbols, will just use the data in the first mutation
		// we find, since so far we haven't had a case where this info is spread across multiple mutations
		for (int i = 0; i < mutationInfo.Length; i++)
		{
			JSON info = mutationInfo[i];
			JSON replaceData = info.getJSON("replace_symbols");

			if (replaceData != null)
			{
				foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
				{
					megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
				}
			}
		}

		return megaReplacementSymbolMap;
	}

	// Check symbolAnticipationDataList against what is set for the symbols to see if we need to enable the anticipation
	private bool shouldAnticipate()
	{
		// check if the anticipation was already shown this spin
		if (wasAnticipationShownThisSpin && !isShowingAnticipation)
		{
			return false;
		}

		SymbolAnticipationData symbolAnticipateData = getSymbolAnticipationDataForCurrentMegaSymbol();

		if (symbolAnticipateData != null)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Turn an anticipation on, either by simply turning the object on, or by playing an animation list
	private IEnumerator turnOnReelAnticipation(SymbolAnticipationData anticipationData)
	{
		wasAnticipationShownThisSpin = true;

		// Override the reelstop sound with the one from reelAnticipationData if it is set
		if (!string.IsNullOrEmpty(anticipationData.reelStopSoundKey))
		{
			// Get second reel on the top layer, since this module is specifically for Aruze04
			SlotReel reel = reelGame.engine.getSlotReelAt(1, -1, 1);

			// Check if we will actually be triggering something, if not we should just leave the
			// reel stop sound as normal.
			if (isTriggeringBonusOrFeatureThisSpin())
			{
				reel.reelStopSoundOverride = anticipationData.reelStopSoundKey;
			}
		}

		// Check for animation first
		if (anticipationData.playAnimationList != null && anticipationData.playAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(anticipationData.playAnimationList));
		}
		else if (anticipationData.reelAnticipationObj != null)
		{
			// no animations so just try toggling the object on
			anticipationData.reelAnticipationObj.SetActive(true);
		}
		else
		{
			Debug.LogWarning("Aruze04MegaSymbolAnticipationModule.turnOnReelAnticipation() - No anticipation data setup!");
		}

		isShowingAnticipation = true;
	}

	// Turn an anticipation off, either by simply turning the object off, or by playing an animation list
	private IEnumerator turnOffReelAnticipation(SymbolAnticipationData anticipationData)
	{
		// Check for animation first
		if (anticipationData.idleAnimationList != null && anticipationData.idleAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(anticipationData.idleAnimationList));
		}
		else if (anticipationData.reelAnticipationObj != null)
		{
			// no animations so just try toggling the object on
			anticipationData.reelAnticipationObj.SetActive(false);
		}
		else
		{
			Debug.LogWarning("Aruze04MegaSymbolAnticipationModule.turnOffReelAnticipation() - No anticipation data setup!");
		}

		isShowingAnticipation = false;
	}

	// Tell if this spin is actually triggering something, in which case the reel stop sounds need to use the overrride
	private bool isTriggeringBonusOrFeatureThisSpin()
	{
		if (reelGame.outcome.isBonus || reelGame.reevaluationSpins.Count > 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
