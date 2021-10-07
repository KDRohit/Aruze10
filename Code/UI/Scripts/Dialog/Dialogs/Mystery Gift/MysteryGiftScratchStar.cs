using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Property holder for Mystery Gift pickem boxes.
*/

public class MysteryGiftScratchStar : MysteryGiftBaseMatch
{

	public override void setup(int index)
	{
		// Nothing to setup here.
	}
	
	public override IEnumerator pick(PickemPick data)
	{
		// Plain multiplier.
		multiplierSprite.gameObject.SetActive(true);
		multiplierSprite.spriteName = string.Format("{0}x", data.baseCredits);
#if !ZYNGA_KINDLE
		multiplierSprite.MakePixelPerfect();
#endif

		yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, "star_reveal"));
	}
	
	public override void reveal(PickemPick data)
	{
		multiplierSprite.gameObject.SetActive(true);
		multiplierSprite.spriteName = string.Format("{0}x_desat", data.baseCredits);
#if !ZYNGA_KINDLE
		multiplierSprite.MakePixelPerfect();
#endif

		StartCoroutine(CommonAnimation.playAnimAndWait(animator, "star_reveal"));
	}
}