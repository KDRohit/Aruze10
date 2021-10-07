using UnityEngine;
using System.Collections;

/*
 * LayeredGameIgnoreLayerAnticipationsModule.cs
 * This module is attached to Layered games so that they know to ignore layer-based anticipations, 
 * which are needed in Multi Games, but not other Layered Games.
 * author: Nick Reynolds
 */
public class LayeredGameIgnoreLayerAnticipationsModule : SlotModule 
{
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine (JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		ignoreAnticipationEffectsOnLayer(1); // for all of these games so far, it's 1.
	}

	/// Allows anticipation effects to be ignored for a specific layer
	protected virtual void ignoreAnticipationEffectsOnLayer(int layer)
	{
		ReelLayer reelLayer = (reelGame as LayeredSlotBaseGame).getReelLayerAt(layer);
		if (reelLayer != null)
		{
			foreach(SlotReel foregroundReel in reelLayer.getReelArray())
			{
				foregroundReel.shouldPlayAnticipateEffect = false;
			}
		}
	}
}
