using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module made to handle playing an animation when BonusGamePresenter is about to call finalCleanup and destroy the bonus game
these animations will play after the dialogs like bonus summary are closed.

Original Author: Scott Lepthien
Creation Date: August 24, 2017
*/
public class ChallengeGamePlayAnimationListOnBonusGamePresenterFinalCleanupModule : ChallengeGameModule 
{
	[SerializeField] private AnimationListController.AnimationInformationList onBonusGamePresenterFinalCleanupAnimationList; // Animation list which can be played before BonusGamePresenter calls finalCleanup for transitions out of the bonus 
	[SerializeField] private List<GameObject> listOfObjectsToDeactivateBeforeAnims = new List<GameObject>();

// Executed via BonusGamePresenter before it call finalCleanup to actually finish and destroy a bonus
// allows for stuff like playing transition animations after the bonus game is over and all dialogs are closed
// but before the bonus game is destroyed
	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		for (int i = 0; i < listOfObjectsToDeactivateBeforeAnims.Count; i++)
		{
			listOfObjectsToDeactivateBeforeAnims[i].SetActive(false);
		}

		if (onBonusGamePresenterFinalCleanupAnimationList != null && onBonusGamePresenterFinalCleanupAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onBonusGamePresenterFinalCleanupAnimationList));
		}
		else
		{
			yield break;
		}
	}
}
