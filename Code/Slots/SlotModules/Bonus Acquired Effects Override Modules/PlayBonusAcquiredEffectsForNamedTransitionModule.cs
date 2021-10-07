using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayBonusAcquiredEffectsForNamedTransitionModule : PlayBonusAcquiredEffectsBaseModule
{
	[SerializeField] private List<string> namedTransitions;
	[SerializeField] private AnimationListController.AnimationInformationList bonusAquiredAnimations;
	[SerializeField] private bool isSkippingPlayBaseEffects;

	// Play bonus effects and any extra bonus celebration animations here.
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();

		if (!isSkippingPlayBaseEffects)
		{
			coroutineList.Add(StartCoroutine(base.executePlayBonusAcquiredEffectsOverride()));
		}

		coroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(bonusAquiredAnimations)));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		SlotBaseGame.instance.isBonusOutcomePlayed = true;
	}

	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		if (BonusGameManager.instance.outcomes == null)
		{
			return false;
		}

		if (reelGame.outcome.hasQueuedBonuses)
		{
			if (namedTransitions.Contains(reelGame.outcome.peekAtNextQueuedBonusGame().getBonusGame()))
			{
				return true;
			}

			return false;
		}

		foreach (KeyValuePair<BonusGameType, BaseBonusGameOutcome> bonusGameOutcomes in BonusGameManager.instance.outcomes)
		{
			if(namedTransitions.Contains(bonusGameOutcomes.Value.bonusGameName))
			{
				return true;
			}
		}

		return false;
	}
}
