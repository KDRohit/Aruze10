using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Base class for all types of quest data structures.
*/

public abstract class Quest : IResetGame
{
	public const string MILESTONE_EVENT = "quest_milestone";
	public const string MILESTONE_REWARD_EVENT = "quest_milestone_reward";
	public const string REWARD_OUTCOME_EVENT = "quests_outcome";
	public const string CHALLENGE_QUEST_UPDATE_EVENT = "quests_update";  // Note the associated ACTION is called "quest_update"   (confusing)
	
	public string keyName = "";
	public GameTimer timer;			// Let's us know how much time is remaining on the current quest.
	public int milestoneTotal = 0;	// Total number of milestones for the quest.
	public JSON staticData = null;

	public static int resetCounter = 0;				// Taken from zrt, used for knowing whether to show a quest MOTD again.
	public static Quest activeQuest = null;

	public static string baseUrl = "";

	public static JSON reward = null;				// Lets us know what kind of special reward to give after showing the normal milestone event.
	public static JSON rewardOutcome = null;		// May contain outcome data for something after showing the normal milestone event,
													// such as mystery gift or final minigame.
	public static JSON milestone = null;			// Info about the new milestone that was reached.
	public static int dailyBonusMultiplier = 1;		// Gets reset to 1 after each daily bonus, just in case the even runs out.
	private Dictionary<int, long> referenceWagers = new Dictionary<int, long>();
	
	public static bool isReady
	{
		get
		{
			return
				activeQuest != null &&
				activeQuest.timer != null &&
				!activeQuest.timer.isExpired &&
				activeQuest.staticData != null;
		}
	}
	
	public Quest(string keyName, int endDate, bool isClassicQuest)
	{
		this.keyName = keyName;
		timer = GameTimer.createWithEndDateTimestamp(endDate);
		
		registerEventDelegates();

		// classic quests fetch JSON data from URL's like https://zdnhir3-a.akamaihd.net/data/0012498/quest/quest_race_to_riches_data.dat, 
		// newer EOS quests dont use this.
		if (isClassicQuest)
		{
			RoutineRunner.instance.StartCoroutine(getData());
		}
	}

	// Note: this is only used by old RaceToRaches v.1
	private IEnumerator getData()
	{
		const string RESPONSE_KEY = "QuestData";
		string url = string.Format("{0}quest_{1}_data.dat", baseUrl, keyName);
		
		//yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(url, null, "error_failed_to_load_data", RESPONSE_KEY));
		yield return RoutineRunner.instance.StartCoroutine(Data.attemptServerRequest(url, null, "error_failed_to_load_data", RESPONSE_KEY));
		staticData = Data.data;
		//staticData = Server.getResponseData(RESPONSE_KEY);

		if (staticData != null)
		{
			milestoneTotal = staticData.getInt("number_of_milestones", 0);
						
			foreach (JSON json in staticData.getJsonArray("rewards"))
			{
				new QuestReward(json);
			}
			
			foreach (JSON json in staticData.getJsonArray("reference_wagers"))
			{
				referenceWagers.Add(json.getInt("tier_id", 0), json.getLong("wager", 0L));
			}
			
			finishedGettingData();
		}
		else
		{
			Debug.LogError("No Quest static data found at " + url);
		}
	}
	
	// Each Quest subtype may want to do something special when the data is finished being downloaded, so override this if necessary.
	protected virtual void finishedGettingData()
	{
	}
	
	public long findReferenceWager(int tierId)
	{
		long refWager;
		if (referenceWagers.TryGetValue(tierId, out refWager))
		{
			return refWager;
		}
		Debug.LogError("Could not find Quest reference wager for tier " + tierId);
		return 0;
	}
	
	// Returns the credits amount for the "coin_rewards" from the static data based on the key name.
	// This only works for coin_rewards that don't use tiers.
	public static long getNormalMilestoneCreditsReward()
	{
		if (!isReady)
		{
			return 0L;
		}
		
		foreach (JSON json in activeQuest.staticData.getJsonArray("coin_rewards"))
		{
			if (json.getString("loc_key", "") == "small_credits_reward")
			{
				return json.getLong("credits", 0L);
			}
		}
		
		return 0L;
	}

