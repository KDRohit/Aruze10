using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Use this module in conjunction with BonusGameToBonusGameChallengeGameModule if this bonus
 * can launch into another bonus and you want to have different outro animations depending on
 * what bonus is launched (or a fallback if no bonus is launched).
 * NOTE: This module should go above BonusGameToBonusGameChallengeGameModule so that the animations
 * happen before that module activates the bonus game swap.
 * 
 * Creation Date: 4/2/2021
 * Original Author: Scott Lepthien
 */
public class ChallengeGameAnimateOnRoundEndBasedOnNextBonusModule : ChallengeGameModule
{
	[System.Serializable]
	public class AnimationListForBonusGameNamesData
	{
		[SerializeField] public AnimationListController.AnimationInformationList animations;
		[Tooltip("Include every bonus name that will use these animations (that may include the names of cheat versions of the bonus).  Supports using \"{0}\" in the name to auto include the game key.")]
		[SerializeField] public string[] bonusNames;
	}

	[SerializeField] private AnimationListForBonusGameNamesData[] animationsByBonusGameNames;
	[Tooltip("These animation will play if no additional bonus is triggered.  Leave this list empty if you don't want any animations to play when no additional bonus is won.")]
	[SerializeField] private AnimationListController.AnimationInformationList noBonusTriggeredAnimationList;
	
	private Dictionary<string, AnimationListForBonusGameNamesData> bonusNameToAnimationsDictionary = new Dictionary<string,AnimationListForBonusGameNamesData>();
	
	public override void Awake()
	{
		base.Awake();

		foreach (AnimationListForBonusGameNamesData animData in animationsByBonusGameNames)
		{
			foreach (string bonusName in animData.bonusNames)
			{
				// Attempt to append the game keyname to the string if it contains {0}
				string formattedString = string.Format(bonusName, GameState.game.keyName);
			
				if (bonusNameToAnimationsDictionary.ContainsKey(formattedString))
				{
					Debug.LogError($"ChallengeGameAnimateOnRoundEndBasedOnNextBonusModule.Awake() - bonusNameToAnimationsDictionary found a duplicate key for bonusName = {formattedString} ignoring the duplicate.");
				}
				else
				{
					bonusNameToAnimationsDictionary.Add(formattedString, animData);
				}
			}
		}
	}
	
	// Enable round end action
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return true;
	}
	
	// Executes the defined animation on round end
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		SlotOutcome bonusGameOutcome = roundVariantParent.getMostRecentOutcomeEntry()?.nestedBonusOutcome;
		if (bonusGameOutcome != null)
		{
			string bonusGameName = bonusGameOutcome.getBonusGame();
			if (bonusNameToAnimationsDictionary.TryGetValue(bonusGameName, out AnimationListForBonusGameNamesData animData))
			{
				if (animData.animations.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animData.animations));
				}
			}
		}
		else
		{
			// There isn't another bonus to launch, so try playing the fallback animations if those are defined
			if (noBonusTriggeredAnimationList.Count > 0)
			{
				yield return  StartCoroutine(AnimationListController.playListOfAnimationInformation(noBonusTriggeredAnimationList));
			}
		}
	}
}
