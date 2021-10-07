using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Com.Scheduler;

// Challenges Wiki
// https://wiki.corp.zynga.com/display/hititrich/Robust+Challenges

public class ChallengeCampaign
{
	
	public const string challengeResetLocalization = "challenge_reset";
	public const string challengeCompleteLocalization = "challenge_complete";
	
	// =============================
	// PROTECTED
	// =============================
	protected enum ChallengeEvalState
	{
		NOT_VALIDATED,
		INVALID,
		VALID
	}
	
	public bool isForceDisabled { get; protected set; } // set to true if feature is lazy loaded, and bundle is not downloaded

	protected ChallengeEvalState lobbyValidState = ChallengeEvalState.NOT_VALIDATED;
	protected ChallengeEvalState campaignValidState = ChallengeEvalState.NOT_VALIDATED;
	
	// This queue is to store completion reponse in case that several reponses come 
	// at the same time and only the first response gets dealt. With this queue, all reponse can be handled one by one.
	protected Queue<JSON> responseQueue = new Queue<JSON>();
	protected string motdPrefix = "challenges";

	// =============================
	// PUBLIC
	// =============================
	public string campaignID;
	public string variant;
	
	public GameTimerRange timerRange = null;
	public bool shouldRepeat { get; protected set; }
	public int maxReplayLimit { get; protected set; }
	public int replayCount { get; protected set; }
	public float replayGoalRatio { get; protected set; }
	public float replayRewardRatio { get; protected set; }
	public bool hasRecievedProgressUpdate { get; protected set; }
	public List<Mission> missions = new List<Mission>();

	// Current active event data.
	public JSON cachedResponse { get; protected set; }
	public int currentEventIndex  = -1; // Current active event index.
	public int startingEventIndex = -1; //Used for knowing what event index we were a when the feature was first set up
	public long[] progress		  = null; // Progress of challenges in current active event.
	public List<string> gameKeys  = new List<string>(); // Game keys in current active event.
	public string state			  = IN_PROGRESS;
	public bool didUpdateProgress = false;
	public bool isEnabled { get; protected set; }

	// =============================
	// CONST
	// =============================
	protected const string CAMPAIGN_COMPLETE  = "campaign_complete";
	protected const string MISSION_COMPLETE	  = "event_complete";
	protected const string CHALLENGE_COMPLETE = "type_complete";
	public const string CURRENT_CAMPAIGN_ID = "current_campaign_id";

	// states
	public const string COMPLETE	= "complete";
	public const string INCOMPLETE	= "incomplete";
	public const string IN_PROGRESS = "in_progress";
	public const string EVENT_OVER  = "event_over";
	public const string REPLAY = "replay";
	
	protected string campaignErrorString = "";

	private static bool needsUIUpdate = false;

	private List<TaskCompletionSource<bool>> onUpdateFinishedTasks = new List<TaskCompletionSource<bool>>();
	private TICoroutine completionRoutine = null;

	/// <summary>
	/// Parses through login data, and sets up the campaign	 
	/// </summary>
	public virtual void init(JSON data)
	{
		// state parse
		if (data.getBool("hasWon", false) == true)
		{
			state = COMPLETE;
		}

		isEnabled	  = data.getBool("enabled", false);
		int startDate = data.getInt("start_time", 0); 
		int endDate	  = data.getInt("end_time", 0);
		
		shouldRepeat = data.getBool("repeatable", false);
		maxReplayLimit = data.getInt("max_replay_limit", 1);
		replayCount = data.getInt("replay_count", 0);
		
		replayGoalRatio = data.getInt("replay_goal_ratio", 0) / 100.0f;
		replayRewardRatio = data.getInt("replay_reward_ratio", 0) / 100.0f;
		variant = data.getString("id", "");
		
		timerRange = new GameTimerRange(startDate, endDate, true);

		//build objectives
		populateMissions(data);
		
		// Just making sure cachedResponse is set to null.
		invalidateCachedResponse();
		
		// Show the incomplete dialog right after timer gets expired.
		timerRange.clearEvent();
		timerRange.registerFunction(showIncompleteDialog);
		
		//register events specific to this campaign
		registerEvents();

		isLobbyValid();
	}

	public virtual void registerEvents()
	{
		
	}
	
	public virtual void unregisterEvents()
	{
		
	}

