using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LayeredSlotBaseGame : SlotBaseGame
{
	[SerializeField] protected ReelLayer[] reelLayers;
	[Tooltip("Disabling this will make it so that layers aren't automatically linked every spin.  This may be useful if layers don't spin at the same time, for instance like in got01.")]
	[SerializeField] protected bool isLinkingLayersEverySpin = true;

	/// Special function that will only really be called by the ReelSetup script as a fallback when a ReelEngine doesn't exist
	public override GameObject getReelRootsAtWhileApplicationNotRunning(int reelID, int row, int layer, CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes)
	{
		if (layer >= 0 && layer < reelLayers.Length)
		{
			GameObject[] layerReelRoots = reelLayers[layer].reelRoots;

			if (reelID >= 0 && reelID < layerReelRoots.Length)
			{
				return layerReelRoots[reelID];
			}
			else
			{
				Debug.LogError("reelID will case an index error!");
				return null;
			}
		}
		else
		{
			Debug.LogError("layer will case an index error!");
			return null;
		}
	}

	public override bool isGameWithSyncedReels()
	{
		return true;
	}

	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0, 0, 0)},
			new StopInfo[] {new StopInfo(1, 0, 1)},
			new StopInfo[] {new StopInfo(2, 0, 1)},
			new StopInfo[] {new StopInfo(3, 0, 1)},
			new StopInfo[] {new StopInfo(4, 0, 1)},
			new StopInfo[] {new StopInfo(1, 0, 0)},
			new StopInfo[] {new StopInfo(2, 0, 0)},
			new StopInfo[] {new StopInfo(3, 0, 0)},
			new StopInfo[] {new StopInfo(4, 0, 0)},
			new StopInfo[] {new StopInfo(5, 0, 0)},
		};
	}

	protected override void handleSetReelSet(string reelSetKey)
	{
		_reelSetData = slotGameData.findReelSet(reelSetKey);
		if (_reelSetData != null)
		{
			// Set the reel game in each of the sliding layers
			foreach (ReelLayer reelLayer in reelLayers)
			{
				reelLayer.reelGame = this;
			}

			((LayeredSlotEngine)engine).setReelLayers(reelLayers);

			/// ================================
			/// Setting the data in the old way, ala Elvira02 and Gen07
			if (reelLayers.Length != 0)
			{
				reelLayers[0].reelSetData = _reelSetData;
			}

			Dictionary<int, string> otherReelSetData = reelSetDataJson.getIntStringDict("initial_reel_sets");
			foreach (KeyValuePair<int, string> initialReelSet in otherReelSetData)
			{
				if (initialReelSet.Key < reelLayers.Length)
				{
					reelLayers[initialReelSet.Key].reelSetData = slotGameData.findReelSet(initialReelSet.Value);
				}
				else
				{
					Debug.LogError("Not enough layered reels set, expecting " + (initialReelSet.Key + 1) + " only have " + reelLayers.Length);
				}
			}

			freeSpinInitialReelSet = reelSetDataJson.getIntStringDict("freespin_initial_reel_sets");

			/// ==========================
			/// Set the data in the new smarter way.

			foreach (JSON info in reelInfo)
			{
				string type = info.getString("type", "");
				string reelSet = info.getString("reel_set", "");
				int z_index = info.getInt("z_index", -1);
				
				// Since we only get one reelSetKey to work with, we will assume that is the background key
				// and will use whatever foreground info we find (if there are multiple we'll use the last one found).
				// (This could be fixed in the future if we wanted to by having the info about each reel set in
				// a layered game to be used at the start sent down and updating our code to handle multiple reel sets).
				if ((type == "background" && (reelSet == reelSetKey || z_index >= 1)) || type == "foreground")
				{
					// Get the reel set
					_reelSetData = slotGameData.findReelSet(reelSet);
					ReelLayer layer = getReelLayerAt(z_index);
					layer.hasReplacementSymbolDataReady = false;
					layer.reelSetData = _reelSetData;
					int startingReel = info.getInt("starting_reel",0);
					layer.setStartingIndex(startingReel);

					Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
					Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();
					JSON replaceData = info.getJSON("replace_symbols");

					if (replaceData != null)
					{
						foreach(KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
						{
							megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
						}
						foreach(KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
						{
							if (!megaReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key))
							{
								// Check and see if mega and normal have the same values.
								normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
							}
							else
							{
								Debug.LogError("The replacement info should not contain duplicate mappings. Investigate!");
							}
						}
					}

					layer.hasReplacementSymbolDataReady = true;
					layer.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
				}
			}

#if UNITY_EDITOR
			Debug.Log("Symbols used by engine: " + engine.getSymbolList());
#endif

			((LayeredSlotEngine)engine).tempPostReelSetDataChangedHandler();

			currentReelSetName = reelSetKey;
			setSpinPanelWaysToWin(reelSetKey);
			resetSlotMessage();
		}
		else
		{
			Debug.LogError("Unable to find reel set data for key " + reelSetKey);
		}
	}

	public int getReelLayersCount()
	{
		if (reelLayers != null)
		{
			return reelLayers.Length;
		}
		else
		{
			return 0;
		}
	}

	public virtual ReelLayer getReelLayerAt(int index)
	{
		foreach (ReelLayer layer in reelLayers)
		{
			if (layer.layer == index)
			{
				return layer;
			}
		}
		Debug.LogWarning("Couldn't find a layer with value " + index);
		return null;
	}

	public override float getSymbolVerticalSpacingAt(int reelID, int layer = 0)
	{
		ReelLayer reelLayer = getReelLayerAt(layer);
		if (reelLayer != null)
		{
			float layerSymbolVerticalSpacing = reelLayer.getSymbolVerticalSpacingAt(reelID);
			if (layerSymbolVerticalSpacing != 0)
			{
				return layerSymbolVerticalSpacing;
			}
		}
		else
		{
			Debug.LogWarning("No layer " + layer + " defined, returning base symbol vertical spacing.");
		}
		return base.getSymbolVerticalSpacingAt(reelID, layer);
	}

	protected override void setEngine()
	{
		engine = new LayeredSlotEngine(this, isLinkingLayersEverySpin);
	}
}
