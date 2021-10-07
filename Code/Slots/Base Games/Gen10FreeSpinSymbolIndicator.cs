using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Helper class for the feature of Gen10 free spins
*/
public class Gen10FreeSpinSymbolIndicator : TICoroutineMonoBehaviour 
{
	[SerializeField] private Animator idleAnimator = null;		// Controls the wild pulse idle animations
	[SerializeField] private GameObject[] symbols = null; 		// List of symbol visuals that can be turned on
	[SerializeField] private GameObject particleTrail = null;	// Particle trail for when the symbol is flying to where it is going

	/// Maps to moonSymbols, M1 is a special case with a different animation set
	private enum SymbolEnum
	{
		M1 = 0,
		M2 = 1,
		M3 = 2,
		M4 = 3,
		F5 = 4,
		F6 = 5,
		F7 = 6,
		F8 = 7,
		F9 = 8
	};

	private readonly Dictionary<string, SymbolEnum> SYMBOL_NAME_MAPPING = new Dictionary<string, SymbolEnum>()
	{		
		{ "M1-2A", SymbolEnum.M1 },
		{ "M2", SymbolEnum.M2 }, 
		{ "M3", SymbolEnum.M3 }, 
		{ "M4", SymbolEnum.M4 }, 
		{ "F5", SymbolEnum.F5 }, 
		{ "F6", SymbolEnum.F6 }, 
		{ "F7", SymbolEnum.F7 }, 
		{ "F8", SymbolEnum.F8 },
		{ "F9", SymbolEnum.F9 }
	};  

	private const string SYMBOL_IDLE_ANIM_NAME = "GEN10 symbol idle";
	private const string M1_IDLE_ANIM_NAME = "GEN10 M1 idle";

	// Control the visibility of the particleTrail
	public void setParticleTrailVisible(bool isVisible)
	{
		particleTrail.SetActive(isVisible);
	}

	/// Play the idle animation for this symbol indicator
	public void playSymbolAnimation(string symbolName)
	{
		if (SYMBOL_NAME_MAPPING.ContainsKey(symbolName))
		{
			SymbolEnum targetSymbol = SYMBOL_NAME_MAPPING[symbolName];
			turnOnSymbolVisual(targetSymbol);

			if (targetSymbol != SymbolEnum.M1)
			{
				idleAnimator.Play(SYMBOL_IDLE_ANIM_NAME);
			}
			else
			{
				idleAnimator.Play(M1_IDLE_ANIM_NAME);
			}
		}
		else
		{
			Debug.LogError("Gen10FreeSpinSymbolIndicator():playSymbolAnimation - Don't know how to handle symbol: " + symbolName);
		}
	}

	/// Handle turning on a symbol visual which will be idling
	private void turnOnSymbolVisual(SymbolEnum targetSymbol)
	{
		for (int i = 0; i < symbols.Length; i++)
		{
			if ((int)targetSymbol == i)
			{
				symbols[i].SetActive(true);
			}
			else
			{
				symbols[i].SetActive(false);
			}
		}
	}
}
