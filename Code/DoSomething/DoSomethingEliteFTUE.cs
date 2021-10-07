using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingEliteFTUE : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (EliteManager.isActive)
		{
			string url = Data.liveData.getString("ELITE_FTUE_VIDEO", "");
			string summaryImage = Data.liveData.getString("ELITE_SUMMARY_IMAGE", "");

			if (!string.IsNullOrEmpty(url))
			{
				if (!string.IsNullOrEmpty(parameter))
				{
					StatsElite.logViewVideo(parameter);
				}
				else
				{
					StatsElite.logViewVideo("primary");
				}
				
				VideoDialog.showDialog
				(
					url,
					closeButtonDelay:3,
					skipButtonDelay:3,
					summaryScreenImage:summaryImage
				);
			}
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return EliteManager.isActive &&
		       !string.IsNullOrEmpty(Data.liveData.getString("ELITE_FTUE_VIDEO", ""));
	}
}
