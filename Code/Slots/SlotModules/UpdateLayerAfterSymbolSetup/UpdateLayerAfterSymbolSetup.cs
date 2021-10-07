using UnityEngine;
using System.Collections;

public class UpdateLayerAfterSymbolSetup : SlotModule 
{
	[SerializeField] private string symbolName = "";
	[SerializeField] private Layers.LayerID layer = Layers.LayerID.ID_SLOT_REELS;
	
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		SymbolAnimator symbolAnimator = symbol.getAnimator();
		
		if (symbolAnimator != null && symbolAnimator.symbolInfoName == symbolName)
		{
			return true;
		}
		
		return false;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		SymbolAnimator symbolAnimator = symbol.getAnimator();
		CommonGameObject.setLayerRecursively(symbolAnimator.gameObject, (int)layer); 
	}
}
