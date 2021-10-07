using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Freespin version of MultiSlotBaseGame

Creation Date: 9/26/2018
Original Author: Scott Lepthien
*/
public class MultiSlotFreeSpinGame : LayeredSlotFreeSpinGame
{
	[SerializeField] protected bool duplicateRPMapAllowed = false;

	public GameObject getReelRootAtLayer(int rootIndex, int layerIndex)
	{
		return (engine as MultiSlotEngine).reelLayers[layerIndex].getReelArray()[rootIndex].getReelGameObject();
	}
	
	public GameObject getReelRootsAtAllLayers(int index)
	{
		int length = 0;
		int previousLength = 0;
		foreach (ReelLayer layer in (engine as MultiSlotEngine).reelLayers)
		{
			SlotReel[] reelArray = layer.getReelArray();
			length += reelArray.Length;
			if (index < length)
			{
				return reelArray[index - previousLength].getReelGameObject();
			}
			previousLength = length;
		}
		return null;
	}
	
	public override ReelLayer getReelLayerAt(int index)
	{
		ReelLayer layer;
		for (int i = 0; i < reelLayers.Length; i++)
		{
			layer = reelLayers[i];
			if (layer.layer == index)
			{
				return layer;
			}
		}

		Debug.LogWarning("Couldn't find a layer with value " + index);
		return null;
	}

	public override bool isGameWithSyncedReels()
	{
		return false;
	}
	
	protected override void setEngine(string payTableKey)
	{
		engine = new MultiSlotEngine(this, payTableKey);
	}
	
	
	protected override void Awake()
	{
		base.Awake();
		// Make all of the reelLayers into SlidingLayers
		for (int i = 0; i < reelLayers.Length; i++)
		{
			reelLayers[i] = new MultiSlotLayer(reelLayers[i]);
		}
	}

	
	// setup "lines" boxes on sides as instructed in HIR-17287
	protected override void setSpinPanelWaysToWin(string reelSetName)
	{
		// One of these should be > 0, and that will tell us which one to use.
		int waysToWin = slotGameData.getWaysToWin(reelSetName);
		int winLines = slotGameData.getWinLines(reelSetName);
		
		waysToWin *= reelLayers.Length;
		winLines *= reelLayers.Length;
		
		if (waysToWin > 0)
		{
			SpinPanel.instance.setSideInfo(waysToWin, "ways", showSideInfo);
			initialWaysLinesNumber = waysToWin;
		}
		else if (winLines > 0)
		{
			SpinPanel.instance.setSideInfo(winLines, "lines", showSideInfo);
			initialWaysLinesNumber = winLines;
		}
	}
	
	/// We want to validate the symobls and change the tier when we set the outcome for the base game.
	public override void setOutcome(SlotOutcome outcome)
	{
		base.setOutcome(outcome);
		setReelSet(null); // do this every time so we do the symbol replacement logic	
	}
	
	public override void setReelSet(string defaultKey, JSON data)
	{
		reelSetDataJson = data;
		setReelInfo();
		setModifiers();
		for (int i = 0; i < reelLayers.Length; i++)
		{
			ReelLayer reelLayer = reelLayers[i];
			reelLayer.reelGame = this;
			ReelSetData layerReelSetData = slotGameData.findReelSet(defaultKey);
			reelLayers[reelLayer.layer].reelSetData = layerReelSetData;
		}
		
		handleSetReelSet(defaultKey);
	}
	
