using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class SelectGameUnlockDialog : DialogBase
{
	public TextMeshPro titleLabel; // Label for the title of the dialog.
	public TextMeshPro subtitleLabel; // Label for the subtitle of the dialog.
	public PageScroller pageScroller; // Pagescroller that controls showing/navigating the different panels.
	
	private string eventID = ""; // The event id that came down from the server event that spawned this dialog.
	private string featureName = ""; // The feature name associated with this game unlock.
	protected int numUnlocks = 0; //assigned but never used

	public static List<LobbyGame> gamesToDisplay; // The list of games that we populate the "store" with. (for general feature use).
	public static bool isWaitingForLevelUpEvent = false; // Whether we are waiting for a level up event from the server
	public static bool isWaitingForProgressiveJackpot = false;	// Whether we are waiting for a progressive jackpot win to be displayed first.
	
	private static Dict queuedDialogArgs = null; // Queued DialogArgs for the select game unlock dialog.
	
	private const float SHOW_DIALOG_WAIT_TIME = 10.0f; // The amount of time we will wait for other server responses.
	
	public override void init()
	{	
		eventID = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");
		featureName = (string)dialogArgs.getWithDefault(D.TYPE, "");
		titleLabel.text = Localize.textUpper(string.Format("select_game_title_{0}", featureName), "");
		subtitleLabel.text = Localize.textUpper(string.Format("select_game_{0}", featureName), "");
		numUnlocks = (int)dialogArgs.getWithDefault(D.VALUES, 1); // TODO change to 0 once we make sure this is coming down properly.

		pageScroller.dialog = this;
		pageScroller.init(gamesToDisplay.Count, onCreateGamePanel);
		
		StatsManager.Instance.LogCount("dialog", getStatsKingdom, "", "store", "view", "view");
	}
	

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (gamesToDisplay == null || gamesToDisplay.Count == 0)
		{
			// If somehow we got to this point without anything to display, then close the dialog.
			Dialog.close();
		}
	}
	
	public void Update()
	{
		if (featureName != "ftue_game_unlock")
		{
			// We don't want to allow users to back out of the game select dialog if this is the FTUE.
			AndroidUtil.checkBackButton(clickClose);
		}
	}
	
	// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	// Callback for the close button.
	private void clickClose()
	{
		StatsManager.Instance.LogCount("dialog", getStatsKingdom, "", "store", "close", "click");
		Dialog.close();
	}
	
	// Callback for the panel button, tells the server to unlock this game.
	public void unlockGame(LobbyGame game = null)
	{
		if (game == null)
		{
			// If no game was passed in, then we are in a multiple select mode and need to grab those games.
			
		}
		ServerAction.setFastUpdateMode("game_unlock");
		PlayerAction.selectGameUnlock(game.keyName, eventID);
		StatsManager.Instance.LogCount("dialog", getStatsKingdom, "", game.keyName, "unlock", "click");

		// Flag this as pending so it doesn't show up on the unlock game list before it's officially unlocked.
		game.xp.isPendingPlayerUnlock = true;
		
		// Immediately set the local flag as being redeemed to prevent triggering things again within this session.
		// Using a switch statement here in anticipation of expanding this to different xpromo features.
		switch (featureName)
		{
			case "woz_slots_xpromo_game_unlock":
				CarouselData slide = CarouselData.findActiveByAction("xpromo_woz");
				if (slide != null)
				{
					slide.deactivate();
				}
				break;
		}
		
		Dialog.close();
	}
	
	// Function to setup a game panel once created.
	private void onCreateGamePanel(GameObject panel, int page)
	{
		SelectGameUnlockPanel unlockPanel = panel.GetComponent<SelectGameUnlockPanel>();
		if (unlockPanel != null && page < gamesToDisplay.Count)
		{
			unlockPanel.initPage(gamesToDisplay[page], this);
		}
	}

	public static bool readyToShowDialog
	{
		get
		{
			return queuedDialogArgs != null && !isWaitingForLevelUpEvent && !isWaitingForProgressiveJackpot;
		}
	}
	
	public static bool shouldShowDialog(string feature, string eventId)
	{
		// Disabling the dialog: https://jira.corp.zynga.com/browse/HIR-88258
		// Uncomment this line if you need to enable the dialog.
		// return (gamesToDisplay != null && gamesToDisplay.Count > 0) && shouldShowDialogForFeature(feature);
		return false;
	}
	
	public static void showDialog(string feature, string eventId)
	{
		if ((gamesToDisplay == null || gamesToDisplay.Count == 0))
		{
			// If we have not yet setup the game list, then do it now.
			setupGameList();
		}
		
		queuedDialogArgs = Dict.create(
			D.EVENT_ID, eventId,
			D.TYPE, feature,
			D.STACK, false,
			D.IS_LOBBY_ONLY_DIALOG, feature == "progressive_jackpot_game_unlock" ? false : true
		);
		
		// If the player won a jackpot to unlock a game, we must wait until we've shown
		// the jackpot win dialog before showing the game unlock dialog.
		// We must also check for whether this happened while in the lobby, since it may
		// come in after a re-load if the player didn't choose his game right after it came in.
		isWaitingForProgressiveJackpot = (feature == "progressive_jackpot_game_unlock" && !GameState.isMainLobby);

		// If the player leveled up, they may have some game unlocks that will show.
		// In this case, we want to wait until we have granted those unlocks to the user before showing this dialog.
		ExperienceLevelData nextLevel = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel + 1);
		if (nextLevel != null)
		{
			isWaitingForLevelUpEvent = (SlotsPlayer.instance.socialMember.xp >= nextLevel.requiredXp);
		}
	}

	// Removes the game from any unlock lists.
	public static void removeGameFromLists(LobbyGame game)
	{
		if (gamesToDisplay.Contains(game))
		{
		    gamesToDisplay.Remove(game);
		}
	}
	
	// Returns whether we should show this dialog for the given feature.
	private static bool shouldShowDialogForFeature(string feature)
	{
		switch (feature)
		{
			case "progressive_jackpot_game_unlock":
			case "generic":
				return true;
			case "woz_slots_xpromo_game_unlock":
				return ExperimentWrapper.XPromoWOZSlotsGameUnlock.isInExperiment;;
			case "stud_based_game_unlock":
				// MCC -- The server is removing this, but we dont want to spam the error log anymore.
				// Adding a case so that we dont spam a log until the server has cleaned up the event on their side.
				return false;
			default:
				Debug.LogError("Unknown select_game_unlock feature: " + feature);
				return false;
		}
	}
	
	// If we have a dialog queued up, show it.
	public static void showQueuedDialog()
	{
		if (queuedDialogArgs != null)
		{
			SelectGameUnlockDialog.showDialog(queuedDialogArgs);
			queuedDialogArgs = null;
		}
	}
	
	// Static function to populate the display list from the game data.
	// This will need to be called whenever we unlock a game.
	public static void setupGameList()
	{
		if (gamesToDisplay == null)
		{
		    gamesToDisplay = new List<LobbyGame>();
		}
		else
		{
			gamesToDisplay.Clear();
		}
		
		// Only show games that are in the main lobby.
		LobbyInfo lobby = LobbyInfo.find(LobbyInfo.Type.MAIN);

		if (lobby != null)
		{
			foreach (LobbyOption option in lobby.allLobbyOptions)
			{
				if (option.game != null && option.game.canBeUnlocked && !gamesToDisplay.Contains(option.game))
				{
					// Check for normal game unlocks
				    gamesToDisplay.Add(option.game);
				}
			}
		}
		
		gamesToDisplay.Sort(unlockLevelSortingFunction);
	}
	
	// Sorting Function to sort the LobbyGame list by unlockLevel.
	private static int unlockLevelSortingFunction(LobbyGame one, LobbyGame two)
	{
		if (one.unlockLevel == two.unlockLevel)
		{
			// If they unlock at the same level (which they shouldn't), then sort by name.
			return one.name.CompareTo(two.name);
		}
		else
		{
			// Otherwise sort by unlock level.
			return one.unlockLevel.CompareTo(two.unlockLevel);
		}
	}
	
	// Returns the Kingdom that we will use for the stats. This is based off of the feature name.
	private string getStatsKingdom
	{
		get
		{
			switch(featureName)
			{
				case "progressive_jackpot_unlock_game":
					return "jackpot_unlock";
				case "woz_slots_xpromo_game_unlock":
					return "woz_slots_xpromo_game_unlock";
				default:
					return "select_game_unlock";
			}
		}
	}
	
	public static void showDialog(Dict args)
	{
		Scheduler.addDialog("select_game_unlock", args);
	}
}

