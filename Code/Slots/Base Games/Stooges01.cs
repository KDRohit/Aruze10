using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the RHW01 Real Housewives of Atlantas!
*/
public class Stooges01 : SlotBaseGame
{
	private int numFinishedBonusSymbolAnims = 0;
	private int numStartedBonusSymbolAnims = 0;

	// Sound names
	private const string FREESPIN_SOUND = "BonusInit3FreespinStooges";
	private const string PICKEM_SOUND = "BonusInit3PickStooges";

	public override IEnumerator playBonusAcquiredEffects()
	{
		if (outcome.isBonus)
		{
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
			{
				Audio.play(FREESPIN_SOUND);
			}
			else
			{
				Audio.play(PICKEM_SOUND);
			}
			//Audio.play(Audio.soundMap("bonus_symbol_animate"));
			yield return StartCoroutine(playBNAnimation());
		}
	}

	private void onBonusSymbolAnimationDone(SlotSymbol sender)
	{
		numFinishedBonusSymbolAnims++;
	}

	private IEnumerator playBNAnimation()
	{
		numFinishedBonusSymbolAnims = 0;
		numStartedBonusSymbolAnims = 0;
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			if (reel.reelID == 1 || reel.reelID == 3 || reel.reelID == 5)
			{
				numStartedBonusSymbolAnims++;
				reel.animateBonusSymbols(onBonusSymbolAnimationDone);
			}
		}
		while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
		{
			yield return null;
		}

		yield return new TIWaitForSeconds(0.7f);
	}
}
