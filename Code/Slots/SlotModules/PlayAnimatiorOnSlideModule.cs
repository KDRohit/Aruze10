using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Lets you play an animation while the game is sliding and stop it after.

Original Author: Leo Schnee
*/
public class PlayAnimatiorOnSlideModule : SlotModule 
{
	[SerializeField] protected Animator slideAnimator;
	[SerializeField] private string SLIDE_START_ANIMATION_NAME;
	[SerializeField] private string SLIDE_STOP_ANIMATION_NAME;

	/// Calls the IEnumerator that will call the game specific sliding functions.
	public override bool needsToExecuteOnReelsSlidingCallback()
	{
		return true;
	}

	/// Handle sliding of the reels
	public override IEnumerator executeOnReelsSlidingCallback()
	{
		// Play animation
		slideAnimator.Play(SLIDE_START_ANIMATION_NAME);
		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsSlidingEnded()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSlidingEnded()
	{
		// Stop the animation
		slideAnimator.Play(SLIDE_STOP_ANIMATION_NAME);
		yield break;
	}
}
