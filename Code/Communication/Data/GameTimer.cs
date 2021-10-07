using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
THIS CLASS HOLDS DATA FROM PLAYER DATA, NOT GLOBAL!
GameTimer class to handle the timers from the server. e.g. bonus credits "bonus"
Alternatively we could construct the populate all from static data "timers" then update them with player data.
*/
public class GameTimer : IResetGame
{
	private float startTime;			// Time used for starting time.
	private float timeSeconds;			// Duration of timer.

	private float pausedTime;
	private float pausedSecondsLeft;

	public bool isPaused  { get; private set; }
	
	// Variables used for determining currentTime.
	public static int sessionStartTime { get; private set; }
	private static float ssssAtSessionStart = 0.0f;

	public static int serverEpochTime { get; private set; } //Time sent by server representing when server sent the event to the client
	private static System.DateTime clientTimeOffset; //set each time we update serverEpochTime. Time that client actually processed the server event

	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// Returns the "System Seconds Since Startup", which works in actual realtime while the app is suspended.
	public static float SSSS
	{
		get
		{
			init();	// Just in case it hasn't been initialized before calling this.

			// Time calculation uses System.TimeSpans instead of Time.realtimeSinceStartup because timers are getting desynced on device
			// due to closing and resuming the app. Unity was not taking the time in between into account. Confirmed with Jon that this is
			// the desired solution, and even though they can manipulate the system time, they would crash if they attempted to...so its ok. I suppose.
			System.TimeSpan timeDuration = System.DateTime.UtcNow.Subtract(_systemStartTime);
			return (float)timeDuration.TotalSeconds;
		}
	}
	private static System.DateTime _systemStartTime;
	
	// current UTC in seconds since Jan 1st, 1970.
	// Calculated by taking the last known server time, the time the client processed the server data
	// and adding the amount of seconds that has passed since the session started.
	public static int currentTime
	{
		get
		{
			System.TimeSpan timeSinceServerUpdate = System.DateTime.UtcNow.Subtract(clientTimeOffset);
			return serverEpochTime + (int) timeSinceServerUpdate.TotalSeconds;
		}
	}

	public static void updateServerEpochTime(JSON data)
	{
		serverEpochTime = data.getInt("server_epoch_time", serverEpochTime);
		clientTimeOffset = System.DateTime.UtcNow;
	}


	public void pauseTimer()
	{
		pausedSecondsLeft = timeRemaining; //Seconds left at the time of pausing
		pausedTime = GameTimer.SSSS - startTime;
		isPaused = true;
	}

	public void unpauseTimer()
	{
		startTime = GameTimer.SSSS; //Change the start time to the current time
		timeSeconds = pausedSecondsLeft; //Seconds left is now what we were at before pausing
		isPaused = false;
	}
	
	public static int convertDateTimeStringToSecondsFromNow(string dateTimeString)
	{
		if (sessionStartTime == 0)
		{
			Debug.LogError("GameTimer.convertDateTimeStringToSecondsFromNow() was called before GameTimer.startSession(). Results will be inaccurate.");
		}
		
		System.DateTime dateTime;

		if (System.DateTime.TryParse(dateTimeString, out dateTime))
		{
			int endTime = Common.convertToUnixTimestampSeconds(dateTime);
			int seconds = endTime - currentTime;
			return seconds;
		}
		
		Debug.LogErrorFormat("GameTimer.convertDateTimeStringToSecondsFromNow() failed with input string '{0}'.", dateTimeString);
		return -1;
	}
	
	/// Initialize the "System Seconds Since Startup" variable at startup.
	public static void init()
	{
		if (_systemStartTime == System.DateTime.MinValue)
		{
			_systemStartTime = System.DateTime.UtcNow;
		}
	}
	
	// Called each time a session is started, so we know what time it started.
	public static void startSession(int startTime)
	{
		if (startTime == 0)
		{
			// If the server didn't provide a time, then we need to fallback to the device's local time and hope it's accurate.
			startTime = Common.utcTimeInSeconds();
		}
		sessionStartTime = startTime;
		ssssAtSessionStart = SSSS;
		serverEpochTime = sessionStartTime;
		clientTimeOffset = System.DateTime.UtcNow;
	}
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// Factory method to create an object by passing in the end datetime string.
	public static GameTimer createWithEndDateString(string dateTimeString)
	{
		return new GameTimer(convertDateTimeStringToSecondsFromNow(dateTimeString));
	}

