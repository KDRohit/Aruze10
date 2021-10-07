using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the dialog for showing progressive jackpot win celebration and sharing the news about it.
*/

public class ProgressiveJackpotDialogHIR : ProgressiveJackpotDialog
{
	public GameObject grandSprite;
	public GameObject largeSprite;
	public GameObject mediumSprite;
	public GameObject vipHeaderParent;
	public GameObject giantHeaderParent;

	public const string WHEEL_PATH = "progressive_jackpot/Progressive_Jackpot_Win_Wheel.png";

	public override void init()
	{
		JSON data = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
				
		ProgressiveJackpot jp = dialogArgs.getWithDefault(D.OPTION, null) as ProgressiveJackpot;
		game = dialogArgs.getWithDefault(D.GAME_KEY, null) as LobbyGame;

		if (game == null)
		{
			Debug.LogError("ProgressiveJackpotDialog: game is null");
		}
		else
		{
			bool isVIPJackpot = (jp == ProgressiveJackpot.vipJackpot);
			
			credits = data.getLong("credits", 0L);
			
			// If this is one of a multi-progressive, find out which one so we can tell the player.
			int size = -1;
			if (game.isMultiProgressive)
			{
				for (int i = 0; i < 3; i++)
				{
					if (game.progressiveJackpots[i] == jp)
					{
						size = i;
						break;
					}
				}

				if (size == -1)
				{
					Debug.LogError("ProgressiveJackpotDialog: Won multi-progressive jackpot wasn't found in the game's progressiveJackpots array!");
				}
				else
				{
					switch (size)
					{
						case 0:
							mediumSprite.SetActive(true);
							largeSprite.SetActive(false);
							grandSprite.SetActive(false);
							break;

						case 1:
							mediumSprite.SetActive(false);
							largeSprite.SetActive(true);
							grandSprite.SetActive(false);
							break;

						default:
							mediumSprite.SetActive(false);
							largeSprite.SetActive(false);
							grandSprite.SetActive(true);
							break;
					}
				}
			}
			else
			{
				mediumSprite.SetActive(false);
				largeSprite.SetActive(false);
				grandSprite.SetActive(false);
			}

			// Show or hide the VIP headers depending on whether this is the VIP jackpot.
			vipHeaderParent.SetActive(isVIPJackpot);
			giantHeaderParent.SetActive(game.isGiantProgressive);
			shareVipHeaderParent.SetActive(isVIPJackpot);
			shareHeaderParent.SetActive(!isVIPJackpot && !game.isGiantProgressive);
			shareGiantHeaderParent.SetActive(game.isGiantProgressive);

			friendInfo.member = SlotsPlayer.instance.facebook;
		
			// Keep everything hidden until the slide-in effect is done.
			celebrationParent.SetActive(false);
			shareParent.SetActive(false);
		
			poolAmountLabel.text = CreditsEconomy.convertCredits(credits);

			jp.reset();

			downloadedTextureToRenderer(gameTexture, 1);

			if (game.isGiantProgressive)
			{
				subheaderLabel.text = Localize.textUpper("you_won_giant_jackpot");
			}
			else
			{
				string jackpotName = game.name;
				if (isVIPJackpot)
				{
					jackpotName = Localize.text("vip");
				}
				subheaderLabel.text = Localize.textUpper("jackpot_you_won_{0}_progressive", jackpotName);
			}

			// Unblock the select game unlock if we were waiting to show the jackpot win.
			SelectGameUnlockDialog.isWaitingForProgressiveJackpot = false;
		}
	}
}
