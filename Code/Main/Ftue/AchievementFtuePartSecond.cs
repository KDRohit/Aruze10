using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AchievementFtuePartSecond : FtueBase
{
	private const string TROPHY_ROOM_FRAME = "trophy_room_frame";
	private const string TROPHY_ROOM_BUTTON = "trophy_room_button";

	public AchievementFtuePartSecond()
	{
	}

	public override void Awake()
	{
		base.Awake();
		ftueText.text = Localize.text(TROPHY_ROOM_FRAME);
		buttonHandler.text = Localize.text(TROPHY_ROOM_BUTTON);
		if (ProfileAchievementsTab.instance != null)
		{
			ProfileAchievementsTab.instance.enableSKUSelect(false);
		}
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "first_trophy", "view", "", SlotsPlayer.instance.networkID, 2);
	}

	public override void ButtonClick(Dict args)
	{
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "first_trophy", "skip", "", SlotsPlayer.instance.networkID, 2);
		if (ProfileAchievementsTab.instance != null)
		{
			ProfileAchievementsTab.instance.enableSKUSelect(true);
		}
		Destroy (FTUEManager.Instance.Go);
	}

	public override void TabClick(Dict args)
	{
		string id = SlotsPlayer.instance != null ?  SlotsPlayer.instance.networkID : "null";
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "first_trophy", "click", "", id, 2);

		if (FTUEManager.Instance != null && FTUEManager.Instance.Go != null)
		{
			Destroy(FTUEManager.Instance.Go);
		}
		
		if (ProfileAchievementsTab.instance != null)
		{
			ProfileAchievementsTab.instance.enableSKUSelect(true);
			List<Achievement> achievementList = ProfileAchievementsTab.instance.getCurrentList();
			Achievement sampleAchievement = null;
			if (achievementList != null && achievementList.Count > 0)
			{
				sampleAchievement = achievementList[0];
			}
			Dict achievementDict = Dict.create(
				D.INDEX, 0,
				D.ACHIEVEMENT, sampleAchievement
			);

			if (NetworkProfileDialog.instance != null)
			{
				NetworkProfileDialog.instance.showSpecificTrophy(achievementDict);
			}
		}
	}
}

