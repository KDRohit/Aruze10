using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WheelGameAnimatedWheelModule : WheelGameModule
{
	/*
	 * Used in wheel game with an animated wheel instead of a traditional wheel object that we manually spin by calculating angles on. 
	 */
	[SerializeField] private List<WheelPickRoundInformation> wheelRevealAnimationInformation = new List<WheelPickRoundInformation>();

	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	public override IEnumerator executeOnSpin()
	{
		for (int i = 0; i < wheelRevealAnimationInformation.Count; i++)
		{
			if (wheelRevealAnimationInformation[i].roundToPlayAnimationsOn.gameParent != null) //Game parent won't be null if this is the round that was initialized
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(wheelRevealAnimationInformation[i].spinButtonPressedAnimations));
			}
		}
	}

	[System.Serializable]
	protected class WheelPickRoundInformation
	{
		public ModularChallengeGameVariant roundToPlayAnimationsOn; //Only plays these animations/sounds for this specific round
		public AnimationListController.AnimationInformationList spinButtonPressedAnimations;
	}
}
