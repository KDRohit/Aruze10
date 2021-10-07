using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module for turning on and off reel anticipation animations that need to occur during
reevaluation spins.  First used in Aruze04 Goddesses Hera.

Original Author: Scott Lepthien
Creation Date: January 6, 2018
*/
public class RespinReelAnticipationAnimationModule : SlotModule 
{
	[System.Serializable]
	public class ReelAnticipationData
	{
		public int reelIndex;
		public int layer;
		public float reelStopDelay;
		public string reelStopSound;
		public GameObject reelAnticipationObj;
		public AnimationListController.AnimationInformationList playAnimationList;
		public AnimationListController.AnimationInformationList idleAnimationList;
	}

	[SerializeField] private ReelAnticipationData[] reelAnticipationDataList;
	[SerializeField] private string anticipationSoundKey = "";
	// controls if the anticipations all start when the respin starts, or as the previous animations ends
	// NOTE: if this is on ReelAnticipationData should be in the order that the aniticipations should be enabled
	[SerializeField] private bool isPlayingAnticipationsOneAtATime = true; 

	private int currentReelAnticipation = 0;
	private bool isHandlingRespin = false;
	private bool isCancelingAnticipations = false;
	private PlayingAudio anticipationSound = null;

// executeOnReevaluationPreSpin() section
// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
// and triggers before the reels begin spinning
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		isHandlingRespin = true;
		isCancelingAnticipations = false;

		// start the anticipation sound if it is set
		if (!string.IsNullOrEmpty(anticipationSoundKey))
		{
			anticipationSound = Audio.playSoundMapOrSoundKey(anticipationSoundKey);
		}

		yield break;
	}

// executeOnReevaluationSpinStart() section
// functions in this section are accessed by ReelGame.startNextReevaluationSpin()
	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return reelAnticipationDataList != null && reelAnticipationDataList.Length > 0;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{
		isHandlingRespin = true;
		currentReelAnticipation = 0;

		if (!isPlayingAnticipationsOneAtATime)
		{
			// start all the anticipations right now
			for (int i = 0; i < reelAnticipationDataList.Length; i++)
			{
				yield return StartCoroutine(turnOnReelAnticipation(reelAnticipationDataList[i]));
			}
		}
		else
		{
			// start the first anticipation, the others will turn on as the previous one ends
			if (reelAnticipationDataList.Length > 0)
			{
				yield return StartCoroutine(turnOnReelAnticipation(reelAnticipationDataList[0]));
			}
		}

		yield break;
	}

// executeOnReevaluationReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		isHandlingRespin = false;

		if (!isCancelingAnticipations)
		{
			// make sure the final anticipation was turned off if we are doing them one at a time
			if (isPlayingAnticipationsOneAtATime)
			{
				if (reelAnticipationDataList.Length > 0)
				{
					yield return StartCoroutine(turnOffReelAnticipation(reelAnticipationDataList[reelAnticipationDataList.Length - 1]));
				}
			}
		}

		yield break;
	}

