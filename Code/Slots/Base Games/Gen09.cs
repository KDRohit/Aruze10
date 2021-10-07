using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base class for Gen09. A basic Tumble Slot game.
*/

public class Gen09 : TumbleSlotBaseGame 
{
	[SerializeField] private GameObject freeSpinTransitionPrefab = null;
	[SerializeField] private GameObject backgroundSlider = null;					// The background slider.
	[SerializeField] private ReelGameBackground backgroundScript;					// The script that holds onto the wings.
	
	// movement constants
	private const float BG_SLIDE_Y_DISTANCE = -1.3f;

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
		yield return new TIWaitForSeconds(ANIMATION_WAIT_TIME);

		bool didTransition = false;

		// Check and see if it's the freespins bonus.
		if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
		{
			// Fade out the reelsymbols. 
			didTransition = true;
			if (freeSpinTransitionPrefab != null)
			{
				freeSpinTransitionPrefab.SetActive(true);
				Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_FREESPIN_PT1));
				Audio.play(Audio.soundMap(FREESPIN_VO), 1.0f, 0, 0.6f);
				yield return new TIWaitForSeconds(TRANSITION_WAIT_TIME_1);
				Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_FREESPIN_PT2));
				yield return new TIWaitForSeconds(TRANSITION_WAIT_TIME_2);
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
		Audio.switchMusicKeyImmediate(Audio.soundMap(TRANSITION_SOUND));
		// Turn off all of the symbols
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				// Turn off all of the symbols.
				symbol.animator.gameObject.SetActive(false);
			}
		}

		if (backgroundScript != null)
		{
			BonusGameManager.instance.wings.show();
		}
		yield return StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, TRANSITION_SLIDE_TIME, false));

		// Move the backgrounds down.
		if (backgroundSlider != null)
		{
			if (SpinPanel.instance != null)
			{
				// Hide the side info
				SpinPanel.instance.showSideInfo(false);
				SpinPanel.instance.showFeatureUI(false);

				// hide the jackpot/mystery panel before the slide (it should be brought back when the Spin Panel is enabled in the base game again)
				if (Overlay.instance != null)
				{
					Overlay.instance.jackpotMystery.hide();
				}

				float spinPanelSlideOutTime = BACKGROUND_SLIDE_TIME;
				if (SpinPanel.instance.backgroundWingsWidth != null)
				{
					float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
					spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
				}
				StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));
			}
			iTween.MoveTo(backgroundSlider, iTween.Hash("y", BG_SLIDE_Y_DISTANCE, "time", BACKGROUND_SLIDE_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
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
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				// Turn on all of the symbols.
				symbol.animator.gameObject.SetActive(true);
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

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		if (SpinPanel.instance != null)
		{
			// restore the side panel info
			SpinPanel.instance.showSideInfo(showSideInfo);
			SpinPanel.instance.showFeatureUI(true);
		}
	}

}