	protected virtual void populateMissions(JSON data)
	{
		if (data != null)
		{
			JSON[] missionsArray = data.getJsonArray("events");

			missions = new List<Mission>();
			foreach (JSON eventJSON in missionsArray)
			{
				Mission mission = createMission(eventJSON);
				missions.Add(mission);
			}
		}
	}

	public bool canRestart()
	{
		return shouldRepeat && (
				(replayCount < maxReplayLimit) || (state != COMPLETE));
	}

	public virtual void restart()
	{
		if (!canRestart())
		{
			Debug.LogError("Cannot replay event:  Reached replay limit");
			return;
		}

		//reset the global variables
		state = IN_PROGRESS;
		currentEventIndex = 0;
		startingEventIndex = 0;

		//remove any cached progres
		invalidateCachedResponse();

		for (int i = 0; i < missions.Count; i++)
		{
			missions[i].resetProgress(replayRewardRatio, replayGoalRatio);
			
		}
	}

	public static void scheduleUIUpdate()
	{
		if (!needsUIUpdate)
		{
			Scheduler.addFunction(updateUI);
		}
	}

	// base class creates a basic Mission instance
	protected virtual Mission createMission(JSON data)
	{
		return new Mission(data);
	}
	
	public void invalidateCachedResponse()
	{
		cachedResponse = null;
	}

	private bool canRunCamapaignCompletionEvents()
	{
		return (SlotBaseGame.instance == null || !SlotBaseGame.instance.isGameBusy) &&
		       FreeSpinGame.instance == null &&
		       ChallengeGame.instance == null;
	}
	
	// Called each time when a type is completed.
	public void addTypeCompleteDataToQueue(JSON response)
	{
		//enqueue response
		responseQueue.Enqueue(response);

		//if we can process events and we haven't already started a coroutine, queue up an update at end of frame (to process all events first);
		if (canRunCamapaignCompletionEvents() && completionRoutine == null)
		{
			completionRoutine = RoutineRunner.instance.StartCoroutine(processCompletionNextFrameAsync());
		}
	}

	private IEnumerator processCompletionNextFrameAsync()
	{
		yield return new WaitForEndOfFrame();
		processCompletionInQueueAsync();
		completionRoutine = null;

	}
	
	public void processCompletionInQueueAsync()
	{
		//process the updates
		processCompletionsAndShowRewardsAsync(responseQueue);
		
		//clear the response queue
		responseQueue.Clear();
	}

	public virtual void onCampaignLost(JSON response)
	{
		// does nothing
	}

	private void processCompletionsAndShowRewardsAsync(IEnumerable<JSON> completions)
	{
		//copy data into dictionary for faster parsing and in case the original data is cleaned up before the process finishes
		Dictionary<string, List<JSON>> updateData = new Dictionary<string, List<JSON>>();
		
		bool hasStopped = false;
		foreach (JSON completionJSON in completions)
		{
			string completionType = completionJSON.getString("completion_type", "");

			List<JSON> data = null;
			if (!updateData.TryGetValue(completionType, out data))
			{
				data = new List<JSON>();
				updateData.Add(completionType, data);
			}

			data.Add(completionJSON);

			// stop auto spin early
			if ((completionType == MISSION_COMPLETE || completionType == CAMPAIGN_COMPLETE) &&
			    !hasStopped)
			{
				// prevent desyncs from autospinning during rewards
				hasStopped = true;
				if (SlotBaseGame.instance != null)
				{
					if (SlotBaseGame.instance.hasAutoSpinsRemaining && SpinPanel.instance.autoSpinPanelAnimator != null)
					{
						SpinPanel.instance.hideAutoSpinPanel();
					}
					SlotBaseGame.instance.stopSpin();
				}
			}
		}

		//create a progress update task and wait for completion
		TaskCompletionSource<bool> getProgress = new TaskCompletionSource<bool>();
		onUpdateFinishedTasks.Add(getProgress);
		
		// Fetch latest progress data before showing up completion and reward dialog.
		CampaignDirector.getProgress(campaignID);

		// run callback when the progress update is done
		// Use option TaskContinuationOptions.ExecuteSynchronously
		// the default behavior for ContinueWith if this flag isn’t specified is to run the continuation asynchronously,
		// meaning that when the antecedent task completes, the continuation task will be queued rather than executed.
		getProgress.Task.ContinueWith((Task<bool> task) =>
		{
			UnityMainThreadDispatcher.Instance().Enqueue(()=>
			{
				foreach (KeyValuePair<string, List<JSON>> kvp in updateData)
				{
					switch (kvp.Key)
					{
						case CAMPAIGN_COMPLETE:
							showCampaignComplete(kvp.Value);
							break;

						case MISSION_COMPLETE:
							showMissionComplete(kvp.Value);
							break;

						case CHALLENGE_COMPLETE:
							showTypeComplete(kvp.Value);
							break;

						default:
							Debug.LogError("Can't find completion type - " + kvp.Key);
							break;
					}
					
				}

				// Unlock challenge games in current active event.
				unlockChallengeGame();
			});
					
		},TaskContinuationOptions.ExecuteSynchronously);
	}

