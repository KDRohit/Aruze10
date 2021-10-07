using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
The osa02 base game class.
*/
public class Osa02 : SlotBaseGame
{
	public GameObject symbolCamera;

	[SerializeField] private GameObject backgrounds;
	[SerializeField] private GameObject challengeGameObjects;
	[SerializeField] private GameObject freespinObjects;
	[SerializeField] private ReelGameBackground backgroundScript;			// The script that holds onto the wings.
	private bool isSlideComplete = false;
	// Timing
	private const float TIME_FADE_OUT_SYMBOLS = 0.5f;
	private const float TIME_FADE_OUT_OVERLAY = 0.5f;
	private const float TRANSITION_SLIDE_TIME = 2.5f;
	private const float POST_FADE_WAIT_TIME = 0.25f;
	private const float BACKGROUNDS_START_POS = 0.0f;
	private const float BACKGROUNDS_END_POS = -1.3f;

	public override void goIntoBonus()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject gets disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(goIntoBonusCoroutine());
	}

	private IEnumerator goIntoBonusCoroutine()
	{
		bool didTransition = false;
		SpinPanel.instance.showSideInfo(false);
		if (_outcome.isBonus)
		{
			// If we get into a bonus game we want to pan the camera up.
			didTransition = true;

			// Fade out the symbols
			SlotReel[] reelArray = engine.getReelArray();

			foreach (SlotReel reel in reelArray)
			{
				foreach (SlotSymbol slotSymbol in reel.symbolList)
				{
					if (slotSymbol.animator != null)
					{
						RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolOutOverTime(TIME_FADE_OUT_SYMBOLS));
					}
				}
			}

			if (backgroundScript != null)
			{
				StartCoroutine(backgroundScript.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Freespins, TIME_FADE_OUT_OVERLAY, iTween.EaseType.linear));
			}

			// Fade out the Overlay.
			if (Overlay.instance != null)
			{
				yield return StartCoroutine(Overlay.instance.fadeOut(TIME_FADE_OUT_OVERLAY));
			}

			// Toggle on the right objects based off the game type
			if (_outcome.isChallenge && challengeGameObjects != null)
			{
				Audio.play("IdleBonusOSA02");
				challengeGameObjects.SetActive(true);
			}
			else if (_outcome.isGifting && freespinObjects != null)
			{
				Audio.switchMusicKeyImmediate("PickACrowBG");
				freespinObjects.SetActive(true);
			}

			symbolCamera.SetActive(false);

			// Adding a slight wait before sliding upward so its obvious that we've faded everything out by now.
			yield return new TIWaitForSeconds(POST_FADE_WAIT_TIME);

			// Slide the game down with the Spin panel.
			if (backgrounds != null)
			{
				// Background
				isSlideComplete = false;
				iTween.ValueTo(gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideBackgrounds", "oncomplete", "onBackgroundSlideComplete"));


				if (SpinPanel.instance != null)
				{
					float spinPanelSlideOutTime = TRANSITION_SLIDE_TIME;
					if (SpinPanel.instance.backgroundWingsWidth != null)
					{
						float spinPanelBackgroundHeight = SpinPanel.instance.backgroundWingsWidth.localScale.y;
						spinPanelSlideOutTime *= spinPanelBackgroundHeight / NGUIExt.effectiveScreenHeight;
					}
					StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, spinPanelSlideOutTime, false));
					if (backgroundScript != null)
					{
						StartCoroutine(backgroundScript.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Fullscreen, spinPanelSlideOutTime, iTween.EaseType.linear));
					}
				}
			}

			while (!isSlideComplete)
			{
				// Wait for the slide to finish.
				yield return null;
			}
			// Load the bonus game.
		}
		base.goIntoBonus();

		if (didTransition)
		{
			// Wait for the free spins  or challenge game to finish loading before cleaning up this transition.
			while (FreeSpinGame.instance == null && ChallengeGame.instance == null)
			{
				yield return null;
			}
			// Give the free spin game one frame to get everything set up.
			yield return null;

			// Clean up the game
			SlotReel[] reelArray = engine.getReelArray();

			foreach (SlotReel reel in reelArray)
			{
				foreach (SlotSymbol slotSymbol in reel.symbolList)
				{
					if (slotSymbol.animator != null)
					{
						RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolInOverTime(0.0f));
					}
				}
			}

			if (backgroundScript != null)
			{
				// Routine Runner needs to run this because the gameobject gets disabled before this finishes.
				backgroundScript.setWingsTo(ReelGameBackground.WingTypeOverrideEnum.Basegame);
			}
			else
			{
				Debug.LogError("Didn't find the reelbackground.");
			}

			if (Overlay.instance != null)
			{
				Overlay.instance.fadeInNow();
			}

			// Toggle the objects back off.
			if (challengeGameObjects != null)
			{
				challengeGameObjects.SetActive(false);
			}
			if (freespinObjects != null)
			{
				freespinObjects.SetActive(false);
			}

			// Put the background back to default.
			slideBackgrounds(0);

			symbolCamera.SetActive(true);

			// Fix the SpinPanel.
			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
			}
		}
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideBackgrounds(float slideAmount)
	{
		if (backgrounds != null)
		{
			// Move the Reelless background
			float targetReellessBkgYPos = ((BACKGROUNDS_END_POS - BACKGROUNDS_START_POS) * slideAmount) + BACKGROUNDS_START_POS;
			Vector3 currentBkgPos = backgrounds.transform.localPosition;
			backgrounds.transform.localPosition = new Vector3(currentBkgPos.x, targetReellessBkgYPos, currentBkgPos.z);
		}
	}

	// Makes sure that we get the background set to where we want it.
	public void onBackgroundSlideComplete()
	{
		isSlideComplete = true;
		slideBackgrounds(1);
	}
}
