using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module for handling granting a dynamic number of freespins from trigger symbols.
 * First used by gen99
 * Author: Caroline 4/2020
 */
public class DynamicNumberOfFreespinsFromSymbolsModule : SlotModule
{
	[Tooltip("Label displaying sum total of freespins from each symbol")]
	[SerializeField] private LabelWrapperComponent freespinsTotalLabel;
	[Tooltip("Animation to play after each symbol has revealed its freespin count to display to the player total freespins granted")]
	[SerializeField] private AnimationListController.AnimationInformationList freespinsGrantedSumDisplayAnimations;
	[Tooltip("Mapping of number of freespins granted by a symbol to the animation name to play on reveal")]
	[SerializeField] private List<CollectedFreespinsRevealSymbolForAmount> freespinRevealSymbolVariants;
	[Tooltip("Delay after playing outcome animation for revealed symbol")]
	[SerializeField] private float freespinRevealSymbolDelay;
	[Tooltip("Delay after playing outcome animations per reel during reveal")]
	[SerializeField] private float freespinRevealPerReelDelay;
	[Tooltip("Sounds for playing outcome animations per reel during reveal")]
	[SerializeField] private AudioListController.AudioInformationList freespinRevealPerReelSound;

	// kvp: reel index, symbols that contribute to dynamic freespins sum
	private Dictionary<int, List<FreespinsGrantingSymbol>> freespinsGrantingSymbols = new Dictionary<int, List<FreespinsGrantingSymbol>>();
	private int freespinsGrantedSum;
	private List<TICoroutine> freespinRevealsPerReelCoroutines = new List<TICoroutine>();
	
	private class FreespinsGrantingSymbol
	{
		// 0-indexed reel index and position
		public int reel;
		public int position;
		public string symbolName;
		public int freespinsGranted;
	}

	[System.Serializable]
	public class CollectedFreespinsRevealSymbolForAmount
	{
		[Tooltip("Number of freespins granted")]
		public int freespinsGranted;
		[Tooltip("Symbol variant to mutate to when revealing freespins granted")]
		public string revealSymbolName;
	}

	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return true;
	}

	public override IEnumerator executeOnPreBonusGameCreated()
	{
		// get reevaluator by type
		JSON triggerSymbolsReevaluator = getTriggerSymbolsReevaluator();
		if (triggerSymbolsReevaluator == null)
		{
			yield break;
		}
		// parse data
		parseCollectedFreespinsSymbols(triggerSymbolsReevaluator.getJsonArray("trigger_symbols"));

		if (freespinsGrantingSymbols.Count <= 0)
		{
			yield break;
		}
		
		freespinsGrantedSum = 0;
		freespinRevealsPerReelCoroutines.Clear();
		for (int reelIndex = 0; reelIndex < reelGame.engine.getAllSlotReels().Length; reelIndex++)
		{
			List<FreespinsGrantingSymbol> triggerSymbols;
			if (freespinsGrantingSymbols.TryGetValue(reelIndex, out triggerSymbols))
			{
				if (freespinRevealPerReelSound.Count > 0)
				{
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(freespinRevealPerReelSound));
				}
				freespinRevealsPerReelCoroutines.Add(StartCoroutine(doTriggerSymbolRevealOnReel(reelIndex, triggerSymbols)));
				if (freespinRevealPerReelDelay > 0)
				{
					yield return new TIWaitForSeconds(freespinRevealPerReelDelay);
				}
			}
		}

		if (freespinRevealsPerReelCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(freespinRevealsPerReelCoroutines));
		}

		if (freespinsTotalLabel != null)
		{
			freespinsTotalLabel.text = CommonText.formatNumber(freespinsGrantedSum);
		}

		if (freespinsGrantedSumDisplayAnimations != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freespinsGrantedSumDisplayAnimations));
		}
	}

	private IEnumerator doTriggerSymbolRevealOnReel(int reelIndex, List<FreespinsGrantingSymbol> triggerSymbols)
	{
		SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
		foreach (FreespinsGrantingSymbol triggerSymbol in triggerSymbols)
		{
			if (reel != null && triggerSymbol.position >= 0 && triggerSymbol.position < reel.visibleSymbols.Length)
			{
				SlotSymbol symbol = reel.visibleSymbolsBottomUp[triggerSymbol.position];
				if (symbol != null)
				{
					freespinsGrantedSum += triggerSymbol.freespinsGranted;
					string newSymbolName = getFreespinsRevealAmountVariantName(triggerSymbol.freespinsGranted);
					if (!string.IsNullOrEmpty(newSymbolName))
					{
						symbol.mutateTo(newSymbolName);
						
						// play reveal animation
						yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
						
						if (freespinRevealSymbolDelay > 0)
						{
							yield return new TIWaitForSeconds(freespinRevealSymbolDelay);
						}
					}
					else
					{
						Debug.Log("Missing freespin amount reveal symbol data");
					}
				}
			}
			else
			{
				Debug.LogError("Invalid symbol data for collected freespins symbol");
			}
		}
	}

	private void parseCollectedFreespinsSymbols(JSON[] triggerSymbols)
	{
		clearFreespinGrantingSymbolsDictionary();
		
		if (triggerSymbols == null)
		{
			return;
		}
		
		foreach (JSON triggerSymbol in triggerSymbols)
		{
			int reel = triggerSymbol.getInt("reel", -1);
			int position = triggerSymbol.getInt("position", -1);
			int freespinsGranted = triggerSymbol.getInt("free_spins", 0);
			string symbolName = triggerSymbol.getString("symbol", "");
			if (reel >= 0 && position >= 0 && !string.IsNullOrEmpty(symbolName))
			{
				FreespinsGrantingSymbol freespinsGrantingSymbol = new FreespinsGrantingSymbol();
				freespinsGrantingSymbol.reel = reel;
				freespinsGrantingSymbol.position = position;
				freespinsGrantingSymbol.freespinsGranted = freespinsGranted;
				freespinsGrantingSymbol.symbolName = symbolName;
				if (!freespinsGrantingSymbols.ContainsKey(reel))
				{
					freespinsGrantingSymbols[reel] = new List<FreespinsGrantingSymbol>();
				}
				freespinsGrantingSymbols[reel].Add(freespinsGrantingSymbol);
			}
			else
			{
				Debug.LogError("CollectedFreespinsSymbol had invalid server data!");
			}
		}
	}

	private void clearFreespinGrantingSymbolsDictionary()
	{
		foreach (int key in freespinsGrantingSymbols.Keys)
		{
			freespinsGrantingSymbols[key].Clear();
		}
	}

	private JSON getTriggerSymbolsReevaluator()
	{
		JSON[] reevaluations = reelGame.outcome.getArrayReevaluations();
		foreach (JSON reevaluation in reevaluations)
		{
			string type = reevaluation.getString("type", "");
			if (!string.IsNullOrEmpty(type) && type.Equals("trigger_bonus_with_rolling_symbol"))
			{
				return reevaluation;
			}
		}

		return null;
	}

	private string getFreespinsRevealAmountVariantName(int freespinsGranted)
	{
		foreach (CollectedFreespinsRevealSymbolForAmount variantForAmount in freespinRevealSymbolVariants)
		{
			if (variantForAmount.freespinsGranted == freespinsGranted)
			{
				return variantForAmount.revealSymbolName;
			}
		}

		return null;
	}
}
