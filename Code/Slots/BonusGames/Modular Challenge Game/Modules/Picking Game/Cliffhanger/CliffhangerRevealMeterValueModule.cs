using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module for revealing the meter values for the cliffhanger bonus.  This reveal will not actually reveal the final
 * value at first, instead revealing 1 and then counting up to the actual value that the character will move.  The
 * character will move as the value increments.
 *
 * Creation Date: 1/27/2021
 * Original Author: Scott Lepthien
 */
public class CliffhangerRevealMeterValueModule : CliffhangerGameModule
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealMeterValue";
	[Tooltip("Set this to override how long the REVEAL_ANIMATION_NAME animation blocks for.  Useful for this game type if you want the meter counting to start quicker.")]
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = -1.0f;
	[SerializeField] protected string REVEAL_LEFTOVER_ANIMATION_NAME = "revealUnpickedMeterValue";

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// Check if the reveal data has a meterValue change in it
		return pickData != null && pickData.meterValue > 0;
	}
	
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		
		if (currentPick == null || pickItem == null)
		{
			yield break;
		}
		
		// Need to set the reveal anim name since well be using the same object to reveal two different things
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);

		// Set the label to 1 (and we will increment it up until we reach the actual full reveal amount)
		PickingGameGenericLabelPickItem genericLabelPick = pickItem.gameObject.GetComponent<PickingGameGenericLabelPickItem>();
		genericLabelPick.setGenericLabel(CommonText.formatNumber(1));
		
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		
		// We need to go up by the total meter value incrementing it one at a time
		for (int i = 0; i < currentPick.meterValue; i++)
		{
			// Update the label (skipping the first time, since the one was shown during the initial reveal)
			if (i > 0)
			{
				genericLabelPick.setGenericLabel(CommonText.formatNumber(i + 1));
			}
			
			yield return StartCoroutine(cliffhangerVariantParent.addToMeterValue(1));

			// Need to check if the game is going to end due to game over after incrementing the meter (so we terminate this coroutine correctly and stop counting up)
			if (cliffhangerVariantParent.isCliffhangerEndedInGameover)
			{
				yield break;
			}
		}
		
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

		// Unlike with the reveal (which counts up) we are just going to set this to the final value because we just want to display
		// what the player could have gotten if they picked this object
		PickingGameGenericLabelPickItem genericLabelPick = leftover.gameObject.GetComponent<PickingGameGenericLabelPickItem>();
		genericLabelPick.setGenericLabel(CommonText.formatNumber(leftoverOutcome.meterValue));

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
		return !cliffhangerVariantParent.isCliffhangerEndedInGameover && shouldHandleOutcomeEntry(cliffhangerVariantParent.getPreviousPickOutcome());
	}

	public override IEnumerator executeOnItemRevealedPreIsRoundOverCheck()
	{
		yield return StartCoroutine(cliffhangerVariantParent.handlePlayerChoiceAfterReveal());
	}
}
