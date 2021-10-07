using UnityEngine;
using System.Collections;

/*
Module to adjust visible symbol layering and/or hide buffer symbols to account for oversized symbols that need
to be adjusted in order to not get in the way of each other.

Last Touched By: Scott Lepthien
Creation Date: 10/11/2016
*/
public class OversizedSymbolDisplayModule : SlotModule 
{
	[SerializeField] private bool isHidingAndShowingBufferSymbols = true; // for games where the buffer symbols should be hidden when reels are stopped and shown when spinning
	[SerializeField] private bool isRelayeringSymbols = true;
	[SerializeField] private bool setToReelLayerOnSpin = true;
	[SerializeField] private Layers.LayerID overlayLayer = Layers.LayerID.ID_SLOT_OVERLAY; // controls what layer the symbols will be forced to in order to overlay and pop over the frame

	// functions here are called when the base game is loading and won't close the load screen until they are finished.
	public override bool needsToExecuteOnBaseGameLoad(JSON slotGameStartedData)
	{
		return true;
	}

	public override IEnumerator executeOnBaseGameLoad(JSON slotGameStartedData)
	{
		if (isHidingAndShowingBufferSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				toggleBufferSymbols(reel, false);
			}
		}

		if (isRelayeringSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				setVisibleSymbolLayers(reel, (int)overlayLayer);
			}
		}

		yield break;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (isHidingAndShowingBufferSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				toggleBufferSymbols(reel, true);
			}
		}

		if (isRelayeringSymbols && setToReelLayerOnSpin)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				setVisibleSymbolLayers(reel, Layers.ID_SLOT_REELS);
			}
		}

		yield break;
	}
		
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return true;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		if (isHidingAndShowingBufferSymbols)
		{
			toggleBufferSymbols(stoppedReel, false);
		}

		if (isRelayeringSymbols)
		{
			setVisibleSymbolLayers(stoppedReel, (int)overlayLayer);
		}

		yield break;
	}

// executeOnReleaseSymbolInstance() section
// functions in this section are accessed by ReelGame.releaseSymbolInstance
	public override bool needsToExecuteOnReleaseSymbolInstance()
	{
		return isRelayeringSymbols;
	}

	public override void executeOnReleaseSymbolInstance(SymbolAnimator animator)
	{
		if (animator != null)
		{
			// restore the animator to the reel layer
			CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
		}
	}

// executeOnReevaluationSpinStart() section
// functions in this section are accessed by ReelGame.startNextReevaluationSpin()
	public override bool needsToExecuteOnReevaluationSpinStart()
	{
		return true;
	}

	public override IEnumerator executeOnReevaluationSpinStart()
	{

		if (isHidingAndShowingBufferSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				toggleBufferSymbols(reel, true);
			}
		}

		if (isRelayeringSymbols && setToReelLayerOnSpin)
		{
			foreach (SlotReel reel in reelGame.engine.getReelArray())
			{
				setVisibleSymbolLayers(reel, Layers.ID_SLOT_REELS);
			}
		}

		yield break;
	}

	public void toggleBufferSymbols(SlotReel slotReel, bool showNonVisible)
	{
		// go through the buffer symbols and control their visiblity
		// handle top buffer symbols
		for (int bufferIndex = 0; bufferIndex < slotReel.numberOfTopBufferSymbols; bufferIndex++)
		{
			if (bufferIndex < slotReel.symbolList.Count)
			{
				SlotSymbol bufferSymbol = slotReel.symbolList[bufferIndex];
				if (bufferSymbol != null && bufferSymbol.gameObject != null && !bufferSymbol.isVisible(anyPart: true, relativeToEngine: true))
				{
					bufferSymbol.gameObject.SetActive(showNonVisible);
				}
			}
		}

		// handle bottom buffer symbols
		int bottomStartPoint = slotReel.numberOfTopBufferSymbols + slotReel.visibleSymbols.Length;
		for (int bufferIndex = 0; bufferIndex < slotReel.numberOfBottomBufferSymbols; bufferIndex++)
		{
			int symbolIndex = bottomStartPoint + bufferIndex;
			if (symbolIndex < slotReel.symbolList.Count)
			{
				SlotSymbol bufferSymbol = slotReel.symbolList[symbolIndex];
				if (bufferSymbol != null && bufferSymbol.gameObject != null && !bufferSymbol.isVisible(anyPart: true, relativeToEngine: true))
				{
					bufferSymbol.gameObject.SetActive(showNonVisible);
				}
			}
		}
	}
		
	public void setVisibleSymbolLayers(SlotReel slotReel, int layer)
	{
		foreach (SlotSymbol symbol in slotReel.visibleSymbols)
		{
			// first halt any playing animations so that we ensure that any symbol layer reorganization has been resolved
			symbol.haltAnimation();

			GameObject symbolGameObject = symbol.gameObject;
			if (symbolGameObject != null)
			{
				CommonGameObject.setLayerRecursively(symbolGameObject, layer);
			}
		}
	}
}
