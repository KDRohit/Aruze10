using UnityEngine;
using System.Collections;

/**
Base game for Ani04 African Thunder

Original Author: Scott Lepthien
*/
public class Ani04 : SlotBaseGame 
{
	[SerializeField] private WildOverlayTransformModule wildOverlayTransformModule = null;	// Module that controls the wild overlay effects for this game

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (wildOverlayTransformModule.doWildReplacement && serverName == wildOverlayTransformModule.mutationTarget)
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(serverName, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
			newSymbolAnimator.showWild();
		}
		else
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		}
		
		return newSymbolAnimator;
	}
}