	public static string getNormalMilestonePaytable()
	{
		if (!isReady)
		{
			return "";
		}
		
		foreach (JSON json in activeQuest.staticData.getJsonArray("coin_rewards"))
		{
			if (json.getString("loc_key", "") == "small_credits_reward")
			{
				return json.getString("key_name", "");
			}
		}
		
		return "";
	}
	
	// Subclasses may override this to register events specific to different quests.
	protected virtual void registerEventDelegates()
	{
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		baseUrl = "";
		activeQuest = null;
		reward = null;
		rewardOutcome = null;
		milestone = null;
		dailyBonusMultiplier = 1;
		resetCounter = 0;
	}

	// public Quest-related Utility fns.  
	// Note: All these could use Dicts instead of switch stmts, but I figure speed isnt important

	public static string GetLocalizedTextFromChallengeType(string challengeType, bool bForProgressBarText=false)
	{
		// For the progress bar text, retrieve a plural noun.   Otherwise singular, present tense for challenge title text

		string localizedChallengeTypeText;
		string localizeKey;

		switch (challengeType)
		{
			case "spin":
				localizeKey = bForProgressBarText ? "spins" : "spin";
				localizedChallengeTypeText = Localize.text(localizeKey);
				break;
			case "big_win":
				localizeKey = bForProgressBarText ? "big_wins" : "big_win";
				localizedChallengeTypeText = Localize.text(localizeKey);
				break;
			case "credits_won":
				if(bForProgressBarText)
				{
					localizedChallengeTypeText = Localize.text("coins");
				}
				else
				{
					// TODO: Localize/Finalize "Win Coins" text in SCAT
					localizedChallengeTypeText = "Win Coins";
				}
				break;
			case "credits_sunk":
				if(bForProgressBarText)
				{
					localizedChallengeTypeText = Localize.text("coins"); // coins bet, but for progress bar we will use 'coins'
				}
				else
				{
					// TODO: Localize/Finalize "Bet Coins" text in SCAT
					localizedChallengeTypeText = "Bet Coins";
				}
				break;
			case "collect_daily_bonus":
				// TODO: Localize/Finalize this text in SCAT
				localizedChallengeTypeText = bForProgressBarText ? "Daily Bonuses" : "Daily Bonus";
				break;
			case "jackpot_win":
				// TODO: Localize/Finalize this text in SCAT
				localizedChallengeTypeText = bForProgressBarText ? "Jackpots" : Localize.text("jackpot");
				break;
			case "bonus_games":
				// TODO: Localize/Finalize this text in SCAT
				localizedChallengeTypeText = bForProgressBarText ? "Bonus Games" : "Bonus Game";
				break;
			case "mystery_gift":
				// TODO: Localize/Finalize this text in SCAT
				localizedChallengeTypeText = bForProgressBarText ? "Sweet Surprizes" : "Sweet Surprize";
				break;
			default:
				string errmsg = string.Format("unknown challengeType: '{0}'", challengeType);
				Debug.LogError(errmsg);
				localizedChallengeTypeText = errmsg;
				break;		
		}

		return localizedChallengeTypeText;
	}

	// Invalidate all cached challenge progress data.
	public static void invalidateCachedProgress()
	{
		DailyChallenge.invalidateCachedProgress();
	}

	// for GetQuestProgressFromChallengeType
	private static Dictionary<string,string> challengeType2UpdateFieldDict = new Dictionary<string, string>() 
	{ 
		{ "spin", "spins_since_start" },
		{ "big_win", "num_big_wins" },
		{ "credits_won", "total_earnings" },
		{ "credits_sunk", "personal_progressive" },
		{ "collect_daily_bonus", "num_daily_bonus" },
		{ "jackpot_win", "num_jackpot_wins" },
		{ "bonus_games", "num_bonus_games" },
		{ "mystery_gift", "num_mystery_gifts" },
	};

	public static int GetQuestProgressFromChallengeType(string challengeType, JSON playerJson)
	{
		string jsonStatusFieldNameForTask;
		if (!challengeType2UpdateFieldDict.TryGetValue(challengeType, out jsonStatusFieldNameForTask))
		{
			Debug.LogErrorFormat("unknown challengeType: {0}", challengeType);
			return -1;
		}

		int questProgressAmount = playerJson.getInt(jsonStatusFieldNameForTask, -1);
		if (questProgressAmount == -1)
		{
			Debug.LogErrorFormat("json is missing quest status update field name: {0}: {1}", jsonStatusFieldNameForTask,playerJson.ToString());
		}
			
		return questProgressAmount;
	}

