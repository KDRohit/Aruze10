using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
A wrapper to help with things that have start and end dates,
for convenience on knowing whether we're currently in that range
without nullchecking all over the damned place.

NOTE : if startimes are 0 the range will not be considered active and isActive will return false
if you want to set up a active range from the beginning of time, use 1 instead of 0
*/

public class GameTimerRange : IResetGame
{
	public GameTimer endTimer { get; private set; }
	public System.DateTime startDate { get; private set; }	// Some things might need this.
	public System.DateTime endDate { get; private set; }	// Some things might need this.
	public int startTimestamp { get; private set; }			// Some things might need this.
	public int endTimestamp { get; private set; }			// Some things might need this.
	public enum TimeFormat {END_DATE, START_DATE, REMAINING, REMAINING_HMS_FORMAT};
	public delegate void onExpireDelegate(Dict args = null, GameTimerRange originalTimer = null);
	public event onExpireDelegate timerExpiredEvent;

	public delegate void onStartedDelegate();
	public onStartedDelegate timerStarted;

	// Event handling stuff
	// TODO: this label-update stuff probably belongs in GameTimer, not GameTimerRange, because it is associated with only 1 timer
	public static List<GameTimerRange> updateList = new List<GameTimerRange>();
	private List<LabelWrapper> labelUpdateList;
	private List<string> baseTextUpdateList;
	private List<string> richTextColorList;

	private TimeFormat format = TimeFormat.REMAINING;
	private Dict arguments;
	private static GameTimer timerThrottle;
	public GameTimer startTimer = null;
	public GameTimerRange mainTimer = null;
	public bool hasStarted = false;

	private List<GameTimerRange> subTimers = new List<GameTimerRange>(); //Need to keep track of these for pausing/unpausing

	public enum State { Before, During, After, Unknown };

	public bool isActive
	{
		get
		{
			return
				startTimer != null &&
				startTimer.isExpired &&
				endTimer != null &&
				!endTimer.isExpired;
		}
	}

	public State getState()
	{
		if (startTimer == null || endTimer == null)
		{
			return State.Unknown;
		}
		if (endTimer.isExpired)
		{
			return State.After;
		}
		if (startTimer.isExpired)
		{
			return State.During;
		}
		return State.Before;
	}

	// sometimes you just care if you are past the endTime, but not if you are before the startTime
	public bool isExpired
	{
		get
		{
			if (endTimer == null)
				return true;
			return endTimer.isExpired;
		}
	}
	
	public int timeRemaining
	{
		get
		{
			if (endTimer == null)
			{
				return 0;
			}
			return endTimer.timeRemaining;
		}
	}
	
	public string timeRemainingFormatted
	{
		get
		{
			if (endTimer == null)
			{
				return "";
			}
			return endTimer.timeRemainingFormatted;
		}
	}
	
	//Formatted time with h:m:s format (eg. 2h:3m:30s)
	public string timeRemainingFormattedHMS
	{
		get
		{
			if (endTimer == null)
			{
				return "";
			}
			return endTimer.timeRemainingFormattedHMS;
		}
	}
	
	public string timeRemainingFormattedMS(float space)
	{
		if (endTimer == null)
		{
			return "";
		}
	
		return endTimer.timeRemainingFormattedMS(space);
	}
	
	public string startDateFormatted
	{
		get { return CommonText.formatDateTime(startDate); }
	}

	public string startDateFormattedLocalTime
	{
		get { return CommonText.formatDateTime(startDate.ToLocalTime()); }
	}

	public string endDateFormatted
	{
		get { return CommonText.formatDateTime(endDate); }
	}

	public string endDateFormattedLocalTime
	{
		get { return CommonText.formatDateTime(endDate.ToLocalTime());  }
	}
	
	public GameTimerRange(int startTimestamp, int endTimestamp)
	{
		init(startTimestamp, endTimestamp);
	}

