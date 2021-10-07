using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module for revealing the upgrades for the cliffhanger bonus.  This reveal increases all the multipliers and
 * gives another pick (since only picks that move the character along the meter decrease the number of picks).
 *
 * Creation Date: 1/27/2021
 * Original Author: Scott Lepthien
 */
public class CliffhangerRevealMultiplierUpgradeModule : CliffhangerGameModule
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealMeterUpgrade";
	[SerializeField] protected string REVEAL_LEFTOVER_ANIMATION_NAME = "revealUnpickedMeterUpgrade";
	[Tooltip("Animations played right before the Picks Remaining label is updated.  You can use a custom blocking length to sync the label update with where you want it to occur during these animations.")]
	[SerializeField] private AnimationListController.AnimationInformationList picksRemainingIncreasedAnims;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// Check if the reveal data has a the meterAction set to "upgrade"
		return pickData != null && pickData.meterAction == "upgrade";
	}
	
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		
		if (currentPick == null || pickItem == null)
		{
			yield break;
		}
		
		// Need to set the reveal anim name since well be using the same object to reveal two different things
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		
		yield return StartCoroutine(pickingVariantParent.gameParent.increasePicks(currentPick.additonalPicks));
		if (picksRemainingIncreasedAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(picksRemainingIncreasedAnims));
		}
		pickingVariantParent.updatePicksRemainingLabel();
		
		yield return StartCoroutine(cliffhangerVariantParent.upgradeSectionMultipliers(1));
		
		// Play post reveal anims (like hiding the picking object)
		if (!pickItem.isPlayingPostRevalAnimsImmediatelyAfterReveal && pickItem.hasPostRevealAnims())
		{
			yield return StartCoroutine(pickItem.playPostRevealAnims());
		}
	}
	
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		if (leftoverOutcome == null || leftover == null)
		{
			yield break;
		}
		
		// Need to set the reveal anim name since well be using the same object to reveal two different things
		leftover.REVEAL_ANIMATION_GRAY = REVEAL_LEFTOVER_ANIMATION_NAME;

		yield return StartCoroutine(leftover.revealLeftover(leftoverOutcome));
	}
	
	// executes on an item reveal, after the pick has been advanced and the picks remaining label has been updated
	// but before the isRoundOver() check is called.  Basically allows you to block a pick being considered handled
	// and the round ending or input being unlocked, until you've handled whatever you want to handle in the module
	// that implements this.
	public override bool needsToExecuteOnItemRevealedPreIsRoundOverCheck()
	{
		// Only want to handle this if we didn't already gameover, if we did then the presentation during the gameover
		// will have already handled sending the server message and we should be terminating the game via that flow as well.
		// Also need to verify that this module is the one who handled the previous pick (so we don't execute this more than once).
		if (!cliffhangerVariantParent.isCliffhangerEndedInGameover && shouldHandleOutcomeEntry(cliffhangerVariantParent.getPreviousPickOutcome()))
		{
			// We've verified that this is supposed to run for this specific module. Now
			// we need to determine if we should show the choice or just enable input again
			// so the player can keep picking (if they haven't moved yet)
			if (cliffhangerVariantParent.hasCharacterEverMoved())
			{
				return true;
			}
			else
			{
				cliffhangerVariantParent.skipPlayerChoice();
				return false;
			}
		}

		return false;
	}

	public override IEnumerator executeOnItemRevealedPreIsRoundOverCheck()
	{
		yield return StartCoroutine(cliffhangerVariantParent.handlePlayerChoiceAfterReveal());
	}
}
