using Com.Scheduler;
using UnityEngine;

public class PremiumSlicePurchaseDialog : DialogBase
{
	private const string headerLocKey = "premium_slice_header";
	private const string bodyLocKey = "premium_slice_body_{0}";
	
	[SerializeField] private ButtonHandler purchaseButton;
	[SerializeField] private LabelWrapperComponent creditLabel;
	[SerializeField] private LabelWrapperComponent headerLabel;
	[SerializeField] private LabelWrapperComponent bodyLabel;
	private bool offerRejected = false;
	
	public override void init()
	{
		//set win amount
		creditLabel.text = PremiumSlice.sliceCreditValue;
		//set header
		headerLabel.text = Localize.text(headerLocKey);
		//set body text
		bodyLabel.text = Localize.text(bodyLocKey, PremiumSlice.sliceCost);
		//register purchase function
		purchaseButton.registerEventDelegate(onPurchasClicked);
		
		StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"premium_wheel", phylum:"nag_screen", genus:"view");
	}

	public override void close()
	{
	}
	
	public override void onCloseButtonClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"premium_wheel", phylum:"nag_screen", genus:"click", family:"close");
		purchaseButton.unregisterEventDelegate(onPurchasClicked);
		DialogBase dailySpinDialog = Dialog.instance.findOpenDialogOfType("new_daily_bonus");
		if (dailySpinDialog != null)
		{
			Dialog.close(dailySpinDialog);
		}
		base.onCloseButtonClicked(args);
	}
	
	public static void showDialog()
	{
		Scheduler.addDialog("premium_slice_purchase", null, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private void onPurchasClicked(Dict args)
	{
		Audio.play(NewDailyBonusDialog.PURCHASE_PREMIUM_WHEEL_SOUND);
		StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"premium_wheel", phylum:"nag_screen", genus:"click", family:"purchase");
		purchaseButton.enabled = false;
		PremiumSlice.purchasePremiumSlice();
	}
}
