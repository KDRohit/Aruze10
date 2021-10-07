using System.Collections;
using UnityEngine;

public class SetupBingoSymbolModule : SlotModule
{
	//Module to handle setting the label on bingo symbols in a game that uses them

	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		//If we have a SL# symbol then we need to grab the # value and set the symbols label to it.
		if (symbol.isBingoSymbol)
		{
			return true;
		}
		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		string symbolNumber = symbol.name.Replace(SlotSymbol.BINGO_SYMBOL_BASE_NAME, "");
		SymbolAnimator symbolAnimator = symbol.getAnimator();
		if (symbolAnimator != null)
		{
			LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();
			if (symbolLabel != null)
			{
				symbolLabel.text = symbolNumber;
			}
		}
	}
}
