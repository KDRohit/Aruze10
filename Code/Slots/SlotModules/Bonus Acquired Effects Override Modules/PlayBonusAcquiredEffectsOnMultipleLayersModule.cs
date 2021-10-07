using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayBonusAcquiredEffectsOnMultipleLayersModule : SlotModule
{
	private const string BN_ANIM_SOUND_KEY = "bonus_symbol_animate";
	[SerializeField] private float bonusAnimationLength = 0.0f;
	[SerializeField] private List<int> layersToPlayBonusEffects = new List<int>();
	[SerializeField] private List<int> bonusReels = new List<int>();

	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		Audio.play(Audio.soundMap(BN_ANIM_SOUND_KEY));
		for (int i = 0; i < layersToPlayBonusEffects.Count; i++)
		{
			for (int j = 0; j < bonusReels.Count; j++)
			{
				foreach(SlotSymbol currentVisibleSymbol in reelGame.engine.getSlotReelAt(bonusReels[j], -1, layersToPlayBonusEffects[i]).visibleSymbols) 
				{
					if(currentVisibleSymbol.isBonusSymbol)
					{
						currentVisibleSymbol.animateOutcome();
					}
				}
			}
		}
		yield return new TIWaitForSeconds(bonusAnimationLength);
	}

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride ()
	{
		return true;
	}
}
