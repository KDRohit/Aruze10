using Com.HitItRich.EUE;
using Com.Scheduler;
using System.Collections;
using UnityEngine;

public class IncentivizedSoftPromptDialog : SoftPromptDialog, IResetGame
{
	[SerializeField] private Transform coinStartPos;
	public static bool awaitingIncentivePNState { get; private set; }

	private static int? _creditAmount;

	private static IncentivizedSoftPromptDialog instance;
	
	public static int creditAmount
	{
		get
		{
			if (!_creditAmount.HasValue)
			{
				_creditAmount = Data.login.getInt("incentivized_pn_reward_amount", 0);
			}
			return _creditAmount.Value;
		}
	}
	
	[SerializeField] private LabelWrapperComponent coinAmountLabel; 
	public static void showDialog(Com.Scheduler.SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH, bool isFromFtue = false)
	{
		//set ftue data
		launchedFromFtue = isFromFtue;
		//log view
		StatsManager.Instance.LogCount("dialog", "incent_pn_dialog", isFromFtue ? "lobby_ftue" : "hourly_bonus", "incent", "", "view");
		//show
		Scheduler.addDialog("ios_incentivized_soft_prompt", null, priority);
	}
	
	protected override bool shouldCloseDialogOnAccept
	{
		get
		{
			return false;
		}
	}

	public override void init()
	{
		base.init();
		coinAmountLabel.text = CreditsEconomy.convertCredits(creditAmount);
		instance = this;

	}
	
	protected override void logClickYes()
	{
		StatsManager.Instance.LogCount("dialog", "incent_pn_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "incent", "yes", "click");
		StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue? "lobby_ftue" : "hourly_bonus", "incent", "", "view");
	}
	
	protected override void logClickNo()
	{
		StatsManager.Instance.LogCount("dialog", "incent_pn_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "incent", "skip", "click");
	}

	protected override void setAwaitingPushNotifState()
	{
		awaitingIncentivePNState = true;
	}
	
	public static void onEnableFromPrompt()
	{
		if (!awaitingIncentivePNState)
		{
			return;
		}
		
		awaitingIncentivePNState = false;

		// If the notifs are enabled (after not being enabled)
		if (NotificationManager.DevicePushNotifsEnabled)
		{
			NotificationManager.RegisteredForPushNotifications = true;
			// Grant the incentive if eligible
			if (ExperimentWrapper.PushNotifSoftPrompt.isIncentivizedPromptEnabled)
			{
				//DO Action;
				IncentivizedPushNotificationAction.grantIncentive();

				if (instance != null)
				{
					instance.StartCoroutine(instance.coinFly());
				}
				else
				{
					//add credits
					SlotsPlayer.addNonpendingFeatureCredits(creditAmount, "Incentive PN Enable");	
				}
				
			}
			
			StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "incent", "yes", "click");
		}
		else
		{
			if (ExperimentWrapper.PushNotifSoftPrompt.isIncentivizedPromptEnabled)
			{
				StatsManager.Instance.LogCount("dialog", "pn_system_dialog", launchedFromFtue ? "lobby_ftue" : "hourly_bonus", "incent", "no", "click");
			}	
			
			if (instance != null)
			{
				Dialog.close(instance);
			}
		}
	}
	
	public static void resetStaticClassData()
	{
		awaitingIncentivePNState = false;
		_creditAmount = null;
		instance = null;
	}
	
	public override void close()
	{
		instance = null;
		base.close();
	}
	
	protected IEnumerator coinFly()
	{
		if (Overlay.instance.topV2 == null)
		{
			Dialog.close();
			yield break;
		}

		//prevent clicks
		closeHandler.enabled = false;
		okayHandler.enabled = false;
		notNowHandler.enabled = false;
		
		
		// Create the coin as a child of "sizer", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScriptUpdated  coin = CoinScriptUpdated.create(
			sizer,
			coinStartPos.position,
			new Vector3(0, 0, -100)
		);

		Audio.play("initialbet0");
		
		Vector3 overlayCoinPosition = Overlay.instance.topV2.coinAnchor.position;
		//note that this is from a different camera, so we have to convert to screen from world position back to screen position
		//Find the ngui camera
		int layerMask = 1 << Overlay.instance.topV2.gameObject.layer;
		Camera nguiCamera = CommonGameObject.getCameraByBitMask(layerMask);

		//calculate the screen position based on viewing camera and world position
		Vector2int destination = NGUIExt.screenPositionOfWorld(nguiCamera, overlayCoinPosition);
		
		//convert from ngui to unity vector 2
		Vector2 endDestination = new Vector2(destination.x, destination.y);
		
		//fly, you're free!
		yield return StartCoroutine(coin.flyTo(endDestination));
		
		//add credits
		SlotsPlayer.addNonpendingFeatureCredits(creditAmount, "Incentive PN Enable");	
		
		coin.destroy();
		Dialog.close();
	}

}
