using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * TumbleReel.cs
 * Subclass of SlotReel. Handles visual outcome for tumble games. [Now deprecated]
 * author: Nick Reynolds
 */
public class DeprecatedTumbleReel : SlotReel
{
	public DeprecatedTumbleReel(ReelGame reelGame) : base(reelGame)
	{
		
	}
	
	// frameUpdate - manages the state progession and the passage of time to the movement of the symbols.
	public override void frameUpdate()
	{
		base.frameUpdate();

		switch (_spinState)
		{
		case ESpinState.BeginRollback:
		{
			// reset the anticipation animation tracking stuff
			startedAnticipationAnims = 0;
			finishedAnticipationAnims = 0;

			_reelOffset = -1.0f * 1.0f * gameData.rollbackAmount;

			_spinState = ESpinState.Spinning;

			break;
		}
			
		case ESpinState.Spinning:
		{
			// no spinning needed
			newSymbolsGrabbed = 0;
			break;
		}
			
		case ESpinState.SpinEnding:
		{
			bool didAdvance = false;
			while (_reelPosition != _reelStopIndex)
			{
				advanceSymbols();
				_reelOffset -= 1f;
				didAdvance = true;
			}
			
			if (!didAdvance)
			{
				float targetOffset = gameData.reelStopAmount;

				if (_reelPosition == _reelStopIndex)
				{
					_reelOffset = targetOffset;
					_spinState = ESpinState.EndRollback;
					_rollbackStartTime = Time.time;
					
					// Grab Bonus Data
					for(int i = 0; i < visibleSymbols.Length; i++)
					{
						if (visibleSymbols[i].isBonusSymbol && visibleSymbols[i].getRow() == 1)
						{
							// Bonus Hit.
							_reelGame.engine.bonusHits++;

							break;
						}
					}
					
					// Grab scatter Data and play appropriate reel stop sounds.
					for (int i = 0; i < visibleSymbols.Length; i++)
					{
						if (visibleSymbols[i].name.Contains("SC"))
						{
							// Scatter hit.
							_reelGame.engine.scatterHits++;
							break;
						}
					}

					_reelGame.engine.hideAnticipationEffect(reelID);
					// Play appropriate reel stop sounds.
					// anticipation info doesn't include layer as a concept, so always assume we are talking about layer 0
					if (!_reelGame.engine.isSlamStopPressed && layer == 0)
					{
						_reelGame.engine.checkAnticipationEffect(this); //We are looking up the reel number not the position in the array.
					}
				}
			}
			slideSymbols(0);
			break;
		}
			
		case ESpinState.EndRollback:
		{
			float rollbackPct = 1.0f;
			_reelOffset = (1.0f - rollbackPct) * gameData.reelStopAmount;

			if (rollbackPct == 1.0f)
			{
				_spinState = ESpinState.Stopped;
				
				if (_replacementStrip != null)
				{
					// check if the size of the base reel strip is shorter than the replacement, 
					// in which case we need to adjust _reelPosition so it isn't out of bounds
					if (_reelData.reelStrip.symbols.Length < _replacementStrip.symbols.Length)
					{
						_reelPosition = _reelData.reelStrip.symbols.Length - numberOfTopBufferSymbols - 1;
					}

				}
				
				// Reset the expected direction of the spin to be down.
				spinDirection = ESpinDirection.Down;

			}
			break;
		}
			
		case ESpinState.Stopped:
		{
			if (!anticipationAnimsFinished)
			{
				// check if the user has forced the reels to stop, in which case we need to cancel waiting for anticipation animations if the user had bonuses showing
				if (_reelGame.engine.isSlamStopPressed)
				{
					// Look for symbols that are already doing anticipation animations and stop them
					for (int i = 0; i < visibleSymbols.Length; i++)
					{
						SlotSymbol symbol = visibleSymbols[i];
						
						if (_isAnticipation && symbol.hasAnimator && symbol.isAnimatorDoingSomething)
						{
							// force animations to stop
							symbol.haltAnimation();
						}
					}
					
					// mark all animation as finished, which will allow us to continue to the next steps of starting a bonus
					finishedAnticipationAnims = startedAnticipationAnims;
				}
			}
			
			break;
		}
		}
	}
	
	/**
	Tracks how many of the started bonus symbol animations have completed
	*/
	public override void onAnticipationAnimationDone(SlotSymbol sender)
	{
		finishedAnticipationAnims++;
	}
}
