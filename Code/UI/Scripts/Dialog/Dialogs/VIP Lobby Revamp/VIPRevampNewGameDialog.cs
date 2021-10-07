using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using TMPro;

public class VIPRevampNewGameDialog : DialogBase
{
	public MeshRenderer gameImage;
	public UISprite icon;
	public TextMeshPro vipTierLabel;
	public TextMeshPro gameTextLabel;
	private const string EXCLUSIVE_TEXT = "exclusive";
	
	public override void init()
	{
		string gameKey = LoLa.vipRevampNewGameKey;
		LoLaGame game = LoLaGame.find(gameKey);
		
 		if (game != null && game.game != null)
		{
			VIPLevel level = game.game.vipLevel;
			string keyName = "";
			if (level != null)
			{
				keyName =  level.keyName;

				if (!string.IsNullOrEmpty(keyName))
				{
					Regex reg = new Regex(@"vip_new_(\d+)_");
					keyName = reg.Replace(keyName, "");
					icon.spriteName = keyName;
					vipTierLabel.text = string.Format("{0} {1}", level.name.ToUpper(), Localize.text(EXCLUSIVE_TEXT));
				}
				else
				{
					Debug.LogError("VIPRevampNewGameDialog::init - missing the vip level key name on the game we're trying to load");
				}
			}
			else
			{
				Debug.LogError("VIPRevampNewGameDialog::init - missing the vip level on the game we're trying to load");
			}
		}

		if (gameTextLabel != null)
		{
			gameTextLabel.text = Localize.text(string.Format("motd_{0}_body_text", gameKey));
		}

		downloadedTextureToRenderer(gameImage, 0);
		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void playClicked()
	{
		Dialog.close();
		DoSomething.now("vip_lobby");
		StatsManager.Instance.LogCount("dialog", "vip_revamp_motd", "", "", "play_now", "click");
	}

	private void closeClicked()
	{
		Dialog.close();
	}
	
	public static bool showDialog(string motdKey = "")
	{
		string imagePath = LoLa.vipRevampNewGameKey;
		
		Dialog.instance.showDialogAfterDownloadingTextures(
			"vip_new_lobby_game",
			string.Format(MOTDDialogDataNewGame.NEW_GAME_MOTD_PNG, imagePath),
			Dict.create(D.MOTD_KEY, motdKey)
		);
		return true;
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Clean up here.
	}
}
