using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class LandOfOzMOTD : DialogBase
{
	[SerializeField] private ClickHandler closeHandler;
	[SerializeField] private ClickHandler playHandler;
	public override void init()
	{
		MOTDFramework.markMotdSeen(dialogArgs);
		Audio.play("DialogOpenLOOZ");
		closeHandler.registerEventDelegate(closeClicked);
		playHandler.registerEventDelegate(playClicked);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void playClicked(Dict args = null)
	{
		Dialog.close();
		DoSomething.now("loz_lobby");
		StatsManager.Instance.LogCount("dialog", "loz_motd", "", "", "play_now", "click");
	}

	private void closeClicked(Dict args = null)
	{
		Dialog.close();
	}
	
	public static bool showDialog(string motdKey = "")
	{
		Scheduler.addDialog("loz_new_motd", Dict.create(D.MOTD_KEY, motdKey));
		return true;
	}
	
	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Clean up here.
	}
}
