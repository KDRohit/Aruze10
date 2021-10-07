using UnityEngine;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;

public class CampaignDirector : IResetGame
{
	public class FeatureTask
	{
		public string id { get; private set; }
		public string type { get; private set; }
		public bool isComplete { get; private set; }
		public int progress { get; private set; }
		public int target { get; private set; }
		public int expirationTime { get; private set; }
		private System.EventHandler<System.EventArgs> onComplete;

		public FeatureTask(string key, JSON data, System.EventHandler<System.EventArgs> completeFunc = null)
		{
			//if we have invalid data don't continue
			if (data == null)
			{
				return;
			}
			
			id = key;
			type = data.getString("type", "");
			isComplete = data.getBool("complete", false);
			progress = data.getInt("progress", -1);
			target = data.getInt("target", -1);
			expirationTime = data.getInt("expiry", 0);

			if (completeFunc != null)
			{
				onComplete += completeFunc;	
			}
			
		}

		public void sendCompleteEvent()
		{
			if (onComplete != null)
			{
				onComplete.Invoke(this, System.EventArgs.Empty);
				onComplete = null; //clear any events (can't complete more than once)	
			}
			
		}

		public void setComplete()
		{
			if (isComplete)
			{
				return;
			}
			isComplete = true;
			sendCompleteEvent();
		}
	}
	
	private static readonly Dictionary<string, FeatureTask> featureTasks = new Dictionary<string, FeatureTask>();  //these are not part of any campaign (can be completed in any order) and are tied to specific features
	
	// =============================
	// PUBLIC
	// =============================
	
	public delegate void OnProgressUpdate(JSON data = null); // optional callback after progress has been updated
	public static Dictionary<string, ChallengeCampaign> campaigns
	{
		get; private set;
	}
	
	// global references to the last instance created for either campaign.
	// please remove these if multiple campaign management becomes necessary, and use find()
	public static RobustCampaign robust
	{
		get; private set;
	}

	public static PartnerPowerupCampaign partner
	{
		get; private set;
	}

	public static ChallengeCampaign eue
	{
		get; private set;
	}

	public static RichPassCampaign richPass
	{
		get; private set;
	}
	
	private static readonly List<ChallengeCampaign> challengeLobbyCampaigns = new List<ChallengeCampaign>();
	
	private static readonly Dictionary<string, OnProgressUpdate> progressCallbacks = new Dictionary<string, OnProgressUpdate>();
	private static readonly Dictionary<string, OnProgressUpdate> resetCallbacks = new Dictionary<string, OnProgressUpdate>();

	// =============================
	// CONST
	// =============================
	public const string ROBUST_CHALLENGES = "challenge_campaigns";
	public const string PARTNER_POWERUP = "co_op_challenge";
	public const string LOZ_CHALLENGES = "challenge_loz";
	public const string SIN_CITY = "challenge_sin_city_strip";
	public const string SLOTVENTURES = "challenge_slotventures";
	public const string POST_PURCHASE_CHALLENGE = "challenge_post_purchase";
	public const string RICH_PASS = "rich_pass";
	public const string EUE_FTUE = "eue_challenge_slotventures";

	// server events
	private const string GET_PROGRESS = "challenge_campaign_progress";
	private const string OBJECTIVE_COMPLETE = "challenge_type_complete";
	private const string CAMPAIGN_LOST  = "challenge_campaign_lost";
	private const string RESET_PROGRESS = "reset_constrained_challenge";

	
	private static readonly Dictionary<string, System.Type> CAMPAIGN_CLASSES = new Dictionary<string, System.Type>()
	{
		{ ROBUST_CHALLENGES, typeof(RobustCampaign) },
		{ LOZ_CHALLENGES, typeof(LOZCampaign) },
		{ PARTNER_POWERUP, typeof(PartnerPowerupCampaign) },
		{ SLOTVENTURES, typeof(SlotventuresChallengeCampaign) },
		{ SIN_CITY, typeof(SinCityCampaign) },
		{ RICH_PASS, typeof(RichPassCampaign) },
		{ POST_PURCHASE_CHALLENGE, typeof(PostPurchaseChallengeCampaign) },
		{ EUE_FTUE, typeof(EueCampaign) }
	};

