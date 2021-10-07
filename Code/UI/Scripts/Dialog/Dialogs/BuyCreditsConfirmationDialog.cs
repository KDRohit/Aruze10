using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class BuyCreditsConfirmationDialog : DialogBase
{
	public TextMeshPro creditsLabel;
	
	public TextMeshPro vipPointsLabel; // Bonus VIP Points Label for New VIP.

	public static int vipNewLevelForPurchase = 0;	// Remember the VIP level when making a purchase, to use on this confirmation screen.

	protected long totalCredits = 0L;
	protected long bonusCredits = 0L;
	protected int vipPoints = 0;
    protected JSON data = null;

	public static System.Action onClose;

	/// Initialization
	public override void init()
	{
		//Enable controls, as it was disabled while making the asyc call from BuyCreditsOption.
		NGUIExt.enableAllMouseInput();
		
		data = dialogArgs.getWithDefault(D.DATA, null) as JSON;

		bonusCredits = (long)dialogArgs.getWithDefault(D.BONUS_CREDITS, 0);
		totalCredits = (long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0);
		vipPoints = (int)dialogArgs.getWithDefault(D.VIP_POINTS, 0);
	
		string packageKey = (string)dialogArgs.getWithDefault(D.PACKAGE_KEY, "");
		StatsManager.Instance.LogCount(
			"dialog",
			"buy_credits_confirm",
			packageKey,
			StatsManager.getGameTheme(),
			StatsManager.getGameName(),
			"view"
		);

		if ((bool)dialogArgs.getWithDefault(D.IS_JACKPOT_ELIGIBLE, false))
		{
			StatsManager.Instance.LogCount("dialog", "buy_credits_confirm", packageKey, "jackpot_days", "", "view");
		}

		if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			ExperimentWrapper.FirstPurchaseOffer.didPurchase = true;
			PurchasablePackage purchasedPackage = PurchasablePackage.find(packageKey);
			if (purchasedPackage != null)
			{
				StatsManager.Instance.LogCount(counterName: "dialog", 
					kingdom: "buy_page_v3", 
					phylum:"purchase", 
					klass: "first_purchase_offer",
					family: "First Purchase Offer",
					genus: purchasedPackage.getLocalizedPrice());
			}
		}

		SlotBaseGame.logOutOfCoinsPurchaseStat(true);

		if(Overlay.instance != null && Overlay.instance.topHIR != null)
		{
			Overlay.instance.topHIR.setupSaleNotification();
		}

		string eventId = "";
		if (data != null)
		{
			eventId = data.getString("event", "");
		}
	}

	public static void ratingPrompt()
	{
		if (GameState.isMainLobby)
		{
			RateMe.checkAndPrompt(RateMe.RateMeTrigger.PURCHASE);
		}
		else
		{
			RateMe.pendingPurchasePrompt = true;
		}
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public void okClicked()
	{
		closeDialog();
	}
	
	public void closeClicked()
	{
		closeDialog();
	}
	
	protected void closeDialog()
	{
		Dialog.close();
		ratingPrompt();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
		if (onClose != null)
		{
			onClose();
		}
	}
	
	public static void showDialog(Dict args)
	{
		string dialogKey = "buy_credits_confirm_new";

		// this must pass true for showImmediately so it jumps to the front of the line in the todo list and the purchase flow is
		// not interrupted, otherwise other feature dialogs that award credits may jump ahead of it and then when credits are added for
		// the purchase some desync protection code will calculate creditsDifference and take the credits away causing a desync. 
		Scheduler.addDialog(dialogKey, args, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
