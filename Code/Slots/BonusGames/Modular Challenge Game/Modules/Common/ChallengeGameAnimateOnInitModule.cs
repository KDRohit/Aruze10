using UnityEngine;
using System.Collections;


/**
 * Module to play a specific animator state on a target on round initialization
 * NOTE: this animation cannot yield due to a lack of coroutine on the round init call
 */
public class ChallengeGameAnimateOnInitModule : ChallengeGameModule
{
	public Animator targetAnimator;
	public string	ANIMATION_NAME = "intro";
	
	// Enable round init action
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
		
	// Executes the defined animation on round init
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		targetAnimator.Play(ANIMATION_NAME);
		base.executeOnRoundInit(round);
	}
}
