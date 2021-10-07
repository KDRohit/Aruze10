using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing credits during a picking round
 */
public class PickingGameCreditsModule : PickingGameRevealModule 
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealCredit";
    [SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
    [SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealCreditGray";
    [SerializeField] protected string REVEAL_AUDIO = "pickem_credits_pick";
    [SerializeField] protected string REVEAL_VO_AUDIO = "pickem_credits_vo_pick";
    [SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip
    [SerializeField] protected float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
    [SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] protected bool USE_BASE_CREDIT_AMOUNT = false;
	[SerializeField] protected bool useCurrentMultiplierInCreditValues;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		// note: jackpot pickitems also have credits assigned, thus the groupId check
		if (
			(pickData != null) &&
			(pickData.credits > 0) &&
			pickData.pickemGroupId == "" &&
			!pickData.canAdvance &&
			(pickData.additonalPicks == 0) &&
			(pickData.extraRound == 0) &&
			(
				(!pickData.isGameOver) ||
				(
					pickData.isGameOver &&
					(
						roundVariantParent.roundIndex == roundVariantParent.gameParent.pickingRounds.Count-1 ||
						pickingVariantParent.gameParent.getDisplayedPicksRemaining() >= 0
					)
				)
			)
		)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		handleCreditItemPicked(currentPick, pickItem);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// rollup with extra animations included
		if (!USE_BASE_CREDIT_AMOUNT)
		{
			yield return StartCoroutine(base.rollupCredits(currentPick.credits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1)));
		}
		else
		{
			yield return StartCoroutine(base.rollupCredits(currentPick.baseCredits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1)));
		}
	}

	protected void handleCreditItemPicked(ModularChallengeGameOutcomeEntry currentPick, PickingGameBasePickItem pickItem)
	{
		// play the associated reveal sound
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}
			
		//set the credit value within the item and the reveal animation
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);

		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		// adjust with bonus multiplier if necessary
		if (!USE_BASE_CREDIT_AMOUNT)
		{
			creditsRevealItem.setCreditLabels(currentPick.credits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1));
		}
		else
		{
			creditsRevealItem.setCreditLabels(currentPick.baseCredits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1));
		}
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				// adjust with bonus multiplier if necessary
				if (!USE_BASE_CREDIT_AMOUNT)
				{
					creditsLeftOver.setCreditLabels(leftoverOutcome.credits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1));
				}
				else
				{
					creditsLeftOver.setCreditLabels(leftoverOutcome.baseCredits * (useCurrentMultiplierInCreditValues ? BonusGameManager.instance.currentMultiplier : 1));
				}
				creditsLeftOver.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
			}
			else
			{
				Debug.LogError("PickingGameCreditsModule.executeOnRevealLeftover() - leftover item didn't have an attached PickingGameCreditPickItem!");
			}
		}
			
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}
}
