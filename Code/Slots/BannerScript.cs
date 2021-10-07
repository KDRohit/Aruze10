using UnityEngine;
using System.Collections;

public class BannerScript : TICoroutineMonoBehaviour
{
	public Animation onClickAnimation;

	public virtual void beginPortalReveals(GameObject target)
	{
		SlotBaseGame.instance.portal.beginPortalReveals(target);
	}
}
