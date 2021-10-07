using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

class LandOfOzAchievementsUpdatedDialog : DialogBase
{
	[SerializeField] private ImageButtonHandler closeHandler;
	[SerializeField] private ClickHandler playHandler;
	public override void init()
	{
		closeHandler.registerEventDelegate(closeClicked);
		playHandler.registerEventDelegate(closeClicked);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	// NGUI button callback
	private void closeClicked(Dict args = null)
	{
		Dialog.close();
//		StatsManager.Instance.LogCount("dialog", "level_up_event", "bonus_level_up_coins", "", "", "click");
	}
	
	
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	protected override void playOpenSound()
	{
		Audio.play("DialogueOpenLOOZ");
		Audio.play("ObjectiveClearSingleLOOZ");
	}

	public override void playCloseSound()
	{
		Audio.play("DialogueCloseLOOZ");
		Audio.play("UnlockNewGameCollectLOOZ");
	}
	
	public static void showDialog()
	{
		Scheduler.addDialog("loz_achievements_updated");
	}
}
