using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * SlotModule to handle when we need different variations of a symbol depending which bonus game it been selected during picking portal.
 * Client grab the bonus game's name and determine which Symbol should be used to replace original symbol name download from server.
 * After symbol setup, mutating to the new symbol name
 * 
 * Original Author: Sean Chen
 * Date Created: 8/13/2019
 */

public class MutateSymbolByBonusGameNameModule : SlotModule 
{
    [System.Serializable]
    protected class GameVariant
    {
        public string bonusGameName;
        public string symbolName;
    }
    
    [Header("Bonus game ID and symbol names")]
    [SerializeField] private List<GameVariant> bonusGameNameSymbolNameInfo;

    [Tooltip("Original symbol name.")] 
    [SerializeField] private string originalSymbolName;

    private string newSymbolName; 
    
    private Dictionary<string, string> mutateSymbolNameInfo = new Dictionary<string, string>();

    public override void Awake()
    {
        base.Awake();
        
        for (int i = 0; i < bonusGameNameSymbolNameInfo.Count; i++)
        {
            string bonusGameName = null;
            bonusGameName = string.Format(bonusGameNameSymbolNameInfo[i].bonusGameName, GameState.game.keyName);
            mutateSymbolNameInfo.Add(bonusGameName, bonusGameNameSymbolNameInfo[i].symbolName);
        }
    }

    public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
    {
        //make sure we get the correct bonus game name(ID). 
        return !string.IsNullOrEmpty(BonusGameManager.instance.bonusGameName) && symbol.serverName.Equals(originalSymbolName);
    }

    public override void executeAfterSymbolSetup(SlotSymbol symbol)
    {
        if (newSymbolName == null)
        {
            newSymbolName = getMutatedSymbolName(BonusGameManager.instance.bonusGameName);
        }
		
        if (!string.IsNullOrEmpty(newSymbolName) && symbol.name != newSymbolName)
        {
            symbol.mutateTo(newSymbolName, skipAnimation: true);
        }
    }

    private string getMutatedSymbolName(string bonusGameName)
    {
        if (mutateSymbolNameInfo.ContainsKey(bonusGameName))
        {
            return mutateSymbolNameInfo[bonusGameName];
        }
        return null;
    }
    
}
