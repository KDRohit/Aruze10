using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlopFreeSpinGame : TumbleFreeSpinGame
{	
	// update "constants" from base class
	private void setConstantValues()
	{
		TIME_TO_REMOVE_SYMBOL = .05f;
	}

	protected const float SYMBOL_Y_DISTANCE = 1.86f;

	// initialize this slot withnecessary things
	protected override void Awake()
	{
		isLegacyPlopGame = true;
		base.Awake();
		isLegacyTumbleGame = false;
		useVisibleSymbolsCloneForScatter = false;
		setConstantValues();
	}

	public override void initFreespins()
	{
		base.initFreespins();

		deprecatedPlopAndTumbleOutcomeDisplayController.setTumbleOutcomeCoroutine(tumbleAfterRollup);
	}

	/// replace normal rollup with this tumbling logic
	private IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		yield return new TIWaitForSeconds(.3f);
		// First need to find out what the key name of the bonus_pool is.
		long payout = 0;
		if(tumbleOutcomeJson.Length == 0)
		{
			yield break;
		}
		else
		{
			TICoroutine rollupRoutine =  StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, rollupDelegate, true, getWaitTimePerCluster() * _outcomeDisplayController.getNumClusterWins()));

			foreach(JSON tumbleOutcome in tumbleOutcomeJson)
			{
				findClusterWinningSymbols();
				findScatterWinningSymbols();
				yield return StartCoroutine(displayWinningSymbols());
				clearOutcomeDisplay();
				// Wait for the rollups to happen
				yield return rollupRoutine;
				if (_outcomeDisplayController.rollupRoutine != null) 
				{
					yield return _outcomeDisplayController.rollupRoutine; // this will make sure we wait until all rollups are done, whether they were started by PlopSlotBaseGame or DisplayOutcomeController
				}

				// Trigger a rollup end, since we've completed a rollup
				yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));

				yield return StartCoroutine(removeWinningSymbols());
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_BEAT);
				yield return StartCoroutine(doExtraBeforePloppingNewSymbols());
				yield return StartCoroutine(plopNewSymbols());
				SlotOutcome outcome = new SlotOutcome(tumbleOutcome);
				setOutcome(outcome);

				yield return StartCoroutine(onPloppingFinished());

				_outcomeDisplayController.rollupsRunning[_outcomeDisplayController.rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
				
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_ON_SPIN);
				yield return StartCoroutine(doReelsStopped());
			}
			
			if (doBigWin && bigWinDelegate != null)
			{
				// We need to handle the big win calls ourselves.
				bigWinDelegate(payout, false);
			}

			yield return new TIWaitForSeconds(TIME_TO_WAIT_AT_END);
			// We're finally done with this whole outcome.
			yield return StartCoroutine(_outcomeDisplayController.finalizeRollup());
		}
	}

	// Destroy all visible symbols, then start the spin
	protected override IEnumerator prespin()
	{
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

		yield return new TIWaitForSeconds(TIME_TO_REMOVE_SYMBOL);
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
					onWinningSymbolRemoved(symbol);
					StartCoroutine(removeASymbol(symbol));
					yield return new TIWaitForSeconds(TIME_TO_REMOVE_SYMBOL);
				}
			}
		}
		yield return null;
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
					StartCoroutine(plopSymbolAt(i,j));
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

	// Iterate through all symbols, setup our columns GameObject arrays
	protected override IEnumerator plopSymbols()
	{
		symbolCamera.SetActive(true);
		originalPositions.Clear();

		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if (visibleSymbols[visibleSymbols.Length-symbolIndex-1] != null && visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator != null && visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator.gameObject != null)
				{
					originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator.gameObject.transform.localPosition);
				}
				else
				{
					Debug.LogWarning("A visible symbol was not initialized properly, lets grab a new instance");
					string symbolName = reelArray[reelIndex].getSpecificSymbolName(symbolIndex);
					SymbolAnimator nextSymbolAnim = getSymbolAnimatorInstance(symbolName, reelIndex);					
					GameObject nextSymbol = nextSymbolAnim.gameObject;
					nextSymbol.transform.parent = reelArray[reelIndex].getReelGameObject().transform;
					nextSymbol.transform.localPosition = new Vector3(0.0f, SYMBOL_Y_DISTANCE * symbolIndex, 0.0f);
					nextSymbol.transform.localScale = nextSymbolAnim.info.scaling;
					visibleSymbols[visibleSymbols.Length-symbolIndex-1].setNameAndAnimator(symbolName, nextSymbolAnim);
					yield return null;
				}
				StartCoroutine(plopSymbolAt(reelIndex, symbolIndex));
				yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
			}
		}
		yield return new TIWaitForSeconds(TIME_TO_PLOP_DOWN - TIME_BETWEEN_PLOPS + TIME_EXTRA_WAIT_ON_SPIN);

		// let a derived class handle the plopped symbols
		yield return StartCoroutine(onPloppingFinished());

		// let other stuff know that the reels are done
		StartCoroutine(doReelsStopped());
	}
}
