using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TumbleReel : SlotReel
{
	private bool isCurrentlyTumbling = false;			// Set while the symbols are visually tumbling
	private bool isPlayingTumbleAnimations = false;		// Set while the cleanup animations (disappearing effect) is in progress
	private bool alreadyWaitingToCleanup = false;		// Wait to finish ending the spin until the previous one is properly cleaned up
	private int nextTumblingSymbolIndex;				// Holds the index of the next symbol in the reel strip that tumbles in
	private bool hasPlayedSymbolAnticipations = false;	// Keeps track of symbol anticipation animation being played for a reel

	private const string TUMBLE_BONUS_SYMBOL_FANFARE_KEY = "bonus_symbol_fanfare";
	private const string TUMBLE_SYMBOL_HIT_BN_VO_SOUND_KEY = "bonus_symbol_vo_sweetener";	// VO sound played every time the BN symbol lands on the reels

	private float[] tumbleSymbolOffsets;				// Keeps track of the offset by which a particular symbol needs to move down (tumble)

	public TumbleReel(ReelGame reelGame) : base(reelGame)
	{

	}

	public TumbleReel(ReelGame reelGame, GameObject reelRoot) : base(reelGame, reelRoot)
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
				hasPlayedSymbolAnticipations = false;
				if (!alreadyWaitingToCleanup) //Only need to start this coroutine once
				{
					RoutineRunner.instance.StartCoroutine(waitForModulesThenCleanUp());
				}
				break;
			}

		case ESpinState.Spinning:
			{
				newSymbolsGrabbed = 0;
				break;
			}

		case ESpinState.SpinEnding:
			{
				if (!isCurrentlyTumbling)
				{
					_reelPosition = _reelStopIndex;
					nextTumblingSymbolIndex = getSymbolIndexAtListPosition(0) - 1;
					tumbleSymbolOffsets = new float[_symbolList.Count];

					for (int i = 0; i < _symbolList.Count; i++)
					{
						tumbleSymbolOffsets[i] = _symbolList.Count;
						int symbolIndex = getSymbolIndexAtListPosition(i);
						string symbolName = getReelSymbolAtIndex(symbolIndex);
						_symbolList[i].setupSymbol(symbolName, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
						_symbolList[i].refreshSymbol(-tumbleSymbolOffsets[i] * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer));
					}
					refreshVisibleSymbols();
					isCurrentlyTumbling = true;
				}
				else if (!isPlayingTumbleAnimations)
				{
					RoutineRunner.instance.StartCoroutine(performTumble());
				}
				break;
			}

		// New state for tumble reels that moves symbols for the tumble outcomes
		case ESpinState.Tumbling:
			{
				if (!isPlayingTumbleAnimations)
				{
					if (!isCurrentlyTumbling)
					{						
						RoutineRunner.instance.StartCoroutine(updateSymbolPositions());
					}
					else
					{
						RoutineRunner.instance.StartCoroutine(performTumble());
					}
				}
				break;
			}

		case ESpinState.EndRollback:
			{
				_spinState = ESpinState.Stopped;
				break;
			}

		case ESpinState.Stopped:
			{
				alreadyWaitingToCleanup = false;
				break;
			}
		}
	}

	private IEnumerator updateSymbolPositions()
	{
		isPlayingTumbleAnimations = true;
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		tumbleSymbolOffsets = new float[_symbolList.Count];
		int numSymbolsCleanedUp = 0;

		//remove symbols that are part of the paylines and add the offset value by which symbols need to visually tumble
		int offsetIndex = _symbolList.Count - 1;

		for (int i = _symbolList.Count - 1; i >= 0; i--)
		{
			tumbleSymbolOffsets[i] = 0.0f;

			if ((_reelGame.engine as TumbleSlotEngine).previousWinningSymbols.Contains(_symbolList[i]))
			{
				bool isSymbolCleanedUpByModule = false;
				foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
				{
					if (module.needsToExecuteBeforeCleanUpWinningSymbolInTumbleReel(_symbolList[i]))
					{
						runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(module.executeBeforeCleanUpWinningSymbolInTumbleReel(_symbolList[i])));
					}

					if (module.isCleaningUpWinningSymbolInTumbleReel(_symbolList[i]))
					{
						isSymbolCleanedUpByModule = true;
					}
				}

				if (!isSymbolCleanedUpByModule)
				{
					// a module didn't cleanup this symbol so we should do it here
					_symbolList[i].cleanUp();
				}
				numSymbolsCleanedUp++;
			}
			else
			{
				tumbleSymbolOffsets[offsetIndex] = numSymbolsCleanedUp;
				offsetIndex--;
			}
		}

		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

		//move symbols down (logical tumble)
		for (int i = _symbolList.Count - 1; i >= numSymbolsCleanedUp; i--)
		{
			if (tumbleSymbolOffsets[i] > 0)
			{
				int oldIndex = (int)(i - tumbleSymbolOffsets[i]);
				_symbolList[i].cleanUp();
				_symbolList[i].setupSymbol(_symbolList[oldIndex].name, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
				_symbolList[i].refreshSymbol(-tumbleSymbolOffsets[i] * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer));
			}

		}

		//setup new symbols in the correct positions (logical tumble)
		for (int i = numSymbolsCleanedUp - 1; i >= 0; i--)
		{
			int symbolIndex = nextTumblingSymbolIndex;
			while (symbolIndex < 0)
			{
				symbolIndex += reelSymbols.Length;
			}
			if (symbolIndex >= reelSymbols.Length)
			{
				symbolIndex %= reelSymbols.Length;
			}
			string symbolName = getReelSymbolAtIndex(symbolIndex);
			nextTumblingSymbolIndex--;

			_symbolList[i].cleanUp();
			_symbolList[i].setupSymbol(symbolName, i, this, normalReplacementSymbolMap, megaReplacementSymbolMap, true);
			tumbleSymbolOffsets[i] = numSymbolsCleanedUp;
			_symbolList[i].refreshSymbol(-tumbleSymbolOffsets[i] * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer));
		}

		refreshVisibleSymbols();
		isCurrentlyTumbling = true;
		isPlayingTumbleAnimations = false;
	}

	private int getSymbolIndexAtListPosition(int pos)
	{
		int symbolIndex = _reelStopIndex - visibleSymbols.Length + pos + 1;

		while (symbolIndex < 0)
		{
			symbolIndex += reelSymbols.Length;
		}
		if (symbolIndex >= reelSymbols.Length)
		{
			symbolIndex %= reelSymbols.Length;
		}

		return symbolIndex;
	}

	protected override void setBufferSymbolAmount(ReelStrip reelStrip)
	{
		numberOfTopBufferSymbols = prevTopBufferSymbolsCount = 0;
		numberOfBottomBufferSymbols = prevBottomBufferSymbolsCount = 0;
	}

	private IEnumerator performTumble()
	{
		bool bonusHit = false;
		isPlayingTumbleAnimations = true;
		SlotSymbol bonusSymbol = null;

		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToChangeTumbleReelWaitFromModule())
			{
				yield return RoutineRunner.instance.StartCoroutine(module.changeTumbleReelWaitFromModule(this.reelID));
				break;
			}
		}

		List<TICoroutine> runningCoroutines = new List<TICoroutine>();

		// we need to store out the current bonus hits to play it later (since the engine one will continue to increment via other tumble coroutines)
		int currentBonusHits = 1;

		for (int i = _symbolList.Count - 1; i >= 0; i--)
		{
			if (!hasPlayedSymbolAnticipations && _symbolList[i].isBonusSymbol)
			{
				
				_reelGame.engine.bonusHits++;
				currentBonusHits = _reelGame.engine.bonusHits;
				bonusHit = true;
				bonusSymbol = _symbolList[i];
			}

			if (tumbleSymbolOffsets[i] > 0)
			{
				for (int moduleIndex = 0; moduleIndex < _reelGame.cachedAttachedSlotModules.Count; moduleIndex++)
				{
					SlotModule module = _reelGame.cachedAttachedSlotModules[moduleIndex];
					
					if (module.needsToTumbleSymbolFromModule())
					{
						runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(module.tumbleSymbolFromModule(_symbolList[i], -tumbleSymbolOffsets[i] * _reelGame.getSymbolVerticalSpacingAt(reelID - 1, layer))));
						yield return new TIWaitForSeconds(0.1f);
					}
				}
			}
		}

		// handle modules that want to do stuff once all the symbols are tumbling in
		for (int i = 0; i < _reelGame.cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = _reelGame.cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnTumbleReelSymbolsTumbling(this))
			{
				yield return RoutineRunner.instance.StartCoroutine(module.executeOnTumbleReelSymbolsTumbling(this));
			}
		}

		if (bonusHit == true && bonusSymbol != null && !hasPlayedSymbolAnticipations)
		{
			hasPlayedSymbolAnticipations = true;
			if (!_reelGame.engine.isSlamStopPressed && bonusSymbol.hasAnimator && (!bonusSymbol.isAnimatorDoingSomething || bonusSymbol.animator.isTumbleSquashAndStretching))
			{
				bool isModuleHandlingAnticipation = false;

				//Debug.Log("Animating aniticipation for symbol: " + symbol.name + " at stopInfo.reelID = " + stopInfo.reelID + "; stopInfo.row = " + stopInfo.row + "; stopInfo.layer = " + stopInfo.layer + "; currentReel = " + currentReel);

				foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
				{
					if (module.needsToExecuteForSymbolAnticipation(bonusSymbol))
					{
						module.executeForSymbolAnticipation(bonusSymbol);
						isModuleHandlingAnticipation = true;
					}
				}

				if (!isModuleHandlingAnticipation)
				{
					// Play any setup anticipation animation if stopping naturally
					incrementStartedAnticipationAnims();
					bonusSymbol.animateAnticipation(onAnticipationAnimationDone);
				}
			}
			Audio.playSoundMapOrSoundKey(TUMBLE_BONUS_SYMBOL_FANFARE_KEY + currentBonusHits);
			Audio.playSoundMapOrSoundKey(TUMBLE_SYMBOL_HIT_BN_VO_SOUND_KEY + currentBonusHits);
		}
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));

		slideSymbols(0);		//Go through all symbols and set the final position
		isCurrentlyTumbling = false;
		_spinState = ESpinState.EndRollback;
		isPlayingTumbleAnimations = false;
	}

	public void setReelTumbling()
	{
		_spinState = ESpinState.Tumbling;
	}

	private IEnumerator waitForModulesThenCleanUp()
	{
		alreadyWaitingToCleanup = true;
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();

		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnBeginRollback(this))
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(module.executeOnBeginRollback(this)));
			}
		}

		if (runningCoroutines.Count > 0)
		{
			yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		}

		for (int i = 0; i < _symbolList.Count; i++)
		{
			_symbolList[i].cleanUp();
		}
		_spinState = ESpinState.Spinning; //Start spinning once the modules are done and everything is cleaned up
	}
}
