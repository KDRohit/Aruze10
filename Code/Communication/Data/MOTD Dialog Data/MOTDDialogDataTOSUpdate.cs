using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataTOSUpdate : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			/* DEPRECATED */
			return false;
		}
	}
	
	public override bool show()
	{
		AnalyticsManager.Instance.LogTOSView();
		return base.show();
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
