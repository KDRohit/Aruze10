using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
/*
 * This Class is for the linked vip congrats dialog.
 * */

public class LinkedVIPCongrats: DialogBase
{
	[SerializeField] private TextMeshPro subtitleLabel;
	[SerializeField] private TextMeshPro coinValueLabel;
	[SerializeField] private ImageButtonHandler collectCoinsButton;
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler networkHelpButton;

	[SerializeField] private GameObject creditsParent;

	[SerializeField] private GameObject wozCheck;
	[SerializeField] private GameObject wonkaCheck;
	[SerializeField] private GameObject hirCheck;
	[SerializeField] private GameObject gotCheck;
	
	private bool canCollect = false;
	private string eventId = "";
	
	//Initialization
	public override void init() 
	{
		string email = (string)dialogArgs.getWithDefault(D.EMAIL, "");
		long incentiveCredits = LinkedVipProgram.instance.incentiveCredits;
		if (incentiveCredits > 0)
		{
			coinValueLabel.text = CreditsEconomy.convertCredits(incentiveCredits);
		}
		else
		{
			creditsParent.SetActive(false);
		}

		eventId = dialogArgs.getWithDefault(D.EVENT_ID, "") as string;
		eventId = eventId.Trim();

		networkHelpButton.registerEventDelegate(helpClicked);
		collectCoinsButton.registerEventDelegate(collectClicked);
		closeButton.registerEventDelegate(closeClicked);
		
		// Set the buttons based on whether or not there is an incentive to show.
		canCollect = incentiveCredits > 0 && !string.IsNullOrEmpty(eventId);
		collectCoinsButton.text = canCollect ? Localize.text("collect", "") : Localize.text("okay", "");
		subtitleLabel.text = Localize.text("you_are_connected_to_account_mobile_{0}", email);

		if (SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null &&
			SlotsPlayer.instance.socialMember.networkProfile != null)
		{

			NetworkProfile profile = SlotsPlayer.instance.socialMember.networkProfile;
			hirCheck.SetActive(true);
			wozCheck.SetActive(profile.isWozConnected);
			wonkaCheck.SetActive(profile.isWonkaConnected);
			gotCheck.SetActive(profile.isGotConnected);
		}
		
		MOTDFramework.markMotdSeen(dialogArgs);

		Audio.playMusic("FeatureBgLL");
		Audio.switchMusicKey("FeatureBgLL");

		Audio.play("AlmostDoneFlourishLL");

		StatsManager.Instance.LogCount ("dialog", "linked_vip", "success", "", "", "view");
	}

	// Callback for the close button.
	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip", "success", "", "close", "click");		
		closeDialog();
	}

	protected void helpClicked(Dict args = null)
	{
		LinkedVipProgram.instance.openHelpUrl();
	}

	protected void collectClicked(Dict args = null)
	{
		if (canCollect)
		{
			SlotsPlayer.addCredits(LinkedVipProgram.instance.incentiveCredits, "Linked VIP Signup");
			NetworkAction.acceptIncentiveCredits(eventId);
		}
		closeDialog();
	}	

	private void closeDialog()
	{
		Dialog.close();
		Audio.play("SummaryFanfareLL");
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic());
	}
	
	public static void showDialog(string email, string motdKey = "", string eventId = "")
	{
		Dict args = Dict.create(
			D.EMAIL, email,
			D.MOTD_KEY, motdKey,
			D.EVENT_ID, eventId
		);
		Scheduler.addDialog("linked_vip_program_congratulations", args);
	}
	
	//Check whether the dialog needs to be shown.
	public static bool shouldShowDialog()
	{
		return LinkedVipProgram.instance.isEligible;
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
}