	public static FeatureTask getTask(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		
		FeatureTask task = null;
		if (featureTasks.TryGetValue(id, out task))
		{
			//see if progress has changed
			return task;
		}

		return null;
	}

	public static FeatureTask setTaskData(string key, JSON data, System.EventHandler<System.EventArgs> completeFunc)
	{;
		bool didComplete = true;
		if (featureTasks.TryGetValue(key, out FeatureTask task))
		{
			//see if progress has changed
			didComplete = task.isComplete;
		}
		
		//replace task
		featureTasks[key] = new FeatureTask(key, data, completeFunc);
		
		if (!didComplete && featureTasks[key].isComplete)
		{
			featureTasks[key].sendCompleteEvent();
		}

		//return the current task
		return featureTasks[key];
	}

	/// <summary>
	/// Creates a new campaign instance, based on CAMPAIGNS hash
	/// </summary>
	public static void populateAll(JSON[] dataArray)
	{
		int length = 0;
		JSON[] allCampaignsArray = null;

		if (dataArray == null)
		{
			// PPU gets setup in a weird way, always check if the co_op_challenge is there along with the data array
			if (ExperimentWrapper.PartnerPowerup.isInExperiment && Data.login != null)
			{
				// If it is, then just turn the data array into the ppu data basically
				JSON ppu = Data.login.getJSON(PARTNER_POWERUP);
				dataArray = new JSON[] { ppu };
				length = 1;
				allCampaignsArray = new JSON[length];
				dataArray.CopyTo(allCampaignsArray, 0);
			}
			else if (richPass == null)
			{
				registerForCampaignUnlocks();
				return;
			}
		}
		else
		{
			// "normal" setup...
			length = ExperimentWrapper.PartnerPowerup.isInExperiment ? dataArray.Length + 1 : dataArray.Length;
			allCampaignsArray = new JSON[length];
			dataArray.CopyTo(allCampaignsArray, 0);

			if (allCampaignsArray.Length == dataArray.Length + 1)
			{
				JSON[] ppu = new JSON[1] { Data.login.getJSON(PARTNER_POWERUP) };
				ppu.CopyTo(allCampaignsArray, dataArray.Length);
			}
		}

		if (allCampaignsArray != null && allCampaignsArray.Length > 0)
		{			
			campaigns = new Dictionary<string, ChallengeCampaign>();

			for (int i = 0; i < allCampaignsArray.Length; i++)
			{
				JSON data = allCampaignsArray[i];	
				string campaignName = "";
				if (data == null && i == dataArray.Length - 1)
				{
					// MCC -- Partner Powerup was built assuming that it would always get initialized even if
					// the data was null. Since we are going to add some time to refactor this properly, I am
					// just putting this in here to make sure it functions correctly for users as a temporary solution.
					// *** The purpose of this is that if the data is null, 
					campaignName = PARTNER_POWERUP;
				}
				else if (data != null)
				{
					// If the experiment is blank, it's PPU because server built the tech asynchronously to robust challenges.
					campaignName = data.getString("experiment", PARTNER_POWERUP);
				}
				else
				{
					if (Data.debugMode)
					{
						Debug.LogErrorFormat("CampaignDirector.cs -- populateAll -- Jinkies! We got null data and we aren't instantiating Parter Power Up! Something mysterious is afoot.");
					}
					continue;
				}
				
				initCampaign(data, campaignName);
			}	
		}

		if (campaigns != null && campaigns.Count <= 0)
		{
			removeServerHandlers();
		}

		registerForCampaignUnlocks();
	}

