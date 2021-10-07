using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Author: Edgar Arriaga
/// This module handles animation triggering for reel wobbling effect
public class ReelWobbleModule : SlotModule 
{
	[Tooltip("Specify each reel animator in left to right order.")]
	public List<Animator> ReelWobbleAnimators; //We need to have one animator per reel if every reel will have a wobble

	public override bool needsToExecuteOnSpinEnding(SlotReel reel)
	{
		return true;
	}
		
	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		
		if (ReelWobbleAnimators.Count >= stoppedReel.reelID) 
		{
			//Trigger animations if they exist
			Animator anim = ReelWobbleAnimators [stoppedReel.reelID - 1];

			anim.speed = 0;
		}
	}

	public override bool needsToExecuteOnReelsSpinning ()
	{
		return true;
	}
   
	public override IEnumerator executeOnReelsSpinning()
	{
		foreach(Animator anim in ReelWobbleAnimators)
		{
			if (!anim.enabled) 
			{
				anim.enabled = true; //By default animator is disabled, so enable if thats the case
				anim.Play (anim.GetCurrentAnimatorStateInfo (0).shortNameHash);
			}

			anim.speed = 1;
		}
		return base.executeOnReelsSpinning ();
	}
}
