
// disabling this optimization because it appears to break progress tracking on PTR, so I dont trust it on DailyChallenge either.  TODO: investigate why.  DO NOT reenable w/o testing!
//#define ENABLE_CACHED_PROGRESS_JSON_OPTIMIZATION

using UnityEngine;
using System;

/*
Data structure for information about the "daily_challenge" quest
*/
public class DailyChallenge : Quest
{
	// Only for SIR MOTD display.
	public static long awardAmount;
	public static string awardType;
	public static string challengeTask;

	public static string creditGrantDialogMainText;
	public static string creditGrantDialogTitle;
	public static string mainTextMOTD;
	public static string titleMOTD;
	public static string gameKey;
	public static string gameName;
	public static string endDialogMainText;
	public static string endDialogTitle;
	public static int challengeProgressTarget;
	public static int lastSeenAnnouncementDialog = 0;
	public static int lastSeenOverDialog = 0;

	public static GameTimerRange timerRange;	// This should be in the Quest class instead of using a different timer here.

	public static bool didWin = false;	// Whether the player has won this challenge.

    public const string LAST_SEEN_MOTD_TIMESTAMP_KEY = "daily_challenge_announcement_seen";
    public const string LAST_SEEN_OVER_TIMESTAMP_KEY = "daily_challenge_over_seen";
	
	private static JSON cachedProgressJson = null;

	public static bool isActive
	{
		get
		{
			return 
				!didWin &&
				DailyChallenge.isReady &&
				Quest.activeQuest is DailyChallenge;
		}
	}

	public new static bool isReady
	{
		get
		{
			return 	
				activeQuest != null && 
				timerRange != null &&
				timerRange.isActive;
		}
	}
		
																				  // classic quests fetch JSON from URL, DailyChallenge2 uses EOS
	public DailyChallenge(string questKey, int endDate, JSON data) : base(questKey, endDate, isClassicQuest:(!ExperimentWrapper.DailyChallengeQuest2.isInExperiment))
	{
		// In HIR, some of the following labels get string from SCAT because of localization.
		awardAmount 				= Data.liveData.getInt("QUESTS_DAILY_CHALLENGE_AWARD_AMOUNT", 0);
		awardType = "credits";  	// version 1 only used credits
		challengeTask = "spin";		// version 1 only used spin
		creditGrantDialogMainText	= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_CREDIT_GRANT_DIALOG_MAIN_TEXT", "", "");
		creditGrantDialogTitle 		= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_CREDIT_GRANT_DIALOG_TITLE", "", "");
		mainTextMOTD 				= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_EVENT_ANNOUNCEMENT_MOTD_MAIN_TEXT", "", "");
		titleMOTD 					= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_EVENT_ANNOUNCEMENT_MOTD_TITLE", "", "");
		gameKey 					= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_MACHINE", "", "");
		endDialogMainText 			= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_OVER_DIALOG_MAIN_TEXT", "", "");
		endDialogTitle 				= Data.liveData.getString("QUESTS_DAILY_CHALLENGE_OVER_DIALOG_TITLE", "", "");
		challengeProgressTarget 	= Data.liveData.getInt("QUESTS_DAILY_CHALLENGE_SPIN_AMOUNT", 0);

		// for https://eos.zynga.com/development/#/experiment/5003512/edit/sir_quest_daily_challenge_lite2
		// Only for SIR Daily Challenge.
		if (ExperimentWrapper.DailyChallengeQuest2.isInExperiment)
		{
			// If data is null, disable this feature.
			if (data == null)
			{
				didWin = true;
				return;
			}

			// Use login data instead of EOS. Because login data only gets changed when the challenge ends.
			// But EOS data can change during the challenge.
			gameKey = data.getString("quest_daily_challenge_lite2.game_key", "");
			creditGrantDialogMainText = data.getString("quest_daily_challenge_lite2.event_achieved", "");
			endDialogMainText = data.getString("quest_daily_challenge_lite2.event_over", "");
			challengeProgressTarget = data.getInt("quest_daily_challenge_lite2.challenge_amount", 0);
			challengeTask = data.getString("quest_daily_challenge_lite2.challenge_task", "");
			awardAmount = data.getLong("quest_daily_challenge_lite2.award_amount", 0);
			awardType = data.getString("quest_daily_challenge_lite2.award_type", "");
			mainTextMOTD = data.getString("quest_daily_challenge_lite2.event_motd", "");
			if (mainTextMOTD.Contains("{0}"))
			{
				// do the hyper-economy conversion if EOS event_motd specifies an embedded value (which it should for ease-of-use, to avoid typo bugs for credits_won case)
				// Example: Win {0} coins in Superman: The Movie
				string challengeAmountString = (challengeTask=="credits_won") ?	CreditsEconomy.convertCredits(challengeProgressTarget) : challengeProgressTarget.ToString();
				mainTextMOTD = string.Format(mainTextMOTD,challengeAmountString);
			}
		}

		// Get game key.
		if (!string.IsNullOrEmpty(gameKey))
		{
			// grab the lobby info for this game
			LobbyGame gameInfo = LobbyGame.find(gameKey);
			if (gameInfo != null)
			{
				gameName = gameInfo.name;
			}
		}

		int startVal = Data.liveData.getInt("QUESTS_ACTIVE_QUEST_START_DATE", 0); 
		int endVal = Data.liveData.getInt("QUESTS_ACTIVE_QUEST_END_DATE", 0);
		timerRange = new GameTimerRange(startVal, endVal, true);
	}

