using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SplitLargeSymbolsUnderBonusSymbolModule : SlotModule
{
	[SerializeField] private CommonDataStructures.SerializableDictionaryOfIntToInt bonusLayers; //Key is the layer of the reel that lands. Value is the layer that we want to check for tall symbols on
	[SerializeField] private int bonusReel;

	private SlotSymbol landedBonusSymbol = null;
	private int bonusSymbolRow = 0; //Used to keep track of what row our BN symbol is on when it lands

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (bonusLayers.ContainsKey(stoppedReel.layer) && bonusReel == stoppedReel.reelID)
		{
			bonusSymbolRow = 0;
			foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(stoppedReel.reelID-1, stoppedReel.layer))
			{
				if (symbol.isBonusSymbol)
				{
					landedBonusSymbol = symbol;
					return true;
				}
				bonusSymbolRow++;
			}
		}

		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		Vector2 symbolDimension = landedBonusSymbol.getWidthAndHeightOfSymbol();
		for (int i = 0; i < symbolDimension.x; i++)
		{
			SlotSymbol[] visibleSymbols = reelGame.engine.getSlotReelAt(stoppedReel.reelID + i, -1, bonusLayers[stoppedReel.layer]).visibleSymbols;
			SlotSymbol symbolToCheck = visibleSymbols[bonusSymbolRow];

			if (symbolToCheck.isTallSymbolPart && symbolToCheck.canBeSplit())
			{
				symbolToCheck.splitSymbol();
			}

			if ((bonusSymbolRow + symbolDimension.x - 1) < visibleSymbols.Length)
			{
				symbolToCheck = visibleSymbols[bonusSymbolRow + (int)symbolDimension.x - 1];
				if (symbolToCheck.isTallSymbolPart && symbolToCheck.canBeSplit())
				{
					symbolToCheck.splitSymbol();
				}
			}
		}
		yield break;
	}


}
