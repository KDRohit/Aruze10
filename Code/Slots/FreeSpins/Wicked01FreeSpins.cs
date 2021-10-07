using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Copied from gen21.
 */
 
public class Wicked01FreeSpins : TumbleFreeSpinGame 
{
	[SerializeField] private float INTRO_ANIM_DUR = 8.5f;
	private const string VERTICAL_WILD_2X_REVEAL_SOUND_KEY = "freespin_vertical_wild2x_reveal";
	
	private List<SlotSymbol> wild2xSymbolList = new List<SlotSymbol>();
	
	public override void initFreespins()
	{
		Audio.switchMusicKeyImmediate("");
 		Audio.play(Audio.soundMap("bonus_freespin_wipe_transition"));
		base.initFreespins();
	}
		
	protected override IEnumerator playGameStartModules()
	{
		yield return new WaitForSeconds(INTRO_ANIM_DUR);
		yield return StartCoroutine(base.playGameStartModules());
	}
	
	protected override void beginFreeSpinMusic()
 	{ 	
 		Audio.switchMusicKey(Audio.soundMap("freespin"));
		Audio.play(Audio.soundMap("freespin_intro_vo"));
		SpinPanel.instance.restoreAlpha();
 	}

	public override IEnumerator showUpdatedSpins(int numberOfSpins)
	{
		int prevNumberOfSpins = numberOfFreespinsRemaining;

		yield return StartCoroutine(base.showUpdatedSpins(numberOfSpins));

		// base.showUpdatedSpins does not update autoSpins becauase free spin animation does not have a AnimatorFreespinEffect
		// or a FreeSpinEffect component. So do it ourselves and check against prevNumberOfSpins in case one of these components ever gets added
		// and base.showUpdatedSpins(numberOfSpins  actually adds the spins
		// grant free spin modules do not work with tumble games either
		if (numberOfFreespinsRemaining == prevNumberOfSpins) 
		{
			numberOfFreespinsRemaining += numberOfSpins;
		}

		yield return null;
	}
	
	protected override IEnumerator displayWinningSymbols()
	{
		wild2xSymbolList.Clear();		
		yield return StartCoroutine(base.displayWinningSymbols());
	}
	
	public override bool needsToPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		if (symbol.name.Contains("W2") && !wild2xSymbolList.Contains(symbol))
		{
			return true;
		}
		
		return false;
	}

	public override void playPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		Audio.play(Audio.soundMap(VERTICAL_WILD_2X_REVEAL_SOUND_KEY));
		wild2xSymbolList.Add(symbol);
	}
}
