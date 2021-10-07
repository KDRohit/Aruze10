using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class BonusSummaryHIR : BonusSummary
{
	public TextMeshPro subHeadLabel;
	public TextMeshPro gameName;
	public UISprite glow;
	public GameObject giftedVipMultiplierBadgeAnchor; // The Anchor that the Gifted VIP Badge will be created under.

	private bool shouldPulse = false;
	
	/// Initialization
	public override void init()
	{
		if (GameState.giftedBonus != null && giftedVipMultiplierBadgeAnchor != null)
		{
			// call shared MFSDialog.badgeLoadCallbackSuccess
			Dict args = Dict.create(
				D.DATA, giftedVipMultiplierBadgeAnchor
			);
			AssetBundleManager.load(this, GiftedSpinsVipMultiplier.BADGE_PREFAB_PATH, MFSDialog.badgeLoadCallbackSuccess, badgeLoadCallbackFailure, args);
		}
		
		base.init();
			
		LobbyGame game = LobbyGame.find(BonusGameManager.instance.currentGameKey);
		subHeadLabel.text = game.name;	// This is pre-localized in global data.

		string bonusGameName = BonusGameManager.instance.summaryScreenGameName;
		if (BonusGameManager.instance.summaryScreenGameNameOverride != "")
		{
			bonusGameName = BonusGameManager.instance.summaryScreenGameNameOverride;
			BonusGameManager.instance.summaryScreenGameNameOverride = "";
		}
		
		BonusGame bonusGame = BonusGame.find(bonusGameName);

		if (BonusGamePresenter.instance != null && BonusGamePresenter.instance.hideNameInBonusSummaryDialog)
		{
			gameName.text = "";
		}
		else
		{
			if (bonusGame != null)
			{
				gameName.text = bonusGame.name;
			}
			else
			{
				Debug.LogError("Bonus Game not found for displaying the name. Wrong summary screen game name may be the culprit.");
			}
		}

		if (!MobileUIUtil.isSmallMobile && !isUsingMultiplier)
		{
			// If not using multiplier info, then center the icon since there is no text on the right to show.
			CommonTransform.setX(summaryIcon.transform, 0);
			CommonTransform.setX(gameName.gameObject.transform, 0);
		}
		if (MobileUIUtil.isSmallMobile && isUsingMultiplier)
		{
			CommonTransform.setY(gameName.transform, gameName.transform.localPosition.y + 0);
			CommonTransform.setY(bonusWinLabel.transform, 180);
		}
		StartCoroutine(showResults());
	}

	private void badgeLoadCallbackFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("BonusSummaryHIR.cs -- init -- failed to load prefab at path: {0}", path);
	}
		
	// Shows the results gradually over a few seconds.
	protected override IEnumerator showResults()
	{
		yield return StartCoroutine(base.showResultsBase(new GameObject[] { baseWinLabel, multiplierLabel}));
		shouldPulse = true;
		while (shouldPulse)
		{
			if (null != glow)
			{
				// Pulsate the glow until the player closes the dialog.
				glow.alpha = CommonEffects.pulsateBetween(.25f, .75f, 5);
			}
			else
			{
				break; // If there is no glow, then break out of the loop.
			}
			
			yield return null;
		}
	}
	
	public void OnDestroy()
	{
		// Set this here so that we make sure we kill the coroutine
		shouldPulse = false;
	}
}
