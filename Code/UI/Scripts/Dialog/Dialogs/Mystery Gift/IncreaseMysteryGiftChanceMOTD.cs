using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class IncreaseMysteryGiftChanceMOTD : DialogBase
{
	public TextMeshPro timeLabel;
	
	private bool shouldCloseOnExpiration = false;
	
	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "increased_mystery_gift_chance", "", "", "", "view");
		
		if (!isValidPlayer())
		{
			return;
		}

		updateTimer();
		
		// If the feature is active, then close this dialog when it expires.
		// If it's not active, they opened it from the Dev GUI, so don't close it.
		shouldCloseOnExpiration = MysteryGift.isIncreasedMysteryGiftChance;

		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		if (!isValidPlayer())
		{
			return;
		}
		AndroidUtil.checkBackButton(clickClose);
		updateTimer();
	}
	
	private bool isValidPlayer()
	{
		if (SlotsPlayer.instance == null)
		{
			// This should never happen since SlotsPlayer.instance is designed to always return a new instance if one doesn't exist.
			enabled = false;
			Debug.LogError("IncreaseMysteryGiftChanceMOTD: SlotsPlayer.instance is null somehow.");
			return false;
		}
		return true;	
	}
	
	public void updateTimer()
	{
		if (MysteryGift.isIncreasedMysteryGiftChance)
		{
			timeLabel.text = MysteryGift.increasedMysteryGiftChanceRange.timeRemainingFormatted;
		}
		else if (shouldCloseOnExpiration)
		{
			Dialog.close();
		}
	}
	
	public void clickClose()
	{
		StatsManager.Instance.LogCount("dialog", "increased_mystery_gift_chance", "", "", "okay", "click");
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}
		
	public static bool showDialog(string motdKey = "")
	{
		string[] textureNames = null;

		// We already validated that there is at least one mystery gift game before calling this,
		// unless it was call from the dev panel.
		LobbyGame game = null;

		for (int i = 0; i < LobbyGame.pinnedMysteryGiftGames.Count; i++)
		{
			LobbyGame gameToCheck = LobbyGame.pinnedMysteryGiftGames[i];
			if (gameToCheck != null && gameToCheck.mysteryGiftType == MysteryGiftType.MYSTERY_GIFT)
			{
				game = gameToCheck;
				break;
			}
		}

		if (game == null)
		{
			Debug.LogError("IncreaseBigSliceChanceMOTD::showDialog - Tried to show the increased big slice chance MOTD without a game exclusivley marked as big slice");
			return false;
		}
		
		textureNames = new string[]
		{
			IncreaseMysteryGiftChanceMOTDHIR.HOT_STREAK_LOGO_PATH,
		};

		string[] gameImage = new string[]
		{
			SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "1X2")
		};
		
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		Dialog.instance.showDialogAfterDownloadingTextures(
			"increase_mystery_gift_chance_motd",
			textureNames,
			args,
			true,
			nonMappedBundledTextures:gameImage
		);
		return true;
	}
}