	public GameTimerRange(int startTimestamp, int endTimestamp, bool isRangeEnabled, onStartedDelegate callback = null)
	{
		if (isRangeEnabled)
		{
			// Convenient way to do this without external conditionals,
			// when we need the GameTimerRange object but not the timers within.
			init(startTimestamp, endTimestamp);
		}

		hasStarted = startTimer == null || startTimer.isExpired;

		if (!hasStarted && callback != null)
		{
			timerStarted = callback;
			updateList.Add(this);
		}
	}
	
	// Factory method to create a simple range based on time remaining in the end date.
	public static GameTimerRange createWithTimeRemaining(int secondsRemaining)
	{
		// we pass in 1 instead of 0 since 0 is considered an invalid start time
		return new GameTimerRange(1, GameTimer.currentTime + secondsRemaining);
	}
	
	private void init(int startTimestamp, int endTimestamp)
	{
		startTimers(startTimestamp, endTimestamp);
	}

	// Used to control registered labels and events.
	public static void update()
	{
		if (Glb.isResetting)
		{
			// Lets not do anything if we are resetting the game.
			return;
		}
		
		if (timerThrottle == null)
		{
			timerThrottle = new GameTimer(1);
		}

		if (!timerThrottle.isExpired)
		{
			return;
		}
		
		for (int i = updateList.Count - 1; i >= 0; i--)
		{
			GameTimerRange timerRange = updateList[i];

			if (timerRange.labelUpdateList != null)
			{
				timerRange.updateText();
			}

			if (timerRange.isExpired)
			{
				timerRange.callEvent();
				timerRange.clearEvent();
				updateList.RemoveAt(i);
			}
			else if (timerRange.timerStarted != null && timerRange.isActive && !timerRange.hasStarted)
			{
				timerRange.hasStarted = true;
				timerRange.timerStarted();
			}
		}

		timerThrottle.startTimer(1);
	}

	private System.Text.StringBuilder textStringBuild;   // use StringBuilder to reduce string allocations

	private void updateText()
	{
		for (int i = labelUpdateList.Count - 1; i >= 0; i--) 
		{
			if (labelUpdateList[i] != null)
			{
				// If the string has a color, make sure we use it.
				string colorCloseTagText = (richTextColorList[i] != "") ? "</color>" : "";
				string timeText;

				switch (format)
				{
					// We don't format START_DATE and END_DATE becaue they should always be static. 
					// If we add more timeformats that actually count up/down, just make sure to handle them here. 
					case TimeFormat.REMAINING:
						timeText = timeRemainingFormatted;
						break;
					case TimeFormat.REMAINING_HMS_FORMAT:
						timeText = timeRemainingFormattedHMS;
						break;
					default:
						timeText = "";
						break;	
				}

				textStringBuild.Length = 0;
				textStringBuild.Append(richTextColorList[i]);
				textStringBuild.Append(baseTextUpdateList[i]);
				textStringBuild.Append(timeText);
				textStringBuild.Append(colorCloseTagText);

				labelUpdateList[i].text = textStringBuild.ToString();
			}
			else
			{
				labelUpdateList.RemoveAt(i);
				baseTextUpdateList.RemoveAt(i);
				richTextColorList.RemoveAt(i);
			}
		}
	}

	public void updateEndTime(int newEndTime)
	{
		if (!startTimer.isPaused)
		{
			startTimers(1, GameTimer.currentTime + newEndTime);
		}
		else
		{
			startTimers(1, GameTimer.currentTime + newEndTime);
			pauseTimers();
		}
	}

	public void callEvent()
	{
		// Null checked because an empty invocation list means the event is null. Lets avoid that.
		if (timerExpiredEvent != null)
		{
			timerExpiredEvent(arguments, effectiveMainTimer);
		}
	}

	public void clearEvent()
	{
		timerExpiredEvent = null;
	}

