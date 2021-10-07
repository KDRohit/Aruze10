using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Class for displaying outcomes for outcome type SlotOutcome.OUTCOME_TYPE_SCATTER_WIN.
public class ScatterOutcomeDisplayModule : OutcomeDisplayBaseModule
{
	// Returns a list of all of the winning symbols for a specific reel for the passed outcome
	// useful if you want to get them as each reel stops (since getSetOfWinningSymbols() will only work correctly if all reels are fully stopped)
	public override HashSet<SlotSymbol> getSetOfWinningSymbolsForReel(SlotOutcome outcome, int reelIndex, int row, int layer)
	{
		if (_controller.payTable == null)
		{
			return new HashSet<SlotSymbol>();
		}

		if (!_controller.payTable.scatterWins.ContainsKey(outcome.getWinId()))
		{
			return new HashSet<SlotSymbol>();
		}

		PayTable.ScatterWin scatterWin = _controller.payTable.scatterWins[outcome.getWinId()];

		SlotReel reel = _controller.slotEngine.getSlotReelAt(reelIndex, row, layer);

		if (reel == null)
		{
			Debug.LogError("ScatterOutcomeDisplayModule.getSetOfWinningSymbolsForReel() - Unable to get reel at: reelIndex = " + reelIndex + "; row = " + row + "; layer = " + layer);
			return new HashSet<SlotSymbol>();
		}

		HashSet<SlotSymbol> winningSymbols = new HashSet<SlotSymbol>();
		foreach (SlotReel currentReel in _controller.slotEngine.getReelArray())
		{
			if (reel == currentReel)
			{
				foreach (SlotSymbol symbol in reel.visibleSymbols)
				{
					// Check to see if this symbol matches any of the symbols in the scatter win symbol list.
					foreach (string winSymbol in scatterWin.symbols)
					{
						//Need to check server name to support flattened symbols after coming back from a bonus game in tumble games
						// since the basegame symbols turn back to flattened after the bonus game ends
						if (winSymbol == symbol.serverName)
						{
							SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();

							if (animatorSymbol != null && !winningSymbols.Contains(animatorSymbol))
							{
								winningSymbols.Add(animatorSymbol);
							}
						}
					}
				}
			}
		}

		return winningSymbols;
	}

	// Returns a list of all symbols that are part of wins
	public override HashSet<SlotSymbol> getSetOfWinningSymbols(SlotOutcome outcome)
	{
		if (_controller.payTable == null)
		{
			return new HashSet<SlotSymbol>();
		}

		if (!_controller.payTable.scatterWins.ContainsKey(outcome.getWinId()))
		{
			return new HashSet<SlotSymbol>();
		}

		PayTable.ScatterWin scatterWin = _controller.payTable.scatterWins[outcome.getWinId()];

		HashSet<SlotSymbol> winningSymbols = new HashSet<SlotSymbol>();
		foreach (SlotReel reel in _controller.slotEngine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				// Check to see if this symbol matches any of the symbols in the scatter win symbol list.
				foreach (string winSymbol in scatterWin.symbols)
				{
					//Need to check server name to support flattened symbols after coming back from a bonus game in tumble games
					// since the basegame symbols turn back to flattened after the bonus game ends
					if (winSymbol == symbol.serverName)
					{
						SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();
						
						if (animatorSymbol != null && !winningSymbols.Contains(animatorSymbol))
						{
							winningSymbols.Add(animatorSymbol);
						}
					}
				}
			}
		}

		return winningSymbols;
	}

	public override void playOutcome(SlotOutcome outcome, bool isPlayingSound)
	{
		base.playOutcome(outcome, isPlayingSound);

		if (_controller.payTable == null)
		{
			return;
		}

		if (!_controller.payTable.scatterWins.ContainsKey(_outcome.getWinId()))
		{
			return;
		}

		PayTable.ScatterWin scatterWin = _controller.payTable.scatterWins[_outcome.getWinId()];
		if (scatterWin.freeSpins != 0 && FreeSpinGame.instance != null && !_outcome.hasAwardedAdditionalSpins)
		{
			_outcome.hasAwardedAdditionalSpins = true;
			StartCoroutine(FreeSpinGame.instance.showUpdatedSpins(scatterWin.freeSpins));
		}

		animPlayingCounter = 0;

		HashSet<Vector2> animatedSymbols = new HashSet<Vector2>();
		foreach (SlotReel reel in _controller.slotEngine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				// Check to see if this symbol matches any of the symbols in the scatter win symbol list.
				foreach (string winSymbol in scatterWin.symbols)
				{
					if (winSymbol == symbol.serverName)
					{
						// double check for mega symbols which may already be animating due to another part already triggering it
						bool isSymbolAlreadyAnimated = animatedSymbols.Contains(symbol.getSymbolPositionId());
						
						// For Scatter outcomes only, we check isAnimatorDoingSomething instead of isAnimatorMutating
						if (symbol.hasAnimator && !symbol.isAnimatorDoingSomething && !symbol.isBonusSymbol && !isSymbolAlreadyAnimated)
						{
							animPlayingCounter++;
							symbol.animateOutcome(onAnimDone);

							Vector2 symbolPosId = symbol.getSymbolPositionId();
							if (!animatedSymbols.Contains(symbolPosId))
							{
								animatedSymbols.Add(symbolPosId);
							}
						}

					}
				}
			}
		}

		// Start waiting until the minimum amount of time has passed and all animations are done before finishing.
		StartCoroutine(waitToFinish());
	}

	// Coroutine that runs the cluster fade and then closes out the current display sequence.
	protected override IEnumerator displayFinish()
	{
		if (_outcome != null)
		{
			// TODO: Hide all the scatter boxes when/if we ever show them,
			// similar to how it's done in ClusterOutcomeDisplayModule.
			yield return null;
			
			handleOutcomeComplete();
		}
	}

	public override void hideLines()
	{
		//TODO: Once these animations are in set this up.
	}
	public override void showLines()
	{
		//TODO: Once these animations are in set this up.
	}

	public override string getLogText()
	{
		string returnVal = "";

		if (_outcome != null)
		{
			returnVal += "SCATTER Result Animating\n";
		}

		return returnVal;
	}

	// return a list of all symbol names that win for an outcome
	public string[] getWinningSymbolNames(SlotOutcome outcome)
	{
		if (_controller.payTable == null || _controller.payTable.scatterWins == null)
		{
			return null;
		}

		if (!_controller.payTable.scatterWins.ContainsKey(outcome.getWinId()))
		{
			return null;
		}
		PayTable.ScatterWin scatterWin = _controller.payTable.scatterWins[outcome.getWinId()];
		return scatterWin.symbols;
	}
}
