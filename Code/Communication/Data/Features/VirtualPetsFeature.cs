using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Com.Rewardables;
using Com.Scheduler;
using Object = UnityEngine.Object;

namespace Com.HitItRich.Feature.VirtualPets
{
	public class VirtualPetsFeature : FeatureBase, IResetGame
	{
		/* Assets */
		public const string PET_COLLECT_PREFAB_PATH = "Features/Virtual Pets/Prefabs/Instanced Prefabs/Pets Daily Bonus Button Content Item";
		private const string VIRTUAL_PETS_TOASTER_MESSAGE_PATH = "Features/Virtual Pets/Text/virtual_pets_toster_messages";

		/* login data key */
		public const string LOGIN_DATA_KEY = "virtual_pet";

		/* dialog key */
		public const string DIALOG_KEY = "virtual_pets_dialog";
		
		/* default name */
		public const string PET_DEFAULT_NAME_LOC = "virtual_pet_default_name";
		
		/* daily bonus item */
		public const string PET_DAILY_BONUS_BUNDLE = "virtual_pet_daily_bonus";

		
		/* Public Events */
		public const string PET_TASK_COMPLETE = "virtual_pet_task_complete";
		public const string PET_STATUS_UPDATE = "virtual_pet_status";
		
		/* pending credit source */
		public const string PET_FEED_PENDING_CREDIT_SOURCE = "pet_treat";
		
		/* Internal Events */
		private const string UNLOCKED = "virtual_pet_unlocked";
		private const string TREAT_USED = "virtual_pet_basic_energy_gained";
		private const string PET_FTUE_EVENT = "virtual_pet_ftue";

		/* Instance Variables*/
		private GameTimerRange toaserTimer = null;
		private string name;
		private string lastTreatTypeUsed = "";
		private bool silentFeedLoginTreat = true;
		public int hyperStreakStartTime { get; private set; }

		public List<string> treatTasks { get; private set; }
		private List<string> toasterMessages = new List<string>();
		private List<float> testToasterWeights = new List<float>();
		
		private ToasterMessageTable messageTable = new ToasterMessageTable();

		
		


		private static Dictionary<int, System.Action> ftueSeenEventsDict = new Dictionary<int,System.Action>();
		
		//static instance
		public static VirtualPetsFeature instance { get; private set; }

		public string petName
		{
			get
			{
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}

				return Localize.text(PET_DEFAULT_NAME_LOC);
			}
		}

		public bool ftueSeen { get; private set; }
		public int fullyFedStreak { get; private set; }
		public int currentEnergy { get; private set; }
		public int maxEnergy { get; private set; }
		public bool hyperReached { get; private set; }
		public int hyperEndTime { get; private set; }
		public int timerCollectsUsed { get; private set; }
		public int nextPettingRewardTime { get; private set; }
		public int normalMaxTimerCollects { get; private set; }
		public int hyperMaxTimerCollects { get; private set; }
		public GameTimer lowEnergyTimer { get; private set; }
		public GameTimer midEnergyTimer { get; private set; }
		public GameTimerRange hyperTimer { get; private set; }
		public int nextStreakRewardDay { get; private set; }

		private static Dictionary<string, VirtualPetTreat> petTreats = new Dictionary<string, VirtualPetTreat>();
		
		public const string LOW_ENERGY_NOTIF_KEY = "low_energy_notif_pet";
		public const string MID_ENERGY_NOTIF_KEY = "mid_energy_notif_pet";
		public const string DB_FETCH_NOTIF_KEY = "db_fetch_notif_pet";

		//When true only a toaster is displayed when the pet is feed
		public bool silentFeedPet
		{
			get
			{
				return CustomPlayerData.getBool(CustomPlayerData.SILENT_FEED_PET,false);
			}
			set
			{
				CustomPlayerData.setValue(CustomPlayerData.SILENT_FEED_PET,value);
			}
		}

		public delegate void OnFtueSeenDelegate();

		private event OnFtueSeenDelegate onFtueSeen;

		public delegate void OnPetRewardDelegate(Rewardable reward);
		private event OnPetRewardDelegate onRewardEvents;

