using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Created to serve as a base class for modules that want to do a swap between Standard and Independent
 * Reels.  The original module that now extends from this SwapNormalToIndependentReelTypesOnReevalModule
 * was written to handle the swap on a reeval.  But for gen97 Cash Tower we want the swap to occur at a different
 * time, so we need to make a module that can perform the swap differently, but should be able to share the basic
 * setup that the original swap module was using.
 *
 * Creation Date: 2/23/2020
 * Original Author: Scott Lepthien
 */
public class SwapNormalToIndependentReelTypesBaseModule : SlotModule
{
	protected const int LAYER_INDEX_NORMAL_REELS = 0;
	protected const int LAYER_INDEX_INDEPENDENT_REELS = 1;

	protected bool isUsingIndependentReels = false;
	protected LayeredSlotEngine layeredEngine;

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		// Force the base reel layer only to be shown (and others to be hidden)
		layeredEngine = reelGame.engine as LayeredSlotEngine;

		if (layeredEngine == null)
		{
#if UNITY_EDITOR
			Debug.LogError("SwapNormalToIndependentReelTypesModule.executeOnSlotGameStartedNoCoroutine() - Game this is attached to was not using a LayeredSlotEngine!");
#endif
		}
		
		showLayer(LAYER_INDEX_NORMAL_REELS);
		// On regular spin we want to disable all of the top layer reels, since
		// they should not need to spin until they are shown.
		lockAllReelsOnReelLayer(LAYER_INDEX_INDEPENDENT_REELS);
		unlockAllReelsOnReelLayer(LAYER_INDEX_NORMAL_REELS);
	}

	protected void unlockAllReelsOnReelLayer(int layer)
	{
		ReelLayer independentReelsLayer = layeredEngine.getLayerAt(layer);
		List<SlotReel> allReelsOnLayer = independentReelsLayer.getAllReels();

		for (int i = 0; i < allReelsOnLayer.Count; i++)
		{
			allReelsOnLayer[i].isLocked = false;
		}
	}

	protected void lockAllReelsOnReelLayer(int layer)
	{
		// On regular spin we want to disable all of the top layer reels, since
		// they should not need to spin until they are shown.
		ReelLayer independentReelsLayer = layeredEngine.getLayerAt(layer);
		List<SlotReel> allReelsOnLayer = independentReelsLayer.getAllReels();

		for (int i = 0; i < allReelsOnLayer.Count; i++)
		{
			allReelsOnLayer[i].isLocked = true;
		}
	}

	protected IEnumerator activateNormalReels()
	{
		if (isUsingIndependentReels)
		{
			// Time to swap back to the normal reels because we are doing a regular spin again
			yield return StartCoroutine(swapSymbolsBackToNormalReels());
			isUsingIndependentReels = false;
		}
		
		// On regular spin we want to disable all of the top layer reels, since
		// they should not need to spin until they are shown.
		lockAllReelsOnReelLayer(LAYER_INDEX_INDEPENDENT_REELS);
		unlockAllReelsOnReelLayer(LAYER_INDEX_NORMAL_REELS);
	}

	protected IEnumerator activateIndependentReels()
	{
		if (!isUsingIndependentReels)
		{
			// Lock all of the base layer reels, since we don't need to spin those as only the top layer
			// will be spinning in independent reels mode
			lockAllReelsOnReelLayer(LAYER_INDEX_NORMAL_REELS);
			unlockAllReelsOnReelLayer(LAYER_INDEX_INDEPENDENT_REELS);

			yield return StartCoroutine(swapSymbolsToIndependentReels());
			isUsingIndependentReels = true;
		}
	}

	protected virtual IEnumerator swapSymbolsBackToNormalReels()
	{
		// Default handling for now is to just turn back on the base layer
		// probably want to override if you need to do more complicated stuff
		showLayer(LAYER_INDEX_NORMAL_REELS);
		yield break;
	}

	protected virtual IEnumerator swapSymbolsToIndependentReels()
	{
		// Default handling is to just copy over the symbols from the base layer as is
		// probably want to override if you need to do more complicated or specific
		// stuff for your feature.
		
		// normalReelArray is the non-independent reel layer
		SlotReel[] normalReelArray = reelGame.engine.getReelArrayByLayer(LAYER_INDEX_NORMAL_REELS);
		for (int reelIndex = 0; reelIndex < normalReelArray.Length; reelIndex++)
		{
			SlotReel currentReel = normalReelArray[reelIndex];
			SlotSymbol[] visibleSymbols = currentReel.visibleSymbols;
			SlotSymbol[] independentVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(reelIndex, LAYER_INDEX_INDEPENDENT_REELS);
			
			// Copy the visible symbols to the independent reel layer
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol independentSymbol = independentVisibleSymbols[symbolIndex];
				if (independentSymbol.serverName != visibleSymbols[symbolIndex].serverName)
				{
					independentSymbol.mutateTo(visibleSymbols[symbolIndex].serverName, null, false, true);
				}
			}
		}

		// Now change what is being shown to be the independent layer
		showLayer(LAYER_INDEX_INDEPENDENT_REELS);
		yield break;
	}

	protected void showLayer(int layerIndexToShow)
	{
		ReelLayer[] layers = layeredEngine.reelLayers;
		for (int i = 0; i < layers.Length; i++)
		{
			ReelLayer currentLayer = layers[i];
			if (layerIndexToShow == currentLayer.layer)
			{
				currentLayer.parent.SetActive(true);
			}
			else
			{
				currentLayer.parent.SetActive(false);
			}
		}
	}
}
