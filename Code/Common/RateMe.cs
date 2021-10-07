using UnityEngine;
using CustomLog;
using System.Runtime.InteropServices;

public static class RateMe
{
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")] private static extern void UnityRequestReview();
	//Ios in game review functionality first available on devices with os 10.3
	private const int IOS_IN_GAME_REVIEW_MIN_MAJOR_VERSION_NUMBER = 10;
	private const int IOS_IN_GAME_REVIEW_MIN_MINOR_VERSION_NUMBER = 3;
#endif

	// The number of days to prompt the user daily to rate the app.
	public const int MAX_PROMPTS = 3;

	// Set by LiveData
	public static int MIN_PROMPT_LEVEL = 1;

	public static bool pendingPurchasePrompt;

	public static System.DateTime lastPromptDateTime
	{
		get
		{
			if (_lastPromptDateTime ==  System.DateTime.MinValue)
			{
				string dateTimeString = PlayerPrefsCache.GetString(Prefs.LAST_RATE_ME_PROMPT_TIME, null);
				if (string.IsNullOrEmpty(dateTimeString) == false)
				{
					long fileTime = long.Parse(dateTimeString);
					_lastPromptDateTime = System.DateTime.FromFileTime(fileTime);
				}
			}

			return _lastPromptDateTime;
		}

		set 
		{
			if (value != _lastPromptDateTime)
			{
				_lastPromptDateTime = value;
				PlayerPrefsCache.SetString(Prefs.LAST_RATE_ME_PROMPT_TIME, value.ToFileTime().ToString());
				PlayerPrefsCache.Save();
			}
		}
	}
	private static System.DateTime _lastPromptDateTime = System.DateTime.MinValue;


	public static string lastCheckedVersion
	{
		get
		{
			if (_lastCheckedVersion == null)
			{
				_lastCheckedVersion = PlayerPrefsCache.GetString(Prefs.LAST_RATE_ME_PROMPT_VERSION, "");
			}

			return _lastCheckedVersion;
		}

		private set
		{
			if (value != _lastCheckedVersion)
			{
				_lastCheckedVersion = value;

			}
		}
	}
	private static string _lastCheckedVersion = null;
	
	public static bool hasPromptBeenAccepted
	{
		get
		{
			return (PlayerPrefsCache.GetInt(Prefs.RATE_ME_PROMPT_ACCEPTED_VERSION, 0) > 0);
		}
		set
		{
			PlayerPrefsCache.SetInt(Prefs.RATE_ME_PROMPT_ACCEPTED_VERSION, value ? 1 : 0);
			PlayerPrefsCache.Save();
		}		
	}

	public static bool hasBeenPromptedThisVersion
	{
		get 
		{
			return (Glb.clientVersion == (RateMe.lastCheckedVersion ?? ""));
		}
	}

	public static bool hasBeenPromptedToday
	{
		get 
		{
			return (System.DateTime.Compare(RateMe.lastPromptDateTime.Date, System.DateTime.Now.Date) == 0);
		}
	}

	public static bool hasBeenPromptedThisWeek
	{
		get
		{
			System.DateTime lastWeek = System.DateTime.Now.AddDays(-7);
			return (System.DateTime.Compare(lastWeek, RateMe.lastPromptDateTime) <= 0);
		}
	}
	
	public static bool exceededMaxPrompts
	{
		get
		{
			return versionViewCount >= MAX_PROMPTS;
		}
	}

	public static bool shouldPromptUser
	{
		get
		{
#if UNITY_WEBGL
			return false;
#else

			if (hasPromptBeenAccepted)
			{
				Log.log("Not prompting for rating because it has already been accepted by the user.");
				return false;
			}
			else if (exceededMaxPrompts)
			{
				Log.log("Not prompting for rating because we have exceeded the max prompts for the user.");
				return false;
			}
			else if (SlotsPlayer.instance == null ||
				SlotsPlayer.instance.socialMember == null ||
				SlotsPlayer.instance.socialMember.experienceLevel < MIN_PROMPT_LEVEL)
			{
				Log.log("Not prompting for rating because the player's level is too low.");
				return false;
			}
			else if (hasBeenPromptedThisVersion)
			{
				if (hasBeenPromptedThisWeek)
				{
					Log.log("Not prompting for rating because we should prompt weekly but have already prompted this week.");
					return false;
				}
			}
			else if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
			{
				Log.log("Not prompting for rating because we are in a Royal Rush game");
				return false;
			}

			return true;
#endif
		}		
	}

