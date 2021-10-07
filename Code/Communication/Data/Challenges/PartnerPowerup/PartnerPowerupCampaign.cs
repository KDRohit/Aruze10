﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartnerPowerupCampaign : ChallengeCampaign, IResetGame
{	
	// =============================
	// PUBLIC
	// =============================
	
	// Partner Powerup Data
	public static string buddyString = ""; // This is a zid
	public static string buddyFBID = "";
	public string buddyFirstName = "";
	public int timeRemaining = 0;
	public long reward = 0;
	public long consolationReward = 0;
	public int pokeLimitTimeFrame = 0;
	public int idleProgressTime = 0;

	// Generic variable names for counting any kind of data/thing.
	public long userProgress;
	public long buddyProgress;
	public long challengeGoal;
	public long individualProgressRequired = 0;

	// Events that may be useful for updating stuff
	public delegate void onCampaignStateChange(Dict args = null);
	public event onCampaignStateChange campaignStateChangeEvent;

	public delegate void onGetCampaignProgress(Dict args = null);
	public event onGetCampaignProgress campaignProgressEvent;

	public string challengeType = "none";

	public long userProgressSinceLastCheck = 0;
	public long buddyProgressSinceLastCheck = 0;

	public GameTimerRange pokeTimeframe;

	public static bool startedAssetLoad = false;
	
	// Assets to load for when we need em.
	public static GameObject lobbyButton = null;
	public static GameObject ppuInGameButton = null;
	public static Texture userTexture = null;
	public static Texture buddyTexture = null;

	// =============================
	// CONST
	// =============================
	public const string LOBBY_PREFAB_PATH = "Features/Partner Powerup/Prefabs/Lobby Option PPU";
	public const string STATUS_BUTTON_PATH = "Features/Partner Powerup/Prefabs/Partner Powerup Overlay Button";
	public const string ANON_TEXTURE_LOCATION = "Features/Partner Powerup/Textures/"; // We decide what texture on the fly.
	private const string CO_OP_GOAL_TYPE = "CO_OP_GOAL_TYPE"; // Live data key to figure out what the goal is.

	// server events
	public const string CO_OP_END = "co_op_ended";
	public const string CO_OP_START = "co_op_start";
	public const string CO_OP_PROGRESS = "co_op_progress";
	public const string CO_OP_COMPLETE = "co_op_complete";
	public const string BUNDLE_NAME = "partner_powerup";

	// Init in a weird way because the server doesn't actually use the right system:
//	"my_goal": 0,
//	"buddy_goal": 0,
//	"my_spins": 0,
//	"buddy_spins": 0,
//	"buddy": "70214708128",
//	"buddy_fb_id": "10152797025292665",
//	"buddy_first_name": "Andy",
//	"time_remaining": 1718685,
//	"co_op_reward": 500000,
//	"consolation_reward": 500,
//	"goal": "coins_won",
//	"required_goal": 4000,
//	"required_spins": 4000,
//	"enabled": true,
//	"poke_limit_timeframe": 3600,
//	"idle_progress_time": 150

	public PartnerPowerupCampaign() : base()
	{
		isForceDisabled = AssetBundleManager.shouldLazyLoadBundle(BUNDLE_NAME);
	}

	public override void init(JSON data = null)
	{	
		unregisterAllDelegates();
		state = IN_PROGRESS;
		campaignID = CampaignDirector.PARTNER_POWERUP;

		// This is now registered in server.cs with the other persistent events to do so as early as we can
		//Server.registerEventDelegate(CO_OP_END, onEnd, true);

		// Register the event delegates even ift eh data is null becuase the onComplete can come down without data.
		registerEventDelegates();
		
		if (data != null)
		{
			invalidateCachedResponse();
			cachedResponse = data;
			// Lets get the assets ASAP
			AssetBundleManager.downloadAndCacheBundle("partner_powerup");

			buddyString = data.getString("buddy", "");
			buddyFBID = data.getString("buddy_fb_id", "-1");

			if (buddyFBID == "0") // If we have no ID this will be 0 but...we need -1
			{
				buddyFBID = "-1";
			}

			buddyFirstName = data.getString("buddy_first_name", Localize.text("partner"));

			timeRemaining = data.getInt("time_remaining", 0);
			reward = CreditsEconomy.economyMultiplier * data.getLong("co_op_reward", 0);
			consolationReward = CreditsEconomy.economyMultiplier * data.getLong("consolation_reward", 0);

			challengeType = data.getString("goal", "spins");

			if (challengeType == "coins_won")
			{
				challengeGoal = CreditsEconomy.economyMultiplier * data.getLong("required_goal", 0);
				userProgress = CreditsEconomy.economyMultiplier * data.getLong("my_goal", 0);
				buddyProgress = CreditsEconomy.economyMultiplier * data.getLong("buddy_goal", 0);
			}
			else
			{
				challengeGoal = data.getLong("required_goal", 0);
				userProgress = data.getLong("my_goal", 0);
				buddyProgress = data.getLong("buddy_goal", 0);
			}

			individualProgressRequired = challengeGoal / 2;

			pokeLimitTimeFrame = data.getInt("poke_limit_timeframe", 3600);
			// Currently there's only 1 possible mission for partner powerup, so lets keep the event index at 0
			currentEventIndex = 0;

			// Show the incomplete dialog right after timer gets expired. Though we can also show it when
			// the complete action comes down so...
			timerRange = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + timeRemaining);
			timerRange.registerFunction(onTimeOut);

			// So partner powerup has a single mission with a single goal. We should however be prepared to change that, should/when the time comes.
			PartnerPowerupMission mission = new PartnerPowerupMission(data);
			missions.Add(mission);

			isEnabled = data.getBool("enabled", false);

			if (userProgress + buddyProgress >= challengeGoal)
			{
				state = ChallengeCampaign.COMPLETE;
			}
			if (buddyString == "pending")
			{
				isEnabled = false;
				state = "PENDING";
			}

			idleProgressTime = data.getInt("idle_progress_time", 0);
			
			// Make sure we can show the status dialog on complete
			if (userProgress < individualProgressRequired)
			{
				PlayerPrefsCache.SetInt(Prefs.HAS_SHOWN_PPU_COMPLETE, 0);
			}

			loadPartnerPowerupAssets();
		}
	}

	public override bool isActive
	{
		get
		{
			return ExperimentWrapper.PartnerPowerup.isInExperiment
				&& CampaignDirector.partner != null
				&& CampaignDirector.partner.isEnabled
				&& CampaignDirector.partner.timerRange.isActive
				&& CampaignDirector.partner.state == ChallengeCampaign.IN_PROGRESS; // No need to show them this, they already won.
		}
	}

	public static void registerStaticEventDelegates()
	{
		Server.registerEventDelegate(CO_OP_START, onStart, true);
		Server.registerEventDelegate(CO_OP_END, onEnd, true);
	}
	
	public void addFunctionToOnStateChange(onCampaignStateChange function)
	{
		campaignStateChangeEvent += function;
	}

	public void addFunctionToOnGetProgress(onGetCampaignProgress function)
	{
		campaignProgressEvent += function;
	}

	public void removeFunctionOnStateChange(onCampaignStateChange function)
	{
		campaignStateChangeEvent -= function;
	}

	public void removeFunctionOnGetProgress(onGetCampaignProgress function)
	{
		campaignProgressEvent -= function;
	}

	//"type":"co_op_ended", happens on login for old events
