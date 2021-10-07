using UnityEngine;
using System.Collections;
using System;
using CustomLog;
using TMPro;

/**
MOTDDialog

This dialog is the message-of-the-day.
It procedurally populates itself using data: including strings, background image and actions.
**/
public class MOTDDialog : DialogBase
{
	public TextMeshPro titleLabel;
	public TextMeshPro subheadingLabel;

	public TextMeshPro bodyTitleLabel;
	public TextMeshPro bodyMessageLabel;

	public TextMeshPro option1Label;
	public TextMeshPro option2Label;

	public Renderer background;

	public GameObject closeButton;

	public TextMeshProHyperLinker bodyHyperLinker;
	
	public GameObject lockedGameParent;
	public GameObject lockedGamePreviewParent;
	public TextMeshPro lockedGameLabel;
	public TextMeshPro unlockLevelLabel;
	public TextMeshPro previewLevelLabel;

	private string action1String;
	private string action2String;
	private string dialogKey;
	private LobbyGame game = null;
	private static DialogAudioPack audioPack = new DialogAudioPack();
	
	protected bool didInitFail = false;
	protected MOTDDialogData myData = null;

	private string statName;

	private const string SKU_GAME_UNLOCK_NAME_FORMAT = "{0}_game_unlock_{1}";
	private const string SKU_GAME_UNLOCK_PHYLUM_FORMAT = "{0}_game_unlock";

	// ==== Pulling in variables from the HIR file to consolidate
	private const string AMBER_BUTTON_SPRITE_NAME = "Button CTA Red 00 Stretchy";
	[SerializeField] private Material amberFontMaterial;

	public ButtonHandler option1Button; // Linking the UIImageButton for cases where the sprites should change.
	public ButtonHandler option2Button; // Linking the UIImageButton for cases where the sprites should change.
	// ==== End

	public static string getSkuGameUnlockName()
	{
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		
		if (skuGameUnlock != null)
		{
			return string.Format(SKU_GAME_UNLOCK_NAME_FORMAT, skuGameUnlock.xp.xpromoTarget, skuGameUnlock.keyName);
		}
		
		return "";
	}
	
	public static string getSkuGameUnlockPhylum()
	{
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		
		if (skuGameUnlock != null)
		{
			return string.Format(SKU_GAME_UNLOCK_PHYLUM_FORMAT, skuGameUnlock.xp.xpromoTarget);
		}
		
		return "";
	}
	
