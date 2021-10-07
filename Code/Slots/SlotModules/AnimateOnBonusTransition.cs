using UnityEngine;
using System.Collections;

/**
Play a set of triggered animations either when the bonus game starts, ends, or both
*/
public class AnimateOnBonusTransition : SlotModule 
{
	[SerializeField] private TransitionPoint transitionPoint;
	[SerializeField] private AnimationListController.AnimationInformationList transitionAnimations;

	enum TransitionPoint
	{
		BonusGameCreated,
		BonusGameEnded, 
		Both
	}

	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return (transitionPoint == TransitionPoint.BonusGameCreated || transitionPoint == TransitionPoint.Both) && transitionAnimations.Count > 0;
	}

	public override IEnumerator executeOnPreBonusGameCreated()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(transitionAnimations));
	}

	public override bool needsToExecuteOnBonusGameEnded()
	{
		return (transitionPoint == TransitionPoint.BonusGameEnded || transitionPoint == TransitionPoint.Both) && transitionAnimations.Count > 0;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(transitionAnimations));
	}
}
