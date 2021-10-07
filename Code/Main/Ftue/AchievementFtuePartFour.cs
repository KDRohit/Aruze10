using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementFtuePartFour : FtueBase
{
	public AchievementFtuePartFour()
	{
	}

	public override void Awake()
	{
		base.Awake();
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "view_ranking", "view", "", SlotsPlayer.instance.networkID, 5);
	}

	public override void ButtonClick(Dict args)
	{
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "view_ranking", "click", "", SlotsPlayer.instance.networkID, 5);
		Destroy (FTUEManager._go);
		NetworkProfileDialog networkProfileDialog = (NetworkProfileDialog)Dialog.instance.currentDialog;
		if (networkProfileDialog != null)
		{
			networkProfileDialog.profileDisplay.hideRankTooltip();
			networkProfileDialog.changeTab (NetworkProfileDialog.PageTabTypes.ACHIEVEMENTS);
			networkProfileDialog.switchState (NetworkProfileDialog.ProfileDialogState.ACHIEVEMENTS);
		}
	}

	public override void TabClick(Dict args)
	{
		Destroy (FTUEManager._go);
	}

}

