using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//This class allows us to animate the robot along with all the other win meter animations
public class Lis01CumulativeBonusModule : CumulativeBonusToPortalTransitionModule
{
	[SerializeField] private bool disableFlyingSymbolOnLanding = false; // if true, don't wait for the reveal animation (gen52)

	[Header("Robot Animations")]
	[SerializeField] private AnimationListController.AnimationInformation winAnimation;
	[SerializeField] private AnimationListController.AnimationInformation celebrateAnimation;
	[SerializeField] private AnimationListController.AnimationInformation idleAnimation;

	protected override IEnumerator playBonusAcquiredAnimations()
	{
		if (celebrateAnimation != null && celebrateAnimation.targetAnimator != null)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(celebrateAnimation));
		}
		yield return StartCoroutine(base.playBonusAcquiredAnimations());		
	}

	public override IEnumerator playCumulativeSymbolAcquiredAnim(Animator cumulativeSymbolAnimator, SlotSymbol symbol)
	{
		// disable the flying symbol before playing the symbol acquired animation.
		if (disableFlyingSymbolOnLanding)
		{
			symbol.gameObject.SetActive(false);
		}

		if (winAnimation != null && winAnimation.targetAnimator != null)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(winAnimation));
		}
		yield return StartCoroutine(base.playCumulativeSymbolAcquiredAnim(cumulativeSymbolAnimator, symbol));
		if (reelGame.outcome == null || !reelGame.outcome.isBonus)
		{
			//Only set back to idle if we didnt find a bonus game if we did let it animate until the transistion
			if (idleAnimation != null && idleAnimation.targetAnimator != null)
			{
				yield return StartCoroutine(AnimationListController.playAnimationInformation(idleAnimation));
			}
		}
	}
}
