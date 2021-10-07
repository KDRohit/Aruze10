using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataWeeklyRace : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.WeeklyRace.isInExperiment && WeeklyRaceDirector.hasActiveRace;
		}
	}

	public override string noShowReason
	{
		get
		{
			if (!ExperimentWrapper.WeeklyRace.isInExperiment)
			{
				return "WeeklyRaceMOTD: User not in experiment";
			}
			
			return "WeeklyRaceMOTD: User does not have an active race";
		}
	}
	
	public override bool show()
	{
		WeeklyRaceMOTD.showDialog();
		return true;
	}
	
	new public static void resetStaticClassData()
	{
	}

}
