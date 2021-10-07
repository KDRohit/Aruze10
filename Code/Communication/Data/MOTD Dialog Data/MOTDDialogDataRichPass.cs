using Zynga.Core.Util;
using System.Text;

/*
Override for special behavior.
*/

public class MOTDDialogDataRichPass : MOTDDialogData
{
	public override bool shouldShow
	{
		get { return CampaignDirector.richPass != null && CampaignDirector.richPass.isActive && (CampaignDirector.richPass.hasNewPass || CampaignDirector.richPass.hasNewChallenges); }
	}

	public override string noShowReason
	{
		get
		{
			StringBuilder noShowSb = new StringBuilder();

			if (CampaignDirector.richPass == null)
			{
				noShowSb.Append("Rich Pass data is null");
				return noShowSb.ToString();
			}

			if (!CampaignDirector.richPass.isActive)
			{
				noShowSb.Append("Rich Pass Campaign isn't currently active");
			}
			
			int latestSeasonalUnlockDate = CampaignDirector.richPass.getLatestSeasonalDate();
			if (latestSeasonalUnlockDate <= CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_RICH_PASS_CHALLENGES_TIME, 0))
			{
				noShowSb.Append("Already seen MOTD for latest group of unlocked seasonal challenges");
			}
			
			if (CampaignDirector.richPass.timerRange.startTimestamp <= CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_RICH_PASS_START_TIME, 0))
			{
				noShowSb.Append("Already seen summary MOTD for current pass");
			}

			return noShowSb.ToString();
		}
	}

	public override bool show()
	{
		if (CampaignDirector.richPass.hasNewPass)
		{
			return RichPassSummaryDialog.showDialog("rich_pass");
		}
		
		if (CampaignDirector.richPass.hasNewChallenges)
		{
			int latestSeasonalUnlockDate = CampaignDirector.richPass.getLatestSeasonalDate();
			return RichPassNewSeasonalChallengesDialog.showDialog(latestSeasonalUnlockDate, "rich_pass");	
		}

		return false;
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
