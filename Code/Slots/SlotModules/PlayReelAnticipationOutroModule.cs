using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayReelAnticipationOutroModule : SlotModule
{
	[SerializeField] private string outroAnimName = "outro";

	public override bool needsToHideReelAnticipationEffectFromModule (SpinReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator hideReelAnticipationEffectFromModule (SpinReel stoppedReel)
	{
		GameObject featureEffect = reelGame.engine.getFeatureAnticipationObject();
		if (featureEffect != null && featureEffect.activeSelf)
		{
			Animator reelAnticipationAnimator = featureEffect.GetComponent<Animator>();
			if (reelAnticipationAnimator != null)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(reelAnticipationAnimator, outroAnimName));
			}
		}
		reelGame.engine.hideAnticipationEffect(stoppedReel.getRawReelID() + 1);
	}
}
