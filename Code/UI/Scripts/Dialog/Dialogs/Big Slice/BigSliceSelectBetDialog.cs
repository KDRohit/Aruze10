using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls display of mystery gift initial bet selection.
*/

public class BigSliceSelectBetDialog : SelectBet
{
	public TextMeshPro subheaderLabel;
	public GameObject increasedChanceIcon;
	public Renderer gameTexture;
	public TextMeshPro[] betLabels;		// Must be defined in the same order as betButtons.
	public GameObject closeButton;	// Close button that hides or shows based on param passed in dialogArgs
	public ProgressiveMysteryCoinOrLockIconData[] coinAndLockData;		// Data for the coins and locks, ordered from bottom of the dialog to the top, i.e. [0] is tied to the lowest bet
	public GameObject hyperEconomyIntroPrefab;
	public GameObject headerAnchor;

	// Initialization
	public override void init()
	{
		bool isShowingCloseButton = (bool)dialogArgs.getWithDefault(D.SHOW_CLOSE_BUTTON, false);
		BigSliceLobbyOptionDecorator1x2.loadPrefab(headerAnchor, null);
		if (closeButton != null)
		{
			closeButton.SetActive(isShowingCloseButton);
		}

		string gameKey = dialogArgs.getWithDefault(D.GAME_KEY, "") as string;

		if (gameKey != "")
		{
			// grab the lobby info for this game
			gameInfo = LobbyGame.find(gameKey);
		}
		else
		{
			// fallback to checking if we are already in a game and can just grab it
			gameInfo = GameState.game;
		}

		increasedChanceIcon.SetActive(MysteryGift.isIncreasedBigSliceChance);

		long minBet = 0L;
		buttonValues = ProgressiveSelectBetDialog.setInitialWagerOptions(betLabels, betButtons, ref minBet, gameInfo, coinAndLockData, "big_slice");

		if (minBet == 0L)
		{
			subheaderLabel.text = "";
		}
		else
		{
			subheaderLabel.text = Localize.text("big_slice_minimum_bet_{0}", CreditsEconomy.convertCredits(minBet));
		}

		string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName, "1X2");
		DisplayAsset.loadTextureToRenderer(gameTexture, filename, skipBundleMapping:true, pathExtension:".png");

		if (HyperEconomyIntroBet.shouldShow)
		{
			GameObject go = NGUITools.AddChild(sizer.gameObject, hyperEconomyIntroPrefab);
			CommonTransform.setZ(go.transform, -50.0f);	// Make sure it's in front of other stuff.
		}

		Audio.play("minimenuopen0");

		StatsManager.Instance.LogCount
		(
			"dialog"
			, "bet_selector"
			, "big_slice"
			, gameInfo.keyName
			, ""
			, "view"
		);
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void clickBetButton(GameObject go)
	{
		if (buttonValues != null)
		{
			int index = System.Array.IndexOf(betButtons, go);
			long selectedAmount = 0;
				
			if (index < buttonValues.Length)
			{
				selectedAmount = buttonValues[index];
			}

			StatsManager.Instance.LogCount
			(
				"dialog"
				, "bet_selector"
				, "big_slice"
				, gameInfo.keyName
				, index.ToString()
				, "click"
				,  CreditsEconomy.multipliedCredits(selectedAmount)
				, (gameInfo.defaultBetValue / SlotsPlayer.creditAmount).ToString()
			);

			dialogArgs.merge(D.ANSWER, selectedAmount);
		}
		Dialog.close();
	}
				
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	/// Used by UIButtonMessage
	public void closeClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount
		(
			"dialog"
			, "bet_selector"
			, "big_slice"
			, gameInfo.keyName
			, ""
			, "close"
		);
	}
	
	public static void showDialog(Dict args)
	{
		Scheduler.addDialog("big_slice_select_bet", args);
	}

#if ZYNGA_TRAMP || UNITY_EDITOR
	public override IEnumerator automate()
	{
		while (this != null &&  Dialog.instance.currentDialog == this && !Dialog.instance.isClosing)
		{
			yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(gameObject));
		}
	}
#endif // ZYNGA_TRAMP || UNITY_EDITOR	
}
