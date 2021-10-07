using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkAchievementAction : ServerAction
{
	public const string GET_ACHIEVEMENTS = "get_achievements";
    public const string SET_DISPLAY_ACHIEVEMENT = "set_display_achievement";
	public const string COLLECT_ACHIEVEMENT_REWARD = "collect_achievement_reward";
	public const string COLLECT_ACHIEVEMENT_BACKFILL = "collect_achievement_backfill";

	// DEV ONLY
	public const string SET_ACHIEVEMENT_SCORE = "set_achievement_score";
	public const string SET_ACHIEVEMENT_PROGRESS = "set_achievement_progress";
	public const string SET_ACHIEVEMENT_UNLOCKED = "set_achievement_unlock";
	

	public const string TARGET_ZID = "target_zid";
	public const string TARGET_FB_ID = "fb_id";
	public const string TARGET_NETWORK_ID = "network_id";
	public const string ACHIEVEMENT = "achievement";

	// DEV ONLY
	public const string ACHIEVEMENTS = "achievements";
	public const string SCORE = "score";
	
	// Needed for shenanigans below becuase of limitaitons with how our system is built.	
	public const string ACHIEVEMNTS_BOOL = "achievements_bool";
	// Needed for shenanigans below becuase of limitaitons with how our system is built.
	public const string ACHIEVEMNTS_LONG = "achievments_long";	

	

	public string targetZID = "";
	public string targetFBID = "";
	public string targetNetworkID = "";
	public string achievement = "";

	// DEV ONLY
	public long score = 0L;
	public bool isUnlocked = false;
	public long progress = 0L;
	
	public Dictionary<string, long> achievementsLong = new Dictionary<string, long>();
	public Dictionary<string, bool> achievementsBool = new Dictionary<string, bool>();

	/** Constructor */
	private NetworkAchievementAction(ActionPriority priority, string type) : base(priority, type) {}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(GET_ACHIEVEMENTS, new string[] {TARGET_ZID, TARGET_FB_ID, TARGET_NETWORK_ID});
				_propertiesLookup.Add(SET_DISPLAY_ACHIEVEMENT, new string[] {ACHIEVEMENT});
				_propertiesLookup.Add(COLLECT_ACHIEVEMENT_REWARD, new string [] { ACHIEVEMENT});
				_propertiesLookup.Add(COLLECT_ACHIEVEMENT_BACKFILL, new string [] {});
				// DEV ONLY
				_propertiesLookup.Add(SET_ACHIEVEMENT_SCORE, new string[] {SCORE});
				_propertiesLookup.Add(SET_ACHIEVEMENT_UNLOCKED, new string [] {ACHIEVEMNTS_BOOL});
				_propertiesLookup.Add(SET_ACHIEVEMENT_PROGRESS, new string [] {ACHIEVEMNTS_LONG});
			}
			return _propertiesLookup;
		}
	}

	public static void getAchievementsForUser(SocialMember member, EventDelegate callback = null)
	{
		getAchievements(member.zId, member.id, member.networkID, callback);
	}

	
	public static void getAchievements(string zid, string facebookId, string networkId, EventDelegate callback)
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.IMMEDIATE, GET_ACHIEVEMENTS);
		action.targetZID = zid;
		action.targetFBID = facebookId;
		action.targetNetworkID = networkId;
		
		if (callback != null)
		{
			NetworkAchievements.addCallbackToDataProcess(callback);
		}
		
		ServerAction.processPendingActions(true);
	}

    public static void setDisplayAchievement(Achievement achievement)
	{
		SocialMember member = SlotsPlayer.instance.socialMember;
		if (member == null)
		{
			Debug.LogErrorFormat("NetworkAchievementAction.cs -- setDisplayAchievement -- member was null somehow, aborting...");
			return;
		}
		string zid = member.zId;
		string facebookId = member.id;
		string networkId = member.networkID;
		
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.HIGH, SET_DISPLAY_ACHIEVEMENT);
		action.targetZID = zid;
		action.targetFBID = facebookId;
		action.targetNetworkID = networkId;
		action.achievement = achievement.id;
		ServerAction.processPendingActions(true);

		if (member.networkProfile != null)
		{
			member.networkProfile.setFavoriteAchievement(achievement);
		}
		else
		{
			Debug.LogErrorFormat("NetworkAchievementAction.cs -- setDisplayAchievement -- setting achievement to be: {0} but not updating the profile because its not present.", achievement.name);
		}
	}

	public static void collectAchievementReward(Achievement achievement)
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.HIGH, COLLECT_ACHIEVEMENT_REWARD);
		action.achievement = achievement.id;
		ServerAction.processPendingActions(true);
	}

	public static void collectAchievementBackfill()
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.HIGH, COLLECT_ACHIEVEMENT_BACKFILL);
		ServerAction.processPendingActions(true);
	}

	/*******

	Dev Actions

	********/

	public static void setAchievementScore(long score)
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.IMMEDIATE, SET_ACHIEVEMENT_SCORE);
		action.score = score;
		ServerAction.processPendingActions(true);
	}

	public static void setAchievementProgress(string id, long progress)
	{
		Dictionary<string, long> progressMap = new Dictionary<string, long>();
		progressMap.Add(id, progress);
		setAchievementProgress(progressMap);
	}

	public static void setAchievementProgress(Dictionary<string, long> progressMap)
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.IMMEDIATE, SET_ACHIEVEMENT_PROGRESS);
	    action.achievementsLong = progressMap;
		ServerAction.processPendingActions(true);
	}

	public static void setAchievementUnlocked(string id, bool unlocked)
	{
	    Dictionary<string, bool> unlockedMap = new Dictionary<string, bool>();
		unlockedMap.Add(id, unlocked);
		setAchievementUnlocked(unlockedMap);
	}
	
	public static void setAchievementUnlocked(Dictionary<string, bool> unlockedMap)
	{
		NetworkAchievementAction action = new NetworkAchievementAction(ActionPriority.IMMEDIATE, SET_ACHIEVEMENT_UNLOCKED);
		action.achievementsBool = unlockedMap;
		ServerAction.processPendingActions(true);
		
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
			case TARGET_ZID:
				appendPropertyJSON(builder, property, targetZID);
				break;
			case TARGET_FB_ID:
				appendPropertyJSON(builder, property, targetFBID);
				break;
			case TARGET_NETWORK_ID:
				appendPropertyJSON(builder, property, targetNetworkID);
				break;
			case ACHIEVEMENT:
				appendPropertyJSON(builder, property, achievement);
				break;
			case ACHIEVEMNTS_BOOL :
				appendPropertyJSON(builder, ACHIEVEMENTS, achievementsBool);
				break;
			case ACHIEVEMNTS_LONG :
				appendPropertyJSON(builder, ACHIEVEMENTS, achievementsLong);
				break;
			case SCORE:
				appendPropertyJSON(builder, ACHIEVEMENTS, score);
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
		_propertiesLookup = null;
	}	
}