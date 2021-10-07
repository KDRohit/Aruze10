using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Helper class for the feature of Gen10 free spins
*/
public class WildOverlayTransformFreeSpinSymbolIndicator : TICoroutineMonoBehaviour 
{
	public bool shouldHideSymbolDurningTween = false;  //Will hide the symbol only using the particle trail to tween to the upper element and turn on the symbol upon arrival
	[SerializeField] private Animator idleAnimator = null;		// Controls the wild pulse idle animations
	[SerializeField] private GameObject[] symbols = null; 		// List of symbol visuals that can be turned on
	[SerializeField] private GameObject particleTrail = null;	// Particle trail for when the symbol is flying to where it is going
	[SerializeField] private string SYMBOL_IDLE_ANIM_NAME = "";
	[SerializeField] private string M1_IDLE_ANIM_NAME = "";
	[SerializeField] private string[] symbolIdleAnimOverrides; // Overides for the idle animations for games that might need unique animaitons say for minor symbols, needs to match up with symbols array

	// Control the visibility of the particleTrail
	public void setParticleTrailVisible(bool isVisible)
	{
		particleTrail.SetActive(isVisible);
	}

	/// Play the idle animation for this symbol indicator
	public void playSymbolAnimation(int symbolIndex, bool doSpecialCaseForM1)
	{
		if (symbolIndex >= 0 && symbolIndex < symbols.Length)
		{
			if (!shouldHideSymbolDurningTween)
			{
				turnOnSymbolVisual(symbolIndex);
			}

			string idleAnimOverride = getSymbolIdleAnimOverride(symbolIndex);

			if (idleAnimOverride != "")
			{
				idleAnimator.Play(idleAnimOverride);
			}
			else
			{
				if (doSpecialCaseForM1)
				{
					idleAnimator.Play(M1_IDLE_ANIM_NAME);
				}
				else
				{
					idleAnimator.Play(SYMBOL_IDLE_ANIM_NAME);
				}
			}
		}
		else
		{
			Debug.LogError("WildOverlayTransformFreeSpinSymbolIndicator():playSymbolAnimation - symbolIndex out of range!");
		}
	}

	/// Get an override animaiton if one is set
	private string getSymbolIdleAnimOverride(int symbolIndex)
	{
		if (symbolIdleAnimOverrides != null && symbolIndex >= 0 && symbolIndex < symbolIdleAnimOverrides.Length)
		{
			return symbolIdleAnimOverrides[symbolIndex];
		}
		else
		{
			return "";
		}
	}

	/// Handle turning on a symbol visual which will be idling
	public void turnOnSymbolVisual(int symbolIndex)
	{
		for (int i = 0; i < symbols.Length; i++)
		{
			if (symbolIndex == i)
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
