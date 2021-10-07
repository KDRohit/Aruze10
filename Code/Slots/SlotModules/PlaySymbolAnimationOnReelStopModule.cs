using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Calls animateOutcome for SlotSymbols matching symbolName on the reel matching reelID;
public class PlaySymbolAnimationOnReelStopModule : SlotModule 
{
	[SerializeField] private int 		reelID;
	[SerializeField] private string 	symbolName;
	[SerializeField] private string 	SYMBOL_SOUND;
	[SerializeField] private float 		SYMBOL_SOUND_DELAY;
	[SerializeField] private string     BG_MUSIC_CHANGE;
	[SerializeField] private bool 		playAnticipation = false;

	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppingReel)
	{
		return (stoppingReel.reelID == reelID);
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppingReel)
	{
		for (int i = 0; i < stoppingReel.visibleSymbols.Length; i++)
		{         
			SlotSymbol slotSymbol = stoppingReel.visibleSymbols[i];
			if (slotSymbol.serverName.Equals(symbolName))
			{
				if (!string.IsNullOrEmpty(BG_MUSIC_CHANGE))
				{
					Audio.switchMusicKeyImmediate(BG_MUSIC_CHANGE, 1.0f);
				}
				if (playAnticipation)
				{
					if (!slotSymbol.animator.isAnimating)
					{
						slotSymbol.animateAnticipation();
					}
				}
				else
				{
					slotSymbol.animateOutcome();
				}
				Audio.play(Audio.soundMap(SYMBOL_SOUND), 1, 0, SYMBOL_SOUND_DELAY);	
			}
		}
		yield return null;
	}
}