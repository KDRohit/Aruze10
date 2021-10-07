using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkAchievements : IResetGame
{
	public delegate void AchievementEventDelegate(string achievementId);

	public static EventDelegate onProcessEvent;
	
	public class AchievementRarity
	{
		public string name { get; private set; }
		public string icon { get; private set; }

		public AchievementRarity(string achievementName, string achievementIcon)
		{
			name = achievementName;
			icon = achievementIcon;
		}
	}

	public static Dictionary<string, Dictionary<string, Achievement>> allAchievements = new Dictionary<string, Dictionary<string, Achievement>>();
	public static Dictionary<int, AchievementRarity> rarities = new Dictionary<int, AchievementRarity>();

	public enum Sku
	{
		HIR = 0,
		WOZ = 1,
		WONKA = 2,
		BLACK_DIAMOND = 3,
		GOT = 4,
		NETWORK = 5
	}

	public static string skuToString(Sku sku)
	{
		switch (sku)
		{
			case Sku.WONKA:
				return "wonka";
			case Sku.WOZ:
				return "woz";
			case Sku.BLACK_DIAMOND:
				return "black_diamond";
			case Sku.GOT:
				return "got";
			case Sku.NETWORK:
				return "network";
			case Sku.HIR:
			default:
				return "hir";
		}
	}

	public static List<Sku> skuList = new List<Sku>{Sku.HIR, Sku.WOZ, Sku.WONKA, Sku.BLACK_DIAMOND, Sku.GOT, Sku.NETWORK};

	public static bool isEnabled
	{
		get
		{
			return ExperimentWrapper.NetworkAchievement.isInExperiment && !forceOff;
		}
	}

	public static bool rewardsEnabled
	{
		get { return isEnabled && ExperimentWrapper.NetworkAchievement.enableTrophyRewards; }
	}

	public static int numNew
	{
		get
		{
			int result = 0;
			if (newAchievements != null)
			{
				result += newAchievements.Count;
			}

			if (unlockedNotSeen != null)
			{
				result += unlockedNotSeen.Count;
			}
			return result;
		}
	}

#if UNITY_EDITOR
	public static JSON dataJSON;
#endif

	/*
	   Tracking of "NEW" achievements.
	   Becuase there is no good way to share tracking a list of strings between web and mobile
	   we have a somewhat complicated workaround to share this tracking.

	   Achievements will be launched in 'sets' each which an incremeneted version number.
	   This version number is what is shared between web and mobile. When a player has seen
	   all the achievements from a set we increment their "seen version" so that web wont show
	   these as "new".

	   However this leads to the need to track if a user has only see 3 of the new 15
	   achievements. To do this we will locally keep a string list of the achievements
	   in player prefs that we have seen of the ones marked "new". This way we won't show
	   those as new, and we will know when we have seen all of the new ones and can then
	   increment the seen version.

	   Additionally, achievements that are earned but not yet viewed need to be marked as "new"
	   as well. When we earn this achievements we will save it to a different player pref list
	   and can then track those as seen separetly, and remove it from the list when the play "views" that trophy.
	 */
	// Keeping track of achievements that are "new".
	public static int newestSeenVersion = 1;
	// Achievements that are new at login.
	public static List<Achievement> newAchievements = new List<Achievement>();

	public static bool forceOff = false;

	// We keep double showing things so we need to keep track of what we have shown, since there are delays between calling.
	public static List<Achievement> shownAchievementsList = new List<Achievement>();

	// Achievements that we have seen from the current version. We will
	// mark the version as updated when we have seen everything from a specific version.
	public static List<string> seenAchievementKeys = new List<string>();
	private static List<string> unlockedNotSeen = new List<string>();
	private static List<string> unlockedNotClicked = new List<string>();

	public delegate void newBadgeAmountChangedDelegate(int num);
	public static event newBadgeAmountChangedDelegate onNewBadgeAmountChanged;

	private static int backfillAmount = 0;

	// Static variables used for showing the toaster.
	private static bool isWaitingToNotifyUser = false;
	private static List<Achievement> pendingAchievementList;
	private const float TOASTER_WAIT_TIME = 1.0f;

	private const string ACHIEVEMENTS_SEEN_KEYS = "achievements_seen_keys";
	private const string ACHIEVEMENTS_UNLOCKED_NOT_SEEN_KEYS = "achievements_unlocked_not_seen_keys";
	private const string ACHIEVEMENTS_UNLOCKED_NOT_CLICKED_KEYS = "achievements_unlocked_not_clicked_keys";

	public const string OVERLAY_BUTTON_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Network Profiles/Profile Overlay Button Achievements.prefab";

	public const string LIVE_DATA_WONKA_INSTALL_KEY = "HIR_ACHIEVEMENTS_WONKA_INSTALL_URL";
	public const string LIVE_DATA_WOZ_INSTALL_KEY = "HIR_ACHIEVEMENTS_WOZ_INSTALL_URL";


	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("achievement_data", achievementDataCallback, true);
		Server.registerEventDelegate("achievement_unlock", achievementUnlockedCallback, true);
		Server.registerEventDelegate("achievement_level_up", achievementLevelUpCallback, true);
		Server.registerEventDelegate("achievement_reward_collected", achievementRewardCallback, true);
		Server.registerEventDelegate("achievement_backfill_collected", achievementBackfillCallback, true);
		Server.registerEventDelegate("network_achievement_unlock", achievementUnlockedCallback, true);
	}

	public static Achievement getAchievement(string id)
	{
		foreach (Sku sku in System.Enum.GetValues(typeof(Sku)))
		{
			string skuString = skuToString(sku);
			if (allAchievements.ContainsKey(skuString) && allAchievements[skuString].ContainsKey(id))
			{
				return allAchievements[skuString][id];
			}
		}
		// If we couldn't find anything with that id, then return null, but throw a warning.
		Debug.LogWarningFormat("NetworkAchievement.cs -- getAchievement -- could not find an achievement for any sku with id: {0}, returning null", id);
		return null;
	}

	public static void setBackfillAmount(int amount)
	{
		backfillAmount = amount;
	}

	public static int getBackfillAmount()
	{
		return backfillAmount;
	}

	public static void populateCollectedAchievements(JSON data)
	{
		if (data == null)
		{
			Debug.LogErrorFormat("NetworkAchievement.cs -- populateCollectedAcheivements -- data is null");
			return;
		}

		SocialMember member = SlotsPlayer.instance.socialMember;
		if (member == null)
		{
			Debug.LogError("user not loaded");
			return;
		}

		if (member.achievementProgress == null)
		{
			Debug.LogError("user hasn't unlocked any achievements yet");
		}

		foreach (string key in data.getKeyList())
		{
			string achievementId = key;
			member.achievementProgress.setRewardCollected(achievementId);
		}
		member.setUpdated();
	}


	public static void populateRarities(JSON[] data)
	{
		if (data == null || data.Length == 0)
		{
			Debug.LogErrorFormat("NetworkAchievement.cs -- populateRarities -- data is null");
			return;
		}


		for (int i = 0; i < data.Length; i++)
		{
			string name = data[i].getString("name", "");
			string icon = data[i].getString("icon", "");
			AchievementRarity rarityData = new AchievementRarity(name, icon);
			rarities.Add(i+1, rarityData); //index from server is 1 based
		}
	}

    public static void populateAll(JSON data)
	{
#if UNITY_EDITOR
		dataJSON = data;
#endif

		// Dealing with marking trophies as "seen".
		newestSeenVersion = CustomPlayerData.getInt(CustomPlayerData.ACHIEVEMENTS_NEWEST_SEEN_VERSION, 1);
		string seenKeysString = PlayerPrefsCache.GetString(ACHIEVEMENTS_SEEN_KEYS, "");
		string[] seenKeys = seenKeysString.Split(',');
		seenAchievementKeys = new List<string>(seenKeys);

		// This is used for whether or not that achievement has been scrolled by, and adds to the numNew variable.
		string unlockedNotSeenString = PlayerPrefsCache.GetString(ACHIEVEMENTS_UNLOCKED_NOT_SEEN_KEYS, "");
		string[] unlockedNotSeenKeys = unlockedNotSeenString.Split(',');
		unlockedNotSeen = new List<string>(unlockedNotSeenKeys);
		unlockedNotSeen.Remove(""); // Remove the empty string.

		// This is used to determine whether we show the particle animation and confetti when the trophy is viewed
		// and does not affect the numNew variable.
		string unlockedNotClickedString = PlayerPrefsCache.GetString(ACHIEVEMENTS_UNLOCKED_NOT_CLICKED_KEYS, "");
		string[] unlockedNotClickedKeys = unlockedNotClickedString.Split(',');
		unlockedNotClicked = new List<string>(unlockedNotClickedKeys);
		unlockedNotClicked.Remove(""); // Remove the empty string.

		if (data == null)
		{
			Debug.LogErrorFormat("NetworkAchievement.cs -- populateAll -- data is null.");
			return;
		}

		Achievement achievement;
		string sku = "";

		foreach (Sku skuType in System.Enum.GetValues(typeof(Sku)))
		{
			sku = skuToString(skuType);
			JSON[] achievementData = data.getJsonArray(sku);
			if (achievementData != null)
			{
				if (!allAchievements.ContainsKey(sku))
				{
					allAchievements.Add(sku, new Dictionary<string, Achievement>());
				}

				// Populate the achievements
				for (int j = 0; j < achievementData.Length; j++)
				{
					achievement = new Achievement(achievementData[j], skuType);
					if (skuType == Sku.HIR &&
						achievement.version > newestSeenVersion &&
						!seenAchievementKeys.Contains(achievement.id))
					{
						// MCC -- HIR-66067: Only add a trophy to the "new" list if it is an HIR trophy.
						// If this is a newer version, and not in our "seen" list, then the user has not seen it.
						// Also check if we earned this last session but never visited the trophy.
						newAchievements.Add(achievement);
					}
					if (isAchievementValid(skuType, achievement))
					{
						allAchievements[sku][achievement.id] = achievement;
					}
				}
			}
		}
	}

	public static void processRewards()
	{
		//show network achievement rewards
		if (!NetworkAchievements.rewardsEnabled)
		{
			return;
		}

		//if user has achievements to claim
		List<string> rewards = SlotsPlayer.instance.socialMember.achievementProgress.getAchievementsWithRewardsAvailable();
		if (rewards != null && rewards.Count > 0)
		{
			for(int i=0; i<rewards.Count; ++i)
			{
				if (string.IsNullOrEmpty(rewards[i]))
				{
					continue;
				}

				Achievement achievement = NetworkAchievements.getAchievement(rewards[i]);

				if (shouldShowDialog(achievement))
				{
					AchievementsRewardDialog.showDialog(achievement, false);
				}
			}
		}
	}

	private static void onRewardProcessed(Dict args)
	{
		if (!args.ContainsKey(D.PAYOUT_CREDITS))
		{
			Debug.LogError("Invalid reward amount");
			return;
		}
		long amount = (int)args[D.PAYOUT_CREDITS];
		SlotsPlayer.addNonpendingFeatureCredits(amount, "trophy reward");
	}

	public static bool isBackfillAwardAvailable()
	{
		return backfillAmount > 0;
	}

	private static bool isAchievementValid(Sku sku, Achievement achievement)
	{
		switch (sku)
		{
			case Sku.HIR:
				return achievement.version <= ExperimentWrapper.NetworkAchievement.hirTrophyVersion;
			case Sku.BLACK_DIAMOND:
				return achievement.version <= ExperimentWrapper.NetworkAchievement.bdcTrophyVersion;
			case Sku.WONKA:
				return achievement.version <= ExperimentWrapper.NetworkAchievement.wonkaTrophyVersion;
			case Sku.WOZ:
				return achievement.version <= ExperimentWrapper.NetworkAchievement.wozTrophyVersion;
			case Sku.GOT:
				return false; //GoT slots doesn't have achievements
			case Sku.NETWORK:
				return achievement.version <= ExperimentWrapper.NetworkAchievement.networkTrophyVersion;
		}
		return false;
	}

	public static bool isUnlockedNotClicked(Achievement achievement)
	{
		return unlockedNotClicked.Contains(achievement.id);
	}

	private static void saveOutUnlockedNotClicked()
	{
		string result = "";
		for (int i = 0; i < unlockedNotClicked.Count; i++)
		{
			result += (i == 0) ? "" : ",";
			result += unlockedNotClicked[i];
		}

		PlayerPrefsCache.SetString(ACHIEVEMENTS_UNLOCKED_NOT_CLICKED_KEYS, result);
	}

	public static bool isUnlockedNotSeen(Achievement achievement)
	{
		return unlockedNotSeen.Contains(achievement.id);
	}

	private static void saveOutUnlockedNotSeen()
	{
		string result = "";
		for (int i = 0; i < unlockedNotSeen.Count; i++)
		{
			result += (i == 0) ? "" : ",";
			result += unlockedNotSeen[i];
		}

		if (onNewBadgeAmountChanged != null)
		{
			onNewBadgeAmountChanged(numNew);
		}
		PlayerPrefsCache.SetString(ACHIEVEMENTS_UNLOCKED_NOT_SEEN_KEYS, result);
	}

	public static void markUnlockedAchievementSeen(Achievement seen)
	{
		if (unlockedNotSeen.Contains(seen.id))
		{
			// If this is one we have unlocked and not seen yet, not remove it from that list.
			unlockedNotSeen.Remove(seen.id);
			saveOutUnlockedNotSeen();
		}
	}

	public static void markUnlockedAchievementClicked(Achievement seen)
	{
		if (unlockedNotClicked.Contains(seen.id))
		{
			// If this is one we have unlocked and not seen yet, not remove it from that list.
			unlockedNotClicked.Remove(seen.id);
			saveOutUnlockedNotClicked();
		}
	}

	public static void markAchievementSeen(Achievement seen)
	{
		if (!seenAchievementKeys.Contains(seen.id))
		{
			seenAchievementKeys.Add(seen.id);
			// Update the player pref.
			string arrayString = "";
			for (int i = 0; i < seenAchievementKeys.Count; i++)
			{
				arrayString += (i == 0) ? "" : ",";
				arrayString += seenAchievementKeys[i];
			}

			PlayerPrefsCache.SetString(ACHIEVEMENTS_SEEN_KEYS, arrayString);

			if (newAchievements.Contains(seen))
			{
				// Remove it from the "new" list.
				newAchievements.Remove(seen);
			}

			if (newAchievements.Count == 0)
			{
				// If there are no more "new" trophies, then update our version number.
				CustomPlayerData.setValue(CustomPlayerData.ACHIEVEMENTS_NEWEST_SEEN_VERSION, seen.version);
			}

			if (onNewBadgeAmountChanged != null)
			{
				onNewBadgeAmountChanged(numNew);
			}
		}
	}

	public static void getAchievementsForUser(SocialMember member)
	{
		NetworkAchievementAction.getAchievements(member.zId, member.id, member.networkID, null);
	}

	private static void achievementDataCallback(JSON data)
	{
		string zid = data.getString("target_zid", "-1");
		string nid = data.getString("target_network_id", "-1");
		string fbid = data.getString("target_fb_id", "-1");
		long newScore = data.getLong("achievement_score", 0);
		SocialMember createdMember = CommonSocial.findOrCreate(
			fbid:fbid,
			zid:zid,
			nid:nid);

		if (createdMember == null)
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- achievementDataCallback -- we couldn't find a social member from the information provided, this should never happen. ZID: {0}, FBID: {1}, NETWORK_ID: {2}", zid, fbid, nid);
		}
		else
		{
			JSON achievementJSON = data.getJSON("achievements");
			if (achievementJSON != null)
			{
				if (createdMember.achievementProgress == null)
				{
					createdMember.achievementProgress = new AchievementProgress(achievementJSON, newScore, createdMember);
				}
				else
				{
					createdMember.achievementProgress.update(achievementJSON, newScore, createdMember);
				}
				createdMember.setUpdated();
			}

			if (ProfileAchievementsTab.instance != null)
			{
				// If we already have the dialog tab open then tell it we have loaded it.
				if (ProfileAchievementsTab.instance.member == createdMember)
				{
					// If the member matches then tell the dialog to continue;
					ProfileAchievementsTab.instance.isWaitingForData = false;
				}
			}
		}

		if (onProcessEvent != null)
		{
			onProcessEvent(data);
			onProcessEvent = null;
		}
	}

	private static bool shouldShowDialog(Achievement achievement)
	{
		// MCC -- Adding loads of null checks since all of these could be null.
		if (SlotsPlayer.instance == null ||
			SlotsPlayer.instance.socialMember == null ||
			achievement == null ||
			shownAchievementsList == null ||
			SlotsPlayer.instance.socialMember.experienceLevel < Glb.ACHIEVEMENT_MOTD_MIN_LEVEL ||
			(achievement.sku != Sku.HIR && achievement.sku != Sku.NETWORK) ||
			isWaitingToNotifyUser ||
			shownAchievementsList.Contains(achievement))
		{
			/* Do NOT show if:
			* If they are too low-level to view the dialog
			* This is not an HIR or Network achievement
			* We are already waiting to notify the user for an achievement.
			* We have already tried to show the achievement this session.
			*/
			return false;
		}
		switch (achievement.id)
		{
			//I Love hit it rich (first rophy)
			case "hir_login_1":
				return false;

			default:
				return true;
		}
	}

	private static void achievementUnlockedCallback(JSON data)
	{
		string achievementId = data.getString("achievement", "");
		// Get the achievementId and then unlock it.
		Achievement achievement = NetworkAchievements.getAchievement(achievementId);
		if (achievement != null)
		{
			// Mark that achievement as unlocked.
			if (!SlotsPlayer.instance.socialMember.achievementProgress.isUnlocked(achievementId))
			{
				// If the achievment is unlocked before we get this then dont increment score or mark 
				// it as unlocked. (which can happen if it comes down right after a get_achievements 
				// event for things that we track locally).

				SlotsPlayer.instance.socialMember.achievementProgress.setUnlock(achievementId, true);
				// Incrememnt the score of the player by that achievement's score.
				// Showing the "rank_up" dialog will have its own event triggered.
				SlotsPlayer.instance.socialMember.achievementProgress.incrementScore(achievement.score);
			}

			// Mark this as not seen here.
			if (!unlockedNotSeen.Contains(achievement.id))
			{
				unlockedNotSeen.Add(achievement.id);
				// Save out the new list to a player pref so that on load we keep it "new".
				saveOutUnlockedNotSeen();
			}

			// Mark this as not clicked here.
			if (!unlockedNotClicked.Contains(achievement.id))
			{
				unlockedNotClicked.Add(achievement.id);
				// Save out the new list to a player pref so that on load we keep it "new".
				saveOutUnlockedNotClicked();
			}

			if (pendingAchievementList == null)
			{
				pendingAchievementList = new List<Achievement>();
			}
			if ((!rewardsEnabled || shouldShowDialog(achievement))
				&& !pendingAchievementList.Contains(achievement)) // Make sure we don't double add.
			{
				pendingAchievementList.Add(achievement);
			}

			if (!isWaitingToNotifyUser)
			{
				if (rewardsEnabled && shouldShowDialog(achievement))
				{
					RoutineRunner.instance.StartCoroutine(showAchievementDialogRoutine());
				}
				else if (!rewardsEnabled)
				{
					// Only start this coroutine if we don't already have one running.
					RoutineRunner.instance.StartCoroutine(showAchievementToasterRoutine());
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- achievementUnlockedCallback -- could not find the achievement for this key: {0}. Bailing on updating anything.", achievementId);
			return;
		}
	}

	private static void achievementRewardCallback(JSON data)
	{
		string achievementId = data.getString("achievement", "");
		long credits = data.getLong("credits_granted", 0);

		if (credits > 0)
		{
			SlotsPlayer.addNonpendingFeatureCredits(credits, "trophy reward");
		}

		if (string.IsNullOrEmpty(achievementId))
		{
			Debug.LogError("No achievement with reward");
			return;
		}

		// Get the achievementId and then unlock it.
		SocialMember member = SlotsPlayer.instance.socialMember;
		if (member.achievementProgress == null)
		{
			Debug.LogError("user hasn't unlocked any achievements yet");
			return;
		}

		member.achievementProgress.setRewardCollected(achievementId);
		member.setUpdated();
	}

	private static void achievementBackfillCallback(JSON data)
	{
		long credits = data.getLong("credits_granted", 0);
		if (credits > 0)
		{
			SlotsPlayer.addCredits(credits, "trophy reward", false);
		}

		// Get the achievementId and then unlock it.
		SocialMember member = SlotsPlayer.instance.socialMember;
		if (member.achievementProgress == null)
		{
			Debug.LogError("user hasn't unlocked any achievements yet");
			return;
		}

		JSON collectedAchievements = data.getJSON("collected");
		if (collectedAchievements != null)
		{
			foreach (string key in collectedAchievements.getKeyList())
			{
				string achievementId = key;
				member.achievementProgress.setRewardCollected(achievementId);
			}
			member.setUpdated();
		}

		//set backfill amount so dialog doens't pop twice
		backfillAmount = 0;
	}

	public static AchievementRarity getRarity(int rarityId)
	{
		if (null == rarities || !rarities.ContainsKey(rarityId))
		{
			return null;
		}

		return rarities[rarityId];
	}

	public static List<string> getAllRarities()
	{
		if (null == rarities)
		{
			return new List<string>();
		}

		List<string> availableRarities = new List<string>();
		foreach(AchievementRarity item in rarities.Values)
		{
			availableRarities.Add(item.name);
		}

		return availableRarities;
	}

	public static string getInstallUrl(Sku skuType)
	{
		switch (skuType)
		{
			case Sku.WONKA:
				return Data.liveData.getString(LIVE_DATA_WONKA_INSTALL_KEY, "https://www.google.com");
			case Sku.WOZ:
				return Data.liveData.getString(LIVE_DATA_WOZ_INSTALL_KEY, "https://www.google.com");
			default:
				return "";
		}
	}

	private static IEnumerator showAchievementDialogRoutine()
	{
		isWaitingToNotifyUser = true;
		yield return new WaitForSeconds(TOASTER_WAIT_TIME);

		// Because network achievement unlocks have no rewards they are not persistent and must show the same session.
		// We want to go through the pending list and find any network achievements, if there are some, show them all,
		// if there are not, then show only one HIR achievement.
		pendingAchievementList.Sort(achievementUnlockSorter);
		bool hasNetworkAchievement = false;
		for (int i = 0; i < pendingAchievementList.Count; i++)
		{
			if (pendingAchievementList[i].sku == NetworkAchievements.Sku.NETWORK)
			{
				// If we encounter a network achievment, mark this as true, since we have
				// sorted the array this will be true before we hit a non network achievement.
				hasNetworkAchievement = true;
			}

			if (pendingAchievementList[i].sku == NetworkAchievements.Sku.HIR && hasNetworkAchievement)
			{
				// If this is a non-network achievemnt, but we had network achievements come down
				// then ignore it and break out so we clear the list.
				pendingAchievementList.Clear();
				break;
			}
			else
			{
				// Otherwise this is either a network achievemnt, or a HIR achievement and
				// no network achievement came down, so show it.
				AchievementsRewardDialog.showDialog(pendingAchievementList[i], false);
				// Make sure we break after removing from the list we are iterating over.
				pendingAchievementList.RemoveAt(i);
				break;
			}
		}
		isWaitingToNotifyUser = false;
	}

	private static int achievementUnlockSorter(Achievement one, Achievement two)
	{
		if (one.sku == NetworkAchievements.Sku.NETWORK && two.sku == NetworkAchievements.Sku.NETWORK)
		{
			// If both are network achievemnts, compare their scores.
			return one.score.CompareTo(two.score);
		}
		else if (one.sku == NetworkAchievements.Sku.NETWORK || two.sku == NetworkAchievements.Sku.NETWORK)
		{
			// If only one of them is a network achievent, sort by their sku.
			return (two.sku == NetworkAchievements.Sku.NETWORK).CompareTo(one.sku == NetworkAchievements.Sku.NETWORK);
		}
		else
		{
			// If neither are network achievemnts, compaire their scores.
			return one.score.CompareTo(two.score);
		}
	}

	private static IEnumerator showAchievementToasterRoutine()
	{
		isWaitingToNotifyUser = true;
		yield return new WaitForSeconds(TOASTER_WAIT_TIME);
		// Make a copy so we can empty the old one without worrying about borking the toaster.
		List<Achievement> toSendList = new List<Achievement>(pendingAchievementList);
		ToasterManager.addToaster(ToasterType.ACHIEVEMENT, Dict.create(D.VALUES, toSendList));

		pendingAchievementList.Clear();
		isWaitingToNotifyUser = false;
	}

	private static void achievementLevelUpCallback(JSON data)
	{
		int levelNum = data.getInt("achievement_level", 0);
		AchievementLevel level = AchievementLevel.getLevel(levelNum);
		if (level != null)
		{
			long playerScore = SlotsPlayer.instance.socialMember.achievementProgress.score;
			if (level.requiredScore < playerScore)
			{
				Debug.LogWarningFormat("NetworkAchievements.cs -- achievementLevelUpCallback -- the required score for this level is {0}, but the player has {1}. This could mean this event came down first, or something weird is going on.", level.requiredScore, playerScore);
			}
		}
		else
		{
			Debug.LogErrorFormat("NetworkAchievements.cs -- achievementLevelUpCallback -- was given rank {0}, but we don't have that on the client.", levelNum);
		}

		// If we got the level up event, show it, even if something weird happened.
		AchievementsRankUpDialog.showDialog(level);
	}

	public static long getCurrentPlayerAchievementScoreForSku(string skuKey)
	{
		long result = 0;
		foreach (KeyValuePair<string, Achievement> pair in allAchievements[skuKey])
		{
			if (pair.Value.isUnlocked(SlotsPlayer.instance.socialMember))
			{
				// If the achievement is unlocked lets add to the total.
				result += pair.Value.score;
			}
		}
		return result;
	}

	public static void addCallbackToDataProcess(EventDelegate callback)
	{
		// Remove first so we never double add
		onProcessEvent -= callback;
		onProcessEvent += callback;
	}

	// Used in case we need to remove a callback manually
	public static void removeCallbackToDataProcess(EventDelegate callback)
	{
		onProcessEvent -= callback;
	}
	
	public static void onLoadBundleRequest()
	{
		AssetBundleManager.downloadAndCacheBundle("network_achievement", false, true, blockingLoadingScreen:false);
	}

	public static void resetStaticClassData()
	{
		allAchievements = new Dictionary<string, Dictionary<string, Achievement>>();
		unlockedNotSeen = new List<string>();
		unlockedNotClicked = new List<string>();
		pendingAchievementList = new List<Achievement>();
		shownAchievementsList = new List<Achievement>();
		isWaitingToNotifyUser = false;
		seenAchievementKeys = new List<string>();
		newAchievements = new List<Achievement>();
		rarities = new Dictionary<int, AchievementRarity>();
	}
}
