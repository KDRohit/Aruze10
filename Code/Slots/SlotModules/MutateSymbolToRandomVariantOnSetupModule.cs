using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * SlotModule to handle replace specific symbol randomly.
 * Client replace the original symbol to new symbol choose from a list of symbol names, each symbol name will be selected randomly.
 * After symbol setup, mutating to the new symbol name
 * 
 * Original Author: Sean Chen
 * Date Created: 8/13/2019
 */
    
public class MutateSymbolToRandomVariantOnSetupModule : SlotModule 
{
    
    [System.Serializable]
    protected class GameVariant
    {
        public string originalSymbolName;
        public List<string> newSymbolNames;
    }
    
    [Header("Bonus game ID and symbol names")]
    [SerializeField] private List<GameVariant> symbolNameInfo;
  
    private Dictionary<string, string> mutateSymbolNameInfo = new Dictionary<string, string>();

    public override void Awake()
    {
        base.Awake(); 
        updateMutateSymbolInfo();
    }

    private void updateMutateSymbolInfo()
    {
        mutateSymbolNameInfo.Clear();
        for (int i = 0; i < symbolNameInfo.Count; i++)
        {
            if (string.IsNullOrEmpty(symbolNameInfo[i].originalSymbolName) || symbolNameInfo[i].newSymbolNames.Count < 1)
            {
                continue;
            }
            int index = Random.Range(0, symbolNameInfo[i].newSymbolNames.Count);
            mutateSymbolNameInfo.Add(symbolNameInfo[i].originalSymbolName, symbolNameInfo[i].newSymbolNames[index]);
        }
    }
    public override bool needsToExecuteOnPreSpin()
    {
        return true;
    }
    public override IEnumerator executeOnPreSpin()
    {
        updateMutateSymbolInfo();
        yield break;
    }
    public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
    {
        return mutateSymbolNameInfo.ContainsKey(symbol.serverName);
    }

    public override void executeAfterSymbolSetup(SlotSymbol symbol)
    {
        string newSymbolName = null;
        newSymbolName = mutateSymbolNameInfo[symbol.serverName];
		if (!string.IsNullOrEmpty(newSymbolName) && symbol.name != newSymbolName)
        {
            symbol.mutateTo(newSymbolName, skipAnimation: true);
        }
    }
    
}
