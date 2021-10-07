using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuToaster : DevGUIMenu
{
	private const string jackpotJSONFormat = "\"first_name\":\"{0}\",\"last_name\":\"{1}\",\"fbid\":\"{2}\",\"zid\":\"{3}\",\"credits\":\"{4}\",\"jackpot_key\":\"{5}\"";
	private string getFacebookJson(string jackpotKey, long credits = 1234)
	{
		string result = "";
		SocialMember member = SlotsPlayer.instance.socialMember;
		if (member != null)
		{
			result = string.Format(jackpotJSONFormat,
				member.firstName, member.lastName, member.id, member.zId, credits, jackpotKey);
		}
		else
		{
			result = string.Format(jackpotJSONFormat,
				"Todd", "Gillissie", "100000262921386", "", credits, jackpotKey);
		}

		return "{" + result + "}";
	}

	private string getAnonJson(string jackpotKey, long credits = 1234)
	{
		string result = string.Format(jackpotJSONFormat,
			"", "", "", "", credits, jackpotKey);
		return "{" + result + "}";
	}
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		
		/*if (GUILayout.Button("Generic Toaster"))
		{
			ToasterManager.addToaster(ToasterType.GENERIC, Dict.create(D.TITLE, "text here"), null, 0.0f);
			DevGUI.isActive = false;
		}*/

		string json = "";
		
#if ZYNGA_SKU_HIR
		string poolKey = "hir_ainsworth01_standard";
		string multiPoolKey = "hir_wonka01_grand";
#else
		string poolKey = "";
		string multiPoolKey = "";
#endif

		if (GUILayout.Button("Prog. Notif. Player"))
		{
			json = getFacebookJson(poolKey, 1234);
		}

		if (GUILayout.Button("Prog. Notif. Anon."))
		{
			json = getAnonJson(poolKey, 1234);
		}
		
		if (GUILayout.Button("Multi Prog. Notif. Player"))
		{
			json = getFacebookJson(multiPoolKey, 1234);
		}

		if (GUILayout.Button("Multi Prog. Notif. Anon."))
		{
			json = getAnonJson(multiPoolKey, 1234);
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("VIP Prog. Player"))
		{
			json = getFacebookJson(ProgressiveJackpot.vipJackpot.keyName, 1234);
		}

		if (GUILayout.Button("VIP Prog. Anon."))
		{
			json = getAnonJson(ProgressiveJackpot.vipJackpot.keyName, 1234);
		}

		if (ProgressiveJackpot.vipRevampGrand != null && GUILayout.Button("VIP Revamp Prog. Player"))
		{
			json = getFacebookJson(ProgressiveJackpot.vipRevampGrand.keyName, 1234);
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		if (ProgressiveJackpot.maxVoltageJackpot != null && GUILayout.Button("Max Voltage Prog. Player"))
		{
			json = getFacebookJson(ProgressiveJackpot.maxVoltageJackpot.keyName, 1234);
		}

		if (ProgressiveJackpot.maxVoltageJackpot != null && GUILayout.Button("Max Voltage Anon"))
		{
			json = getAnonJson(ProgressiveJackpot.maxVoltageJackpot.keyName, 1234);
		}

		if (ProgressiveJackpot.giantJackpot != null && GUILayout.Button("Giant Progressive Toaster"))
		{
			json = getFacebookJson(ProgressiveJackpot.giantJackpot.keyName, 1234);
		}
				
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (ProgressiveJackpot.buyCreditsJackpot != null && GUILayout.Button("Buy Progressive Toaster"))
		{
			json = getFacebookJson(ProgressiveJackpot.buyCreditsJackpot.keyName, 15000000);
		}

		if (ProgressiveJackpot.buyCreditsJackpot != null && GUILayout.Button("Jackpot Days"))
		{
			json = getAnonJson(ProgressiveJackpot.buyCreditsJackpot.keyName, 15000000);
		}

		if (json != "")
		{
			ProgressiveJackpot.processJackpotTaken(new JSON(json));
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Achievement Toaster 1"))
		{
			if (NetworkAchievements.allAchievements != null &&
				NetworkAchievements.allAchievements.ContainsKey("hir") &&
				NetworkAchievements.allAchievements["hir"].Count > 0)
			{
			List<Achievement> fakeList = new List<Achievement>();				
				foreach (KeyValuePair<string, Achievement> pair in NetworkAchievements.allAchievements["hir"])
				{
					if (fakeList.Count > 0)
						break;
					fakeList.Add(pair.Value);
				}
				Dict args = Dict.create(D.VALUES, fakeList);
				ToasterManager.addToaster(ToasterType.ACHIEVEMENT, args);
				DevGUI.isActive = false;				
			}
			else
			{
				Debug.LogErrorFormat("DevGUIMenuToaster.cs -- show Achievement 3 Toaster -- could not find any achievements for HIR.");
			}
		}

		if (GUILayout.Button("Achievement Toaster 2"))
		{
			if (NetworkAchievements.allAchievements != null &&
				NetworkAchievements.allAchievements.ContainsKey("hir") &&
				NetworkAchievements.allAchievements["hir"].Count > 0)
			{
				List<Achievement> fakeList = new List<Achievement>();				
				foreach (KeyValuePair<string, Achievement> pair in NetworkAchievements.allAchievements["hir"])
				{
					if (fakeList.Count > 1)
						break;
					fakeList.Add(pair.Value);
				}
				
				Dict args = Dict.create(D.VALUES, fakeList);
				ToasterManager.addToaster(ToasterType.ACHIEVEMENT, args);
				DevGUI.isActive = false;				
			}
			else
			{
				Debug.LogErrorFormat("DevGUIMenuToaster.cs -- show Achievement 3 Toaster -- could not find any achievements for HIR.");
			}
		}		
		
		if (GUILayout.Button("Achievement Toaster 3"))
		{
			if (NetworkAchievements.allAchievements != null &&
				NetworkAchievements.allAchievements.ContainsKey("hir") &&
				NetworkAchievements.allAchievements["hir"].Count > 0)
			{
				List<Achievement> fakeList = new List<Achievement>();				
				foreach (KeyValuePair<string, Achievement> pair in NetworkAchievements.allAchievements["hir"])
				{
					if (fakeList.Count > 2)
						break;
					fakeList.Add(pair.Value);
				}
				
				Dict args = Dict.create(D.VALUES, fakeList);
				ToasterManager.addToaster(ToasterType.ACHIEVEMENT, args);
				DevGUI.isActive = false;				
			}
			else
			{
				Debug.LogErrorFormat("DevGUIMenuToaster.cs -- show Achievement 3 Toaster -- could not find any achievements for HIR.");
			}
		}		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		if (GUILayout.Button("LL Lobby Toaster"))
		{
			ToasterManager.addToaster(ToasterType.LOBBY_V3_LL, null);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Collection Toaster"))
		{
			ToasterManager.addToaster(ToasterType.COLLECTIONS, null);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Friends Toaster"))
		{
			NetworkFriends.instance.onNewFriendData(1, 0, "Mike M");
			DevGUI.isActive = false;
		}
		
		if (GUILayout.Button("LUUE Toaster"))
		{
			Dict args = Dict.create(D.DATA, 9999L);
			ToasterManager.addToaster(ToasterType.LEVEL_UP, args);
			DevGUI.isActive = false;
		}
		
		if (GUILayout.Button(string.Format("Toaster Pause: {0}", onOff[ToasterManager.isPaused ? 1 : 0 ])))
		{
			ToasterManager.isPaused = !ToasterManager.isPaused;
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical();
		
		GUILayout.BeginHorizontal();
		long friendsCooldown = NetworkFriends.instance.lastToasterTime + (new System.TimeSpan(0, 0, NetworkFriends.instance.toasterCooldown)).Ticks - System.DateTime.UtcNow.Ticks;
		if (friendsCooldown < 0)
		{
			friendsCooldown = 0;
		}
		GUILayout.Label(string.Format("Friends Toaster Cooldown: {0}", friendsCooldown));
		if (GUILayout.Button("Reset friends cooldown"))
		{
			NetworkFriends.instance.lastToasterTime = 0;
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
