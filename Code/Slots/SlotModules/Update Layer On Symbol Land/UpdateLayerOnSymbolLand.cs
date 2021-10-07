using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Based on the 'UpdateLayerOnBonusSymbolLand' module but can be used for any arbitrary symbol.
public class UpdateLayerOnSymbolLand : SlotModule 
{
	[SerializeField] private string symbolName = "";
	[SerializeField] private bool shouldResetSymbolAfterAnimation = false;
	[SerializeField] private bool shouldExecuteOnlyOnAnticipationReel = false;
	[SerializeField] private bool shouldIgnoreScatterSymbols = false;
	[SerializeField] private int layerIndex = 0;

	private int previousLayer = 21;		// The default layer for symbols (SLOT_REEL)
	private Dictionary<GameObject, Dictionary<Transform, int>> gameObjectToLayerRestoreMap = new Dictionary<GameObject, Dictionary<Transform, int>>();

	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return true;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		if (shouldResetSymbolAfterAnimation && animator.symbolInfoName == symbolName)
		{
			animator.stopAnimation();
			revertGameObjectLayer(animator.gameObject);
		}
	}
	
	// executeOnSpecificReelStop() section
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (!shouldExecuteOnlyOnAnticipationReel)
		{
			return true;
		}
		
		if (stoppingReel.isAnticipationReel())
		{
			return true;
		}

		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		if (!(reelGame.engine.isSlamStopPressed && shouldResetSymbolAfterAnimation))
		{
			foreach (SlotSymbol symbol in stoppingReel.visibleSymbols)
			{
				string testName = symbolName;

				if (symbol.isFlattenedSymbol)
				{
					testName += SlotSymbol.FLATTENED_SYMBOL_POSTFIX;
				}

				if (symbol.name.Equals(testName) || (symbol.isScatterSymbol && !shouldIgnoreScatterSymbols)) // every bonus symbol that's visible should animate (can't get bonus symbols that aren't part of it)
				{
					setGameObjectLayer(symbol.gameObject, layerIndex);
				}
			}
		}

		yield break;
	}

	public override bool needsToExecuteChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{
		return shouldResetSymbolAfterAnimation;
	}
	
	public override void executeChangeSymbolLayerAfterSymbolAnimation(SlotSymbol symbol)
	{
		if (shouldResetSymbolAfterAnimation)
		{
			symbol.haltAnimation(); // Just making sure that it's been halted.
			revertGameObjectLayer(symbol.gameObject);
		}
	}

	public override bool needsToExecuteOnPreSpinNoCoroutine()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		if (shouldResetSymbolAfterAnimation)
		{
			foreach (SlotSymbol symbol in reelGame.engine.getAllVisibleSymbols())
			{
				if (symbol.name.Contains(symbolName))		// Handles the case where symbols are flattened or are outcome versions of the symbol
				{
					symbol.haltAnimation();
					revertGameObjectLayer(symbol.gameObject);
				}
			}
		}
		gameObjectToLayerRestoreMap.Clear();
	}

	private void setGameObjectLayer(GameObject go, int layer)
	{
		if (gameObjectToLayerRestoreMap.ContainsKey(go))
		{
			// Already here so we will just update it.
			gameObjectToLayerRestoreMap[go] = CommonGameObject.getLayerRestoreMap(go);
		}
		else
		{
			gameObjectToLayerRestoreMap.Add(go, CommonGameObject.getLayerRestoreMap(go));
		}
		previousLayer = go.layer;
		CommonGameObject.setLayerRecursively(go, layer);
	}

	private void revertGameObjectLayer(GameObject go)
	{
		if (gameObjectToLayerRestoreMap.ContainsKey(go))
		{
			CommonGameObject.restoreLayerMap(go, gameObjectToLayerRestoreMap[go]);
		}
		else
		{
			CommonGameObject.setLayerRecursively(go, previousLayer); 
		}
	}
}
