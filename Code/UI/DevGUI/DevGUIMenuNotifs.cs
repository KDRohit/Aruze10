using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/*
A dev panel.
*/
using Zynga.Core.Util;

public class DevGUIMenuNotifs : DevGUIMenu
{
	private string zid = PlayerPrefsCache.GetString(DebugPrefs.DEVGUI_NOTIF_REMEBER_ZID);
	private static PreferencesBase _prefs = null;
	
	private static PreferencesBase prefs
	{
		get
		{
			if (_prefs == null)
			{
				_prefs = SlotsPlayer.getPreferences();
			}

			return _prefs;
		}
	}

	private void SaveZid(string newZid)
	{
		zid = newZid;
		prefs.SetString(DebugPrefs.DEVGUI_NOTIF_REMEBER_ZID, zid);
		prefs.Save();
	}

	public override void drawGuts()
	{	
		GUILayout.Label("========== Push Notifications =============");
		GUILayout.Label(string.Format("Experiment-PushNotificationSoftPrompt : {0}", ExperimentWrapper.PushNotifSoftPrompt.isInExperiment));
		GUILayout.Label(string.Format("Experiment-PushNotificationSoftPrompt-incentiveEnabled : {0}", ExperimentWrapper.PushNotifSoftPrompt.isIncentivizedPromptEnabled));
		GUILayout.Label(string.Format("Pust Notif Incentive Amount : {0}", IncentivizedSoftPromptDialog.creditAmount));
		GUILayout.Label(string.Format("RegisterPNAttempted : {0}", NotificationManager.RegisterPNAttempted));
		GUILayout.Label(string.Format("Device Enabled : {0}", NotificationManager.DevicePushNotifsEnabled));
		GUILayout.Label(string.Format("PushNotifsAnswered : {0}", NotificationManager.PushNotifsAnswered));
		GUILayout.Label(string.Format("Soft Prompt Date : {0}", PlayerPrefsCache.GetString(Prefs.SOFT_NOTIFICATION_PROMPT, "Never")));
		GUILayout.Label(string.Format("PushNotifsAllowed : {0}", NotificationManager.PushNotifsAllowed));
		GUILayout.Label(string.Format("LocalNotifsAllowed : {0}", NotificationManager.LocalNotifsAllowed));
		GUILayout.Label(string.Format("RegisteredForPushNotifications : {0}", NotificationManager.RegisteredForPushNotifications));
		GUILayout.Label(string.Format("Device Token : {0}", NotificationManager.storedDeviceToken));

		if (ExperimentWrapper.PushNotifSoftPrompt.isInExperiment)
		{
			System.DateTime lastPromptDateTime = System.DateTime.MinValue;
		
			string dateTimeString = prefs.GetString(Prefs.SOFT_NOTIFICATION_PROMPT, null);
			if (string.IsNullOrEmpty(dateTimeString) == false)
			{
				long fileTime = 0;
				if (!long.TryParse(dateTimeString, out fileTime))
				{
					Debug.LogWarning("No soft prompt file");
				}
				lastPromptDateTime = System.DateTime.FromFileTime(fileTime);
			}
		
			float seconds = (float)System.DateTime.Now.Subtract(lastPromptDateTime).TotalSeconds;
			float timeUntilPrompt = ExperimentWrapper.PushNotifSoftPrompt.cooldown - seconds;
			if (timeUntilPrompt < 0)
			{
				timeUntilPrompt = 0;
			}
			GUILayout.Label(string.Format("Time until push notif cooldown expires: {0}", timeUntilPrompt));	
		}
		

		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("PN Soft Prompt"))
			{
				SoftPromptDialog.showDialog();
				DevGUI.isActive = false;
			}
			if (GUILayout.Button("PN Incentive Prompt"))
			{
				IncentivizedSoftPromptDialog.showDialog();
				DevGUI.isActive = false;
			}

			if (GUILayout.Button("Reset cooldown"))
			{
				System.DateTime epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
				prefs.SetString(Prefs.SOFT_NOTIFICATION_PROMPT, epoch.ToString());
				prefs.Save();
			}

			if (GUILayout.Button("Call callback"))
			{
				IncentivizedSoftPromptDialog.onEnableFromPrompt();
			}

#if !ZYNGA_PRODUCTION
			if (GUILayout.Button("Resent Incentive Eligibility"))
			{
				IncentivizedPushNotificationAction.devResetPushNotifIncentive();
			}
#endif
			if (GUILayout.Button("PushNotificationAllowOnEnv3"))
			{
				NotificationManager.AllowEnv3PushNotifs = true;
			}
#if UNITY_IPHONE
			if (GUILayout.Button("Goto Settings (ios8 or higher)"))
			{
				NativeBindings.OpenSettings();
			}
#endif
#if !UNITY_EDITOR
			if (GUILayout.Button("Force Registration"))
			{
				NotificationManager.DeRegisterFromPN();
				NotificationManager.RegisteredForPushNotifications = false;
				NotificationManager.RegisterPNAttempted = false;
				NotificationManager.Instance.RegisterForPushNotifications();
			}
#endif
		}
		GUILayout.EndHorizontal();	

		GUILayout.Label("=== Social Push Notifications ===");

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label("Target ZID : ");
			string newZid = GUILayout.TextField(zid);
			if (newZid != zid)
			{
				SaveZid(newZid);
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{

			if (GUILayout.Button("StartPlaying"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification StartPlaying");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.StartPlaying);
			}

			if (GUILayout.Button("SendCoins"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification SendCoins");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.SendCoins);
			}

			if (GUILayout.Button("SendSpins"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification SendSpins");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.SendSpins);
			}

			if (GUILayout.Button("Jackpot"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification Jackpot");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.Jackpot);
			}
			
			if (GUILayout.Button("RequestCoins"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification RequestCoins");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.RequestCoins);
			}

			if (GUILayout.Button("RequestCoinsGranted"))
			{
				Debug.Log("DevGUI-> PN> Testing SocialPushNotification RequestCoinsGranted");

				NotificationManager.Instance.SendSocialPushNotification(zid, NotificationEvents.RequestCoinsGranted);
			}
		}
		GUILayout.EndHorizontal();	

		GUILayout.Label("========== Local Notifications =============");

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("LocalNotification: Now"))
		{
			NotificationManager.scheduleTestLocalNotifications(NotificationManager.NOTIF_HOURLY_BONUS);
		}

		if (GUILayout.Button("LocalNotification: 30s"))
		{
			NotificationManager.scheduleTestLocalNotifications(NotificationManager.NOTIF_HOURLY_BONUS, 30);
		}

		if (GUILayout.Button("LocalNotification: 60s"))
		{
			NotificationManager.scheduleTestLocalNotifications( NotificationManager.NOTIF_HOURLY_BONUS, 60);
		}

		GUILayout.EndHorizontal();
	}
}
