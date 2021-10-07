using System.Collections;
using UnityEngine;

/*
 * Overlay to select token for board game.
 */
public class CasinoEmpireBoardGameTokenSelectOverlay : TICoroutineMonoBehaviour
{
	private BoardGameModule.BoardTokenType selectedToken;
	
	private ClickHandler.onClickDelegate onClickHandler;

	[SerializeField] private ClickHandler startButton;
	[SerializeField] private CasinoEmpireBoardGameOverlaySelectable[] tokenSelectButtons;

	[SerializeField] private AnimationListController.AnimationInformationList introAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList outroAnimations;
	
	public void init(ClickHandler.onClickDelegate onClick)
	{
		this.onClickHandler = onClick;
		for (int i = 0; i < tokenSelectButtons.Length; i++)
		{
			tokenSelectButtons[i].init(onTokenSelected);
		}
		startButton.gameObject.SetActive(false);
		startButton.registerEventDelegate(onStartClick);
		StartCoroutine(playIntroAnimation());
	}

	private IEnumerator playIntroAnimation()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimations));
	}

	private void onTokenSelected(Dict args)
	{
		selectedToken = (BoardGameModule.BoardTokenType)args.getWithDefault(D.OPTION, BoardGameModule.BoardTokenType.Bell);
		for (int i = 0; i < tokenSelectButtons.Length; i++)
		{
			if (selectedToken != tokenSelectButtons[i].tokenType)
			{
				tokenSelectButtons[i].unSelect();
			}
		}
		startButton.gameObject.SetActive(true);
	}
	
	private void onStartClick(Dict args)
	{
		StartCoroutine(playOutroAnimationsAndClose(args));
	}

	private IEnumerator playOutroAnimationsAndClose(Dict args)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimations));
		args.Add(D.OPTION, selectedToken);
		if (onClickHandler != null)
		{
			onClickHandler.Invoke(args);
		}
	}
}
