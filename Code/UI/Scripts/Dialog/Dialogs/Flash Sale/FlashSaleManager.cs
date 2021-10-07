using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FlashSaleManager : IResetGame
{
	public static GameTimerRange waitToStartTimer { get; private set; }
	public static int packagesRemaining = 100;
	private static int startingNumberOfPackages = 100;
	private static int secondsPassedSinceLastSaleStarted = 0;
	private static int secondsPassedSinceLastSaleEnded = 0;
	private static int lastStartDate = 0;
	private static int duration = 600;
	private static int cooldown = 30;
	private static float speedParameter = 0.8f;
	public static bool flashSaleIsActive = false;
	public static bool flashSaleIsInProgressFromLastSession = false;
	public static bool flashSaleIsReadyToPop = false;
	public static bool purchaseSucceeded = false;

	public static void init()
	{
		packagesRemaining = startingNumberOfPackages = ExperimentWrapper.FlashSale.startingPackageCount;
		duration = ExperimentWrapper.FlashSale.duration;
		speedParameter = ExperimentWrapper.FlashSale.speedParameter;

		flashSaleIsInProgressFromLastSession = CustomPlayerData.getBool(CustomPlayerData.FLASH_SALE_IN_PROGRESS, false);
		cooldown = ExperimentWrapper.FlashSale.cooldown;

		//Get seconds since the last flash sale started:
		lastStartDate = CustomPlayerData.getInt(CustomPlayerData.FLASH_SALE_LAST_SALE_START_DATE, 0);
		int lastEndDate = CustomPlayerData.getInt(CustomPlayerData.FLASH_SALE_LAST_SALE_END_DATE, 0);
		secondsPassedSinceLastSaleStarted = GameTimer.currentTime - lastStartDate;
		secondsPassedSinceLastSaleEnded = GameTimer.currentTime - lastEndDate;

		//see if there are packages still remaining in the most recent sale:
		updatePackagesRemaining();

		int waitToStartSeconds = UnityEngine.Random.Range(ExperimentWrapper.FlashSale.minWaitTime, ExperimentWrapper.FlashSale.maxWaitTime);

		if (flashSaleIsInProgressFromLastSession && packagesRemaining > 1 && secondsPassedSinceLastSaleStarted < duration) //Previous flash sale is still going on
		{
			//previous flash sale still in progress
			waitToStartTimer = GameTimerRange.createWithTimeRemaining(2); //pop up the flash sale immediately if the previous one is still going on.
		}
		else
		{
			int cooldownRemaining = cooldown - secondsPassedSinceLastSaleEnded;
			int timeRemaining = Mathf.Max(cooldownRemaining, waitToStartSeconds);

			packagesRemaining = startingNumberOfPackages;
			secondsPassedSinceLastSaleStarted = secondsPassedSinceLastSaleEnded = 0;
			flashSaleIsInProgressFromLastSession = false; //This won't get set to false in customPlayerData if the app is closed before the flash sale ends, but here, we know it ended because too much time has passed.

			waitToStartTimer = GameTimerRange.createWithTimeRemaining(timeRemaining);
		}

		waitToStartTimer.registerFunction(setFlashSaleAsReadyToPop);

	}

	public static IEnumerator waitThenTryToSetFlashSaleAsReadyToPop(float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		setFlashSaleAsReadyToPop();
	}

	public static void setFlashSaleAsReadyToPop(Dict args = null, GameTimerRange originalTimer = null)
	{

		if (ExperimentWrapper.FlashSale.filterNineAmToTenPm)
		{
			int hourOfDay = DateTime.Now.Hour;
			if (hourOfDay >= 22 || hourOfDay < 9)
			{
				//If it is before 9am or after 10pm, try again in 5 minutes.
				RoutineRunner.instance.StartCoroutine(waitThenTryToSetFlashSaleAsReadyToPop(5 * 60));
				return;
			}
		}

        flashSaleIsReadyToPop = true;

		if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
		{
			Overlay.instance.topV2.buyButtonManager.setButtonType();
		}
	}

	public static void startSale(Dict args = null, GameTimerRange originalTimer = null)
	{
		if (flashSaleIsActive) { return; }

		if (!flashSaleIsInProgressFromLastSession) //If we aren't resuming from a previous session, now is the new lastStartTime.
		{
			CustomPlayerData.setValue(CustomPlayerData.FLASH_SALE_LAST_SALE_START_DATE, GameTimer.currentTime);
			CustomPlayerData.setValue(CustomPlayerData.FLASH_SALE_LAST_SALE_END_DATE, GameTimer.currentTime + duration); //We need this in case the app gets force closed before the sale expires.
			lastStartDate = GameTimer.currentTime;
		}

		CustomPlayerData.setValue(CustomPlayerData.FLASH_SALE_IN_PROGRESS, true);
		flashSaleIsActive = flashSaleIsInProgressFromLastSession = true;
		FlashSaleDialog.showDialog();
		
		purchaseSucceeded = false;
		RoutineRunner.instance.StartCoroutine(flashSalePackageCountdownRoutine());
	}

	private static IEnumerator flashSalePackageCountdownRoutine()
	{
		//Update once at the beginning to make sure the deal button never displays "00:00:00"
		updatePackagesRemaining();
		updateDealButtonText();
		secondsPassedSinceLastSaleStarted = GameTimer.currentTime - lastStartDate;

		bool toolTipShown = false;

		while (packagesRemaining > 0 && !purchaseSucceeded)
		{
			updatePackagesRemaining();

			if (UnityEngine.Random.Range(0,2) == 0)
			{
				updateDealButtonText();
			}

			if( ((float)packagesRemaining/(float)startingNumberOfPackages) < 0.06f && !toolTipShown)
            {
                if (FlashSaleDealButton.instance != null)
                {
					toolTipShown = true;
                    FlashSaleDealButton.instance.showToolTip();
                }
            }

			yield return new WaitForSeconds(1f);
			secondsPassedSinceLastSaleStarted = GameTimer.currentTime - lastStartDate;
		}

		//Flash sale ended. Reset variables:
		CustomPlayerData.setValue(CustomPlayerData.FLASH_SALE_IN_PROGRESS, false);
		flashSaleIsActive = flashSaleIsInProgressFromLastSession = false;
		flashSaleIsReadyToPop = false;
		waitToStartTimer = GameTimerRange.createWithTimeRemaining(cooldown);
		waitToStartTimer.registerFunction(setFlashSaleAsReadyToPop);
		packagesRemaining = ExperimentWrapper.FlashSale.startingPackageCount;
		secondsPassedSinceLastSaleStarted = 0;
		CustomPlayerData.setValue(CustomPlayerData.FLASH_SALE_LAST_SALE_END_DATE, GameTimer.currentTime);

		if (FlashSaleDialog.instance != null && !purchaseSucceeded) //If a purchase succeeded, then the server callback will trigger this dialog to close.
		{
			Dialog.close(FlashSaleDialog.instance);
		}
		if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
		{
			Overlay.instance.topV2.buyButtonManager.setButtonType();
		}

		purchaseSucceeded = false;
	}

	private static void updateDealButtonText()
	{
		if (Overlay.instance.topV2 != null && Overlay.instance.topV2.buyButtonManager != null)
		{
			Overlay.instance.topV2.buyButtonManager.updateTimerTextDeal(packagesRemaining.ToString() + " Left!");
		}
	}


	private static void updatePackagesRemaining()
	{
		if (secondsPassedSinceLastSaleStarted > duration)
		{
			packagesRemaining = 0;
			return;
		}

		//formula:
		//ROUND(( SIN(  ((seconds_passed/duration_length)^speed_parameter-0.5)*PI()  )+1  )/2*init_packages,0)

		float powParam = Mathf.Pow(((float)secondsPassedSinceLastSaleStarted) / ((float)duration), speedParameter);
		float sinParam = Mathf.Sin((powParam - 0.5f) * Mathf.PI);
		float packagesSoldFloat = ((sinParam + 1f) / 2f) * startingNumberOfPackages;
		if (packagesSoldFloat < 0f) packagesSoldFloat = 0f;
		int packagesSoldInt = (int)packagesSoldFloat;
		packagesRemaining = startingNumberOfPackages - packagesSoldInt;
		if (packagesRemaining < 0) { packagesRemaining = 0; }
	}

	public static void resetStaticClassData()
    { 
	     packagesRemaining = 100;
    	 startingNumberOfPackages = 100;
    	 secondsPassedSinceLastSaleStarted = 0;
    	 secondsPassedSinceLastSaleEnded = 0;
    	 lastStartDate = 0;
    	 duration = 600;
    	 cooldown = 30;
         speedParameter = 0.8f;
    	 flashSaleIsActive = false;
    	 flashSaleIsInProgressFromLastSession = false;
    	 flashSaleIsReadyToPop = false; 
         purchaseSucceeded = false;
    }

}