	// Note during Init this is called before DailyChallenge constructor, so
	// we cant assume DailyChallenge.challengeTask is valid yet
	public static int GetCollectibleAmountFromJSON(JSON quests_daily_challenge_json)
	{
		if (quests_daily_challenge_json == null)
		{
			Debug.LogErrorFormat("DailyChallenge.cs -- GetCollectingAmountFromJSON -- json is null, breaking out.");
			return 0;
		}
		int challengeProgressStatus = 0;

		string challengeTask;
		if (ExperimentWrapper.DailyChallengeQuest2.isInExperiment)
		{
			challengeTask = quests_daily_challenge_json.getString("quest_daily_challenge_lite2.challenge_task", "");
		}
		else
		{
			Debug.LogWarning("No challenge task defined: Defaulting to spin");
			challengeTask = "spin";
		}

		// questCollectibles differs based on challenge_task
		// translate EOS challenge task values to quest blob progress type, then get right value from quest blob
		// see https://docs.google.com/document/d/1nEhXm1FzJuUMPqCieapHwzV4EFZxlZlHMcgZ7ZuAmOM/edit
		challengeProgressStatus = Quest.GetQuestProgressFromChallengeType(challengeTask, quests_daily_challenge_json);

		return challengeProgressStatus;
	}

	protected override void registerEventDelegates()
	{
		base.registerEventDelegates();
		Server.registerEventDelegate(Quest.MILESTONE_REWARD_EVENT, processWinData, true);
	}

	// The player has won the challenge.
	private static void processWinData(JSON awardJson)
	{
		/* awardJson looks like this:
		 * 
		 *  'reward_type'        => ("xp"/"vip"/"credits")
            'credits'            => $credits,
            'event_type_id'	     => $eventType->getId(),
            'xp_points'          => $xpPoints,
            'vip_points'         => $vipPoints
		 */
		Debug.LogWarning("DailyChallenge win event");
		didWin = true;
		DailyChallengeCreditGrant.showDialog(awardJson);
		CustomPlayerData.setValue(CustomPlayerData.DAILY_CHALLENGE_DID_WIN, true); // Mark this as true for the current quest.
	}
	
	// There is no server event that tells the client when the challenge is expired,
	// so we must rely on the client timer.
	// Returns true if the challenge was lost.
	public static bool checkExpired()
	{
		return !didWin &&
			lastSeenOverDialog < timerRange.startTimestamp && // We didnt already see this dialog.
			(lastSeenAnnouncementDialog >= timerRange.startTimestamp) && // They saw the announcement dialog, and so participated.
			!timerRange.isActive &&
			SlotsPlayer.instance.questCollectibles < challengeProgressTarget;
	}

	// just returns if challenge is still running for anyone (including this user)
	public static bool challengeActive()
	{
		return timerRange.isActive;
	}

	protected override void finishedGettingData()
	{
	}

	// This must get called whenever anything happens that could change the progress,
	// such as spinning or collecting a bonus.
	new public static void invalidateCachedProgress()
	{
		cachedProgressJson = null;
	}

	public static void getChallengeProgressFromServer(Dict args)
	{
	#if ENABLE_CACHED_PROGRESS_JSON_OPTIMIZATION
		if (cachedProgressJson != null)
		{
			// If we have cached data, just use it instead of re-requesting it.
			finishCallback(args);
			return;
		}
	#endif
		
		// to get intermediate progress data before milestone is hit, we need to poll the server, because it only sends events when milestone is hit,
		// not on every big_win/spin/etc.  
		Server.registerEventDelegate(Quest.CHALLENGE_QUEST_UPDATE_EVENT, challengeProgressCallback, args);

		// need to get the latest big_win/credits_won/etc info from server before showing dialog
		// the server action will return and initiate a callback to the dialog
		QuestAction.getQuestUpdate();
	}

	private static void challengeProgressCallback(JSON questsUpdateResponseJson, object data)
	{
		cachedProgressJson = questsUpdateResponseJson.getJSON("daily_challenge");

		Dict args = data as Dict;
		finishCallback(args);
	}
	
	private static void finishCallback(Dict args)
	{
		SlotsPlayer.instance.questCollectibles = DailyChallenge.GetCollectibleAmountFromJSON(cachedProgressJson);

		var callback = args.getWithDefault(D.CALLBACK, null) as Action<string>;
		string motdKey = (string)args.getWithDefault(D.MOTD_KEY, "");
		
		callback(motdKey);		
	}

	// Base class implements IResetGame, so we must do it here to suppress warnings.
	new public static void resetStaticClassData()
	{
		awardAmount = 0;
		awardType = "";
		awardType = "";
		challengeTask = "";
		challengeProgressTarget = 0;
		creditGrantDialogMainText = "";
		creditGrantDialogTitle = "";
		mainTextMOTD = "";
		titleMOTD = "";
		gameKey = "";
		endDialogMainText = "";
		endDialogTitle = "";
		timerRange = null;
		didWin = false;
		cachedProgressJson = null;
	}
}
