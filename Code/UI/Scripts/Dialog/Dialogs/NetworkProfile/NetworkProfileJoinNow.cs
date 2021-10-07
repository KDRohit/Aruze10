using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class NetworkProfileJoinNow : NetworkProfileTabBase
{
	public TextMeshPro unconnectedMessageLabel;
	public ImageButtonHandler joinNowButton; // Used when not-connected to Loyalty Lounge.
	private NetworkProfileDialog dialog;	


	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		StatsManager.Instance.LogCount("dialog", "ll_profile_anonymous_signup", "edit_profile", "view", "", member.networkID.ToString());
		yield return null;
	}
	
	public void init(SocialMember member, NetworkProfileDialog dialog)
	{
		this.member = member;
		this.dialog = dialog;
		joinNowButton.registerEventDelegate(joinNowClicked);
		
		string messageText = "";
		if (SlotsPlayer.instance.socialMember.experienceLevel >= Glb.LINKED_VIP_MIN_LEVEL)
		{
			messageText = Localize.text("connect_to_edit_profile");
			joinNowButton.label.text = Localize.text("join_now_ex");
		}
		else
		{
			messageText = Localize.text("level_up_and_connect_to_edit_profile_{0}", Glb.LINKED_VIP_MIN_LEVEL);
			joinNowButton.label.text = Localize.text("okay");
		}
		unconnectedMessageLabel.text = messageText;		
	}

	private void joinNowClicked(Dict args = null)
	{
		if (SlotsPlayer.instance.socialMember.experienceLevel >= Glb.LINKED_VIP_MIN_LEVEL)
		{
			Dialog.close();
			if (LinkedVipProgram.instance.isPending)
			{
				// If they are pending then show the status dialog instead.
				LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
			}
			else
			{
				// If they are elligible for Loyalty Lounge then send them to sign up.
				LinkedVipConnectDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
				dialog.shouldLogCloseStat = false;
				StatsManager.Instance.LogCount("dialog", "ll_profile_anonymous_signup", "edit_profile", "sign_up");
			}
		}
		else
		{
			// Otherwise go back to the profile display.			
			dialog.switchState(NetworkProfileDialog.ProfileDialogState.PROFILE_DISPLAY);
		}
	}	
}