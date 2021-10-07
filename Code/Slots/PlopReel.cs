using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * PlopReel.cs
 * Subclass of SlotReel. Handles visual outcome for Plop games.
 * Skips all the actually spinning of reals and goes straight to displaying the outcome immediately
 * author: Nick Reynolds
 */
public class PlopReel : SlotReel
{

	public PlopReel(ReelGame reelGame) : base(reelGame)
	{

	}

	// frameUpdate - manages the state progession and the updating of the symbols.
	public override void frameUpdate()
	{
		base.frameUpdate();
		
		switch (_spinState)
		{
		case ESpinState.BeginRollback:
		{
			// skip straight to spinning
			_spinState = ESpinState.Spinning;
			break;
		}
			
		case ESpinState.Spinning:
		{
			newSymbolsGrabbed = 0;
			// do nothing while spinning, nothing to see here
			break;
		}
			
		case ESpinState.SpinEnding:
		{
			// This state switches reel position to start inserting the symbols needed to stop on the correct one.
			// State ends when the _reelStopIndex symbol is gameData.reelStopHeight past the correct stop position.
			_reelOffset += (100 * Time.deltaTime * 30.0f) / 1;
			
			bool didAdvance = false;
			while (_reelOffset > 1f && _reelPosition != _reelStopIndex)
			{
				advanceSymbols();
				_reelOffset -= 1f;
				didAdvance = true;
			}
			
			if (!didAdvance)
			{
				float targetOffset = gameData.reelStopAmount;
				bool bonusHit = false;
				bool scatterHit = false;
				bool TWHit = false;
				
				if (_reelPosition == _reelStopIndex && _reelOffset >= targetOffset)
				{
					_reelOffset = targetOffset;
					_spinState = ESpinState.EndRollback;
					_rollbackStartTime = Time.time;

					// Grab Bonus Data
					for(int i = 0; i < visibleSymbols.Length; i++)
					{
						if (visibleSymbols[i].name.Contains("BN"))
						{
							// Bonus Hit.
							_reelGame.engine.bonusHits++;
							bonusHit = true;
							break;
						}
					}

					for(int i = 0; i < visibleSymbols.Length; i++)
					{
						if (visibleSymbols[i].name.FastStartsWith("TW"))
						{
							// TWHit
							TWHit = true;
							break;
						}
					}

				}
				// Play our anticipation sounds.
				_reelGame.engine.playAnticipationSound(_reelID - 1, bonusHit, scatterHit, TWHit);
			}
			slideSymbols(_reelOffset);
			break;
		}
			
		case ESpinState.EndRollback:
		{
			// nothing special here either, just advance state to stopped
			_spinState = ESpinState.Stopped;
			break;
		}
			
		case ESpinState.Stopped:
		{
			// don't do anything while stopped
			break;
		}
		}
	}
}
