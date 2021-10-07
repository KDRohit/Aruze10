using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module made to handle playing an animation when OnDoSpecialOnBonusGameEndModule is called after a bonus game returns
to the previous game. This module is needed because it supports playing playing these animations in freespins played
in basegame which PlayAnimationListOnEventModule does not currently do.

Original Author: Nick Saito
Creation Date: May 7th, 2020
games : gen98
*/
public class PlayAnimationListOnDoSpecialOnBonusGameEndModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList onDoSpecialOnBonusGameEndModuleAnimationList;

	// Override awake because we want to support playing only in freespins without destroying this component like the base class does.
	public override void Awake()
	{
		// Normally the base class Awake method will destroy this if we have freeSpinGameRequired enabled, but in our
		// case we want to support using this module in in a game where the freespins are played in the basegame and we
		// only want this module to execute in freespins.

		// Avoid a getcomponent call if a subclass has already assigned reelGame
		reelGame = reelGame ?? GetComponent<ReelGame>();

		if (reelGame == null)
		{
			Debug.LogError("No ReelGame component found for " + this.GetType().Name + " - Destroying script.");
			Destroy(this);
		}
	}

	public override bool needsToExecuteOnDoSpecialOnBonusGameEnd()
	{
		if (freeSpinGameRequired && reelGame.hasFreespinGameStarted)
		{
			return true;
		}

		if (freeSpinGameRequired && !reelGame.hasFreespinGameStarted)
		{
			return false;
		}

		return true;
	}

	public override void executeOnDoSpecialOnBonusGameEnd()
	{
		if (onDoSpecialOnBonusGameEndModuleAnimationList != null && onDoSpecialOnBonusGameEndModuleAnimationList.Count > 0)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(onDoSpecialOnBonusGameEndModuleAnimationList));
		}
	}
}