		public delegate void OnHyperStatusChangeDelegate(bool isHyper);

		private event OnHyperStatusChangeDelegate onHyperStatusEvent;
		
		public delegate void OnPetStatusDelegate();
		private event OnPetStatusDelegate onPetStatusEvent;

		private GameTimerRange taskExpirationTimer;
		
		public int timerCollectsMax
		{
			get { return isHyper ? hyperMaxTimerCollects : normalMaxTimerCollects; }
		}


		public bool isHyper
		{
			get
			{
				return GameTimer.currentTime < hyperEndTime;
			}
		}

		public static bool canPetCollectBonus
		{
			get { return canPetCollectBonusForCurrentDay || didPetCollectBonusLastSession; }
		}

		public static bool canPetCollectBonusForCurrentDay
		{
			get
			{
				if (instance == null)
				{
					return false;
				}

				return instance.timerCollectsUsed < instance.hyperMaxTimerCollects &&
				       //user has reached hyper today
				       instance.hyperReached &&
				       //timer must be expired
				       SlotsPlayer.instance.dailyBonusTimer.isExpired &&
				       //user is currently idle
				       UserActivityManager.instance.isIdle;
			}
		}

		public static bool didPetCollectBonusLastSession
		{
			get
			{
				if (instance == null)
				{
					return false;
				}
				
				//the pet can collect the bonus up until the start of the next day, so within 24 hours of the streak start
				//streak start is 10am (rollover time) of the day the streak started according to qd
				int totalTimeOfStreak = Common.SECONDS_PER_DAY * instance.fullyFedStreak;
				int maxTimeForPetCollect = instance.hyperStreakStartTime + totalTimeOfStreak;
				
				//determine if the daily bonus timer expired in this time range
				int nextCollectTime = UserActivityManager.instance.lastTimerCollectTime + SlotsPlayer.instance.dailyBonusDuration;
				bool timerExpiredInRange =  nextCollectTime < maxTimeForPetCollect;

				//find time of last rollover in this streak
				int rollOverTime = maxTimeForPetCollect - Common.SECONDS_PER_DAY;
				
				//check that they haven't used all 3 pet collects in the past day
				bool didNotUseAllPetCollects = instance.timerCollectsUsed < instance.hyperMaxTimerCollects;

				return
					//timer is currently expired
					SlotsPlayer.instance.dailyBonusTimer.isExpired && 
					//timer expired on a day you achieved hyper status, or before you achieved hyper status, ie not after you lost hyper
					timerExpiredInRange &&
					//you didn't use 3 pet collects that day
					didNotUseAllPetCollects &&
					//this event occured in the past
					instance.hyperStreakStartTime < UserActivityManager.instance.loginTime &&
					//you've waited log enough between logins
					UserActivityManager.instance.wasIdleBeforeLogin &&
					//you haven't collected this session
					UserActivityManager.instance.lastTimerCollectTime < UserActivityManager.instance.loginTime;
			}
		}

		public static string collectNotValidReason
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (instance == null)
				{
					sb.AppendLine("Instance is not active");
				}
				else
				{
					if (instance.timerCollectsUsed >= instance.hyperMaxTimerCollects)
					{
						sb.AppendLine("Used all timer collects for the day");
					}

					if (!instance.hyperReached)
					{
						sb.AppendLine("hyper not reached today");
					}

					if (!SlotsPlayer.instance.dailyBonusTimer.isExpired)
					{
						sb.AppendLine("Daily bonus timer not expired");
					}

					if (!UserActivityManager.instance.isIdle)
					{
						sb.AppendLine("Not idle");
					}
				}

