using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class LoyaltyLoungeLobbyToaster : Toaster
{
	public ClickHandler clickHandler;
	public TextMeshPro incentiveLabel;

    public override string introAnimationName { get { return "intro"; } }
	
    public override string outroAnimationName { get { return "outro"; } }
	
	public override void init(ProtoToaster proto)
	{
		base.init(proto);
		if (lifetime > 0.0f)
		{
			runTimer = new GameTimer(lifetime);
		}		
		clickHandler.registerEventDelegate(onClick);
	   

		if (LinkedVipProgram.instance.incentiveCredits > 0)
		{
			string incentiveValue = CreditsEconomy.convertCredits(LinkedVipProgram.instance.incentiveCredits);
			incentiveLabel.text = Localize.text("loyalty_lounge_toaster_incentive", incentiveValue);
		}
		else
		{
			incentiveLabel.text = Localize.text("connect_to_loyalty_lounge_and_earn");
		}
		
		StatsManager.Instance.LogCount(
			counterName: "lobby",
			kingdom: "toaster",
			phylum: "loyalty_lounge",
			klass: "",
			family: "",
			genus: "view");
	}

	public void onClick(Dict args = null)
	{
	    close();
	    LinkedVipConnectDialog.showDialog(SchedulerPriority.PriorityType.IMMEDIATE);
	}

	private static bool hasShownThisSession = false;
	
    public static void registerToasterEvent()
	{
		GameEvents.onLevelUp -= checkForToaster;
		GameEvents.onLevelUp += checkForToaster;
	}

	private static void checkForToaster(int newLevel)
	{
		if (LinkedVipProgram.instance.isEligible &&
			!hasShownThisSession &&
			newLevel % 5 == 0 &&
			!LinkedVipProgram.instance.isConnected &&
			!LinkedVipProgram.instance.isPending)
		{
			hasShownThisSession = true;
			ToasterManager.addToaster(ToasterType.LOBBY_V3_LL, Dict.create());
		    if (Overlay.instance != null && Overlay.instance.top != null)
			{
				if (Overlay.instance.top is OverlayTopHIRv2)
				{
					((OverlayTopHIRv2)Overlay.instance.top).showLoyaltyLoungeBadge();
				}
			}
		}
	}
}
