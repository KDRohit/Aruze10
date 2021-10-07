using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This module will play everytime a major symbol lands on the reel regardless of its type (M1, M2, F6, BN, etc...) with the abilty to 
public class PlaySoundOnMegaSymbolLand : SlotModule
{
	//Use this enum to keep the inspector clearer and less error prone
	private enum SymbolAnimation
	{
		NONE, 
		OUTCOME, 
		ANTICIPATION
	}

	//Set to true if you only want the mega sound to play on major megas
	[SerializeField] private bool shouldPlayForMajorsOnly = false;
	[SerializeField] private bool shouldExcludeBonusSymbols = true;
	[SerializeField] private string[] specificSymbolList;
	[SerializeField] private AudioListController.AudioInformationList landingSounds;
	//We may want an animation to play when the symbols land
	[SerializeField] private SymbolAnimation animationToPlay = SymbolAnimation.NONE;
	//Any landing sounds we may want to play
	[SerializeField] private AudioListController.AudioInformationList animationSounds;
	[Tooltip("Set this to true if the keys should include symbol postfixes. Ex: symbol_animation_mega_m1")]
	[SerializeField] private bool animationSoundsUsePerSymbolPostfixes = false;
	[SerializeField] private bool symbolNeedsToBeWinningSymbol = true;
	[SerializeField] private bool isSkippingOnReevaluationSpin = false; // controls if sounds play on respins, can help prevent extra sounds from playing for reels that aren't spinning

	private HashSet<SlotSymbol> symbolsAnimatedThisSpin = new HashSet<SlotSymbol>(); // Store out which symbols have already been animated so we don't animate a mega symbol more than once
	private List<TICoroutine> symbolCoroutineList = new List<TICoroutine>(); // List of coroutines so we can block and make sure symbol sounds/anims finish before continuing to the results/next spin

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		bool shouldExecute = true;
		if (isSkippingOnReevaluationSpin && reelGame.currentReevaluationSpin != null)
		{
			// doing a reevaluation spin and we are skipping those
			shouldExecute = false;
		}

		return shouldExecute;
	}

	private bool isNameInSpecificSymbolList(string name)
	{
		// check against the specific list
		for (int i = 0; i < specificSymbolList.Length; i++)
		{
			if (name == specificSymbolList[i])
			{
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		HashSet<SlotSymbol> winningSymbolsForReel = reelGame.outcomeDisplayController.getSetOfWinningSymbolsForReel(reelGame.outcome, stoppedReel.reelID - 1, stoppedReel.position, stoppedReel.layer);

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(stoppedReel.reelID - 1))
		{
			if (specificSymbolList != null && specificSymbolList.Length > 0)
			{
				// check against the specific list
				if (!isNameInSpecificSymbolList(symbol.shortServerName))
				{
					// not a symbol we care about
					continue;
				}
			}
			else
			{
				//We may not want to do anything if the symbol is a minor mega
				if ((symbol.isMinor && shouldPlayForMajorsOnly) || (symbol.isBonusSymbol && shouldExcludeBonusSymbols))
				{
					continue;
				}
			}

			if (symbol.isMegaSymbolPart)
			{
				// Get the animator part of the mega symbol since that is what we want to check
				SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();

				if (animatorSymbol != null)
				{
					bool shouldPlay = true;
					if (symbolNeedsToBeWinningSymbol)
					{
						shouldPlay = winningSymbolsForReel.Contains(animatorSymbol);
					}

					// If we are the first part of the mega symbol and wholy on screen play sound (and maybe animation)
					if (!symbolsAnimatedThisSpin.Contains(animatorSymbol) && animatorSymbol.isWhollyOnScreen && shouldPlay)
					{
						symbolsAnimatedThisSpin.Add(animatorSymbol);
						symbolCoroutineList.Add(StartCoroutine(playSymbolAnimAndSound(animatorSymbol)));
					}
				}
			}
		}

		yield break;
	}

	private IEnumerator playSymbolAnimAndSound(SlotSymbol symbol)
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(landingSounds));

		//Do we want to play on of the symbol animations
		if (animationToPlay == SymbolAnimation.OUTCOME)
		{
			symbol.animateOutcome();
		}
		else if (animationToPlay == SymbolAnimation.ANTICIPATION)
		{
			symbol.animateAnticipation();
		}

		if (animationSounds != null && animationSounds.Count > 0)
		{
			//List to hold the unchanged audio keys we'll need to set them back too
			List<string> soundKeys = new List<string>();
			if (animationSoundsUsePerSymbolPostfixes)
			{
				foreach (AudioListController.AudioInformation audioInfo in animationSounds.audioInfoList)
				{
					soundKeys.Add(audioInfo.SOUND_NAME);
					//add the per symbol post fix to the sounds
					audioInfo.SOUND_NAME += "_" + symbol.shortName; //Look at love01: symbol_animation_mega -> symbol_animation_mega_m1 or symbol_animation_mega_m2 etc...
				}
			}
			//This will play either the sound we set in the inspector or the per symbol sounds set above
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(animationSounds));

			//If we changed the sound names reset the sound keys to the orginal unaltered names
			if (soundKeys.Count > 0)
			{
				for (int i = 0; i < soundKeys.Count; i++)
				{
					animationSounds.audioInfoList[i].SOUND_NAME = soundKeys[i];
				}
			}
		}

		// wait for the symbol animation to complete
		while (symbol.isAnimating)
		{
			yield return null;
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// if we've played sounds/animated any symbols we might want to wait until 
		// those are done before proceeding to the next spin, otherwise animations and
		// sounds might overlap
		return symbolCoroutineList.Count > 0;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Wait for symbol animation and sounds to finish form this module before proceeding
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolCoroutineList));
		symbolCoroutineList.Clear();
		symbolsAnimatedThisSpin.Clear();
	}
}