//		"event":"abfy4kzkDrrpmfGTMUoOQYzyadwrpeBZ4ZTnIoe8TCAZB",
//		"creation_time":"1491331633",
//		"credits":"0","
//		result":"0",
//		"end_time":"1491311700"
	public static void onEnd(JSON data)
	{
		int result = data.getInt("result", 0);
		string eventId = data.getString("event", "");
		int endTime = data.getInt("end_time", GameTimer.currentTime);
		long creditsWon = (long)CreditsEconomy.economyMultiplier * data.getLong("credits", 0L);
		string buddyZid = data.getString("buddy", ""); // ye old buddy zid
		string buddyID = data.getString("buddy_fb_id", "-1"); // Default to "-1" to work with profiles just in case
		long buddyScore = data.getLong("buddy_goal", 1);
		long userScore = data.getLong("my_goal", 1);
		long requiredScore = data.getLong("required_goal", 2) / 2;
		string goalType = data.getString("goal", "");

		if(CampaignDirector.partner != null)
		{
			CampaignDirector.partner.reward = creditsWon;
		}

		if (goalType == "coins_won")
		{
			buddyScore *= CreditsEconomy.economyMultiplier;
			userScore *= CreditsEconomy.economyMultiplier;
		}

		if (buddyID == "0") // If we have no ID this will be 0 but...we need -1
		{
			buddyID = "-1";
		}

		Dict args;
		//		3	Both players won
		//		2	Player won, but buddy did not
		//		1	Buddy won, but player did not
		//		0	Neither player won
		switch (result)
		{
			// Case 0 - 2 are just incomplete states that get handled by the dialogs themselves.
			case 3:
				args = Dict.create(D.END_TIME, endTime,
								   D.AMOUNT, (long)creditsWon,
								   D.DATA, "PAST COMPLETE",
								   D.EVENT_ID, eventId,
								   D.TYPE, "co_op_challenge_complete",
			                       D.PLAYER, buddyZid,
			                       D.FACEBOOK_ID, buddyID,
				                   D.SCORE, userScore,
				                   D.SCORE2, buddyScore,
				                   D.TOTAL_CREDITS, requiredScore
								   );

				PartnerPowerupIntroDialog.showDialog(args);
				break;

			default:
				args = Dict.create(D.END_TIME, (int)endTime,
								   D.AMOUNT, (long)creditsWon,
								   D.DATA, (int)result,
								   D.EVENT_ID, eventId,
								   D.PLAYER, buddyZid,
								   D.FACEBOOK_ID, buddyID
								   );

				PartnerPowerupIncompleteDialog.showDialog(args);
				break;
		}
	}

	// Load the assets. Useful for if the campaign starts while we're playing.
	public static void loadPartnerPowerupAssets()
	{
		startedAssetLoad = true;

		if (CampaignDirector.partner == null)
		{
			RoutineRunner.instance.StartCoroutine(waitForCampaign());
			return;
		}

		AssetBundleManager.load(LOBBY_PREFAB_PATH, partnerPowerIconLoadSuccess, partnerPowerIconLoadFailure);

		// This was getting created twice for reasons. Lets avoid doing that if possible
		if (ppuInGameButton == null)
		{
			AssetBundleManager.load(STATUS_BUTTON_PATH, partnerPowerIconLoadSuccess, partnerPowerIconLoadFailure);
		}
	}

	private static IEnumerator waitForCampaign()
	{
		while (CampaignDirector.partner == null)
		{
			yield return null;
		}

		loadPartnerPowerupAssets();

		yield return null;
	}

	// Takes the player pref string used for saving a users icon path
	private static string getPPUIconString(string playerPrefString)
	{
		string userIcon = PlayerPrefsCache.GetString(playerPrefString, "");

		if (userIcon == "")
		{
			int random = Random.Range(1, 6);
			userIcon = ANON_TEXTURE_LOCATION + "profile_" + random;
			PlayerPrefsCache.SetString(playerPrefString, userIcon);
		}

		return userIcon;
	}

	private static void onDownloadUserImage(string assetPath, Object obj, Dict data = null)
	{
		userTexture = obj as Texture;
	}

	private static void onDownloadBuddyImage(string assetPath, Object obj, Dict data = null)
	{
		buddyTexture = obj as Texture;
	}

	private static void partnerPowerIconLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		switch (assetPath)
		{

			case LOBBY_PREFAB_PATH:
				// Load things one after the other.
				PartnerPowerupCampaign.lobbyButton = obj as GameObject;
				break;

			case STATUS_BUTTON_PATH:
				ppuInGameButton = obj as GameObject;
				break;

			default:
				Debug.LogError("Got an asset idk what to do with " + assetPath);
				break;
		}
	}

	public static void partnerPowerIconLoadFailure(string assetPath, Dict data = null)
	{
		Debug.LogError("PartnerPowerupCampaign::partnerPowerIconLoadFailure - Failed to load asset at: " + assetPath);
	}

	public static void onStart(JSON responseData)
	{
		if (CampaignDirector.partner != null)
		{
			CampaignDirector.partner.startCallback(responseData);
		}
	}

	public void startCallback(JSON responseData)
	{
		// I saw some double on starts. Stop it.
		Server.unregisterEventDelegate(CO_OP_START, onStart, true);
		init(responseData);
		NotificationAction.sendPartnerPowerupPairedNotif();
		Dict args = Dict.create(D.DATA, "START");
		PartnerPowerupIntroDialog.showDialog(args);
	}
	
	public void onGetProgress(JSON responseData = null)
	{
		cachedResponse = responseData;

		long newPlayerProgress;
		long newBuddyProgress;

		if (challengeType == "coins_won")
		{
			newPlayerProgress = CreditsEconomy.economyMultiplier * responseData.getLong("my_goal", 0);
			newBuddyProgress = CreditsEconomy.economyMultiplier * responseData.getLong("buddy_goal", 0);
		}
		else
		{
			newPlayerProgress = responseData.getLong("my_goal", 0);
			newBuddyProgress = responseData.getLong("buddy_goal", 0);
		}

		// This is used so when we open the intro dialog, we can decide on what hammer animation to play.
		userProgressSinceLastCheck += newPlayerProgress - userProgress;
		buddyProgressSinceLastCheck += newBuddyProgress - buddyProgress;

		userProgress = newPlayerProgress;
		buddyProgress = newBuddyProgress;

		// Only 1 mission/objective at any given time so, update it.
		if (missions != null && missions[0] != null)
		{
			missions[0].updateObjectiveProgress(0, userProgress + buddyProgress, null);
		}

		// May or may not be relevant
		if (campaignProgressEvent != null)
		{
			campaignProgressEvent();
		}

		// Show the user met their goal animation in the intro dialog if it isn't open
		if (userProgress >= individualProgressRequired &&
			PlayerPrefsCache.GetInt(Prefs.HAS_SHOWN_PPU_COMPLETE) == 0 &&
			Dialog.instance.findOpenDialogOfType("partner_power_intro") == null) // Greater than or equal to in case of shenanigans
		{
			// for stats
			SocialMember buddy = SocialMember.find("-1", buddyString);
			int buddyVIPLevel = -1;

			if (buddy != null)
			{
				buddyVIPLevel = buddy.vipLevel;
			}

			StatsManager.Instance.LogCount("co_op_challenge", "individual_complete", challengeType, SlotsPlayer.instance.vipNewLevel.ToString(), buddyVIPLevel.ToString(), individualProgressRequired.ToString());
			// Show the "intro dialog". which is also the status dialog.
			Dict args = Dict.create(D.DATA, "USERCOMPLETE");
			PartnerPowerupIntroDialog.showDialog(args);
			NotificationAction.sendPartnerPowerupUserCompleteNotif();
		}
	}

	// For debug
	public void onGetProgress(int playerAmount = 0, int buddyAmount = 0)
	{
		long newPlayerProgress;
		long newBuddyProgress;

		if (challengeType == "coins_won")
		{
			newPlayerProgress = CreditsEconomy.economyMultiplier * playerAmount;
			newBuddyProgress = CreditsEconomy.economyMultiplier * buddyAmount;
		}
		else
		{
			newPlayerProgress = playerAmount;
			newBuddyProgress = buddyAmount;
		}

		// This is used so when we open the intro dialog, we can decide on what hammer animation to play.
		userProgressSinceLastCheck += (int)newPlayerProgress - userProgress;
		buddyProgressSinceLastCheck += (int)newBuddyProgress - buddyProgress;

		userProgress = (int)newPlayerProgress;
		buddyProgress = (int)newBuddyProgress;

		// Only 1 mission/objective at any given time so, update it.
		if (missions != null && missions[0] != null)
		{
			missions[0].updateObjectiveProgress(0, userProgress + buddyProgress, null);
		}

		// May or may not be relevant
		if (campaignProgressEvent != null)
		{
			campaignProgressEvent();
		}

	}

