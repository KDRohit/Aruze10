using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This class handles the Elvira02 sliding stuff.
 */
public class Bettie01 : SlidingSlotBaseGame 
{
	public GameObject gameBackground = null;					// Used in transition.
	public GameObject gameFrame = null;
	public GameObject foregroundReels = null;
	public GameObject reellessBackground = null;

	private Dictionary<Material, float> backgroundAlphaMap = null;
	private Dictionary<Material, float> foregroundAlphaMap = null;
	private bool isTransitionComplete = false;
	[SerializeField]  private ReelGameBackground backgroundScript;			// The script that holds onto the wings.
	private int numFinishedBonusSymbolAnims = 0;
	private int numStartedBonusSymbolAnims = 0;
	// Constant variables
	private const float TIME_TO_SLIDE_FOREGROUND = 1.0f;
	private const float TIME_FADE_OUT_SYMBOLS = 1.0f;
	private const float TRANSITION_SLIDE_TIME = 2.1f;
	private const float WING_EXPAND_TIME = TRANSITION_SLIDE_TIME * 0.75f;
	private const float REELLESS_BKG_START_X_POS = 0.5f;					// Position that the free spin background for the transition moves in from
	private const float REELLESS_BKG_END_X_POS = -0.5f;						// Position that the free spin background ends up in when the transition is complete

