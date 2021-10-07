using UnityEngine;
using System.Collections;
using TMPro;

/*
HIR version of this dialog.
*/

public class NeedCreditsSTUDDialogHIR : NeedCreditsSTUDDialog
{
	public override void init()
	{
		base.init();
		//WatchToEarn.init(WatchToEarn.REWARD_VIDEO);
	}

	protected override void logViewStats()
	{
		if (WatchToEarn.isEnabled && w2eOfferInfo != null)
		{
			string gameKey = "";
			if (GameState.game != null)
			{
				gameKey = GameState.game.keyName;
			}
			else
			{
				gameKey = "game_not_found";
			}
			StatsManager.Instance.LogCount("dialog", "out_of_coins", "watch_Ad", gameKey, "", "view");
		}
		else
		{
			base.logViewStats();
		}
	}
	
	protected override void buyClicked(Dict args = null)
	{
		if (WatchToEarn.isEnabled)
		{
			WatchToEarn.watchVideo();
			StatsManager.Instance.LogCount("dialog", "need_credits", "watch_Ad", StatsManager.getGameTheme(), StatsManager.getGameName(), "click");
			// Might as well just close the dialog here, since it's not a normal purchase.
			Dialog.close();
		}
		else
		{
			base.buyClicked(args);
		}
	}
}
