///Author: Hans Hameline
///Date: Feb 25, 2016
///This module should be used when you have a multiplier reel you want to animate with a payline. 
///Please note this will only animate the multiplier if you have a multiplier value greater than 1 and if you win a payline.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiplierReelAnimationModule : SlotModule
{
	[SerializeField] protected int multiplierReelID = 0;
	[SerializeField] protected int multiplierSymbolIndex = 1;
	[SerializeField] protected bool hasBNSymbolOnMultiplierReel = true;
	[SerializeField] protected bool playSymbolAnimationAndSpecialAnimationsTogether = false;
	[SerializeField] protected string BN_SYMBOL_INIT_KEY = "bonus_symbol_fanfare3";

	[SerializeField] protected bool playSymbolVOEffectEveryTime = false;
	[SerializeField] protected bool playSymbolSoundEffectEveryTime = false;
	[SerializeField] protected bool playSymbolAnimationEffectEveryTime = false;
	[SerializeField] protected List<SymbolEffects> symbolEffectsList;
	[SerializeField] protected AnimationListController.AnimationInformationList preSpinSymbolEffects;

	protected const string SPECIAL_REEL_SPIN_START_SOUND_KEY = "last_reel_multiplier_spin_start";
	private const string BG_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX = "last_reel_multiplier_";
	private const string FS_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX = "last_reel_fs_multiplier_";
	private const string BN_SYMBOL_ANIMATE_KEY = "bonus_symbol_animate";
	private Dictionary<string, SymbolEffects> _symbolEffectsMap;
	private bool isNewSpin;


	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (_symbolEffectsMap == null && symbolEffectsList != null && symbolEffectsList.Count > 0)
		{
			initSymbolEffectMap();
		}
		isNewSpin = true;

		Audio.play(Audio.soundMap(SPECIAL_REEL_SPIN_START_SOUND_KEY));
		
		if (preSpinSymbolEffects != null && preSpinSymbolEffects.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preSpinSymbolEffects));
		}
	}

	private void initSymbolEffectMap()
	{
		_symbolEffectsMap = new Dictionary<string, SymbolEffects>();

		for (int i = 0; i < symbolEffectsList.Count; i++)
		{
			string symbolName = symbolEffectsList[i].symbolName;
			if (!_symbolEffectsMap.ContainsKey(symbolName))
			{
				_symbolEffectsMap.Add(symbolName, symbolEffectsList[i]);
			}
		}
	}

	public override bool needsToExecuteOnPaylineDisplay()
	{
		if (reelGame.outcome.getBonusMultiplier() > 0 && reelGame.outcome.hasSubOutcomes())
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		SlotSymbol symbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];
		if (symbol.serverName != "BN" && !symbol.isBlankSymbol)
		{
			if(_symbolEffectsMap != null && _symbolEffectsMap.ContainsKey(symbol.serverName))
			{
				playSymbolEffects(symbol);
			}

			if (!symbolHasSpecialAnimations(symbol) || playSymbolAnimationAndSpecialAnimationsTogether)
			{
				symbol.animateOutcome();
			}
		}
		yield break;
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		// run the multiplier stuff on the reel stop callback if the player didn't win anything
		if (reelGame.outcome.getBonusMultiplier() > 0)
		{
			return true;
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		SlotSymbol symbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];
		if (!symbolHasSpecialSoundEffects(symbol))
		{
			playMultiplierSymbolSound(symbol.serverName);
		}
		yield break;
	}

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		//Need this so we can play our sounds and animations before going into the freespins
		return hasBNSymbolOnMultiplierReel;
	}

	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		SlotSymbol bonusSymbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];
		Audio.play(Audio.soundMap(BN_SYMBOL_ANIMATE_KEY));
		bonusSymbol.animateOutcome();
		yield break;
	}

	// executeOnSpecificReelStopping() section
	// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		bool isMultiplierReel = stoppingReel.reelID - 1 == multiplierReelID;
		if (hasBNSymbolOnMultiplierReel && isMultiplierReel)
		{
			SlotSymbol bonusSymbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];
			return bonusSymbol.serverName == "BN";
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		Audio.play(Audio.soundMap(BN_SYMBOL_INIT_KEY));
		yield break;
	}

	// Play the sound for the multiplier that was hit
	protected void playMultiplierSymbolSound(string symbolName)
	{
		if (reelGame.isFreeSpinGame())
		{
			if (Audio.canSoundBeMapped(FS_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX + symbolName))
			{
				Audio.play(Audio.soundMap(FS_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX + symbolName));
			}
		}
		else
		{
			if (Audio.canSoundBeMapped(BG_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX + symbolName))
			{
				Audio.play(Audio.soundMap(BG_MULTIPLER_REEL_REVEAL_SOUND_KEY_PREFIX + symbolName));
			}
		}
	}

	// Plays any sounds, voice over, and animations associated with this symbol.
	public virtual void playSymbolEffects(SlotSymbol symbol)
	{
		SymbolEffects symbolEffects = _symbolEffectsMap[symbol.serverName];
		if (symbolEffects.voEffects != null && symbolEffects.voEffects.Count > 0)
		{
			if (playSymbolVOEffectEveryTime || isNewSpin)
			{
				StartCoroutine(AudioListController.playListOfAudioInformation(symbolEffects.voEffects));
			}
		}

		if (symbolEffects.soundEffects != null && symbolEffects.soundEffects.Count > 0)
		{
			if (playSymbolSoundEffectEveryTime || isNewSpin)
			{
				StartCoroutine(AudioListController.playListOfAudioInformation(symbolEffects.soundEffects));
			}
		}

		if (symbolEffects.animationEffects != null && symbolEffects.animationEffects.Count > 0)
		{
			if (playSymbolAnimationEffectEveryTime || isNewSpin)
			{
				StartCoroutine(AnimationListController.playListOfAnimationInformation(symbolEffects.animationEffects));
			}
		}

		isNewSpin = false;
	}

	// Helper method to check if a symbol has special animations defined.
	private bool symbolHasSpecialAnimations(SlotSymbol symbol)
	{
		if(_symbolEffectsMap != null && _symbolEffectsMap.ContainsKey(symbol.serverName))
		{
			SymbolEffects symbolEffects = _symbolEffectsMap[symbol.serverName];
			if(symbolEffects.animationEffects != null && symbolEffects.animationEffects.Count > 0)
			{
				return true;
			}
		}

		return false;
	}

	// Helper method to check if a symbol has special animations defined.
	private bool symbolHasSpecialSoundEffects(SlotSymbol symbol)
	{
		if(_symbolEffectsMap != null && _symbolEffectsMap.ContainsKey(symbol.serverName))
		{
			SymbolEffects symbolEffects = _symbolEffectsMap[symbol.serverName];
			if(symbolEffects.soundEffects != null && symbolEffects.soundEffects.Count > 0)
			{
				return true;
			}
		}

		return false;
	}

	// Special Container to hold sound and animations for a specific symbol.
	[System.Serializable]
	protected class SymbolEffects
	{
		public string symbolName;
		public AudioListController.AudioInformationList soundEffects;
		public AudioListController.AudioInformationList voEffects;
		public AnimationListController.AnimationInformationList animationEffects;
	}
}
