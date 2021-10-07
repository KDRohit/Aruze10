
using Com.Scheduler;
using Zynga.Core.Util;
using UnityEngine;

namespace Com.HitItRich.EUE
{
	public class EUEManager 
	{
		/* Assets */
		public const string IN_GAME_COUNTER_PREFAB_PATH = "Features/EUE FTUE/Prefabs/EUE FTUE In Game Challenge Item";
		public const string IN_GAME_PANEL_PREFAB_PATH = "Features/EUE FTUE/Prefabs/EUE FTUE In Game Panel Item";
		public const string CHARACTER_ITEM_PREFAB_PATH = "Features/EUE FTUE/Prefabs/EUE FTUE Overlay";
		public const string LEVEL_UP_ANIMATION_PATH = "Features/EUE FTUE/Animation/Rich Level Up/Rich Level Up";

		public const int RICH_TIP_LEVEL = 2;
		
		//Max level integer at which the active discovery "new" badge will not show
		public const string ACTIVE_DISCOVERY_MAX_LEVEL_LIVEDATA = "EUE_ACTIVE_DISCOVERY_MAX_LEVEL";
		public const int ACTIVE_DISCOVERY_DEFAULT_MAX_LEVEL = 100;
		private static int activeDiscoveryMaxLevel = 0;

		public static bool shouldDisplayInGame
		{
			get
			{
				return ExperimentWrapper.EueFtue.isInExperiment && CampaignDirector.eue != null &&
				       (!CampaignDirector.eue.isComplete || pauseInGameCounterUpdates);
			}
		}

		public static bool pauseInGameCounterUpdates { get; private set; }
		
		//cannot be set true outside of this class
		public static bool hasPendingDialog { get; private set; }

		public static void clearPendingDialog()
		{
			hasPendingDialog = false;
		}

		public static bool isEnabled
		{
			get
			{
				return ExperimentWrapper.EueFtue.isInExperiment && SlotsPlayer.instance.socialMember.experienceLevel < ExperimentWrapper.EueFtue.maxLevel;
			}
		}

		private static bool isFirstLoginOnThisDevice
		{
			get
			{
				PreferencesBase preferences = SlotsPlayer.getPreferences();
				string installDate = preferences.GetString(Prefs.FIRST_APP_START_TIME, null);
				return string.IsNullOrEmpty(installDate) || NotificationManager.DayZero;
			}
		}

		private static bool isFirstLogin
		{
			get
			{
				return isEnabled && GameExperience.totalSpinCount == 0 && isFirstLoginOnThisDevice;
			}
		}
		
		public static bool shouldDisplayFirstLoadOverlay
		{
			get
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				return isEnabled && !prefs.GetBool(Prefs.FTUE_FIRST_LOGIN);
			}
			
		}

		public static bool shouldDisplayBonusCollect
		{
			get
			{
				return isEnabled && !CustomPlayerData.getBool(CustomPlayerData.DAILY_BONUS_COLLECTED, false);
			}
		}

