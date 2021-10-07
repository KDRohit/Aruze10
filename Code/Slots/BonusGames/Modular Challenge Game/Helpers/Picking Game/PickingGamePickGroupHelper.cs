using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Utility functions for dealing with specific animations on pick items appearing in groups for matching
 */
public static class PickingGamePickGroupHelper 
{
	// storage class to represent an animation info list linked to a specific group name
	[System.Serializable]
	public class PickGroupAnimationInfoList
	{
		public string groupName;
		public AnimationListController.AnimationInformationList animationInfoList;
		public string animationName;	// if a single animation name is needed (such as for a reveal setting), store here
		public string audioName;		// if a single audio clip name is needed (such as for a reveal setting), store here
		public int hitCount = 1;		// Need to track the hit counts, because some games, like aruze02, have different value hit counts for group reveals, and those have to have unique animations
	}

	// Find an animation info object in the list based on the group name
	public static AnimationListController.AnimationInformationList getAnimationInfoForGroupInList(string groupName, int hitCount, List<PickGroupAnimationInfoList> targetList)
	{
		PickGroupAnimationInfoList matchedList = targetList.Find(groupAnimation => (groupAnimation.groupName == groupName && groupAnimation.hitCount == hitCount));
		if (matchedList != null)
		{
			return matchedList.animationInfoList;
		}
		else
		{
			return null;
		}
	}

	// Return an individual animation key for reveals 
	public static string getAnimationNameForGroupInList(string groupName, int hitCount, List<PickGroupAnimationInfoList> targetList)
	{
		PickGroupAnimationInfoList matchedList = targetList.Find(groupAnimation => (groupAnimation.groupName == groupName && groupAnimation.hitCount == hitCount));
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
	public static string getAudioKeyForGroupInList(string groupName, int hitCount, List<PickGroupAnimationInfoList> targetList)
	{
		PickGroupAnimationInfoList matchedList = targetList.Find(groupAnimation => (groupAnimation.groupName == groupName && groupAnimation.hitCount == hitCount));
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
