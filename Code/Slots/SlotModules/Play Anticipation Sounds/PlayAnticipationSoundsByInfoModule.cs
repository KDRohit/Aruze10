using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// allows you to define a symbol and sound key to play different anticipation sounds for different symbols

public class PlayAnticipationSoundsByInfoModule : SlotModule 
{

	[System.Serializable]
	public class SymbolAnticipationInfo 
	{
		public string symbolName;
		public string soundKeyToPlay;
		public float delay = 0;
	}

	public SymbolAnticipationInfo[] symbolAnticipationInfo;

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0) 
	{
		return true;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0) 
	{
		// need to check symbols this way since proper symbol infomation isn't passed in
		// check each visible symbol
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(stoppedReel)) 
		{
			// against our supplied symbols
			foreach (SymbolAnticipationInfo info in symbolAnticipationInfo) 
			{
				// if we have something to play
				if (symbol.serverName == info.symbolName) 
				{
					// play our sound
					Audio.playSoundMapOrSoundKeyWithDelay(info.soundKeyToPlay, info.delay);
				}
			}
		}

		yield return null;
	}	

}
