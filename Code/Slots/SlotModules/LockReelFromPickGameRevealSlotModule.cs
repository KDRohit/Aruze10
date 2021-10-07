using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/*
 * This module locks a reel for a matching group_id found in a pick game.
 * optionally it will also animate the symbols on that reel.
 *
 * game : gen84 freespin
 *
 * Author : Nick Saito <nsaito@zynga.com>
 * Date : Apr 2, 2019
 */
public class LockReelFromPickGameRevealSlotModule : SlotModule
{
	[Tooltip("The group id in the reveal pick")]
	[SerializeField] private string lockGroupId;

	[Tooltip("The reel id the was should lock (0 based)")]
	[SerializeField] private int lockReelId;

	[Tooltip("Animate all the symbols on this reel in a loop")]
	[SerializeField] private bool animateLockedReelSymbols;

	private PickemPick pickemPick;
	private bool pauseAnimatingLockedSymbols;
	private List<SlotSymbol> animatedSymbols = new List<SlotSymbol>();

	// get the pick game results so we can check which pick the player got.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		SimplePickemOutcome pickemOutcome = new SimplePickemOutcome(BonusGameManager.currentBonusGameOutcome);
		pickemPick = pickemOutcome.getNextEntry();

		if (pickemPick.groupId == lockGroupId)
		{
			return true;
		}

		return false;
	}

	// lock the reel
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		SlotReel slotReel = reelGame.engine.getSlotReelAt(lockReelId);
		slotReel.isLocked = true;
		pauseAnimatingLockedSymbols = false;
	}

	// This is where we decide to start animating the symbols on the locked reel.
	// We only need to do this once, but this is the right spot to do it for timing.
	public override bool needsToExecuteOnReelsSpinning()
	{
		return animateLockedReelSymbols && pickemPick.groupId == lockGroupId;
	}

	// Animating the symbols on the locked reel.
	public override IEnumerator executeOnReelsSpinning()
	{
		SlotReel slotReel = reelGame.engine.getSlotReelAt(lockReelId);

		if (animatedSymbols.Count > 0)
		{
			pauseAnimatingLockedSymbols = false;
			playLockedSymbolAnimations();
			yield break;
		}

		foreach (SlotSymbol slotSymbol in slotReel.visibleSymbols)
		{
			SlotSymbol animatorSymbol = slotSymbol.getAnimatorSymbol();

			if (slotSymbol.name == animatorSymbol.name)
			{
				slotSymbol.mutateToUnflattenedVersion();
				StartCoroutine(animateBonusSymbol(slotSymbol));
				animatedSymbols.Add(slotSymbol);
			}
		}
	}

	private void playLockedSymbolAnimations()
	{
		foreach (SlotSymbol slotSymbol in animatedSymbols)
		{
			StartCoroutine(animateLockedSymbolOutcome(slotSymbol));
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return animateLockedReelSymbols && isLineWin();
	}

	private bool isLineWin()
	{
		SlotOutcome slotOutcome = reelGame.getCurrentOutcome();
		ReadOnlyCollection<SlotOutcome> subOutcomes = slotOutcome.getSubOutcomesReadOnly();

		bool isLineWin = false;

		if (subOutcomes != null)
		{
			foreach (SlotOutcome subSlotOutcome in subOutcomes)
			{
				if (subSlotOutcome.getWinId() > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		pauseAnimatingLockedSymbols = true;
		while (areLockedSymbolsAnimating())
		{
			yield return null;
		}
	}

	private IEnumerator animateBonusSymbol(SlotSymbol slotSymbol)
	{
		yield return StartCoroutine(slotSymbol.playAndWaitForAnimateAnticipation());
		yield return StartCoroutine(animateLockedSymbolOutcome(slotSymbol));
	}

	private IEnumerator animateLockedSymbolOutcome(SlotSymbol symbol)
	{
		while (!pauseAnimatingLockedSymbols)
		{
			if (symbol.isAnimating)
			{
				// animation may still be playing from the payline animations, so just
				// make sure we wait our turn
				yield return null;
			}
			else
			{
				yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
			}
		}
	}

	// Call back for the symbol animations that keeps it looping
	private void animateLockedSymbolOutcome2(SlotSymbol slotSymbol)
	{
		if (!pauseAnimatingLockedSymbols)
		{
			StartCoroutine(slotSymbol.playAndWaitForAnimateOutcome(animateLockedSymbolOutcome2));
		}
	}

	private bool areLockedSymbolsAnimating()
	{
		foreach (SlotSymbol slotSymbol in animatedSymbols)
		{
			if (slotSymbol.isAnimatorDoingSomething)
			{
				return true;
			}
		}

		return false;
	}
}