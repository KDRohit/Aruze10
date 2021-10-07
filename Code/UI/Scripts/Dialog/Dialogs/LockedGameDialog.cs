using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles display of dialog for telling a player that a touched game is locked.
*/

public class LockedGameDialog : DialogBase
{
	public TextMeshPro subhead;
	public TextMeshPro gameName;
	public TextMeshPro gameFeatures;
	public TextMeshPro levelLabel;
	public Renderer gameRenderer;
	public GameObject levelBasedMessage;
	public GameObject challengeBasedMessage;
	
	private LobbyGame game = null;

	// Returns whether the game is unlocked based on challenges instead of XP levels.
	private bool isChallengeBasedUnlock
	{
		get
		{
			// When we are in the main lobby, we should shw the level locked message instead.
			return (MainLobby.instance == null && game.isChallengeLobbyGame);
		}
	}

	public override void init()
	{
		if (dialogArgs != null)
		{
			game = dialogArgs.getWithDefault(D.GAME_KEY, null) as LobbyGame;
		}
		
		if (game == null)
		{
			Debug.LogError("LockedGame dialog: No game provided.");
		}
		else
		{
			// Note: challengeBasedMessage will be NULL for SIR prefabs, since Land-of-OZ is HIR-only now
			SafeSet.gameObjectActive(levelBasedMessage, !isChallengeBasedUnlock);
			SafeSet.gameObjectActive(challengeBasedMessage, isChallengeBasedUnlock);
			
			if (!isChallengeBasedUnlock)
			{
				subhead.text = Localize.text("reach_level_{0}_to_unlock_game", game.unlockLevel);				
				levelLabel.text = CommonText.formatNumber(game.unlockLevel);
			}
			
			gameName.text = Localize.toUpper(game.name);

			if (game.info.Count > 0)
			{
				gameFeatures.text = game.info[0].info;
			}
			else
			{
				gameFeatures.text = "";
				Debug.LogWarning("LockedGame dialog: Game " + game.keyName + " has no info to show.");
			}

			if (gameRenderer != null)
			{
				downloadedTextureToRenderer(gameRenderer, 0);
			}
		}
		
		Audio.play("minimenuopen0");
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		if (game == null)
		{
			// If no game is assigned, something went wrong, so just close immediately.
			// The specific error message has already been logged in init().
			Dialog.close();
		}
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public void okClicked()
	{
		Dialog.close();
	}
	
	public void closeClicked()
	{
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void showDialog(LobbyGame game)
	{
		// The 1x1 icon for the "NEW" dialog should theoretically already exist locally,
		// but preload here just in case for consistency.
		
		string filename = SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, "1X2");
		
		Dialog.instance.showDialogAfterDownloadingTextures("locked_game", args:Dict.create(D.GAME_KEY, game), nonMappedBundledTextures:new string[] {filename});
	}
}
