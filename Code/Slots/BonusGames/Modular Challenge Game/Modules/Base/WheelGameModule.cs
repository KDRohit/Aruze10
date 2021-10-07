using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Round variant module specific to wheel games
 */
public class WheelGameModule : TICoroutineMonoBehaviour
{
	protected ModularWheelGameVariant wheelRoundVariantParent;
	protected ModularWheel wheelParent;

	protected virtual void OnDestroy()
	{
		if (wheelParent != null)
		{
			wheelParent.removeWheelGameModule(this);
		}
	}

	// executeOnRoundInit() section
	// executes right when a round starts or finishes initing.
	public virtual bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public virtual void executeOnRoundInit(ModularWheelGameVariant round, ModularWheel wheel)
	{
		this.wheelRoundVariantParent = round;
		this.wheelParent = wheel;

		if (round == null)
		{
			Debug.LogError("WheelGameModule.executeOnRoundInit() - round was null, this is needed in order for modules to function!  Destroying this moudle.");
			Destroy(this);
		}

		if (wheel == null)
		{
			Debug.LogError("WheelGameModule.executeOnRoundInit() - wheel was null, this is needed in order for modules to function!  Destroying this moudle.");
			Destroy(this);
		}
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

	// Execute when the wheel is triggered to begin spinning
	public virtual bool needsToExecuteOnSpin()
	{
		return false;
	}

	public virtual IEnumerator executeOnSpin()
	{
		yield break;
	}

	// Execute when the wheel has completed spinning
	public virtual bool needsToExecuteOnSpinComplete()
	{
		return false;
	}

	public virtual IEnumerator executeOnSpinComplete()
	{
		yield break;
	}

	public virtual bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		return false;
	}

	public virtual float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		return -1;
	}

	public virtual bool needsToExecuteOnNumberOfWheelSlicesChanged(int newSize)
	{
		return false;
	}

	public virtual void executeOnNumberOfWheelSlicesChanged(int newSize)
	{
		
	}

// Hook for when you want to change the spin direction of the wheel
// hooks when a spin has been triggered but before anything has been calculated
	public virtual bool needsToExecuteOnOverrideSpinDirection(bool isCurrentSpinDirectionClockwise)
	{
		return false;
	}

	public virtual bool executeOnOverrideSpinDirection(bool isCurrentSpinDirectionClockwise)
	{
		// default is to return the direction we were passed which is the direction
		// that the wheel was intending to go
		// true = clockwise, false = counter-clockwise
		return isCurrentSpinDirectionClockwise;
	}

	protected IEnumerator rollupCredits(
		AnimationListController.AnimationInformationList rollupAnimations,
		AnimationListController.AnimationInformationList rollupFinishedAnimations,
		long startValue,
		long endValue,
		bool isAddingCreditsToPresenterPayout = true)
	{
		long credits = endValue - startValue;

		List<TICoroutine> runningAnimations = new List<TICoroutine>();
		runningAnimations.Add(StartCoroutine(wheelRoundVariantParent.animateScore(startValue, endValue)));
	
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupAnimations, runningAnimations));

		// once the rollup finishes 
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupFinishedAnimations));

		if (isAddingCreditsToPresenterPayout)
		{
			BonusGamePresenter.instance.currentPayout += credits;
		}
	}
}
