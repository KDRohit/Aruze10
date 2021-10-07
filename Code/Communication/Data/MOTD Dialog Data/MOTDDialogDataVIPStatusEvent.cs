using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataVIPStatusEvent : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				ExperimentWrapper.VIPLevelUpEvent.isInExperiment
				&& VIPStatusBoostEvent.isEnabled()
				&& !VIPStatusBoostEvent.isEnabledByPowerup();
		}
	}

	public override bool show()
	{
		return VIPStatusBoostMOTD.showDialog();
	}

	new public static void resetStaticClassData()
	{
	}

}
