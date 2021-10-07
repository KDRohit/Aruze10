using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Common functions for all manner of picking games
 */
public class ChallengeGameModule : TICoroutineMonoBehaviour
{
	protected ModularChallengeGameVariant roundVariantParent;
	
	protected virtual void OnDestroy()
	{
		if (roundVariantParent != null)
		{
			roundVariantParent.removeChallengeGameModule(this);
		}
	}

	public virtual void Awake()
	{
		if (GetComponent<ModularChallengeGameVariant>() == null)
		{
			Debug.LogError("No ModularChallengeGameVariant component found for " + this.GetType().Name + " - Destroying script.");
			Destroy(this);
		}
	}

	// executeOnRoundInit() section
	// executes right when a round starts or finishes initing.
	public virtual bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public virtual void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		this.roundVariantParent = round;
	}

	// executeOnRoundStarted() section
	// executes right when a round starts or finishes initing.
	public virtual bool needsToExecuteOnRoundStart()
	{
		return false;
	}

	public virtual IEnumerator executeOnRoundStart()
	{
		yield break;
	}
	
	// executeOnRoundEnd() section
	// executes right when a round starts or finishes initing.
	public virtual bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return false;
	}

	public virtual IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		yield break;	
	}

	// executeCopyDataFromModulesOfPrevRound() section
	// exectues when ModularChallengeGame.advanceRound() is called with a valid next round.  
	// This function allows the copying of data that needs to persist between rounds for a type of module.
	public virtual bool needsToExecuteCopyDataFromModulesOfPrevRound()
	{
		return false;
	}

	public virtual void executeCopyDataFromModulesOfPrevRound(List<ChallengeGameModule> modulesToCopyFrom)
	{
		// Fill this with code handling what needs to be copied from one round's modules to the next round's
	}
		
	// executeOnShowCustomWings() section
	public virtual bool needsToExecuteOnShowCustomWings()
	{
		return false;
	}

	public virtual IEnumerator executeOnShowCustomWings()
	{
		yield break;
	}

	// executeOnBonusGamePresenterFinalCleanup() section
	// Triggers when BonusGamePresenter is about to call finalCleanup to fully terminate and
	// cleanup.  Can be used to do stuff like play an outro transition.
	public virtual bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return false;
	}

	public virtual IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		yield break;
	}

	public virtual bool needsToShowCustomBonusSummaryDialog()
	{
		return false;
	}

	public virtual void createCustomSummaryScreenDialog(GenericDelegate answerDelegate)
	{
		
	}

	protected IEnumerator rollupCredits(
		AnimationListController.AnimationInformationList rollupAnimations,
		AnimationListController.AnimationInformationList rollupFinishedAnimations,
		long startValue,
		long endValue,
		bool addCredits = true,
		string rollupSoundLoopOverride = null,
		string rollupSoundEndOverride = null)
	{
		long credits = endValue - startValue;

		List<TICoroutine> runningAnimations = new List<TICoroutine>();
		runningAnimations.Add(StartCoroutine(roundVariantParent.animateScore(startValue, endValue, rollupSoundLoopOverride, rollupSoundEndOverride)));
	
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupAnimations, runningAnimations));

		// once the rollup finishes 
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupFinishedAnimations));

		if (addCredits)
		{
			BonusGamePresenter.instance.currentPayout += credits;
		}
	}
}
