using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Plays a list of animations when the first pick item is clicked.  Useful if you have something like explanation text which is supposed
 * to hide when the first item is clicked on.
 *
 * Creation Date: 5/10/2021
 * Original Author: Scott Lepthien
 */
public class PickingGamePlayAnimationListOnFirstPickItemClicked : PickingGameModule
{
	[SerializeField] private AnimationListController.AnimationInformationList onFirstPickItemClickedAnimationList; // Animation list which can be played before BonusGamePresenter calls finalCleanup for transitions out of the bonus 

	// executes when the first pick is revealed, happens right before executeOnItemClick()
	// module hook happens on the first reveal
	public override bool needsToExecuteOnFirstPickItemClicked()
	{
		return onFirstPickItemClickedAnimationList != null && onFirstPickItemClickedAnimationList.Count > 0;
	}

	public override IEnumerator executeOnFirstPickItemClicked()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onFirstPickItemClickedAnimationList));
	}
}
