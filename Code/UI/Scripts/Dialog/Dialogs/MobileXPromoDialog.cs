using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MobileXPromoDialog : DialogBase, IResetGame
{	
	public Renderer creativeTexture;
	public ImageButtonHandler closeButton;
	public ImageButtonHandler playButton;
	
	private string xPromoKey = "";
	private bool shouldAbort = false;
	private MobileXpromo.SurfacingPoint surfacing = MobileXpromo.SurfacingPoint.NONE;
	
	void Update()
	{
		AndroidUtil.checkBackButton(checkBackButton);
	}

	private string getSurfacingString()
	{
		switch (surfacing)
		{
			case MobileXpromo.SurfacingPoint.RTL:
				return "rtl";

			case MobileXpromo.SurfacingPoint.OOC:
				return "ooc";

			default:
				return "lobby";
		}
	}

	public override void init()
	{
		xPromoKey = (string)dialogArgs.getWithDefault(D.OPTION, "");
		surfacing = (MobileXpromo.SurfacingPoint)dialogArgs.getWithDefault(D.OPTION1, MobileXpromo.SurfacingPoint.NONE);
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			family: MobileXpromo.isGameInstalled() ? "installed" : "not_installed",
			genus:"view",
			milestone: getSurfacingString());

		//Place texture on renderer
		if (!downloadedTextureToRenderer(creativeTexture, 0))
		{
			//abort if texture is not set up so we don't giant show pink square
			shouldAbort = true;
		}

		closeButton.registerEventDelegate(closeClicked);
		MOTDFramework.markMotdSeen(dialogArgs);
#if UNITY_WEBGL
		// WebGL does an all-in-one download+play
		playButton.registerEventDelegate(downloadClicked);
#else
		bool isDownloaded = false;
		string bundleId = MobileXpromo.getBundleId();
		if (!string.IsNullOrEmpty(bundleId))
		{
			isDownloaded = AppsManager.isBundleIdInstalled(bundleId);
		}
		
		if (isDownloaded)
		{
			playButton.registerEventDelegate(playClicked);
		}
		else
		{
			playButton.registerEventDelegate(downloadClicked);
		}
#endif
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (shouldAbort)
		{
			Dialog.close();
		}
	}

	// NGUI button callback
	public void playClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			family:"installed",
			genus:"click",
			milestone: getSurfacingString());
		Dialog.close();
		string sAppId = MobileXpromo.getBundleId();
		if (!string.IsNullOrEmpty(sAppId))
		{
			AppsManager.launchBundle(sAppId);
		}
	}
	
	// NGUI button callback. 
	public void downloadClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			family:"not_installed",
			genus:"click",
			milestone: getSurfacingString());		
		Dialog.close();

		string downloadUrl = MobileXpromo.getDownloadUrl();
		Debug.Log("xpromo download URL = " + downloadUrl);

		if (!string.IsNullOrEmpty(downloadUrl))
		{
			// Append the advertising track code.
			downloadUrl = CommonText.appendQuerystring(downloadUrl, Zynga.Slots.ZyngaConstantsGame.advertisingTrackSuffix);
			Debug.Log("xpromo download URL + querystring = " + downloadUrl);

#if UNITY_WEBGL && !UNITY_EDITOR
			Application.ExternalEval( string.Format("eval(\"window.top.location = '{0}'\")", downloadUrl));
#else
			Application.OpenURL(downloadUrl);
#endif
		}
	}
	
	// NGUI button callback.
	public void closeClicked(Dict args = null)
	{
		Dialog.close();
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "xpromo",
			phylum: "hir_xpromo_creative_v2",
			klass: xPromoKey,
			family: MobileXpromo.isGameInstalled() ? "installed" : "not_installed",
			genus:"close",
			milestone: getSurfacingString());
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static bool showDialog(string xPromoKey, string dialogArt, MobileXpromo.SurfacingPoint surfacing, DialogBase.AnswerDelegate callback, string motdKey = "")
	{
		if (string.IsNullOrEmpty(xPromoKey))
		{
			Debug.LogError("Invalid promo");
			return false;
		}

		Dict args = Dict.create(D.OPTION, xPromoKey,
			D.OPTION1, surfacing,
			D.CALLBACK, callback);

		if (!string.IsNullOrEmpty(motdKey))
		{
			args.Add(D.MOTD_KEY, motdKey);
		}

		Dialog.instance.showDialogAfterDownloadingTextures(
			"mobile_xpromo",
			dialogArt,
			args,
			true	// abort on failing to load the image
		);
		return true;
	}

	// required for Unity/Mono compiler
	private void checkBackButton()
	{
		closeClicked();
	}

	public static void resetStaticClassData(){}
}
