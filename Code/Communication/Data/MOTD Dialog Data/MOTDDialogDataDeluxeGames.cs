using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataDeluxeGames : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.DeluxeGames.isInExperiment;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!ExperimentWrapper.DeluxeGames.isInExperiment)
			{
				reason += "Not in DeluxeGames experiment.\n";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return DeluxeGamesMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
}
