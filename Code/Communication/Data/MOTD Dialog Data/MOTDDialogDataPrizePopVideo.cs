using PrizePop;
using System.Text;

public class MOTDDialogDataPrizePopVideo : MOTDDialogData
{
    public override bool shouldShow
    {
	    get
	    {
		    return PrizePopFeature.instance != null && 
		           PrizePopFeature.instance.isEnabled && 
		           PrizePopFeature.instance.featureTimer.startTimestamp > CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_PRIZE_POP_INTRO_VIDEO, 0);
	    }
    }
    
	public override string noShowReason
	{
		get
		{
			StringBuilder noShowSb = new StringBuilder();
    
			if (PrizePopFeature.instance == null)
			{
				noShowSb.Append("Prize Pop data is null");
				return noShowSb.ToString();
			}
    
			if (!PrizePopFeature.instance.isEnabled)
			{ 
				noShowSb.Append("Prize Pop Feature isn't enabled");
			}

			if (PrizePopFeature.instance.featureTimer.startTimestamp <
			    CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_PRIZE_POP_INTRO_VIDEO, 0))
			{
				noShowSb.Append("Already seen video for this event or an event in the future");
			}
    
			return noShowSb.ToString();
		}
	}
    
	public override bool show()
	{
		CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_PRIZE_POP_INTRO_VIDEO, PrizePopFeature.instance.featureTimer.startTimestamp);
		PrizePopFeature.instance.showVideo(true);
		return true;
	}
}
