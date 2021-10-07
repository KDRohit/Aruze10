using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dynamically attaches the bottom of the Slingo bonus game object to the top
// of the Slingo base game object. Then, manually tweens the two game objects
// to their proper positions for the bonus game. 
// This is needed because when the base game dynamically scales to UI, we can't  
// have one set target y-value (like in an Animation) 
// to move the base game down to in order to be completely hidden after the transition. 
 
// Author: Jake Smith
// Date: 7/19/18

public class SlingoBonusGameTransitionModule : BonusGameAnimatedTransition
{
	[SerializeField] private GameObject freeSpinsRoot;
	[SerializeField] private GameObject slider;
	[SerializeField] private MeshRenderer freeSpinsBackground;
	[SerializeField] private MeshRenderer baseGameBackground;
	[SerializeField] private Camera backgroundCamera;
	[SerializeField] private float transitionDelay = 1.5f; //How long to wait until we do our manual tweening
	[SerializeField] private float transitionTime = 4.016f; //How long we want our transition tween to last


	private float originalSliderPosition;
	private float originalTopFreeSpinsPosition;
	private bool hasBeenAttached; //saves us from reattaching the objects if the bonus game gets activated 2+ times

	protected override IEnumerator doTransition()
	{
		if (!hasBeenAttached)
		{
			//First, dynamically attach the bottom of bonus game to top of base game after base game has scaled	
			//Calculate the vector between the top of camera and bottom of bonus game background
			float topOfBaseGame = baseGameBackground.bounds.max.y;
			float travel = topOfBaseGame - (freeSpinsRoot.transform.position.y - freeSpinsBackground.bounds.size.y/2);
			//Move the free spins root down this distance between the top of camera and bottom of freespins background
			CommonTransform.addY(freeSpinsRoot.transform, travel);
			
			//Store the original positions for calculating distances later
			originalSliderPosition = slider.transform.position.y;
			originalTopFreeSpinsPosition = freeSpinsBackground.bounds.max.y;
			hasBeenAttached = true;
		}
		
		//The bonus game is now attached to the base game. Start the transition
		StartCoroutine(base.doTransition());
		yield return new WaitForSeconds(transitionDelay);
		iTween.ValueTo(this.gameObject, 
						iTween.Hash("from", originalTopFreeSpinsPosition, 
									"to", backgroundCamera.ViewportToWorldPoint(backgroundCamera.rect.max).y, 
									"time", transitionTime,
									"easeType", iTween.EaseType.easeInOutSine, 
									"onupdate", "slideBackgrounds", 
									"oncomplete", "onBackgroundSlideComplete"));
		while (!isTransitionComplete)
		{
			yield return null;
		}
	}

	public void slideBackgrounds(float distance)
	{
		//Move the slider down however much the top of free spins would have to move down to hit the top of the camera
		CommonTransform.setY(slider.transform, originalSliderPosition - (originalTopFreeSpinsPosition - distance));
	}

	public void onBackgroundSlideComplete()
	{
		isTransitionComplete = true;
	}
}
