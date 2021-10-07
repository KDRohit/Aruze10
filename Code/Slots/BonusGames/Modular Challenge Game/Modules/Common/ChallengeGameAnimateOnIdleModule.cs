using UnityEngine;
using System.Collections;


/**
 * Module to play a specific animator state on a target at an interval
 */
public class ChallengeGameAnimateOnIdleModule : ChallengeGameModule
{

	public Animator targetAnimator;
	public string	IDLE_ANIMATION_NAME = "spin_pickme";
	public float	ANIMATION_INTERVAL = 3.5f;

	public bool	isIdle = true;



	// Enable round start acction
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	// Executes the defined animation on round start
	public override IEnumerator executeOnRoundStart()
	{
		// don't yield, this should run continuously
		StartCoroutine(idleAtInterval());

		yield return null;
	}
		
	private IEnumerator idleAtInterval()
	{
		while (isIdle)
		{
			yield return new TIWaitForSeconds(ANIMATION_INTERVAL);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(targetAnimator, IDLE_ANIMATION_NAME));
		}

		yield return null;
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return true;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		isIdle = false;
		yield return null;
	}
	
}
