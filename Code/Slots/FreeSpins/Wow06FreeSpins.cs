using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Wow06FreeSpins : FreeSpinGame {

	public GameObject firePrefab;
	public Animator leftDragonAnimator;
	public Animator rightDragonAnimator;

	private const string TW_SOUND = "TWDragonInit";
	
	public override void initFreespins()
	{
		base.initFreespins();
		mutationManager.isLingering = false;
	}

	protected override void reelsStoppedCallback()
	{
		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject());
		this.StartCoroutine(Wow06.doAnimationWilds(this, base.reelsStoppedCallback, firePrefab, leftDragonAnimator, rightDragonAnimator));
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		List<SlotSymbol> symbols = stoppedReel.visibleSymbolsBottomUp;

		for (int j = 0; j < symbols.Count; j++)
		{
			if (symbols[j].animator != null && symbols[j].animator.symbolInfoName == "TW")
			{
				Audio.play(TW_SOUND);
				symbols[j].animateOutcome();
			}
		}

		yield break;
	}
}
