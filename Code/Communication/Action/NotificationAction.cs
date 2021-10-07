using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NotificationAction : ServerAction
{
	public const string NOTIF_CAN_SEND = "notif_can_send";
	public const string CAN_SEND_NOTIF = "can_send_notifs";
	
	List<string> zids = null;
	
	public delegate void NotificationCanSendCallback(List<SocialMember> members);
	
	//property names
	private const string ZIDS = "zids";
	
	private NotificationAction(ActionPriority priority, string type) : base(priority, type) 
	{
		
	}
	
	// Send to just one zid
	public static void notificationCanSend(string zid, NotificationCanSendCallback callback)
	{
		if (string.IsNullOrEmpty(zid) == false)
		{
			notificationCanSend(new List<string>( new string[] {zid} ), callback);
		}
	}
	
	// Checks whether the given list of zids have received either 3 notifications in 24 hours or one in the last 3 hours.
	// If yes sends to list of facebook members through callback
	public static void notificationCanSend(List<string> zids, NotificationCanSendCallback callback)
	{
		if (zids == null || zids.Count == 0 || !NotificationManager.RegisteredForPushNotifications)
		{
			return;
		}
		
		NotificationAction action = new NotificationAction(ActionPriority.HIGH, NOTIF_CAN_SEND);
		action.zids = new List<string>(zids);
		
		Server.registerEventDelegate(NOTIF_CAN_SEND, (json) => 
		{
			JSON zidResults = json.getJSON(CAN_SEND_NOTIF);
			
			List<SocialMember> canSendTo = new List<SocialMember>();
			if (zidResults != null)
			{
				foreach (string key in zidResults.getKeyList())
				{
					if (zidResults.getBool(key, false) || Data.IsSandbox)
					{
						SocialMember member = SocialMember.findByZId(key);
						if (member != null)
						{
							canSendTo.Add(member);
						}
						else
						{
							Debug.LogWarning("Returned true to a Zid that wasn't really a friend : " + key);
						}
					}
				}
				
				if (callback != null)
				{
					callback(canSendTo);
				}
			}
		});
	}

	// Send a partner powerup notification to your buddy
	public static void sendPartnerPowerupPairedNotif ()
	{
		string zidToSend = PartnerPowerupCampaign.buddyString;

		NotificationManager.Instance.SendSocialPushNotification(zidToSend,NotificationEvents.PartnerPowerupPaired);
		NotificationManager.Instance.SendSocialPushNotification(zidToSend,NotificationEvents.PartnerPowerupFBPaired);
	}

	// Send a partner powerup notification to your buddy
	public static void sendPartnerPowerupNotif()
	{
		string zidToSend = PartnerPowerupCampaign.buddyString;

		NotificationManager.Instance.SendSocialPushNotification (zidToSend, NotificationEvents.PartnerPowerupNudge);
		NotificationManager.Instance.SendSocialPushNotification (zidToSend, NotificationEvents.PartnerPowerupFBNudge);
	}

	// Send a partner powerup notification to your buddy that you did your job
	public static void sendPartnerPowerupUserCompleteNotif()
	{
		string zidToSend = PartnerPowerupCampaign.buddyString;

		NotificationManager.Instance.SendSocialPushNotification (zidToSend, NotificationEvents.PartnerPowerupPlayerComplete);
		NotificationManager.Instance.SendSocialPushNotification (zidToSend, NotificationEvents.PartnerPowerupFBComplete);
	}
	
	// Send jackpot notification to all friends
	public static void sendJackpotNotifications(string gameName = "")
	{
		Debug.LogFormat("PN> sendJackpotNotifications");

		notificationCanSend(SocialMember.getzIds(SocialMember.friendPlayers), (members) =>
		{
				NotificationManager.SocialPushNotification(members, NotificationEvents.Jackpot);
		});
	}

	// Send started playing notification to all friends
	public static void sendStartPlayingNotifications(string gameName = "")
	{
		notificationCanSend(SocialMember.getzIds(SocialMember.friendPlayers), (members) =>
		{
			NotificationMessage nMessage = NotificationInfo.getRandomMessage("notif_social_play");
			if (nMessage.key != null)
			{
				string notifMessage = Localize.text(nMessage.key, gameName);
				foreach(SocialMember member in members)
				{
					NotificationManager.PushNotification(member, NotificationEvents.StartPlaying, notifMessage);
				}
			}
		});	
	}
	
	public static void sendCompletedChallenge(SocialMember challenger)
	{
		if (challenger != null)
		{
			notificationCanSend(challenger.zId, (members) =>
			{
				foreach(SocialMember member in members)
				{
					string notifMessage = Localize.text("notif_challenge_complete_{0}", SlotsPlayer.instance.socialMember.firstName);
					NotificationManager.PushNotification(member,NotificationEvents.CompletedChallenge, notifMessage); 
				}
			});	
		}
	}
	
	public static void sendSpins(List<SocialMember> friends)
	{
		Debug.LogFormat("PN> sendSpins");

		notificationCanSend(SocialMember.getzIds(friends), (members) =>
		{
			NotificationManager.SocialPushNotification(members, NotificationEvents.SendSpins);
		});	
	}
	
	public static void sendChallenges(List<SocialMember> friends, string game)
	{
		Debug.LogFormat("PN> sendChallenges");

		notificationCanSend(SocialMember.getzIds(friends), (members) =>
		{
			string notifMessage = Localize.text("notif_challenge_received_1_{0}_{1}", 
						SlotsPlayer.instance.socialMember.fullName,
						game
						);
					
			NotificationManager.PushNotification(members, NotificationEvents.SendChallenge, notifMessage);		
		});	
	}
	
	public static void sendCoins(List<SocialMember> friends)
	{
		Debug.LogFormat("PN> sendCoins");

		notificationCanSend(SocialMember.getzIds(friends), (members) =>
		{
			NotificationManager.SocialPushNotification(members, NotificationEvents.SendCoins);
		});
	}
	
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(NOTIF_CAN_SEND, new string[] { ZIDS });
			}
			return _propertiesLookup;
		}
	}
	
	private static Dictionary<string, string[]> _propertiesLookup = null;
	
	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}
		
		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case ZIDS:
					appendPropertyJSON(builder, property, zids);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}
	
	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}

#if UNITY_EDITOR
	public static void testCreateNotificationAction()
	{
		NotificationAction action = new NotificationAction(ActionPriority.HIGH, NOTIF_CAN_SEND);
		action.zids = new List<string>(new string[] { "01", "02", "03" });
		string template = "{\"sort_order\":1,\"type\":\"notif_can_send\",\"zids\":[\"01\",\"02\",\"03\"]}";
		string output = ServerAction.getBatchStringForTesting(action);
		Debug.Log(string.Format("ServerAction {0}: {1}", action, output));
		if (output != template)
		{
			Debug.LogError(string.Format("ServerAction {0} has invalid serialization {1}: doesn't match {2}", action, output, template));
		}
	}
#endif

}

