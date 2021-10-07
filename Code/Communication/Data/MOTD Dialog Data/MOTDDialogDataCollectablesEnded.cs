using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataCollectablesEnded : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			//Show this is server told us that an event ended
			return CollectablesSeasonOverDialog.needsToShowDialog;
		}
	}

	public override bool show()
	{
		return CollectablesSeasonOverDialog.showDialog();
	}

	public override string noShowReason
	{
		get 
		{
			string reason = base.noShowReason;
			if (!CollectablesSeasonOverDialog.needsToShowDialog)
			{
				reason += "Didn't receive season ended info from the server";
			}
			return reason;
		}
	}

	new public static void resetStaticClassData()
	{
	}
}
