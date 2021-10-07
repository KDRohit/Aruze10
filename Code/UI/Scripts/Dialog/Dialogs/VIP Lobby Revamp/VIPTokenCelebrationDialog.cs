using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class VIPTokenCelebrationDialog : DialogBase
{
	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("vip_celebration_dialog", args);
	}

	public override void init()
	{
		Audio.play("VIPAwardToken");
		if (Overlay.instance.jackpotMystery.tokenBar != null)
		{
			RoutineRunner.instance.StartCoroutine(Overlay.instance.jackpotMystery.tokenBar.waitThenHoldToken());
		}
	}

	public static void closeDialog()
	{
		Dialog.close();
	}

	public override void close()
	{

	}
}