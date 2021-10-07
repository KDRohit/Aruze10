using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles basic stuff for all indpendent reel free spin games
*/
public class IndependentReelFreeSpinGame : SlidingSlotFreeSpinGame
{
	[SerializeField] private CommonDataStructures.SerializableDictionaryOfStringToInt symbolCacheDictionary = new CommonDataStructures.SerializableDictionaryOfStringToInt();

	protected override void Awake()
	{
		// Fill in layer 0 data if the layer isn't already defined that way this class will
		// be backwards compatible with IndependentReelFreeSpinGame's that didn't fill in layer info
		if (reelLayers == null || reelLayers.Length == 0)
		{
			reelLayers = new ReelLayer[1];
			ReelLayer newLayer = reelLayers[0] = new ReelLayer();
			newLayer.layer = 0;
			newLayer.reelRoots = getReelRootsArrayShallowCopy();
			newLayer.symbolVerticalSpacing = symbolVerticalSpacingLocal;
			if (reelGameBackground != null)
			{
				newLayer.parent = reelGameBackground.gameObject;
			}
			else
			{
				newLayer.parent = gameObject;
			}
		}
		
		base.Awake();
	}
	
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0,0,0), new StopInfo(0,0,1),},
			new StopInfo[] {new StopInfo(0,1,0), new StopInfo(0,1,1),},
			new StopInfo[] {new StopInfo(0,2,0), new StopInfo(0,2,1),},
			new StopInfo[] {new StopInfo(0,3,0), new StopInfo(0,3,1),},
			new StopInfo[] {new StopInfo(0,4,0), new StopInfo(0,4,1),},

			new StopInfo[] {new StopInfo(1,0,0), new StopInfo(1,0,1),},
			new StopInfo[] {new StopInfo(1,1,0), new StopInfo(1,1,1),},
			new StopInfo[] {new StopInfo(1,2,0), new StopInfo(1,2,1),},
			new StopInfo[] {new StopInfo(1,3,0), new StopInfo(1,3,1),},
			new StopInfo[] {new StopInfo(1,4,0), new StopInfo(1,4,1),},

			new StopInfo[] {new StopInfo(2,0,0), new StopInfo(2,0,1),},
			new StopInfo[] {new StopInfo(2,1,0), new StopInfo(2,1,1),},
			new StopInfo[] {new StopInfo(2,2,0), new StopInfo(2,2,1),},
			new StopInfo[] {new StopInfo(2,3,0), new StopInfo(2,3,1),},
			new StopInfo[] {new StopInfo(2,4,0), new StopInfo(2,4,1),},

			new StopInfo[] {new StopInfo(3,0,0), new StopInfo(3,0,1),},
			new StopInfo[] {new StopInfo(3,1,0), new StopInfo(3,1,1),},
			new StopInfo[] {new StopInfo(3,2,0), new StopInfo(3,2,1),},
			new StopInfo[] {new StopInfo(3,3,0), new StopInfo(3,3,1),},
			new StopInfo[] {new StopInfo(3,4,0), new StopInfo(3,4,1),},

			new StopInfo[] {new StopInfo(4,0,0), new StopInfo(4,0,1),},
			new StopInfo[] {new StopInfo(4,1,0), new StopInfo(4,1,1),},
			new StopInfo[] {new StopInfo(4,2,0), new StopInfo(4,2,1),},
			new StopInfo[] {new StopInfo(4,3,0), new StopInfo(4,3,1),},
			new StopInfo[] {new StopInfo(4,4,0), new StopInfo(4,4,1),},
		};
	}

	/// Special function that will only really be called by the ReelSetup script as a fallback when a ReelEngine doesn't exist
	public override GameObject getReelRootsAtWhileApplicationNotRunning(int reelID, int row, int layer, CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes)
	{
		// fallback to old way of doing things if we don't have data setup to handle the independent reels
		if (independentReelVisibleSymbolSizes == null || !independentReelVisibleSymbolSizes.ContainsKey(reelID) || row >= independentReelVisibleSymbolSizes[reelID].Count)
		{
			// Avoid calling into the layers code if this is a backwards compatible freespins
			// that never had layers defined
			if (reelLayers == null || reelLayers.Length == 0)
			{
				// Layers weren't setup for this game, so it is a game that wasn't using layers
				// and needs to be handled in a backwards compatible way so call the ReelGame version
				// of this here.
				return getBasicReelGameReelRootsAtWhileApplicationNotRunning(reelID);
			}
			else
			{
				return base.getReelRootsAtWhileApplicationNotRunning(reelID, row, layer, independentReelVisibleSymbolSizes);
			}
		}
		else
		{
			int reelRootIndex = 0;

			// factor in previous reels indexing
			for (int prevReelID = 0; prevReelID < reelID; prevReelID++)
			{
				if (independentReelVisibleSymbolSizes.ContainsKey(prevReelID))
				{
					// increment the index by the numer of independent reels that exist on this reel
					reelRootIndex += independentReelVisibleSymbolSizes[prevReelID].Count;
				}
			}

			// factor in current reel indexing based on row
			if (independentReelVisibleSymbolSizes.ContainsKey(reelID))
			{
				List<int> currentReelVisibleSymbolSizes = independentReelVisibleSymbolSizes[reelID];
				for (int prevRowID = 0; prevRowID < row; prevRowID++)
				{
					if (prevRowID < currentReelVisibleSymbolSizes.Count)
					{
						reelRootIndex += currentReelVisibleSymbolSizes[prevRowID];
					}
				}
			}

			// Avoid calling into the layers code if this is a backwards compatible freespins
			// that never had layers defined
			if (reelLayers == null || reelLayers.Length == 0)
			{
				return getBasicReelGameReelRootsAtWhileApplicationNotRunning(reelRootIndex);
			}
			else
			{
				if (layer >= 0 && layer < reelLayers.Length)
				{
					GameObject[] layerReelRoots = reelLayers[layer].reelRoots;

					if (reelRootIndex >= 0 && reelRootIndex < layerReelRoots.Length)
					{
						return layerReelRoots[reelRootIndex];
					}
					else
					{
						Debug.LogError("reelRootIndex will case an index error!");
						return null;
					}
				}
				else
				{
					Debug.LogError("layer will case an index error!");
					return null;
				}
			}
		}
	}

	public override void initFreespins()
	{	
		base.initFreespins();

		if (SystemInfo.deviceModel != "Amazon KFOT" && SystemInfo.deviceModel != "Amazon KFTT") // Kindles are being a pain and crashing on load
		{
			// Load in all of the symbols we might need for this game. We may need more WD's, but that's pretty unlikely.
			foreach (KeyValuePair<string, int> symbolCacheEntry in symbolCacheDictionary)
			{
				cacheSymbolsToPool(symbolCacheEntry.Key, symbolCacheEntry.Value, false);
			}
		}
	}
	
	public override bool isGameWithSyncedReels()
	{
		return false;
	}

	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"));
		}
	}
}
