using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Tiny modificatio to Hot01FreespinModule that allows us to animate the sticky symbol animation on a loop
 * Author: Nick Reynolds
 */ 
public class Som01FreespinModule : Hot01FreespinModule 
{
	// loop through the sticky symbols and animate them over and over
	protected override IEnumerator loopAnimateAllStickySymbols()
	{
		while (this != null)
		{
			yield return new TIWaitForSeconds(STICKY_ANIM_LENGTH);
			foreach (SlotSymbol symbol in currentStickySymbols)
			{
				symbol.animateOutcome();
			}
		}
	}
}
