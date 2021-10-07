using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataCollectablesRewardsIncreased : MOTDDialogData
{
	public override bool shouldShow
	{
        get
        {
			//ststewart 5/29/20
			//We are disabling the ability to open this dialog. It seems impossible to get this thing to pop up in the current version,
			//but at the moment, we want to make sure it never pops up, since, on the rare occation that it does pop up, it shows incorrect data.
			return false;

			//	//Show this is server told us that an event ended
			//	return Collectables.isActive() && Collectables.Instance.hasIncreasedRewards;
		}
    }

	public override bool show()
	{
		return CollectablesRewardsIncreasedDialog.showDialog();
	}

	public override string noShowReason
	{
		get 
		{
			string reason = base.noShowReason;
			if (!Collectables.isActive())
			{
				reason += "Collections event isn't active \n";
			}
			if (!Collectables.Instance.hasIncreasedRewards)
			{
				reason += "Didn't receive increased rewards event from server \n";
			}
			return reason;
		}
	}

	new public static void resetStaticClassData()
	{
	}
}
