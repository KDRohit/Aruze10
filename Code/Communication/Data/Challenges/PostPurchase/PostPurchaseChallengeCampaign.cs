using Com.Scheduler;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class PostPurchaseChallengeCampaign : ChallengeCampaign
{
	public event System.Action<string> postPurchaseChallengeUnlocked;
	public event System.Action postPurchaseChallengeProgressUpdated;
	public event System.Action postPurchaseChallengeCompleted;
	public event System.Action postPurchaseChallengeMaxBonusUpdated;
	public event System.Action postPurchaseChallengeEnded;
	
	public const string ICON_TOP_BAR_BASE_PATH = "Features/Post Purchase Pinata/Common/Textures/Pinata Icon Topbar";
	public const string ITEM_BASE_PATH = "Features/Post Purchase Pinata/Themes/{0}/Prefabs/Character Item {0}";
	public const string REMINDER_TOOLTIP_PREFAB_PATH = "Features/Gift Chest Offer/Prefabs/Gift Chest Offer Tooltip";
	public const int REMINDER_DURATION = 300;
	
	private GameTimerRange runningTimer = null;
	private GameTimerRange reminderTimer = null;
	//This is used to hide the buy button and buy page changes so the players can't purchase x mins before the challenge end date
	//where x is the duration a challenge lasts once purchased.
	private GameTimerRange earlyEndTimer = null; 

	private const string UNLOCK_ENTRY = "challenge_campaign_purchase_unlock_entry";
	
	public int duration { get; private set; } //the total duration a challenge lasts once peurchased
	private int timeRemaining;  //the time remaining at login
	public bool isLocked { get; private set; }
	public Texture2D icon { get; private set; }

	private class LabelData
	{
		public LabelData(TextMeshPro txt, GameTimerRange.TimeFormat fmt)
		{
			label = txt;
			format = fmt;
		}
		public void updateFormat(GameTimerRange.TimeFormat fmt)
		{
			format = fmt;
		}
		public TextMeshPro label { get; private set; }
		public GameTimerRange.TimeFormat format { get; private set; }
	}
	
	private List<LabelData> runningTimeLabels;
	private List<GameTimerRange.onExpireDelegate> reminderCallbacks;
	
	public static bool isAnyPostPurchaseChallengeActive
	{
		get { return getActivePostPurchaseChallengeCampaign() != null; }
	}
	
	public bool isRunning
	{
		get
		{
			return isActive && runningTimer != null && runningTimer.timeRemaining > 0;
		}
	}

	public int runningTimeRemaining
	{
		get
		{
			return runningTimer != null ? runningTimer.timeRemaining : -1;
		}
	}

	public int purchaseTimeRemaining
	{
		get
		{
			return earlyEndTimer != null ? earlyEndTimer.timeRemaining : -1;
		}
	}

	public int reminderTimeRemaining
	{
		get
		{
			return reminderTimer != null ? reminderTimer.timeRemaining : -1;
		}
	}

	public bool hasReminderEvent
	{
		get
		{
			return isActive && reminderTimer != null && reminderTimer.timeRemaining > 0;
		}
	}

	public void registerReminderCallback(GameTimerRange.onExpireDelegate function)
	{
		if (reminderTimer == null)
		{
			Debug.LogWarning("Trying to register a callback to a timer that doesn't exist");
			return;
		}

		//add to list in case we need to reconstruct timer
		if (reminderCallbacks == null)
		{
			reminderCallbacks = new List<GameTimerRange.onExpireDelegate>();
		}
		
		if (!reminderCallbacks.Contains(function))
		{
			reminderCallbacks.Add(function);
			reminderTimer.registerFunction(function);
		}
		
	}

	public void registerRunningTimeLabel(TextMeshPro label, GameTimerRange.TimeFormat format)
	{
		if (runningTimer == null)
		{
			Debug.LogWarning("Trying to register a label to a timer that doesn't exist");
			return;
		}

		if (runningTimeLabels == null)
		{
			runningTimeLabels = new List<LabelData>();
		}
		
		//check if we've already register this label
		for (int i = 0; i < runningTimeLabels.Count; i++)
		{
			if (runningTimeLabels[i].label == label)
			{
				//unregister and re-register with new format
				runningTimer.removeLabel(label);
				runningTimer.registerLabel(label, format);
				runningTimeLabels[i].updateFormat(format);
				return;
			}
		}

		//save data in case we modify timer
		LabelData data = new LabelData(label, format);
		runningTimeLabels.Add(data);
		
		//register label
		runningTimer.registerLabel(label, format);
	}
	
	public override bool isActive
	{
		get
		{
			return isEnabled &&
			       timerRange != null &&
			       timerRange.isActive &&
			       state != INCOMPLETE && // one key difference from normal campaigns, you can play the games even after the campaign has finished
			       campaignValidState == ChallengeEvalState.VALID; // second key difference is validating the campaign is setup correctly
		}
	}

	public bool isEarlyEndActive
	{
		get { return isActive && earlyEndTimer != null && earlyEndTimer.timeRemaining > 0 && (runningTimer == null || runningTimer.timeRemaining > 0); }
	}

	public static PostPurchaseChallengeCampaign getActivePostPurchaseChallengeCampaign()
	{
		return CampaignDirector.find(CampaignDirector.POST_PURCHASE_CHALLENGE) as PostPurchaseChallengeCampaign;
	}

	public static JSON pendingLostData = null;
	public static void handleLost(JSON data)
	{
		pendingLostData = data;
		if (pendingLostData != null)
		{
			handleChallengeLost(pendingLostData);
		}
	}

	public override void init(JSON data)
	{
		base.init(data);
		setupCampaingValidity();
		campaignID = CampaignDirector.POST_PURCHASE_CHALLENGE;
		isLocked = data.getBool("entry_locked", true);
		duration = data.getInt("duration", 0);
		timeRemaining = data.getInt("time_remaining", 0);
		if (timeRemaining > 0)
		{
			runningTimer = GameTimerRange.createWithTimeRemaining(timeRemaining);
			runningTimer.registerFunction(onChallengeLost);
			int reminder = timeRemaining - REMINDER_DURATION;
			reminderTimer = GameTimerRange.createWithTimeRemaining(reminder >= 0 ? reminder : 10);
		}
		
		int earlyEndDuration = timerRange.timeRemaining - duration;
		if (earlyEndDuration >= 0)
		{
			earlyEndTimer = GameTimerRange.createWithTimeRemaining(earlyEndDuration);
			earlyEndTimer.registerFunction(hideEventDisplay);
		}
		else
		{
			hideEventDisplay();
		}
			
		//init is also called when the challenge timer ends and the campaign resets
		//So unregister first so we don't fire onCampaignUnlocked multiple times when challenge is again unlocked in the same session
		Server.unregisterEventDelegate(UNLOCK_ENTRY, onCampaignUnlocked, true);
		Server.registerEventDelegate(UNLOCK_ENTRY, onCampaignUnlocked, true);
		
		if (icon == null)
		{
			AssetBundleManager.load(ICON_TOP_BAR_BASE_PATH, onPostPurchaseLoadSuccess, onPostPurchaseLoadFail);
		}
	}
	
#if !ZYNGA_PRODUCTION
	public void devEndTimerInSeconds(int seconds)
	{
		if (runningTimer != null)
		{
			runningTimer.clearLabels();
			runningTimer.removeFunction(onChallengeLost);
			runningTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + seconds);
			runningTimer.registerFunction(onChallengeLost);
			registerLabelsToRunningTimer();
		}
	}

	public void devEndReminderTimerInSecons(int seconds)
	{
		if (reminderTimer != null)
		{
			if (reminderCallbacks != null)
			{
				for (int i = 0; i < reminderCallbacks.Count; i++)
				{
					if (reminderCallbacks[i] != null)
					{
						reminderTimer.removeFunction(reminderCallbacks[i]);
					}
				}
			}

			reminderTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + seconds);
			registerCallbacksToReminderTimer();
		}
	}

	public void devUnlock(int challengeDuration)
	{
		isLocked = false;
		timeRemaining = challengeDuration; //when unlocked timeRemaining is the total duration
		
		clearTimers();
		if (runningTimer != null)
		{
			runningTimer.clearLabels();	
		}
		runningTimer = GameTimerRange.createWithTimeRemaining(timeRemaining);
		reminderTimer = GameTimerRange.createWithTimeRemaining(timeRemaining-REMINDER_DURATION);
		
		//TODO: Construct fake mission
		//populateMissions(data.getJSON("campaign"));
		
		if (postPurchaseChallengeUnlocked != null)
		{
			postPurchaseChallengeUnlocked(null);
		}
		runningTimer.registerFunction(onChallengeLost);
	}
