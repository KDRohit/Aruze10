using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LayeredSlotFreeSpinGame : FreeSpinGame
{
	public ReelLayer[] reelLayers;
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
			foreach (ReelLayer reelLayer in reelLayers)
			{
				reelLayer.reelGame = this;
			}

			if (reelLayers.Length != 0)
			{
				reelLayers[0].reelSetData = _reelSetData;
			}

			((LayeredSlotEngine)engine).setReelLayers(reelLayers);

			if (reelInfo != null && reelInfo.Length > 0)
			{
				// New way of getting down reel info.
				foreach (JSON info in reelInfo)
				{
					string type = info.getString("type", "");
					string reelSet = info.getString("reel_set", "");
					int z_index = info.getInt("z_index", -1);
					// Since we only get one reelSetKey to work with, we will assume that is the background key
					// and will use whatever foreground info we find.  (This could be fixed in the future if we
					// wanted to by having the info about each reel set in a layered game to be used at the start
					// sent down and updating our code to handle multiple reel sets).
					if ((type == "freespin_background" && (reelSet == reelSetKey || z_index >= 1)) || type == "freespin_foreground")
					{
						// Get the reel set
						_reelSetData = slotGameData.findReelSet(reelSet);
						ReelLayer layer = getReelLayerAt(z_index);
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

						layer.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
					}
				}
			}
			else
			{
				// Old way of getting down reel info for freespin games.
				Dictionary<int, string> freeSpinInitialReelSet = null;
				if (SlotBaseGame.instance == null)
				{
					freeSpinInitialReelSet = _freeSpinsOutcomes.freeSpinInitialReelSet;
				}
				else
				{
					freeSpinInitialReelSet = SlotBaseGame.instance.freeSpinInitialReelSet;
				}

				if (freeSpinInitialReelSet == null || freeSpinInitialReelSet.Count == 0)
				{
					if (reelLayers.Length > 1)
					{
						// This is really bad.
						Debug.LogError("LayeredSlotFreeSpinGame.handleSetReelSet() - No initialReelSet defined for a game with multiple layers.  Some layers aren't going to be setup!");
					}
				}
				else
				{
					foreach (KeyValuePair<int, string> initialReelSet in freeSpinInitialReelSet)
					{
						if (initialReelSet.Key < reelLayers.Length)
						{
							reelLayers[initialReelSet.Key].reelSetData = slotGameData.findReelSet(initialReelSet.Value);
						}
						else
						{
							Debug.LogError("Not enough sliding reels set, expecting " + (initialReelSet.Key + 1) + " only have " + reelLayers.Length);
						}
					}
				}
			}

			// Set the reel game in each of the sliding layers
			foreach (ReelLayer slidingLayer in reelLayers)
			{
				slidingLayer.reelGame = this;
			}

			currentReelSetName = reelSetKey;
			setSpinPanelWaysToWin(reelSetKey);
			((LayeredSlotEngine)engine).tempPostReelSetDataChangedHandler();
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
		// Handle backwards compatibility for IndependentReelFreeSpinGame's that weren't
		// layered before and didn't define them.
		if (reelLayers == null || reelLayers.Length == 0)
		{
			// if there aren't layers, use the standard vertical spacing
			return symbolVerticalSpacingLocal;
		}
		else
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
	}

	protected override void setEngine(string payTableKey)
	{
		engine = new LayeredSlotEngine(this, isLinkingLayersEverySpin, payTableKey);
	}
}
