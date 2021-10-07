using System.Collections.Generic;
using UnityEngine;

public class UnlockAllGamesFeature: FeatureBase, IResetGame
{
	public enum Source
	{
		LiveData,
		Powerup,
		Unknown
	}
	
	private bool cachedIsActiveFlag;

	private LongestTimer unlockTimer = new LongestTimer();
	private static UnlockAllGamesFeature _instance = null;
	//static instance
	public static UnlockAllGamesFeature instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new UnlockAllGamesFeature();
			}

			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public GameTimerRange unlockAllGamesTimer
	{
		get 
		{ 
			return unlockTimer.combinedActiveTimeRange;
		}
	}

	private string getStringFromSource(Source source)
	{
		string sourceStr = LongestTimer.unknownSource;
		switch (source)
		{
			case Source.Powerup:
				sourceStr = "PowerUp";
				break;
			case Source.LiveData:
				sourceStr = "LiveData";
				break;
		}
		return sourceStr;
	}

	public void addTimeRange(int startTime, int endTime,Source source = Source.Unknown, bool isTimerEnabled = true)
	{
		unlockTimer.addTimeRange(startTime,endTime, getStringFromSource(source));
	}
	
	protected override void OnEnabled()
	{
		cachedIsActiveFlag = true;
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		cachedIsActiveFlag = false;
		base.OnDisabled();
		UnlockAllGamesMotd.showDialog("", true);
	}
	
	public override void disableFeature()
	{
		//not supported
		Debug.LogError("This is not supported");
	}

	public override void reenableFeature()
	{
		Debug.LogError("This is not supported");
	}

	public void stopTimer(Source source = Source.Unknown)
	{
		
		unlockTimer.stopTimer(getStringFromSource(source));
	}

	public override bool isEnabled
	{
		get
		{
			return unlockTimer.isEnabled;
		}
	}

	private UnlockAllGamesFeature()
	{
		unlockTimer.timerExpireDelegate = onTimerExpire;
		unlockTimer.timerStartDelegate = onTimerStart;
	}

	protected override void initializeWithData(JSON data)
	{
		cachedIsActiveFlag = false;
	}

	private void onTimerExpire(Dict args = null, GameTimerRange parent = null)
	{
		//check if we're still enabled
		if (cachedIsActiveFlag && !isEnabled)
		{
			OnDisabled();
		}
	}

	private void onTimerStart(Dict args = null, GameTimerRange parent = null)
	{
		if (!cachedIsActiveFlag)
		{
			OnEnabled();
		}
		UnlockAllGamesMotd.showDialog("");

	}

	public static void resetStaticClassData()
	{
		instance = null;
	}

}
