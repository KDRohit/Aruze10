/*
Class: NetworkStatusDialog
Description: Shows status of Loyalty Lounge (connected/pending/need to sign up)
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
*/

using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class LinkedVipStatusDialog: DialogBase
{
	[SerializeField] private TextMeshPro connectedEmailLabel;
	[SerializeField] private TextMeshPro currentEmailLabel;

	[SerializeField] private GameObject connectedParent;
	[SerializeField] private GameObject pendingParent;
	[SerializeField] private GameObject anonymousParent;

	// Common Status Buttons
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private ImageButtonHandler helpButton;
	// Connected Status Buttons
	[SerializeField] private ImageButtonHandler disconnectButton;
	[SerializeField] private ImageButtonHandler okayButton;
	// Pending Status Buttons
	[SerializeField] private ImageButtonHandler resendEmailButton;
	[SerializeField] private ImageButtonHandler cancelPendingButton;
	// Anonymous Status Button
	[SerializeField] private ImageButtonHandler connectButton;


	[SerializeField] private GameObject wozCheck;
	[SerializeField] private GameObject wonkaCheck;
	[SerializeField] private GameObject hirCheck;
	[SerializeField] private GameObject gotCheck;

	public override void init()
	{
		helpButton.registerEventDelegate(networkHelpClicked);
		closeButton.registerEventDelegate(closeClicked);
		disconnectButton.registerEventDelegate(disconnectClicked);
		okayButton.registerEventDelegate(okayClicked);
		connectButton.registerEventDelegate(connectClicked);
		resendEmailButton.registerEventDelegate(resendClicked);
		cancelPendingButton.registerEventDelegate(cancelClicked);
		
		string email = (string)dialogArgs.getWithDefault(D.EMAIL, "none");
		updateButtons(email);

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
		
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "", "", "", "view");

		Audio.playMusic("FeatureBgLL");
		Audio.switchMusicKey("FeatureBgLL");
	}

	public override void close()
	{
		// Clean up before close here. Called by Dialog.cs do not call directly.
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	// Callback for the close button.
	private void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "", "", "close", "click");
		Dialog.close();
		Audio.play("QuitFeatureLL");
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic(1.0f));
	}

	private void okayClicked(Dict args = null)
	{
		closeClicked();
	}

	public void networkHelpClicked(Dict args = null)
	{
		LinkedVipProgram.instance.openHelpUrl();
	}

	// Callback for the connect button.
	private void connectClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "connect_network", "", "theme", "click");
		// Spawn the network connect dialog.
		LinkedVipConnectDialog.showDialog();
		Dialog.close();
		RoutineRunner.instance.StartCoroutine(Glb.restoreMusic());
	}

	// Callback for the disconnect button.
	private void disconnectClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "log_out_network", "", "theme", "click");
		Dialog.close();
		if (ExperimentWrapper.ZisPhase2.isInExperiment)
		{
			ZisSignOutDialog.showDialog(Dict.create(
				D.TITLE, ZisSignOutDialog.DISCONNECTED_HEADER_LOCALIZATION
			));
		}
		else
		{
			Server.registerEventDelegate("network_disconnect", networkCallback);
		}
		NetworkAction.disconnectNetwork();
	}

	// Callback for the resend authorization button.
	private void resendClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "resend_email_network", "", "theme", "click");
		Server.registerEventDelegate("network_connect", networkCallback);
		NetworkAction.networkResendAuthorization();
	}

	// Callback for the network cancel button.
	private void cancelClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount ("dialog", "linked_vip_status", "cancel_network", "", "theme", "click");
		Server.registerEventDelegate("network_cancel_pending_authorization", networkCallback);
		NetworkAction.cancelPendingNetwork();
	}

	private void networkCallback(JSON data)
	{
		string email = (string)data.getString("network_state.email", "no email");
		string status = (string)data.getString("network_state.status", "none");
		LinkedVipProgram.instance.updateNetworkStatus(status);
		updateButtons(email);
	}

	// Goes through the buttons an enables/disabled them based on the network state.
	private void updateButtons(string email)
	{
		if ((anonymousParent == null) || (anonymousParent.gameObject == null))
		{
			// KrisG: I saw an instance where this got called in LateUpdate from server-msg-callback 
			// after whole dialog object has been destroyed(?) this frame, in that case no buttons need updating, 
			// so throwing this in to prevent null exception
			return;
		}

		currentEmailLabel.text = email;
		connectedEmailLabel.text = Localize.text("you_are_connected_to_account_mobile_{0}", email);
		
		// Changed the button enabled-ness based on the state.
		anonymousParent.gameObject.SetActive(LinkedVipProgram.instance.currentState == LinkedVipProgram.NetworkState.NONE);
		connectedParent.gameObject.SetActive(LinkedVipProgram.instance.currentState == LinkedVipProgram.NetworkState.CONNECTED);
		pendingParent.gameObject.SetActive(LinkedVipProgram.instance.currentState == LinkedVipProgram.NetworkState.PENDING);
	}

	// Checks what the network state is and shows the dialog
	public static void checkNetworkStateAndShowDialog()
	{
		LinkedVipProgram.instance.updateStatusAndOpenDialog("status_dialog");
	}

	public static void showDialog(string email)
	{
		Dict args = Dict.create(D.EMAIL, email);
		Scheduler.addDialog("linked_vip_status", args, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
