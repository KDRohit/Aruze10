using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls display of mystery gift initial bet selection.
*/

public class MysteryGiftSelectBetDialog : MysteryGiftBaseSelectBetDialog
{
	public Renderer hotStreakTexture;
	public GameObject hyperEconomyIntroPrefab;

	// Initialization
	public override void init()
	{
		base.init();
		if (hotStreakTexture != null)
		{
			hotStreakTexture.gameObject.SetActive(false);	// Hide by default. Will be shown after texture is loaded.

			if (MysteryGift.isIncreasedMysteryGiftChance)
			{
				DisplayAsset.loadTextureToRenderer(hotStreakTexture, "misc_dialogs/mystery_gift_hot_streak.png");
			}
		}

		if (HyperEconomyIntroBet.shouldShow)
		{
			GameObject go = NGUITools.AddChild(sizer.gameObject, hyperEconomyIntroPrefab);
			CommonTransform.setZ(go.transform, -50.0f);	// Make sure it's in front of other stuff.
		}
	}
				
	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		if (hotStreakTexture != null && hotStreakTexture.material.mainTexture != null)
		{
			Destroy(hotStreakTexture.material.mainTexture);
		}
	}
}
