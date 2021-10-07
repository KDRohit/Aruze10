using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class HelpDialogHIR : HelpDialog, IResetGame
{
	[SerializeField] private GameObject lazyLoadObject;
	[SerializeField] private GameObject reloadGameButton;
	[SerializeField] private GameObject meterParent;
	[SerializeField] private UIMeterNGUI progressMeter;
	[SerializeField] private TextMeshPro statusLabel;
	[SerializeField] private GameObject personalDataButton;
	[SerializeField] private GameObject privacyPolicyButton;
	[SerializeField] private TextMeshPro manageButtonLabel;
	[SerializeField] private UIImageButton ccpaButton;
	[SerializeField] private UIButtonMessage ccpaButtonMessage;
	[SerializeField] private TextMeshPro ccpaButtonLabel;


	protected float actualProgress = 0;				// Normalized progress of actual downloading.
	protected float displayedProgress = 0;			// Normalized progress that is displayed.

	public static List<string> downloadingLazyBundles = new List<string>();
	public static bool hasLoadedBundles = false;
	
	/// Initialization
	public override void init()
	{
		if (SlotsPlayer.instance == null)
		{
			return;
		}

		bool enableGDPRFeatures = Data.liveData.getBool("GDPR_CLIENT_ENABLED", false) && ExperimentWrapper.GDPRHelpDialog.isInExperiment;
		personalDataButton.SetActive(enableGDPRFeatures);
		
		meterParent.SetActive(false);
		statusLabel.text = "";
		if (manageButtonLabel != null)
		{
			manageButtonLabel.SetText(Localize.text("Manage Account"));
		}
		
		if (hasLoadedBundles)
		{
			showReloadButton();
		}
		else
		{
			hideReloadButton();
		}
		
		//enable ccpa if necessary
		if (ExperimentWrapper.CCPA.isEnabled())
		{
			ComplianceUrlAction.GetCCPAUrl(onGetCCPAUrlCallback);
			if (ccpaButton != null)
			{
				ccpaButton.gameObject.SetActive(true);
				ccpaButton.isEnabled = false;
			}
			if (ccpaButtonMessage != null)
			{
				ccpaButtonMessage.enabled = false;
			}
			if (ccpaButtonLabel != null)
			{
				ccpaButtonLabel.text = Localize.text(ExperimentWrapper.CCPA.getLocKey());
			}
		}
		else
		{
			if (ccpaButton != null)
			{
				ccpaButton.gameObject.SetActive(false);
			}
		}
		
		base.init();
	}
	
	private void onGetCCPAUrlCallback(string zid, string pin, string url)
	{
		//set url
		ccpaURL = url;
		//enable button
		if (ccpaButton != null)
		{
			ccpaButton.isEnabled = true;
		}
		if (ccpaButtonMessage != null)
		{
			ccpaButtonMessage.enabled = true;
		}
	}

	/*=========================================================================================
	LAZY LOADING
	=========================================================================================*/
	public static void addDownload(string bundleName)
	{
		if (!downloadingLazyBundles.Contains(bundleName) && !AssetBundleManager.isBundleCached(bundleName))
		{
			downloadingLazyBundles.Add(bundleName);
		}
	}

	void Update()
	{
		if (!isLoading)
		{
			return;
		}
		else
		{
			meterParent.SetActive(true);
			statusLabel.gameObject.SetActive(true);
			statusLabel.text = Localize.text("lazy_loading_content");
		}

		float bundleLoadProgress = 0f;
		foreach (string dl in downloadingLazyBundles)
		{
			bundleLoadProgress += AssetBundleManager.loadProgress(dl);
		}
		
		bundleLoadProgress /= downloadingLazyBundles.Count;
		if (bundleLoadProgress >= 0.999f)
		{
			downloadingLazyBundles.Clear();
			showReloadButton();
		}
		else if (downloadingLazyBundles.Count > 0)
		{
			reloadGameButton.SetActive(false);
		}
		
		// Debug.Log("bundleLoadProgress: " + bundleLoadProgress);
		actualProgress = Mathf.Min(0.99f, bundleLoadProgress);

		if (displayedProgress < actualProgress)
		{
			// If displayed progress is behind actual progress, move up to 2% more towards actual progress.
			displayedProgress += Mathf.Clamp(actualProgress - displayedProgress, 0, .02f);
		}
		else if (displayedProgress < .9f && downloadingLazyBundles.Count == 0)
		{
			// Limit simulated displayed progress to 90% unless actual progress is more than displayed progress,
			// or if the hide() call has been made.
			displayedProgress += .00125f;
		}

		//Debug.LogFormat("HELPDialog -- bundleLoadProgress: {0}, displayedProgress: {1}", bundleLoadProgress.ToString(), displayedProgress.ToString());
		displayedProgress = Mathf.Min(1f, displayedProgress);

		progressMeter.currentValue = (int)(displayedProgress * 100f);
	}

	private void showReloadButton()
	{
		hasLoadedBundles = true;
		meterParent.SetActive(false);
		reloadGameButton.SetActive(!isLoading);
		statusLabel.text = Localize.text("lazy_load_ready");
	}

	private void hideReloadButton()
	{
		hasLoadedBundles = false;
		meterParent.SetActive(false);
		reloadGameButton.SetActive(false);
		statusLabel.text = "";
	}


	public void onManageButtonClick()
	{
		Dialog.close(this);

		ZisManageAccountDialog.showDialog();

		StatsZIS.logSettingsZis("manage_account");
	}

	public void onReloadClick()
	{
		hasLoadedBundles = false;
		Glb.resetGame("User reloaded game to see new features");
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	protected bool isLoading
	{
		get
		{
			return downloadingLazyBundles.Count > 0;
		}
	}

	public static void resetStaticClassData()
	{
		downloadingLazyBundles = new List<string>();
		hasLoadedBundles = false;
	}

}
