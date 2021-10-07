using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for changing the visual for a symbol based on a progressive jackpot tier 
 * Used because SC9 comes down, regardless of selected jackpot tier, but visuals may be different
 * Example: SC9 for MINI jackpot says Mini, SC9 for MINOR says Minor, etc., but SC9 is the only symbol to come down
 *
 * Author : Shaun Peoples <speoples@zynga.com>
 * First Use : Orig001
 */
public class MutateSymbolByProgressiveJackpotTierModule : SlotModule 
{
    [System.Serializable]
    protected class JackpotTierToSymbolName
    {
        public string jackpotTierName;
        public string serverSymbolName;
        public GameObject symbolPrefab;
        public bool isForBonusGame;
        [HideInInspector] public GameObject originalPrefab;
        [HideInInspector] public SymbolInfo referenceSymbolInfo;
    }

    [System.NonSerialized] private string lastActiveTier;
    [SerializeField] private List<JackpotTierToSymbolName> jackpotTierToSymbolNames;
    
    public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
    {
        return jackpotTierToSymbolNames != null && jackpotTierToSymbolNames.Count > 0;
    }

    public override void executeAfterSymbolSetup(SlotSymbol symbol)
    {	
	    JackpotTierToSymbolName activeTier = getActiveJackpotTierSymbolsData();
	    if (lastActiveTier == activeTier.jackpotTierName)
	    {
		    return;
	    }
	    
	    if (symbol.serverName != activeTier.serverSymbolName)
	    {
		    return;	
	    }
			
	    SymbolInfo symbolInfo = reelGame.findSymbolInfo(symbol.name);
	    activeTier.originalPrefab = symbolInfo.symbolPrefab;
	    symbolInfo.symbolPrefab = activeTier.symbolPrefab;
	    reelGame.createCachedSymbols(new List<SymbolInfo>{symbolInfo}, true);
    }

    public override bool needsToExecuteOnBonusGameEnded()
    {
	    return reelGame.isFreeSpinGame();
    }

    public override IEnumerator executeOnBonusGameEnded()
    {
	    foreach (JackpotTierToSymbolName jackpotTierToSymbolName in jackpotTierToSymbolNames)
	    {
		    if (!jackpotTierToSymbolName.isForBonusGame)
		    {
			    continue;
		    }
		    
		    if (jackpotTierToSymbolName.referenceSymbolInfo != null && jackpotTierToSymbolName.originalPrefab != null)
		    {
			    jackpotTierToSymbolName.referenceSymbolInfo.symbolPrefab = jackpotTierToSymbolName.originalPrefab;
		    }
	    }
	    yield break;
    }

	private JackpotTierToSymbolName getActiveJackpotTierSymbolsData()
    {
	    string currentJackpotTierKey = SlotBaseGame.instance.getCurrentProgressiveJackpotKey();
	    foreach (JackpotTierToSymbolName jackpotTierToSymbolName in jackpotTierToSymbolNames)
	    {
		    if (jackpotTierToSymbolName.jackpotTierName == currentJackpotTierKey)
		    {
			    return jackpotTierToSymbolName;
		    }
	    }
	    return null;
    }
}
