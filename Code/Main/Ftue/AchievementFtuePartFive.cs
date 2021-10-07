using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementFtuePartFive : FtueBase
{
	public AchievementFtuePartFive()
	{
	}

	public override void Awake()
	{
		base.Awake();
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "favorite_trophy", "view", "", SlotsPlayer.instance.networkID, 3);
	}

	public override void ButtonClick(Dict args)
	{
		Destroy (FTUEManager._go);
	}

	public override void TabClick(Dict args)
	{
		Destroy (FTUEManager._go);
	}

}

