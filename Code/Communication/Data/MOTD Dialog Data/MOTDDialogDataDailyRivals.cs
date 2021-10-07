using UnityEngine;


public class MOTDDialogDataDailyRivals : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled &&
			       !string.IsNullOrEmpty(Data.liveData.getString("DAILY_RIVAL_FTUE_URL", ""));
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

			if (!ExperimentWrapper.WeeklyRace.isDailyRivalsEnabled)
			{
				result += "Users not in daily rivals variant";
			}

			return result;
		}
	}

	public override bool show()
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
				motdKey:"daily_rivals_ftue_video",
				summaryScreenImage:"videos/dailyrivals_summary.png",
				statClass: WeeklyRaceDirector.currentRace != null ? WeeklyRaceDirector.currentRace.division.ToString() : ""
			);

			MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "daily_rival_ftue"));

			return true;
		}

		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}