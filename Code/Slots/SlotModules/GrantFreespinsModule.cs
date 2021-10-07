using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This module can be used on FreeSpin games which grant freespins on certain outcomes.
//	By default it uses the scatter win outcome.
[HelpURL("https://wiki.corp.zynga.com/pages/viewpage.action?pageId=41290898")]
public class GrantFreespinsModule : SlotModule
{
	[SerializeField] protected string RETRIGGER_BANNER_SOUND = "retrigger_banner";
	[Tooltip("Use this to force the bonus symbols to animate")]
	[SerializeField] protected bool isAnimatingBonusSymbols = false;
	[Tooltip("If isAnimatingBonusSymbols is true, this will also play the bonus symbol animate sound when it plays the symbol animations")]
	[SerializeField] protected bool playBonusSymbolSoundDuringBonusSymbolAnimation = false;
	[SerializeField] protected string BONUS_SYMBOL_SOUND = "bonus_symbol_animate";
	[SerializeField] protected bool isPlayingRollupSoundsForBonusCredits = true;

	protected int numberOfFreeSpins;
	protected const string SPINS_ADDED_INCREMENT_SOUND_KEY = "freespin_spins_added_increment";
	protected int numberOfBonusSymbolsAnimating = 0; // tracks how many bonus symbols are animating, to have the game wait until the symbols finish animating before proceeding with animations

	protected long bonusCreditAmount = -1;

	public override void Awake()
	{
		reelGame = GetComponent<ReelGame>();
		// We don't want the default behavior (destroy this module) if we're saving this for the free spins game
		// Or, if reelGame is null, base class will handle destruction and error notification
		if (reelGame == null || !(freeSpinGameRequired && reelGame.playFreespinsInBasegame))
		{
			base.Awake();
		}
	}

	protected virtual void incrementFreespinCount()
	{
		if (!string.IsNullOrEmpty(RETRIGGER_BANNER_SOUND))
		{
			Audio.tryToPlaySoundMap(RETRIGGER_BANNER_SOUND);
		}
		
		Audio.play(Audio.soundMap(SPINS_ADDED_INCREMENT_SOUND_KEY));

		reelGame.numberOfFreespinsRemaining += numberOfFreeSpins;
	}
	
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		numberOfFreeSpins = 0;

		// Don't execute if we're activating on freespin and the freespin game hasn't started
		if (freeSpinGameRequired && !reelGame.hasFreespinGameStarted)
		{
			return false;
		}
		
		List<SlotOutcome> subOutcomes = reelGame.outcome.getSubOutcomesCopy();
		// Add in multi game outcomes if they exist as well, since they might pay out a scatter
		// that awards spins as well
		List<SlotOutcome> layeredOutcomes = reelGame.outcome.getReevaluationSubOutcomesByLayer();
		subOutcomes.AddRange(layeredOutcomes);

		foreach (SlotOutcome subOutcome in subOutcomes)
		{
			if (subOutcome.getOutcomeType() == SlotOutcome.OutcomeTypeEnum.SCATTER_WIN)
			{
				PayTable.ScatterWin scatterWin = BonusGameManager.instance.currentBonusPaytable.scatterWins[subOutcome.getWinId()];
				if (scatterWin.freeSpins != 0 && (FreeSpinGame.instance != null || SlotBaseGame.instance.hasFreespinGameStarted))
				{
					numberOfFreeSpins = scatterWin.freeSpins;
					return true;
				}
			}
		}

		// for for freespins awarded via a reevaluation
		bool foundReevaluationFreespinsAward = false;
		JSON[] reevals = reelGame.outcome.getArrayReevaluations();
		for (int i = 0; i < reevals.Length; i++)
		{
			int freespinsAwarded = reevals[i].getInt("free_spins", 0);
			if (freespinsAwarded != 0)
			{
				foundReevaluationFreespinsAward = true;
				numberOfFreeSpins += freespinsAwarded;

				bonusCreditAmount = reevals[i].getLong("bonus_credits", -1);
			}
		}

		if (foundReevaluationFreespinsAward)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// used as a callback to animateBonusSymbols to track if the bonus symbols are still animating
	protected void onBonusSymbolAnimDone(SlotSymbol sender)
	{
		numberOfBonusSymbolsAnimating--;
	}

	protected virtual IEnumerator playAndWaitOnBonusSymbolAnimations()
	{
		numberOfBonusSymbolsAnimating = 0;

		// animate bonus symbols on reels for freespin awards
		SlotReel[] allReels = this.reelGame.engine.getAllSlotReels();
		for (int i = 0; i < allReels.Length; i++)
		{
			SlotSymbol[] visibleSymbols = this.reelGame.engine.getVisibleSymbolsAt(allReels[i].reelID - 1);
			for (int k = 0; k < visibleSymbols.Length; k++)
			{
				SlotSymbol symbol = visibleSymbols[k];
				if (symbol.isBonusSymbol)
				{
					symbol.animateOutcome(onBonusSymbolAnimDone);
					numberOfBonusSymbolsAnimating++;
				}
			}
		}

		if (numberOfBonusSymbolsAnimating > 0 && !string.IsNullOrEmpty(BONUS_SYMBOL_SOUND))
		{
			Audio.playSoundMapOrSoundKey(BONUS_SYMBOL_SOUND);
		}

		while (numberOfBonusSymbolsAnimating > 0)
		{
			yield return null;
		}
	}
	
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// First animate the bonus symbols on the reel, if we are forcing them to
		if (isAnimatingBonusSymbols)
		{
			yield return StartCoroutine(playAndWaitOnBonusSymbolAnimations());
		}

		incrementFreespinCount();

		if (bonusCreditAmount > 0)
		{
			// bonus credits were also won as part of this freespin award, so we need to roll those up
			bonusCreditAmount *= reelGame.multiplier;
			yield return StartCoroutine(SlotUtils.rollup(0, bonusCreditAmount, reelGame.onPayoutRollup, isPlayingRollupSoundsForBonusCredits));
			// trigger the end rollup to move the winnings into the runningPayoutRollupValue
			yield return StartCoroutine(reelGame.onEndRollup(isAllowingContinueWhenReady: false));
			bonusCreditAmount = -1;
		}
	}
}
