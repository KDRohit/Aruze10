using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Module to handle playing special audio cues that are meant to play when a specific symbol stops in a reel

public class PlaySpecialSymbolSoundOnSpinEndModule : SlotModule 
{
	[SerializeField] private bool isExecutingOnReelEndRollback = false; // some games may want the sounds to play during the rollback instead of on spin ending, use this flag for this
	[SerializeField] private string symbolName; //Play the sound when this symbol lands
	public List<SpecialSymbolSound> soundsToPlay;

	public override bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		return !isExecutingOnReelEndRollback;
	}
	
	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		playSoundsForSymbolOnReel(stoppedReel);
	}

// executeOnReelEndRollback() section
// functions here are called by the SpinReel incrementBonusHits() function
// currently only used by gwtw01 for it's funky bonus symbol sounds 
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return isExecutingOnReelEndRollback;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		// This function doesn't block the spin from ending. Be wary of using it.
		playSoundsForSymbolOnReel(reel);
		yield break;
	}

	private void playSoundsForSymbolOnReel(SlotReel reel)
	{
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			if (reel.visibleSymbols[i].name.StartsWith(symbolName))
			{
				foreach (SpecialSymbolSound currentSound in soundsToPlay)
				{
					if (currentSound.useSoundMapping)
					{
						Audio.play(Audio.soundMap(currentSound.soundName), 1.0f, 0f, currentSound.delayBeforeSound, 0.0f);
					}
					else
					{
						Audio.play(currentSound.soundName, 1.0f, 0f, currentSound.delayBeforeSound, 0.0f);
					}
				}
			}
		}
	}

	//Data structure to hold basic properties we might need for a soundclip
	[System.Serializable]
	public class SpecialSymbolSound
	{
		public string soundName = ""; //Name of audio clip or sound map
		public float delayBeforeSound = 0; //amount of time to delay before playing the sound
		public bool useSoundMapping = false; //Use soundMaps or not.
	}
}
