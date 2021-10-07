using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
Module to force the game into portrait mode on device when the game starts and then back into
landscape mode when the game ends.  Should only be used on freespins games since the main
game UI is not made to work in portrait mode.

Original Author: Scott Lepthien
Creation Date: 9/14/2018
*/

public class SwitchGameToPortraitModeOnDeviceModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformationList resolutionChangePortraitStartAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList resolutionChangePortraitSkippableIntroAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList resolutionChangePortraitCompleteAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList resolutionChangeBackToLandscapeStartAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList resolutionChangeBackToLandscapeCompleteAnimations;

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// One of these animations should probably go into a loop that covers the screen until the swap is done
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resolutionChangePortraitStartAnimations));

		// Make sure the animation has started before doing the swap
		yield return null;
		yield return null;
		
		// Trigger the swap, but ignore doing it on WebGL since it is a no-op
#if !UNITY_WEBGL
		ResolutionChangeHandler.switchToPortrait();
#endif

		// Give some frames for the swap to complete before starting the complete animation which will reveal the game
		// this should avoid still showing the game mid shift
		yield return null;
		yield return null;

		// Play the skippable intro animations
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resolutionChangePortraitSkippableIntroAnimations));
		
		// Play the complete animaitons to reveal the game now that it is swapped to portrait
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resolutionChangePortraitCompleteAnimations));
	}

	// Executed via BonusGamePresenter before it call finalCleanup to actually finish and destroy a bonus
	// allows for stuff like playing transition animations after the bonus game is over and all dialogs are closed
	// but before the bonus game is destroyed
	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		// One of these animations should probably go into a loop that covers the screen until the swap is done
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resolutionChangeBackToLandscapeStartAnimations));
		
		// Make sure the animation has started before doing the swap
		yield return null;
		yield return null;
	
		// Trigger the swap, but ignore doing it on WebGL since it is a no-op
#if !UNITY_WEBGL
		ResolutionChangeHandler.switchToLandscape();
#endif

		// Give some frames for the swap to complete before starting the complete animation which will reveal the game
		// this should avoid still showing the game mid shift
		yield return null;
		yield return null;
		
		// Play any animation needed to complete the transition now that the game should be back in landscape
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(resolutionChangeBackToLandscapeCompleteAnimations));
	}
}
