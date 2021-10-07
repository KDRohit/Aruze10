using UnityEngine;
using Com.Rewardables;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class BuyCreditsConfirmationDialogNewHIR : BuyCreditsConfirmationDialog
{
	public TextMeshPro messageLabel;
	public GameObject creditSweepstakesElements;

	public static int elitePointsGranted = 0;

	[SerializeField] private PurchasePerksPanel perksPanel;
	
	private long creditsBeforePurchase;
	private const string VIP_TEXT = "vip_purchase_points_{0}";
	private const string VIP_BOOST_TEXT = "vip_purchase_boost_{0}_{1}";
	private const string ELITE_TEXT = "elite_purchase";

	[SerializeField] private ButtonHandler collectHandler;
	
	public static void initDependency()
	{
		RewardablesManager.addEventHandler(onRewardGranted);	
	}

	public static void onRewardGranted(Rewardable rewardable)
	{
		switch (rewardable.type)
		{
			case RewardElitePassPoints.TYPE:
			{
				RewardElitePassPoints elitePassPoints = rewardable as RewardElitePassPoints;
				if (elitePassPoints != null)
				{
					elitePointsGranted = elitePassPoints.points;
				}
			} 
			break;
		}
		
	}
	
	/// Initialization
	public override void init()
	{
		if (ExperimentWrapper.BuyPageDrawer.isInExperiment)
		{
			string packageKey = (string)dialogArgs.getWithDefault(D.PACKAGE_KEY, "");
			PurchaseFeatureData.Type purchaseType = (PurchaseFeatureData.Type) dialogArgs.getWithDefault(D.TYPE, PurchaseFeatureData.Type.NONE);
			RewardPurchaseOffer purchaseOffer = (RewardPurchaseOffer) dialogArgs.getWithDefault(D.PAYLOAD, null);
			if (!string.IsNullOrEmpty(packageKey))
			{
				messageLabel.gameObject.SetActive(false);
				perksPanel.gameObject.SetActive(true);
				perksPanel.initConfirmationPerks(purchaseType, packageKey, purchaseOffer);
			}
		}
		
		base.init();
		
		Audio.play("CelebrateBuyCoins");
		
		if (EliteManager.isActive && elitePointsGranted > 0)
		{
			messageLabel.text = Localize.text(ELITE_TEXT, vipPoints.ToString(), elitePointsGranted);
			
			//fetch latest inbox items for elite member here so when the inbox opens the latest data is present
			if (EliteManager.hasActivePass)
			{
				InboxAction.getInboxItems();
			}
			
		}
		else if (ExperimentWrapper.VIPLevelUpEvent.isInExperiment && VIPStatusBoostEvent.isEnabled())
		{
			messageLabel.text = Localize.text(VIP_BOOST_TEXT, CreditsEconomy.convertCredits(getBoostedCreditsAmount()), vipPoints.ToString());
		}
		else
		{
			messageLabel.text = Localize.text(VIP_TEXT, vipPoints.ToString());
		}
		creditsLabel.text = CreditsEconomy.convertCredits((long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0));
		
		creditSweepstakesElements.SetActive(CreditSweepstakes.isActive);
		collectHandler.registerEventDelegate(collectClicked);
	}

	private void collectClicked(Dict args = null)
	{
		closeDialog();
		if (EliteManager.isActive)
		{
			elitePointsGranted = 0;

			//Show the inbox after purchase only if elite was already active and not activated from
			//this particular purchase.
			if (EliteManager.hasActivePass && !EliteManager.showLobbyTransition)
			{
				InboxDialog.showDialog(InboxDialog.MESSAGES_STATE);
			}
		}
	}

	private long getBoostedCreditsAmount()
	{
		long baseCredits = (long)dialogArgs.getWithDefault(D.BASE_CREDITS, 0);
		int salePercent = (int) dialogArgs.getWithDefault(D.SALE_BONUS_PERCENT, 0);
		int bonusPercent = (int) dialogArgs.getWithDefault(D.BONUS_PERCENT, 0);
		int nonBoostedVip = VIPLevel.find(vipNewLevelForPurchase).purchaseBonusPct;

		long nonBoostedAmount = PurchasablePackage.getTotalPackageAmount(baseCredits, bonusPercent, salePercent, nonBoostedVip); //Need to do the full calculation on how much the non-boosted VIP bonus would pay out
		return totalCredits - nonBoostedAmount;
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
	}
}
