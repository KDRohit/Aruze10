using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module that plays animations during the payline display and hide. Used for things like playing a shroud animation over
 * symbols while paylines display. Also plays a prespin animation for clearing the state set by the payline animation
 * (eg removing the shroud).  NOTE: Default for firstPaylineOnly is true, so disable that if you want animations to trigger
 * for each payline shown.
 *
 * Created: 12/18/2019
 * Author: Caroline Cevallos
 */
public class AnimateOnPaylineDisplayModule : SlotModule
{
	[Tooltip("Animations to play when a payline is shown. If firstPaylineOnly is true this will only happen once per spin, otherwise it will happen each time a payline is shown.")]
	[SerializeField] private AnimationListController.AnimationInformationList beforePaylines;
	[Tooltip("Animations to play when a payline is hidden.  If firstPaylineOnly is true this will only trigger once per spin, otherwise it will happen each time a payline is hidden")]
	[SerializeField] private AnimationListController.AnimationInformationList onPaylineHideAnims;
	[Tooltip("Animations to play when next spin starts, useful for clearing state set by payline animations")]
	[SerializeField] private AnimationListController.AnimationInformationList onSpin;

	[Tooltip("If true, only plays animations once, otherwise plays on every outcome displayed")]
	[SerializeField] private bool firstPaylineOnly = true;

	private bool hasPlayedPaylineAnimationOnce = false; // Tracking to determine if the payline show animations have been played
	private bool hasHiddenPaylineAnimationOnce = false; // Tracking for only playing anims once if firstPaylineOnly is true
	private bool hasPrespinStarted; // hacky way to check that the next spin hasn't started yet

	public override bool needsToExecuteOnPaylineDisplay()
	{
		// check if we actually have animated payboxes showing and skip otherwise
		bool hasPayboxes = reelGame.outcomeDisplayController.hasDisplayedOutcomes();
		return ((!hasPlayedPaylineAnimationOnce || !firstPaylineOnly) && hasPayboxes && !hasPrespinStarted);
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(beforePaylines));
		hasPlayedPaylineAnimationOnce = true;
	}
	
	// executeOnPaylineHide() section
	// function in this section are accesed by ReelGame.onPaylineHidden()
	public override bool needsToExecuteOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		bool shouldHide = !firstPaylineOnly || !hasHiddenPaylineAnimationOnce;
		return hasPlayedPaylineAnimationOnce && !hasPrespinStarted && shouldHide && onPaylineHideAnims.Count > 0;
	}

	public override IEnumerator executeOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onPaylineHideAnims));
		hasHiddenPaylineAnimationOnce = true;
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return hasPlayedPaylineAnimationOnce;
	}

	public override IEnumerator executeOnPreSpin()
	{
		hasPrespinStarted = true;
		if (hasPlayedPaylineAnimationOnce)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onSpin));
		}
	}
	
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return hasPlayedPaylineAnimationOnce;
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		if (hasPlayedPaylineAnimationOnce)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onSpin));
		}
	}
	
	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return hasPlayedPaylineAnimationOnce;
	}

	public override void executeOnShowSlotBaseGame()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(beforePaylines));
	}
	
	// executePreReelsStopSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		return hasPlayedPaylineAnimationOnce;
	}
	
	public override IEnumerator executePreReelsStopSpinning()
	{
		// Reset this here instead of in prespin because there is a chance that another payline may trigger after Prespin is called
		// before the outcome display controller can be cleared.  If we reset this flag once the next spin is occuring then we know
		// that another payline will not trigger and the beforePaylines animations will not accidently trigger as a new spin starts.
		hasPlayedPaylineAnimationOnce = false;
		hasHiddenPaylineAnimationOnce = false;
		hasPrespinStarted = false;
		yield break;
	}
}
