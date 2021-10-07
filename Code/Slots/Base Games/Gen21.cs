using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base class for Gen21. A basic Tumble Slot game.
*/

public class Gen21 : TumbleSlotBaseGame 
{
	[SerializeField] private Animator freeSpinTransitionAnimator = null;
	[SerializeField] private string freeSpinTransitionName;				
	[SerializeField] private string freeSpinTransitionToBaseName;				
	[SerializeField] private string wheelBonusTransitionName;				
	[SerializeField] private string wheelBonusTransitionToBaseName;				
	[SerializeField] private string transitionClipName;
	[SerializeField] private float transitionClipDelay = 0f;
	[SerializeField] private string wheelTransitionClipName;
	[SerializeField] private float wheelTransitionClipDelay = 0f;

	[SerializeField] private GameObject backgroundSlider = null;					// The background slider.
	[SerializeField] private ReelGameBackground backgroundScript;					// The script that holds onto the wings.

	private bool canResumeTumbling = true;
	private bool doingFreeSpins;

	// movement constants
	private const float BG_SLIDE_Y_DISTANCE = -1.3f;


	protected override IEnumerator animateAllBonusSymbols()
	{
		canResumeTumbling = false;

		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int symbolIndex = 0; symbolIndex < visibleSymbolClone[reelIndex].Count; symbolIndex++)
			{
				if (visibleSymbolClone[reelIndex][symbolIndex].isBonusSymbol)
				{
					visibleSymbolClone[reelIndex][symbolIndex].animateOutcome();
				}
			}
		}
		// Let the animations finish.
		yield return new TIWaitForSeconds(ANIMATION_WAIT_TIME);


		// Check and see if it's the freespins bonus.
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
		{
			doingFreeSpins = true;
			yield return StartCoroutine(playBonusGameTransition(freeSpinTransitionName));
		}
		else
		{
			doingFreeSpins = false;
			yield return StartCoroutine(playBonusGameTransition(wheelBonusTransitionName));
		}
	}

	protected override IEnumerator prespin()
	{	
		if (isGameUsingOptimizedFlattenedSymbols && visibleSymbolClone != null)
		{
			SlotReel[] reelArray = engine.getReelArray();

			// go through the  symbols and make sure they are flattened for fading
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;

				for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
				{
					flattenSymbolWithRestore(visibleSymbolClone[reelIndex][reelArray[reelIndex].visibleSymbols.Length-symbolIndex-1]);
				}
			}
		}

		yield return StartCoroutine(base.prespin());

	}	

	protected override IEnumerator plopNewSymbols()
	{
		// check which symbols need to be unflattened before animating in a winning payline
		// it needs to be done before any tumble movement happens since the flatten will reset the symbol
		// to its original position
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymbols();
			unflattenBonusSymbols();
		}

		yield return StartCoroutine(base.plopNewSymbols());

	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		if (_outcome.isBonus)
		{
			createBonus();
		}

		// check which symbols need to be unflattened before animating in a winning payline
		// it needs to be done before any tumble movement happens since the flatten will reset the symbol
		// to its original position
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymbols();
			unflattenBonusSymbols();
		}

		StartCoroutine(plopSymbols());
	}

	private void flattenSymbolWithRestore(SlotSymbol symbol)
	{
		if (symbol != null && symbol.transform != null)
		{
			if (!symbol.isFlattenedSymbol)
			{
				Vector3 originalPosition = symbol.transform.position;
				symbol.mutateToFlattenedVersion(null, false, true, false);
				symbol.transform.position = originalPosition;  // restore position changed by flatten
			}
		}
	}

	private void unFlattenWithRestore(SlotSymbol symbol)
	{
		if (symbol.isFlattenedSymbol)
		{
			Vector3 originalPosition = symbol.transform.position;
			symbol.mutateTo(symbol.serverName, null, false, true);
			symbol.transform.position = originalPosition;  // restore position changed by flatten
		}
	}	

	protected void unflattenBonusSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		// check for bonus symbols to flatten since they always animate winning payline or not
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = visibleSymbols[symbolIndex];	
				if (symbol.isBonusSymbol && symbol.isFlattenedSymbol)
				{
					unFlattenWithRestore(symbol);
				}
			}
		}

	}

	private void unFlattenWinningSymbols()
	{
		HashSet<SlotSymbol> winningSymbols = outcomeDisplayController.getSetOfWinningSymbols(_outcome);

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.isMajor || symbol.isWildSymbol)   // only majors and wilds animate in gen21
			{
				unFlattenWithRestore(symbol);
			}
		}		
	}		

	private IEnumerator playBonusGameTransition(string transitionName)
	{
		slideOutSpinPanel();	

		// kick off extra transition audio
		if (!string.IsNullOrEmpty(transitionClipName))
		{
			// all transitions
			Audio.playSoundMapOrSoundKeyWithDelay(transitionClipName, transitionClipDelay);
		}

		if (!string.IsNullOrEmpty(wheelTransitionClipName) && transitionName == "fade base wheel")
		{
			// just wheel game
			Audio.playSoundMapOrSoundKeyWithDelay(wheelTransitionClipName, wheelTransitionClipDelay);
		}

		if (freeSpinTransitionAnimator != null)
		{
			deactivateSymbols();

			freeSpinTransitionAnimator.gameObject.SetActive(true);
			freeSpinTransitionAnimator.Play(transitionName);

			Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_FREESPIN_PT1));
			Audio.play(Audio.soundMap(FREESPIN_VO), 1.0f, 0, 0.6f);
			yield return new TIWaitForSeconds(TRANSITION_WAIT_TIME_1);


			Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_FREESPIN_PT2));
			yield return new TIWaitForSeconds(TRANSITION_WAIT_TIME_2);

		}
	}	

	private void deactivateSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.gameObject != null)
				{
					// Turn off all of the symbols.
					symbol.gameObject.SetActive(false);
				}
			}
		}
	}

	private void slideOutSpinPanel()
	{
			if (SpinPanel.instance != null)
			{
				float spinPanelSlideOutTime = BACKGROUND_SLIDE_TIME;
				if (SpinPanel.instance.backgroundWingsWidth != null)
				{
					float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
					spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
				}
				StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));
			}	

			StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, TRANSITION_SLIDE_TIME, false));

	}

	private  IEnumerator transitionToBaseGame(string animationName)
	{
		freeSpinTransitionAnimator.gameObject.SetActive(true);

		if (!string.IsNullOrEmpty(animationName))
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(freeSpinTransitionAnimator, animationName));
		}

		restoreBaseGame();

		base.doSpecialOnBonusGameEnd();	

		yield return null;

		freeSpinTransitionAnimator.gameObject.SetActive(false);	
			
		canResumeTumbling = true;

		showNonBonusOutcomes();

		yield return null;
	} 		

	public override void doSpecialOnBonusGameEnd()
	{
		SpinPanel.instance.showFeatureUI (false);   // hide charm (hide charm module does not work with tumble games when returning from bonus game)
		if (doingFreeSpins)
		{
			StartCoroutine(transitionToBaseGame(freeSpinTransitionToBaseName));
		}
		else
		{
			StartCoroutine(transitionToBaseGame(wheelBonusTransitionToBaseName));
		}
	}

	// override this so there is no tumbling until transition from bonus game is done.
	protected override IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJsonArray, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		while (!canResumeTumbling)
		{
			yield return null;
		}


		yield return StartCoroutine(base.tumbleAfterRollup(tumbleOutcomeJsonArray, basePayout, bonusPayout, rollupDelegate, doBigWin, bigWinDelegate, rollupStart, rollupEnd));
	}	

	public override void showNonBonusOutcomes()
	{
		if (!canResumeTumbling)
		{
			return;
		}


		base.showNonBonusOutcomes();

	}	

	private void restoreBaseGame()
	{
		// Now put the base game back in a good state to return to
		// reset everything back to normal.
		backgroundSlider.transform.localPosition = Vector3.zero;

		Overlay.instance.top.restorePosition();
		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);

		SlotReel[] reelArray = engine.getReelArray();

		// Turn on all of the symbols
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			//SlotSymbol[] visibleSymbols = visibleSymbolClone[reelID];
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.gameObject != null)
				{
					symbol.animator.gameObject.SetActive(true);
				}
			}
		}

	}

	// We get some weird anticipation information in tumble games so we need to do this.
	public override IEnumerator playBonusAcquiredEffects()
	{
		if (outcome.isBonus)
		{
			// we want to see what symbol landed on reel 2, to find out what sound we should play.
			SlotReel[] reelArray = engine.getReelArray();

			SlotSymbol[] visibleSymbols =reelArray[2].visibleSymbols;
			foreach (SlotSymbol symbol in visibleSymbols)
			{
				if (symbol.serverName == "BN1")
				{
					Audio.play(Audio.soundMap(FREESPINS_BONUS_SOUND));
				}
				else if (symbol.serverName == "BN2")
				{
					Audio.play(Audio.soundMap(PICKEM_BONUS_SOUND));
				}
			}
			yield return StartCoroutine(animateAllBonusSymbols());
			isBonusOutcomePlayed = true;
		}
	}
	protected override void onBigWinNotification (long payout, bool isSettingStartingAmountToPayout = false)
	{
		StartCoroutine(bigWinCoroutine(payout, isSettingStartingAmountToPayout));
	}

	private IEnumerator bigWinCoroutine(long payout, bool isSettingStartingAmountToPayout = false)
	{
		float rollupTime = BASE_ROLLUP_TIME * 2.0f; //Want to wait the duration of the rollup before doing the big win
		yield return new TIWaitForSeconds(rollupTime); 
		base.onBigWinNotification(payout, isSettingStartingAmountToPayout);
	}
}