//	{
//		"type": "co_op_complete",
//		"event": "kFBUJfHw1S9D6S8zfvU6yHYZosx81ahm5cXrcAjBHTt5k",
//		"creation_time": 1476475162,
//		"credits": 500000,
//		"result": 3
//		"end_time":"1489075200"
//	}
	public void onComplete(JSON responseData = null)
	{
		// Wait until we have our buddy in case we won right as we logged in.
		if (!SocialMember.isFriendsPopulated)
		{
			RoutineRunner.instance.StartCoroutine(waitForSocialMember(responseData));
			return;
		}

		string eventId = "";
		int buddyVIPLevel = -1;
		int result = responseData.getInt("result", 0);
		string buddyZid = responseData.getString("buddy", "");
		string buddyID = responseData.getString("buddy_fb_id", "-1"); // Default to "-1" to work with profiles just in case
		long creditsWon = CreditsEconomy.economyMultiplier * responseData.getLong("credits", 0L);
		reward = creditsWon;
		if (buddyID == "0") // If we have no ID this will be 0 but...we need -1
		{
			buddyID = "-1";
		}

		unregisterAllDelegates();
		isEnabled = false;

		// for stats
		SocialMember buddy = SocialMember.find("-1", buddyString);

		if (buddy != null)
		{
			buddyVIPLevel = buddy.vipLevel;
		}

		StatsManager.Instance.LogCount("co_op_challenge", "team_complete", challengeType, SlotsPlayer.instance.vipNewLevel.ToString(), buddyVIPLevel.ToString(), individualProgressRequired.ToString());

		// The else statement should always be hit unless forcing from the dev menu.
		if (responseData == null)
		{
			// For testing.
			state = COMPLETE;
		}
		else
		{

			eventId = responseData.getString("event", "");

			switch (result)
			{
			// Case 0 - 2 are just incomplete states that get handled by the dialogs themselves.
			case 3:
				state = COMPLETE;
				break;

			// so this gets triggered off the timer expiring as well. Might as well toss it here just in case.
			default:
				state = INCOMPLETE;
				break;
			}
		}

		// Only 1 mission/objective at any given time so, update it (if complete)
		if (missions != null && missions[0] != null && state == COMPLETE)
		{
			missions[0].updateObjectiveProgress(0, (int)challengeGoal, null);
			missions[0].checkCompletedObjectives();
		}

		// Fire things off
		if (campaignStateChangeEvent != null)
		{
			campaignStateChangeEvent();
		}

		DialogBase statusDialog = Dialog.instance.findOpenDialogOfType("partner_power_intro");

		// If the status dialog is open, it will have handled things itself from campaignStateChangeEvent firing
		if (statusDialog == null && state == COMPLETE)
		{
			Dict args = Dict.create(D.DATA, "COMPLETE",
									D.EVENT_ID, eventId,
									D.TYPE, "co_op_challenge_complete",
			                        D.PLAYER, buddyZid,
			                        D.FACEBOOK_ID, buddyID,
			                        D.AMOUNT, creditsWon
			                       );
			
			PartnerPowerupIntroDialog.showDialog(args);
		}
		// I have this going off on a timer at the moment, but just in case server decides to use this as they said they would...
		else if (state == INCOMPLETE)
		{
			if (statusDialog != null && timeRemaining <= 0)
			{
				Dialog.close(statusDialog);
			}

			// Show this dialog when we're incomplete.
			int endTimeStamp = responseData.getInt("end_time", GameTimer.currentTime);
			Dict args = Dict.create(D.END_TIME, (int)endTimeStamp,
									D.EVENT_ID, eventId,
									D.DATA, result,
									D.PLAYER, buddyZid,
									D.FACEBOOK_ID, buddyID,
									D.AMOUNT, creditsWon
								   );

			PartnerPowerupIncompleteDialog.showDialog(args);
		}
	}

