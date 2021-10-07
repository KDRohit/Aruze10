using UnityEngine;
using System.Collections;

public class MOTDDialogDataLOZ : MOTDDialogData 
{
	public override bool shouldShow
	{
		get
		{
			LOZCampaign loz = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES) as LOZCampaign;
			return loz != null && loz.isActive && loz.isPortalUnlocked;
		}
	}

	public override string noShowReason
	{
		get
		{
			ChallengeCampaign loz = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES);
			
			string reason = base.noShowReason;
			
			if (loz == null)
			{
				reason += "No land of oz currently running.";
			}
			else
			{
				reason += "Land of oz is not enabled.";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return LandOfOzMOTD.showDialog("motd_new_loz");
	}

	new public static void resetStaticClassData()
	{
	}
}
