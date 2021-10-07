﻿﻿﻿﻿using System;
using UnityEngine;
using System.Collections.Generic;

// This class contains information about a particular games royal rush. 

public class RoyalRushInfo
{
	// So we can't try to register for more than one rush at a time since it causes issues
	private static bool isLocked = false;
	private static GameTimerRange lockTimeout;
	
	// Seconds
	private static int LOCK_TIMEOUT = 5;

	public bool registrationIsLocked
	{
		get { return isLocked; }
		private set { isLocked = value; }
	}
	
	public enum STATE { 
		AVAILABLE,
		STARTED,
		SPRINT,
		COMPLETE,
		PAUSED,
		UNAVAILABLE,
		NONE
	};

	// What game is running this
	public string gameKey;
	public string rushKey = "";
	public long userScore = 0;
	public long lastRoundFinalScore = 0;
	public long highScore = 0;
	public int sprintTimeLeft = 0;
	public int timeMeterProgress = 0;
	public int timeMeterThreshold = 0;
	public int competitionRank = -1; //Negative if unranked. Current rank on the submitted scores leaderboard
	public RoyalRushUser previousWinner;

	// These are just for the event ending more or less
	public long endTime = 0;
	public int finalRank = 0; // for the user
	public long creditsAwarded = 0;

	// Who's playing
	public List<RoyalRushUser> userInfos;

	// What are the prizes
	public List<RoyalPrizeInfo> prizeList;

	// Contains info for a given sprint. We'll get it from a central event listener 
	// In the RoyalRushEvent class, lookup the appropriate info, and pass the JSON through here. 
	// This event will then get called to update anyone who cares about new info.
	public delegate void onGetRoyalRushInfo(Dict args = null);
	public event onGetRoyalRushInfo onGetInfo;

	public delegate void onRoyalRushEnd(Dict args = null);
	public event onRoyalRushEnd onEndRush;

	public delegate void onRegisteForRush(Dict args = null);
	public event onRegisteForRush onRushRegister;

	public delegate void onRoyalRushCompetetionEnd(Dict args = null);
	public event onRoyalRushCompetetionEnd onEndCompetetion;

	// For the 2 minute sprints, and the whole rush.
	public GameTimerRange rushSprintTimer;
	public GameTimerRange rushFeatureTimer;

	public STATE currentState = STATE.NONE;

	// Gets created by RoyalRushEvent and fed data from there as well. 
	public RoyalRushInfo(JSON data, bool isEventComplete, string eventKey = "")
	{
		int timeLeft = data.getInt("time_left", -1);
		sprintTimeLeft = data.getInt("sprint_time_left", -1);

		if (isEventComplete)
		{
			endTime = data.getLong("event_end_time", GameTimer.currentTime);
			finalRank = data.getInt("competition_rank", -1);
			creditsAwarded = data.getLong("credits", 0);
		}

		rushKey = eventKey;

		gameKey = data.getString("game_key", "");
		userScore = data.getLong("score", userScore);
		highScore = data.getLong("high_score", highScore);

		competitionRank = data.getInt("competition_rank", competitionRank);
		rushFeatureTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + timeLeft);
		rushFeatureTimer.registerFunction(onFeatureTimeout);
		timeMeterProgress = data.getInt("progress", 0);
		timeMeterThreshold = data.getInt("threshold", 0);

		JSON previousWinnerJSON = data.getJSON("previous_winner");
		if (previousWinnerJSON != null)
		{
			previousWinner = new RoyalRushUser(previousWinnerJSON);

			// We only really need this zid here.
			if (string.IsNullOrEmpty(previousWinner.zid))
			{
				previousWinner = null;
			}
		}

		string state = data.getString("state", "none").ToUpper();
		if (System.Enum.IsDefined(typeof(STATE), state))
		{
			currentState = (STATE)System.Enum.Parse(typeof(STATE), state);
		}
		else
		{
			Debug.LogErrorFormat("Royal Rush state {0} is invalid in data.", state);
		}
			
