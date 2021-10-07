using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Copied from Gen21.cs and modified for wicked01.
*/

public class Wicked01 : TumbleSlotBaseGame 
{
	[SerializeField] private Animator gearAnimator;
	private const string START_GEAR_ANIM_NAME = "Base_Idle_Gears_rotating";
	private const string STOP_GEAR_ANIM_NAME = "Stop";
	
	[SerializeField] private Animator portalTransitionAnimator;
	private const string PORTAL_TRANSITION_ANIM_NAME = "Anim";
	private const string PORTAL_TRANSITION_MUSIC_KEY = "bonus_portal_transition_to_portal";

	[SerializeField] GameObject[] reelGos;

	[SerializeField] private Animator freeSpinTransitionAnimator = null;
	[SerializeField] private string freeSpinTransitionName;				
	[SerializeField] private string freeSpinTransitionToBaseName;				
	[SerializeField] private string wheelBonusTransitionName;				
	[SerializeField] private string wheelBonusTransitionToBaseName;				
	[SerializeField] private GameObject backgroundSlider = null;
	[SerializeField] private ReelGameBackground backgroundScript;
	[SerializeField] private float FADE_TIME = 0.5f;
	[SerializeField] private float WAIT_BEFORE_BONUS_OUTCOME_ANIMS = 1.0f;

	private bool canResumeTumbling = true;
	private bool doingFreeSpins;

	private const float BG_SLIDE_Y_DISTANCE = -1.3f;

/*================================================================================================*/

	protected override IEnumerator prespin()
	{	
		if (isGameUsingOptimizedFlattenedSymbols && visibleSymbolClone != null)
		{
			SlotReel[] reelArray = engine.getReelArray();

			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;

				for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
				{
					flattenSymbolWithRestore(
						visibleSymbolClone[reelIndex][reelArray[reelIndex].visibleSymbols.Length-symbolIndex-1]);
				}
			}
		}

		yield return StartCoroutine(base.prespin());
	}	

	private void flattenSymbolWithRestore(SlotSymbol symbol)
	{
		if (!symbol.isFlattenedSymbol)
		{
			Vector3 originalPosition = symbol.transform.position;
			
			symbol.mutateToFlattenedVersion(null, false, true, false);
			symbol.transform.position = originalPosition;
		}
	}

/*================================================================================================*/

	public override void showNonBonusOutcomes()
	{
		if (!canResumeTumbling)
		{
			return;
		}

		base.showNonBonusOutcomes();

	}	

	protected override IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJsonArray, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		while (!canResumeTumbling)
		{
			yield return null;
		}

		yield return StartCoroutine(base.tumbleAfterRollup(tumbleOutcomeJsonArray, basePayout, bonusPayout, rollupDelegate, doBigWin, bigWinDelegate, rollupStart, rollupEnd));
	}	

	protected override IEnumerator plopNewSymbols()
	{
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymnbols();
		}

		yield return StartCoroutine(base.plopNewSymbols());

	}

/*================================================================================================*/

	protected override void reelsStoppedCallback()
	{
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			unFlattenWinningSymnbols();
			unflattenBonusSymbols();
		}

		StartCoroutine(plopSymbols());
	}

	private void unFlattenWinningSymnbols()
	{
		HashSet<SlotSymbol> winningSymbols = outcomeDisplayController.getSetOfWinningSymbols(_outcome);

		foreach (SlotSymbol symbol in winningSymbols)
		{
			if (symbol.isMajor || symbol.isWildSymbol)
			{
				unFlattenWithRestore(symbol);
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

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				SlotSymbol symbol = visibleSymbols[symbolIndex];	

				if (symbol.isBonusSymbol && symbol.isFlattenedSymbol)
				{
					symbol.mutateTo(symbol.serverName, null, false, true);
				}
			}
		}
	}

/*================================================================================================*/

	protected override IEnumerator animateAllBonusSymbols()
	{
		canResumeTumbling = false;

		// wait in case we want the anticipation animations to finish before triggering the outcome animations
		yield return new TIWaitForSeconds(WAIT_BEFORE_BONUS_OUTCOME_ANIMS);

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

		yield return new TIWaitForSeconds(ANIMATION_WAIT_TIME);

		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) &&
		    BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
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

	private IEnumerator playBonusGameTransition(string transitionName)
	{		
		yield return StartCoroutine(fadeOutUIPanels());

		if (gearAnimator != null)
		{
			StartCoroutine(
				CommonAnimation.playAnimAndWait(
					gearAnimator, START_GEAR_ANIM_NAME));
		}

		activateReels(false);

		if (portalTransitionAnimator != null)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(PORTAL_TRANSITION_MUSIC_KEY));
			
			yield return StartCoroutine(
				CommonAnimation.playAnimAndWait(
					portalTransitionAnimator, PORTAL_TRANSITION_ANIM_NAME));
		}

		if (_outcome.isBonus)
		{
			createBonus();
		}
	}	

	private void slideOutUIPanels()
	{
		SpinPanel.instance.showFeatureUI(false);
		
		StartCoroutine(
			Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, TRANSITION_SLIDE_TIME, false));
		
		if (SpinPanel.instance != null)
		{
			float spinPanelSlideOutTime = BACKGROUND_SLIDE_TIME;
			
			if (SpinPanel.instance.backgroundWingsWidth != null)
			{
				float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
				spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
			}
			
			StartCoroutine(
				SpinPanel.instance.slideSpinPanelOut(
					SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));
		}	
	}

	private void restoreUIPanelPositions()
	{
		Overlay.instance.top.restorePosition();
		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
		SpinPanel.instance.showFeatureUI(true);
	}

	private IEnumerator fadeOutUIPanels()
	{
		StartCoroutine(Overlay.instance.fadeOut(FADE_TIME));
		StartCoroutine(SpinPanel.instance.fadeOut(FADE_TIME));

		yield return new TIWaitForSeconds(FADE_TIME);

		// at this point force the ui off so it doesn't update and try to render or change colors which might break the fade
		Overlay.instance.top.show(false);
		SpinPanel.instance.hidePanels();
	}

	private void restoreUIPanelsAlpha()
	{
		Overlay.instance.top.show(true);
		SpinPanel.instance.showPanel (SpinPanel.Type.NORMAL);

		Overlay.instance.fadeInNow();
		SpinPanel.instance.restoreAlpha();
	}

	protected void activateReels(bool isActive)
	{
		foreach (GameObject reelGo in reelGos)
		{
			if (reelGo != null)
			{
				reelGo.SetActive(isActive);
			}
		}

		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.gameObject != null)
				{
					symbol.gameObject.SetActive(isActive);
				}
			}
		}
	}
	
/*================================================================================================*/

	public override void doSpecialOnBonusGameEnd()
	{
		StartCoroutine(restoreBaseGame());
	}
	
	protected IEnumerator restoreBaseGame()
	{
		backgroundSlider.transform.localPosition = Vector3.zero;

		restoreUIPanelsAlpha();

		activateReels(true);
	
		base.doSpecialOnBonusGameEnd();	
		yield return null;

		canResumeTumbling = true;

		showNonBonusOutcomes();
		yield return null;
	} 		

/*================================================================================================*/
}
