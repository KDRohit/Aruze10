
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles the portal for the new VIP lobby revamp
*/

public class LobbyOptionButtonVIPPortal : LobbyOptionButton
{
	// =============================
	// PUBLIC
	// =============================

	public TextMeshPro grandJackpotLabel;
	[SerializeField] private VIPRoomBottomButton vipPortal;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		updateJackpotLabel();
	}

	public void updateJackpotLabel()
	{
		if (grandJackpotLabel != null && ProgressiveJackpot.vipRevampGrand != null)
		{
			ProgressiveJackpot.vipRevampGrand.registerLabel(grandJackpotLabel);
		}
	}

	protected override void OnClick()
	{
		vipPortal.triggerClick();
		StatsManager.Instance.LogCount("lobby", "", "vip_banner", "", "vip_room", "click");
	}
}