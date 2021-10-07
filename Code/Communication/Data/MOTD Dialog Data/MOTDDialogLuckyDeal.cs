using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogLuckyDeal : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			System.DateTime startTime = Common.convertFromUnixTimestampSeconds(Data.liveData.getInt("WHEEL_DEAL_START_TIME", 0));
			System.DateTime endTime = Common.convertFromUnixTimestampSeconds(Data.liveData.getInt("WHEEL_DEAL_END_TIME", 0));
			bool validTimeRange = endTime > System.DateTime.UtcNow && startTime < System.DateTime.UtcNow;
			return ExperimentWrapper.WheelDeal.isInExperiment && !LuckyDealDialog.doNotShowUntilRestart && validTimeRange;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			
			if (!ExperimentWrapper.WheelDeal.isInExperiment)
			{
				result += "Experiment is off.\n";
			}

			if (LuckyDealDialog.eventData == null)
			{
				result += "LuckyDealDialog.eventData is null.\n";
			}

			if (LuckyDealDialog.doNotShowUntilRestart)
			{
				result += "LuckyDealDialog.doNotShowUntilRestart is true.\n";
			}

			return result;
		}
	}

	public override bool show()
	{
		if (LuckyDealDialog.eventData != null)
		{
			return LuckyDealDialog.showDialog(keyName);
		}
		else
		{
			// Ask Stefan about this, because this seems wack to just wait forever.
			RoutineRunner.instance.StartCoroutine(waitUntilReady());
			return false;
		}
	}

	public IEnumerator waitUntilReady()
	{
		while (LuckyDealDialog.eventData == null)
		{
			yield return null;
		}

		LuckyDealDialog.showDialog(keyName);	
	}

	new public static void resetStaticClassData()
	{
	}
}
