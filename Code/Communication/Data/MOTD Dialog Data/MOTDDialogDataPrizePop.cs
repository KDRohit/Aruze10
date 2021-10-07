

using PrizePop;
using System.Text;

public class MOTDDialogDataPrizePop : MOTDDialogData
{
    public override bool shouldShow
    {
	    get { return PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled && PrizePopFeature.instance.meterFillCount > 0;  }
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
    
			return noShowSb.ToString();
		}
	}
    
	public override bool show()
	{
		PrizePopFeature.instance.startBonusGame(false, false);
		return true;
	}
    	
	new public static void resetStaticClassData()
	{
	}
}
