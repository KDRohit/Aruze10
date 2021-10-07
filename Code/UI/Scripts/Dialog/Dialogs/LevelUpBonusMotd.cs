using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

class LevelUpBonusMotd : DialogBase
{
	public Renderer backgroundRenderer = null;
	public TextMeshPro timerLabel = null;
	public TextMeshPro patternLabel = null;
	public TextMeshPro patternShadowLabel;
	public TextMeshPro multiplierLabel;
	public TextMeshPro multiplierShadowlabel;
	public ButtonHandler okButton;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		StatsManager.Instance.LogCount("dialog", "level_up_event", "bonus_level_up_coins", "", "", "view");
		okButton.registerEventDelegate(okClicked);
	}

	public void Update()
	{
		if (timerLabel != null)
		{
			timerLabel.text = Localize.text("expires_in_{0}", LevelUpBonus.timeRange.timeRemainingFormatted);
		}
		AndroidUtil.checkBackButton(okClicked);
	}

	public void okClicked(Dict args = null)
	{
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "level_up_event", "bonus_level_up_coins", "", "", "click");
	}
	
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}
	
	public static bool showDialog(string key = "")
	{
		Scheduler.addDialog("level_up_bonus_motd", Dict.create(D.MOTD_KEY, key));
		return true;
	}
}
