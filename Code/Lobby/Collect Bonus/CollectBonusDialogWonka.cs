using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

/*
Handles Wonka override of collect bonus dialog.
*/

public class CollectBonusDialogWonka : CollectBonusDialog
{
	private const float OOMPA_LOOMPA_ANIM_DELAY = 2.25f;
	
	public Animator oompaLoompaAnim;
	public Animator coinsAnim;
	
	public override void init()
	{
		base.init();
		oompaLoompaAnim.gameObject.SetActive(false);
	}

	
	protected override IEnumerator playAnimations()
	{
		StartCoroutine(base.playAnimations());
		
		SkippableWait wait = new SkippableWait();
		
		yield return StartCoroutine(wait.wait(OOMPA_LOOMPA_ANIM_DELAY));
		
		if (didSkipAnimation())
		{
			yield break;
		}
		
		oompaLoompaAnim.gameObject.SetActive(true);
		oompaLoompaAnim.Play("intro");
	}

	protected override bool didSkipAnimation()
	{
		if (!base.didSkipAnimation())
		{
			return false;
		}
		
		// Skipping animation. Jump to the idle animation and immediately set the box values.
		oompaLoompaAnim.gameObject.SetActive(true);
		oompaLoompaAnim.Play("loop");
		
		return true;
	}
	
	protected override IEnumerator closeAfterDelay()
	{
		coinsAnim.Play("collect");
		yield return new WaitForSeconds(2.5f);	// Wait a little for the coins to finish flying up.
		
		addCreditsAndClose();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}




