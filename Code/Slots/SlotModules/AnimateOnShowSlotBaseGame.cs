using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Plays animations when the base game is shown after being hidden. Things that can hide a base game are big wins or feature dialogs (eg. Partner Power-Up).
 */

public class AnimateOnShowSlotBaseGame : SlotModule 
{
	[SerializeField] private AnimationListController.AnimationInformationList showGameAnimations;

	public override bool needsToExecuteOnShowSlotBaseGame ()
	{
		return showGameAnimations.Count > 0;
	}

	public override void executeOnShowSlotBaseGame ()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(showGameAnimations)); 
	}
}
