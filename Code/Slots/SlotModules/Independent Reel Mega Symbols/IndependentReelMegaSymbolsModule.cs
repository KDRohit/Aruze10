using UnityEngine;
using System.Collections;

public class IndependentReelMegaSymbolsModule : SlotModule 
{
	[SerializeField] protected Animator megaReelFire2x2 = null;
	[SerializeField] protected Animator megaReelFire3x3 = null;
	// Consts
	[SerializeField] private string MEGA_2X2_ANIMATION;
	[SerializeField] private string MEGA_3X3_ANIMATION;

	[SerializeField] private Vector3 MEGA_2X2_SCALE;
	[SerializeField] private Vector3 MEGA_3X3_SCALE;
	
	[SerializeField] private Vector3 MEGA_2X2_OFFSET;
	[SerializeField] private Vector3 MEGA_3X3_OFFSET;
	
	private const string MEGA_SYMBOL_INIT_MAPPING = "mega_reel_init";

	private ReelLayer[] reelLayers;

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		if (reelLayers == null)
		{
			reelLayers = (reelGame.engine as SlidingSlotEngine).reelLayers;
			// Make the top layer into a MegaReelLayer, idealy this would be data driven.
			reelLayers[1] = new MegaReelLayer(reelLayers[1]);
		}

		MegaReelLayer megaReelLayer = reelLayers[1] as MegaReelLayer;
		if (megaReelLayer != null)
		{
			// We want to move the mega reel layer to where it should be.
			megaReelLayer.toggleReels(false);
		}
		yield break;
	}

	public override bool needsToExecuteOnReelsSlidingCallback()
	{
		return true;
	}
	
	public override IEnumerator executeOnReelsSlidingCallback()
	{
		if (reelLayers.Length > 1)
		{
			MegaReelLayer megaReelLayer = reelLayers[1] as MegaReelLayer;
			if (megaReelLayer != null && megaReelLayer.height == 2 || megaReelLayer.height == 3)
			{
				// We want to move the mega reel layer to where it should be.
				Audio.play(Audio.soundMap(MEGA_SYMBOL_INIT_MAPPING));
				// Play the VFX.
				if (megaReelLayer.height == 2)
				{
					megaReelLayer.moveLayer(MEGA_2X2_OFFSET);
					if (megaReelFire2x2 != null)
					{
						megaReelFire2x2.Play(MEGA_2X2_ANIMATION);
						megaReelFire2x2.transform.position = megaReelLayer.parent.transform.position;
					}
					megaReelLayer.parent.transform.localScale = MEGA_2X2_SCALE;
				}
				else if (megaReelLayer.height == 3)
				{
					megaReelLayer.moveLayer(MEGA_3X3_OFFSET);
					if (megaReelFire3x3 != null)
					{
						megaReelFire3x3.Play(MEGA_3X3_ANIMATION);
						megaReelFire3x3.transform.position = megaReelLayer.parent.transform.position;
					}
					megaReelLayer.parent.transform.localScale = MEGA_3X3_SCALE;
				}
				else
				{
					Debug.LogWarning("No Effect to play for mega_reels.");
				}
				megaReelLayer.parent.SetActive(false);
				yield return new TIWaitForSeconds(0.5f);
				megaReelLayer.parent.SetActive(true);
			}
		}
	}
}