	public static int versionViewCount
	{
		get 
		{
			if (_versionViewCount < 0)
			{
				_versionViewCount = PlayerPrefsCache.GetInt(Prefs.RATE_ME_VERSION_VIEW_COUNT, 0);
			}

			return _versionViewCount;
		}

		set
		{
			if (value != _versionViewCount)
			{
				_versionViewCount = value;
				PlayerPrefsCache.SetInt(Prefs.RATE_ME_VERSION_VIEW_COUNT, _versionViewCount);
				PlayerPrefsCache.Save();
			}
		}
	}
	private static int _versionViewCount = -1;

	public static void checkAndPrompt(RateMeTrigger trigger = RateMeTrigger.MISC, bool force = false)
	{
#if UNITY_WEBGL
		return;
#else
		pendingPurchasePrompt = false;
		// When the app version changes, reset the view count.
		// Make sure to do this before checking if we should prompt the user.
		if (lastCheckedVersion != Glb.clientVersion)
		{
			versionViewCount = 0;
			hasPromptBeenAccepted = false;
			lastCheckedVersion = Glb.clientVersion;
		}

		if (!string.IsNullOrEmpty(Glb.clientAppstoreURL) &&
			(shouldPromptUser || force))
		{
			bool showedInGameReviewOption = false;

			if (SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining)
			{
				//HIR-65137 - Rate Me dialog shouldn't disrupt autospins
				return;
			}
#if UNITY_IPHONE && !UNITY_EDITOR
			//Show the in game review system if we can or else go back to the old way
			if (MobileUIUtil.getMajorOSVersion() > IOS_IN_GAME_REVIEW_MIN_MAJOR_VERSION_NUMBER || (MobileUIUtil.getMajorOSVersion() == IOS_IN_GAME_REVIEW_MIN_MAJOR_VERSION_NUMBER && MobileUIUtil.getMinorOSVersion() >= IOS_IN_GAME_REVIEW_MIN_MINOR_VERSION_NUMBER))
			{
				versionViewCount++;
				lastPromptDateTime = System.DateTime.Now;

				// Save those variables now just in case the app crashes.
				PlayerPrefsCache.Save();

				StatsManager.Instance.LogCount("dialog", "app_rating_prompt", "view", versionViewCount.ToString(), trigger.ToString().ToLower());
				showedInGameReviewOption = true;
				UnityRequestReview();
			}
#endif
			if (!showedInGameReviewOption)
			{
				// Update some variables about when the last time this dialog was shown.
				versionViewCount++;
				lastPromptDateTime = System.DateTime.Now;

				// Save those variables now just in case the app crashes.
				PlayerPrefsCache.Save();

				StatsManager.Instance.LogCount("dialog", "app_rating_prompt", "view", versionViewCount.ToString(), trigger.ToString().ToLower());
				
				RateMeDialog.showDialog(
					Dict.create(
						D.OPTION, trigger,
						D.CALLBACK, new DialogBase.AnswerDelegate(rateMeDialogCallback)
					)
				);
			}
		}
		else
		{
			Debug.Log(string.Format("RateMe will not prompt user now.  Last prompt '{0}'", lastPromptDateTime.ToString()));

			if (string.IsNullOrEmpty(Glb.clientAppstoreURL))
			{
				Log.log("Not prompting for rating because the app store URL is empty.");
			}
		}
#endif
	}
	
	// Callback after closing RATE_ME dialog.
	private static void rateMeDialogCallback(Dict args)
	{
		RateMeTrigger trigger = (RateMeTrigger)args.getWithDefault(D.OPTION, RateMeTrigger.MISC);
		bool userRatedApp = (args.getWithDefault(D.ANSWER, "no") as string) == "yes";
		StatsManager.Instance.LogCount(
			"dialog", 
			"app_rating_prompt", 
			"tap", 
			versionViewCount.ToString(), 
			trigger.ToString().ToLower(), 
			userRatedApp ? "ok": "not_now"
		);
		if (userRatedApp)
		{
			hasPromptBeenAccepted = true;
			Common.openUrlWebGLCompatible(Glb.clientAppstoreURL);
		}
	}

	public enum RateMeTrigger
	{
		MISC = 0,
		BIG_WIN,
		PURCHASE,
		WIN_CHALLENGE
	}
}


