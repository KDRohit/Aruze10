using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IndependentReelBaseGame : SlidingSlotBaseGame
{
	[SerializeField] private CommonDataStructures.SerializableDictionaryOfStringToInt symbolCacheDictionary = new CommonDataStructures.SerializableDictionaryOfStringToInt();
	[SerializeField] private List<CommonDataStructures.KeyValuePairOfIntToInt> anticipationReelPositions = new List<CommonDataStructures.KeyValuePairOfIntToInt>() { new CommonDataStructures.KeyValuePairOfIntToInt(4, 2) };
	[SerializeField] private bool isMovingBonusSymbolsToSlotOverlay = false;

	protected bool isDoingSoundOverrides = true;

	private const string REEL_ANTICIPATION_KILL_JOY_SOUND_KEY = "independent_reel_kill_bonus";
	private const string BONUS_SYMBOL_FANFARE_KEY_PREFIX = "bonus_symbol_fanfare";

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
			return base.getReelRootsAtWhileApplicationNotRunning(reelID, row, layer, independentReelVisibleSymbolSizes);
		}
		else
		{
			int reelRootIndex = getReelRootIndex(reelID, row, independentReelVisibleSymbolSizes);

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

	// similiar to getReelRootsAtWhileApplicationNotRunning() in that this is a
	// special function for previewing independent reels/symbols in editor when a ReelEngine doesn't exist whilst not actually running game
	public int getReelRootIndex(int reelID, int row, CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes)
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

		return reelRootIndex;
	}

	protected override IEnumerator finishLoading(JSON slotGameStartedData)
	{
		if (SystemInfo.deviceModel != "Amazon KFOT" && SystemInfo.deviceModel != "Amazon KFTT") // Kindles are being a pain and crashing on load
		{
			// Load in all of the symbols we might need for this game. We may need more WD's, but that's pretty unlikely.
			foreach (KeyValuePair<string, int> symbolCacheEntry in symbolCacheDictionary)
			{
				yield return StartCoroutine(cacheSymbolsToPoolCoroutine(symbolCacheEntry.Key, symbolCacheEntry.Value, symbolCacheEntry.Value));
			}
		}
		yield return StartCoroutine(base.finishLoading(slotGameStartedData));
	}

	public override bool isGameWithSyncedReels()
	{
		return false;
	}

	/// Overriding so we can change the symbol layer if the game needs that to happen
	public override IEnumerator playBonusAcquiredEffects()
	{
		TICoroutine bonusAnimateRoutine = StartCoroutine(base.playBonusAcquiredEffects());

		if (isMovingBonusSymbolsToSlotOverlay)
		{
			foreach(SlotReel reel in engine.getReelArray())
			{
				foreach(SlotSymbol symbol in reel.visibleSymbols)
				{
					if (symbol.isBonusSymbol) // every bonus symbol that's visible should animate (can't get bonus symbols that aren't part of it)
					{
						CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_OVERLAY); // SLOT_OVERLAY seems to work well
					}
				}
			}
			yield return null; // wait 1 frame so the symbol has been swapped to the _Outcome version
		}

		yield return bonusAnimateRoutine; // wait for animations to complete
	}

	/// Need the outcome to determine if this is a bonus, and then if not, change a reel stop sound
	public override void setOutcome(SlotOutcome outcome)
	{
		base.setOutcome(outcome);

		if (!isDoingSoundOverrides)
		{
			return;
		}

		int[] anticipationSoundIndexs = outcome.getAnticipationSounds();
		if (!outcome.isBonus && anticipationSoundIndexs.Length != 0)
		{
			// need to grab the last anticipation reel that could have triggered, so we can change the sound
			// just using a hardcoded value
			if (anticipationReelPositions.Count > 0)
			{
				CommonDataStructures.KeyValuePairOfIntToInt anticipationReelPos = anticipationReelPositions[anticipationReelPositions.Count - 1];
				SlotReel lastAnticipationReel = engine.getSlotReelAt(anticipationReelPos.key, anticipationReelPos.value);
				lastAnticipationReel.reelStopSoundOverride = Audio.soundMap(REEL_ANTICIPATION_KILL_JOY_SOUND_KEY);
			}
		}
		else if (outcome.isBonus && anticipationSoundIndexs.Length != 0) 
		{
			// setup override sounds for bonus symbol fanfares (required by zynga03)
			for (int i = 0; i < anticipationReelPositions.Count; ++i)
			{
				CommonDataStructures.KeyValuePairOfIntToInt anticipationPos = anticipationReelPositions[i];
				SlotReel currentAnticipationReel = engine.getSlotReelAt(anticipationPos.key, anticipationPos.value);
				string bonusSymbolFanfareKey = BONUS_SYMBOL_FANFARE_KEY_PREFIX + (i + 1);

				if (currentAnticipationReel != null && Audio.canSoundBeMapped(bonusSymbolFanfareKey))
				{
					currentAnticipationReel.reelStopSoundOverride = Audio.soundMap(bonusSymbolFanfareKey);
				}
			}
		}
	}
}
