using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles a transition to a bonus game where the background slides in some direction

@todo : May not implement all possible slide types, if this isn't exactly what you need consider adding a way to add in support for the type of slide you want to do

NOTE - This module ONLY works on ReelGames that derive from SlotBaseGame!

Original Author: Scott Lepthien
*/
public class DeprecatedBonusGameSlideTransitionModule : SlotModule 
{
	[SerializeField] private GameObject foregroundReels;
	[SerializeField] private GameObject reellessBackground;
	[SerializeField] private SlideForBonusTypeEnum slideForType = SlideForBonusTypeEnum.FREESPINS;
	[SerializeField] private ReelGameBackground backgroundScript;					// The script that holds onto the wings.
	[SerializeField] private Vector3 reellessBkgStartPos;					// Position that the free spin background for the transition moves in from
	[SerializeField] private Vector3 reellessBkgEndPos;						// Position that the free spin background ends up in when the transition is complete
	[SerializeField] private bool SHOULD_HIDE_ALL_UI_DURING_TRANSITION;
	[SerializeField] private float TIME_FADE_OUT_SYMBOLS;
	[SerializeField] private float TRANSITION_SLIDE_TIME;
	[SerializeField] private float PRE_SLIDE_WAIT = 1.5f;
	[SerializeField] private bool fadeWings = true;
	[SerializeField] private bool shouldFadeTopOverlay;
	[SerializeField] private bool shouldFadeSpinPanel;
	[SerializeField] private float WING_EXPAND_TIME;
	[SerializeField] private ReelGameBackground.WingTypeOverrideEnum wingSizeToTweenTo = ReelGameBackground.WingTypeOverrideEnum.Freespins; // Defaults to freespins because that's what it was doing before this was added.
	[SerializeField] private bool shouldFadeSymbols = false;				// Due to Animators cuasing issues with animating color values, many games don't support fading of symbols without swapping in non-animated versions
	[SerializeField] private bool shouldSlideSpinPanel = false;
	[SerializeField] private List<GameObject> ObjectsToDeactivate;				// Objects to deactivate during the transition (eg: ambient effect)


	private Dictionary<Material, float> backgroundAlphaMap;
	private Dictionary<Material, float> foregroundAlphaMap;
	private bool isTransitionComplete = false;

	private const float REEL_STOP_WAIT_TIMEOUT = 0.5f;										// If waiting on the reels to stop is taking too long, don't lock the game up
	private const string FREESPIN_TRANSITION_SOUND_KEY = "bonus_freespin_wipe_transition";
	private const string FREESPIN_MUSIC_SOUND_KEY = "freespin";

	private enum SlideForBonusTypeEnum
	{
		FREESPINS 	= 0,
		PICKEM 		= 1,
		BOTH		= 2,
		PORTAL		= 3,
	}

// executeOnPreBonusGameCreated() section
// functions here are called by the SLotBaseGame reelGameReelsStoppedCoroutine() function
// used to handle delays (like transitions) before the bonus game is created, otherwise you will end up with both games showing up at the same time
	public override bool needsToExecuteOnPreBonusGameCreated()
	{
		return true;
	}