// Replace the reel timing value entirely, rather than adding to it
	public override bool shouldReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		// only want to adjust the respin
		if (isHandlingRespin && !isCancelingAnticipations)
		{
			ReelAnticipationData currentReelAnticipationData = getReelAnticipationDataForReels(reelsForStopIndex);

			if (currentReelAnticipationData != null)
			{
				for (int i = 0; i < reelsForStopIndex.Count; i++)
				{
					SlotReel stopReel = reelsForStopIndex[i];

					// Verify that the passed reel is the one for this data to act on
					if (stopReel.reelID - 1 == currentReelAnticipationData.reelIndex && stopReel.layer == currentReelAnticipationData.layer)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public override float getReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		ReelAnticipationData currentReelAnticipationData = getReelAnticipationDataForReels(reelsForStopIndex);
		return currentReelAnticipationData.reelStopDelay;
	}

	// Get the data for the currently playing reel anticipation
	private ReelAnticipationData getReelAnticipationDataForReels(List<SlotReel> reelsForStopIndex)
	{
		for (int i = 0; i < reelsForStopIndex.Count; i++)
		{
			SlotReel stopReel = reelsForStopIndex[i];
			for (int k = 0; k < reelAnticipationDataList.Length; k++)
			{
				ReelAnticipationData anticipationData = reelAnticipationDataList[k];
				if (stopReel.reelID - 1 == anticipationData.reelIndex && stopReel.layer == anticipationData.layer)
				{
					return anticipationData;
				}
			}	
		}

		return null;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return isHandlingRespin && isReelWithAnticipation(stoppedReel);
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		// Make sure that a win is going to happen, otherwise we 
		// are just going to ignore triggering the other anticipations
		// since the player will know when that first reel lands
		// NOTE: Some future game that reuses this module might consider
		// changing how this works if they need the anticipation to function
		// slightly differently when needing to stop being shown due to
		// the anticipation not leading to a win
		bool isNoWinForOutcome = false;
		if (reelGame.currentReevaluationSpin != null)
		{
			int subOutcomeCount = reelGame.currentReevaluationSpin.getSubOutcomesReadOnly().Count;
			if (subOutcomeCount == 0)
			{
				isNoWinForOutcome = true;
			}
		}

		// Check if we've slam stopped, if so we should cancel doing anything further, 
		// and should cancel the animationSound if it is playing
		if ((reelGame.engine.isSlamStopPressed || isNoWinForOutcome) && !isCancelingAnticipations)
		{
			// cancel anticipation sound
			if (anticipationSound != null)
			{
				Audio.stopSound(anticipationSound, 0);
				anticipationSound = null;
			}

			// turn off all of the anticipations
			for (int i = 0; i < reelAnticipationDataList.Length; i++)
			{
				yield return StartCoroutine(turnOffReelAnticipation(reelAnticipationDataList[i]));
			}

			isCancelingAnticipations = true;
		}
		else if (!isCancelingAnticipations)
		{
			// turn off whatever the current anticipation is
			if (currentReelAnticipation < reelAnticipationDataList.Length)
			{
				yield return StartCoroutine(turnOffReelAnticipation(reelAnticipationDataList[currentReelAnticipation]));
			}

			if (currentReelAnticipation + 1 < reelAnticipationDataList.Length)
			{
				currentReelAnticipation++;
			}

			// if we are playing one at a time then we should start the next one
			if (isPlayingAnticipationsOneAtATime)
			{
				if (currentReelAnticipation < reelAnticipationDataList.Length)
				{
					yield return StartCoroutine(turnOnReelAnticipation(reelAnticipationDataList[currentReelAnticipation]));
				}
			}
		}

		yield break;
	}

	// Check if we have anticipation data for the passed reel
	private bool isReelWithAnticipation(SlotReel stoppedReel)
	{
		int reelIndex = stoppedReel.reelID - 1;
		int reelLayer = stoppedReel.layer;
		for (int i = 0; i < reelAnticipationDataList.Length; i++)
		{
			ReelAnticipationData currentReelAnticipationData = reelAnticipationDataList[i];
			if (reelIndex == currentReelAnticipationData.reelIndex && reelLayer == currentReelAnticipationData.layer)
			{
				return true;
			}
		}

		return false;
	}

	// Turn an anticipation on, either by simply turning the object on, or by playing an animation list
	private IEnumerator turnOnReelAnticipation(ReelAnticipationData reelAnticipationData)
	{
		// Override the reelstop sound with the one from reelAnticipationData if it is set
		if (!string.IsNullOrEmpty(reelAnticipationData.reelStopSound))
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelAnticipationData.reelIndex, -1, reelAnticipationData.layer);
			reel.reelStopSoundOverride = reelAnticipationData.reelStopSound;
		}

		// Check for animation first
		if (reelAnticipationData.playAnimationList != null && reelAnticipationData.playAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelAnticipationData.playAnimationList));
		}
		else if (reelAnticipationData.reelAnticipationObj != null)
		{
			// no animations so just try toggling the object on
			reelAnticipationData.reelAnticipationObj.SetActive(true);
		}
		else
		{
			Debug.LogWarning("RespinReelAnticipationAnimationModule.turnOnReelAnticipation() - No anticipation data setup!");
		}
	}

	// Turn an anticipation off, either by simply turning the object off, or by playing an animation list
	private IEnumerator turnOffReelAnticipation(ReelAnticipationData reelAnticipationData)
	{
		// Check for animation first
		if (reelAnticipationData.idleAnimationList != null && reelAnticipationData.idleAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelAnticipationData.idleAnimationList));
		}
		else if (reelAnticipationData.reelAnticipationObj != null)
		{
			// no animations so just try toggling the object on
			reelAnticipationData.reelAnticipationObj.SetActive(false);
		}
		else
		{
			Debug.LogWarning("RespinReelAnticipationAnimationModule.turnOffReelAnticipation() - No anticipation data setup!");
		}
	}
}
