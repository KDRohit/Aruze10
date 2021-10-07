using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing "bad" picks during a picking round
 */
public class PickingGameBadPickWithJackpotLadderModule : PickingGameBadPickModule
{
	[SerializeField] private JackpotLadderAnimationData jackpotRanks;

    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        //  for jackpot games in this style, bad items with credits and bad items with multipliers don't count
        return pickData != null && pickData.credits == 0 && pickData.multiplier == 0 && (pickData.isGameOver || pickData.isBad);
    }

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotRanks.getCurrentRankLabel().badEndRevealAnimation));
	}
}
