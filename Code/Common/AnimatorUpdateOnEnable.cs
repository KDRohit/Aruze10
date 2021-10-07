using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Hacky fix to control Unity Animator and force it to update when enabled, this should fix issues
where the Animator has some elements stuck in the last frame it was in before the GameObject it
is in was disabled for a frame before the Animator actually seems to update and correct itself.

Original Author: Scott Lepthien
Creation Date: 4/10/2018
*/
public class AnimatorUpdateOnEnable : MonoBehaviour 
{
	private Animator animator = null;
	// Used to fix a bug where symbol flattening was having trouble reading 
	// the animator changes if we Update after creating the object.  Technically
	// the bug this is intended to fix can't really happen on first enable so
	// this should be fine.
	private bool isFirstEnableFromCreation = true; 

	private void Awake()
	{
		animator = gameObject.GetComponent<Animator>();
	}

	// When enabled force the animator to update to ensure that
	// all animated parts are restored to where they should be
	// for the animation that should be starting
	private void OnEnable() 
	{
		if (!isFirstEnableFromCreation)
		{
			if (animator != null)
			{
				animator.Update(0.0f);
			}
		}
		else
		{
			isFirstEnableFromCreation = false;
		}
	}
}
