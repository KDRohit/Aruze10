using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/**
 * PlaySymbolSoundOnReelStopModule.cs
 *
 * Original Author: Scott Lepthien
 * Creation Date: 4/4/2017
 *
 * Very similar to PlaySymbolAnticipationOnReelStopModule, but only plays a sound when a specific symbol lands but doesn't animate the symbol
 */
public class PlaySymbolSoundOnReelStopModule : SlotModule 
{
	[SerializeField] protected string symbolToPlaySoundFor = "";
	
	[Tooltip("List of the symbols to play the sound for")]
	[SerializeField] protected List<string> symbolsToPlaySoundFor;
	
	[SerializeField] protected bool includeMegaSymbols = false;
	[SerializeField] protected string symbolLandSoundKey;
	[Tooltip("Override sound key that will be used if the game is performing freespins in base and is set.")]
	[SerializeField] protected string freespinInBaseSymbolLandSoundKey;
	[Tooltip("Some anticipations might want to be played during the rollback instead of once the reel fully stops")]
	[SerializeField] protected bool shouldExecuteOnEndRollback = false;
	[Tooltip("Some sounds may require an incrementing value to be added as more symbols land, make this true if you want to add the hit number")]
	[SerializeField] protected bool isAddingHitCountToSound = false;

	protected int hitCount = 0;
	private LayeredSlotEngine layeredSlotEngine = null;

	private Dictionary<string, bool> _symbolToPlaySoundForMap;

	public override void Awake()
	{
		base.Awake();
		
		layeredSlotEngine = reelGame.engine as LayeredSlotEngine;
		createSymbolToPlaySoundForMap();
	}

	// create a dictionary for symbol name lookups so it's nice and fast.
	private void createSymbolToPlaySoundForMap()
	{
		_symbolToPlaySoundForMap = new Dictionary<string, bool>();
		foreach (string symbolName in symbolsToPlaySoundFor)
		{
			_symbolToPlaySoundForMap[symbolName] = true;
		}
	}

	// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		hitCount = 0;
		yield break;
	}

// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return !shouldExecuteOnEndRollback;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		handleSymbolAnticipations(stoppedReel);
		yield break;
	}

// executeOnReelEndRollback() section
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return shouldExecuteOnEndRollback;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		// This function doesn't block the spin from ending. Be wary of using it.
		handleSymbolAnticipations(reel);
		yield break;
	}

	// Handle playing the symbol anticipations
	protected virtual void handleSymbolAnticipations(SlotReel reel)
	{
		// This will get every visible symbol on the whole reel, which will include all independent reels
		// that are part of that reel if independent reels are being used.  So we need to make sure we only
		// look at the visible symbols that are linked to the reel that is passed in.
		// Technically we could just read the visible symbols from the SlotReel itself, but I decided not
		// to do that since the original call was using SlotEngine.getVisibleSymbolsAt() which for layered games
		// would only return the top layer.  So to keep that functionality the same some extra code has to be added
		// to deal with independent and hybrid independent reel games.
		SlotSymbol[] visibleSymbolsForReel = reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1);
		bool hasIndependentReels = false;
		if (layeredSlotEngine != null)
		{
			ReelLayer currentLayer = layeredSlotEngine.getLayerAt(reel.layer);
			hasIndependentReels = currentLayer.reelSetData.isIndependentReels || currentLayer.reelSetData.isHybrid;
		}
		else
		{
			hasIndependentReels = reelGame.engine.reelSetData.isIndependentReels || reelGame.engine.reelSetData.isHybrid;
		}

		if (hasIndependentReels)
		{
			for (int i = reel.position; i < reel.position + reel.visibleSymbols.Length; i++)
			{
				SlotSymbol symbol = null;
				if (i >= 0 && i < visibleSymbolsForReel.Length)
				{
					symbol = visibleSymbolsForReel[i];
				}
				
				if (handleSoundForSymbol(symbol))
				{
					// only play one sound per reel land
					break;
				}
			}
		}
		else
		{
			foreach (SlotSymbol symbol in visibleSymbolsForReel)
			{
				if (shouldPlaySoundForSymbol(symbol))
				{
					if (handleSoundForSymbol(symbol))
					{
						// only play one sound per reel land
						break;
					}
				}
			}
		}
	}

	protected bool handleSoundForSymbol(SlotSymbol symbol)
	{
		if (shouldPlaySoundForSymbol(symbol))
		{
			hitCount++;
			if (!string.IsNullOrEmpty(symbolLandSoundKey))
			{
				string currentLandSoundKey = symbolLandSoundKey;
			
				if (reelGame.isDoingFreespinsInBasegame() && !string.IsNullOrEmpty(freespinInBaseSymbolLandSoundKey))
				{
					currentLandSoundKey = freespinInBaseSymbolLandSoundKey;
				}

				if (isAddingHitCountToSound)
				{
					currentLandSoundKey += hitCount;
				}

				Audio.playSoundMapOrSoundKey(currentLandSoundKey);

				// only play one sound per reel land
				return true;
			}
		}

		return false;
	}

	protected bool isSymbolBehindStickyWild(SlotSymbol symbol)
	{
		return reelGame.isSymbolLocationCovered(symbol.reel, symbol.index);
	}

	protected bool shouldPlaySoundForSymbol(SlotSymbol symbol)
	{
		if (symbol == null || isSymbolBehindStickyWild(symbol))
		{
			return false;
		}
		
		if (symbol.serverName == symbolToPlaySoundFor 
		    || _symbolToPlaySoundForMap.ContainsKey(symbol.serverName) 
		    || includeMegaSymbols && !string.IsNullOrEmpty(symbolToPlaySoundFor) && symbol.serverName.Contains(symbolToPlaySoundFor)
		    )
		{
			return true;
		}

		return false;
	}
}
