using UnityEngine;
using System.Collections;

public class BuyPageDynamic : MonoBehaviour
{
    public static GameTimer cooldownTimer;

	public static bool isEnabled
	{
		get
		{
			return (!PurchaseFeatureData.isSaleActive && // Make sure we don't show the buy page twice.
					!ExperimentWrapper.FirstPurchaseOffer.isInExperiment &&  // Don't show the buy page twice.
					ExperimentWrapper.DynamicBuyPageSurfacing.isInExperiment &&
					(cooldownTimer != null && cooldownTimer.isExpired));
		}
	}

	public static void init()
	{
		if (!ExperimentWrapper.DynamicBuyPageSurfacing.isInExperiment)
		{
			// Don't do setup if the feature is not on.
			return;
		}
		float previousShowTime = CustomPlayerData.getInt(CustomPlayerData.DYNAMIC_BUY_PAGE_LAST_SEEN, -1);
		if (previousShowTime >= 0)
		{
			float timeSinceShown = (GameTimer.currentTime - previousShowTime);
			if (timeSinceShown > ExperimentWrapper.DynamicBuyPageSurfacing.cooldown)
			{
				// If we have waited longer than the desired cooldown, then create an expired timer.
				cooldownTimer = new GameTimer(0);
			}
			else
			{
				// Otherwise create the timer with the remaining time.
				float remainingTime = ExperimentWrapper.DynamicBuyPageSurfacing.cooldown - timeSinceShown;
				Debug.LogErrorFormat("BuyPageDynamic.cs -- init -- creating timer with remaining time: {0}", remainingTime);
				cooldownTimer = new GameTimer(remainingTime);
			}

		}
		else
		{
			// Create a timer that has already expired.
			cooldownTimer = new GameTimer(0);
		}
	}
	public static bool show(string keyName)
	{
		// Store the time as last seen so it persists for next load.
		CustomPlayerData.setValue(CustomPlayerData.DYNAMIC_BUY_PAGE_LAST_SEEN, GameTimer.currentTime);
		// Show the dialog.
		BuyCreditsDialog.showDialog(keyName);

		// Remake the timer in case they want to change this to show multiple times per session.
		cooldownTimer = new GameTimer(ExperimentWrapper.DynamicBuyPageSurfacing.cooldown);
		return true;
	}
}
