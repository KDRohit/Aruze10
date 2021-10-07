
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attached to the Targeted Sale carousel panel since it has a live timer.
Any necessary UI elements are linked to this to get for setting up.
*/

public class CarouselPanelZade : CarouselPanelBase
{
	public Renderer background;
	
	public static bool isValid = false;
	
	private static Texture2D bannerTexture;
	//TODO: Girish
	//private static Zap.Ad bannerAd; // The ad slot that contains the banner ad information.
	//private static Zap.Ad interstitialAd; // The ad slot that contains the interstitial.
	
	public const string DO_SOMETHING = "zade_xpromo_carousel";
	public override void init()
	{
		/*#if UNITY_EDITOR
		if (bannerTexture != null)
		#else
		//TODO: Girish
		//if (bannerTexture != null && bannerAd != null && interstitialAd != null)
		#endif
		{
			setTexture(background, bannerTexture);
		}
		else
		{*/
			data.deactivate();
		//}
	}
	
	// Clear out the cached information from ZADE if we have any, and reqeust the ad slot again.
	public static void getZadeAd()
	{
#if UNITY_EDITOR
		string assetPath = "http://zynga2-a.akamaihd.net/zap/customassets/production/22991b4fc6d5d303cc9edec1cde2e7e6c87ac51456c19bddc044c59946630671";
		RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(assetPath, textureLoadedCallback, null, "", true));
#else
		// We want to get a clean set of information from Zade, so uncache all the data.
		//TODO: Girish
		/*ZADEAdManager.Instance.uncacheAd(ZADEAdManager.ZADE_CAROUSEL_SLOT_NAME);
		ZADEAdManager.Instance.uncacheAd(ZADEAdManager.ZADE_CAROUSEL_INTERSTITIAL_SLOT_NAME);
		bannerAd = null;
		interstitialAd = null;
		bannerTexture = null;
		ZADEAdManager.Instance.RequestAd(ZADEAdManager.ZADE_CAROUSEL_SLOT_NAME, onZadeCarouselAdLoaded, onZadeAdLoadedError);
		ZADEAdManager.Instance.RequestAd(ZADEAdManager.ZADE_CAROUSEL_INTERSTITIAL_SLOT_NAME, onZadeCarouselInterstitialAdLoaded, onZadeAdLoadedError);
		*/
		#endif
	}
	
#if UNITY_EDITOR
	private static void textureLoadedCallback(Texture2D tex, Dict args = null)
	{
		isValid = true; // Fake this for the editor.
		bannerTexture = tex;
		CarouselData data = CarouselData.findInactiveByAction(DO_SOMETHING);
		if (data != null)
		{
			data.activate();
		}
	}
#endif
	
	// ZADE RequestAd callback for the carousel banner ad.
	/*private static void onZadeCarouselAdLoaded(Zap.Ad ad, Texture2D tex)
	{	
		if (ad != null && tex != null)
		{
			bannerTexture = tex;
			bannerAd = ad;
			checkIfReady();
		}
		else
		{
			Debug.Log("Did not find a banner ad for the zade carousel");
		}
			
	}*/

	// ZADE RequestAd callback for the interstitial ad.
	/*private static void onZadeCarouselInterstitialAdLoaded(Zap.Ad ad, Texture2D tex)
	{
		if (ad != null &&
			(ad.CanOpenMRAID || ad.CanOpenRedirect)// Make sure that this slide will do something when clicked on.
		)
		{
			interstitialAd = ad;
			checkIfReady();
		}
		else
		{
			Debug.Log("Did not find an interstitial ad for the zade carousel");
		}
	}*/

	// Called after each zade ad is loaded to check if the other has finished as they are asynchronous.
	private static void checkIfReady()
	{
		#pragma warning disable 219 // The variable 'action' is assigned but its balue is never used (CS0219)
		CarouselData data = CarouselData.findInactiveByAction(DO_SOMETHING);
		#pragma warning restore 219

		/*if (bannerAd != null && interstitialAd != null)
		{
			isValid = true;
			data.activate();
		}
		else
		{
			isValid = false;
		}*/
	}
	
	// ZADE RequestAd error callback. Turns off the panel if it is currently on.
	/*private static void onZadeAdLoadedError(Zap.AdError error)
	{
		isValid = false;
		CarouselData data = CarouselData.findActiveByAction(DO_SOMETHING);
		if (data != null)
		{
			data.deactivate();
		}
	}*/
	
	// Method for when the carousel panel is clicked on. Called from DoSomething.cs
	public static void carouselClicked()
	{
		/*if (interstitialAd != null && interstitialAd.CanOpenMRAID)
		{
			interstitialAd.openMRAID(true);
			getZadeAd();
		}*/
	}
}
