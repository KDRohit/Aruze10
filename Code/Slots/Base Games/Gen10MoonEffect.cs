using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Helper class for the feature of Gen10 base and free spins
*/
public class Gen10MoonEffect : TICoroutineMonoBehaviour 
{
	[SerializeField] private Animator moonAnimator = null;		// The animation component for the moon revealing the symbols
	[SerializeField] private GameObject shroud = null;			// Shroud that darkens the reels
	[SerializeField] private GameObject[] moonSymbols = null; 	// List of moon symbols that need to be turned on, doesn't include the M1 symbol which is 2 high
	[SerializeField] private float FEATURE_EFFECT_START_ANIM_LENGTH = 3.0f;
	[SerializeField] private float SYMBOL_SHOW_IDLE_TIME = 1.5f;
	[SerializeField] private float MOON_HOWL_SOUND_DELAY = 1.5f;

	/// Maps to moonSymbols, M1 is a special case with a different animation set
	private enum SymbolEnum
	{
		M1 = -1,
		M2 = 0,
		M3 = 1,
		M4 = 2,
		F5 = 3,
		F6 = 4,
		F7 = 5,
		F8 = 6,
		F9 = 7
	};

	private static readonly Dictionary<string, SymbolEnum> SYMBOL_NAME_MAPPING = new Dictionary<string, SymbolEnum>()
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

	private const string FEATURE_HIDDEN_ANIM_NAME = "GEN10 Feature hidden";
	private const string FEATURE_EFFECT_START_ANIM_NAME = "GEN10 Feature effect_start";
	private const string FEATURE_EFFECT_M1_START_ANIM_NAME = "GEN10 Feature effect_start M1";
	private const string FEATURE_EFFECT_IDLE_ANIM_NAME = "GEN10 Feature effect_idle";
	private const string FEATURE_EFFECT_M1_IDLE_ANIM_NAME = "GEN10 Feature effect_idle M1";

	// Sound names
	private const string MOON_ENTER_SOUND = "WildMoonEnterFoley";
	private const string MOON_BACKGROUND_SOUND = "WildMoonBgCoyote";
	private const string MOON_HOWL_SOUND = "WildMoonHowlCoyote";
	private const string REEL_STOP_SOUND_IF_HAS_WILD = "WildMoonLittleHowl";

	// allows the animaiton to be turned off, but leave the shroud up, used by free spins so it can replace the symbol with a moveable version
	public void hideFeatureAnimation()
	{
		moonAnimator.Play(FEATURE_HIDDEN_ANIM_NAME);
	}

	// turns the feature off, in function form so the free spin game can turn it off when needed
	public void hideFeature()
	{
		shroud.SetActive(false);
		hideFeatureAnimation();
	}

	/// Play the moon animation to reveal the symbol that is changing to wilds
	/// Note passing doWildReplacement by reference so I can modify the value during the animation so symbols on the reels start having the wild text attached
	public IEnumerator playMoonFeature(string symbolName)
	{

		if (SYMBOL_NAME_MAPPING.ContainsKey(symbolName))
		{
			ReelGame reelGame = ReelGame.activeGame;

			shroud.SetActive(true);

			SymbolEnum symbol = SYMBOL_NAME_MAPPING[symbolName];

			Audio.play(MOON_ENTER_SOUND);
			Audio.play(MOON_BACKGROUND_SOUND);
			Audio.play(MOON_HOWL_SOUND, 1.0f, 0.0f, MOON_HOWL_SOUND_DELAY);
			if (symbol != SymbolEnum.M1)
			{
				turnOnSymbolVisual(symbol);

				// play the moon reveal animation
				moonAnimator.Play(FEATURE_EFFECT_START_ANIM_NAME);
				yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);

				if (!reelGame.isFreeSpinGame())
				{
					Gen10 gen10BaseGame = reelGame as Gen10;
					gen10BaseGame.doWildReplacement = true;
					// Go through each reel and see if it's going to land with any of the symbols.
					foreach (SlotReel reel in gen10BaseGame.engine.getAllSlotReels())
					{
						foreach (string name in reel.getFinalReelStopsSymbolNames())
						{
							if (name == symbolName)
							{
								reel.reelStopSoundOverride = REEL_STOP_SOUND_IF_HAS_WILD;
							}
						}
					}
				}

				// now show the idle for a second or two so the player sees what it is
				moonAnimator.Play(FEATURE_EFFECT_IDLE_ANIM_NAME);
				yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);

				if (!reelGame.isFreeSpinGame())
				{
					hideFeature();
				}
			}
			else
			{
				// play the moon reveal animation
				moonAnimator.Play(FEATURE_EFFECT_M1_START_ANIM_NAME);
				yield return new TIWaitForSeconds(FEATURE_EFFECT_START_ANIM_LENGTH);

				if (!reelGame.isFreeSpinGame())
				{
					Gen10 gen10BaseGame = reelGame as Gen10;
					gen10BaseGame.doWildReplacement = true;
				}

				// now show the idle for a second or two so the player sees what it is
				moonAnimator.Play(FEATURE_EFFECT_M1_IDLE_ANIM_NAME);
				yield return new TIWaitForSeconds(SYMBOL_SHOW_IDLE_TIME);

				if (!reelGame.isFreeSpinGame())
				{
					hideFeature();
				}
			}
		}
		else
		{
			Debug.LogError("Gen10MoonEffect():playMoonFeature - Don't know how to handle symbol: " + symbolName);
			yield break;
		}
	}

	/// Handle turning on a symbol visual which reveals in the moon animation
	private void turnOnSymbolVisual(SymbolEnum symbol)
	{
		for (int i = 0; i < moonSymbols.Length; i++)
		{
			if ((int)symbol == i)
			{
				moonSymbols[i].SetActive(true);
			}
			else
			{
				moonSymbols[i].SetActive(false);
			}
		}
	}
}
