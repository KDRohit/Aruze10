using UnityEngine;
using System.Collections;

public class Billted01PickPortal : PickPortal
{
	[SerializeField] protected Animator portalAnimator;
	[SerializeField] protected string PHONE_BOOTH_IN_ANIM = "";
	[SerializeField] protected string LEFT_BOOTH_AMBIENT_ANIM = "";
	[SerializeField] protected string RIGHT_BOOTH_AMBIENT_ANIM = "";
	[SerializeField] protected string BACKGROUND_STARTING_ANIM = "";

	/// Init game specific stuff
	public override void init()
	{
		backgroundAnimator.Play(BACKGROUND_STARTING_ANIM);
		foreach(PickGameButton pgb in pickButtons)
		{
			pgb.animator.Play(PHONE_BOOTH_IN_ANIM);
		}		
		StartCoroutine(playBoothAmbients());
		base.init();
	}

	private IEnumerator playBoothAmbients()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalAnimator, LEFT_BOOTH_AMBIENT_ANIM));
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(portalAnimator, RIGHT_BOOTH_AMBIENT_ANIM));
	}
}
