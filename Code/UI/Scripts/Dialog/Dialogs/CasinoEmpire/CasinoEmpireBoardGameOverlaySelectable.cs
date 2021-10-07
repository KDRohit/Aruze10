using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This is a animated token ui element
 * each representing a selectable token used in BoardGameTokenSelectOverlay
 */
public class CasinoEmpireBoardGameOverlaySelectable : TICoroutineMonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList onAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList offAnimations;
	[SerializeField] public BoardGameModule.BoardTokenType tokenType;
	
	[SerializeField] private ClickHandler tokenSelectButton;
	
	private ClickHandler.onClickDelegate onClickDelegate;
	
	public void init(ClickHandler.onClickDelegate onClickDelegate)
	{
		tokenSelectButton.registerEventDelegate(tokenSelected);
		this.onClickDelegate = onClickDelegate;
	}

	public void unSelect()
	{
		tokenSelectButton.registerEventDelegate(tokenSelected);
		StartCoroutine(playOffAnimation());
	}

	private IEnumerator playOffAnimation()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offAnimations));
	}

	private void tokenSelected(Dict args)
	{
		tokenSelectButton.unregisterEventDelegate(tokenSelected);
		args.Add(D.OPTION, tokenType);
		StartCoroutine(playOnAnimationsAndCallback(args));
	}

	private IEnumerator playOnAnimationsAndCallback(Dict args)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onAnimations));
		onClickDelegate.Invoke(args);
	}
}
