using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementFtuePartThree : FtueBase
{
	private const string TROPHY_FAVORITE_BUTTON = "trophy_favorite_button";
	private const string FAVORITE_FRAME_TEXT = "favorite_frame_text";

	public AchievementFtuePartThree()
	{
	}

	public override void Awake()
	{
		base.Awake();
		buttonHandler.text = Localize.text(TROPHY_FAVORITE_BUTTON);
		ftueText.text = Localize.text(FAVORITE_FRAME_TEXT);
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "view_favorite_trophy", "view", "", SlotsPlayer.instance.networkID, 4);
	}

	public override void ButtonClick(Dict args)
	{
		OnClickButton();
	}

	public override void TabClick(Dict args)
	{
		OnClickButton();
	}


	private void OnClickButton()
	{
		StatsManager.Instance.LogCount("dialog", "ll_trophy_ftue", "view_favorite_trophy", "click", "", SlotsPlayer.instance.networkID, 4);
		Destroy (FTUEManager._go);
		if (Dialog.instance.currentDialog.GetType() == typeof(NetworkProfileDialog))
		{	
			NetworkProfileDialog networkProfileDialog = (NetworkProfileDialog)Dialog.instance.currentDialog;
			if (networkProfileDialog != null && networkProfileDialog.profileDisplay != null)
			{
				networkProfileDialog.profileDisplay.rankClicked();
			}
			FTUEManager.Instance.StartFtue (FTUEManager.ACHIEVEMENT_FTUE_STEP_4);
		} 
		else 
		{
			Debug.LogErrorFormat ("Expecting NetworkProfile Dialog but got {0}", Dialog.instance.currentDialog.GetType().ToString());
		}
	}
}