	public void clearLabels()
	{
		if (labelUpdateList != null)
		{
			labelUpdateList.Clear();
			baseTextUpdateList.Clear();
			richTextColorList.Clear();
		}
	}

	public void removeLabel(LabelWrapper objectToRemove)
	{
		if (labelUpdateList == null || objectToRemove == null)
		{
			return;
		}

		int index = labelUpdateList.IndexOf(objectToRemove);

		if (index != -1)
		{
			labelUpdateList.RemoveAt(index);
			baseTextUpdateList.RemoveAt(index);
			richTextColorList.RemoveAt(index);
		}
	}
	public void removeLabel(TextMeshPro objectToRemove)
	{
		removeLabel(new LabelWrapper(objectToRemove));
	}

	// Normally, we'd want to check if the timer is expired, but technically, an expired
	// value may still be valid (having a text object read 00:00 or something seems ok)
	public void registerLabel(LabelWrapper label, TimeFormat format = TimeFormat.REMAINING, bool keepCurrentText = false, string richTextColor = "")
	{
		if (labelUpdateList == null) // lazy-allocate these
		{
			labelUpdateList = new List<LabelWrapper>();
			baseTextUpdateList = new List<string>();
			richTextColorList = new List<string>();
		}

		// Decide whether or not we need the base text
		label.text = keepCurrentText ? label.text : "";

		if (!updateList.Contains(this))
		{
			updateList.Add(this);
		}

		// If the the text isn't already being tracked
		if (!labelUpdateList.Contains(label) && format != TimeFormat.END_DATE && format != TimeFormat.START_DATE)
		{
			labelUpdateList.Add(label);

			// Always add the base text and color to a list, even if it's blank. We handle it as needed in the update loop.
			baseTextUpdateList.Add(label.text);
			richTextColorList.Add(richTextColor);
		}

		// set the label text immediately.  without this, the label isnt updated for up to 1 second.
		// some code duplication here with updateText(), hard to avoid w/o incurring per-update fn-call cost
		if (textStringBuild == null)
		{
			textStringBuild = new System.Text.StringBuilder();
		}

		// If the string has a color, make sure we use it.
		string colorCloseTagText = (richTextColor != "") ? "</color>" : "";
		string timeText;
		this.format = format;

		switch (format)
		{
			case TimeFormat.END_DATE:
				timeText = endDateFormatted;
				break;
			case TimeFormat.START_DATE:
				timeText = startDateFormatted; 
				break;
			case TimeFormat.REMAINING:
				timeText = timeRemainingFormatted;
				break;
			case TimeFormat.REMAINING_HMS_FORMAT:
				timeText = timeRemainingFormattedHMS;
				break;
			default:
				timeText = "";
				break;	
		}

		textStringBuild.Length = 0;
		textStringBuild.Append(richTextColor);
		textStringBuild.Append(label.text);
		textStringBuild.Append(timeText);
		textStringBuild.Append(colorCloseTagText);

		label.text = textStringBuild.ToString();
	}
	
	public void registerLabel(TextMeshPro label, TimeFormat format = TimeFormat.REMAINING, bool keepCurrentText = false, string richTextColor = "")
	{
		registerLabel(new LabelWrapper(label), format, keepCurrentText, richTextColor);
	}
	
	// Can be used to just add a function
	public void registerFunction(onExpireDelegate function, Dict args = null, int triggerTime = 0)
	{
		int timeRemaining = this.timeRemaining;

		// Adding a function to an expired timer will just call the function on the first update loop.
		if (isExpired)
		{
			return;
		}

		if (!updateList.Contains(this) && triggerTime == 0)
		{
			updateList.Add(this);
		}
		else if (triggerTime > 0)
		{
			int subTimerTime = timeRemaining - triggerTime;
			if (subTimerTime > 0)
			{
				// make our sub-timer.
				GameTimerRange timerRange = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + subTimerTime);
				timerRange.mainTimer = effectiveMainTimer;
				timerRange.registerFunction(function, args);
				subTimers.Add(timerRange);
				return;
			}
		}
			