	/// Initialization
	/// Adding more safety and breadcrumbs to try to prevent and find a crash that Crittercism reported.
	public override void init()
	{
		Bugsnag.LeaveBreadcrumb("MotD Dialog init");
		didInitFail = false;
		
		if (dialogArgs == null)
		{
			didInitFail = true;
			Debug.LogError("MotD dialog args are null.");
			return;
		}
		
		myData = dialogArgs.getWithDefault(D.DATA, null) as MOTDDialogData;
		if (myData == null)
		{
			// Error
			didInitFail = true;
			Debug.LogError("MotD dialog data is null.");
			return;
		}
		else
		{
			//playAudioFromEos(audioPack.getAudioKey(DialogAudioPack.OPEN));
			//playAudioFromEos(audioPack.getAudioKey(DialogAudioPack.MUSIC));
		}

		dialogKey = myData.keyName;
		Bugsnag.LeaveBreadcrumb("MotD dialog key is " + myData.keyName);
		
		statName = myData.statName;
		if (!downloadedTextureToRenderer(background, 0))
		{
			if (!dialogKey.Contains("new_game"))
			{
				background.gameObject.SetActive(false);
			}
		}

		// Title:
		if (titleLabel == null || subheadingLabel == null)
		{
			didInitFail = true;
			Debug.LogError("MotD title is null.");
			return;
		}
		setLocLabel(titleLabel, myData.locTitle);
		setLocLabel(subheadingLabel, myData.locSubheading);

		// Body:
		if (bodyTitleLabel == null || bodyMessageLabel == null)
		{
			didInitFail = true;
			Debug.LogError("MotD body is null.");
			return;
		}

		action1String = myData.commandAction1;
		action2String = myData.commandAction2;
		
		setLocLabel(bodyTitleLabel, myData.locBodyTitle);
		setLocLabel(bodyMessageLabel, myData.locBodyText);

		// button labels:
		if (option1Label == null || option2Label == null)
		{
			didInitFail = true;
			Debug.LogError("MotD button labels are null.");
			return;
		}

		setLocLabel(option1Label, myData.locAction1);
		setLocLabel(option2Label, myData.locAction2);
		
		if (closeButton == null)
		{
			didInitFail = true;
			Debug.LogError("MotD close button is null.");
			return;
		}
		if (myData.isCloseHidden && closeButton != null)
		{
			closeButton.SetActive(false);
		}

		// Functionality:
		if (action1String == null || action2String == null)
		{
			didInitFail = true;
			Debug.LogError("MotD action strings are null.");
			return;
		}
		
		// Set these things inactive by default.
		if (lockedGameParent == null || lockedGamePreviewParent == null)
		{
			didInitFail = true;
			Debug.LogError("MotD game parents are null.");
			return;
		}
		lockedGameParent.SetActive(false);
		lockedGamePreviewParent.SetActive(false);

		game = myData.action1Game;
		if (game != null)
		{
			Bugsnag.LeaveBreadcrumb("MotD game is " + game.keyName);
			StatsManager.Instance.LogCount("dialog", "new_game_normal", "", "", game.keyName, "view");

			MOTDDialogDataNewGame newGameData = MOTDDialogData.newGameMotdData;
			if (newGameData != null)
			{
				newGameData.markNewGameSeen(game);
			}
			
			Bugsnag.LeaveBreadcrumb("MotD game is locked?");
						
			if (game.xp == null || game.isUnlocked)
			{
				Bugsnag.LeaveBreadcrumb("MotD game is unlocked.");
				
				if (game.xp == null && !game.isProgressive)
				{
					Bugsnag.LeaveBreadcrumb("MotD game is a free preview.");
					
					SafeSet.gameObjectActive(lockedGamePreviewParent, true);
					
					if (previewLevelLabel == null)
					{
						didInitFail = true;
						Debug.LogError("MotD preview level label is null.");
						return;
					}
					previewLevelLabel.text = CommonText.formatNumber(game.unlockLevel);
				}
			}
			else
			{
				// Game is locked.
				
				Bugsnag.LeaveBreadcrumb("MotD game locked.");
				
				// Only show the lock if it's a new game MotD.
				if (myData.locTitle == "new_game")
				{
					SafeSet.gameObjectActive(lockedGameParent, true);
					
					if (lockedGameLabel == null || unlockLevelLabel == null || option1Label == null)
					{
						didInitFail = true;
						Debug.LogError("MotD game lock labels are null.");
						return;
					}

					lockedGameLabel.text = Localize.textUpper("reach_level_{0}_to_unlock", CommonText.formatNumber(game.unlockLevel));
					unlockLevelLabel.text = CommonText.formatNumber(game.unlockLevel);
				}
				
				// If the game is locked, then make the button just "OK" and do nothing but close the dialog.
				action1String = "";
				setLocLabel(option1Label, "ok");
			}
		}
		
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		if (action1String == "sku_game_unlock" && skuGameUnlock != null)
		{
			StatsManager.Instance.LogCount("dialog", "xpromo", getSkuGameUnlockPhylum(), "game_unlock_intro", "", "view");
		}
		
		StatsManager.Instance.LogCount("dialog", dialogKey, statName, "", "", "view");
		Bugsnag.LeaveBreadcrumb("MotD init completed.");

		// Pulling in the HIR file init functions after consolidating
		if (didInitFail)
		{
			return;
		}

		if (option1Button == null || option2Button == null)
		{
			didInitFail = true;
			Debug.LogError("MotD buttons are null.");
			return;
		}

		if (!setLocLabel(option2Label, myData.locAction2))
		{
			option2Button.gameObject.SetActive(false);
			CommonTransform.setX(option1Button.transform, 0.0f);
		}

		if (option1Button != null)
		{
			option1Button.registerEventDelegate(clickOption1);
		}

		if (option2Button != null)
		{
			option2Button.registerEventDelegate(clickOption2);
		}

		string[] appearanceOptions = myData.appearance.Split(',');
		for (int i = 0; i < appearanceOptions.Length; i++)
		{
			string token = appearanceOptions[i];
			if (token.Equals("amberOptionOne"))
			{
				option1Button.sprite.spriteName = AMBER_BUTTON_SPRITE_NAME;
				option1Button.label.fontMaterial = amberFontMaterial;
				// dis/re-abling to force the change to show.
				option1Button.enabled = false;
				option1Button.enabled = true;
			}
			if (token.Equals("amberOptionTwo"))
			{
				option2Button.sprite.spriteName = AMBER_BUTTON_SPRITE_NAME;
				option2Button.label.fontMaterial = amberFontMaterial;
				// dis/re-abling to force the change to show.
				option2Button.enabled = false;
				option2Button.enabled = true;
				continue;
			}
		}

		if (myData.keyName.Contains("new_game") ||
			myData == MOTDDialogDataDynamic.instance)
		{
			// If this is the dyanmic MOTD, or a new_game MOTD under lola, then do not mark the MOTD as seen,
			// as they are not keys that exist in SCAT
			return;
		}
		else
		{
			MOTDFramework.markMotdSeen(dialogArgs);
		}
		// END
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		if (didInitFail)
		{
			Bugsnag.LeaveBreadcrumb("MotD init failed.");
			Dialog.close();
		}
	}
	
