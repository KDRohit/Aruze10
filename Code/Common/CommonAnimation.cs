using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


/**
This is a purely static class of generic useful functions that relate to animation.
*/
public static class CommonAnimation
{
	
	// Plays the given animation and waits for it to finish.
	public static IEnumerator playAnimAndWait(Animator animator, string animationName, float startDelay = 0.0f, int stateLayer = 0, bool shouldWaitAFrameForActive = false)
	{
		if (animator != null && !string.IsNullOrEmpty(animationName))
		{
			if (animator.gameObject.activeInHierarchy || shouldWaitAFrameForActive)
			{
				// BY: 03-03-2019
				// Noticed a lot of cases of logging trying to play animations on disabled objects.
				// When I intestigated an issue with max voltage, I noticed this only occurred when returning
				// from some bonus games but not others. The source of it was because SlotBaseGame calls onBonusGameEnded() before basegameFreespinsSummaryClosedCoroutine().
				// onBonusGameEnded() sets up the "token" meter (which plays an animation here), and the other function is what actually enables the meter.
				// I suspect that some of the other logging could have this issue of a race condition.
				// Due to this, I added a boolean for "shouldWaitAFrameForActive" which will give a frame back to see if this resolves so things
				// Lastly, I really wanted to make the default for this "true", but since so many things use it I can't tell what the impact will be.
				// It's entirely possible we have adapted to this slightly broken game state, and trying to add this seemingly innocuous fix makes it worse
				if (shouldWaitAFrameForActive && !animator.gameObject.activeInHierarchy)
				{
					yield return null;
				}

				if (animator.HasState(stateLayer, Animator.StringToHash(animationName)))
				{
					if (startDelay != 0.0f)
					{
						yield return new WaitForSeconds(startDelay);
					}

					// Null check just in case the animator is destroyed while we wait for the start delay.
					if (animator != null)
					{
						animator.Play(animationName, stateLayer);

						// It has to wait one frame before it can get the duration of the animation.			
						yield return null;
						yield return null;  // Sometimes it takes more than one frame for some reason.

						// Additional null check just in case the animator is destroyed while we wait the two frames above.
						// MCC -- Checking if the name that we tried to play matches the name of current animation
						// If they dont match we can assume that the animation has finished already during the two frames.
						if (animator != null && animator.GetCurrentAnimatorStateInfo(stateLayer).IsName(animationName))
						{
							float dur = animator.GetCurrentAnimatorStateInfo(stateLayer).length;
							yield return new WaitForSeconds(dur);
						}
					}
				}
				else
				{
					Debug.LogError("Could not find animation: \"" + animationName + "\" for animator: \"" + animator.name + "\" on layer: " + stateLayer);
				}
			}
			else
			{
				Debug.LogError("Animator was disabled when trying to play animation: \"" + animationName + "\" for animator: \"" + animator.name);
			}
		}
		else
		{
			Debug.LogError("Trying to play animation: \"" + animationName + "\" with a null animator or empty animation name!");
		}
		yield return null;
	}
		
	// Wait for the duration of an animation.
	// This only works if you play the animation right before you start this coroutine.
	// I think it waits the duration + one frame, unfortunately.
	public static IEnumerator waitForAnimDur(Animator animator)
	{
		// It has to wait one frame before it can get the duration of the animation.			
		yield return null;
		yield return null;	// Sometimes it takes more than one frame for some reason.
		
		// Null check just in case the animator is destroyed while we wait the two frames above.
		if (animator != null)
		{
			float dur = animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new WaitForSeconds(dur);
		}
	}
	
	// Play a crossfade and wait for the duration of the transition
	public static IEnumerator crossFadeAnimAndWait(Animator animator, string animationName, float fixedTransitionDuration, float startDelay, float normalizedCrossFadeTransitionTime, int stateLayer = 0)
	{
		if (animator != null && !string.IsNullOrEmpty(animationName))
		{
			if (animator.gameObject.activeInHierarchy)
			{
				if (animator.HasState(stateLayer, Animator.StringToHash(animationName)))
				{
					if (startDelay != 0.0f)
					{
						yield return new WaitForSeconds(startDelay);
					}

					// Null check just in case the animator is destroyed while we wait for the start delay.
					if (animator != null)
					{
						animator.CrossFadeInFixedTime(animationName, fixedTransitionDuration, stateLayer, normalizedCrossFadeTransitionTime);
						yield return new WaitForSeconds(fixedTransitionDuration);
					}
				}
				else
				{
					Debug.LogError("Could not find animation: \"" + animationName + "\" for animator: \"" + animator.name + "\" on layer: " + stateLayer);
				}
			}
			else
			{
				Debug.LogError("Animator was disabled when trying to play animation: \"" + animationName + "\" for animator: \"" + animator.name);
			}		
		}
		else
		{
			Debug.LogError("Trying to play animation: \"" + animationName + "\" with a null animator");
		}
		
		yield break;
	}
	
	/**
	Stop all animations that play on this object and its children
	*/
	public static void stopAllAnimationsOnObject(GameObject obj)
	{
		foreach (Animation animation in obj.GetComponentsInChildren<Animation>(true))
		{
			animation.playAutomatically = false;
			animation.Stop();
		}
	}
	
	/**
	Stop all the animators on this game object and its children 
	from playing (note this is going to disable the script object, 
	and it will need to be re-enabled to function again)
	*/
	public static void stopAllAnimatorsOnObject(GameObject obj)
	{
		foreach (Animator animator in obj.GetComponentsInChildren<Animator>(true))
		{
			animator.StopPlayback();
			animator.enabled = false;
		}
	}
		
	/**
	Loop through all the animation clips in an animator and see if the given stateName exits.
    NOTE: This function is rather slow and should only be used in non-critical code.
	*/
	public static bool doesAnimatorHaveState(Animator animator, string stateName)
	{
		AnimationClip[] animations = animator.runtimeAnimatorController.animationClips;
		if (animations != null)
		{
			for (int i = 0; i < animations.Length; i++)
			{
				if (animations[i].name == stateName)
				{
					return true;
				}
			}
		}
		return false;
	}

	/**
	Spline an object over a set of key points that should already be setup by the spline
	NOTE: Spline::update() should already be called to build the spline before you call this function
	*/
	public delegate void SplineToCompleteDelegate(GameObject spliningObj);
	public static IEnumerator splineTo(Spline spline, float duration, int numSplineFrames, GameObject spliningObj, SplineToCompleteDelegate completeCallback = null)
	{
		float elapsedTime = 0.0f;
		
		while (spliningObj != null && elapsedTime <= duration)
		{
			spliningObj.transform.position = spline.getValue(numSplineFrames * (elapsedTime/duration));
			yield return null;
			elapsedTime += Time.deltaTime;
		}
		
		if (spliningObj != null)
		{
			spliningObj.transform.position = spline.getValue(spline.lastFrame);
		
			if (completeCallback != null)
			{
				completeCallback(spliningObj);
			}
		}
	}
}
