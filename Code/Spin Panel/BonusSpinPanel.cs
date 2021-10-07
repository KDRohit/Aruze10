using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls the appearance and behaviour of the spin panel.
*/

public class BonusSpinPanel : TICoroutineMonoBehaviour
{
	public static BonusSpinPanel instance = null;
	
	public TextMeshPro spinsRemainingLabel;
	public TextMeshPro spinCountLabel;
	public TextMeshPro messageLabel;
	public TextMeshPro winningsAmountLabel;
	public TextMeshPro betAmountLabel;
	public Animator bonusMessageBoxAnimator;
	public GameObject centerMessage;
	public GameObject betAmountBox;

	public Transform spinsRemainingBoxParentTransform;
	public Transform winningsBoxParentTransform;
	public RectTransform spinsRemainingLabelTransform;
	public RectTransform spinCountLabelTransform;
	public Transform backgroundSpriteTransform;
	public Transform winningsBackgroundTransform;
	public Transform spinsRemainingBackgroundTransform;

	//Const values used for shifting around text to make room for the Wager Box in special cases
	private const float SPINS_REMAINING_BOX_PARENT_POS_BETS_ON = -690f;
	private const float SPIN_REMAINING_LABEL_POS_BETS_ON = -210f;
	private const float SPIN_COUNT_LABEL_POS_BETS_ON = 202f;
	private const float SPIN_COUNT_LABEL_WIDTH_BETS_ON = 250f;
	private const float BACKGROUND_SPRITE_WIDTH_BETS_ON = 468f;

	private float originalSpinsRemainingBoxWidth;
	private float originalSpinsRemainingBoxXLoc;
	private float originalWinningsBackgroundWidth;
	private float originalWinsBoxParentXLoc;


	private bool isHoldingMessageBox = false; //On non-orthographic games that don't scale up we don't hide/show the message box
		
	void Awake()
	{
		instance = this;
		originalSpinsRemainingBoxWidth = spinsRemainingBackgroundTransform.transform.localScale.x;
		originalSpinsRemainingBoxXLoc = spinsRemainingBoxParentTransform.transform.localPosition.x;
		originalWinningsBackgroundWidth = winningsBackgroundTransform.transform.localScale.x;
		originalWinsBoxParentXLoc = winningsBoxParentTransform.transform.localPosition.x;
	}

	//Replaces the standard text in the middle with a box showing the wager amount and adjusts other UI elements accordingly
	public void turnOnBetAmountBox(ReelGame reelGame)
	{
		CommonTransform.setX(spinsRemainingBoxParentTransform, SPINS_REMAINING_BOX_PARENT_POS_BETS_ON);
		CommonTransform.setX(spinsRemainingLabelTransform, SPIN_REMAINING_LABEL_POS_BETS_ON);
		CommonTransform.setX(spinCountLabelTransform, SPIN_COUNT_LABEL_POS_BETS_ON);
		spinCountLabelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, SPIN_COUNT_LABEL_WIDTH_BETS_ON);
		CommonTransform.setWidth(backgroundSpriteTransform, BACKGROUND_SPRITE_WIDTH_BETS_ON);

		betAmountLabel.text = CommonText.formatNumber(CreditsEconomy.multipliedCredits(reelGame.multiplier * reelGame.slotGameData.baseWager));
		centerMessage.SetActive(false);
		betAmountBox.SetActive(true);
	}

	public void setToPortraitMode()
	{
		centerMessage.SetActive(false);
		CommonTransform.setX(spinsRemainingBoxParentTransform, -346);
		CommonTransform.setX(winningsBoxParentTransform, 221);

		CommonTransform.setWidth(winningsBackgroundTransform, 670f);
		CommonTransform.setWidth(spinsRemainingBackgroundTransform, 420f);
	}

	public void setToLandscapeMode()
	{
		centerMessage.SetActive(true);
		CommonTransform.setX(spinsRemainingBoxParentTransform, originalSpinsRemainingBoxXLoc);
		CommonTransform.setX(winningsBoxParentTransform, originalWinsBoxParentXLoc);

		CommonTransform.setWidth(winningsBackgroundTransform, originalWinningsBackgroundWidth);
		CommonTransform.setWidth(spinsRemainingBackgroundTransform, originalSpinsRemainingBoxWidth);
	}

	public void slideInPaylineMessageBox()
	{
		if (bonusMessageBoxAnimator != null && !isHoldingMessageBox && !bonusMessageBoxAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.on"))
		{
			bonusMessageBoxAnimator.Play("on");
		}
	}

	public void slideOutPaylineMessageBox()
	{
		if (bonusMessageBoxAnimator != null && !isHoldingMessageBox && bonusMessageBoxAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.on"))
		{
			bonusMessageBoxAnimator.Play("off");
		}
	}

	public void holdPaylineMessageBox()
	{
		if (bonusMessageBoxAnimator != null)
		{
			bonusMessageBoxAnimator.Play("hold");
			isHoldingMessageBox = true;
		}
	}
}
