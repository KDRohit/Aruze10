using UnityEngine;

public class DoSomethingPowerupsFTUE : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (PowerupsManager.isPowerupsEnabled)
		{
			string url = Data.liveData.getString("POWERUPS_FTUE_VIDEO", "");

			if (!string.IsNullOrEmpty(url))
			{
				VideoDialog.showDialog
				(
					url,
					action:"",
					actionLabel:"",
					statName:"powerup_in_collections_ftue",
					closeButtonDelay:3,
					skipButtonDelay:3,
					motdKey:"",
					summaryScreenImage:"videos/powerups_generic_summary.png"
				);
			}
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return PowerupsManager.isPowerupsEnabled &&
		       !string.IsNullOrEmpty(Data.liveData.getString("POWERUPS_FTUE_VIDEO", ""));
	}
}