				return sb.ToString();
			}
			
		}
		
		public static string collectLastSessionNotValidReason
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (instance == null)
				{
					sb.AppendLine("Instance is not active");
				}
				else
				{
					
					//the pet can collect the bonus up until the start of the next day, so within 24 hours of the streak start
					//streak start is 10am (rollover time) of the day the streak started according to qd
					int totalTimeOfStreak = Common.SECONDS_PER_DAY * instance.fullyFedStreak;
					int maxTimeForPetCollect = instance.hyperStreakStartTime + totalTimeOfStreak;
				
					//determine if the daily bonus timer expired in this time range
					int nextCollectTime = UserActivityManager.instance.lastTimerCollectTime + SlotsPlayer.instance.dailyBonusDuration;
					bool timerExpiredInRange = nextCollectTime < maxTimeForPetCollect;

					//find time of last rollover in this streak
					int rollOverTime = maxTimeForPetCollect - Common.SECONDS_PER_DAY;
				
					//check that they haven't used all 3 pet collects in the past day
					bool usedAllPetCollects = instance.timerCollectsUsed >= instance.hyperMaxTimerCollects;

					
					//timer expired on a day you achieved hyper status
					if (!timerExpiredInRange)
					{
						sb.AppendLine("Bonus Timer expired the day after your hyper ended");
					}
					
					//you didn't use 3 pet collects that day
					if (usedAllPetCollects)
					{
						sb.AppendLine("Used all pet collects up last time you were hyper");
					}
					//this event occured in the past
					if (instance.hyperStreakStartTime >= UserActivityManager.instance.loginTime)
					{
						sb.AppendLine("Hyper streak start time is after login, start:  " + instance.hyperStreakStartTime + "login: " + UserActivityManager.instance.loginTime);
					}
					
					//you've waited log enough between logins
					if (!UserActivityManager.instance.wasIdleBeforeLogin)
					{
						sb.AppendLine("you were not idle before login");
					}
					
					//you haven't collected this session
					if(UserActivityManager.instance.lastTimerCollectTime >= UserActivityManager.instance.loginTime)
					{
						sb.AppendLine("You already collected this session");
					}
				}

				return sb.ToString();
			}
			
		}
		
		

		private VirtualPetsFeature()
		{
		}

		public override bool isEnabled
		{
			get
			{ return base.isEnabled && ExperimentWrapper.VirtualPets.isInExperiment; }
		}

		public static void instantiateFeature(JSON data)
		{
			if (instance != null)
			{
				instance.clearEventDelegates();
			}

			instance = new VirtualPetsFeature();
			instance.initFeature(data);
			instance.silentFeedLoginTreat = !instance.ftueSeen;
			instance.loadMessageFile();
		}

		protected override void initializeWithData(JSON data)
		{
			if (data == null)
			{
				_isEnabled = false;
				return;
			}

			_isEnabled = true;
			treatTasks = new List<string>();
			onPetStatusUpdate(data, true);
		}

#if !ZYNGA_PRODUCTION
		//Test Functions
		public bool testEvent(JSON data, string eventKey)
		{
			//Used instead of methods so the callbacks can remain private
			bool evenHandled = false;
			switch (eventKey)
			{
				case UNLOCKED:
					evenHandled = true;
					onUnlockedEvent(data);
					break;
				default:
					break;
			}

			return evenHandled;
		}

		public void testSetHyperMaxTimerCollects(int testHyperMaxTimerCollects)
		{
			hyperMaxTimerCollects = testHyperMaxTimerCollects;
		}
		
		public void testSetNormalMaxTimerCollects(int testNormalMaxTimerCollects)
		{
			normalMaxTimerCollects = testNormalMaxTimerCollects;
		}

		public int testToasterEventCount()
		{
			return messageTable.eventCount();
		}

		public int testToasterEventMessageCount(string eventName)
		{
			return messageTable.eventMessageCount(eventName);
		}

		public string[] testEvents()
		{
			return messageTable.testEvents();
		}


		public void testSetPetName(string newName)
		{
			name = newName;
		}

		public void testShowToasterForEventWithMessageIndex(string eventName, int index)
		{
			scheduleToasterWithMessage( messageTable.testForceEvent(eventName, index), null);
		}

		public void testSetHyperEndTime(int testHyperEndTime)
		{
			hyperEndTime = testHyperEndTime;
		}

