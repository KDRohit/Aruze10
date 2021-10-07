
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class LongestTimer
{
	public const string unknownSource = "UNKNOWN";
	private class  TimerData
	{
		public GameTimerRange range { get; private set; }
		public int source { get; private set; }
		public TimerData(int startTime, int endTime, string id, bool isEnabled,
			GameTimerRange.onExpireDelegate callbackFunc)
		{
			range = new GameTimerRange(startTime, endTime, isEnabled);
			if (callbackFunc != null)
			{
				range.registerFunction(callbackFunc);
			}

			source = id.GetHashCode();
		}
	}

	private List<TimerData> unlockTimes;

	private bool cachedIsActiveFlag;


	
	public delegate void onDisableDelegate();
	public onDisableDelegate disableDelegate;
	
	public delegate void onEnableDelegate();
	public onEnableDelegate enableDelegate;
	
	public delegate void onTimerStartDelegate(Dict args = null, GameTimerRange parent = null);
	public onTimerStartDelegate timerStartDelegate;
	
	public delegate void onTimerExpireDelegate(Dict args = null, GameTimerRange parent = null);
	public onTimerExpireDelegate timerExpireDelegate;
	
	public GameTimerRange combinedActiveTimeRange
	{
		get
		{
			if (unlockTimes != null && unlockTimes.Count > 0)
			{
				int startTime = -1;
				int endTime = -1;
				for (int i = 0; i < unlockTimes.Count; i++)
				{
					if (unlockTimes[i].range.isActive)
					{
						//this timer is active
						if (unlockTimes[i].range.startTimestamp < startTime)
						{
							startTime = unlockTimes[i].range.startTimestamp;
						}

						if (unlockTimes[i].range.endTimestamp > endTime)
						{
							endTime = unlockTimes[i].range.endTimestamp;
						}
					}
				}

				//if no active ranges, return null
				if (endTime < GameTimer.currentTime)
				{
					return null;
				}

				if (startTime < GameTimer.currentTime)
				{
					startTime = GameTimer.currentTime;
				}

				return new GameTimerRange(startTime, endTime);
			}

			return null;
		}
	}
	
	
	public LongestTimer()
	{
		cachedIsActiveFlag = false;
	}

	public void addTimeRangeDuration(int timeRemaining, string source)
	{
		if (timeRemaining > 0)
		{
			int cTime = GameTimer.currentTime;
			addTimeRange(cTime, cTime + timeRemaining, source, true);
		}
	}

	public void addTimeRange(int startTime, int endTime, string source, bool isTimerEnabled = true)
	{
		//ignore times in the past
		if (endTime < GameTimer.currentTime)
		{
			return;
		}
		

		if (unlockTimes == null)
		{
			unlockTimes = new List<TimerData>();
		}

		TimerData newTimer = new TimerData(startTime, endTime, source, isTimerEnabled, onTimerExpire);
		unlockTimes.Add(newTimer);

		if (startTime <= GameTimer.currentTime && endTime >= GameTimer.currentTime)
		{
			onTimerStart(null, newTimer.range);
		}
		else if (startTime > GameTimer.currentTime && endTime > startTime)
		{
			//add callback when this timer starts to call OnEnabled if required
			int timeToEnd = newTimer.range.timeRemaining;
			int timeToStart = newTimer.range.startTimestamp;
			int triggerTime = timeToEnd - timeToStart;
			newTimer.range.registerFunction(onTimerStart, null, triggerTime);
		}
	}

	protected void OnEnabled()
	{
		cachedIsActiveFlag = true;
		if (enableDelegate != null)
		{
			enableDelegate();
		}
	}

	protected void OnDisabled()
	{
		cachedIsActiveFlag = false;
		if (disableDelegate != null)
		{
			disableDelegate();
		}
	}

	public void stopTimer(string source)
	{
		if (unlockTimes == null)
		{
			return;
		}

		List<TimerData> toRemove = new List<TimerData>();
		for (int i = 0; i < unlockTimes.Count; i++)
		{

			if (unlockTimes[i].source == source.GetHashCode())
			{
				unlockTimes[i].range.pauseTimers();
				toRemove.Add(unlockTimes[i]);
			}
		}

		for (int i = 0; i < toRemove.Count; i++)
		{
			unlockTimes.Remove(toRemove[i]);
		}
	}

	public bool isActive
	{
		get
		{
			return unlockTimes != null && unlockTimes.Count >0 ? true : false;
		}
	}
	

	public bool isEnabled
	{
		get
		{
			if (unlockTimes != null && unlockTimes.Count > 0)
			{
				for (int i = 0; i < unlockTimes.Count; i++)
				{
					if (unlockTimes[i] == null)
					{
						continue;
					}

					if (unlockTimes[i].range.isActive)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	private void onTimerExpire(Dict args = null, GameTimerRange parent = null)
	{
		//check if we're still enabled
		if (cachedIsActiveFlag && !isEnabled)
		{
			OnDisabled();
		}

		if (timerExpireDelegate != null)
		{
			timerExpireDelegate();
		}

	
	}

	private void onTimerStart(Dict args = null, GameTimerRange parent = null)
	{
		if (!cachedIsActiveFlag)
		{
			OnEnabled();
		}

		if (timerStartDelegate != null)
		{
			timerStartDelegate();
		}

	}
	
}

