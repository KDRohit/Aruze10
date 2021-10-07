using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base class for Osa06. A basic Tumble Slot game.
*/

public class Osa06 : TumbleSlotBaseGame 
{
	[SerializeField] private GameObject freeSpinTransitionPrefab = null;
	[SerializeField] private GameObject backgroundSlider = null;					// The background slider.
	[SerializeField]  private ReelGameBackground backgroundScript;					// The script that holds onto the wings.

	// timing constants
	//private const float TIME_BETWEEN_REMOVALS = 0.05f;

	protected override IEnumerator animateAllBonusSymbols()
	{
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
		yield return new TIWaitForSeconds(1.5f);

		bool didTransition = false;
		// Check and see if it's the freespins bonus.
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
		{
			// Fade out the reelsymbols. 
			didTransition = true;
			if (freeSpinTransitionPrefab != null)
			{
				freeSpinTransitionPrefab.SetActive(true);
				Audio.play("Transition2FreespinPt1HauntedForest", 1.0f, 0, 0);
				Audio.play("FreespinIntroVOHauntedForest", 1.0f, 0, 0.6f);
				Audio.play("Transition2FreespinPt2HauntedForest", 1.0f, 0, 3.0f);
				yield return new TIWaitForSeconds(4.4f);
			}
		}
		else
		{
			yield return StartCoroutine(doWheelTransition());
		}

		if (didTransition)
		{
			// Clean up the prefab.
			if (freeSpinTransitionPrefab != null)
			{
				freeSpinTransitionPrefab.SetActive(false);
			}
		}
	}

	private IEnumerator doWheelTransition()
	{
		Audio.playMusic(Audio.soundMap(TRANSITION_SOUND));
		// Turn off all of the symbols
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			//SlotSymbol[] visibleSymbols = visibleSymbolClone[reelID];
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.gameObject != null)
				{
					// Turn off all of the symbols.
					symbol.gameObject.SetActive(false);
				}
			}
		}

		// Slide out the top bar and expand the wings into freespin size.
		if (backgroundScript != null)
		{
			BonusGameManager.instance.wings.show();
			//StartCoroutine(backgroundScript.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Freespins, WING_EXPAND_TIME, iTween.EaseType.linear));
		}
		yield return StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, TRANSITION_SLIDE_TIME, false));

		// Move the backgrounds down.
		if (backgroundSlider != null)
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
			iTween.MoveTo(backgroundSlider, iTween.Hash("y", -1.3, "time", BACKGROUND_SLIDE_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
			yield return new TIWaitForSeconds(BACKGROUND_SLIDE_TIME);
		}
	}

	public override void goIntoBonus()
	{
		base.goIntoBonus();

		// Now put the base game back in a good state to return to
		
		// reset everything back to normal.
		backgroundSlider.transform.localPosition = Vector3.zero;
		// Turn on all of the symbols
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			//SlotSymbol[] visibleSymbols = visibleSymbolClone[reelID];
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.gameObject != null)
				{
					// Turn off all of the symbols.
					symbol.animator.gameObject.SetActive(true);
				}
			}
		}

		if (backgroundScript != null)
		{
			// Routine Runner needs to run this because the gameobject gets disabled before this finishes.
			backgroundScript.setWingsTo(ReelGameBackground.WingTypeOverrideEnum.Basegame);
		}

		Overlay.instance.top.restorePosition();
		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
	}

	// We get some weird anticipation information in tumble games so we need to do this.
	public override IEnumerator playBonusAcquiredEffects()
	{
		if (outcome.isBonus)
		{
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
			{
				Audio.play("SymbolBonusWheelHauntedForest");
			}
			else
			{
				Audio.play("SymbolBonusWheelHauntedForest");
			}
			yield return StartCoroutine(animateAllBonusSymbols());
			isBonusOutcomePlayed = true;
		}
	}
}
