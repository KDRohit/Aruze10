using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
/*
 * This Class is for the linked vip program dialog
 * */

public class LinkedVIPPendingDialog: DialogBase
{
	[SerializeField] private TextMeshPro emailToCheck;
	[SerializeField] private TextMeshPro clickTheLinkText;
	
	[SerializeField] private ImageButtonHandler helpButton;
	[SerializeField] private ImageButtonHandler okayButton;
	[SerializeField] private ImageButtonHandler closeButton;

	//Initialization
	public override void init() 
	{
		helpButton.registerEventDelegate(networkHelpClicked);
		closeButton.registerEventDelegate(closeClicked);
		okayButton.registerEventDelegate(okayClicked);
		if ((dialogArgs != null) && dialogArgs.ContainsKey(D.DATA))
		{
			JSON jsonData = dialogArgs[D.DATA] as JSON;
			string email = jsonData.getString("network_state.email", "No email");
			emailToCheck.text = Localize.text("weve_sent_an_email_to_{0}_mobile", email);
		}
		helpButton.SetActive(LinkedVipProgram.instance.isHelpShiftActive);
		if (LinkedVipProgram.instance.incentiveCredits <= 0)
		{
			clickTheLinkText.text = Localize.text("click_link_linked_VIP_rewards");
		}
		else
		{
			clickTheLinkText.text = Localize.text("click_link_linked_VIP_{0}_mobile", CreditsEconomy.convertCredits(LinkedVipProgram.instance.incentiveCredits));
		}
		
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "", "view");
		
		Audio.playMusic("FeatureBgLL");
		Audio.switchMusicKey("FeatureBgLL");
		Audio.play("AlmostDoneFlourishLL");
	}

	//Check whether the dialog needs to be showed
	public static bool shouldShowDialog()
	{
		return LinkedVipProgram.instance.isEligible;
	}

	// Callback for the close button.
	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "intro_linked_vip", "", "close", "click");
		Dialog.close();
		Audio.play("QuitFeatureLL");
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic());
	}

	private void okayClicked(Dict args = null)
	{
		Dialog.close();
	}

	protected void networkHelpClicked(Dict args = null)
	{
		LinkedVipProgram.instance.openHelpUrl();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void showDialog(Dict dialogArgs = null)
	{
		Scheduler.addDialog("linked_vip_program_pending", dialogArgs);
	}
}



