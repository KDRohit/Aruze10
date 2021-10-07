using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles HIR specific version of this dialog.
*/

public class IncreaseMysteryGiftChanceMOTDHIR : IncreaseMysteryGiftChanceMOTD
{
	public static string HOT_STREAK_LOGO_PATH = "misc_dialogs/mystery_gift/mystery_gift_hot_streak.png";
	
	public Renderer hotStreakTexture;
	public Renderer gameTexture;
	[SerializeField] private GameObject decoratorAnchor;

	public override void init()
	{
		downloadedTextureToRenderer(hotStreakTexture, 0);
		downloadedTextureToRenderer(gameTexture, 1);
		MysteryGiftLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
		base.init();
	}

}