	public override IEnumerator executeOnPreBonusGameCreated()
	{
		if (reelGame.outcome.isBonus)
		{
			// Check and see if it's the freespins bonus.
			if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING) 
				&& BonusGameManager.instance.outcomes[BonusGameType.GIFTING] != null 
				&& (slideForType == SlideForBonusTypeEnum.FREESPINS || slideForType == SlideForBonusTypeEnum.BOTH))
			{

				yield return StartCoroutine(startTransition());
			}
			else if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE) 
				&& BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] != null 
				&& (slideForType == SlideForBonusTypeEnum.PICKEM || slideForType == SlideForBonusTypeEnum.BOTH))
			{
				yield return StartCoroutine(startTransition());
			}
			else if(SlotBaseGame.instance.getBonusOutcome().isPortal
			        && slideForType == SlideForBonusTypeEnum.PORTAL)
			{
				yield return StartCoroutine(startTransition());
			}
			else
			{
				yield break;
			}
		}
	}

	/// Making this a seperate function so we can reduce the copied code in here
	private IEnumerator startTransition()
	{
		// Fade out the reelsymbols.
		// Make sure all of the reels are stopped, or else everything may not fade out.
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			float time = 0.0f;	// Keep a timer here so the game doesn't stall out and lock up
			while (!reel.isStopped)
			{
				yield return null;
				if (time > REEL_STOP_WAIT_TIMEOUT)
				{
					break;
				}
				time += Time.deltaTime;
			}
		}
		// handle playing this early, so that it happens before the transition starts
		yield return StartCoroutine(((SlotBaseGame)reelGame).doPlayBonusAcquiredEffects());
		if (shouldFadeSpinPanel)
		{
			StartCoroutine(SpinPanel.instance.fadeOut(TIME_FADE_OUT_SYMBOLS));
		}
		yield return StartCoroutine(doSlideTransition());
		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.hidePanels();
			SpinPanel.instance.restoreAlpha();
		}
	}

	private IEnumerator doSlideTransition()
	{
		yield return new TIWaitForSeconds(PRE_SLIDE_WAIT);

		if (SHOULD_HIDE_ALL_UI_DURING_TRANSITION)
		{
			SpinPanel.instance.showSideInfo(false);
			SpinPanel.instance.showFeatureUI(false);
		}

		//@todo : Handle possible starting of audio keys for other bonus types
		if (slideForType == SlideForBonusTypeEnum.FREESPINS)
		{
			Audio.play(Audio.soundMap(FREESPIN_TRANSITION_SOUND_KEY));
			Audio.switchMusicKey(Audio.soundMap(FREESPIN_MUSIC_SOUND_KEY));
		}

		foreach (GameObject objectToDeactivate in ObjectsToDeactivate) 
		{
			objectToDeactivate.SetActive(false);
		}
		// Get the alpha map for all of the objects
		List<GameObject> ignoreFade = new List<GameObject>();
		ignoreFade.Add(reellessBackground);

		//For some portals we wont want to fade the wings
		if (!fadeWings)
		{
			if (backgroundScript != null && backgroundScript.wings != null)
			{
				ignoreFade.Add(backgroundScript.wings.leftMeshFilter.gameObject);
				ignoreFade.Add(backgroundScript.wings.rightMeshFilter.gameObject);
			}
		}

		TICoroutine hideTopOverlayCoroutine = null;
		if (shouldFadeTopOverlay)
		{
			hideTopOverlayCoroutine = RoutineRunner.instance.StartCoroutine(Overlay.instance.fadeOut(TIME_FADE_OUT_SYMBOLS));
		}
		else
		{
			hideTopOverlayCoroutine = RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Left, TRANSITION_SLIDE_TIME, true));
			Overlay.instance.jackpotMystery.hide();
		}

		TICoroutine fadeSpinPanelCoroutine = null;
		if (shouldFadeSpinPanel)
		{
			fadeSpinPanelCoroutine = StartCoroutine(SpinPanel.instance.fadeOut(TIME_FADE_OUT_SYMBOLS));
		}

		if (shouldSlideSpinPanel)
		{
			RoutineRunner.instance.StartCoroutine (SpinPanel.instance.slideSpinPanelOut (SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Left, TRANSITION_SLIDE_TIME, false));
		}
		SpinPanel.instance.showFeatureUI(false);

		backgroundAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(reellessBackground.transform.parent.gameObject);

		// not all games using this module will have foreground reels
		if (foregroundReels != null)
		{
			foregroundAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(foregroundReels.gameObject);
		}
		
		// Fade out the symbols
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			foreach (SlotSymbol slotSymbol in reel.symbolList)
			{
				if (slotSymbol.animator != null)
				{
					if (shouldFadeSymbols)
					{
						if(!slotSymbol.isFlattenedSymbol)
						{
							slotSymbol.mutateToFlattenedVersion();
						}
						RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolOutOverTime(TIME_FADE_OUT_SYMBOLS));
					}
					else
					{
						slotSymbol.gameObject.SetActive(false);
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

			if (foregroundReels != null)
			{
				CommonGameObject.alphaGameObject(foregroundReels.gameObject, 1 - (elapsedTime / TIME_FADE_OUT_SYMBOLS), ignoreFade);
			}
			yield return null;
		}

		// Wait for our fade coroutines to finish so that when we restore them so
		// they don't have any extra frames to keep fading out on us. 
		if (hideTopOverlayCoroutine != null && !hideTopOverlayCoroutine.isFinished)
		{
			yield return hideTopOverlayCoroutine;
		}
		
		if (fadeSpinPanelCoroutine != null && !fadeSpinPanelCoroutine.isFinished)
		{
			yield return fadeSpinPanelCoroutine;
		}

		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(false);
			Overlay.instance.fadeInNow();
		}
		else
		{
			// Hide and restore the position, so that if the freespins reel
			// game tries to auto size, the overlay is in the correct spot
			Overlay.instance.top.show(false);
			Overlay.instance.top.restorePosition();
		}

		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.hidePanels();
			SpinPanel.instance.restoreAlpha();
		}

		CommonGameObject.alphaGameObject(reellessBackground.transform.parent.gameObject, 0.0f, ignoreFade);
		
		if (foregroundReels != null)
		{
			CommonGameObject.alphaGameObject(foregroundReels.gameObject, 0.0f, ignoreFade);
		}

		// Side the BG over to the right.

		// Move over the distance of the BG without the reels.

		// Slide in the freespins game.
		isTransitionComplete = false;
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideBackgrounds", "oncomplete", "onBackgroundSlideComplete"));
	
		// Get the wings and expand them to fill out the gaps.
		if (backgroundScript != null)
		{
			StartCoroutine(backgroundScript.tweenWingsTo(wingSizeToTweenTo, WING_EXPAND_TIME, iTween.EaseType.linear));
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
		Vector3 targetReellessBkgPos = ((reellessBkgEndPos - reellessBkgStartPos) * slideAmount) + reellessBkgStartPos;
		reellessBackground.transform.localPosition = targetReellessBkgPos;
	}

	public void onBackgroundSlideComplete()
	{
		//Vector3 currentReellessBkgPos = reellessBackground.transform.localPosition;
		reellessBackground.transform.localPosition = reellessBkgEndPos;

		isTransitionComplete = true;
	}

