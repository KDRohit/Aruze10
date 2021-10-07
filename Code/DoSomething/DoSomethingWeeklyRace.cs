using UnityEngine;
using System.Collections;

public class DoSomethingWeeklyRace : DoSomethingAction 
{
	public override void doAction(string parameter)
	{
		if (WeeklyRaceDirector.hasActiveRace)
		{
			WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{		
		return WeeklyRaceDirector.hasActiveRace;
	}
}
