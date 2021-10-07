using System.Collections;
using UnityEngine;

/*
 * Overlay shown when the board is complete
 */
public class BoardCompleteOverlay : TICoroutineMonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList onAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList offAnimations;

	[SerializeField] private ClickHandler collectButton;
	[SerializeField] private ClickHandler closeButton;
	
	[SerializeField] private LabelWrapperComponent creditsLabel;
	private ClickHandler.onClickDelegate onClickCallback;
	
	public void init(long credits, ClickHandler.onClickDelegate onCollectClick)
	{
		onClickCallback = onCollectClick;
		collectButton.registerEventDelegate(onCollectClicked);
		closeButton.registerEventDelegate(onCollectClicked);
		gameObject.SetActive(true);
		creditsLabel.text = CreditsEconomy.convertCredits(credits);
		StartCoroutine(AnimationListController.playListOfAnimationInformation(onAnimations));
	}

	private void onCollectClicked(Dict args)
	{
		StartCoroutine(onClose(args));
	}

	private IEnumerator onClose(Dict args)
	{
		collectButton.unregisterEventDelegate(onCollectClicked);
		closeButton.unregisterEventDelegate(onCollectClicked);
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offAnimations));
		if (onClickCallback != null)
		{
			onClickCallback.Invoke(args);
		}
	}
}