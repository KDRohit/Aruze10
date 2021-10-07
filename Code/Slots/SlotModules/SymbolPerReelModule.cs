using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * SlotModule to handle when we need different variations of a symbol depending which reel it lands on.
 * Backend is not aware of this need, it will send the symbol down as its normal label F5, M1, BN, etc.
 * Client will rename by adding the SlotSymbol.SYMBOL_VARIANT_POSTFIX ( "_Variant" ) plus reelID to the symbol name
 * F5_Variant1, F5_Variant2, F5_Variant3, F5_Variant4, etc.
 * And mutating to the new symbol name
 * 
 * Original Author: Carl Gloria
 * Date Created: 4/24/2019
 */

public class SymbolPerReelModule : SlotModule 
{
	[Tooltip("List of symbol names that need a unique symbol per reel, will rename to something like F5_Variant1, F5_Variant2, etc. and mutate to new symbol name")]
	[SerializeField] private List<string> symbolNames;

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return symbolNames != null && symbolNames.IndexOf(symbol.serverName) > -1;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		string newName = symbol.serverName + SlotSymbol.SYMBOL_VARIANT_POSTFIX + symbol.reel.reelID;

		if (symbol.name != newName)
		{
			symbol.mutateTo(newName);
		}
	}
}