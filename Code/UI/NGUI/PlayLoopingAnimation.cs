using UnityEngine;
using System.Collections;

/**
NGUI usage.
Attach to objects that need to animate when moused over, and stop animating when moused out.
*/

public class PlayLoopingAnimation : TICoroutineMonoBehaviour
{
	public Animation targetAnimation;
	
	void OnHover(bool isOver)
	{
		if (targetAnimation == null)
		{
			Debug.LogError("Object " + gameObject.name + " has no animation attached for PlayLoopingAnimation script to use.");
			return;
		}
		
		if (isOver)
		{
			if (targetAnimation.isPlaying)
			{
				// If already playing but paused, start playing at full speed again.
				foreach (AnimationState state in targetAnimation)
				{
					state.speed = 1;
				}
			}
			else
			{	
				targetAnimation.Play();
			}
		}
		else
		{
			// Pause the animation instead of stopping it.
			foreach (AnimationState state in targetAnimation)
			{
				state.speed = 0;
			}
		}
	}
}
