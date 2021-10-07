using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class SpinPanelV2MOTD : DialogBase
{
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler continueButton;

	/// Initialization
	public override void init()
	{
		closeButton.registerEventDelegate(onCloseClicked, Dict.create(D.DATA, "close"));
		continueButton.registerEventDelegate(onCloseClicked, Dict.create(D.DATA, "close"));
		MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "motd_spin_panel_v2"));

		StatsManager.Instance.LogCount(
			counterName : "dialog",
			kingdom : "hir_spin_panel_v2",
			genus : "view"
		);
	}

	private void onCloseClicked(Dict args = null)
	{
		
		Dialog.close();
	}


	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static bool showDialog()
	{
		Scheduler.addDialog("spin_panel_v2_motd");
		return true;
	}
}
