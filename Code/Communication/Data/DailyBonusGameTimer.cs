using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_WSA_10_0 && NETFX_CORE
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
#endif

/*
A version of the GameTimer that is specifically for the daily bonus.
*/

public class DailyBonusGameTimer : GameTimer
{
//	private static bool enableVIPCheck = false;		// Is the user going to have vip bonus points

	public JSON[] rewards = null;		// Stores the "rewards" block of json from the timer, since it can have various types of rewards.
	
//	private int lastDay = 1;					// What day of the daily bonus has been last collected.
	private bool isWaitingForNewTime;			// State for when the action to claim credits has been called but response has not been recieved.
	private GameTimer nextProgressionTimer = null;
	private GameTimer resetProgressionTimer = null;
    public string dateLastCollected = null;

	public static DailyBonusGameTimer instance = null;

	public DailyBonusGameTimer(JSON data) : base(data.getInt("seconds_left", 0))
	{
//		startTimer(data.getInt("seconds_left", 0));
		instance = this;
		SlotsPlayer.instance.dailyBonusDuration = data.getInt("duration_minutes", 120);
		refreshData(data);
	}

	public static void refreshData(JSON data)
	{
		if (data == null || instance == null)
		{
			Debug.LogWarning("Invalid bonus game timer data");
			return;
		}
		if (data.hasKey("rewards"))
		{
			instance.rewards = data.getJsonArray("rewards");
		}

		instance.dateLastCollected = data.getString("last_collection_at", "none");
		instance._day = data.getInt("current_payout_number", 0);
		
		int seconds = data.getInt("next_progression_in", 0);
		if (seconds > 0)
		{
			// Create a timer to know when the daily bonus day increments.
			instance.nextProgressionTimer = new GameTimer(seconds);
		}

		seconds = data.getInt("reset_progression_in", 0);
		if (seconds > 0)
		{
			// Create a timer to know when the daily bonus resets back to day 1 if a bonus game isn't played first.
			instance.resetProgressionTimer = new GameTimer(seconds);
		}
	}

	// Called when claim action has been initiated but update event has yet to be called.
	public static void markTimerActionCalled()
	{
		if (SlotsPlayer.instance == null || SlotsPlayer.instance.dailyBonusTimer == null)
		{
			return;
		}
		SlotsPlayer.instance.dailyBonusTimer.isWaitingForNewTime = true;
	}
	
	// Called on creation as well as from an event through static updateTimer()
	public override void startTimer(float timeSeconds)
	{
		base.startTimer(timeSeconds);
		isWaitingForNewTime = false;

		if (DailyBonusButton.instance != null)
		{
			DailyBonusButton.instance.resetTimer();
		}
#if UNITY_WSA_10_0 && NETFX_CORE
		scheduleWin10ToastNotification(timeSeconds);
#endif
	}

	// Cancels all existing scheduled toasts, then schedules a new one
	void scheduleWin10ToastNotification(float timeSeconds)
	{
#if UNITY_WSA_10_0 && NETFX_CORE
		
		var notifier = ToastNotificationManager.CreateToastNotifier();
		var scheduledToasts = notifier.GetScheduledToastNotifications();
		if (scheduledToasts != null)
			foreach (var t in scheduledToasts)
				notifier.RemoveFromSchedule(t);

		if (timeSeconds > 0)
		{
			Debug.Log("DailyBonusGameTimer::startTimer() - scheduling Windows 10 toast to appear in " + timeSeconds + " seconds");

			ToastContent toastContent = new ToastContent()
			{
				Visual = new ToastVisual()
				{
					BindingGeneric = new ToastBindingGeneric()
					{
						Children =
						{
							new AdaptiveText() { Text = "Hit it Rich!" },
							new AdaptiveText() { Text = "Your FREE bonus coins are ready!" }
						}
					}
				},

				Actions = new ToastActionsCustom()
				{
					Buttons =
					{
						new ToastButton("Collect free coins!", "collectDailyBonus"),
						new ToastButtonDismiss("Not Now")
					}
				}
			};

			notifier.AddToSchedule(new ScheduledToastNotification(toastContent.GetXml(), DateTimeOffset.Now.AddSeconds(timeSeconds)));
		}

#endif
	}

	// Is the timer expired and ready to do something?
	public override bool isExpired
	{
		get { return !isWaitingForNewTime && timeRemaining <= 0; }
	}
	
	// Returns the current day for daily bonus games.
	public int day	//dailyBonusDay
	{
		get
		{
			// First make sure the day progression hasn't reset from not playing for a full day.
			if (resetProgressionTimer != null && resetProgressionTimer.isExpired)
			{
				// Reset to day 1.
//				enableVIPCheck = true;
//				lastDay = 1;
				_day = 1;
			}
			else
			{
				// Next see if the day has incremented.
				if (nextProgressionTimer != null && nextProgressionTimer.isExpired)
				{
//					enableVIPCheck = true;
					_day++;
//					lastDay = _day;
				}
			}
		
			return _day;
		}
		
		set
		{
			// This is only called by the dev panel when setting the daily bonus day,
			// so we fake the next and reset progression values, assuming we won't want them to progress while testing a day.
			_day = value;
//			lastDay = 0;
			nextProgressionTimer = new GameTimer(99999999);
			resetProgressionTimer = new GameTimer(99999999);
		}
	}
	private int _day = 1;

	public long resetProgressionTimerTimeRemaining
	{
		get { return resetProgressionTimer.timeRemaining; }
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		instance = null;
	}

}