	private static void registerForCampaignUnlocks()
	{
		if (!EueFeatureUnlocks.isFeatureUnlocked("rich_pass") && richPass == null)
		{
			EueFeatureUnlocks.instance.registerForFeatureLoadEvent("rich_pass", RichPassCampaign.onFeatureLoad);
			EueFeatureUnlocks.instance.registerForGetInfoEvent("rich_pass", onCampaignUnlocked);
		}

		if (EueFeatureUnlocks.hasFeatureUnlockData("sv_challenges") && (campaigns == null || !campaigns.ContainsKey(SlotventuresChallengeCampaign.CAMPAIGN_ID)))
		{
			//Need to setup the theme data regardless if the feature is locked or the campaign is inactive
			//Need to still show the icon with "coming soon" text
			
			SlotventuresChallengeCampaign.setupThemeData();
			if (!EueFeatureUnlocks.isFeatureUnlocked("sv_challenges"))
			{
				EueFeatureUnlocks.instance.registerForFeatureLoadEvent("sv_challenges", SlotventuresChallengeCampaign.loadBundles);
				EueFeatureUnlocks.instance.registerForGetInfoEvent("sv_challenges", onCampaignUnlocked);
			}
		}
	}

	private static void onCampaignUnlocked(JSON data)
	{
		string campaignName = data.getString("experiment", "");
		initCampaign(data, campaignName);

		ChallengeCampaign newCampaign = find(campaignName);
		if (newCampaign != null)
		{
			newCampaign.currentEventIndex = 0;
			newCampaign.startingEventIndex = 0;
		}
	}

	public static void initCampaign(JSON data, string campaignName)
	{
		if (campaigns == null)
		{
			campaigns = new Dictionary<string, ChallengeCampaign>();
		}
		
		if (campaigns.ContainsKey(campaignName))
		{
			Debug.LogWarning("Duplicate challenge data found for " + campaignName);
		}
		else
		{
			ChallengeCampaign campaign = null;
			if (CAMPAIGN_CLASSES.ContainsKey(campaignName))
			{
				campaign = (ChallengeCampaign)System.Activator.CreateInstance(CAMPAIGN_CLASSES[campaignName]);
			}
			else
			{
				campaign = new ChallengeLobbyCampaign();
			}

			if (campaign.isForceDisabled)
			{
				campaign = null;
				return;
			}

			if (campaign is PartnerPowerupCampaign)
			{
				campaigns.Add("co_op_campaign", campaign);
			}
			else
			{
				campaigns.Add(campaignName, campaign);
			}
			setGlobalRef(campaign, campaignName);
			campaign.init(data);
		}
	}

	private static void setGlobalRef(ChallengeCampaign campaign, string campaignType)
	{
		if (campaign == null) {	return;	}

		switch (campaignType)
		{
			case ROBUST_CHALLENGES:
				robust = campaign as RobustCampaign;
				break;
			
			case PARTNER_POWERUP:
				partner = campaign as PartnerPowerupCampaign;
				break;
			
			case RICH_PASS:
				richPass = campaign as RichPassCampaign;
				break;
			
			case EUE_FTUE:
				eue = campaign as EueCampaign;
				break;
			
			default: 
				challengeLobbyCampaigns.Add(campaign); 
				break;
		}
	}
	
	
	/// <summary>
	/// Returns a campaign from the list, based on the campaign name
	/// </summary>
	public static ChallengeCampaign find(string campaignName)
	{
		ChallengeCampaign campaign = null;
		if (campaigns != null && !campaigns.TryGetValue(campaignName, out campaign))
		{
			Debug.LogWarning("Couldn't find campaign named " + campaignName);
		}
		return campaign;
	}

	/// <summary>
	/// Returns a campaign that contains the specified game
	/// </summary>
	public static ChallengeCampaign findWithGame(string gameKey)
	{
		if (campaigns != null)
		{
			foreach (ChallengeCampaign campaign in campaigns.Values)
			{
				if (campaign.findWithGame(gameKey) != null)
				{
					return campaign;
				}
			}
		}

		return null;
	}

	public static ChallengeCampaign findWithGame(string keyPattern, string gameKey)
	{
		if (campaigns != null)
		{
			foreach (string type in campaigns.Keys)
			{
				if (type.Contains(keyPattern))
				{
					ChallengeCampaign challengeCampaign = campaigns[type];
					if (challengeCampaign.findWithGame(gameKey) != null)
					{
						return challengeCampaign;
					}
				}
			}
		}
		return null;
	}