// executeOnBonusGameEnded() section
// functions here are called by the SlotBaseGame onBonusGameEnded() function
// usually used for reseting transition stuff
	public override bool needsToExecuteOnBonusGameEnded()
	{
		return true;
	}

	public override IEnumerator executeOnBonusGameEnded()
	{
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			foreach (SlotSymbol slotSymbol in reel.symbolList)
			{
				if (slotSymbol.animator != null)
				{
					if (shouldFadeSymbols)
					{
						RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolInOverTime(0.0f));
					}
					else
					{
						slotSymbol.gameObject.SetActive(true);
					}
				}
			}
		}

		foreach (GameObject objectToDeactivate in ObjectsToDeactivate) 
		{
			objectToDeactivate.SetActive(true);
		}

		if (backgroundScript != null)
		{
			// Routine Runner needs to run this because the gameobject gets disabled before this finishes.
			backgroundScript.setWingsTo(backgroundScript.wingType);
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

			if (foregroundReels != null)
			{
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(foregroundReels.gameObject, foregroundAlphaMap);
			}
		}

		// put the top bar back where it came from and turn the UIAnchors back on
		if ((reelGame.outcome.isGifting && (slideForType == SlideForBonusTypeEnum.FREESPINS || slideForType == SlideForBonusTypeEnum.BOTH || slideForType == SlideForBonusTypeEnum.PORTAL))
			|| (reelGame.outcome.isChallenge && (slideForType == SlideForBonusTypeEnum.PICKEM || slideForType == SlideForBonusTypeEnum.BOTH || slideForType == SlideForBonusTypeEnum.PORTAL))
			|| (reelGame.outcome.isBonus && (slideForType == SlideForBonusTypeEnum.PICKEM || slideForType == SlideForBonusTypeEnum.BOTH || slideForType == SlideForBonusTypeEnum.PORTAL)))
		{
			Overlay.instance.top.restorePosition();
			if (shouldSlideSpinPanel)
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
			}
			SpinPanel.instance.showFeatureUI(true);
		}

		yield break;
	}
}
