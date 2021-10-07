using UnityEngine;
using Com.HitItRich.Feature.VirtualPets;

public class DevGUIMenuVirtualPets : DevGUIMenu, IResetGame
{
	private const string TEST_DATA_DIRECTORY = "Test Data/VirtualPets/";
	private const string LOGIN_FILE = "login";
	private static bool loadedTestData = false;
	private static int fullyFedStreak = 1;
	private static int energyAmount = 0;

	public static long bonusPayoutOverride
	{
		get
		{
			return _enableBonusDataPayout ? _bonusDataPayout : 0;
		}
	}

	private static long _bonusDataPayout = 30;
	private static bool _enableBonusDataPayout;

	public override void drawGuts()
	{
		bool featureEnabled = VirtualPetsFeature.instance?.isEnabled ?? false;


		GUIStyle redStyle = new GUIStyle();
		redStyle.normal.textColor = Color.red;
		
		GUIStyle greenStyle = new GUIStyle();
		greenStyle.normal.textColor = Color.green;
		
		GUILayout.BeginVertical();	
	
		
		
		GUILayout.Label("Feature Enabled: " + featureEnabled);
		if (featureEnabled)
		{
			GUILayout.BeginHorizontal();
			
			GUILayout.BeginVertical();
			
			GUILayout.Label("Pet Name: " + VirtualPetsFeature.instance.petName);
			GUILayout.Label("FTUE Seen: " + VirtualPetsFeature.instance.ftueSeen);
			GUILayout.Label("Current Energy: " +  VirtualPetsFeature.instance.currentEnergy);
			GUILayout.Label("Max Energy: " + VirtualPetsFeature.instance.maxEnergy);
			GUILayout.Label("Is Hyper: " + VirtualPetsFeature.instance.isHyper);
			GUILayout.Label("Hyper Reached: " + VirtualPetsFeature.instance.hyperReached);
			GUILayout.Label("Hyper End Time (utc): " +  Common.convertFromUnixTimestampSeconds(VirtualPetsFeature.instance.hyperEndTime));
			GUILayout.Label("User Collected bonus: " + VirtualPetsFeature.instance.timerCollectsUsed);
			GUILayout.Label("Max Collects - Standard: " + VirtualPetsFeature.instance.normalMaxTimerCollects);
			GUILayout.Label("Max Collects - Hyper: " + VirtualPetsFeature.instance.hyperMaxTimerCollects);
			GUILayout.Label("Fully Fed Streak: " + VirtualPetsFeature.instance.fullyFedStreak);
			System.DateTime date = Common.convertFromUnixTimestampSeconds(VirtualPetsFeature.instance.nextPettingRewardTime);
			GUILayout.Label("Next Pet Reward Time (utc): " + CommonText.formatDateTime(date)); 
			GUILayout.Label("Silent Feed Pet: " + VirtualPetsFeature.instance.silentFeedPet);
			date = Common.convertFromUnixTimestampSeconds(VirtualPetsFeature.instance.hyperStreakStartTime);
			GUILayout.Label("Hyper streak start time: " + CommonText.formatDateTime(date));
			
			//the pet can collect the bonus up until the start of the next day, so within 24 hours of the streak start
			//streak start is 10am (rollover time) of the day the streak started according to qd
			int totalTimeOfStreak = Common.SECONDS_PER_DAY * VirtualPetsFeature.instance.fullyFedStreak;
			int maxTimeForPetCollect = VirtualPetsFeature.instance.hyperStreakStartTime + totalTimeOfStreak;
			date = Common.convertFromUnixTimestampSeconds(maxTimeForPetCollect);
			GUILayout.Label("Max available time for pet collect: " + CommonText.formatDateTime(date));
			date = Common.convertFromUnixTimestampSeconds(UserActivityManager.instance.usedAllPetCollectsTime);
			GUILayout.Label("Last time all pet collects were used: " + CommonText.formatDateTime(date));
			GUILayout.Label("Timer expiration date: " + SlotsPlayer.instance.dailyBonusTimer.dateLastCollected);
			
			if (VirtualPetsFeature.canPetCollectBonus)
			{
				GUILayout.Label("Pet can collect bonus", greenStyle);	
			}
			else
			{
				if (!VirtualPetsFeature.canPetCollectBonusForCurrentDay)
				{
					GUILayout.Label("Pet cannot collect bonus for today:" +
					                System.Environment.NewLine +
					                VirtualPetsFeature.collectNotValidReason, redStyle);	
				}

				if (!VirtualPetsFeature.didPetCollectBonusLastSession)
				{
					GUILayout.Label("Pet did not collect bonus while you're away: " +
					                System.Environment.NewLine +
					                VirtualPetsFeature.collectLastSessionNotValidReason, redStyle);
				}
					
			}

			if (loadedTestData)
			{
				//give option to disable feature
				if (GUILayout.Button("Disable Feature"))
				{
					loadedTestData = false;
					VirtualPetsFeature.resetStaticClassData();
				}
			}
			
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical();

#if !ZYNGA_PRODUCTION
			GUILayout.Label("Treat Tasks:");
			for (int i = 0; i < VirtualPetsFeature.instance.treatTasks.Count; i++)
			{
				string id = VirtualPetsFeature.instance.treatTasks[i];
				if (string.IsNullOrEmpty(id))
				{
					continue;
				}
				CampaignDirector.FeatureTask task = CampaignDirector.getTask(id);
				if (task == null)
				{
					continue;
				}

				GUILayout.BeginHorizontal();
				GUILayout.Label("Task: " + task.type + ", progress: " + task.progress + ", target: " + task.target, task.isComplete ? greenStyle : redStyle);
				if (task.isComplete)
				{
					if (GUILayout.Button("Claim Reward"))
					{
						//TODO: put in treat claim action action
					}
				}
				else
				{
					if (GUILayout.Button("Complete"))
					{
						VirtualPetsActions.devSetTaskComplete(task.id);
					}
				}
				
				GUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Reset All Tasks"))
			{
				VirtualPetsActions.devResetTasks();
			}
			
			if (GUILayout.Button("Grant Special Treat"))
			{
				VirtualPetsActions.devAddSpecialTreat();
			}
			
			//Resetting various things so that we can simulate being on a new day of the pets features
			//Should make it so its possible to increase the streak count bby reaching hyper again
			if (GUILayout.Button("Go To Next Day"))
			{
				VirtualPetsActions.devResetTasks(); //Reset Tasks
				RichPassAction.resetPlayer(); //Reset Rich Pass Progress
				VirtualPetsActions.devSetHyperEndTime(GameTimer.currentTime - Common.SECONDS_PER_HOUR*8); //End Hyper Time
				VirtualPetsActions.devResetTimerCollectPerk();

				//Set streak day to be 10AM for the previous day 
				System.DateTime currentTime = System.DateTime.UtcNow;
				int expireDay = currentTime.Hour >= 18 ? currentTime.Day - 1 : currentTime.Day-2;
				System.DateTime resetTime = new System.DateTime(currentTime.Year, currentTime.Month, expireDay, 18, 0, 0, System.DateTimeKind.Utc);
				VirtualPetsActions.devSetFullyFedTimestamp(Common.convertToUnixTimestampSeconds(resetTime));
				
				//Force idle so bonus fetch is active at login
				UserActivityManager.instance.debugForceIdleNow();
				UserActivityManager.instance.forceWriteToDisk();
				
				//Force daily bonus to be active
				int nextBonusDay = SlotsPlayer.instance.dailyBonusTimer.day < 7 ? SlotsPlayer.instance.dailyBonusTimer.day+1 : 7;
				
				CreditAction.setTimerClaimDay(nextBonusDay, ExperimentWrapper.NewDailyBonus.bonusKeyName);

				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, "Reloading",
						D.MESSAGE, "Updating Pets Data",
						D.REASON, "pets-day-reset",
						D.CALLBACK, new DialogBase.AnswerDelegate((args) => 
						{
							Glb.resetGame("pets_reset"); 
						})
					),
					Com.Scheduler.SchedulerPriority.PriorityType.IMMEDIATE
				);
			
				DevGUI.isActive = false;
			}
#endif
			GUILayout.EndVertical();
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Show Feature Dialog"))
			{
				DevGUI.isActive = false;
				VirtualPetsFeatureDialog.showDialog();
			}
			
			if (GUILayout.Button("Refresh Pet Status"))
			{
				VirtualPetsActions.refreshPetStatus();
			}
#if !ZYNGA_PRODUCTION
			if (GUILayout.Button("Reset Daily Bonus Fetch Count"))
			{
				VirtualPetsActions.devResetTimerCollectPerk();
			}
#endif
			
			if (GUILayout.Button("Play Pet Respin Animations"))
			{
				DevGUI.isActive = false;
				VirtualPetRespinOverlayDialog.showDialog();
			}

			Color oldColour = GUI.color;
			GUI.color = _enableBonusDataPayout ? Color.green: Color.red;
			if (GUILayout.Button("Toggle Debug Pet Bonus on Spin"))
			{
				_enableBonusDataPayout = !_enableBonusDataPayout;
			}

			GUI.color = oldColour;
			GUILayout.EndHorizontal();
		}
		else if (GUILayout.Button("Initialize with Test Data"))
		{
			//send login data
			JSON data = getFakeLoginData();
			loadedTestData = true;
			VirtualPetsFeature.instantiateFeature(data);
		}
		GUILayout.BeginHorizontal();
#if !ZYNGA_PRODUCTION
		if (GUILayout.Button("Show Toaster"))
        {
        	if (VirtualPetsFeature.instance != null)
        	{
        		if (VirtualPetsFeature.instance.petName == "")
        		{
        			VirtualPetsFeature.instance.testSetPetName("#Test Pet Name#");
        		}
        		string[] events = VirtualPetsFeature.instance.testEvents();
        		if (events.Length != 0)
        		{
        			int eventIndex = Random.Range(0, events.Length - 1);
        			string eventName = events[eventIndex];
        			int messageCount = VirtualPetsFeature.instance.testToasterEventMessageCount(eventName);
        			if (messageCount != 0)
        			{
        				int messageIndex = Random.Range(0, messageCount - 1);
        				VirtualPetsFeature.instance.testShowToasterForEventWithMessageIndex(eventName, messageIndex);
        			}
        		}
        	}
        }

