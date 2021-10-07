using UnityEngine;
using System.Collections;

/// <summary>
/// A continuous idle check timer. Pops a dialog when the timer expires due to inactivity
/// </summary>
public class IdleWatch : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	private static SmartTimer idleTimer;

	// =============================
	// CONST
	// =============================
	private const int TIMER_DELAY = 600;
	private const string ENABLE_TIMER_KEY = "ENABLE_MOBILE_IDLE_TIMER";
	private const string TIMER_ACTION_KEY = "MOBILE_IDLE_TIMER_ACTION";

	void Awake()
	{
		// attempt initialization
		init();
		DontDestroyOnLoad(gameObject);
	}

	void Update()
	{
		// if we haven't initialized the timer, do that now
		if (idleTimer == null && Data.liveData != null)
		{
			init();
		}
		else if (Input.anyKey)
		{
			onInputDetected();
		}
	}

	private static void init()
	{
		if (Data.liveData != null && Data.liveData.getBool(ENABLE_TIMER_KEY, true))
		{
			idleTimer = new SmartTimer(TIMER_DELAY, false, onTimeExpired, "idle_timer");
			idleTimer.start();
		}
	}

	/// <summary>
	/// Callback for when the timer expires
	/// </summary>
	private static void onTimeExpired()
	{
		if (Data.liveData != null && !string.IsNullOrEmpty(Data.liveData.getString(TIMER_ACTION_KEY, "")))
		{
			DoSomething.now(Data.liveData.getString(TIMER_ACTION_KEY, ""));
		}
		else
		{
			InboxDialog.showDialog();
		}
	}

	/// <summary>
	/// Handles resetting the idle timer when user input has happened
	/// </summary>
	public static void onInputDetected()
	{
		if (idleTimer != null)
		{
			idleTimer.reset();
		}
	}

	/// <summary>
	/// Disables the timer, so we don't trigger the callback.
	/// Use in conditions where you don't want the timer running.
	/// </summary>
	public static void disable()
	{
		if (idleTimer != null)
		{
			idleTimer.stop();
		}
	}

	/// <summary>
	/// Enables the timer. Call after disabling to resume the timer.
	/// </summary>
	public static void enable()
	{
		if (idleTimer != null)
		{
			idleTimer.start();
		}
	}
}