		public static bool shouldDisplayGameIntro
		{
			get
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				return isEnabled && !prefs.GetBool(Prefs.FTUE_GAME_INTRO) && !prefs.GetBool(Prefs.FTUE_ABORT);
			}
		}

		public static bool shouldDisplayChallengeIntro
		{
			get
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				return isEnabled && CampaignDirector.eue != null && CampaignDirector.eue.isActive && !prefs.GetBool(Prefs.FTUE_CHALLENGE_INTRO) && !prefs.GetBool(Prefs.FTUE_ABORT);
			}
		}

		public static bool shouldDisplayChallengeComplete
		{
			get
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				//use complete check to see if there are multiple objectives/missions.
				return isEnabled && CampaignDirector.eue != null && CampaignDirector.eue.isActive && !CampaignDirector.eue.isComplete && !prefs.GetBool(Prefs.FTUE_CHALLENGE_COMPLETE) && !prefs.GetBool(Prefs.FTUE_ABORT);
			}
			
		}

		public static bool isComplete
		{
			get
			{
				return !isEnabled || CampaignDirector.eue == null || CampaignDirector.eue.isComplete;
			}
		}

		//Max level at which the EUE active discovery 'new' badge is hidden.
		//This value is common for all the features using EUE Active Discovery
		public static bool reachedActiveDiscoveryMaxLevel
		{
			get
			{
				if (activeDiscoveryMaxLevel == 0)
				{
					activeDiscoveryMaxLevel = Data.liveData.getInt(ACTIVE_DISCOVERY_MAX_LEVEL_LIVEDATA,  ACTIVE_DISCOVERY_DEFAULT_MAX_LEVEL);
				}
				return SlotsPlayer.instance.socialMember.experienceLevel >= activeDiscoveryMaxLevel;
			}
		}

		//We pass in the playerLevel because on a level up event the SlotsPlayer.instance.socialMember.experienceLevel
		//is still the previous level. The event json has the newLevel in it which should be passed here.
		public static bool showWeeklyRaceActiveDiscovery(int playerLevel)
		{
			return ExperimentWrapper.WeeklyRace.activeDiscoveryEnabled &&
			       playerLevel >= ExperimentWrapper.WeeklyRace.activeDiscoveryLevel &&
			       !CustomPlayerData.getBool(CustomPlayerData.EUE_ACTIVE_DISCOVERY_WEEKLY_RACE, false);
		}
		
		public static bool showLoyaltyLoungeActiveDiscovery
		{
			get
			{
				return ExperimentWrapper.NetworkProfile.activeDiscoveryEnabled &&
				       SlotsPlayer.instance.socialMember.experienceLevel >= ExperimentWrapper.NetworkProfile.activeDiscoveryLevel && 
				       !CustomPlayerData.getBool(CustomPlayerData.EUE_ACTIVE_DISCOVERY_LOYALTY_LOUNGE, false);
			}
		}
		
		public static bool showTrophiesActiveDiscovery
		{
			get
			{
				return ExperimentWrapper.NetworkAchievement.activeDiscoveryEnabled &&
				       SlotsPlayer.instance.socialMember.experienceLevel >= ExperimentWrapper.NetworkAchievement.activeDiscoveryLevel &&  
				       !CustomPlayerData.getBool(CustomPlayerData.EUE_ACTIVE_DISCOVERY_TROPHIES, false);
			}
		}

		public static bool isBelowTrophiesActiveDiscoveryLevel
		{
			get
			{
				return ExperimentWrapper.NetworkAchievement.activeDiscoveryEnabled &&
				       SlotsPlayer.instance.socialMember.experienceLevel < ExperimentWrapper.NetworkAchievement.activeDiscoveryLevel;
			}
		}
		
		public static bool showFriendsActiveDiscovery
		{
			get
			{
				return ExperimentWrapper.CasinoFriends.activeDiscoveryEnabled &&
				       SlotsPlayer.instance.socialMember.experienceLevel >= ExperimentWrapper.CasinoFriends.activeDiscoveryLevel && 
				       !CustomPlayerData.getBool(CustomPlayerData.EUE_ACTIVE_DISCOVERY_FRIENDS, false);
			}
		}

		public static void logActiveDiscovery(string featureName)
		{
			StatsManager.Instance.LogCount("game_actions", "active_discovery", featureName,
				val: SlotsPlayer.instance.socialMember.experienceLevel);
		}
		
		public static void showFirstLoadOverlay()
		{
			EueFtueRichDialog.Show(EueFtueRichDialog.OverlayState.FIRST_LOGIN);
		}

		public static void showBonusCollect()
		{
			EueFtueRichDialog.Show(EueFtueRichDialog.OverlayState.FORCED_BONUS);
		}

		public static void showGameIntro()
		{
			EueFtueRichDialog.Show(EueFtueRichDialog.OverlayState.GAME_INTRO);
		}

		public static void showChallengeIntro()
		{
			EueFtueRichDialog.Show(EueFtueRichDialog.OverlayState.CHALLENGE_INTRO, SchedulerPriority.PriorityType.IMMEDIATE);
		}

		public static void onChallengeComplete()
		{
			if (shouldDisplayChallengeComplete)
			{
				hasPendingDialog = true;
			}
			
			InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_KEY, Dict.create(D.KEY, true));
			if (CampaignDirector.eue != null && CampaignDirector.eue.isComplete)
			{
				if (!LevelUpUserExperienceFeature.instance.isEnabled)
				{
					//if pause updaters if false, we haven't got the level up dialog yet so pause them
					//if pause updates is true, we have gotten the level up dialog, so un-pause them
					pauseInGameCounterUpdates = !pauseInGameCounterUpdates;
				}

				InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_COUNTER_KEY, Dict.create(D.KEY, true, D.CALLBACK,
					new DialogBase.AnswerDelegate((args) => { showEndDialog(); })));	
			}
			else
			{
				InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.EUE_COUNTER_KEY, Dict.create(D.KEY, true));
			}
		}

		public static void showEndPresentation()
		{
			if (!LevelUpUserExperienceFeature.instance.isEnabled)
			{
				if (!pauseInGameCounterUpdates)
				{
					//complete event hasn't arrived yet
					pauseInGameCounterUpdates = true;
				}
				else if (pauseInGameCounterUpdates)
				{
					pauseInGameCounterUpdates = false;
				}
			}
		}

		public static void showEndDialog()
		{
			SlotventuresChallengeCampaign campaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;
			if (campaign != null && ExperimentWrapper.Slotventures.isEUE && !campaign.isComplete)
			{
				Scheduler.Scheduler.addDialog("eue_end_slotventure");
			}
			else
			{
				Scheduler.Scheduler.addDialog("eue_end_generic");
			}
		}

		public static void showChallengeComplete()
		{
			EueFtueRichDialog.Show(EueFtueRichDialog.OverlayState.CHALLENGE_COMPLETE, SchedulerPriority.PriorityType.IMMEDIATE);
		}

		public static bool canCollectDailyBonus
		{
			get
			{
				return SlotsPlayer.instance != null && 
				       SlotsPlayer.instance.dailyBonusTimer != null &&
				       SlotsPlayer.instance.dailyBonusTimer.isExpired;
			}
		}

		public static bool showLobbyEueFtue()
		{
			if (shouldDisplayFirstLoadOverlay)
			{
				//run the ftue
				showFirstLoadOverlay();
				return true;
			}
			
			if (shouldDisplayBonusCollect && canCollectDailyBonus)
			{
				showBonusCollect();
				return true;
			}
			
			if (shouldDisplayGameIntro)
			{
				showGameIntro();
				return true;
			}

			return false;
		}
	}    
}