		if (GUILayout.Button("Show Toaster With Substitute"))
		{
			if (VirtualPetsFeature.instance != null)
			{
				if (VirtualPetsFeature.instance.petName == "")
				{
					VirtualPetsFeature.instance.testSetPetName("#Test Pet Name#");
				}

				VirtualPetsFeature.instance.testShowToasterForEventWithMessageIndex("On Spin", 2);
			}
		}
#endif
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if(GUILayout.Button("Show Feed Dialog"))
		{
			VirtalPetsFeedDialog.showDialog();
		}
		if(GUILayout.Button("Toggle Silent Feed Pet"))
		{
			if (VirtualPetsFeature.instance != null)
			{
				VirtualPetsFeature.instance.silentFeedPet = !VirtualPetsFeature.instance.silentFeedPet;
			}
		}
		if (GUILayout.Button("Click Carousel Action"))
		{
			DoSomething.now("virtual_pet");
		}
		GUILayout.EndHorizontal();
		
#if !ZYNGA_PRODUCTION
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Initialize Pet"))
		{
			VirtualPetsActions.devInitialize();
		}
		
		if (GUILayout.Button("Set hyper end 1 hour now"))
		{
			int time = GameTimer.currentTime + Common.SECONDS_PER_HOUR;
			VirtualPetsActions.devSetHyperEndTime(time);
		}


