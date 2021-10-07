using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MobileXpromo : IResetGame
{
	private const string CAROUSEL_IMAGE_PATH_FORMAT = "lobby_carousel/xpromo/{0}_CarouselBG.jpg";
	private const string CAROUSEL_IMAGE_PATH_FORMAT_LOBBY_V3 = "lobby_carousel/xpromo/V3_{0}_CarouselBG.png";

	// List of the live xpromo keynames
	public static List<string> liveXpromos = new List<string>();

	private static bool hasLoggedView = false;

	public enum SurfacingPoint
	{
		NONE,
		OOC,
		RTL
	}

	public static bool isEnabled()
	{
		if (!ExperimentWrapper.MobileToMobileXPromo.isInExperiment)
		{
			return false;
		}

		bool isInstalled = isGameInstalled();

		return (!isInstalled || ExperimentWrapper.MobileToMobileXPromo.enablePlay);
	}

	public static bool isGameInstalled()
	{
		string appId = getBundleId();
		bool isInstalled = false;
		if (!string.IsNullOrEmpty(appId))
		{
			isInstalled = AppsManager.isBundleIdInstalled(appId);
		}

		return isInstalled;
	}

	public static bool isEnabled(string targetPromo)
	{
		// See if the xpromo'd app is already installed.
		bool isTargetApp = false;

		if (null != ExperimentWrapper.MobileToMobileXPromo.experimentData)
		{
			isTargetApp = targetPromo == ExperimentWrapper.MobileToMobileXPromo.getPromoGame();
		}

		if (!isTargetApp)
		{
			return false;
		}

		return isEnabled();
	}

	public static void logView()
	{
		//only once per session
		if (hasLoggedView)
		{
			return;
		}

		hasLoggedView = true;
		string xPromoKey = ExperimentWrapper.MobileToMobileXPromo.getArtCampaign();
		StatsManager.Instance.LogCount(counterName:"lobby",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			genus: "view");
	}

	// Get the image path of the carousel image for the current campaign and experiment variant.
	public static string getCarouselImagePath()
	{
		string xPromoKey = ExperimentWrapper.MobileToMobileXPromo.getArtCampaign();
		if (string.IsNullOrEmpty(xPromoKey))
		{
			return "";
		}
		else
		{
			return string.Format(CAROUSEL_IMAGE_PATH_FORMAT_LOBBY_V3, xPromoKey);
		}
	}

	public static string getBundleId()
	{
		return ExperimentWrapper.MobileToMobileXPromo.playUrl;
	}

	public static string getDownloadUrl()
	{
		string downloadUrl = ExperimentWrapper.MobileToMobileXPromo.installUrl;
		downloadUrl = downloadUrl.Trim();	// Just in case.
		return downloadUrl;
	}

	private static void checkForVariantChange()
	{
		string lastVariant = CustomPlayerData.getString(CustomPlayerData.MOBLE_XPROMO_LAST_VARIANT, "");
		if (lastVariant != ExperimentWrapper.MobileToMobileXPromo.recipient)
		{
			//reset
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_CHANGE_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.MOBLE_XPROMO_LAST_VARIANT, ExperimentWrapper.MobileToMobileXPromo.recipient);
		}
	}

	public static bool shouldShow(SurfacingPoint surfacing)
	{
		if (!isEnabled())
		{
			return false;
		}

		checkForVariantChange();

		int lastSeenTimestamp = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_LAST_SHOW_TIME, 0);
		int timeSinceLastSeen = GameTimer.currentTime - lastSeenTimestamp;
		bool isOnCooldown = timeSinceLastSeen < (ExperimentWrapper.MobileToMobileXPromo.autoPopCooldown);
		bool isAtMaxViewsForSurfacePoint = false;
		bool result = false;
		int totalCount = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
		bool isAtMaxViews = totalCount >= ExperimentWrapper.MobileToMobileXPromo.dialogMaxViewToSwap;

		switch (surfacing)
		{
			case SurfacingPoint.OOC:
				int oocCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
				isAtMaxViewsForSurfacePoint = (oocCount > ExperimentWrapper.MobileToMobileXPromo.OOCMaxViews);
				result = ExperimentWrapper.MobileToMobileXPromo.shouldOOCAutoPop && !isAtMaxViews && !isAtMaxViewsForSurfacePoint && !isOnCooldown;
				break;
			case SurfacingPoint.RTL:
				int rtlCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
				isAtMaxViewsForSurfacePoint = (rtlCount > ExperimentWrapper.MobileToMobileXPromo.RTLMaxViews);
				result = ExperimentWrapper.MobileToMobileXPromo.shouldRTLAutoPop && !isAtMaxViews && !isAtMaxViewsForSurfacePoint && !isOnCooldown;
				break;
			default:
				result = false;
				break;
		}
		return result;
	}

	public static void onDialogClose(Dict answerArgs)
	{
		MobileXpromo.SurfacingPoint surfacing = (MobileXpromo.SurfacingPoint)answerArgs.getWithDefault(D.OPTION1, MobileXpromo.SurfacingPoint.NONE);
		incrementCounts(surfacing);

		//re-activate carousel data so it downloads a new image
		CarouselData carouselData = CarouselData.findInactiveByAction("xpromo_v2");
		if (carouselData != null)
		{
			carouselData.activate();
		}
	}

	public static bool showXpromo(SurfacingPoint surfacing)
	{
		//remove carousel card when clicked
		CarouselData carouselData = CarouselData.findActiveByAction("xpromo_v2");
		if (carouselData != null)
		{
			carouselData.deactivate();
		}

		return MobileXPromoDialog.showDialog(
			ExperimentWrapper.MobileToMobileXPromo.getArtCampaign(),
			ExperimentWrapper.MobileToMobileXPromo.getDialogArt(),
			surfacing,
			onDialogClose);
	}

	public static void incrementCounts(SurfacingPoint surfacing)
	{
		int totalCount = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
		++totalCount;
		
		int currentCount = 0;
		switch (surfacing)
		{
			case SurfacingPoint.OOC:
				currentCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
				CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, currentCount + 1);
				break;
			case SurfacingPoint.RTL:
				currentCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
				CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, currentCount + 1);
				break;
		}

		string[] allArt = ExperimentWrapper.MobileToMobileXPromo.getAllArtInCampaign();
		bool isAtMaxViews = totalCount >= ExperimentWrapper.MobileToMobileXPromo.dialogMaxViewToSwap;

		int artIndex = 0;
		bool isNewArtAvailable = false;

		if (null != allArt && allArt.Length > 0)
		{
			artIndex = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_CHANGE_COUNT, 0);
			int maxRotation = ExperimentWrapper.MobileToMobileXPromo.autoSwapRotationTimes * allArt.Length;
			isNewArtAvailable = artIndex < maxRotation;
		}

		if (isAtMaxViews && isNewArtAvailable)
		{
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_CHANGE_COUNT, artIndex + 1);
			totalCount = 0; //set total count to 0
		}

		CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_VIEW_COUNT, totalCount);
		CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_LAST_SHOW_TIME, GameTimer.currentTime);
	}

	public static void resetStaticClassData()
	{
		hasLoggedView = false;
	}
}
