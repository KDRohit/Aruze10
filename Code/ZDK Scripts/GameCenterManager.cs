
// 
//  @file      GameCenterManager.cs
//  @authors    Adam Rutland arutland@zynga.com

//  Class that holds manages experiment defaults, 
//  pulls experiments from DAPI. This also handles
//  experiment overrides that are set with admin tool.

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Zynga.Zdk;
using UnityEngine.SocialPlatforms;
#if UNITY_IPHONE
using UnityEngine.SocialPlatforms.GameCenter;
#endif
using System.Runtime.InteropServices;

public class GameCenterManager : IDependencyInitializer {

	// Returns the singleton instance of GameCenterManager
	public static GameCenterManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new GameCenterManager();
			}
			return _instance;
		}
	}
	private static GameCenterManager _instance;
	
	private static List<KeyValuePair<string, double>> m_achievementsToReportList;
	private static IAchievement[] m_achievementsArray;
	
	/// Auths for GameCenter without showing the UI. If auth fails then nothing happens for the user.
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")] private static extern void _authWithoutLoginPrompt();
#else
	private static void _authWithoutLoginPrompt() {}
#endif

	private static Dictionary<string, long> _reportedScores = new Dictionary<string, long>();
//	private static Dictionary<string, int> _achievements = new Dictionary<string, int>();

	public static int lastLoginPrompt
	{
		get
		{
			return PlayerPrefsCache.GetInt(Prefs.LAST_GC_LOGIN_PROMPT, 0);
		}
		set
		{
			PlayerPrefsCache.SetInt(Prefs.LAST_GC_LOGIN_PROMPT, value);
			PlayerPrefsCache.Save();
		}
	}

	public static int GCLoginAttempts
	{
		get
		{
			return PlayerPrefsCache.GetInt(Prefs.LAST_GC_LOGIN_ATTEMPTS, 0);
		}

		set
		{
			PlayerPrefsCache.SetInt(Prefs.LAST_GC_LOGIN_ATTEMPTS, value);
			PlayerPrefsCache.Save();
		}
	}

	// Check if the Social Api is available on this platform
	public static bool isSocialAvailable
	{
		get 
		{ 
			// Only Game Center right now
			return Application.platform == RuntimePlatform.IPhonePlayer; 
		}
	}

	/// <summary>
	///  Checks to see if the user is already logged in to GC
	/// </summary>
	/// <returns><c>true</c>, if user authenticated, <c>false</c> otherwise.</returns>
	public static bool isUserAuthenticated()
	{
		if (isSocialAvailable && Social.localUser.authenticated)
		{
			return true;
		}

		return false;
	}

	// Callback from _authWithoutLoginPrompt Plugin fires on success and failure
	public void confirmGameCenterAuth(string message) 
	{
		processAuthentication(isUserAuthenticated());
	}

	// Governs whether Authentication UI will appear.
	// Spec: Not on first day - If cancel not for 7 days - If cancel twice, never again
	// Reset logic if ever logged in with GC
	private static bool shouldShowLoginUI()
	{
		// Check used to allow users to login without following the spec
		if (Data.IsSandbox)
		{
			if (PlayerPrefsCache.GetInt(DebugPrefs.FORCE_GC_LOGIN_PROMPT, 0) > 0)
			{
				PlayerPrefsCache.SetInt(DebugPrefs.FORCE_GC_LOGIN_PROMPT, 0);
				PlayerPrefsCache.Save();
				return true;
			}
		}

		// Disallow if less than 24 hours since the game was installed:
		int elapsedTimeSeconds = GameTimer.sessionStartTime - SlotsPlayer.instance.firstPlayTime;
		if (elapsedTimeSeconds <= Glb.GAMECENTER_SEC_AFTER_INSTALL)
		{
			return false;
		}

		// If we've already had one login attempt:
		if (GCLoginAttempts == 1)
		{
			// Disallow if less than 7 days since last prompted:
			int loginPromptElapsed = GameTimer.sessionStartTime - lastLoginPrompt;
			if (loginPromptElapsed <= Glb.GAMECENTER_SEC_REPEAT_PERIOD)
			{
				return false;
			}
		}

		// If we've already had more than one login attempt, don't try any more:
		if (GCLoginAttempts > 1)
		{
			return false;
		}

		return true;
	}

	// The only major issue with GC Login is that
	// You can only attempt to Authenticate once per app load
	// If the Authentication doesn't get closed. This happens if the user cancels
	// Or if we use _authWithoutLoginPrompt
	// So this is called only at app load and won't really function after
	public static void checkAndAuth()
	{
		if (isSocialAvailable)
		{
			if (shouldShowLoginUI())
			{
				authOrLoginUI();
			}
			else
			{
				_authWithoutLoginPrompt();
			}
		}
	}

	// Use Unity Functions to show login UI
	// Will auth without UI if already logged in
	public static void authOrLoginUI()
	{
		lastLoginPrompt = (int)NotificationManager.CurrentUnixSeconds();
		GCLoginAttempts++;
		Social.localUser.Authenticate(processAuthentication);
	}

	private static void processAuthentication(bool success)
	{
		if (success)
		{
			//Debug.Log("GC Authentication successful.");
			// Reset attempts assuming successful Login means they want to login again later
			GCLoginAttempts = 0;
		//	loadAchievements();
		}
		else
		{
			Debug.LogWarning("Failed to authenticate");
		}
	}

	public static void showAchievementUI()
	{
		if (isUserAuthenticated())
		{
			Social.ShowAchievementsUI();
		}
	}

	public static void showLeaderboardUI()
	{
		if (isUserAuthenticated())
		{
			Social.ShowLeaderboardUI();
		}
	}

	private static void loadAchievements()
	{
		if (isUserAuthenticated())
		{
			Social.LoadAchievements(processLoadedAchievements);
		}
	}

	private static void processLoadedAchievements(IAchievement[] achievements) 
	{
		if (m_achievementsArray != null)
		{
			m_achievementsArray = null;	
		}

		if (achievements.Length > 0)
		{
			Debug.Log(achievements.Length + " achievements loaded.");
			m_achievementsArray = achievements;
		}
		else
		{
			Debug.Log("No achievements found");
		}
	}

	public static bool isAchievementComplete(string achievementID)
	{
		if (isUserAuthenticated())
		{
			if (m_achievementsArray != null)
			{
				foreach(IAchievement achievement in m_achievementsArray)
				{
					if (achievement.id == achievementID && achievement.completed)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public static void addAchievementsProgress(List<KeyValuePair<string, double>> achievementsList)
	{
		if (isUserAuthenticated())
		{
			m_achievementsToReportList = achievementsList;
			reportAchievementsProgress(0);
		}
	}

	public static void reportAchievementsProgress(int index)
	{
		//clear the previously saved achievements
		if (m_achievementsArray != null)
		{
			m_achievementsArray = null;	
		}

		if (isUserAuthenticated())
		{
			//load the current achievements from Game Center
			Social.LoadAchievements( (achievements) => {
				//store these achievements to our instance array
				m_achievementsArray = achievements;
				
				//extract the achievement ID and the achievement progress to be submitted
				KeyValuePair<string, double> achievementKeyValue = m_achievementsToReportList[index];
				string achievementID = achievementKeyValue.Key;
				double progressCompleted = achievementKeyValue.Value;
				
				//from our recently saved array of achievements,
				//extract the player's current progress on this achievement, if there is any
				//then add the new progress made
				IAchievement a = getAchievement(achievementID);
				if(a != null)
				{
					progressCompleted += a.percentCompleted;
				}

				//Now that we have the total progress, submit it
				Social.ReportProgress( achievementID, progressCompleted, (success) => {

					if(success)
					{
						Debug.Log(success ? "Reported progress successfully" : "Failed to report progress");
					}
				} );
				
				//go through the rest of achievements in the array
				++index;

				if(index < m_achievementsToReportList.Count)
				{
					reportAchievementsProgress(index);
				}

			} );
		}
	}

	private static IAchievement getAchievement(string achievementID)
	{
		if (m_achievementsArray != null)
		{
			foreach(IAchievement achievement in m_achievementsArray)
			{
				if (achievement.id == achievementID)
				{
					return achievement;
				}
			}
		}

		return null;
	}

	// Reports a high score that doesn't depend on any previous information
	private static void reportHighScore(long scoreToReport, string leaderBoardID)
	{
		if (isUserAuthenticated())
		{
			if (!_reportedScores.ContainsKey(leaderBoardID) || _reportedScores[leaderBoardID] < scoreToReport)
			{
				//report score
				reportScore(scoreToReport, leaderBoardID);
			}
		}
	}

	// Reports a score that depends on previously reported score
	private static void reportScoreContinuous(long scoreToReport, string leaderBoardID)
	{
		if (isUserAuthenticated())
		{
			long currentScore = 0;

			// Look this up once per app reset otherwise just use last reported value
			if (!_reportedScores.ContainsKey(leaderBoardID))
			{
					//retrieve current score of the user, if there is any
					Social.LoadScores(leaderBoardID, scores => {
					if (scores.Length > 0) 
					{
						foreach (IScore score in scores)
						{
							if (Social.localUser.id == score.userID)
							{
								currentScore = score.value;
							}
						}
					}
					
					//add the previous progress with the current score
					scoreToReport += currentScore;

					//report score
					reportScore(scoreToReport, leaderBoardID);
				} );
			}
			else
			{
				//add the previous progress with the current score
				scoreToReport += _reportedScores[leaderBoardID];
				
				//report score
				reportScore(scoreToReport, leaderBoardID);
			}
		}
	}

	private static void reportScore(long scoreToReport, string leaderBoardID)
	{
		Social.ReportScore(scoreToReport, leaderBoardID, success => {

			if (success)
			{
				_reportedScores[leaderBoardID] = scoreToReport;
			}
			
			Debug.Log(success ? ("Reported score successfully: " + leaderBoardID + " " + scoreToReport) : "Failed to report score");
		} );
	}

	public static void reportPlayerWinnings(long winnings)
	{
		reportScoreContinuous(winnings, "hir_lifetime_winnings");
	}

	public static void reportChallengeWon()
	{
		reportScoreContinuous(1, "hir_challenges_won");
	}

//  Update and Report Achievements currently disabled
//	public static void updateGameCenterProgress()
//	{
//		if (isUserAuthenticated())
//		{
//			List<KeyValuePair<string, double>> achievementsList = new List<KeyValuePair<string, double>>();
//			string achievementID;
//			
//			//Achievement 1
//			achievementID = "com.zynga.hititrich.achievement1";	
//			if (!isAchievementComplete(achievementID))
//			{
//				KeyValuePair<string, double> achievement1 = new KeyValuePair<string, double>(achievementID, 10.0d);
//				achievementsList.Add(achievement1);
//			}
//			
//			//Achievement 2
//			achievementID = "com.zynga.hititrich.achievement2";
//			int achievement2Counter = PlayerPrefsCache.GetInt(achievementID, 0);
//			
//			if (!isAchievementComplete(achievementID))
//			{
//				if (achievement2Counter < 10)
//				{
//					PlayerPrefsCache.SetInt(achievementID, ++ achievement2Counter);
//				}
//				
//				if (achievement2Counter % 10 == 0)
//				{
//					KeyValuePair<string, double> achievement2 = new KeyValuePair<string, double>(achievementID,1.0d);
//					achievementsList.Add(achievement2);
//				}
//			}
//			
//			if (achievementsList.Count > 0)
//			{
//				addAchievementsProgress(achievementsList);
//			}
//
//			//Leaderboard 1
//			string leaderboardID = "com.company.testapp.LeaderboardTest1";
//			int leaderboard1Counter = PlayerPrefsCache.GetInt(leaderboardID, 0);
//			if (leaderboard1Counter < 10)
//			{
//				PlayerPrefsCache.SetInt(leaderboardID, ++ leaderboard1Counter);
//			}
//			
//			if (leaderboard1Counter % 10 == 0)
//			{
//				reportScore(100, leaderboardID);
//			}
//		}
//	}
	
	#region ISVDependencyInitializer implementation	
	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	public Type[] GetDependencies() {
		return new Type[] { typeof (AuthManager), typeof(SocialManager)};
	}

	public void Initialize(InitializationManager mgr) 
	{	
		//checkAndAuth(); //TODO: This call happens earlier in the dependency chain than it should.  Why? Commenting out as it is currently useless.
		_reportedScores.Clear();
		mgr.InitializationComplete(this);
	
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "GameCenterManager";
	}
	#endregion

}
