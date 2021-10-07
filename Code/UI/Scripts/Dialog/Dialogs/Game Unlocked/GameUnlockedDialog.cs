using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Zynga.Core.Util;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class GameUnlockedDialog : DialogBase
{
	public Transform background;
	public TextMeshPro subTitleLabel;
	public TextMeshPro currentLevelLabel;
	public TextMeshPro nextLevelLabel;
	public UIInput messageInput;
	public TextMeshPro shareLabel;
	public TextMeshPro unlockedGameLabel;
	public TextMeshPro nextGameLabel;
	public GameObject unlockedLabel;
	public GameObject unlockedParent;
	public GameObject nextUnlockParent;
	public GameObject unlockedFrameParent;
	public GameObject unlockedLockParent;
	public GameObject nextUnlockedLockParent;
	public FacebookFriendInfo friendInfo;
	public GameObject inputParent;
	public GameObject closeButton;
	public GameObject shareButton;
	public Renderer unlockedRenderer;
	public Renderer nextUnlockRenderer;
	
	protected LobbyGame unlockedGame = null;
	protected LobbyGame nextUnlockGame = null;

	// Constant varaibles
	protected const int NUMBER_OF_FIREWORKS = 6;									// Number of times we want to play the firework sound.
	protected const float FIREWORK_DELAY = 1.0f;									// Time between each firework sound
	// Sound Names
	protected const string PART1 = "UnlockGamePt1";								// Played at the start of the dialog.
	protected const string PART2 = "UnlockGamePt2";								// Played once the second image comes in
	protected const string LOCK_JIGGLE = "UnlockGameLockJiggle";					// Sound played while the lock is jiggling left and right.
	protected const string LOCK_OPEN = "fastsparklyup1";							// Sound played when the lock clicks open.
	protected const string LOCK_FALL = "FastSparklyWhooshDown1";					// Sound name played when the lock falls in from off screen.
	protected const string LOCK_LAND = "Hit";										// Sound name played when the lock lands after falling in.
	protected const string FIREWORK_SOUND = "fireworks_all";						// Sound played for each firework that goes off.


	public override void init()
	{
		unlockedGame = dialogArgs.getWithDefault(D.OPTION, null) as LobbyGame;
		nextUnlockGame = dialogArgs.getWithDefault(D.OPTION1, null) as LobbyGame;

		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		if (skuGameUnlock != null && unlockedGame == skuGameUnlock)
		{
			subTitleLabel.text = Localize.text("sku_game_unlock_subtitle");
			StatsManager.Instance.LogCount("dialog", "xpromo", MOTDDialog.getSkuGameUnlockPhylum(), "game_unlock_granted", "", "view");			
		}
		else
		{
			subTitleLabel.text = Localize.text("game_unlock_dialog_header");
		}

		// Hide the interactive elements by default, so no interaction is possible during the animation.
		closeButton.SetActive(false);
		shareButton.SetActive(false);
		inputParent.SetActive(false);

		if (!SlotsPlayer.isFacebookUser && !Sharing.isAvailable)
		{
			// If not a facebook user, then the custom message area isn't shown,
			// so make the entire dialog a bit shorter so there isn't a weird gap.
			CommonTransform.setHeight(background, 1100);
		}
		
		// Set up default position and visibilities of animated elements.
		CommonTransform.setX(unlockedParent.transform, 0);
		unlockedParent.transform.localScale = Vector3.one * .1f;
		nextUnlockParent.transform.localScale = Vector3.one * .1f;
		unlockedLabel.SetActive(false);
		// Anything that uses the HIDDEN layer to hide must have its own UIPanel, otherwise it doesn't work as expected.
		CommonGameObject.setLayerRecursively(unlockedParent, Layers.ID_HIDDEN);
		CommonGameObject.setLayerRecursively(nextUnlockParent, Layers.ID_HIDDEN);
		CommonGameObject.setLayerRecursively(nextUnlockedLockParent, Layers.ID_HIDDEN);
		if (unlockedGame == null)
		{
			Debug.LogError("GameUnlockedDialog: No option passed in.", gameObject);
		}
		else
		{
			if (unlockedGame.unlockLevel > 0)
			{
				if (unlockedGame.isLOZGame)
				{
					currentLevelLabel.text = "";
				}
				else
				{
					currentLevelLabel.text = CommonText.formatNumber(unlockedGame.unlockLevel);
				}
			}
			else
			{
				currentLevelLabel.text = "";
			}

			if (!downloadedTextureToRenderer(unlockedRenderer, 0))
			{
				// If the texture didn't download, then show the name of the game on a label.
				unlockedGameLabel.text = unlockedGame.name;
				unlockedGameLabel.gameObject.SetActive(true);
			}
			
			if (nextUnlockGame == null)
			{
				// Must have unlocked that last game to unlock, so don't show the next unlock.
				nextUnlockParent.SetActive(false);
			}
			else
			{
				nextLevelLabel.text = CommonText.formatNumber(nextUnlockGame.unlockLevel);

				if (!downloadedTextureToRenderer(nextUnlockRenderer, 1))
				{
					// If the texture didn't download, then show the name of the game on a label.
					nextGameLabel.text = nextUnlockGame.name;
					nextGameLabel.gameObject.SetActive(true);
				}
			}
		
			if (Sharing.isAvailable)
			{
				shareLabel.text = Localize.textUpper("share");
			}		
			else if (SlotsPlayer.isFacebookUser)
			{
				friendInfo.member = SlotsPlayer.instance.socialMember;
			
				shareLabel.text = Localize.textUpper("share_and_play");
			}
			else
			{
				shareLabel.text = Localize.textUpper("play");
			}
		}
		
		// Audio sound call on open.
		Audio.play("cheer_c");
	}

	/// Do stuff after finishing the transition in.
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (unlockedGame == null)
		{
			// No valid game was passed in, so just close. Error was already logged in init().
			cancelAutoClose();
			Dialog.close();
		}
		else
		{
			if (unlockedGame.isLOZGame)
			{
				StatsManager.Instance.LogCount("dialog", "loz_unlock_game", "view", currentLevelLabel.text, "", "view");
			}
			else
			{
				StatsManager.Instance.LogCount("dialog", "game_unlock", "view", currentLevelLabel.text, "", "view");
			}
			StatsManager.Instance.LogMileStone("game_unlock_" + unlockedGame.keyName, unlockedGame.unlockLevel);
			StartCoroutine(animateDialog());
		}
	}

	protected virtual IEnumerator animateDialog()
	{
		// Override for dialog animation.
		yield return null;
	}

	
	public virtual void Update()
	{
		
		AndroidUtil.checkBackButton(closeClicked, "dialog", "game_unlocked", StatsManager.getGameTheme(), StatsManager.getGameName(), "back", "click");

		if (messageInput.selected)
		{
			resetIdle();
		}

		if (shouldAutoClose)
		{
			cancelAutoClose();
			Dialog.close();
		}
	}
	
	/// NGUI button callback.
	protected void shareClicked()
	{
		StatsManager.Instance.LogCount("dialog","game_unlock","", currentLevelLabel.text, "", "claim");
		postAndClose();
	}
	
	/// Post the user message and close the dialog.
	protected void postAndClose()
	{
		cancelAutoClose();

		if (SlotsPlayer.isFacebookUser || Sharing.isAvailable)
		{
			string postMessage;
			if (messageInput.text == messageInput.defaultText)
			{
				postMessage = "";
			}
			else
			{
				postMessage = messageInput.text;
			}

			if (Sharing.isAvailable)
			{
				Sharing.shareGameEventWithScreenShot("game_unlock_share_subject", "", postMessage, "game_unlock_share_body", "game_unlock", unlockedGame.name);
				return;
			}
			else
			{
				Dialog.close();
			}
		}
		else
		{
			// the button is just a plain old "Play" button so close dialog and play!
			Dialog.close();
		}			
		
		
		// Load the game right now.
		// Tell the lobby which game to launch when finished returning to the lobby.
		PreferencesBase prefs = SlotsPlayer.getPreferences();
		prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, unlockedGame.keyName);
		prefs.Save();

		SlotAction.setLaunchDetails("motd");
		
		if (GameState.isMainLobby)
		{
			LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
			if (skuGameUnlock != null && unlockedGame == skuGameUnlock)
			{
				StatsManager.Instance.LogCount("dialog", "xpromo", MOTDDialog.getSkuGameUnlockPhylum(), "game_unlock_granted", "", "click");
				Scheduler.addFunction(forceGameRefresh);
			}
			else
			{
				// Refresh the lobby if already in it during game unlock, so the unlocked game will appear unlocked.
				Scheduler.addFunction(MainLobby.refresh);
			}
		}
		else
		{
			// Currently in a game.
			// First go back to the lobby and go through the common route to launching a game.
			Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
		}
	}

	/// NGUI button callback.
	protected void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog","game_unlock","close", currentLevelLabel.text, "", "close");
		Dialog.close();
		
		if (GameState.isMainLobby)
		{
			LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
			if (skuGameUnlock != null && unlockedGame == skuGameUnlock)
			{
				StatsManager.Instance.LogCount("dialog", "xpromo", MOTDDialog.getSkuGameUnlockPhylum(), "game_unlock_granted", "", "click");
				Scheduler.addFunction(forceGameRefresh);
			}
			else
			{
				// Refresh the lobby if already in it during game unlock, so the unlocked game will appear unlocked.
				Scheduler.addFunction(MainLobby.refresh);
			}
		}
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	protected void forceGameRefresh(Dict args)
	{
		Scheduler.removeFunction(forceGameRefresh);
		Server.forceGameRefresh("SKU Game Unlock", "sku_game_unlock_force_game_refresh_message", false);
	}
	
    public static void showDialog(LobbyGame unlockedGame, LobbyGame nextUnlockGame)
	{
		if (unlockedGame == null)
		{
			Debug.LogError("Attempting to show unlock game dialog without a valid game");
			return;
		}
		
		if (unlockedGame.isChallengeLobbyGame)
		{
			// Show a different dialog when unlocking a Land of Oz game.
			// We have to pass in LOOZ for theme instead of LOZ because
			// audio assets were created with LOOZ suffix.
			// TODO: This needs to be verified as to whiy challengelobby.instance is null (pretty sure it's destroyed on entering a game)
			// possibly need to replace with a system that can't be destroyed for fetching the lobbyAssetData.themename here...
			/*if (ChallengeLobby.instance == null) //This should only happen when going into games through the dev gui
			{
				return;
			}*/
			string suffix = unlockedGame.isLOZGame ? "LOOZ" : "SCS";
			GameUnlockedSimpleDialog.showDialog(unlockedGame, suffix);
			return;
		}

		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
		{
			//Disabling this dialog while in a royal rush event game
			return;
		}
		
		Dict args = Dict.create(
			D.OPTION, unlockedGame,
			D.OPTION1, nextUnlockGame
		);
		
		string imageSize = "1X2";
        
        List<string> filenames = new List<string>();
		filenames.Add(SlotResourceMap.getLobbyImagePath(unlockedGame.groupInfo.keyName, unlockedGame.keyName, imageSize));
		
		if (nextUnlockGame != null)
		{
			filenames.Add(SlotResourceMap.getLobbyImagePath(nextUnlockGame.groupInfo.keyName, nextUnlockGame.keyName, imageSize));
		}

		Dialog.instance.showDialogAfterDownloadingTextures("game_unlocked", nonMappedBundledTextures:filenames.ToArray(), args:args);

	}
}