	private bool didFreeSpinTransition = false;

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject gets disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	private IEnumerator reelsStoppedCoroutine()
	{
		if (_outcome.isBonus)
		{
			// Check and see if it's the freespins bonus.
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
			{
				// Fade out the reelsymbols. 

				// Make sure all of the reels are stopped, or else everything may not fade out.

				for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
				{
					SlotReel[] reelArray = reelLayers[layerIndex].getReelArray();
					for (int i = 0; i < reelArray.Length; i++)
					{
						float time = 0.0f;	// Keep a timer here so the game doesn't stall out.
						while (!reelArray[i].isStopped)
						{
							yield return null;
							if (time > 0.5f)
							{
								break;
							}
							time += Time.deltaTime;
						}
						
					}
				}
				yield return StartCoroutine(playBonusAcquiredEffects());
				for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
				{
					SlotReel[] reelArray = reelLayers[layerIndex].getReelArray();
					for (int i = 0; i < reelArray.Length; i++)
					{
						foreach (SlotSymbol slotSymbol in reelArray[i].symbolList)
						{
							if (slotSymbol.isBonusSymbol)
							{
								slotSymbol.mutateToFlattenedVersion(null, true, true);
							}
						}
					}
				}

				yield return StartCoroutine(doFreeSpinsTransition());
			}
		}

		base.reelsStoppedCallback();
	}

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		// Clean up the game
		if (didFreeSpinTransition)
		{
			for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
			{
				SlotReel[] reelArray = reelLayers[layerIndex].getReelArray();

				for (int i = 0; i < reelArray.Length; i++)
				{
					foreach (SlotSymbol slotSymbol in reelArray[i].symbolList)
					{
						if (slotSymbol.animator != null)
						{
							RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolInOverTime(0.0f));
						}
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

			// put the sliding reelless background back where it should start
			slideBackgrounds(0);

			if (backgroundAlphaMap != null)
			{
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(reellessBackground.transform.parent.gameObject, backgroundAlphaMap);
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(foregroundReels.gameObject, foregroundAlphaMap);
			}

			if (_outcome.isGifting)
			{
				// put the top bar back where it came from and turn the UIAnchors back on
				Overlay.instance.top.restorePosition();
			}

			didFreeSpinTransition = false;
		}
	}

	public override IEnumerator playBonusAcquiredEffects()
	{
		if (outcome.isBonus)
		{
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) && BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null)
			{
				Audio.play("SymbolBonusBN1SurfBettie01");
			}
			else
			{
				Audio.play("SymbolBonusBN2CameraBettie01");
			}
			//Audio.play(Audio.soundMap("bonus_symbol_animate"));
			yield return StartCoroutine(playBNAnimation());
		}
		SlotReel[] reelArray = engine.getReelArray();
		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol slotSymbol in engine.getVisibleSymbolsAt(reelID))
			{
				if (findSymbolInfo(slotSymbol.name + "_Acquired") != null)
				{
					slotSymbol.mutateTo(slotSymbol.name + "_Acquired", null, true, true);
				}
			}
		}
		if (outcome.isBonus)
		{
			yield return StartCoroutine(playBNAnimation());
			isBonusOutcomePlayed = true;
		}
	}

	private void onBonusSymbolAnimationDone(SlotSymbol sender)
	{
		numFinishedBonusSymbolAnims++;
	}

	private IEnumerator playBNAnimation()
	{
		numFinishedBonusSymbolAnims = 0;
		numStartedBonusSymbolAnims = 0;
		foreach (SlotReel reel in reelLayers[1].getReelArray())
		{
			numStartedBonusSymbolAnims++;
			reel.animateBonusSymbols(onBonusSymbolAnimationDone);
		}
		while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
		{
			yield return null;
		}
	}

	private IEnumerator doFreeSpinsTransition()
	{
		didFreeSpinTransition = true;

		Audio.switchMusicKey("FreespinBettie01");
		Audio.playMusic("IntroFreeSpinBettie01", 0.0f);
		// Get the alpha map for all of the objects
		List<GameObject> ignoreFade = new List<GameObject>();
		ignoreFade.Add(reellessBackground);
		// Animate the top to the right.
		backgroundAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(reellessBackground.transform.parent.gameObject);
		foregroundAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(foregroundReels.gameObject);
		
		// Fade out the symbols
		for (int layerIndex = 0; layerIndex < reelLayers.Length; layerIndex++)
		{
			SlotReel[] reelArray = reelLayers[layerIndex].getReelArray();

			for (int i = 0; i < reelArray.Length; i++)
			{
				foreach (SlotSymbol slotSymbol in reelArray[i].symbolList)
				{
					if (slotSymbol.animator != null)
					{
						RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolOutOverTime(TIME_FADE_OUT_SYMBOLS));
					}
				}
			}
		}

		// Fade out the rest of the game objects.
		float elapsedTime = 0;
		while (elapsedTime < TIME_FADE_OUT_SYMBOLS)
		{
			elapsedTime += Time.deltaTime;
			CommonGameObject.alphaGameObject(reellessBackground.transform.parent.gameObject, 1 - (elapsedTime / TIME_FADE_OUT_SYMBOLS), ignoreFade);
			CommonGameObject.alphaGameObject(foregroundReels.gameObject, 1 - (elapsedTime / TIME_FADE_OUT_SYMBOLS), ignoreFade);
			yield return null;
		}

		CommonGameObject.alphaGameObject(reellessBackground.transform.parent.gameObject, 0.0f, ignoreFade);
		CommonGameObject.alphaGameObject(foregroundReels.gameObject, 0.0f, ignoreFade);

		// Slide in the freespins game.
		isTransitionComplete = false;
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideBackgrounds", "oncomplete", "onBackgroundSlideComplete"));
		RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Left, TRANSITION_SLIDE_TIME, false));

		// Get the wings and expand them to fill out the gaps.
		if (backgroundScript != null)
		{
			StartCoroutine(backgroundScript.tweenWingsTo(ReelGameBackground.WingTypeOverrideEnum.Freespins, WING_EXPAND_TIME, iTween.EaseType.linear));
		}

		while (!isTransitionComplete)
		{
			yield return null;
		}
	}



	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideBackgrounds(float slideAmount)
	{
		// Move the Reelless background
		float targetReellessBkgXPos = ((REELLESS_BKG_END_X_POS - REELLESS_BKG_START_X_POS) * slideAmount) + REELLESS_BKG_START_X_POS;
		Vector3 currentReellessBkgPos = reellessBackground.transform.localPosition;
		reellessBackground.transform.localPosition = new Vector3(targetReellessBkgXPos, currentReellessBkgPos.y, currentReellessBkgPos.z);
	}

	public void onBackgroundSlideComplete()
	{
		Vector3 currentReellessBkgPos = reellessBackground.transform.localPosition;
		reellessBackground.transform.localPosition = new Vector3(REELLESS_BKG_END_X_POS, currentReellessBkgPos.y, currentReellessBkgPos.z);

		isTransitionComplete = true;
	}
}
