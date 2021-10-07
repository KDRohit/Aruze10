using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataWatchToEarn : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				WatchToEarn.isEnabled &&
				!WatchToEarn.motdSeen;
		}
	}

	public override bool show()
	{
		if (base.show())
		{
			WatchToEarn.motdSeen = true;
			WatchToEarnAction.markMotdSeen();			
			return true;
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
	
}