//	"type": "co_op_credits",
//	"event": "0BTgjeLCVEIT8tDKPXg4E1HHvu7jIWmvMwybeRAdDQ80Y",
//	"creation_time": 1488854353,
//	"credits": 500000
	public static void onGetCredits(JSON data)
	{
		long creditReward = data.getLong("credits", 0);
		SlotsPlayer.addCredits(creditReward, "PPU Complete");
	}

	private void onTimeOut(Dict args = null, GameTimerRange sender = null)
	{
		PartnerPowerupAction.getProgress();
	}

	private void unregisterAllDelegates()
	{
		Server.unregisterEventDelegate(CO_OP_PROGRESS, onGetProgress, true);
		Server.unregisterEventDelegate(CO_OP_COMPLETE, onComplete, true);

		// A case could be made to keep on end, but we shouldn't be chaining PPU instances
		// like... 3 a day or anything. And we keep start anyway, so this should just get
		// re-setup
		Server.unregisterEventDelegate(CO_OP_END, onEnd, true);

		// stop any events.
		campaignProgressEvent = null;
		campaignStateChangeEvent = null;
	}

	private void registerEventDelegates()
	{
		Server.registerEventDelegate(CO_OP_PROGRESS, onGetProgress, true);
		Server.registerEventDelegate(CO_OP_COMPLETE, onComplete, true);
	}

	// Wait until we have buddy info before showing a complete dialog
	private IEnumerator waitForSocialMember(JSON dataToUse)
	{
		while (!SocialMember.isFriendsPopulated)
		{
			yield return null;
		}

		onComplete(dataToUse);

		yield return null;
	}

	protected virtual PartnerPowerupMission createPPUMission(JSON data)
	{
		return new PartnerPowerupMission(data);
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		startedAssetLoad = false;
		buddyTexture = null;
		lobbyButton = null;
		userTexture = null;
	}

}
