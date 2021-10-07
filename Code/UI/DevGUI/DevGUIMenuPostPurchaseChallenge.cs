using Com.Scheduler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevGUIMenuPostPurchaseChallenge : DevGUIMenu
{
	public override void drawGuts()
	{
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		
		bool featureEnabled = campaign?.isEnabled ?? false;


		GUIStyle redStyle = new GUIStyle();
		redStyle.normal.textColor = Color.red;
		
		GUIStyle greenStyle = new GUIStyle();
		greenStyle.normal.textColor = Color.green;
		
		GUILayout.BeginVertical();	
		
		
		GUILayout.Label("Feature Enabled: " + featureEnabled);
		if (featureEnabled)
		{
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();

			GUILayout.Label("Campaign is locked: " + campaign.isLocked);
			GUILayout.Label("Purchase time left: " + CommonText.formatTimeSpanAbrreviated(new System.TimeSpan(0,0,0,campaign.purchaseTimeRemaining)));
			GUILayout.Label("Campaign time left: " + CommonText.formatTimeSpanAbrreviated(new System.TimeSpan(0,0,0,campaign.runningTimeRemaining)));
			GUILayout.Label("Reminder time left: " + CommonText.formatTimeSpanAbrreviated(new System.TimeSpan(0,0,0,campaign.reminderTimeRemaining)));
			
			
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			
			if (GUILayout.Button("Show Dialog"))
			{
				Dict args = Dict.create(D.OPTION, campaign);
				Scheduler.addDialog(campaign.campaignID + "_dialog", args, SchedulerPriority.PriorityType.IMMEDIATE);
				DevGUI.isActive = false;
			}

#if !ZYNGA_PRODUCTION
			if (!campaign.isRunning)
			{
				if (GUILayout.Button("Fake Purchase (client only simulation)"))
				{
					campaign.devUnlock(Common.SECONDS_PER_MINUTE * 5);	
					DevGUI.isActive = false;
				}
				
				if (GUILayout.Button("Dev Action Purchase"))
				{
					ServerAction action = new ServerAction(ActionPriority.HIGH, "purchase_challenge");
					DevGUI.isActive = false;
				}
			}
			if (GUILayout.Button("End Running Timer in 10 seconds (client only simulation)"))
			{
				campaign.devEndTimerInSeconds(10);
				DevGUI.isActive = false;
			}

			if (GUILayout.Button("End Reminder Timer in 10 seconds (client only simulation)"))
			{
				campaign.devEndReminderTimerInSecons(10);
				DevGUI.isActive = false;
			}
#endif
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			
		}
		GUILayout.EndVertical();
	}
}
