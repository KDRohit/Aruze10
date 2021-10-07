using UnityEngine;
using System.Collections;

public class DoSomethingRichPass : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		if (parameter == "video" && string.IsNullOrEmpty(ExperimentWrapper.RichPass.videoSummaryPath))
		{
			return false;
		}
		
		return CampaignDirector.richPass != null && CampaignDirector.richPass.isActive;
	}

	public override void doAction(string parameter)
	{
		if (parameter == "video")
		{
			CampaignDirector.richPass.showVideo("rich_pass");
		}
		else if (parameter == "summary")
		{
			RichPassSummaryDialog.showDialog();
		}
		else
		{
			RichPassFeatureDialog.showDialog(CampaignDirector.richPass);
		}
	}
}
