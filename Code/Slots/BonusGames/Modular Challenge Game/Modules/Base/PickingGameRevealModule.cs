using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Base module class that represents a picking module that performs a reveal
 */
public class PickingGameRevealModule : PickingGameModule
{
	[SerializeField] protected AnimationListController.AnimationInformationList ambientAnimationInformationOnReveal;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupFinishedAnimations;

	// Override per-module for a common place to test whether an outcome should be handled based on its properties
	protected virtual bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return false;
	}

	// Execute a rollup with optional animations on elements
	protected IEnumerator rollupCredits(long startValue, long endValue, bool addCredits = true)
	{
		yield return StartCoroutine(rollupCredits(rollupAnimations, rollupFinishedAnimations, startValue, endValue, addCredits));
	}

	// Execute a rollup with optional animations on elements
	protected IEnumerator rollupCredits(long credits, bool addCredits = true)
	{
		yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + credits));
	}

	// executes when a player clicks / taps on an item
	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return shouldHandleOutcomeEntry(pickData);
	}

	// this function can be called when you want to do a standard reveal animation
	protected IEnumerator executeBasicOnRevealPick(PickingGameBasePickItem pickItem)
	{
		// at a base level, just reveal the item.
		if (needsToExecuteOnRevealPick())
		{
			yield return StartCoroutine(executeOnRevealPick(pickItem));
		}
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(executeBasicOnRevealPick(pickItem));
	}
		
	// executes when an item is selected by the player
	public virtual bool needsToExecuteOnRevealPick()
	{
		return true;
	}

	public virtual IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		List<TICoroutine> runningAnimations = new List<TICoroutine>();
		runningAnimations.Add(StartCoroutine(pickItem.revealPick(currentPick)));
		yield return StartCoroutine(playAmbientInformationOnReveal(runningAnimations));
	}

	protected virtual IEnumerator playAmbientInformationOnReveal(List<TICoroutine> runningAnimations)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationInformationOnReveal, runningAnimations));
	}

	// executes when the round is completing, revealing picks that were not chosen
	public virtual bool needsToExecuteOnRevealLeftover(ModularChallengeGameOutcomeEntry pickData)
	{
		return shouldHandleOutcomeEntry(pickData);
	}

	public virtual IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		yield return StartCoroutine(leftover.revealLeftover(leftoverOutcome));
	}

	// executes when the round is completing, revealing picks that were not chosen
	public virtual bool needsToExecuteOnRevealRoundEnd()
	{
		return false;
	}

	public virtual IEnumerator executeOnRevealRoundEnd(List<PickingGameBasePickItem> leftovers)
	{
		yield break;
	}

}
