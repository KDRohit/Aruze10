using System.Collections;
using UnityEngine;

/*
 * Help/How to play button brings up this overlay
 */
public class CasinoEmpireBoardGameHelpOverlay : TICoroutineMonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList onAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList offAnimations;
	
	[SerializeField] private ClickHandler closeButton;
	[SerializeField] private ClickHandler okayButton;
	private ClickHandler.onClickDelegate onClickCallback;
	
	[SerializeField] private LabelWrapperComponent creditsLabel;
	
	public void init(long credits, ClickHandler.onClickDelegate onClick)
	{
		creditsLabel.text = CreditsEconomy.convertCredits(credits);
		onClickCallback = onClick;
		closeButton.registerEventDelegate(onCloseClicked);
		okayButton.registerEventDelegate(onCloseClicked);
		StartCoroutine(AnimationListController.playListOfAnimationInformation(onAnimations));
	}

	private void onCloseClicked(Dict args)
	{
		StartCoroutine(closeOverlay(args));
		closeButton.unregisterEventDelegate(onCloseClicked);
		okayButton.unregisterEventDelegate(onCloseClicked);
	}

	IEnumerator closeOverlay(Dict args)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offAnimations));
		if (onClickCallback != null)
		{
			onClickCallback.Invoke(args);
		}
	}
}
