using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataLifecycleSales : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return LifecycleDialog.isActive;
		}
	}

	public override string noShowReason
	{
		get
		{
			// Since there are several possible reasons why the starter dialog might not be active,
			// I made a notActiveReason property to tell us which of them is the reason.
			return base.noShowReason + LifecycleDialog.notActiveReason;
		}
	}
	
	public override bool show()
	{
		if (LifecycleDialog.isActive)
		{
			return LifecycleDialog.showDialog(keyName);
		}
		return false;
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
