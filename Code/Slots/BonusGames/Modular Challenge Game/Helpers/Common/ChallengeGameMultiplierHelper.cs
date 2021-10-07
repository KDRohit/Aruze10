using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Utility functions for dealing with specific multiplier to animation or audio assignments
 */
public static class ChallengeGameMultiplierHelper 
{
	// storage class to represent an animation info list linked to a specific multiplier
	[System.Serializable]
	public class MultiplierAnimationInfoList
	{
		public int multiplierValue;
		public AnimationListController.AnimationInformationList animationInfoList;
		public string animationName;	// if a single animation name is needed (such as for a reveal setting), store here
		public string audioName;		// if a single audio clip name is needed (such as for a reveal setting), store here
	}
		
	// Find an animation info object in the list based on the multiplier
	public static AnimationListController.AnimationInformationList getAnimationInfoForMultiplierInList(int multiplier, List<MultiplierAnimationInfoList> targetList)
	{
		return targetList.Find(multiplierAnimation => (multiplierAnimation.multiplierValue == multiplier)).animationInfoList;
	}

	// Return an individual animation key for reveals 
	public static string getAnimationNameForMultiplierInList(int multiplier, List<MultiplierAnimationInfoList> targetList)
	{
		MultiplierAnimationInfoList matchedList = targetList.Find(multiplierAnimation => (multiplierAnimation.multiplierValue == multiplier));
		if (matchedList != null)
		{
			return matchedList.animationName;
		}
		else
		{
			return null;
		}
	}

	// Return an individual audio key for reveals 
	public static string getAudioKeyForMultiplierInList(int multiplier, List<MultiplierAnimationInfoList> targetList)
	{
		MultiplierAnimationInfoList matchedList = targetList.Find(multiplierAudio => (multiplierAudio.multiplierValue == multiplier));
		if (matchedList != null)
		{
			return matchedList.audioName;
		}
		else
		{
			return null;
		}
	}
}
