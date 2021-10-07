using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Code for handling special reel in games like ainsworth06 that has a multiplier reel
which needs to animate symbols and play sounds.

Original Author: True Vegas Code
*/
public class FreespinBonusReelMultiplier : SlotModule 
{
	public bool playSound = false;
	public string soundKey = "";
	[SerializeField] private int bonusReelID = -1;

	private bool soundPlayed = false;

	public override bool needsToExecuteOnPaylinesPayoutRollup()
	{
		soundPlayed = false;
		return false;
	}

	public override bool needsToExecuteOnPaylineDisplay()
	{
		return true;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		if (reelGame.isFreeSpinGame())
		{
			SlotReel[] reels = reelGame.engine.getReelArray();
			SlotSymbol[] symbols = reels[reels.Length - 1].visibleSymbols;

			foreach (SlotSymbol symbol in symbols)
			{
				if (symbol.info.outcomeAnimation == SymbolAnimationType.NONE)
				{
					// This doesn't have an animation so we will skip it
					continue;
				}

				if (playSound)
				{
					if (symbol.name != "BN" && soundPlayed == false)
					{
						Audio.play(Audio.soundMap(soundKey));
						soundPlayed = true;
					}
				}

				if (symbol.reel.reelID == bonusReelID)
				{
					yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
				}
			}
		}
		yield break;
	}
}
