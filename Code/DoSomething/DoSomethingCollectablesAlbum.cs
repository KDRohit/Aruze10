using UnityEngine;
using System.Collections;

public class DoSomethingCollectablesAlbum : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the Collections carousel if collections is active
		return Collectables.isActive();
	}

	public override void doAction(string parameter)
	{
		if (!string.IsNullOrEmpty(Collectables.currentAlbum))
		{
			CollectableAlbumDialog.showDialog(Collectables.currentAlbum, "carousel");
		}
	}

	public override GameTimer getTimer(string parameter)
	{
		if (Collectables.endTimer != null && Collectables.endTimer.timeRemaining <= Common.SECONDS_PER_DAY * 7)
		{
			return new GameTimer(Collectables.endTimer.timeRemaining);
		}

		return null;
	}
}
