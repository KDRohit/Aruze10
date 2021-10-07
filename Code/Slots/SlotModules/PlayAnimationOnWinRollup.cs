using UnityEngine;
using System.Collections;

/**
 * PlayAnimationOnWinRollup.cs
 * author: Mike Cabral
 * Plays an arbitrary animation as the game rolls up. This is intended for things like celebration effects on the
 * NGUI layer as seen in harvey01 FreeSpins game.
 */
public class PlayAnimationOnWinRollup : SlotModule 
{
	[SerializeField] private Animator animator;

	public override void Awake()
	{
		if (animator != null)
		{
			base.Awake();
			
			// NOTE: harvey01 has a very simple constantly looping anim, this basic logic may need to be expanded
			animator.gameObject.SetActive(false);
		}
	}
	
	public override bool needsToExecuteOnPaylinesPayoutRollup()
	{
		if (animator != null)
		{
			return true;
		}
		
		return false;
	}
	
	private IEnumerator waitForRollupFinishCorroutine(TICoroutine rollupRoutine)
	{
		while (!rollupRoutine.finished)
		{
			yield return null;
		}

		animator.gameObject.SetActive(false);
	} 

	public override void executeOnPaylinesPayoutRollup(bool winsShown, TICoroutine rollupRoutine)
	{
		animator.gameObject.SetActive(true);

		if (rollupRoutine != null)
		{
			StartCoroutine(waitForRollupFinishCorroutine(rollupRoutine));
		}
	}	
	
}
