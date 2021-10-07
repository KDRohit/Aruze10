using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Lets you enabled a list of animators while the reels are spinning, and disable them when the reels finish spinning.

Original Author: Chad McKinney
*/
public class ToggleAnimatorsOnSpinningModule : SlotModule 
{
	[SerializeField] protected List<Animator> animators;
	
	protected override void OnEnable()
	{
        stopAnimators();
    }

// executeOnReelsSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
    {
        playAnimators();
        yield break;
	}
	
// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
    {
		stopAnimators();
		yield break;
	}

    protected void playAnimators()
	{
        foreach (Animator animator in animators)
		{
            animator.enabled = true;
        }
    }

    protected void stopAnimators()
	{
		foreach (Animator animator in animators)
		{
            animator.enabled = false;
        }
    }
}