	protected override void handleSetReelSet(string reelSetKey)
	{
		for (int i = 0; i < reelLayers.Length; i++)
		{
			reelLayers[i].reelGame = this;
		}
	
		if (!string.IsNullOrEmpty(reelSetKey)) // initial reel setup
		{
			currentReelSetName = reelSetKey;
			setSpinPanelWaysToWin(reelSetKey);
		}
		else // setup reels for an outcome
		{			
			JSON[] reevalInfo = outcome.getArrayReevaluations();			
			if (reevalInfo == null || reevalInfo.Length == 0)
			{
				Debug.LogError("No Reevaluations were found in the outcome for this Multi Slot Game");
			}		
			JSON[] multiGamesData = reevalInfo[0].getJsonArray("games");
			if (multiGamesData == null || multiGamesData.Length == 0)
			{
				Debug.LogError("No Games data was found in the outcome for this Multi Slot Game");
			}
			else if (multiGamesData.Length != reelLayers.Length)
			{
				Debug.LogError("Number of games defined in outcome: " + multiGamesData.Length + "    did not match number of reel layers defined on this game object: " + reelLayers.Length);
			}

			// Set the reel game in each of the sliding layers
			for (int i = 0; i < reelLayers.Length; i++)
			{
				ReelLayer reelLayer = reelLayers[i];
				reelLayer.reelGame = this;
				string layerReelSetKey = multiGamesData[reelLayer.layer].getString("reel_set", "");	
				ReelSetData layerReelSetData = slotGameData.findReelSet(layerReelSetKey);
				reelLayers[reelLayer.layer].reelSetData = layerReelSetData;
			}
		}

		// do this here so we can use it in the replace symbol logic below
		((LayeredSlotEngine)engine).setReelLayers(reelLayers);
		
		if (reelInfo != null && reelInfo.Length > 0)
		{
			// New way of getting down reel info.
			JSON info;
			for (int i = 0; i < reelInfo.Length; i++)
			{
				info = reelInfo[i];
				string type = info.getString("type", "");
				if (type == "freespin_foreground" ||
					type == "freespin_background"
					)
				{
					// Get the reel set
					string reelSet = info.getString("reel_set", "");
					_reelSetData = slotGameData.findReelSet(reelSet);
					int z_index = info.getInt("z_index", -1);
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

					layer.setReplacementSymbols(normalReplacementSymbolMap, megaReplacementSymbolMap);
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
				// This is really bad.
				Debug.LogError("No initialReelSet!");
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
			
			// Handle trying to extract the replace info from a reevaluation entry
			if (outcome != null)
			{
				JSON[] reevalInfo = outcome.getArrayReevaluations();			
				Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
				Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();
				JSON replaceData = reevalInfo[0].getJSON("replace_symbols");
				bool shouldLayersShareMaps = false;
				if (replaceData != null)
				{
					ReelLayer reelLayer;
					for (int i = 0; i < reelLayers.Length; i++)
					{
						reelLayer = reelLayers[i];
						Dictionary<string, string> gameReplaceSymbols = replaceData.getStringStringDict("game_" + reelLayer.layer);
						if (gameReplaceSymbols != null && gameReplaceSymbols.Count > 0)
						{
							foreach (KeyValuePair<string, string> replaceInfo in gameReplaceSymbols)
							{
								megaReplacementSymbolMap.Add(replaceInfo.Key, replaceInfo.Value);
								normalReplacementSymbolMap.Add(replaceInfo.Key, replaceInfo.Value);
							}
							shouldLayersShareMaps = true;
							reelLayer.setReplacementSymbols(normalReplacementSymbolMap, megaReplacementSymbolMap);
							normalReplacementSymbolMap = new Dictionary<string, string>();
							megaReplacementSymbolMap = new Dictionary<string, string>();
						}
					}
					foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
					{
						megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
					}
					foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
					{
						if (!megaReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key) || duplicateRPMapAllowed)
						{
							normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
						}
						else
						{
							Debug.LogError("The replacement info should not contain duplicate mappings. Investigate!");
						}
					}
				}
				
				if (!shouldLayersShareMaps)
				{
					// Set the reel game in each of the sliding layers
					foreach (ReelLayer reelLayer in reelLayers)
					{	
						reelLayer.setReplacementSymbols(normalReplacementSymbolMap, megaReplacementSymbolMap);
					}
				}
			}
		}

		((LayeredSlotEngine)engine).tempPostReelSetDataChangedHandler();
	}
}
