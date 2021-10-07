using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlopSlotBaseGame : TumbleSlotBaseGame
{	
	// timing variables
	public const float CALCULATED_TIME_PER_CLUSTER = TIME_MOVE_SYMBOL_UP + TIME_MOVE_SYMBOL_DOWN + TIME_FADE_SHOW_IN + TIME_FADE_SHOW_OUT + TIME_SHOW_DURATION + TIME_POST_SHOW + TIME_EXTRA_WAIT_AFTER_PAYBOXES;

	protected const float TIME_TO_REMOVE_SYMBOL = .05f;
	protected const float TIME_TO_PLOP_DOWN = .5f;

	new protected const float TIME_TO_WAIT_AT_END = .5f;
	new protected const float TIME_BETWEEN_PLOPS = 0.05f;
	new protected const float TIME_MOVE_SYMBOL_UP = .35f;
	new protected const float TIME_MOVE_SYMBOL_DOWN = .35f;
	new protected const float TIME_FADE_SHOW_IN = .35f;
	new protected const float TIME_FADE_SHOW_OUT = .35f;
	new protected const float TIME_SHOW_DURATION = .5f;
	new protected const float TIME_POST_SHOW = .3f;
	new protected const float TIME_EXTRA_WAIT_ON_SPIN = .8f;
	new protected const float TIME_EXTRA_WAIT_AFTER_PAYBOXES = .1f;
	new protected const float TIME_EXTRA_WAIT_BEAT = .5f;
	new protected const float TIME_ROLLUP_TERMINATING_WAIT = .6f;

	// how far to move symbols (in z direction) when indicating a win
	new protected const float WIN_SYMBOL_RAISE_DISTANCE = -.6f;

	protected override void Awake()
	{
		isLegacyPlopGame = true;
		base.Awake();
		useVisibleSymbolsCloneForScatter = false;
		deprecatedPlopAndTumbleOutcomeDisplayController.setTumbleOutcomeCoroutine(tumbleAfterRollup);
	}

	/// slotStartedEventCallback - called by Server when we first enter the slot game.
	protected override void slotStartedEventCallback(JSON data)
	{
		base.slotStartedEventCallback(data);
		isDoingFirstTumble = true;
		Overlay.instance.setButtons(false);
		//StartCoroutine(plopSymbols(true));
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		// zero this out every spin, normally handled by ReelGame version of prespin, but not sure if that is safe to call
		runningPayoutRollupValue = 0;
		runningPayoutRollupAlreadyPaidOut = 0;
		lastPayoutRollupValue = 0;

		reevaluationSpinMultiplierOverride = -1;

		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if(visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator != null)
				{
					StartCoroutine(removeASymbol(visibleSymbols[visibleSymbols.Length-symbolIndex-1]));
				}
				yield return new TIWaitForSeconds(TIME_TO_REMOVE_SYMBOL);
			}
		}

		if (SpinPanel.instance != null && SpinPanel.instance.stopButton != null && autoSpins == 0)
		{
			//setting the "STOP" button as disabled except during autospins to mimic web.
			SpinPanel.instance.stopButton.isEnabled = false;
		}
	}

	/// replace normal rollup with this tumbling logic
	protected override IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		yield return new WaitForSeconds(.3f);
		// First need to find out what the key name of the bonus_pool is.
		if (tumbleOutcomeJson.Length == 0)
		{
			Debug.LogWarning("Done!");
			yield break;
		}
		else
		{
			bool shouldBigWin = ((rollupEnd-rollupStart) >= Glb.BIG_WIN_THRESHOLD * SpinPanel.instance.betAmount);
			float rollupTime =  CALCULATED_TIME_PER_CLUSTER * _outcomeDisplayController.getNumClusterWins();
			if (shouldBigWin)
			{
				rollupTime *= 2.0f;
			}

			TICoroutine rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, rollupDelegate, true, rollupTime));
			currentlyTumbling = true;
			foreach(JSON tumbleOutcome in tumbleOutcomeJson)
			{
				findClusterWinningSymbols();
				findScatterWinningSymbols();
				yield return StartCoroutine(displayWinningSymbols());
				clearOutcomeDisplay();
				// Wait for the rollups to happen.
				yield return rollupRoutine;
				if (_outcomeDisplayController.rollupRoutine != null) 
				{
					yield return _outcomeDisplayController.rollupRoutine; // this will make sure we wait until all rollups are done, whether they were started by PlopSlotBaseGame or DisplayOutcomeController
				}

				// Trigger a rollup end, since we've completed a rollup
				yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));

				yield return new TIWaitForSeconds (TIME_ROLLUP_TERMINATING_WAIT);
				yield return StartCoroutine(removeWinningSymbols());
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_BEAT);
				yield return StartCoroutine(doExtraBeforePloppingNewSymbols());
				yield return StartCoroutine(plopNewSymbols());
				SlotOutcome outcome = new SlotOutcome(tumbleOutcome);
				setOutcome(outcome);
				_outcomeDisplayController.rollupsRunning[_outcomeDisplayController.rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
				
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_ON_SPIN);
				yield return StartCoroutine(doReelsStopped(isAllowingContinueWhenReadyToEndSpin: false));
				// When we pause this coroutine after doReelsStopped we need to wait a frame here so it knows where to
			}
			currentlyTumbling = false;
			yield return new TIWaitForSeconds(TIME_TO_WAIT_AT_END);


			handleBigWinEnd();
			// We're finally done with this whole outcome.
			yield return StartCoroutine(_outcomeDisplayController.finalizeRollup());
			
		}
	}

	// go through all the winning symbols and remove them one by one regardless of which cluster they're in
	protected override IEnumerator removeAllWinningSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i=0; i < willBeRemoved.Count; i++)
		{
			for (int j=0; j < willBeRemoved[i].Count; j++)
			{
				if (willBeRemoved[i][j])
				{
					SlotSymbol symbol = reelArray[i].visibleSymbols[slotGameData.numVisibleSymbols-j-1];
					StartCoroutine(removeASymbol(symbol));
					yield return new TIWaitForSeconds(TIME_TO_REMOVE_SYMBOL);
				}
			}
		}
		yield return null;
	}

	// Wait for the symbol to plop away and then deactivate it
	protected override IEnumerator doWinMovementAndPaybox(SlotSymbol symbol, KeyValuePair<int,int> pair, ClusterOutcomeDisplayModule.Cluster cluster, int symbolNum = 0, bool hasDoneCluster = false )
	{
		PlopClusterScript plopCluster = null;
		if (cluster.clusterScript != null && !hasDoneCluster) // could be null if we're dealing with scatter outcome (like from Bonus Game)
		{
			plopCluster = cluster.clusterScript as PlopClusterScript;
			yield return StartCoroutine(plopCluster.specialShow(TIME_FADE_SHOW_IN, 0.0f, WIN_SYMBOL_RAISE_DISTANCE));

		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_IN);
		}

		yield return new TIWaitForSeconds(TIME_SHOW_DURATION);

		if (plopCluster != null && !hasDoneCluster)
		{
			yield return StartCoroutine(plopCluster.specialHide(TIME_FADE_SHOW_OUT));
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_OUT);
		}
		yield return new TIWaitForSeconds(TIME_POST_SHOW);
	}

	// Loop through all symbols in the visible area. If we need to replace it, find the replacement symbol, create it and update
	protected override IEnumerator plopNewSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i=0; i < willBeRemoved.Count; i++)
		{
			for (int j=willBeRemoved[i].Count - 1; j >= 0; j--) // plop symbols in reverse
			{
				if (willBeRemoved[i][j])
				{
					string nextSymbolName = reelArray[i].getNextSymbolName();
					Vector3 symbolPosition = originalPositions[new KeyValuePair<int,int>(i, j)];
					
					SymbolAnimator nextSymbolAnim = getSymbolAnimatorInstance(nextSymbolName, i);
					GameObject nextSymbol = nextSymbolAnim.gameObject;
					nextSymbol.transform.parent = reelArray[i].getReelGameObject().transform;
					nextSymbol.transform.localPosition = symbolPosition;
					nextSymbol.transform.localScale = nextSymbolAnim.info.scaling;

					reelArray[i].visibleSymbols[reelArray[i].visibleSymbols.Length-j-1].setNameAndAnimator(nextSymbolName, nextSymbolAnim);
					willBeRemoved[i][j] = false;
					StartCoroutine(plopSymbolAt(i, j));
					yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
				}
			}
		}
		
		symbolsToRemove.Clear();
		yield return new TIWaitForSeconds(TIME_TO_PLOP_DOWN - TIME_BETWEEN_PLOPS);
	}

	protected virtual IEnumerator plopSymbolAt(int row, int column)
	{
		SlotReel[] reelArray = engine.getReelArray();

		StartCoroutine(reelArray[row].visibleSymbols[reelArray[row].visibleSymbols.Length-column-1].plopDown(TIME_TO_PLOP_DOWN, iTween.EaseType.easeInCubic));
		yield return new TIWaitForSeconds(TIME_TO_PLOP_DOWN);
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(plopSymbols());
	}

	// Iterate through all symbols, setup our columns GameObject arrays
	protected override IEnumerator plopSymbols(bool firstTime = false)
	{
		originalPositions.Clear();
		symbolCamera.SetActive(true);
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator.gameObject.transform.localPosition);
				StartCoroutine(plopSymbolAt(reelIndex, symbolIndex));
				yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
			}
		}
		yield return new TIWaitForSeconds(TIME_TO_PLOP_DOWN - TIME_BETWEEN_PLOPS + TIME_EXTRA_WAIT_ON_SPIN);
		if (!firstTime)
		{
			StartCoroutine(doReelsStopped());
#if ZYNGA_TRAMP
				AutomatedPlayer.spinFinished();
#endif
		}
		else
		{
			isDoingFirstTumble = false;
			Overlay.instance.setButtons(true);
		}
	}

	public override void doSpecialOnBonusGameEnd()
	{
		playBgMusic();

		returnSymbolsAfterBonusGame();
		deprecatedPlopAndTumbleOutcomeDisplayController.resumeTumble();
		// Visible Symbols crashes out zynga01, so we're not going to stop these animations.
		/*
		for (int reelID = 0; reelID < engine.reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null)
				{
					symbol.animator.stopAnimation(true);
				}
			}
		}
		*/
	}

	/// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
	
}
