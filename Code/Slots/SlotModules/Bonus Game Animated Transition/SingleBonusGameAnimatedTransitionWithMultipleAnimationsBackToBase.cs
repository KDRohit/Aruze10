using UnityEngine;
using System.Collections;

/*
 * Special child of BonusGameAnimatedTransition
 * Meant to handle situtations where theres only 1 base to bonus animation but multiple animations back to the basegame
 * First used in Batman01
*/

public class SingleBonusGameAnimatedTransitionWithMultipleAnimationsBackToBase : BonusGameAnimatedTransition
{
	[SerializeField] private string backToBaseFromFreespins = "";
	[SerializeField] private string backToBaseFromPickem = "";

	[SerializeField] private Vector3 backToBaseFromPickemStartingPosition;
	[SerializeField] private Vector3 backToBaseFromFreespinsStartingPosition;

	protected override IEnumerator playTransitionBackToBaseGameAnimation()
	{
		if (BonusGameManager.instance.currentGameType == BonusGameType.GIFTING)
		{
			transitionBackStartingPosition = backToBaseFromFreespinsStartingPosition;
			backToBaseGameAnimName = backToBaseFromFreespins;
		}
		else if (BonusGameManager.instance.currentGameType == BonusGameType.CHALLENGE)
		{
			transitionBackStartingPosition = backToBaseFromPickemStartingPosition;
			backToBaseGameAnimName = backToBaseFromPickem;
		}
		yield return StartCoroutine(base.playTransitionBackToBaseGameAnimation());
	}
}