	public static List<ChallengeCampaign> findAllCampaignsWithGame(string gameKey)
	{

		List<ChallengeCampaign> foundCampaigns = new List<ChallengeCampaign>();
		if (campaigns != null)
		{
			foreach (ChallengeCampaign campaign in campaigns.Values)
			{
				if (campaign.findWithGame(gameKey) != null)
				{
					foundCampaigns.Add(campaign);
				}
			}
		}

		return foundCampaigns;
	}

	public static bool isChallengeLobbyGameUnlocked(string gameKey)
	{
		ChallengeCampaign campaign = findWithGame(gameKey);
		return campaign != null && campaign is ChallengeLobbyCampaign && campaign.isGameUnlocked(gameKey);
	}

	public static bool isCampaignEnabled(string campaignName)
	{
		if (campaigns != null && campaigns.ContainsKey(campaignName))
		{
			return campaigns[campaignName].isEnabled;
		}

		return false;
	}

	/*=========================================================================================
	SERVER HANDLING
	=========================================================================================*/
	public static void addServerHandlers()
	{
		Server.registerEventDelegate(GET_PROGRESS, onProgressUpdate, true);
		Server.registerEventDelegate(OBJECTIVE_COMPLETE, onObjectiveComplete, true);
		Server.registerEventDelegate(CAMPAIGN_LOST, onCampaignLost, true);
		Server.registerEventDelegate(RESET_PROGRESS, onProgressReset, true);
		Server.registerEventDelegate(VirtualPetsFeature.PET_TASK_COMPLETE, onTaskComplete, true);
		Server.registerEventDelegate(RichPassCampaign.BANK_REWARD_EVENT, RichPassCampaign.onBankReward, true); //Registering here since it comes in when the campaign is already ended
	}
		
	protected static void removeServerHandlers()
	{
		Server.unregisterEventDelegate(GET_PROGRESS, onProgressUpdate, true);
		Server.unregisterEventDelegate(OBJECTIVE_COMPLETE, onObjectiveComplete, true);
		Server.unregisterEventDelegate(CAMPAIGN_LOST, onCampaignLost, true);
		Server.unregisterEventDelegate(RESET_PROGRESS, onProgressReset, true);
		Server.unregisterEventDelegate(VirtualPetsFeature.PET_TASK_COMPLETE, onTaskComplete, true);
		Server.unregisterEventDelegate(RichPassCampaign.BANK_REWARD_EVENT, RichPassCampaign.onBankReward, true);

		if (campaigns != null)
		{
			foreach (ChallengeCampaign campaign in campaigns.Values)
			{
				campaign.unregisterEvents();
			}	
		}
	}
	
	/// <summary>
	/// Request the current progress for a specified campaign. If none is specified, progress for
	/// all campaigns is given.
	/// </summary>
	public static void getProgress(string campaignID = null, OnProgressUpdate callback = null)
	{
		if (campaigns == null)
			return; // i.e. SIR has no campaigns now
		
		if (!string.IsNullOrEmpty(campaignID))
		{
			ChallengeCampaign campaign = find(campaignID);

			if (campaign != null)
			{	
				campaign.didUpdateProgress = false;
				// Use the cached progress data instead of requesting it again, then return early.
				// This is equivalent to the logic in onProgressUpdate() being called immediately.
				if (campaign.cachedResponse != null)
				{
					campaign.onProgressUpdate(campaign.cachedResponse);
					if (callback != null)
					{
						callback(campaign.cachedResponse);
					}
					return;
				}

				// Otherwise add a callback to be called later once we get a server response from the call to 
				// getRobustChallengesProgressUpdateInfo() below.
				if (callback != null && !progressCallbacks.ContainsKey(campaign.campaignID))
				{
					progressCallbacks.Add(campaign.campaignID, callback);
				}
			}
		}
		else
		{
			foreach (ChallengeCampaign c in campaigns.Values)
			{
				c.didUpdateProgress = false;
			}
		}
		
		RobustChallengesAction.getRobustChallengesProgressUpdateInfo(campaignID);
	}

	public static void registerResetCallback(string campaignID, OnProgressUpdate callback)
	{
		if (!string.IsNullOrEmpty(campaignID))
		{
			resetCallbacks[campaignID] = callback;
		}
	}

