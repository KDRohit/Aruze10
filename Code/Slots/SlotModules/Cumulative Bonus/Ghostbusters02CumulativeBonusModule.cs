using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghostbusters02CumulativeBonusModule : CumulativeBonusToPortalTransitionModule
{
	private const string BONUS_PORTAL_BG_MUSIC_KEY = "bonus_portal_bg";

	public override IEnumerator playCumulativeSymbolAcquiredAnim (Animator cumulativeSymbolAnimator, SlotSymbol symbol)
	{
		if (numBonusSymbolsAcquired == 4)
		{
			Audio.playMusic(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY), 2, 0, true);
		}

		 return base.playCumulativeSymbolAcquiredAnim(cumulativeSymbolAnimator, symbol);
	}
}