#endif

	private void registerLabelsToRunningTimer()
	{
		if (runningTimeLabels == null)
		{
			return;
		}
		
		for(int i=0; i<runningTimeLabels.Count; i++)
		{
			if (runningTimeLabels[i] == null)
			{
				continue;
			}

			runningTimer.registerLabel(runningTimeLabels[i].label, runningTimeLabels[i].format);
		}
	}

	private void registerCallbacksToReminderTimer()
	{
		if (reminderCallbacks == null)
		{
			return;
		}
		
		for (int i = 0; i < reminderCallbacks.Count; i++)
		{
			if (reminderCallbacks[i] != null)
			{
				reminderTimer.registerFunction(reminderCallbacks[i]);
			}
		}
		
	}
	
	private void onPostPurchaseLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		icon = obj as Texture2D;
	}

	private void onPostPurchaseLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load icon at " + assetPath);
	}
	
	private void onCampaignUnlocked(JSON data)
	{
		if (data == null)
		{
			return;
		}
		isLocked = false;
		timeRemaining = data.getInt("duration", 0); //when unlocked timeRemaining is the total duration
		if (runningTimer != null && runningTimer.timeRemaining > 0)
		{
			StatsPostPurchaseChallenge.logMilestone("post_purchase_reset", timeRemaining - runningTimer.timeRemaining);
		}
		clearTimers();
		runningTimer = GameTimerRange.createWithTimeRemaining(timeRemaining);
		reminderTimer = GameTimerRange.createWithTimeRemaining(timeRemaining-REMINDER_DURATION);
		populateMissions(data.getJSON("campaign"));
		string experiment = data.getString("experiment", "");
		if (postPurchaseChallengeUnlocked != null)
		{
			postPurchaseChallengeUnlocked(experiment);
		}
		runningTimer.registerFunction(onChallengeLost);
		PostPurchaseChallengeDialog.showDialog();
	}

	public void onChallengeLost(Dict args = null, GameTimerRange originalTimer = null)
	{
		PostPurchaseChallengeDialog curDialog = 
			(PostPurchaseChallengeDialog)Dialog.instance.findOpenDialogOfType(campaignID + "_dialog");
		if (curDialog != null && curDialog.canBeForcedToClose())
		{
			Dialog.close(curDialog);
		}
		Server.registerEventDelegate("challenge_campaign_lost", handleChallengeLost, false);
		RobustChallengesAction.getRobustChallengesProgressUpdateInfo(campaignID);
	}

	private static void handleChallengeLost(JSON data)
	{
		bool lost = data.getBool("has_lost", false);
		string eventId = data.getString("event", "");
		string campaignId = data.getString("campaign_experiment", "");
		int progress = data.getInt("event_progress", 0);
		JSON[] consolations = data.getJsonArray("consolations");
		int amount = 0;
		if (consolations != null)
		{
			foreach (JSON reward in consolations)
			{
				amount += reward.getInt("credits", 0);
			}
		}
		StatsPostPurchaseChallenge.logMilestone("post_purchase_incomplete", progress);
		Dict dialogArgs = Dict.create(D.EVENT_ID, eventId, D.KEY, campaignId,  D.DATA, lost, D.AMOUNT, amount, D.SCORE, progress);
		PostPurchaseChallengeDialog.showDialog(SchedulerPriority.PriorityType.HIGH, dialogArgs);
		PostPurchaseChallengeCampaign currentCampaign = getActivePostPurchaseChallengeCampaign();
		if (currentCampaign != null)
		{
			currentCampaign.clearTimers();
		}
	}

	public override void onProgressUpdate(JSON response)
	{
		base.onProgressUpdate(response);
		if (postPurchaseChallengeProgressUpdated != null)
		{
			postPurchaseChallengeProgressUpdated();
		}

		if (isComplete)
		{
			StatsPostPurchaseChallenge.logMilestone("post_purchase_complete", timeRemaining - runningTimer.timeRemaining);
		}
	}
	
	public override bool isCampaignValid()
	{
		return campaignValidState == ChallengeEvalState.VALID;
	}

	protected override void showCampaignComplete(List<JSON> completionJSON)
	{
		base.showCampaignComplete(completionJSON);
		
		PostPurchaseChallengeDialog.showDialog();
	}

	protected override void showMissionComplete(List<JSON> completionJSON)
	{
		base.showMissionComplete(completionJSON);
		PostPurchaseChallengeDialog.showDialog();
	}

	private void hideEventDisplay(Dict args = null, GameTimerRange originalTimer = null)
	{
		if (postPurchaseChallengeEnded != null)
		{
			postPurchaseChallengeEnded();
		}
	}
	
	public int getPostPurchaseChallengeBonus(int index)
	{
		if (index >= 0 && index < ExperimentWrapper.PostPurchaseChallenge.purchaseIndexBonusAmounts.Length)
		{
			return ExperimentWrapper.PostPurchaseChallenge.purchaseIndexBonusAmounts[index];
		}

		return 0;
	}

	public int getPostPurchaseChallengeMaxBonus()
	{
		int maxBonus = 0;
		int startingIndex = 0;
		if (WatchToEarn.isEnabled)
		{
			startingIndex++; //The lowest package isn't shown when W2E is enabled to skip checking its purchase bonus
		}

		for (int i = startingIndex; i < ExperimentWrapper.PostPurchaseChallenge.purchaseIndexBonusAmounts.Length; i++)
		{
			if (ExperimentWrapper.PostPurchaseChallenge.purchaseIndexBonusAmounts[i] >= maxBonus)
			{
				maxBonus = ExperimentWrapper.PostPurchaseChallenge.purchaseIndexBonusAmounts[i];
			}
		}

		return maxBonus;
	}

	public void updatePostPurchaseChallengeMaxBonus()
	{
		if (postPurchaseChallengeMaxBonusUpdated != null)
		{
			postPurchaseChallengeMaxBonusUpdated();
		}
	}
	
	public void winAndReset(JSON clientData)
	{
		restart(); //Need to reset the client UI first before refreshing data in case the new data says we're on the final event that can't replay anymore
		init(clientData);
		if (postPurchaseChallengeCompleted != null)
		{
			postPurchaseChallengeCompleted();
		}
	}

	public long getCurrentAmount()
	{
		if (currentMission != null && currentMission.currentObjective != null)
		{
			return (long)Mathf.Max(0,currentMission.currentObjective.currentAmount);
		}

		return 0;
	}
	
	public long getTargetAmount()
	{
		if (currentMission != null && currentMission.currentObjective != null)
		{
			return currentMission.currentObjective.amountNeeded;
		}

		return 0;
	}

	public int getProgressPercent()
	{
		return (int)((float)getCurrentAmount() / getTargetAmount() * 100.0f);
	}

	public string getBannerPath()
	{
		if (isCampaignValid() && !isLocked && !isComplete)
		{
			return ExperimentWrapper.PostPurchaseChallenge.bannerActivePath;
		}
		
		return ExperimentWrapper.PostPurchaseChallenge.bannerInactivePath;
	}

	private void clearTimers()
	{
		if (runningTimer != null)
		{
			runningTimer.clearLabels();
			runningTimer.clearEvent();
			runningTimer = null;

			if (runningTimeLabels != null)
			{
				runningTimeLabels.Clear();
			}
		}

		if (reminderTimer != null)
		{
			reminderTimer.clearLabels();
			reminderTimer.clearEvent();
			reminderTimer = null;
			if (reminderCallbacks != null)
			{
				reminderCallbacks.Clear();
			}
		}
	}
	
	private void setupCampaingValidity()
	{
		if (missions == null)
		{
			campaignValidState = ChallengeEvalState.INVALID;
			return;
		}
		
		foreach (Mission mission in missions)
		{
			if (mission.objectives == null || mission.objectives.Count == 0)
			{
				campaignValidState = ChallengeEvalState.INVALID;
				return;
			}
		}

		campaignValidState = ChallengeEvalState.VALID;
	}
	
	public override void drawInDevGUI()
	{
		base.drawInDevGUI();
		
		if (GUILayout.Button("Show Dialog"))
		{
			DevGUI.isActive = false;
			PostPurchaseChallengeCampaign campaign = getActivePostPurchaseChallengeCampaign();
			if (campaign != null)
			{
				Dict args = Dict.create(D.OPTION, campaign);
				Scheduler.addDialog(campaign.campaignID + "_dialog", args, SchedulerPriority.PriorityType.IMMEDIATE);
			}
		}

		if (GUILayout.Button("Start Challenge"))
		{
			ServerAction action = new ServerAction(ActionPriority.HIGH, "purchase_challenge");
			DevGUI.isActive = false;
		}
	}
}
