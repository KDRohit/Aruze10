using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// base module to initialize SC symbol values and update them on every spin.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : 2019-12-12
// games : billions02
//
public class ScatterSymbolBaseModule : SlotModule
{
	// These key values can vary depending on the reevaluator the server uses for some reason.
	// So to support the server we expose these in the event they vary, like for gen95.
	[SerializeField] private string BASEGAME_SYMBOL_PAYOUT_KEY_SUFFIX = "_base_payout";
	[SerializeField] private string FREESPIN_SYMBOL_PAYOUT_KEY_SUFFIX = "_freespin_payout";

	protected Dictionary<string, long> symbolCreditMap = new Dictionary<string, long>();
	protected bool didInit = false;
	private string symbolPayoutKeyName;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		// if we're in a freespin game we should grab modifier exports data from base game since data isn't passed into freespins
		if (FreeSpinGame.instance != null && SlotBaseGame.instance != null)
		{
			return SlotBaseGame.instance.modifierExports != null;
		}
		
		// else we're in the base game (or gifted free spin), check that data exists
		if (reelSetDataJson == null)
		{
			return false;
		}

		return reelSetDataJson.hasKey("modifier_exports");
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		initSymbolCreditMap(reelSetDataJson);
		initScatterSymbolsOnReel();
		didInit = true;
		yield break;
	}

	protected virtual void initSymbolCreditMap(JSON reelSetDataJson)
	{
		JSON[] modifiers;
		if (SlotBaseGame.instance != null && FreeSpinGame.instance != null)
		{
			modifiers = SlotBaseGame.instance.modifierExports;
		}
		else
		{
			modifiers = reelSetDataJson.getJsonArray("modifier_exports");
		}

		// We might want to separate basegame and freespin payout values. Since transitioning from basegame to freespins
		// doesn't pass reelSetData in we instead get two JSON entries when setting up basegame with type symbol_credit_values
		// and use key_name to distinguish between them. See gen93 as an example.
		if (FreeSpinGame.instance != null)
		{
			symbolPayoutKeyName = GameState.game.keyName + FREESPIN_SYMBOL_PAYOUT_KEY_SUFFIX;
		}
		else
		{
			symbolPayoutKeyName = GameState.game.keyName + BASEGAME_SYMBOL_PAYOUT_KEY_SUFFIX;
		}

		initSymbolCreditMapFromModifiers(modifiers);
	}

	protected void initSymbolCreditMapFromModifiers(JSON[] modifiers)
	{
		for (int i = 0; i < modifiers.Length; i++)
		{
			string modiferExportType = modifiers[i].getString("type", "");
			string modifierExportKeyName = modifiers[i].getString("key_name", "");
			bool hasSymbolPayouts = modifiers[i].hasKey("symbol_payouts");

			if (modiferExportType == "symbol_credit_values")
			{
				// skip entries whose key_name doesn't match, this let's us separate basegame payouts from freespins payouts
				if (string.IsNullOrEmpty(modifierExportKeyName) || modifierExportKeyName.Equals(symbolPayoutKeyName))
				{
					List<SymbolCreditValue> symbolCreditValueList = getSymbolCreditValueList(modifiers[i]);
					initSymbolCreditMapWith(symbolCreditValueList);
				}
			}
			else if (hasSymbolPayouts)
			{
				// In orig010 there is a limitation on the server that prevents unpacking the symbol_payouts
				// into their own proper modifer export, so they are bundled inside another modifier export.
				// This is needed to extract symbol_payouts json from any modifier_export that contains
				// symbol_payouts so it can be used by this module to set the values of SC symbols.
				JSON[] symbolPayoutsJSON = modifiers[i].getJsonArray("symbol_payouts");
				foreach (JSON symbolPayoutJSON in symbolPayoutsJSON)
				{
					List<SymbolCreditValue> symbolCreditValueList = getSymbolCreditValueList(symbolPayoutJSON);
					initSymbolCreditMapWith(symbolCreditValueList);
				}
			}
		}
	}

	protected void initSymbolCreditMapWith(List<SymbolCreditValue> symbolCreditValues)
	{
		foreach (SymbolCreditValue symbolCreditValue in symbolCreditValues)
		{
			symbolCreditMap[symbolCreditValue.symbolName] = symbolCreditValue.credits;
		}
	}

	protected List<SymbolCreditValue>getSymbolCreditValueList(JSON symbolCreditValueModifierExportJson)
	{
		List<SymbolCreditValue> symbolCreditValueList = new List<SymbolCreditValue>();

		JSON[] symbolPayoutJson = symbolCreditValueModifierExportJson.getJsonArray("symbol_payouts");

		foreach (JSON objectJson in symbolPayoutJson)
		{
			symbolCreditValueList.Add(new SymbolCreditValue(objectJson));
		}

		return symbolCreditValueList;
	}

	// Update the credit labels on all the slot symbols when the game starts
	protected void initScatterSymbolsOnReel()
	{
		foreach (SlotSymbol slotSymbol in reelGame.engine.getAllSymbolsOnReels())
		{
			setSymbolLabel(slotSymbol);
		}
	}

	// Sets the credit value of a symbol in the symbolCreditMap.
	protected void setSymbolLabel(SlotSymbol symbol)
	{
		if (symbolCreditMap.ContainsKey(symbol.serverName))
		{
			LabelWrapperComponent symbolLabel = symbol.getDynamicLabel();

			if (symbolLabel != null)
			{
				symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(getSymbolValue(symbol), shouldRoundUp: false);
			}
		}
	}

	protected long getSymbolValue(SlotSymbol symbol)
	{
		return symbolCreditMap[symbol.serverName] * reelGame.multiplier;
	}

	// We need to setup symbols with dynamic labels to have the correct value
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	// Sets the credit value of an SC symbol after it is setup
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		setSymbolLabel(symbol);
	}

	public class SymbolCreditValue
	{
		public string symbolName;
		public long credits;

		public SymbolCreditValue(JSON json)
		{
			symbolName = json.getString("symbol_name", "");
			credits = json.getLong("value", 0);
		}
	}
}

