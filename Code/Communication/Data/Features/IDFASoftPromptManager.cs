using System;
using Com.Scheduler;
using UnityEngine;
using Zynga.Core.Util;

// Similar to EUEManager.cs, has all IDFA common functions organized here.
namespace Com.HitItRich.IDFA
{
	public class IDFASoftPromptManager
	{
		public enum SurfacePoint
		{
			GameEntry,
			W2E
		}

		// Cache prefs for better performance
		private static PreferencesBase prefs
		{
			get
			{
				if (_cachedPrefs == null)
				{
					_cachedPrefs = SlotsPlayer.getPreferences();
				}

				return _cachedPrefs;
			}
		}

		private static PreferencesBase _cachedPrefs = null;

		public static void displayIDFADialog(SurfacePoint sp, Action onRequestFinish)
		{
#if (UNITY_IPHONE || UNITY_IOS)
			if (shouldDisplayIDFASoftDialog(sp))
			{
				// Add soft prompt, if the player chooses "Allow", then we need to bring up hard prompt
				// hard prompt can be seen only once unless you reinstall the app again.  if player choose "No Thanks"
				// we do not want to display them the hard prompt immediately, since the player will most likely
				// do "Ask App not to Track" at this point.  so we will do a cooldown and bring up soft prompt again after
				// a given time period, and the player might change his/her mind then.
				IDFASoftPromptDialog.showDialog(sp,
					(x) =>
					{
#if UNITY_EDITOR
						if (onRequestFinish != null)
						{
							onRequestFinish();
						}
#else
						// Bring up hard prompt if the player chose "Allow"
						// Add hard prompt
						addiOSHardPromptTask(new iOSPromptTask(Dict.create(D.OPTION, sp, D.CALLBACK, onRequestFinish)));
#endif
					},
					
					(x) =>
					{
						if (onRequestFinish != null)
						{
							onRequestFinish();
						}
					}
				);

				// Record the time and view count
				if (prefs != null)
				{
					int curTime = GameTimer.currentTime;
					switch (sp)
					{
						case SurfacePoint.GameEntry:
							prefs.SetInt(Prefs.IDFA_SOFTPROMPT_AT_ENTRY_TIME, curTime);
							break;
						case SurfacePoint.W2E:
							prefs.SetInt(Prefs.IDFA_SOFTPROMPT_AT_W2E_TIME, curTime);
							break;
					}

					// Increment view count
					int viewCount = prefs.GetInt(Prefs.IDFA_SOFTPROMPT_VIEW_COUNT, 0);
					viewCount++;
					prefs.SetInt(Prefs.IDFA_SOFTPROMPT_VIEW_COUNT, viewCount);

					prefs.Save();
				}
			}
			else if (shouldDisplayOnlyIDFAHardDialog(sp))
			{
				// Add hard prompt
				addiOSHardPromptTask(new iOSPromptTask(Dict.create(D.OPTION, sp, D.CALLBACK, onRequestFinish)));
			}
			else
			{
				// not doing anything, onRequestFinish callback
				if (onRequestFinish != null)
				{
					onRequestFinish();
				}
			}
#else
			// just simply move on
			if (onRequestFinish != null)
			{
				onRequestFinish();
			}
#endif
		}

		private static void addiOSHardPromptTask(iOSPromptTask task)
		{
			Scheduler.Scheduler.addTask(task, SchedulerPriority.PriorityType.BLOCKING);
		}

		private static bool shouldDisplayIDFADialog(SurfacePoint sp)
		{
			// Make sure we are working on the correct version of iOS and EOS is on
			if (!isIOSVersionValid() || !ExperimentWrapper.IDFASoftPrompt.isInExperiment || prefs == null)
			{
				return false;
			}

			// If iOSTracking consent value has been determined, it means the player has seen this hard prompt and made
			// decision, we can not see this hard prompt unless reinstall the game again. 
			iOSAppTracking.AuthorizationStatus consentValue = iOSAppTracking.GetTrackingPreference;
			if ( consentValue != iOSAppTracking.AuthorizationStatus.NOT_DETERMINED)
			{
				return false;
			}

			int curTimeInSeconds = GameTimer.currentTime;
			int lastViewTime = 0;
			switch (sp)
			{
				case SurfacePoint.GameEntry:
					// Need to make sure the EOS variable for showing is true
					if (!ExperimentWrapper.IDFASoftPrompt.showLocationEntry)
					{
						return false;
					}

					// Need to make sure the cooldown timm passed
					lastViewTime = prefs.GetInt(Prefs.IDFA_SOFTPROMPT_AT_ENTRY_TIME, 0);
					return (curTimeInSeconds - lastViewTime) >= ExperimentWrapper.IDFASoftPrompt.showEntryCoolDown;

				case SurfacePoint.W2E:
					// Need to make sure the EOS variable for showing is true
					if (!ExperimentWrapper.IDFASoftPrompt.showLocationW2E)
					{
						return false;
					}

					// Need to make sure the cooldown timm passed
					lastViewTime = prefs.GetInt(Prefs.IDFA_SOFTPROMPT_AT_W2E_TIME, 0);
					return (curTimeInSeconds - lastViewTime) >= ExperimentWrapper.IDFASoftPrompt.showW2ECoolDown;
			}

			return false;
		}

		private static bool shouldDisplayIDFASoftDialog(SurfacePoint sp)
		{
			if (!shouldDisplayIDFADialog(sp) || !ExperimentWrapper.IDFASoftPrompt.showSoftPrompt || prefs == null)
			{
				return false;
			}
			
			int viewCount = prefs.GetInt(Prefs.IDFA_SOFTPROMPT_VIEW_COUNT, 0);
			return (viewCount < ExperimentWrapper.IDFASoftPrompt.softPromptMaxViews);
		}
		
		// Hard prompt should be coupled with soft prompt unless soft prompt is turned off.
		private static bool shouldDisplayOnlyIDFAHardDialog(SurfacePoint sp)
		{
			if (!shouldDisplayIDFADialog(sp))
			{
				return false;
			}
			
			if (ExperimentWrapper.IDFASoftPrompt.showSoftPrompt)
			{
				return false;
			}

			return true;
		}
		
		//Function to check whether the ios version is valid for ZADE
		private static bool isIOSVersionValid()
		{

#if (UNITY_IPHONE || UNITY_IOS)
	#if UNITY_EDITOR
			return true;  //return true so we can test in editor
	#else
			int iosMajorVersion = 0;
			string versionString = UnityEngine.iOS.Device.systemVersion;
			string[] versionArray = versionString.Split('.');
			if ((versionArray.Length > 0) && int.TryParse(versionArray[0], out iosMajorVersion))
			{
				return iosMajorVersion >= 14;
			}

			return false;
	#endif
#else
			return false;
#endif
		}
	}
}


