using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class IncreaseBigSliceChanceMOTD : DialogBase
{
	public Renderer hotStreakTexture;
	public Renderer gameTexture;
	public TextMeshPro timeLabel;
	[SerializeField] private GameObject decoratorAnchor;

	
	private bool shouldCloseOnExpiration = false;
	
	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "increased_big_slice_chance", "", "", "", "view");
		
		downloadedTextureToRenderer(hotStreakTexture, 0);
		downloadedTextureToRenderer(gameTexture, 1);
		BigSliceLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);

		updateTimer();
		
		// If the feature is active, then close this dialog when it expires.
		// If it's not active, they opened it from the Dev GUI, so don't close it.
		shouldCloseOnExpiration = MysteryGift.isIncreasedBigSliceChance;
		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
		updateTimer();
	}
	
	public void updateTimer()
	{
		if (MysteryGift.isIncreasedBigSliceChance)
		{
			timeLabel.text = MysteryGift.increasedBigSliceChanceRange.timeRemainingFormatted;		
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
		// We already validated that there is at least one mystery gift game before calling this,
		// unless it was call from the dev panel.
		LobbyGame game = null;

		for (int i = 0; i < LobbyGame.pinnedMysteryGiftGames.Count; i++)
		{
			LobbyGame gameToCheck = LobbyGame.pinnedMysteryGiftGames[i];
			if (gameToCheck != null && gameToCheck.mysteryGiftType == MysteryGiftType.BIG_SLICE)
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

		string[] textureNames = new string[]
		{
			"misc_dialogs/mystery_gift_hot_streak.png"
		};

		string[] gameImages = new string[]
		{
			SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "1X2")
		};
		
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		Dialog.instance.showDialogAfterDownloadingTextures(
			"increase_big_slice_chance_motd",
			textureNames,
			args,
			true,
			nonMappedBundledTextures:gameImages
		);
		return true;
	}
}
