using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls display of mystery gift initial bet selection.
*/

public class MysteryGiftBaseSelectBetDialog : SelectBet
{	
	public TextMeshPro[] betLabels;			// Must be defined in the same order as betButtons.
	public GameObject closeButton;		// Close button that hides or shows based on param passed in dialogArgs
	public ProgressiveMysteryCoinOrLockIconData[] coinAndLockData;		// Data for the coins and locks, ordered from bottom of the dialog to the top, i.e. [0] is tied to the lowest bet

	// Initialization
	public override void init()
	{
		bool isShowingCloseButton = (bool)dialogArgs.getWithDefault(D.SHOW_CLOSE_BUTTON, false);

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

			
		buttonValues = ProgressiveSelectBetDialog.setInitialWagerOptions(betLabels, betButtons, gameInfo, coinAndLockData, "mystery_gift");

		Audio.play("minimenuopen0");

		StatsManager.Instance.LogCount
		(
			"dialog"
			, "bet_selector"
			, "mystery_gift"
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
				, "mystery_gift"
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
	}

	/// Used by UIButtonMessage
	public void closeClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount
		(
			"dialog"
			, "bet_selector"
			, "mystery_gift"
			, gameInfo.keyName
			, ""
			, "close"
		);
	}
	
	public static void showDialog(Dict args)
	{
		Scheduler.addDialog("mystery_gift_select_bet", args);
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