		if (GUILayout.Button("Set hyper end in 10s"))
		{
			int time = GameTimer.currentTime + 10;
			VirtualPetsActions.devSetHyperEndTime(time);
		}
		
		if (GUILayout.Button("Set fully fed time -- now"))
		{
			VirtualPetsActions.devSetFullyFedTimestamp(GameTimer.currentTime);
		}

		if (GUILayout.Button("Activate Petting Reward"))
		{
			VirtualPetsActions.devSetPettingRewardTime(GameTimer.currentTime);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Streak Value:");
		string tempText = GUILayout.TextField(fullyFedStreak.ToString());
		if (!System.Int32.TryParse(tempText, out fullyFedStreak))
		{
			fullyFedStreak = 1;
		}
		
		if (GUILayout.Button("Set streak count"))
		{
			VirtualPetsActions.devSetFullyFedStreak(fullyFedStreak);
		}
		if (GUILayout.Button("Toggle FTUE"))
		{
			if (VirtualPetsFeature.instance != null)
			{
				VirtualPetsActions.devSetFtueSeen(!VirtualPetsFeature.instance.ftueSeen);
			}
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Energy Value:");
		string energyText = GUILayout.TextField(energyAmount.ToString());
		if (!System.Int32.TryParse(energyText, out energyAmount))
		{
			energyAmount = 1;
		}
		
		if (GUILayout.Button("Set Energy Amount"))
		{
			VirtualPetsActions.devSetEnergy(energyAmount);
		}
		GUILayout.EndHorizontal();

#endif
		GUILayout.EndVertical();
	}
	
	private JSON getFakeLoginData()
	{
		string testDataPath = TEST_DATA_DIRECTORY + LOGIN_FILE;
		TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
		string text = textAsset.text;
		//update start/end time to be current
		text = text.Replace("\"next_petting_reward_time\": 0", "\"next_petting_reward_time\": " +   (GameTimer.currentTime + 60));
		return new JSON(text);
	}
	
	public static void resetStaticClassData()
	{
		loadedTestData = false;
	}
}
