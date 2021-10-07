using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls UI behavior of a menu option button related to PPU
*/

public class LobbyOptionButtonPPU : LobbyOptionButton
{
	public static LobbyOptionButtonPPU instance;
	public TextMeshPro timerText;
	bool canClick = true;
	public UITexture texture;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		refresh();
		instance = this;

		if (CampaignDirector.partner != null && CampaignDirector.partner.timerRange.isActive)
		{
			CampaignDirector.partner.timerRange.registerLabel(timerText);
			CampaignDirector.partner.timerRange.registerFunction(onEventEnd);
		} 
		else
		{
			canClick = false;
			timerText.text = Localize.text ("event_over");
		}

		texture.material.shader = LobbyOptionButtonActive.getOptionShader(true);
	}

	private void onEventEnd(Dict args = null, GameTimerRange sender = null)
	{
		canClick = false;
		// Just in case
		if (timerText != null)
		{
			timerText.text = Localize.text("event_over");
		}
	}

	protected override void OnClick()
	{
		if (canClick)
		{
			StatsManager.Instance.LogCount(counterName: "lobby", kingdom: "co_op_challenge", genus: "click");
			base.OnClick();	
		}
	}

	public override void refresh()
	{
		base.refresh();
	}
}