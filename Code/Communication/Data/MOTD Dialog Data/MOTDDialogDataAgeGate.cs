using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MOTDDialogDataAgeGate : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return GameExperience.totalSpinCount == 0 &&
			//ExperimentWrapper.AgeGate.isInExperiment &&
			// This is a temporary liveData conversion so that we don't need a data push.
			(Data.liveData != null && Data.liveData.getBool("SHOULD_SHOW_AGE_GATE", false)) &&
			CustomPlayerData.getInt(CustomPlayerData.SHOW_AGE_GATE, 1) == 1;
		}
	}

	public override bool show()
	{
		return AgeGateDialog.showDialog();
	}

	new public static void resetStaticClassData()
	{
	}
}