	protected virtual void showCampaignComplete(List<JSON> completionJSON)
	{
		state = COMPLETE;
		// If the completion type is NOT "campaign_complete", the value of event_index means the current ACTIVE event,
		// which also means the completedEventIndex should be value "campaign_complete" minus 1.
		// But the value "rewards" means the event index which should be rewarded.
		int completedEventIndex = missions.Count - 1;

		// Show the progress after a completion of an event.
		StatsManager.Instance.LogCount
		(
			"dialog",
			motdPrefix + "_motd",
			variant,
			"event_complete_pop_up",
			(completedEventIndex + 1).ToString(),
			"view"
		);
		
		// For Testing
		onCampaignCompleteCall(campaignID, completionJSON);
	}

	protected virtual void showMissionComplete(List<JSON> completionJSON)
	{
		// If the completion type is NOT "campaign_complete", the value of event_index means the current ACTIVE event,
		// which also means the completedEventIndex should be value "campaign_complete" minus 1.
		// But the value "rewards" means the event index which should be rewarded.
		
		int completedEventIndex = -1;
		
		if (completionJSON != null)
		{	
			for (int i = 0; i < completionJSON.Count; i++)
			{
				if (completionJSON[i] == null)
				{
					continue;
				}
				
				completedEventIndex = completionJSON[i].getInt("event_index", 0) - 1;

				// It's probably on replay and we just rolled over
				if (completedEventIndex == -1 && canRestart())
				{
					completedEventIndex = missions.Count - 1;
				}
		
				if (missions != null && (completedEventIndex >= missions.Count || completedEventIndex < 0))
				{
					Debug.LogError("event " + completedEventIndex + ", is out of range on missions count: " + missions.Count);
					continue;
				}
				else if (missions == null)
				{
					Debug.LogError("Tried to complete a challenge mission that we didn't know about (mission array was null!");;
					continue;
				}

				if (completedEventIndex == missions.Count - 1)
				{
					state = COMPLETE;
				}

				// close all the objectives, they are finished
				missions[completedEventIndex].complete();
			}
		}

		

		StatsManager.Instance.LogCount
		(
			  "dialog",
			  motdPrefix + "_motd",
			  variant,
			  "event_complete_pop_up",
			  (completedEventIndex + 1).ToString(),
			  "view"
		);

		// Update in-game progress label.
		scheduleUIUpdate();
		
		// For Testing
		onMissionCompleteCall(campaignID, completionJSON);
	}

	protected virtual void showTypeComplete(List<JSON> completionJSON)
	{
		string gameKeyName = (GameState.game == null) ? "" : GameState.game.keyName;
		if (completionJSON != null)
		{
			for (int eventIndex = 0; eventIndex < completionJSON.Count; eventIndex++)
			{
				int[] typeIndexes = completionJSON[eventIndex].getIntArray("types");
				int type = -1;

				if (typeIndexes.Length != 0)
				{
					for (int i = 0; i < typeIndexes.Length; i++)
					{
						type = typeIndexes[i];
						StatsManager.Instance.LogCount(
							"robust_challenge",
							"challenge_type_complete",
							campaignID,
							gameKeyName,
							currentEventIndex.ToString(),
							type.ToString()
						);
					}
				}	
			}
			
		}
		
		
		// For Testing
		onChallengeCompleteCall(campaignID, completionJSON);
	}

