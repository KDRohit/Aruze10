using UnityEngine;
using System.Collections;

// This module is used for setting the layer of bonus symbols so they animate on top of everything else during anticipation animations
// author: Nick Reynolds
public class UpdateLayerOnBonusSymbolLand : SlotModule 
{
	[SerializeField] private bool SHOULD_RESET_TO_SLOT_REELS_AFTER_ANIM;
	[SerializeField] private bool SHOULD_USE_SLOT_REELS_OVERLAY_LAYER;
	[SerializeField] private bool IGNORE_SCATTER_SYMBOLS;

	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return true;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		if (SHOULD_RESET_TO_SLOT_REELS_AFTER_ANIM)
		{
			CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS); 
		}
	}
	
	// executeOnSpecificReelStop() section
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (stoppingReel.isAnticipationReel())
		{
			return true;
		}

		return false;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (!(reelGame.engine.isSlamStopPressed && SHOULD_RESET_TO_SLOT_REELS_AFTER_ANIM))
		{
			foreach(SlotSymbol symbol in stoppingReel.visibleSymbols)
			{
				if (symbol.isBonusSymbol || (symbol.isScatterSymbol && !IGNORE_SCATTER_SYMBOLS) || symbol.isJackpotSymbol) // every bonus symbol that's visible should animate (can't get bonus symbols that aren't part of it)
				{
					if (SHOULD_USE_SLOT_REELS_OVERLAY_LAYER)
					{
						CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS_OVERLAY); // SLOT_REELS_OVERLAY seems to work well					
					}
					else
					{
						CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_OVERLAY); // SLOT_REELS_OVERLAY seems to work well
					}
				}
			}
		}

	
		yield break;
	}

	public override bool needsToExecuteChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{
		return SHOULD_RESET_TO_SLOT_REELS_AFTER_ANIM;
	}
	
	public override void executeChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{
		if (symbol.isBonusSymbol || (symbol.isScatterSymbol && !IGNORE_SCATTER_SYMBOLS) || symbol.isJackpotSymbol)
		{
			CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS);
		}
	}
}