	void Update()
	{
		if (closeButton.activeSelf)
		{
			// If the close button is active, then we listen for the back button.
			AndroidUtil.checkBackButton(clickClose);
		}
	}

	// Localize (or hide) the given label:
	protected bool setLocLabel(TextMeshPro label, string locKey, bool toUpper = false)
	{
		if (!string.IsNullOrEmpty(locKey))
		{
			string text = "";
			if (toUpper)
			{
				text = Localize.textUpper(locKey);
			}
			else
			{
				text = Localize.text(locKey);
			}
			
			if (label == bodyMessageLabel)
			{
				if (dialogKey == "watch_to_earn")
				{
					// Special case for Watch To Earn MOTD, which has a variable in the body text
					text = Localize.text(locKey, CreditsEconomy.convertCredits(WatchToEarn.rewardAmount));
				}
				// Special case for body text, which may use hyper links.
				bodyHyperLinker.label.text = text;
				// This is some test text that contains hyperlinks:
//				bodyHyperLinker.setLabelText("Zynga has updated our <link action=\"url_terms\">Terms of Service</link> and <link action=\"url_privacy\">Privacy Policy</link>. Please read both of these documents as the changes affect your legal rights. <link action=\"game:oz00\">Play Oz</link>");
			}
			else
			{
				label.text = text;
			}
			
			return true;
		}
		label.gameObject.SetActive(false);
		return false;
	}
	
	// NGUI button callback for clicking a hyperlink in text.
	private void hyperLinkClicked(GameObject go)
	{
		HyperLinkData data = go.GetComponent<HyperLinkData>();
		if (data != null)
		{
			if (data.action.StartsWith(DoSomething.GAME_PREFIX))
			{
				// If launching a game, close this dialog.
				// Leave it open for all other actions for now.
				Dialog.close();
			}
			DoSomething.now(data.action);
		}
	}

	public void clickOption1(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", dialogKey, statName, action1String, "click");
		
		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;
		if (action1String == "sku_game_unlock" && skuGameUnlock != null)
		{
			StatsManager.Instance.LogCount("dialog", "xpromo", getSkuGameUnlockPhylum(), "game_unlock_intro", "", "click");
		}
		
		dialogArgs.merge(D.ANSWER, "1");	// The user selected option 1.
		Dialog.close();
		DoSomething.now(action1String);
		//playAudioFromEos(audioPack.getAudioKey(DialogAudioPack.OK));
	}

	public void clickOption2(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", dialogKey, statName, action2String, "click");
		dialogArgs.merge(D.ANSWER, "2");	// The user selected option 2.
		Dialog.close();
		DoSomething.now(action2String);
		//playAudioFromEos(audioPack.getAudioKey(DialogAudioPack.OK));
	}
	
	public void clickClose()
	{
		//playAudioFromEos(audioPack.getAudioKey(DialogAudioPack.CLOSE));
		StatsManager.Instance.LogCount("dialog", dialogKey, statName, "close", "click");
		dialogArgs.merge(D.ANSWER, "no");	// The user selected neither option.
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		Destroy(background.material.mainTexture);
		
		if (game != null)
		{
			StatsManager.Instance.LogCount("dialog", "new_game_normal", "", "", game.keyName, "close");
		}
	}
	
	public static bool showDialog(MOTDDialogData myData)
	{
		/*if (!string.IsNullOrEmpty(myData.audioPackKey))
		{
			audioPack = new DialogAudioPack(myData.audioPackKey);
			audioPack.addAudio(DialogAudioPack.CLOSE, myData.soundClose);
			audioPack.addAudio(DialogAudioPack.OPEN, myData.soundOpen);
			audioPack.addAudio(DialogAudioPack.OK, myData.soundOk);
			audioPack.addAudio(DialogAudioPack.MUSIC, myData.soundMusic);
			audioPack.preloadAudio();
		}*/
		
		Dict args = Dict.create
		(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.DATA, myData,
			D.MOTD_KEY, myData.keyName
		);
		
		if (string.IsNullOrEmpty(myData.imageBackground))
		{
			Debug.LogErrorFormat("MOTDDialog::showDialog - Empty background texture setting for MOTDDialogData key: {0}", myData.keyName);
			return false;
		}
		else
		{
			Dialog.instance.showDialogAfterDownloadingTextures("motd", myData.imageBackground, args);
			return true;
		}
	}
	
	public static void clearAllSeenDialogs()
	{	
	
	}
}