	// this does the CreditsEconomy conversion as well
	public static string GetLocalizedRewardTextFromRewardType(string rewardType, long rewardAmount)
	{
		string localizedRewardText;

		switch (rewardType)   // AWARD_TYPE_COINS/AWARD_TYPE_VIP/AWARD_TYPE_XP on server
		{
			case "credits":
				localizedRewardText = Localize.textUpper("credits_{0}", CreditsEconomy.convertCredits(rewardAmount));
				break;
			case "xp":
				localizedRewardText = Localize.textUpper("xp_points_{0}", rewardAmount);
				break;
			case "vip":
				localizedRewardText = Localize.textUpper("vip_points_{0}", rewardAmount);
				break;
			case "bonus_game":
			#if true
				// Current state of bonus_game awards on mobile client
				// [12/15/16, 10:23:33 AM] John Bess: Basically, it's very hard to launch into a bonus game of another game from your current game
                // [12/15/16, 10:24:00 AM] John Bess: At the moment, Im pretty sure it forces you to the lobby, then to the game, then back to the lobby, then we'd need to add a case to send you back to your old game

				// Kris: putting these errors in until the above situation is addressed.
				{
					localizedRewardText = "bonus_game unsupported award type";
					Debug.LogErrorFormat("bonus_game award type not supported");
				}
			#else
				localizedRewardText = Localize.textUpper("bonus_game");
			#endif
				break;
			case "cooldown_timer":
				localizedRewardText = Localize.textUpper("cooldown_timer"); //string.Format("Reduce Daily Bonus Cooldown Timer by {0} seconds",rewardAmount);
				break;
			case "big_win":
				if (ExperimentWrapper.DailyChallengeQuest2.isInExperiment)
				{
					// handled through EOS
					localizedRewardText = ExperimentWrapper.DailyChallengeQuest2.bigWinMotdText;
				}
				else
				{
					localizedRewardText = "big_win unsupported award type";
					Debug.LogErrorFormat("big_win award type not supported");
				}
				break;
			default:
				string errmsg = string.Format("unknown rewardType: '{0}'", rewardType);
				Debug.LogError(errmsg);
				localizedRewardText = errmsg;
				break;	
		}

		return localizedRewardText;
	}

	public static void SetProgressMeterState(UIMeterNGUI challengeProgressMeter, TextMeshPro challengeProgressLabel, string challengeType, 
											 int iCurrentProgress, int iTargetAmount)					           		 
	{
		// we no longer want to show the challenge description in the progress bar itself, it's redundant and makes text longer
		//string challengeDescription = Quest.GetLocalizedTextFromChallengeType(challengeType, bForProgressBarText:true).ToUpper();

		long currentProgress = iCurrentProgress;
		long targetAmount = iTargetAmount;

		if ((challengeType == "credits_won")||(challengeType == "credits_sunk"))
		{  
			// for credits_won, we need to format values in millions and convert to hyperEconomy
			currentProgress = CreditsEconomy.multipliedCredits(currentProgress);
			targetAmount = CreditsEconomy.multipliedCredits(targetAmount);
		}

		challengeProgressLabel.text = string.Format("{0}/{1}", CommonText.formatNumber(currentProgress),  CommonText.formatNumber(targetAmount));

		challengeProgressMeter.setState(currentProgress,targetAmount, doTween:true, setMaxTweenDuration:2.0f);
	}
}

// Note: this is only used by old HIR RaceToRaches v.1
public class QuestReward : IResetGame
{
	public int milestone = 0;
	public string type = "";
	public string paytable = "";
	
	private static Dictionary<int, QuestReward> all = new Dictionary<int, QuestReward>();
	
	public QuestReward(JSON json)
	{
		if (json != null)
		{
			milestone = json.getInt("milestone", 0);
			type = json.getString("quest_reward_type", "");
			paytable = json.getString("quest_reward_paytable", "");
			all.Add(milestone, this);
		}
	}
	
	public static QuestReward find(int milestone)
	{
		QuestReward reward;
		if (all.TryGetValue(milestone, out reward))
		{
			return reward;
		}
		return null;
	}
	
	public static int sortByMilestone(QuestReward a, QuestReward b)
	{
		return a.milestone.CompareTo(b.milestone);
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<int, QuestReward>();
	}
}

