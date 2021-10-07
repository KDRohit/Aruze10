using UnityEngine;

public class DoSomethingDailyRivalsFTUE : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled)
		{
			string url = Data.liveData.getString("DAILY_RIVAL_FTUE_URL", "");

			if (!string.IsNullOrEmpty(url))
			{
				VideoDialog.showDialog
				(
					url,
					action:"",
					actionLabel:"",
					statName:"daily_rival_ftue",
					closeButtonDelay:3,
					skipButtonDelay:3,
				 	motdKey:"",
					summaryScreenImage:"videos/dailyrivals_summary.png",
					statClass: WeeklyRaceDirector.currentRace != null ? WeeklyRaceDirector.currentRace.division.ToString() : ""
				);
			}
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled &&
			   !string.IsNullOrEmpty(Data.liveData.getString("DAILY_RIVAL_FTUE_URL", ""));
	}
}