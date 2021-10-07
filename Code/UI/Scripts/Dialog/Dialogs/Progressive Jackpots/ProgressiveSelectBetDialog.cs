using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls display of progressive jackpot initial bet selection.
*/

public abstract class ProgressiveSelectBetDialog : SelectBet
{
	protected const int GAME_PANEL_VIP_Y = -169;		// The Y position of the game panel when it's a VIP game.
	protected const int GAME_PANEL_GIANT_Y = -163;	// The Y position of the game panel when it's a Giant progressive game.
	protected const float TOP_BUTTONS_UNLOCK_Y = 10f;
	protected const float BOTTOM_BUTTONS_UNLOCK_Y = -80f;
	protected const float BET_BUTTONS_PARENT_UNLOCK_Y = 230f;
	protected readonly static Color BUTTON_DISABLE_COLOR = new Color(0.33f, 0.33f, 0.33f, 1.0f);
	
	public TextMeshPro subheaderLabel;
	public TextMeshPro[] jackpotLabels;
	public TextMeshPro[] betLabels;		// Must be defined in the same order as betButtons.
	public GameObject descriptionLabel;
	public GameObject betButtonsParent;
	public GameObject topLockIconsParent;
	public GameObject bottomLockIconsParent;
	public GameObject closeButton;	// Close button that hides or shows based on param passed in dialogArgs
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

		// Define some shorthand variables for convenience.
		if (gameInfo == null)
		{
			Debug.LogError("ProgressiveSelectBetDialog: gameInfo is null! Dialog can't be shown correctly.");
			Dialog.close();
		}
		else if (!gameInfo.isProgressive)
		{
			Debug.LogError("ProgressiveSelectBetDialog: Game " + gameInfo.keyName + " is not a progressive game!");
			Dialog.close();
		}
		else
		{
			ProgressiveJackpot pj = gameInfo.progressiveJackpots[gameInfo.progressiveJackpots.Count - 1];
			initForSKU(pj);
			Audio.play("minimenuopen0");

			StatsManager.Instance.LogCount
			(
				"dialog"
				, "bet_selector"
				, "progressive_jackpot"
				, gameInfo.keyName
				, ""
				, "view"
			);
	   }
	}

	// Do sku-specific init.
	protected virtual void initForSKU(ProgressiveJackpot pj)
	{
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	// Sets the initial bet amounts on the buttons, and returns the multipliers in an array for clicking use.
	// Also sets the overlay tooltip with the min bet amount.
	// This is used by both Progressive Jackpots and Mystery Gifts.
	public static long[] setInitialWagerOptions(TextMeshPro[] betLabels, GameObject[] betButtons, LobbyGame gameInfo, ProgressiveMysteryCoinOrLockIconData[] coinAndLockData, string type = "jackpot")
	{
		// Overload for callers that don't need the minBet output.
		long minBet = 0L;
		return setInitialWagerOptions(betLabels, betButtons, ref minBet, gameInfo, coinAndLockData);
	}
	
	public static long[] setInitialWagerOptions
	(
		  TextMeshPro[] betLabels
		, GameObject[] betButtons
		, ref long minBet
		, LobbyGame gameInfo
		, ProgressiveMysteryCoinOrLockIconData[] coinAndLockData
		, string type = "jackpot"
	)
	{
		string wagerSet = SlotsWagerSets.getWagerSetForGame(gameInfo.keyName);
		long[] buttonValues = gameInfo.getBetButtonValues(wagerSet);
	
		long specialGameMinQualifyingAmount = gameInfo.specialGameMinQualifyingAmount;

		minBet = 0L;
		
		for (int i = 0; i < buttonValues.Length && i < betLabels.Length; i++)
		{
			betLabels[i].text = CreditsEconomy.convertCredits(buttonValues[i]);

			if (buttonValues[i] >= specialGameMinQualifyingAmount && minBet == 0)
			{
				minBet = buttonValues[i];
			}
		}

		// new wager system will lock wager values that the player can't bet at yet
		if (coinAndLockData != null)
		{
			int playerLevel = SlotsPlayer.instance.socialMember.experienceLevel;

			for (int i = 0; i < coinAndLockData.Length; i++)
			{
				int wagerUnlockLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, buttonValues[i]);
				if (wagerUnlockLevel <= playerLevel)
				{
					// this wager is unlocked for the player, or is the lowest bet amount which should always be unlocked
					coinAndLockData[i].showCoinIcon();
				}
				else
				{
					// this wager is not unlocked for the player yet, so disable the button, and change the bet label to unlock text
					coinAndLockData[i].showLockIcon(wagerUnlockLevel);

					disableButton(betButtons[i], betLabels[i]);
				}
			}
		}

		return buttonValues;
	}

    // abstract
	/// Disable a button, used to disable the wager butons when they are locked
	protected static void disableButton(GameObject button, TextMeshPro buttonLabel)
	{
		Collider collider = button.GetComponent<Collider>();
		if (collider != null)
		{
			// disable input
			collider.enabled = false;
		}

		// If the button has a UIImageButton component, disabled it (regardless of SKU).
        UIImageButton imageButton = button.GetComponent<UIImageButton>();
		if (imageButton != null)
		{
			imageButton.isEnabled = false;
		}
		else
		{
			// If the button doesn't have UIImageButton, look for UIButtonColor objects
			// and set the target to the disabled color (regardless of SKU).
	        foreach (UIButtonColor buttonColor in button.GetComponentsInChildren<UIButtonColor>())
			{
				CommonGameObject.colorUIGameObject(buttonColor.tweenTarget, BUTTON_DISABLE_COLOR);
			}
		}
		
		// Traci Hui decided to not tint the text in SIR, which is why this condition exists. HIR-26171
		buttonLabel.color = BUTTON_DISABLE_COLOR;

        // This is so the button acutally switches states
        button.SetActive(false);
        button.SetActive(true);
	}

	protected void clickBetButton(GameObject go)
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
				, "progressive_jackpot"
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
			, "progressive_jackpot"
			, gameInfo.keyName
			, ""
			, "close"
		);
	}

	public static void showDialog(Dict args)
	{
		string gameKey = args.getWithDefault(D.GAME_KEY, "") as string;
		LobbyGame gameInfo = null;

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

		if (gameInfo != null)
		{
			// Multiprogressive mode uses a 1X1 image, which is achieved by passing in "".
			string imageSize = (gameInfo.isMultiProgressive ? "" : "1X2");
			string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName, imageSize);

			Dialog.instance.showDialogAfterDownloadingTextures("progressive_jackpot_select_bet", nonMappedBundledTextures:new string[]{filename}, args:args);
		}
		else
		{
			Debug.LogError("ProgressiveSelectBetDialog::showDialog() - gameInfo is null and couldn't be found!  Dialog will not be shown!");
		}
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

[System.Serializable]
public class ProgressiveMysteryCoinOrLockIconData
{
	public GameObject coinIcon;		// Coin icon shown for eligable bets, and when not using wager locking
	public GameObject lockIcon;		// Lock shown when using wager locking if the player hasn't unlocked the wager yet because they aren't high enough level
	public TextMeshPro unlockLevelText; 	// Text for the lock level

	public void showCoinIcon()
	{
		SafeSet.gameObjectActive(lockIcon, false);
		SafeSet.gameObjectActive(coinIcon, true);
	}

	public void showLockIcon(int unlockLevel)
	{
		SafeSet.gameObjectActive(lockIcon, true);
		SafeSet.gameObjectActive(coinIcon, false);
		SafeSet.labelText(unlockLevelText, CommonText.formatNumber(unlockLevel));
	}
}
