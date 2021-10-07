using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayOverridenSymbolSoundOnPaylineAnimationModule : SlotModule
{
	[SerializeField] private CommonDataStructures.SerializableDictionaryOfStringToString symbolSoundsDictionary = new CommonDataStructures.SerializableDictionaryOfStringToString();
	[Tooltip("If this is enabled it will try to match the symbol name including _Variant part to the entries in symbolSoundsDictionary.  Useful if each variant is intended to have a different payline sound.")]
	[SerializeField] private bool isSearchingUsingVariantName = false;
	
	private string soundToPlay = ""; //Caching this to prevent double dictionary look ups

	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		soundToPlay = "";
		string value = "";
		foreach (SlotSymbol sym in slotSymbols)
		{
			string nameToSearchFor = sym.shortServerName;
			if (isSearchingUsingVariantName)
			{
				nameToSearchFor = sym.shortServerNameWithVariant;
			}
			
			if (symbolSoundsDictionary.TryGetValue(nameToSearchFor, out value))
			{
				soundToPlay = value;
				return true;
			}
		}
		return false;
	}

	public override void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		Audio.playSoundMapOrSoundKey(soundToPlay);
	}
}