	// Factory method to create an object by passing in the end date as a UTC timestamp. (LiveData uses this)
	public static GameTimer createWithEndDateTimestamp(int endDate, bool useHMSFormatting = false)
	{
		if (sessionStartTime == 0)
		{
			Debug.LogError("GameTimer.createWithEndDateTimestamp() was called before GameTimer.startSession(). Results will be inaccurate.");
		}
		
		return new GameTimer(endDate - currentTime);
	}

	/// Constructor for a simple timer whose reference is owned by the creator rather than in the static list by name.
	public GameTimer(int seconds) 
	{
		startTimer((float)seconds);
	}

	/// Constructor for a simple timer whose reference is owned by the creator rather than in the static list by name.
	public GameTimer(float seconds) 
	{
		startTimer(seconds);
	}
		
	// Called on creation as well as from an event through static updateTimer()
	public virtual void startTimer(float timeSeconds)
	{
		startTime = GameTimer.SSSS;
		this.timeSeconds = timeSeconds;
		isPaused = false;
	}
	
	/// Returns the amount of time (in seconds) remaining on the timer before it expires.
	public int timeRemaining
	{
		get
		{
			float timeDelta = 0;
			if (!isPaused)
			{
				timeDelta = GameTimer.SSSS - startTime;
			}
			else
			{
				timeDelta = pausedTime; //If we're paused than lets calulate the time we're at based on when we paused the timer
			}
			float timeLeft = (timeSeconds - timeDelta);
			return ((timeLeft < 0) ? 0 : Mathf.CeilToInt(timeLeft));
		}
	}
	
	public string timeRemainingFormatted
	{
		get
		{
			int t = Mathf.CeilToInt(timeRemaining);
			if (cachedFormattedString != null && cachedFormattedTime == t)
			{
				// Caching dramatically cuts down on CPU time consumed when the game
				// is operating at a high framerate for this specific operation.
				return cachedFormattedString;
			}
			else
			{
				cachedFormattedString = CommonText.secondsFormatted(t);
				cachedFormattedTime = t;
				return cachedFormattedString;
			}
		}
	}
	private string cachedFormattedString = null;
	private int cachedFormattedTime = 0;

	// Returns the time remaining with with a monospace tag around it. (Should only be used for TextMeshPro labels)
	public string timeRemainingFormattedMS(float spacing)
	{
		int t = Mathf.CeilToInt(timeRemaining);
		if (cachedFormattedMSString != null && cachedFormattedMSTime == t)
		{
			// Caching dramatically cuts down on CPU time consumed when the game
			// is operating at a high framerate for this specific operation.
			return cachedFormattedMSString;
		}
		else
		{
			cachedFormattedMSString = CommonText.secondsFormattedMS(t, spacing);
			cachedFormattedMSTime = t;
			return cachedFormattedString;
		}
	}
	private string cachedFormattedMSString = null;
	private int cachedFormattedMSTime = 0;
	
	public string timeRemainingFormattedHMS
	{
		get
		{
			int t = Mathf.CeilToInt(timeRemaining);
			if (cachedFormattedHMSString != null && cachedFormattedHMSTime == t)
			{
				// Caching dramatically cuts down on CPU time consumed when the game
				// is operating at a high framerate for this specific operation.
				return cachedFormattedHMSString;
			}
			else
			{
				cachedFormattedHMSString = CommonText.secondsFormatted(t, true);
				cachedFormattedHMSTime = t;
				return cachedFormattedHMSString;
			}
		}
	}
	private string cachedFormattedHMSString = null;
	private int cachedFormattedHMSTime = 0;
			
	/// Is the timer expired and ready to do something?
	public virtual bool isExpired
	{
		get { return timeRemaining <= 0; }
	}
	
	public static void resetStaticClassData()
	{
		sessionStartTime = 0;
		ssssAtSessionStart = 0.0f;
		serverEpochTime = 0;
		clientTimeOffset = System.DateTime.Now;
	}
}