		if (sprintTimeLeft > 0 && (currentState == STATE.SPRINT || currentState == STATE.PAUSED))
		{
			rushSprintTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + sprintTimeLeft);
			rushSprintTimer.registerFunction(onSprintTimeout);
		}

		if (sprintTimeLeft == 0 && currentState == STATE.SPRINT)
		{
			sendScore();
		}

		JSON[] rankingsArray = data.getJsonArray("rankings", true);
		JSON[] prizeArray = data.getJsonArray("prizes", true);

		// If there's rankings available to use...
		if (rankingsArray != null)
		{
			userInfos = new List<RoyalRushUser>();
			for (int i = 0; i < rankingsArray.Length; i++)
			{
				if (rankingsArray[i] != null)
				{
					userInfos.Add(new RoyalRushUser(rankingsArray[i]));
				}
			}
		}

		// If there's prizes..
		if (prizeArray != null)
		{
			prizeList = new List<RoyalPrizeInfo>();
			for (int i = 0; i < prizeArray.Length; i++)
			{
				if (prizeArray[i] != null)
				{
					prizeList.Add(new RoyalPrizeInfo(prizeArray[i]));
				}
			}
		}
	}

	// Callbacks are handled in RoyalRushEvent and passed down. 
	// We could just call these directly from RoyalRushAction if we have to.
	public void registerForRush()
	{
		if (!isLocked)
		{
			lockTimeout = GameTimerRange.createWithTimeRemaining(LOCK_TIMEOUT);
			lockTimeout.registerFunction(onLockTimeout);
			isLocked = true;
			RoyalRushAction.startSprint(gameKey);
		}
		else
		{
			Debug.LogError("RoyalRushInfo::registerForRush - We tried to register for multiple rushes while waiting on a callback");
		}
	}

	private void onLockTimeout(Dict args, GameTimerRange sender)
	{
		isLocked = false;
	}
	
	public void getInfoForRush()
	{
		RoyalRushAction.getUpdate(gameKey);
	}

	public void sendScore()
	{
		RoyalRushAction.submitScore(gameKey);
	}

	#region timer functions

	// These can probably be "handled" by the server in some sense, since we need parity on
	// the client side. BUT when the timer is out, the timer is out.
	private void onSprintTimeout(Dict args = null, GameTimerRange parent = null)
	{
		//Might happen if we hit a bonus game close to the wire and client animations cause us to "timeout" 
		if (currentState == STATE.PAUSED && sprintTimeLeft > 0)
		{
			return;
		}

		if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && GameState.game != null && GameState.game.keyName == gameKey) //We're submitting the score for the RR game we're currently in so we need to block spins
		{
			if (Overlay.instance.jackpotMysteryHIR.tokenBar != null && Overlay.instance.jackpotMysteryHIR.tokenBar as RoyalRushCollectionModule != null)
			{
				(Overlay.instance.jackpotMysteryHIR.tokenBar as RoyalRushCollectionModule).disableSpinsOnTimeOut();
			}
			else
			{
				Debug.LogError("Royal Rush UI Bar is null. Just sending the score without disabling spins");
				sendScore();
			}
		}
		else
		{
			sendScore();
		}
	}

	private void onFeatureTimeout(Dict args = null, GameTimerRange parent = null)
	{
		// We'll probably do a couple things here...
		if (onEndCompetetion != null)
		{
			onEndCompetetion();	
		}
	}

	public bool inWithinRegistrationTime()
	{
		return (rushFeatureTimer != null && rushFeatureTimer.timeRemaining > RoyalRushEvent.minTimeRequired);	
	}

	#endregion

	#region server callbacks
	// This gets passed down from RoyalRushEvent.
	// We'll want to update via this data, THEN dispatch the event.
	public void onGetRushInfo(JSON data)
	{
		isLocked = false;
		if (lockTimeout != null)
		{
			lockTimeout.clearEvent();
		}
		
		// Update state
		string state = data.getString("state", "none").ToUpper();

		// Update scores and data
		JSON[] rankingsArray = data.getJsonArray("rankings", true);
		JSON[] prizeArray = data.getJsonArray("prizes", true);
		sprintTimeLeft = data.getInt("sprint_time_left", -1);
		int featureTimeLeft = data.getInt("time_left", -1);

		userScore = data.getLong("score", userScore);
		highScore = data.getLong("high_score", highScore);

		rushFeatureTimer.updateEndTime(featureTimeLeft);
		competitionRank = data.getInt("competition_rank", competitionRank);
		timeMeterProgress = data.getInt("progress", 0);
		timeMeterThreshold = data.getInt("threshold", 0);

		if (System.Enum.IsDefined(typeof(STATE), state))
		{
			currentState = (STATE)System.Enum.Parse(typeof(STATE), state);
		}
		else
		{
			Debug.LogErrorFormat("Royal Rush state {0} is invalid in data.", state);
		}

		if (currentState == STATE.AVAILABLE)
		{
			if (userInfos != null)
			{
				userInfos.Clear();
			}
		}

		JSON previousWinnerJSON = data.getJSON("previous_winner");
		if (previousWinnerJSON != null)
		{
			previousWinner = new RoyalRushUser(previousWinnerJSON);

			if (string.IsNullOrEmpty(previousWinner.zid))
			{
				previousWinner = null;
			}
		}

		// If there's rankings available to use...
		if (rankingsArray != null && rankingsArray.Length > 0)
		{
			// We could just clear it but, whatever, this is ok too.
			userInfos = new List<RoyalRushUser>();
			for (int i = 0; i < rankingsArray.Length; i++)
			{
				if (rankingsArray[i] != null)
				{
					userInfos.Add(new RoyalRushUser(rankingsArray[i]));
				}
			}
		}

		if (prizeArray != null && prizeArray.Length > 0)
		{
			prizeList = new List<RoyalPrizeInfo>();
			for (int i = 0; i < prizeArray.Length; i++)
			{
				if (prizeArray[i] != null)
				{
					prizeList.Add(new RoyalPrizeInfo(prizeArray[i]));
				}
			}
		}

		if (currentState == STATE.SPRINT || currentState == STATE.PAUSED) //Only want to count down sprint time if we're in an active sprint
		{
			if (rushSprintTimer != null)
			{
				if (rushSprintTimer.isExpired)
				{
					rushSprintTimer.updateEndTime(sprintTimeLeft);
					rushSprintTimer.registerFunction(onSprintTimeout);
				}
				else
				{
					if (currentState != STATE.PAUSED || !rushSprintTimer.endTimer.isPaused) //Only update the timer if we're not in the paused state or if we're in the paused state and the timer isn't paused yet
					{
						rushSprintTimer.updateEndTime(sprintTimeLeft);
					}
				}
			}
			else
			{
				rushSprintTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + sprintTimeLeft);
				rushSprintTimer.registerFunction(onSprintTimeout);
			}
		}

		Dict onGetInfoDict = Dict.create(D.DATA, data);

		if (onGetInfo != null)
		{
			onGetInfo(onGetInfoDict);	
		}

		// Makes it way easier to get into games from multiple venues. (1x1, 1x2)
		if (onRushRegister != null)
		{
			onRushRegister(onGetInfoDict);
			onRushRegister = null;
		}
	}

	public void onGetRushEnd(JSON data)
	{
		// Update scores
		RoyalRushEvent.waitingForSprintSummary = false;
		JSON[] rankingsArray = data.getJsonArray("rankings", true);

		// If there's rankings available to use...
		if (rankingsArray != null)
		{
			userInfos = new List<RoyalRushUser>();
			for (int i = 0; i < rankingsArray.Length; i++)
			{
				if (rankingsArray[i] != null)
				{
					userInfos.Add(new RoyalRushUser(rankingsArray[i]));
				}
			}
		}

		// We already know the state
		currentState = STATE.COMPLETE;
		highScore = data.getLong("high_score", highScore);
		lastRoundFinalScore = data.getLong("ended_score", lastRoundFinalScore);

		// Setting this here might be redundant but it IS safer.
		userScore = data.getLong("ended_score", userScore);
		competitionRank = data.getInt("competition_rank", competitionRank);
		timeMeterThreshold = data.getInt("threshold", 0);

		if (onEndRush != null)
		{
			onEndRush();
		}
	}
	#endregion
}

