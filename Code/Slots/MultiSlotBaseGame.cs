using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiSlotBaseGame : LayeredSlotBaseGame
{
	[SerializeField] protected float BONUS_GAME_WIN_LABEL_UPDATE_DELAY;
	[SerializeField] protected bool duplicateRPMapAllowed = false;

	public GameObject getReelRootAtLayer(int rootIndex, int layerIndex)
	{
		return (engine as MultiSlotEngine).reelLayers[layerIndex].getReelArray()[rootIndex].getReelGameObject();
	}
	
	public GameObject getReelRootsAtAllLayers(int index)
	{
		int length = 0;
		int previousLength = 0;
		foreach(ReelLayer layer in (engine as MultiSlotEngine).reelLayers)
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

	public override bool isGameWithSyncedReels()
	{
		return false;
	}
	
	protected override void setEngine()
	{
		engine = new MultiSlotEngine(this);
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
	
	/// Shows the layered bonus outcomes (or regular non-bonus outcomes if there are no bonus outcomes left)
	public override void showNonBonusOutcomes()
	{		
		if (layeredBonusOutcomes.Count > 0)
		{
			StartCoroutine(animateBonusSymbolsThenStartBonus());
		}
		else
		{
			base.showNonBonusOutcomes();
		}
	}
	
	public override SlotOutcome getBonusOutcome(bool shouldRemoveLayeredOutcome = false)
	{
		SlotOutcome bonusOutcome = null;
		if (_outcome.isBonus && !isBaseBonusOutcomeProcessed)
		{
			bonusOutcome = _outcome;
			isBaseBonusOutcomeProcessed = true;
		}
		else if (layeredBonusOutcomes.Count > 0)
		{
			bonusOutcome = layeredBonusOutcomes[0];
			if (shouldRemoveLayeredOutcome)
			{
				layeredBonusOutcomes.Remove(bonusOutcome);
			}
			bonusGameOutcomeOverride = bonusOutcome;
		}
		else
		{
			Debug.LogError("Bonus game processing is broken");
		}
		setupBonusOutcome(bonusOutcome);
		return bonusOutcome;
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
		foreach (ReelLayer reelLayer in reelLayers)
		{
			reelLayer.reelGame = this;
			ReelSetData layerReelSetData = slotGameData.findReelSet(defaultKey);
			reelLayers[reelLayer.layer].reelSetData = layerReelSetData;
			
		}
		
		handleSetReelSet(defaultKey);
	}
	
	protected override void handleSetReelSet(string reelSetKey)
	{
		if (!string.IsNullOrEmpty(reelSetKey)) // initial reel setup
		{
			currentReelSetName = reelSetKey;
			setSpinPanelWaysToWin(reelSetKey);
			resetSlotMessage();
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
			foreach (ReelLayer reelLayer in reelLayers)
			{
				reelLayer.reelGame = this;


				string layerReelSetKey = multiGamesData[reelLayer.layer].getString("reel_set", "");	
				ReelSetData layerReelSetData = slotGameData.findReelSet(layerReelSetKey);
				reelLayers[reelLayer.layer].reelSetData = layerReelSetData;
			}
		}

		// do this here so we can use it in the replace symbol logic below
		((LayeredSlotEngine)engine).setReelLayers(reelLayers);
		
		if (!string.IsNullOrEmpty(reelSetKey))
		{			
			foreach (JSON info in reelInfo)
			{
				string type = info.getString("type", "");
				if (type == "foreground" ||
				    type == "background"
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
							if (!megaReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key) || duplicateRPMapAllowed)
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
			JSON[] reevalInfo = outcome.getArrayReevaluations();			
			Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
			Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();
			JSON replaceData = reevalInfo[0].getJSON("replace_symbols");
			bool shouldLayersShareMaps = false;
			if (replaceData != null)
			{
				foreach(ReelLayer reelLayer in reelLayers)
				{
					Dictionary<string, string> gameReplaceSymbols = replaceData.getStringStringDict("game_" + reelLayer.layer);
					if (gameReplaceSymbols != null && gameReplaceSymbols.Count > 0)
					{
						foreach(KeyValuePair<string, string> replaceInfo in gameReplaceSymbols)
						{
							megaReplacementSymbolMap.Add(replaceInfo.Key, replaceInfo.Value);
							normalReplacementSymbolMap.Add(replaceInfo.Key, replaceInfo.Value);	
						}
						shouldLayersShareMaps = true;
						reelLayer.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
						normalReplacementSymbolMap = new Dictionary<string, string>();
						megaReplacementSymbolMap = new Dictionary<string, string>();
					}
				}
				foreach(KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
				{
					megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
				}
				foreach(KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
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
					reelLayer.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
				}
			}
		}

		((LayeredSlotEngine)engine).tempPostReelSetDataChangedHandler();
		
		if (engine.isStopped)
		{
			resetSlotMessage();
		}

		
	}

	// for multi games we need to process the bonus outcome before going into it
	public override void setupBonusOutcome(SlotOutcome bonusOutcome)
	{
		bonusOutcome.processBonus();
	}

	private IEnumerator showPreviousBonusGameWinnings()
	{
		if (BonusGameManager.instance.multiBonusGamePayout > 0)
		{
			yield return new TIWaitForSeconds(BONUS_GAME_WIN_LABEL_UPDATE_DELAY);
			SpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGameManager.instance.multiBonusGamePayout);
			yield return new TIWaitForSeconds(BONUS_GAME_WIN_LABEL_UPDATE_DELAY);
		}
	}
	
	private IEnumerator animateBonusSymbolsThenStartBonus()
	{
		bool letModuleCreateBonus = false;
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToLetModuleCreateBonusGame())
			{
				letModuleCreateBonus = true;
				break;
			}
		}
		if (!letModuleCreateBonus)
		{
			createBonus();
		}
		yield return null;
		yield return StartCoroutine(showPreviousBonusGameWinnings());
		yield return null;
		if (!letModuleCreateBonus) //The transition module also handles playing our bonus acquired effects
		{
			yield return StartCoroutine(playBonusAcquiredEffectsByLayer(layeredBonusOutcomes[0].layer));
		}		
		layeredBonusOutcomes[0].processBonus();
		if (!letModuleCreateBonus)
		{
			startBonus();
		}
	}
	
	/**
	Function to play the bonus acquired effects (bonus symbol animaitons and an audio 
	appluase for getitng the bonus), can be overridden to handle games that need or 
	want to handle this bonus transition differently
	*/
	protected virtual IEnumerator playBonusAcquiredEffectsByLayer(int layer)
	{
		yield return StartCoroutine(engine.playBonusAcquiredEffects(layer));
	}
}