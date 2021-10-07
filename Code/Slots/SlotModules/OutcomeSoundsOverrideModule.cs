using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OutcomeSoundsOverrideModule : SlotModule
{
	[SerializeField] private List<string> paylineSymbolList;
	[SerializeField] private List<string> paylineSoundKeys;
	[SerializeField] private float ROLLUP_DELAY = 0.0f;
	private List<bool> paylineFoundList;

	public override void Awake()
	{
		paylineFoundList = Enumerable.Repeat(false, paylineSymbolList.Count).ToList();
	}

	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		if (paylineSymbolList.Count != paylineSoundKeys.Count)
		{
			Debug.LogError("Payline Symbol List Count should match Payline Sound Key count!");
			return false;
		}

		paylineFoundList = Enumerable.Repeat(false, paylineSymbolList.Count).ToList();

		bool foundOne = false;

		foreach(SlotSymbol sym in slotSymbols)
		{
			for(int i = 0; i < paylineSymbolList.Count; i++)
			{
				string[] symbols = paylineSymbolList[i].Split('_');
				foreach (string symbolName in symbols)
				{
					if (symbolName == sym.serverName)
					{
						paylineFoundList[i] = true;
						foundOne = true;
						break;
					}
				}
			}
		}

		return foundOne;
	}

	public override void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		for (int i = 0; i < paylineFoundList.Count; i++)
		{
			if(paylineFoundList[i])
			{
				Audio.play(Audio.soundMap(paylineSoundKeys[i]));
			}
		}
	}

	public override float getRollupDelay()
	{
		return ROLLUP_DELAY;
	}
}