	private static void onTaskComplete(JSON response)
	{
		if (response == null)
		{
			Debug.LogError("Invalid task data");
			return;
		}
		
		string id = response.getString("task_id", "");
		if (string.IsNullOrEmpty(id))
		{
			Debug.LogError("No task id in completed task json");
			return;
		}
		
		FeatureTask task = null;
		if (featureTasks.TryGetValue(id, out task))
		{
			task.setComplete();
		}
		
		InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.OPTION, true));
	}

	/// <summary>
	/// Handle progress updates
	/// </summary>
	protected static void onProgressUpdate(JSON response)
	{
		ChallengeCampaign campaign = find(response.getString("experiment", ""));
		if (campaign != null)
		{
			campaign.onProgressUpdate(response);
			if (progressCallbacks.ContainsKey(campaign.campaignID))
			{
				progressCallbacks[campaign.campaignID](response);
				progressCallbacks.Remove(campaign.campaignID);
			}
		}
	}

	protected static void onProgressReset(JSON response)
	{
		ChallengeCampaign campaign = find(response.getString("experiment", ""));
		if (campaign != null)
		{
			campaign.onProgressReset(response);
			if (resetCallbacks.ContainsKey(campaign.campaignID))
			{
				resetCallbacks[campaign.campaignID](response);
				resetCallbacks.Remove(campaign.campaignID);
			}

		}
	}

	// Invalidates all the cached progress, to force getting the progress again.
	public static void invalidateCachedProgress()
	{
		if (campaigns == null)
		{
			// Not initialized yet. Nothing to invalidate.
			return;
		}
		
		foreach (ChallengeCampaign campaign in campaigns.Values)
		{
			campaign.invalidateCachedResponse();
		}
	}

	/// <summary>
	/// Handle challenge type completion, this event is sent from the server when an objective is finished
	/// </summary>
	protected static void onObjectiveComplete(JSON response)
	{
		ChallengeCampaign campaign = find(response.getString("experiment", ""));
		if (campaign != null)
		{
			campaign.addTypeCompleteDataToQueue(response);
		}
	}

	protected static void onCampaignLost(JSON response)
	{
		string campaign = response.getString("experiment", "");
		switch (campaign)
		{
			case ROBUST_CHALLENGES:
				RobustChallengesEnded.processEndedData(response);
				break;
			case POST_PURCHASE_CHALLENGE:
				PostPurchaseChallengeCampaign.handleLost(response);
				break;
			
		}
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	/// <summary>
	/// Returns true if any campaign is active
	/// </summary>
	public static bool isActive
	{
		get
		{
			if (campaigns != null)
			{
				foreach(ChallengeCampaign campaign in campaigns.Values)
				{
					if (campaign.isActive)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public static bool isCampaignActive(ChallengeCampaign campaign)
	{
		return campaign != null && campaign.isActive;
	}



#region ZAP_CODE
	public static void resetAllChallenges()
	{
		Server.registerEventDelegate("", resetChallengesCallback);
		RobustChallengesAction.resetChallenges();
	}

	private static void resetChallengesCallback(JSON data)
	{
		if (data == null || !data.getBool("success", false))
		{
			// If we failed, then show a dialog saying we failed.
			Debug.LogErrorFormat("CampaignDirector.cs -- resetChallengesCallback() -- failed to reset challenges.");
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "Error",
					D.MESSAGE, "Failed to reset challenges.",
					D.OPTION1, "Okay"
				),
				SchedulerPriority.PriorityType.IMMEDIATE
				);
		}
		else
		{
			// Otherwise lets reset the game to make sure everything is synced with the server.
			Glb.resetGame("Resetting after a challenge reset");
		}
	}

#endregion
	// IResetGame contract
	public static void resetStaticClassData()
	{
		// TODO: check for quick clears, leveraging GC e.g. length set 0.
		// TODO: verify no cleanup from campaigns (i.e. campaign.destroy())
		robust = null;
		partner = null;
		richPass = null;
		campaigns = null;
		eue = null;
		
		challengeLobbyCampaigns.Clear();
		resetCallbacks.Clear();
		progressCallbacks.Clear();
	}
}