	protected virtual void showTypeReset(JSON resetJSON)
	{
		string gameKeyName = (GameState.game == null) ? "" : GameState.game.keyName;
		int[] typeIndexes = resetJSON.getIntArray("types");
		int type = -1;

		if (typeIndexes.Length != 0)
		{
			for (int i = 0; i < typeIndexes.Length; i++)
			{
				type = typeIndexes[i];
				StatsManager.Instance.LogCount(
					"robust_challenge",
					"challenge_type_reset",
					campaignID,
					gameKeyName,
					currentEventIndex.ToString(),
					type.ToString()
				);
			}
		}
		// For Testing
		onChallengeResetCall(campaignID, resetJSON);
	}
		
	// Process latest progress data.
	public virtual void onProgressUpdate(JSON response)
	{
		cachedResponse = response;
		hasRecievedProgressUpdate = true;
		
		currentEventIndex = response.getInt("event_index", 0);
		if (startingEventIndex == -1)
		{
			startingEventIndex = currentEventIndex;
		}
		progress  = response.getLongArray("types");
		long[] constraints =  response.getLongArray("constraints");
		List<long>[] constraintCount = null;
		if (constraints != null && constraints.Length > 0)
		{
			constraintCount = new List<long>[constraints.Length];
		
			for (int i = 0; i < constraints.Length; ++i)
			{
				constraintCount[i] = new List<long>();
				constraintCount[i].Add(constraints[i]);
			}
	
		}
		
		gameKeys.Clear();
		
		// If the campaign is complete, the event_index will be equal to RobustChallenges.missions.Count.
		if (currentEventIndex >= missions.Count)
		{
			currentEventIndex = missions.Count - 1;
			if (currentEventIndex >= missions.Count)
			{
				Debug.LogErrorFormat("ChallengeCampaign: Server just sent us an invalid event index! index: {0}, campaign {1}", currentEventIndex, campaignID);
				return;
			}
			
			//set the constraint to the constraint limit for better formatting
			
			if (currentMission.objectives != null)
			{
				progress = new long[currentMission.objectives.Count];
				for (int i = 0; i < currentMission.objectives.Count; i++)
				{
					Objective objective = currentMission.objectives[i];
					progress[i] = objective.amountNeeded;
					
					
					if (objective.constraints != null && constraintCount != null)
					{
						if (constraintCount[i] == null)
						{
							constraintCount[i] = new List<long>();
						}
						constraintCount[i].Clear();
						for (int constraintIndex = 0; constraintIndex < objective.constraints.Count; constraintIndex++)
						{
							constraintCount[i].Add(objective.constraints[constraintIndex].limit);
						}
					}
				}
			}
			else
			{
				progress = null;
			}
			state = COMPLETE;
		}
		
		// close previous missions
		if (currentEventIndex > 0 && currentEventIndex <= missions.Count)
		{
			for (int i = 0; i < currentEventIndex; ++i)
			{
				if (!missions[i].isComplete)
				{
					missions[i].complete();
				}
			}
		}

		// Get game keys to download game icons.
		gameKeys = getGameKeys(currentEventIndex);

		// After updating progress, set this flag to true.
		didUpdateProgress = true;

		// Unlock challenge games in current active event.
		unlockChallengeGame();

		// Update count of completions.
		int typeCount = currentMission.objectives.Count;

		if (progress.Length > 0 && progress.Length == typeCount)
		{
			for (int i = 0; i < typeCount; i++)
			{
				currentMission.updateObjectiveProgress(i, progress[i], (constraintCount != null && constraintCount.Length > i) ? constraintCount[i] : null);
			}
		}
		else
		{
			Bugsnag.LeaveBreadcrumb(string.Format(
				"ChallengeCampaign: Server just sent us an invalid progress update! progress {0} | objectives {1}",
				progress.Length,
				typeCount
			));
		}

		currentMission.checkCompletedObjectives();
		
		// Update in-game progress label.
		scheduleUIUpdate();
		
		
		//tell any tasks that are waiting on update that we're done
		if (onUpdateFinishedTasks != null)
		{
			for (int i = 0; i < onUpdateFinishedTasks.Count; i++)
			{
				onUpdateFinishedTasks[i].SetResult(true);
			}
			
			onUpdateFinishedTasks.Clear();
		}
	}

