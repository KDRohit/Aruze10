using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataStarterDialog : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return StarterDialog.isActive;
		}
	}

	public override string noShowReason
	{
		get
		{
			// Since there are several possible reasons why the starter dialog might not be active,
			// I made a notActiveReason property to tell us which of them is the reason.
			return base.noShowReason + StarterDialog.notActiveReason;
		}
	}
	
	public override bool show()
	{
		return StarterDialog.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
