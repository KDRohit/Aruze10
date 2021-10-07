using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class STUDSaleDialog : DialogBase
{
	public Renderer background;						// Renderer of the background image.
	public TextMeshPro timerLabel;					// Label showing the time remaining for the sale. (optional)
	public FacebookFriendInfo friendInfo;			// Player's Facebook Info. (optional)
	public TextMeshPro playerVipStatusLabel;		// Player's VIP Status. (optional) (eg. Emerald Member)
	public TextMeshPro playerVipPointsLabel; 		// Player's current VIP Points. (optional)
	public Transform playerInfoTextSizer;			// Text sizer. (part of the friend info above)
	public STUDSaleOptionTMPro[] packageOptions;	// Components to manage the UI elements for each credit package.
	
	protected string creditPackagesLogString = "";
	protected bool shouldClose = false;
	protected STUDSale sale;
	protected const string BACKGROUND_IMAGE_FORMAT = "{0}/DialogBG.png";
	protected static bool failedDownload = false;
	
	void Update()
	{
		// Only set the timer label on Update if both the label, and the action timer exist.
		if (timerLabel != null &&
			sale.featureData != null &&
			sale.featureData.timerRange.isActive)
		{
			timerLabel.text = sale.featureData.timerRange.timeRemainingFormatted;
		}
		AndroidUtil.checkBackButton(rejectButtonClicked);
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		logViewStats();
		if (shouldClose)
		{
			// If something broke while initializing this dialog, then we want to close it right away.
			Dialog.close();
		}
	}

	protected void logViewStats()
	{
		if (sale != null)
		{
			StatsManager.Instance.LogCount("dialog", sale.kingdom, creditPackagesLogString, sale.saleName, "", "view");
		}
	}

	protected void logCloseStats(string classString, string family)
	{
		if (sale != null)
		{
			StatsManager.Instance.LogCount("dialog", sale.kingdom, "", classString, family, "click");
		}
	}
	
	public override void init()
	{
		if (SlotsPlayer.instance == null)
		{
			Debug.LogError("STUDSaleDialog: SlotsPlayer.instance is null... closing");
			shouldClose = true;
			return;
		}
		
		if (dialogArgs == null)
		{
			Debug.LogError("STUDSaleDialog: dialogArgs is null... closing");
			shouldClose = true;
			return;
		}

		sale = dialogArgs.getWithDefault(D.SALE, null) as STUDSale;
		
		if (sale == null)
		{
			Debug.LogError("STUDSaleDialog: Somehow managed to open this dialog without a sale... closing");
			shouldClose = true;
			return;
		}
		
		downloadedTextureToRenderer(background, 0); // Set the background texture to the downloaded image.

		MOTDFramework.markMotdSeen(dialogArgs);
		economyTrackingName = sale.kingdom;
		if (sale.featureData != null)
		{
			List <CreditPackage> packages = sale.featureData.creditPackages;

			if (packages == null || packageOptions == null || packages.Count != packageOptions.Length)
			{
				// Double checking that the STUD action and the dialog match before continuing.
				Debug.LogError("STUDSaleDialog: The STUDAction packages and the dialog packages do not match, cancelling dialog");
				shouldClose = true;
			}
			else
			{
				setPackages(packages);
				setLabels();
			}

			Audio.play("minimenuopen0");
		}
		else
		{
			Debug.LogError("STUDSaleDialog: STUDAction is null, this dialog should not have spawned.");
			shouldClose = true;
		}
	}
	
	protected virtual void setLabels()
	{
		// If this action has a timer, and the dialog has a label for the timer, then set it.
		if (sale.featureData.timerRange.isActive)
		{
			SafeSet.labelText(timerLabel, sale.featureData.timerRange.timeRemainingFormatted);
		}
				
		// The below is (currently) used for the VIP Sale Dialog Only.
				
		string statusLevelText;
		VIPLevel statusLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);
		if (statusLevel != null)
		{
			statusLevelText = statusLevel.name;
		}
		else
		{
			statusLevelText = "";
		}

		SafeSet.labelText(playerVipStatusLabel, Localize.text("vip_member_{0}", statusLevelText));
		SafeSet.labelText(playerVipPointsLabel, Localize.text("vip_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints)));

		if (friendInfo != null)
		{
			friendInfo.member = SlotsPlayer.instance.socialMember;
		}
		// End of VIP Sale only section.
	}

	protected virtual void setPackages(List<CreditPackage> packages)
	{
		List<PurchasePerksPanel.PerkType> perksToCycle = PurchasePerksPanel.getEligiblePerks(sale.featureData);
		PurchasePerksCycler cycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, ExperimentWrapper.BuyPageDrawer.maxItemsToRotate);
		// Setting all the packages used in this dialog.
		for (int i = 0; i < packages.Count; i++)
		{
			if (packageOptions.Length <= i || packageOptions[i] == null || packages[i] == null)
			{
				creditPackagesLogString += ((i == 0) ? "" : ",") + "error";
				continue;
			}
			packageOptions[i].setOption(packages[i], sale, perksToCycle, cycler);
			creditPackagesLogString += ((i == 0) ? "" : ",") + packages[i].purchasePackage.keyName;
		}
		
		cycler.startCycling();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		MainLobby.playLobbyMusic();
	}


	// NGUI Callback for the reject button.
    protected virtual void rejectButtonClicked()
	{
		Dialog.close();
	}
	
	// Static method to show the dialog.
	// Start to download the background texture.
	public static bool showDialog(STUDSale sale, string motdKey = "")
	{
		if (sale == null)
		{
			Debug.LogError("STUDSaleDialog -- sale is null");
			return false;
		}
		if (sale.featureData == null || sale.dialogTypeKey == "" || string.IsNullOrEmpty(sale.featureData.imageFolderPath))
		{
			Debug.LogError("Trying to show a sale dialog that is not properly setup.");
			return false;
		}
		if (ExperimentWrapper.SaleDialogLevelGate.isLockingSaleDialogs) //Don't show any sales dialogs if the player's level isn't high enough
		{
			Debug.Log(string.Format("Supressing sales dialog {0} due to level gate", sale.dialogTypeKey));
			return false;
		}
		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			Debug.Log(string.Format("Supressing sales dialog {0} due to First Purchase Offer being available", sale.dialogTypeKey));
			return false;
		}

		string backgroundPath = string.Format(BACKGROUND_IMAGE_FORMAT, sale.featureData.imageFolderPath);
		string dialogKey = sale.dialogTypeKey;
		
		if (sale.saleType == SaleType.POPCORN)
		{
			if (PopcornVariant.isActive)
			{
				// If we are in the new experiment to test popcorn variants, and this is a popcorn sale, then ignore the stud path and build a path from the EOS data.
		    	backgroundPath = PopcornVariant.currentBackgroundPath;
				dialogKey = PopcornVariant.currentDialogKey;
			}

			if (sale.featureData.timerRange == null || !sale.featureData.timerRange.isActive)
			{
				Debug.Log("Popcorn sale expired");
				return false;
			}
		}
		
		Dict args = Dict.create
		(
			D.SALE, sale,
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);

		// should we abort this dialog? or are we handling cases where downloads fail?
		bool shouldAbortOnFail = true;
		switch(sale.saleType)
		{
			case SaleType.POPCORN:
				shouldAbortOnFail = false;
				break;
		    // add more cases as needed
		}
		
		Dialog.instance.showDialogAfterDownloadingTextures(
		    dialogTypeKey: dialogKey,
		    textureUrl: backgroundPath,
			args: args,
			shouldAbortOnFail: shouldAbortOnFail,
			priorityType: SchedulerPriority.PriorityType.LOW,
			isExplicitPath: false,
			isPersistent: false,
			onDownloadFailed: onDownloadFailed
		);
		sale.markShown();
		return true;
	}

	public static void onDownloadFailed(string path, Dict data)
	{
		failedDownload = true;
	}
}
