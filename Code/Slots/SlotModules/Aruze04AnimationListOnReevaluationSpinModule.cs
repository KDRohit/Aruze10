using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class for playing an AnimationListController list for reevaulation spins
Originally built for the frame animation in aruze04 Goddesses Hera
This is a version which extends from AnimationListOnReevaluationSpinModule
which also resets the symbol animation to the intro state when the feature 
starts

Creation Date: February 6, 2018
Original Author: Scott Lepthien
*/
public class Aruze04AnimationListOnReevaluationSpinModule : AnimationListOnReevaluationSpinModule 
{
	public override IEnumerator executeOnReevaluationPreSpin()
	{
		// Change the middle mega symbol back to the intro version if it isn't that version
		// (because it was animated)
		SlotReel megaSymbolReel = reelGame.engine.getSlotReelAt(1, -1, 1);
		SlotSymbol megaSymbol = megaSymbolReel.visibleSymbols[0];

		if (megaSymbol.name.Contains("_Anim2"))
		{
			string introSymbolName = megaSymbol.name.Replace("_Anim2", "");
			megaSymbol.mutateTo(introSymbolName, null, true, true);
		}
		
		yield return StartCoroutine(base.executeOnReevaluationPreSpin());
	}
}
