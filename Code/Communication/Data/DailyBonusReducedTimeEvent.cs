using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DailyBonusReducedTimeEvent : IResetGame
{
	public static int reducedLength = 0;
    public static GameTimerRange timerRange;

	// this is for use by regular DailyBonusReducedTime event.  Note that this event
	// may not be in use by PathToRiches event though the DailyBonusReducedTime Experiment  is not enabled (that is for a different user event)
	public static void init()
	{
		// REDUCED_DAILY_BONUS_TIME_LENGTH seems to be unused?  probably because server sends 'seconds_left' daily bonus timer via player login info
	    //reducedLength = Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_LENGTH", 0);
	    timerRange = new GameTimerRange(
		    Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_START_DATE", 0),
			Data.liveData.getInt("REDUCED_DAILY_BONUS_TIME_END_DATE", 0),
			Data.liveData.getBool("REDUCED_DAILY_BONUS_TIME_ENABLED", false)
	    );
	}

	// this used by path-to-riches which has a reduced-cooldown-timer award
	public static void init(GameTimerRange newTimerRange)
	{
		timerRange = newTimerRange;
	}
	
	public static bool isActive
	{
		get
		{
			return
				ExperimentWrapper.ReducedDailyBonusEvent.isInExperiment &&
				(timerRange != null) &&
				timerRange.isActive;
		}
	}

	public static bool isActiveFromPowerup
	{
		get
		{
			return PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_DAILY_BONUS_KEY) &&
				!(ExperimentWrapper.ReducedDailyBonusEvent.isInExperiment &&
				timerRange != null &&
				timerRange.isActive);
		}
	}
	
	public static void resetStaticClassData()
	{
		//reducedLength = 0;
		timerRange = null;
	}	
}

