using UnityEngine;
using System.Collections;

public class T102FreespinModule : Hot01FreespinModule
{
	protected override void animate(GameObject sparkleEffect)
	{
		Hashtable args = iTween.Hash("position", BonusSpinPanel.instance.spinCountLabel.transform.position,
		                             "time", TIME_MOVE_SPARKLE, "easetype", iTween.EaseType.linear,
		                             "looktarget", BonusSpinPanel.instance.spinCountLabel.gameObject,
		                             "oncomplete", "onAnimationComplete",
		                             "oncompletetarget", gameObject,
		                             "oncompleteparams", sparkleEffect.gameObject);
		iTween.MoveTo(sparkleEffect, args);
	}
}
