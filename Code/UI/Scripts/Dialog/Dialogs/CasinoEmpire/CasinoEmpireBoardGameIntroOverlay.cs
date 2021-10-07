using System.Collections;
using UnityEngine;

public class CasinoEmpireBoardGameIntroOverlay : TICoroutineMonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList introAnimations;

	private ClickHandler.onClickDelegate onClose;
	
	public void init(ClickHandler.onClickDelegate onClose)
	{
		this.onClose = onClose;
		StartCoroutine(playIntroAnimations());
	}

	private IEnumerator playIntroAnimations()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation((introAnimations)));
		if (onClose != null)
		{
			onClose.Invoke(null);
		}
	}
}