using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module activates when a freespin bonus game is won. We override executePlayBonusAcquiredEffectsOverride
// and collect the values in our symbolCreditAwardList. This amount is then awarded to the player as a bonus
// before continuing on to the freespin game. The total of the awarded symbols becomes the values of the symbols
// in the freespin game.
//
// Date : Jan 10th, 2020
// Author : Nick Saito <nsaito@zynga.com>
//
// games : billions02
//
public class ScatterCollectBonusGameTriggerModule : ScatterSymbolBaseModule
{
	[SerializeField] private LabelWrapperComponent scatterRollupLabel;

	[SerializeField] private float afterRollupDelay;

	[Tooltip("Animations to play before collecting the symbols")]
	[SerializeField] private AnimationListController.AnimationInformationList preSymbolCollectAnimations;

	[Tooltip("Provide specific animations for each symbol position for the collect symbol effect")]
	[SerializeField] private List<SymbolCollectAnimationData> symbolCollectAnimations;

	[Tooltip("AnimatedParticleEffect to play after each symbolCollectAnimation.")]
	[SerializeField] private AnimatedParticleEffect burstAnimatedParticleEffect;

	// cache this transform so we can send sparkle trails to it
	private Transform scatterRollupLabelTransform;

	// a list of symbols values that triggered the freespins so we can award them in order
	private List<long> symbolCreditAwardList = new List<long>();

	// The total value of the symbols that triggered the freespins
	// which we to award the player from the symbols before triggering freespins
	private long totalSymbolValue;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		scatterRollupLabelTransform = scatterRollupLabel.transform;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (scatterRollupLabel != null)
		{
			scatterRollupLabel.text = CommonText.formatNumber(0);
		}

		totalSymbolValue = 0;
		yield break;
	}

	// Collect trigger symbols if we triggered a freespin game
	public override bool needsToExecutePlayBonusAcquiredEffectsOverride()
	{
		if (reelGame.getCurrentOutcome().hasFreespinsBonus())
		{
			return true;
		}

		return false;
	}

	// Collect the trigger symbols, fire off trails, and roll the up to the scatter rollup label
	public override IEnumerator executePlayBonusAcquiredEffectsOverride()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preSymbolCollectAnimations));

		// animate trails and gather a list of symbol credits to award as the trails land
		foreach (SlotSymbol slotSymbol in reelGame.engine.getAllVisibleSymbols())
		{
			if (symbolCreditMap.ContainsKey(slotSymbol.serverName))
			{
				symbolCreditAwardList.Add(symbolCreditMap[slotSymbol.serverName]);

				if (symbolCollectAnimations != null)
				{
					AnimationListController.AnimationInformationList animationInformationList = getSymbolCollectAnimationData(slotSymbol.reel.reelID, slotSymbol.visibleSymbolIndex);
					if (animationInformationList != null)
					{
						yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInformationList));
					}

					// In billions02 we are sharing a burst effect for all the custom animations. So they don't overlap and not play,
					// we use an animated particle effect to instantiate the animation as needed.
					if (burstAnimatedParticleEffect != null)
					{
						yield return StartCoroutine(burstAnimatedParticleEffect.animateParticleEffect());
					}

					awardSymbolEvent();
				}
				else
				{
					// just collect the scatter values since there is no trail effect
					awardSymbolEvent();
				}
			}
		}

		// rollup winnings from trigger symbols
		yield return StartCoroutine(SlotUtils.rollup(0, totalSymbolValue * reelGame.multiplier, onRollupPayoutToWinningsOnly));
		SlotBaseGame.instance.isBonusOutcomePlayed = true;

		// pause before continuing to the bonus game to let the rollup
		if (afterRollupDelay > 0.0f)
		{
			yield return new WaitForSeconds(afterRollupDelay);
		}
	}

	private AnimationListController.AnimationInformationList getSymbolCollectAnimationData(int reelId, int symbolPosition)
	{
		foreach(SymbolCollectAnimationData animationData in symbolCollectAnimations)
		{
			if (animationData.reelId == reelId && animationData.symbolPosition == symbolPosition)
			{
				return animationData.animationInformationList;
			}
		}

		return null;
	}

	// We need to override this because BonusGamePresenter checks
	// the basegame to get this value.
	public override bool needsToGetCarryoverWinnings()
	{
		return true;
	}

	public override long executeGetCarryoverWinnings()
	{
		return totalSymbolValue * reelGame.multiplier;;
	}

	// Sets the value in the winbox as the rollup happens.
	private void onRollupPayoutToWinningsOnly(long payoutValue)
	{
		reelGame.setWinningsDisplay(payoutValue);
	}

	private void awardSymbolEvent()
	{
		totalSymbolValue += symbolCreditAwardList[0];
		symbolCreditAwardList.RemoveAt(0);
		scatterRollupLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(totalSymbolValue * reelGame.multiplier, shouldRoundUp: false);
	}

	[System.Serializable]
	public class SymbolCollectAnimationData
	{
		[Tooltip("Reel id starting from 1")]
		public int reelId;

		[Tooltip("Symbol position id starting from 0 going from top to bottom")]
		public int symbolPosition;

		public AnimationListController.AnimationInformationList animationInformationList;
	}
}

