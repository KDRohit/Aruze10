
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles the portal for the new VIP lobby revamp
*/

public class LobbyOptionButtonMaxVoltagePortal : LobbyOptionButton
{
	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro jackpotLabel;
	[SerializeField] private GenericLobbyPortalV4 maxVoltagePortal;

	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		maxVoltagePortal.setup(SlotsPlayer.instance.socialMember.experienceLevel < Glb.MAX_VOLTAGE_MIN_LEVEL, false, false);
		updateJackpotLabel();
	}

	public void updateJackpotLabel()
	{
		if (jackpotLabel != null && ProgressiveJackpot.maxVoltageJackpot != null)
		{
			ProgressiveJackpot.maxVoltageJackpot.registerLabel(jackpotLabel);
		}
	}

	protected override void OnClick()
	{
		maxVoltagePortal.enterRoomClicked(null);
	}
}
