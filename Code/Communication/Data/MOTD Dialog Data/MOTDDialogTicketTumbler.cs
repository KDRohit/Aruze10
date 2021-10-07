using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogTicketTumbler : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return TicketTumblerFeature.instance.isEnabled;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			
			if (!ExperimentWrapper.LotteryDayTuning.isInExperiment)
			{
				result += "Experiment is off.\n";
			}

			if (SlotsPlayer.instance.socialMember.experienceLevel < ExperimentWrapper.LotteryDayTuning.levelLock)
			{
				result += "Player level is to low, level lock is . " + ExperimentWrapper.LotteryDayTuning.levelLock  + "\n";
			}

			if (TicketTumblerFeature.instance.eventData == null)
			{
				result += "TicketTumbler.eventData is null.\n";
			}

			return result;
		}
	}

	public override bool show()
	{
		if (TicketTumblerFeature.instance.eventData != null)
		{
			return TicketTumblerDialog.showDialog(keyName);
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}