	public virtual void onProgressReset(JSON response)
	{
		currentEventIndex = response.getInt("event_index", 0);
		if (startingEventIndex == -1)
		{
			startingEventIndex = currentEventIndex;
		}

		// close previous missions
		if (currentEventIndex >= 0 && currentEventIndex < missions.Count)
		{
			missions[currentEventIndex].resetProgress(replayRewardRatio, replayGoalRatio);
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("ChallengeCampaign: Server just sent us an invalid progress reset");
		}

		showTypeReset(response);

		// Update in-game progress label.
		
		Scheduler.addFunction(resetUI, Dict.create(D.KEY, campaignID));
	}

	public static void updateUI(Dict args = null)
	{
		needsUIUpdate = false;
		if (SpinPanel.hir != null)
		{
			if (SpinPanel.hir.featureButtonHandler != null)
			{
				SpinPanel.hir.featureButtonHandler.refreshUI(false);
			}
			SpinPanel.hir.updateChallengeLobbyProgress();
		}
		
		InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.OPTION, true));
		InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_KEY, null);
		InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_COUNTER_KEY, null);
	}

	public virtual void resetUI(Dict args = null)
	{
		string campaignID = null;
		if (args != null)
		{
			campaignID = (string)args.getWithDefault(D.KEY, "");
		}
		
		Scheduler.removeFunction(resetUI);

		if (SpinPanel.hir != null && SpinPanel.hir.featureButtonHandler != null)
		{
			SpinPanel.hir.featureButtonHandler.resetUI(campaignID);
		}
	}

	// Need this wrapper function as a callback in the timer.
	private void showIncompleteDialog(Dict args = null, GameTimerRange originalTimer = null)
	{
		state = INCOMPLETE;
		showOverDialog();
	}

	/// <summary>
	/// Displays the main dialog/displays when the event ends
	/// </summary>
	protected virtual void showOverDialog()
	{
		// does nothing, should be overidden from subclasses
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	///   Returns a list of game keys for the specified mission
	/// </summary>
	public List<string> getGameKeys(int eventIndex)
	{
		if (eventIndex >= missions.Count)
		{
			Debug.LogError("Can't find the event.");
			return null;
		}

		List<string> keys = new List<string>();
		foreach (Objective objective in missions[eventIndex].objectives)
		{
			if (!string.IsNullOrEmpty(objective.game))
			{
				keys.Add(objective.game);
			}
		}
		return keys;
	}

	/// <summary>
	///   Returns a list of all games tied to all missions
	/// </summary>
	public List<string> getGameKeys()
	{
		List<string> keys = new List<string>();
		foreach (Mission mission in missions)
		{
			foreach (Objective objective in mission.objectives)
			{
				if (!string.IsNullOrEmpty(objective.game) && !keys.Contains(objective.game))
				{
					keys.Add(objective.game);
				}
			}
		}
		return keys;
	}

	// Unlock challenge games in current active event.
	protected virtual void unlockChallengeGame()
	{
		for (int i = 0; i < currentMission.objectives.Count; ++i)
		{
			Objective objective = currentMission.objectives[i];
			if (objective.game != null)
			{
				LobbyGame game = LobbyGame.find(objective.game);
				if (game != null)
				{
					game.xp.isPermanentUnlock = true;
					game.setIsUnlocked();
				}
			}
		}
	}

	/// <summary>
	///   Returns a mission that contains the specified gamekey
	/// </summary>
	public virtual Mission findWithGame(string gameKey)
	{
		if (currentMission != null && currentMission.containsGame(gameKey))
		{
			return currentMission;
		}

		for (int i = missions.Count; --i >= 0; )
		{
			if (missions[i].containsGame(gameKey))
			{
				return missions[i];
			}
		}

		return null;
	}

	/// <summary>
	///   Returns true if the lobby is set up correctly to foster the campaign. Base class only checks if any
	///   lola lobby contains the game in the challenge.
	///   <param name="lobby">Optionally pass a specific lobby to check games in</param>
	/// </summary>
	public virtual bool isLobbyValid(LoLaLobby lobby = null)
	{
		campaignErrorString = ""; // Reset this to empty.
		if (lobbyValidState != ChallengeEvalState.NOT_VALIDATED || missions == null || missions.Count == 0)
		{
			return lobbyValidState == ChallengeEvalState.VALID;
		}
		
		
		for (int missionIndex = 0; missionIndex < missions.Count; ++missionIndex)
		{
			Mission mission = missions[missionIndex];
			if (mission == null || mission.objectives == null)
			{
				campaignErrorString = string.Format("mission {0} is invalid or doesn't exist.", missionIndex);
				return false;
			}
			for (int i = mission.objectives.Count; --i >= 0;)
			{
				if (mission.objectives[i] == null)
				{
					campaignErrorString = string.Format("mission object {0} in mission {1} is invalid or doesn't exsit", i, missionIndex);
					return false;
				}
				if (!string.IsNullOrEmpty(mission.objectives[i].game))
				{
					LobbyGame lobbyGame = LobbyGame.find(mission.objectives[i].game);
					if (lobbyGame == null || !lobbyGame.isAllowedLicense)
					{
						lobbyValidState = ChallengeEvalState.INVALID;						
						campaignErrorString = string.Format("Game has no a valid license: {0}", lobbyGame == null ? "[NULL]" : lobbyGame.name);
						return false;
					}

					// specified lobby?
					if (lobby != null)
					{
						if (lobby.findGame(mission.objectives[i].game) == null)
						{
							lobbyValidState = ChallengeEvalState.INVALID;
							campaignErrorString = string.Format("Could not find the lobby game: {0}", mission.objectives[i].game);
							return false;
						}
					}
					// check any lobby
					else
					{
						if (LoLaLobby.findWithGame(mission.objectives[i].game) == null)
						{	
							lobbyValidState = ChallengeEvalState.INVALID;						
							campaignErrorString = string.Format("Game is not in any lola lobby: {0}", mission.objectives[i].game);

							return false;
						}
					}
				}
				
				if ((mission.objectives[i].type == Objective.CARDS_COLLECTED || mission.objectives[i].type == Objective.PACKS_COLLECTED) &&
				    !Collectables.isActive())
				{
					//Disabling challenges if we have collectables related challenges but collectables isn't active
					lobbyValidState = ChallengeEvalState.INVALID;
					campaignErrorString = string.Format("Collectables aren't active: {0}", mission.objectives[i].type);

					return false;
				}
			}
		}

		lobbyValidState = ChallengeEvalState.VALID;
		return true;
		
	}

	/// <summary>
	///   Returns true if the campaign is setup correctly. Base class returns true only, this can be overwritten by subclasses
	///   to add logic validating missions, and objectives are set up as expected.
	/// </summary>
	public virtual bool isCampaignValid()
	{
		return true;
	}

	public static bool hasActiveInstance(string campaignID)
    {
       ChallengeCampaign campaign = CampaignDirector.find(campaignID);
			
	   if (campaign == null)
	   {
		   return false;
	   }
			
	   if (!campaign.isActive)
	   {
			Debug.LogWarning(campaign.campaignID + " not enabled: " + campaign.notActiveReason);
			return false;
	   }
	   return campaign.isEnabled;
    }

	/*=========================================================================================
	GETTERS
	=========================================================================================*/	
	public virtual Mission currentMission
	{
		get 
		{
			if ( currentEventIndex >= 0 )
			{
				if (currentEventIndex < missions.Count)
				{
					return missions[currentEventIndex];
				}
			}
			return null;
		}
	}

	public Mission nextMission
	{
		get
		{
			if (currentEventIndex + 1 <= missions.Count - 1)
			{
				return missions[currentEventIndex + 1];
			}
			return null;
		}
	}

	public bool isComplete
	{
		get
		{
			foreach (Mission mission in missions)
			{
				if (!mission.isComplete)
				{
					return false;
				}
			}
			return true;
		}
	}

	public virtual bool isActive
	{
		get
		{
			return isEnabled && 
					timerRange != null &&
					timerRange.isActive &&
					state == IN_PROGRESS &&
					lobbyValidState == ChallengeEvalState.VALID;
		}
	}

	public virtual string notActiveReason
	{
		get
		{
			string reason = "";
						
			if (!isEnabled)
			{
				reason += campaignID + " is disabled.\n";
			}
			if (timerRange == null || !timerRange.isActive)
			{
				reason += "Not within the feature time range.\n";
			}
			if (state != IN_PROGRESS)
			{
				reason += " The state is not IN_PROGRESS it's " + state;
			}
			if (lobbyValidState == ChallengeEvalState.INVALID)
			{
				reason += "Lobby does not contain all the games in the campaign.\n";
			}
			if (campaignValidState == ChallengeEvalState.INVALID)
			{
				reason += "Campaign is set up incorrectly. Verify games, and objectives are correct.\n";
			}
			reason += campaignErrorString;
			
			return reason;
		}
	}

	protected long getTotalCreditRewardForMission(int completedMissionIndex)
	{
		if (completedMissionIndex < 0 || completedMissionIndex > missions.Count - 1)
		{
			return 0;
		}

		if (missions[completedMissionIndex] == null || missions[completedMissionIndex].rewards == null)
		{
			return 0;
		}

		Mission mission = missions[completedMissionIndex];
		long totalCredits = mission.rewards.
			Where(reward => reward.type == ChallengeReward.RewardType.CREDITS).
			Sum(reward => reward.amount);

		return totalCredits;
	}

	public bool shouldProcess
	{
		get
		{
			return responseQueue.Count > 0;
		}
	}

	public virtual bool isGameUnlocked(string gameKey)
	{
		LobbyInfo lobbyInfo = LobbyInfo.findChallengeLobbyWithGame(gameKey);
		if (lobbyInfo != null)
		{
			foreach (LobbyOption option in lobbyInfo.unpinnedOptions)
			{
				if (option.game != null && option.game.keyName == gameKey)
				{
					return option.sortOrder <= 1 || // first game is always unlocked
						option.sortOrder <= currentEventIndex + 1 || // previous games that were unlocked
						currentMission != null && currentMission.isComplete && // checking for completed current mission, before receiving the next update
						(nextMission == null || nextMission.containsGame(gameKey)); // valid next mission contains the game we are looking for, unlock it
				}
			}
		}

		return false;
	}

	public bool shouldShowDialog
	{
		get
		{
			bool didShow = false;
			foreach (JSON response in responseQueue)
			{
				string completionType = response.getString("completion_type", "");
				if (completionType != CHALLENGE_COMPLETE)
				{
					didShow = true;
				}
			}
			return didShow;
		}
	}

	public bool hasCardRewards()
	{
		for (int i = 0; i < missions.Count; i++)
		{
			for (int j = 0; j < missions[i].rewards.Count; j++)
			{
				if (!string.IsNullOrEmpty(missions[i].rewards[j].cardPackKeyName))
				{
					return true;
				}
			}
		}

		return false;
	}

	// Function to help draw the contents of the campaign in the devGUI.
	public virtual void drawInDevGUI()
	{
		GUILayout.BeginVertical();
		GUILayout.Label(string.Format("Campaign ID: {0}", campaignID));
		GUILayout.Label(string.Format("isEnabled: {0}", isEnabled));
		GUILayout.Label(string.Format("Error string: {0}", campaignErrorString));
		GUILayout.Label(string.Format("isForceDisabled: {0}", isForceDisabled));
		GUILayout.Label(string.Format("Num Missions: {0}", missions.Count));
		GUILayout.Label(string.Format("State: {0}", state));
		GUILayout.Label(string.Format("CurrentEventIndex: {0}", currentEventIndex));
		GUILayout.Label(string.Format("Range Active: {0}", timerRange != null ? timerRange.isActive : false));
		GUILayout.Label(string.Format("Range Left: {0}", timerRange != null ? timerRange.timeRemainingFormatted : "null"));
		GUILayout.EndVertical();
	}

	#region TESTING_DECLARATIONS

	public delegate void campaignEventDelegate(string campaignID, List<JSON> eventData);

	public delegate void campaignSingleEventDelegate(string campaignID, JSON eventData);
	public static event campaignEventDelegate onShowCampaignComplete;
	public static event campaignEventDelegate onShowMissionComplete;
	public static event campaignEventDelegate onShowChallengeComplete;
	public static event campaignSingleEventDelegate onShowChallengeReset;

	public static void onCampaignCompleteCall(string campaignID, List<JSON> eventData)
	{ if (onShowCampaignComplete != null) { onShowCampaignComplete(campaignID, eventData); } }

	public static void onMissionCompleteCall(string campaignID, List<JSON> eventData)
	{ if (onShowMissionComplete != null) { onShowMissionComplete(campaignID, eventData); } }

	public static void onChallengeCompleteCall(string campaignID, List<JSON> eventData)
	{ if (onShowChallengeComplete != null) { onShowChallengeComplete(campaignID, eventData); } }

	public static void onChallengeResetCall(string campaignID, JSON eventData)
	{ if (onShowChallengeReset != null) { onShowChallengeReset(campaignID, eventData); } }
	#endregion
}
