using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Module to force symbols to all be on the layer ID_SLOT_REELS when they are released into the symbol cache
this should ensure that all symbols can be reused in different game types.  This module may get moved into
more core code if testing reveals it is safe to do so, but for now is made as a fix for gwtw01 which is a 4-up
base game with a standard freespins game which is resulting in cached symbols being used for the wrong layer in freespins

Author: Scott Lepthien
Creation Date: 9/18/2018
*/
public class UpdateSymbolLayersToReelLayerOnReleaseSymbol : SlotModule 
{
// executeOnReleaseSymbolInstance() section
// functions in this section are accessed by ReelGame.releaseSymbolInstance
	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return true;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		if (animator != null)
		{
			// restore the animator to the reel layer
			CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
		}
	}
}
