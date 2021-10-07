using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls UI behavior of a menu option button in the main lobby.
*/

public class LobbyOptionButtonMultiProgressiveHIR : LobbyOptionButtonMultiProgressive
{
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
	
		option.game.registerMultiProgressiveLabels(jackpotLabels, false);
	}
}
