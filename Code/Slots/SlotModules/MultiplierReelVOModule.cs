using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Module to allow playing of specific voice over audio keys for specific multiplier symbols on multiplier reel.
/// Initially implemented for tv38 (Batman 66) to allow overriding of symbol_animation_M1-M4 audio keys.
/// </summary>

public class MultiplierReelVOModule : SlotModule 
{
	[SerializeField] protected int multiplierReelID = 0;
	[SerializeField] protected int multiplierSymbolIndex = 1;

	// Set this to true if you're using sybmol_animation_ prefixed sound keys. 
	// This will cause those sounds to not be played by the PaylineOutcomeDisplayModule.
	public bool overridePaylineSounds = true;

	public string w2SoundKey;
	public string w3SoundKey;
	public string w5SoundKey;
	public string w8SoundKey;
	public string w10SoundKey;

	private Dictionary<string, string> dict = new Dictionary<string, string>();

	public override void Awake()
	{
		base.Awake();

		dict = new Dictionary<string, string> 
		{
			{"W2", w2SoundKey},
			{"W3", w3SoundKey},
			{"W5", w5SoundKey},
			{"W8", w8SoundKey},
			{"W10", w10SoundKey},
		};
	}

	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		return overridePaylineSounds;
	}

	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.outcome.getBonusMultiplier() > 0)
		{
			// Only returns true if there is a win
			return reelGame.outcome.hasSubOutcomes();
		}
		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		SlotSymbol symbol = reelGame.engine.getVisibleSymbolsAt(multiplierReelID)[multiplierSymbolIndex];

		if (dict.ContainsKey(symbol.serverName) && Audio.canSoundBeMapped(dict[symbol.serverName]))
		{
			Audio.play(Audio.soundMap(dict[symbol.serverName]));
		}
		yield break;
	}

}