#endif
		private void loadMessageSuccess(string assetPath, Object obj, Dict data = null)
		{
			TextAsset textFile = obj as TextAsset;
			JSON messages = new JSON(textFile.text);
			foreach (string key in messages.getKeyList())
			{
				JSON [] eventMessages = messages.getJsonArray(key);
				foreach (JSON message in eventMessages)
				{
				
					string [] stringSubs = message.getStringArray("params");
					List<string> subs =new List<string>(stringSubs);

					string debugMessage = message.getString("message", "");
					messageTable.addMessage(key,message.getString("message",""),message.getFloat("weight",0),subs.Count != 0 ? subs : null);
				}

			}   
		}
		private void loadMessageFail(string assetPath, Dict data = null)
		{
		}
		
		private void loadMessageFile()
		{
			AssetBundleManager.load(this, VIRTUAL_PETS_TOASTER_MESSAGE_PATH, loadMessageSuccess, loadMessageFail, null,isLazy:false,isSkippingMapping:true, fileExtension:".json");

		}
		private bool canShowToaster(string eventName) 
		{

			  return messageTable.hasEvent(eventName)
#if !ZYNGA_PRODUCTION
					 || (eventName == "test" && toasterMessages.Count > 0 && testToasterWeights.Count > 0)
#endif
				  ;
			
		}
		
		private void scheduleToaster(string eventName, Dict data = null)
		{
			if (canShowToaster(eventName) )
			{
				int indexChoosen = 0;
				string message = "";
				message = messageTable.getRandomWeightedMessage(eventName);
				scheduleToasterWithMessage(message, data);
			}
		}

		private void scheduleToasterWithMessage(string message, Dict data = null)
		{
			if (data == null)
			{
				data = Dict.create();
			}
			data.Add(D.TITLE,message);
			data.Add(D.CUSTOM_INPUT,petName);
			ToasterManager.addToaster(ToasterType.VIRTUAL_PETS, data);
		}


		protected override void registerEventDelegates()
		{
			if (!ftueSeen)
			{
				Server.registerEventDelegate(UNLOCKED, onUnlockedEvent, false);
			}
			Server.registerEventDelegate(TREAT_USED, onPetStatusUpdate, true);
			Server.registerEventDelegate(PET_STATUS_UPDATE, onPetStatusUpdate, true);
			Server.registerEventDelegate(PET_FTUE_EVENT, onPetFTUE);

			RewardablesManager.addEventHandler(onRewardSuccess);
			RewardablesManager.addBatchEventHandler("pet_treat", "treat_name", onPetTreatRewardSuccess);
		}

		protected override void clearEventDelegates()
		{
			Server.unregisterEventDelegate(UNLOCKED, onUnlockedEvent, true);
			Server.unregisterEventDelegate(TREAT_USED, onPetStatusUpdate, true);
			Server.unregisterEventDelegate(PET_STATUS_UPDATE, onPetStatusUpdate, true);
			Server.unregisterEventDelegate(PET_FTUE_EVENT, onPetFTUE);

			RewardablesManager.removeEventHandler(onRewardSuccess);
			RewardablesManager.removeBatchEventHandler("pet_treat", onPetTreatRewardSuccess);
		}

		private void onUnlockedEvent(JSON data)
		{
		}

		
		private long onBasicTreatRewardable(RewardablePetBasicEnergy reward)
		{
			onPetStatusUpdate(reward.petStatus);
			return reward.amount;
		}
		

		private long onSpecialTreatRewardable(RewardableSpecialPetEnergy reward)
		{
			long preTreatEnergy = currentEnergy;
			onPetStatusUpdate(reward.data);
			return currentEnergy - preTreatEnergy;
		}

		private void onPetStatusUpdate(JSON data)
		{
			onPetStatusUpdate(data, false);
		}
		private void onPetStatusUpdate(JSON data, bool isInitialization)
		{
			if (data == null)
			{
				return;
			}
			
			name = data.getString("name", name);
			ftueSeen = data.getBool("ftue_seen", ftueSeen);
			maxEnergy = data.getInt("max_energy", 0);
			currentEnergy = data.getInt("current_energy", currentEnergy);
			hyperReached = data.getBool("hyper_reached", hyperReached);
			updateHyperEndTime(data.getInt("hyper_end_time", 0));
			
			fullyFedStreak = data.getInt("fully_fed_streak", 0);

			timerCollectsUsed = data.getInt("timer_collects_used", 0);

			hyperStreakStartTime = data.getInt("last_hyper_streak_time", 0);
			hyperMaxTimerCollects = data.getInt("timer_collects_max_hyper", 0);
			normalMaxTimerCollects = data.getInt("timer_collects_max_standard", 0);
			nextPettingRewardTime = data.getInt("next_petting_reward_time", 0);
			
			int secondsToLowEnergy = data.getInt("seconds_to_low_energy", 0);
			lowEnergyTimer = secondsToLowEnergy > 0 ? GameTimer.createWithEndDateTimestamp(GameTimer.currentTime + secondsToLowEnergy) : null;
			
			int secondsToMidEnergy = data.getInt("seconds_to_mid_energy", 0);
			midEnergyTimer = secondsToMidEnergy > 0 ? GameTimer.createWithEndDateTimestamp(GameTimer.currentTime + secondsToMidEnergy) : null;


			if (nextPettingRewardTime <= 0)
			{
				//server sent down null instead of a time stamp.  We can reward now
				nextPettingRewardTime = GameTimer.currentTime;
			}
			

			JSON tasks = data.getJSON("tasks");

			
			if (tasks != null)
			{
				//new tasks, clear the refresh timer and set to next task expiration
				if (taskExpirationTimer != null)
				{
					taskExpirationTimer.removeFunction(onTaskExpire);
					taskExpirationTimer = null;	
				}
				
				treatTasks.Clear();

				int firstExpiration = -1;
				
				List<string> keys = tasks.getKeyList();
				keys = sortTreatTaskKeys(keys);

				for (int i = 0; i < keys.Count; i++)
				{
					if (string.IsNullOrEmpty(keys[i]))
					{
						Debug.LogError("Invalid task json");
						continue;
					}
					treatTasks.Add(keys[i]);
					CampaignDirector.FeatureTask task = CampaignDirector.setTaskData(keys[i], tasks.getJSON(keys[i]), taskCompleteCallback);
					if (firstExpiration < 0 || task.expirationTime < firstExpiration)
					{
						firstExpiration = task.expirationTime;
					}
				}

				if (firstExpiration > GameTimer.currentTime)
				{
					taskExpirationTimer = new GameTimerRange(GameTimer.currentTime, firstExpiration);
					taskExpirationTimer.registerFunction(onTaskExpire);
				}
				
			}

			nextStreakRewardDay = data.getInt("next_streak_reward_day", 0);
			if (onPetStatusEvent != null)
			{
				onPetStatusEvent();
			}
		}

		private void onTaskExpire(Dict args, GameTimerRange timer)
		{
			//set timer to null
			taskExpirationTimer = null;
			
			//do status update -- new tasks will create a new task expiration timer
			VirtualPetsActions.refreshPetStatus();
		}

		private List<string> sortTreatTaskKeys(List<string> keys)
		{
			List<string> sortedKeys = new List<string>();
			
			//Put the keys in the order dictated by EOS
			for (int i = 0; i < ExperimentWrapper.VirtualPets.treatsOrder.Length; i++)
			{
				string taskId = ExperimentWrapper.VirtualPets.treatsOrder[i];
				if (keys.Contains(taskId))
				{
					sortedKeys.Add(taskId);
					keys.Remove(taskId);
				}
			}

			//Add back any remaining tasks that are set but not given an ordered place from EOS
			sortedKeys.AddRange(keys);

			return sortedKeys;
		}

		private void activateHyperModeUI()
		{
			//Only need to handle this if the panel is already active or else this should get setup during Spin Panel startup
			if (SpinPanel.hir != null)
			{
				SpinPanel.hir.turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode.HYPER);
			}
		}

		private void onHyperModeEnd(Dict args, GameTimerRange caller)
		{
			if (onHyperStatusEvent != null)
			{
				onHyperStatusEvent(false);
			}
			
			//Only need to handle this if the panel is already active or else this should get setup during Spin Panel startup
			if (SpinPanel.instance != null)
			{
				//Don't turn off in the middle of a respin
				//The respin has its own spin panel button flow 
				if (VirtualPetRespinOverlayDialog.instance == null)
				{
					SpinPanel.hir.turnOffPetsSpinButton();
				}
			}
			
		}

		private void updateHyperEndTime(int newHyperEndTime)
		{
			if (newHyperEndTime <= 0)
			{
				//server sent down null instead of a timestamp, just set it to expired (current time -1)
				hyperEndTime = GameTimer.currentTime -1;
			}
			else
			{
				hyperEndTime = newHyperEndTime;
				
				if (hyperEndTime > GameTimer.currentTime)
				{
					if (hyperTimer == null || hyperTimer.isExpired)
					{
						hyperTimer = GameTimerRange.createWithTimeRemaining(hyperEndTime - GameTimer.currentTime);
						hyperTimer.registerFunction(onHyperModeEnd);
						StatsManager.Instance.LogCount(
							counterName: "popup",
							kingdom:"pet",
							phylum:"hyper_state_on",
							klass:lastTreatTypeUsed,
							family:CommonText.secondsFormatted(hyperEndTime - GameTimer.currentTime),
							val:currentEnergy
						);
						if (onHyperStatusEvent != null)
						{	
							onHyperStatusEvent(true);
						}
						activateHyperModeUI();
					}
					else
					{
						int tillEndTime = hyperEndTime - GameTimer.currentTime;
						if (tillEndTime >= 0)
						{
							hyperTimer.updateEndTime(tillEndTime);
						}
					}
				}
			}
		}

		public bool allTaskComplete
		{
			get
			{
				if (petTreats == null)
				{
					return false;
				}
				
				for (int i = 0; i < treatTasks.Count; i++)
				{
					CampaignDirector.FeatureTask task = CampaignDirector.getTask(treatTasks[i]);
					if (task == null)
					{
						continue;
					}
					else if (!task.isComplete)
					{
						return false;
					}
				}

				return true;
			}
		}

		public int getNumCompletedTasks()
		{
			int numCompletedTasks = 0;
			if (treatTasks != null)
			{
				for (int i = 0; i < treatTasks.Count; i++)
				{
					CampaignDirector.FeatureTask task = CampaignDirector.getTask(treatTasks[i]);
					if (task == null || task.isComplete)
					{
						numCompletedTasks++;
					}
				}	
			}
			
			return numCompletedTasks;
		}

		private void onPetFTUE(JSON data)
		{
			ftueSeen = true;
			if (onFtueSeen != null)
			{
				onFtueSeen();	
			}
			VideoDialog.showDialog(
				videoPath:ExperimentWrapper.VirtualPets.videoUrl,
				summaryScreenImage:ExperimentWrapper.VirtualPets.videoSummaryPath,
				priority:SchedulerPriority.PriorityType.HIGH
				);
			
			VirtualPetsFeatureDialog.showDialog(VirtualPetsFeatureDialog.TabType.MY_PET);
			VirtualPetsActions.markFtueSeen(data.getString("event", ""));
		}

		private void taskCompleteCallback(object sender, System.EventArgs e)
		{
			CampaignDirector.FeatureTask task = sender as CampaignDirector.FeatureTask;
			
			if (task != null)
			{
				Bugsnag.LeaveBreadcrumb("Virtual Pet:  Finished Pet task - " + task.type);
			}
		}
		private void schedulePetFeedingDialog(VirtalPetsFeedDialog.TreatData task)
		{
			List<VirtalPetsFeedDialog.TreatData> tasks = new List<VirtalPetsFeedDialog.TreatData>();
			tasks.Add(task);
			VirtalPetsFeedDialog.showDialog(Dict.create(
				D.OPTION, tasks,D.OPTION1,getTreatTasks()));
		}

		private void showPetFeedingDialogOrToaster(VirtalPetsFeedDialog.TreatData treat, bool forceShow = false)
		{
			
			if (!silentFeedPet  || forceShow)
			{
				DialogTask feedDialogTask =Scheduler.Scheduler.findTaskWith(VirtalPetsFeedDialog.dialogName) as DialogTask;
				if (feedDialogTask != null)
				{
					//already open
					List<DialogBase> dialogs = Dialog.instance.findOpenDialogsOfType(VirtalPetsFeedDialog.dialogName);
					
					if (dialogs.Count != 0)
					{
						//schedule another dialog directly after this one to feed.  This is most likely never going to happen as it would
						// require us to get another feed event aon a different frame from teh first feed event  but before the dialog pops.
						schedulePetFeedingDialog(treat);
					}
					else
					{
						//we haven't started playing the animation yet, so just update data
						List<VirtalPetsFeedDialog.TreatData> argsTasks = feedDialogTask.args[D.OPTION] as List<VirtalPetsFeedDialog.TreatData>;
						argsTasks.Add(treat);
					}
				}
				else
				{
					schedulePetFeedingDialog(treat);
					
				}

			}
			else
			{
				scheduleToaster("Silent Feed", Dict.create(D.AMOUNT, treat.treatAward));
			}
		}

		public void setDogName(string newName)
		{
			name = newName;
			VirtualPetsActions.setPetName(newName);
		}

		public void onPetCollect()
		{
			++timerCollectsUsed;
			
			if (timerCollectsUsed == hyperMaxTimerCollects)
			{
				UserActivityManager.instance.usedAllPetCollectsTime = GameTimer.currentTime;
			}
		}

		public bool isPettingRewardActive
		{
			get
			{
				return GameTimer.currentTime > nextPettingRewardTime;
			}
		}

		public void claimPettingReward()
		{
			nextPettingRewardTime = int.MaxValue; //Set this far into the future while we wait for the response with the real next reward time
			VirtualPetsActions.tapPet();
		}

		public void registerForAward(OnPetRewardDelegate awardEvent)
		{
			onRewardEvents -= awardEvent;
			onRewardEvents += awardEvent;
		}

		public void deregisterForAward(OnPetRewardDelegate awardEvent)
		{
			onRewardEvents -= awardEvent;
		}

		public void registerForFtueSeenEvent(OnFtueSeenDelegate ftueEvent)
		{
			onFtueSeen -= ftueEvent;
			onFtueSeen += ftueEvent;
		}

		public void deregisterForFtueSeenEvent(OnFtueSeenDelegate ftueEvent)
		{
			onFtueSeen -= ftueEvent;
		}
		
		public void registerForStatusUpdate(OnPetStatusDelegate statusEvent)
		{
			onPetStatusEvent -= statusEvent;
			onPetStatusEvent += statusEvent;
		}

		public void deregisterForStatusUpdate(OnPetStatusDelegate statusEvent)
		{
			onPetStatusEvent -= statusEvent;
		}
		
		public void registerForHyperStatusChange(OnHyperStatusChangeDelegate statusEvent)
		{
			onHyperStatusEvent -= statusEvent;
			onHyperStatusEvent += statusEvent;
		}

		public void deregisterForHyperStatusChange(OnHyperStatusChangeDelegate statusEvent)
		{
			onHyperStatusEvent -= statusEvent;
		}

		public CampaignDirector.FeatureTask[] getTreatTasks()
		{
			CampaignDirector.FeatureTask[] tasks = new CampaignDirector.FeatureTask[treatTasks.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				tasks[i] = CampaignDirector.getTask(treatTasks[i]);
			}

			return tasks;
		}

		private void onRewardSuccess(Rewardable rewardable)
		{
			if (rewardable == null || rewardable.feature != LOGIN_DATA_KEY)
			{
				return;
			}

			
			//call rewards event, coin add removed from dialog and added below
			if (onRewardEvents != null)
			{
				onRewardEvents(rewardable);
			}
			switch (rewardable.type)
			{
				case RewardablePetBasicEnergy.TYPE:
					onBasicTreatRewardable(rewardable as RewardablePetBasicEnergy);
					break;
				
				case RewardableSpecialPetEnergy.TYPE:
					onSpecialTreatRewardable(rewardable as RewardableSpecialPetEnergy);
					break;
				
				case RewardCoins.TYPE:
					//TODO: Nothing, let the calling function add credits to get valid source for logging
					break;
			}

			//consume reward
			rewardable.consume();

		}

		private void onPetTreatRewardSuccess(string treatType, List<Rewardable> rewards)
		{
			if (rewards == null)
			{
				return;
			}

			lastTreatTypeUsed = treatType;
			long totalEnergy = 0;
			long previousEnergy = 0;
			int previousPlaytimeEnd = 0;
			int playtimeEnd = 0;
			long totalCredits = 0;
			
			//save before treat data
			previousEnergy = currentEnergy;
			previousPlaytimeEnd = hyperEndTime;
			
			
			for (int i = 0; i < rewards.Count; i++)
			{
				if (rewards[i] == null || rewards[i].feature != LOGIN_DATA_KEY)
				{
					continue;
				}
				
				switch (rewards[i].type)
				{
					case RewardablePetBasicEnergy.TYPE:
						onBasicTreatRewardable(rewards[i] as RewardablePetBasicEnergy);
						break;
				
					case RewardableSpecialPetEnergy.TYPE:
						onSpecialTreatRewardable(rewards[i] as RewardableSpecialPetEnergy);
						break;
					
					case RewardCoins.TYPE:
						RewardCoins coinsReward = rewards[i] as RewardCoins;
						if (coinsReward != null)
						{
							totalCredits += coinsReward.amount;
						}
						break;
				}
				
				//consume
				rewards[i].consume();

			}
			
			//set after treat data
			totalEnergy = currentEnergy;
			playtimeEnd = hyperEndTime;

			// Add pending credits to avoid desync.
			// 1. Pet feed dialog / toaster might take few frames to download and display.
			// 2. Pet feed dialog will not be scheduled until RichPassInGameCounter completes presentation.
			// Next spin might cut in to cause desync if we do not add pending credits here.
			if (totalCredits > 0)
			{
				Server.handlePendingCreditsCreated(PET_FEED_PENDING_CREDIT_SOURCE, totalCredits);
			}
			
			if (!silentFeedLoginTreat) // keeps the ftu login treat from displaying the feed dialog
			{
				showPetFeedingDialogOrToaster(new VirtalPetsFeedDialog.TreatData(treatType, previousEnergy, totalEnergy, previousPlaytimeEnd, playtimeEnd, totalCredits));
			}
			else
			{
				silentFeedLoginTreat = false;
				
				//Instantly add the credits for the first treat where we skip the toaster & dialog
				if (totalCredits > 0)
				{
					SlotsPlayer.addFeatureCredits(totalCredits, PET_FEED_PENDING_CREDIT_SOURCE);
				}
			}
		}

		public bool isPackageEligibleForTreat(CreditPackage package)
		{
			if (!ftueSeen)
			{
				return false; //Can't receive buy page treats until feature is unlocked & activated after FTUE respin has been seen
			}

			//Package is eligible for a treat as long as its price is higher than any of our treats
			foreach (KeyValuePair<int, string> kvp in ExperimentWrapper.VirtualPets.specialTreatPrices)
			{
				if (kvp.Key <= package.purchasePackage.priceTier)
				{
					return true;
				}
			}
			return false;
		}

		public VirtualPetTreat getTreatTypeForPackage(PurchasablePackage package)
		{
			if (!ftueSeen)
			{
				return null; //Can't receive buy page treats until feature is unlocked & activated after FTUE respin has been seen
			}

			string treatTypeKey = "";

			//Loop through the treats configured in EOS and find the one with a price equal to the given package or the next lowest one
			foreach (KeyValuePair<int, string> kvp in ExperimentWrapper.VirtualPets.specialTreatPrices)
			{
				if (kvp.Key <= package.priceTier)
				{
					treatTypeKey = kvp.Value;
				}
				else
				{
					//Stop looking for the desired treat once we've hit a price requirement higher than the given package
					break;
				}
			}

			if (petTreats != null && petTreats.TryGetValue(treatTypeKey, out VirtualPetTreat treat))
			{
				return treat;
			}

			return null;
		}

		public static void populateTreats(JSON[] treatsData)
		{
			for (int i = 0; i < treatsData.Length; i++)
			{
				string name = treatsData[i].getString("name", "");
				petTreats[name] = new VirtualPetTreat(name, treatsData[i]);
			}
		}

		public static void resetStaticClassData()
		{
			instance = null;
		}
	}
}