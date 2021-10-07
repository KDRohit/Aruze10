using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the RHW01 Real Housewives of Atlantas!
*/
public class Moon01 : SlotBaseGame
{
	private int numFinishedBonusSymbolAnims = 0;
	private int numStartedBonusSymbolAnims = 0;

	// Sound names
	private const string FREESPIN_SOUND = "SymbolFreespinMoonpies";
	private const string PICKEM_SOUND = "SymbolPickBonusMoonpies";
	private const string TW_VO_INTRO_SOUND = "TWIntroVOMoonpies";
	private const string TW_EXPAND_SOUND = "TWExpandSundaeMoonpies";
	private const string TW_VO_MID_SOUND = "TWMidVOMoonpie";
	private const string TW_FILL_SOUND = "TWSundaeFillsMoonpies";
	private const string TW_END_SOUND = "TWEndMoonpies";
	private const string TW_REACT_VO_SOUND = "TWReaxVOMoonpies";

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
