using UnityEngine;
using System.Collections;

public class BillAndTed01AnimatedTransitionModule : BonusGameAnimatedTransition
{
	//We want our BN symbol to be in a specific state when coming back from a transition so we need
	//to mutate to a special non animated version of the symbol in the correct state.
	[SerializeField] private string MUTATED_BN_SYMBOL_NAME = "";

	protected const string BONUS_PORTAL_BG_MUSIC_KEY = "bonus_portal_bg";

	protected override IEnumerator doTransition ()
	{
		if (Audio.canSoundBeMapped(BONUS_PORTAL_BG_MUSIC_KEY))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));
		}

		if (MUTATED_BN_SYMBOL_NAME != "")
		{
			foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
			{
				SlotSymbol[] symbolList = reel.visibleSymbols;
				foreach (SlotSymbol symbol in symbolList)
				{	
					if (symbol.isBonusSymbol && !symbol.isFlattenedSymbol)
					{
						symbol.mutateTo(MUTATED_BN_SYMBOL_NAME);
					}
				}
			}
		}
		return base.doTransition();
	}
}
