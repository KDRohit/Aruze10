using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Will play an animation as soon as the loading screen is hidden and the game is shown
*/
//[RequireComponent(typeof(GameTransitionAnimationSelector))]
public class GameTransitionAnimationModule : SlotModule 
{
	[SerializeField] public GameObject landscapeGameObject;		//This is a gameobject that will be enabled by default and deactivated after INTRO_BONUS animation has finished in case it exist for showing temporary gameobjects in the animation basically

	public override bool needsToExecuteOnBonusGameCreatedSync()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameCreatedSync()
    {
		//We have a locator position, so use an animation to move towards the bonus game
		/*
		GameTransitionAnimationSelector animSelector = (GameTransitionAnimationSelector)GameTransitionAnimationSelector.instance;
		if (animSelector != null && animSelector.isValid())
		{
			bool isControllerFound = animSelector.SetAnimationController(GameTransitionAnimationType.ENTER_BONUS);

			if(isControllerFound)
				yield return StartCoroutine(ZAnimationUtils.crAnimateAndWait(animSelector.animator,animSelector.currentAnim.endStateName));
		} 
		*/
		yield return null;
	}

	public override bool needsToExecuteOnBonusGameEndedSync()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEndedSync()
	{
		/*
		//Animate out of the bonus game
		GameTransitionAnimationSelector animSel = (GameTransitionAnimationSelector)GameTransitionAnimationSelector.instance;
		if (animSel != null && animSel.isValid ())
		{
			if(animSel.SetAnimationController (GameTransitionAnimationType.EXIT_BONUS))
				yield return StartCoroutine (ZAnimationUtils.crAnimateAndWait (animSel.animator, animSel.currentAnim.endStateName));
		} 
		*/
		yield return null;
	}

	public override bool needsToExecuteAfterLoadingScreenHidden()
	{
		return true;
	}

	public override IEnumerator executeAfterLoadingScreenHidden()
	{
		/*
		GameTransitionAnimationSelector animSelector = (GameTransitionAnimationSelector)GameTransitionAnimationSelector.instance;
		if (animSelector != null && animSelector.isValid())
		{
			if (animSelector.SetAnimationController(GameTransitionAnimationType.INTRO_GAME))
			{
				Overlay.instance.top.gameObject.SetActive(false);

				yield return StartCoroutine(ZAnimationUtils.crAnimateAndWait(animSelector.animator, animSelector.currentAnim.endStateName));

				Overlay.instance.top.gameObject.SetActive(true);
			}
		} 
		*/
		return null;
	#if false
		if (landscapeGameObject != null)
			Destroy(landscapeGameObject);
	#endif
	}
}
