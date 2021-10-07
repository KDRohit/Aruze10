using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// Collects symbols with values when symbol_payout_on_payline is in the outcome
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : July 9, 2020
// games : gen95
//
public class ScatterCollectSymbolPayoutOnPayline : ScatterSymbolBaseModule
{
	[SerializeField] private AnimatedParticleEffect scatterCollectParticleEffect;

	private SymbolPayoutOnPayline symbolPayoutOnPayline;
	private SlotReel[] slotReels;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		slotReels = reelGame.engine.getAllSlotReels();
		scatterCollectParticleEffect.particleEffectStartedPrefabEvent.AddListener(particleEffectStartedPrefabEventCallback);
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		extractSymbolPayoutOnPayline();
		return symbolPayoutOnPayline != null;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (scatterCollectParticleEffect != null)
		{
			foreach (SymbolPayoutOnPayline.SymbolCreditsOutcome symbolCreditsOutcome in symbolPayoutOnPayline.symbolCreditsOutcomeList)
			{
				SlotSymbol slotSymbol = slotReels[symbolCreditsOutcome.reel].visibleSymbolsBottomUp[symbolCreditsOutcome.position];
				yield return StartCoroutine(scatterCollectParticleEffect.animateParticleEffect(slotSymbol.transform));
			}
		}
	}

	private void particleEffectStartedPrefabEventCallback(GameObject particleEffect)
	{
		foreach (SymbolPayoutOnPayline.SymbolCreditsOutcome symbolCreditsOutcome in symbolPayoutOnPayline.symbolCreditsOutcomeList)
		{
			SlotSymbol slotSymbol = slotReels[symbolCreditsOutcome.reel].visibleSymbolsBottomUp[symbolCreditsOutcome.position];
			string labelText = CreditsEconomy.multiplyAndFormatNumberAbbreviated(getSymbolValue(slotSymbol), shouldRoundUp: false);
			setLabelText(labelText, particleEffect);
		}
	}

	// Finds a label wrapper and sets the text on it
	private void setLabelText(string labelText, GameObject particleEffect)
	{
		if (string.IsNullOrEmpty(labelText))
		{
			return;
		}

		LabelWrapperComponent labelWrapperComponent = particleEffect.GetComponentInChildren<LabelWrapperComponent>();
		if (labelWrapperComponent != null)
		{
			labelWrapperComponent.text = labelText;
		}
	}

	private void extractSymbolPayoutOnPayline()
	{
		symbolPayoutOnPayline = null;

		JSON[] reevaluationArray = reelGame.outcome.getArrayReevaluations();

		if (reevaluationArray == null || reevaluationArray.Length <= 0)
		{
			return;
		}

		for (int i = 0; i < reevaluationArray.Length; i++)
		{
			if(reevaluationArray[i].getString("type", "") == "symbol_payout_on_payline")
			{
				symbolPayoutOnPayline = new SymbolPayoutOnPayline(reevaluationArray[i]);
			}
		}
	}

	void OnDestroy()
	{
		scatterCollectParticleEffect.particleEffectStartedPrefabEvent.RemoveListener(particleEffectStartedPrefabEventCallback);
	}

	public class SymbolPayoutOnPayline : ReevaluationBase
	{
		public int credits;
		public List<SymbolCreditsOutcome> symbolCreditsOutcomeList = new List<SymbolCreditsOutcome>();

		public SymbolPayoutOnPayline(JSON reevalJSON) : base(reevalJSON)
		{
			credits = reevalJSON.getInt("credits", 0);

			foreach (JSON outcomeJSON in outcomes)
			{
				string outcomeType = outcomeJSON.getString("outcome_type", "");
				if (outcomeType == "symbol_credits")
				{
					SymbolCreditsOutcome symbolCreditsOutcome = new SymbolCreditsOutcome();
					symbolCreditsOutcome.outcomeType = outcomeType;
					symbolCreditsOutcome.reel = outcomeJSON.getInt("reel", 0);
					symbolCreditsOutcome.position = outcomeJSON.getInt("position", 0);
					symbolCreditsOutcome.credits = outcomeJSON.getInt("credits", 0);
					symbolCreditsOutcome.symbol = outcomeJSON.getString("symbol", "");
					symbolCreditsOutcomeList.Add(symbolCreditsOutcome);
				}
			}
		}

		public class SymbolCreditsOutcome
		{
			public string outcomeType;
			public int reel;
			public int position;
			public string symbol;
			public int credits;
		}
	}
}