		// This is free, and prevents the function from being added twice.
		timerExpiredEvent -= function;
		timerExpiredEvent += function;

		// Note: by using 1 Dict for all arguments for all callbacks, callbacks have access to parameters to be passed to other registered functions, 
		//       conflicts with different callbacks using same parameter keyname are a possibility, and when you unregister a function,
		//       it doesnt unregister the parameter in the dictionary, the parameter will sit there forever until GameTimerRange is deleted, which
		//       may be bad if parameter is reference to a large object that would otherwise be deleted.
		//       Probably would be better to keep a dictionary of callback delegate=>parameter associated with that delegate.

		if (args != null)
		{
			// Arguments is null by default, since not all timer ranges will need it.
			if (arguments == null)
			{
				arguments = Dict.create();
			}

			// Add the function
			if (!arguments.merge(args))
			{
				Debug.LogWarning("GameTimerRange::registerFunction - Couldn't merge args");
			}
		}
	}

	public void removeFunction(onExpireDelegate function)
	{
		timerExpiredEvent -= function;
		for (int i = 0; i < subTimers.Count; i++)
		{
			subTimers[i].timerExpiredEvent -= function;
		}
	}

	public void clearSubtimers()
	{
		subTimers.Clear();
	}

	public void startTimers(int startTimestamp, int endTimestamp)
	{
		if (startTimestamp > endTimestamp)
		{
			// Don't log anything here. It's common and ok for this to happen.
			return;
		}

		this.startTimestamp = startTimestamp;
		this.endTimestamp = endTimestamp;

		if (startTimestamp > 0)
		{
			if (startTimer == null)
			{
				startTimer = GameTimer.createWithEndDateTimestamp(startTimestamp);
			}
			else
			{
				startTimer.startTimer(startTimestamp - GameTimer.currentTime);
			}
			startDate = Common.convertFromUnixTimestampSeconds(startTimestamp);
		}
		else
		{
			startTimer = null;
		}
		
		if (endTimestamp > 0)
		{
			if (endTimer == null)
			{
				endTimer = GameTimer.createWithEndDateTimestamp(endTimestamp);
			}
			else
			{
				endTimer.startTimer(endTimestamp - GameTimer.currentTime);
			}
			endDate = Common.convertFromUnixTimestampSeconds(endTimestamp);
		}
		else
		{
			endTimer = null;
		}
	}
	
	// Apparently there might be problems with using the syntax: "mainTimer ?? this"
	// so I created this simple getter to use in multiple places.
	private GameTimerRange effectiveMainTimer
	{
		get
		{
			if (mainTimer != null)
			{
				return mainTimer;
			}
			return this;
		}
	}

	public void pauseTimers()
	{
		for (int i = subTimers.Count - 1; i >= 0; i--)
		{
			GameTimerRange timerRange = subTimers[i];
			timerRange.startTimer.pauseTimer();
			timerRange.endTimer.pauseTimer();
		}
		startTimer.pauseTimer();
		endTimer.pauseTimer();
	}

	public void unPauseTimers()
	{
		for (int i = subTimers.Count - 1; i >= 0; i--)
		{
			GameTimerRange timerRange = subTimers[i];
			timerRange.startTimer.unpauseTimer();
			timerRange.endTimer.unpauseTimer();
		}
		startTimer.unpauseTimer();
		endTimer.unpauseTimer();
	}

	public bool isEventRegisteredOnActiveTimer(onExpireDelegate function)
	{
		if (!isExpired && timerExpiredEvent == function)
		{
			return true;
		}

		for (int i = 0; i < subTimers.Count; i++)
		{
			if (!subTimers[i].isExpired && subTimers[i].timerExpiredEvent == function)
			{
				return true;
			}
		}
		return false;
	}

	public static void resetStaticClassData()
	{
		// Clear all update lists.
		updateList.Clear();
	}
}
