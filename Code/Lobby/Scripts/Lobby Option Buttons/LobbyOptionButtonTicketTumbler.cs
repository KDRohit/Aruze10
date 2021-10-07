using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls UI behavior of a menu option button related to PPU
*/

public class LobbyOptionButtonTicketTumbler : LobbyOptionButton
{
	public static LobbyOptionButtonTicketTumbler instance;
	public TextMeshPro timerText;
	public TextMeshPro creditsText;
	public UITexture texture;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		refresh();
		instance = this;

		if (TicketTumblerFeature.instance.roundEventTimer != null)
		{
			TicketTumblerFeature.instance.roundEventTimer.registerLabel(timerText);
		}

		if (TicketTumblerFeature.instance.featureTimer != null)
		{
			Dict args = Dict.create(D.DATA, timerText);			
			TicketTumblerFeature.instance.featureTimer.registerFunction(onEventEnd, args);
		}

		texture.material.shader = LobbyOptionButtonActive.getOptionShader(true);

		SafeSet.labelText(creditsText, CreditsEconomy.convertCredits(TicketTumblerFeature.instance.eventPrizeAmount));
	}

	// this is static so there is only one instance ever passed to eventTimer.registerFunction
	private static void onEventEnd(Dict args = null, GameTimerRange sender = null)
	{
		if (args != null)
		{
			TextMeshPro textArg = args[D.DATA] as TextMeshPro;
			if (textArg != null)
			{
				textArg.text = Localize.text("event_over");
			}
		}
	}

	protected override void OnClick()
	{
		if (TicketTumblerFeature.instance.featureTimer != null && !TicketTumblerFeature.instance.featureTimer.isExpired)
		{
			StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_motd", klass:"banner", genus:"view");
			base.OnClick();
		}
	}

	public override void refresh()
	{
		base.refresh();
	}
}