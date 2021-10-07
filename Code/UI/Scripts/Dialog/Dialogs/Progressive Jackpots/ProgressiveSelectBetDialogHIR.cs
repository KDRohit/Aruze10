using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls display of progressive jackpot initial bet selection.
*/

public class ProgressiveSelectBetDialogHIR : ProgressiveSelectBetDialog
{
	public Renderer gameTexture;
	public Renderer multiProgressiveGameTexture;
	public GameObject multiDescriptionLabel;
	public Transform gamePanel;
	public GameObject gameUnlockParent; // Parent of the Jackpot Unlock Game description.
	public GameObject topButtonsParent;
	public GameObject bottomButtonsParent;
	public GameObject topCoinIconsParent;
	public GameObject bottomCoinIconsParent;
	public TextMeshPro bottomLabel;
	[SerializeField] private GameObject decoratorAnchor;

	// Init called after common base init code.
	protected override void initForSKU(ProgressiveJackpot pj)
	{
		if (pj != null)
		{			
			multiDescriptionLabel.SetActive(gameInfo.isMultiProgressive);
			descriptionLabel.SetActive(!gameInfo.isMultiProgressive);

			if (pj.shouldGrantGameUnlock)
			{
				CommonTransform.setY(topButtonsParent.transform, TOP_BUTTONS_UNLOCK_Y);
				CommonTransform.setY(topLockIconsParent.transform, TOP_BUTTONS_UNLOCK_Y);
	
				CommonTransform.setY(topCoinIconsParent.transform, TOP_BUTTONS_UNLOCK_Y);
				CommonTransform.setY(bottomCoinIconsParent.transform, BOTTOM_BUTTONS_UNLOCK_Y);

				CommonTransform.setY(bottomButtonsParent.transform, BOTTOM_BUTTONS_UNLOCK_Y);
				CommonTransform.setY(bottomLockIconsParent.transform, BOTTOM_BUTTONS_UNLOCK_Y);		
				
				CommonTransform.setY(betButtonsParent.transform, BET_BUTTONS_PARENT_UNLOCK_Y);
			}

			gameUnlockParent.SetActive(pj.shouldGrantGameUnlock);

			if (gameInfo.isMultiProgressive)
			{
				MultiJackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
				MultiJackpotLobbyOptionDecorator1x2 multiJackpotDecorator = decoratorAnchor.GetComponentInChildren<MultiJackpotLobbyOptionDecorator1x2>();
				if (multiJackpotDecorator != null)
				{
					gameInfo.registerMultiProgressiveLabels(multiJackpotDecorator.jackpotLabels, false);
				}
			}
			else
			{
				if (pj != ProgressiveJackpot.giantJackpot)
				{
					JackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null, JackpotLobbyOptionDecorator.JackpotTypeEnum.Default);
					JackpotLobbyOptionDecorator1x2 jackpotDecorator = decoratorAnchor.GetComponentInChildren<JackpotLobbyOptionDecorator1x2>();
					if (jackpotDecorator != null)
					{
						pj.registerLabel(jackpotDecorator.jackpotTMPro);
					}
				}
				else
				{
					GiantJackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
					GiantJackpotLobbyOptionDecorator jackpotDecorator = decoratorAnchor.GetComponentInChildren<GiantJackpotLobbyOptionDecorator>();
					if (jackpotDecorator != null)
					{
						pj.registerLabel(jackpotDecorator.jackpotTMPro);
					}
				}
			}
		
			long minBet = 0L;
			buttonValues = setInitialWagerOptions(betLabels, betButtons, ref minBet, gameInfo, coinAndLockData);

			if (gameInfo.isMultiProgressive)
			{
				gameTexture.gameObject.SetActive(false);
				downloadedTextureToRenderer(multiProgressiveGameTexture, 0);
			}
			else
			{
				multiProgressiveGameTexture.gameObject.SetActive(false);
				downloadedTextureToRenderer(gameTexture, 0);
			}

			if (minBet == 0L)
			{
				subheaderLabel.text = "";
			}
			else
			{
				subheaderLabel.text = Localize.text("jackpot_minimum_bet_{0}", CreditsEconomy.convertCredits(minBet));
			}
			
			bottomLabel.text = Localize.text("jackpot_minimum_win_{0}", CreditsEconomy.convertCredits(pj.maxJackpot));
		}
	}
}
