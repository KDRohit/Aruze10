using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/**
Handles a transition to a bonus game that involves a just fading, kind of a dumbed down version of BonusGameAnimatedTransition

Original Author: Scott Lepthien
*/
public class BonusGameFadeTransition : BaseTransitionModule
{
	[SerializeField] private bool shouldFadeSymbols = true;
	[SerializeField] private bool shouldFadeWings;
	[SerializeField] private bool shouldFadeTopOverlay;
	[SerializeField] private bool shouldFadeSpinPanel;
	[SerializeField] private float FADE_TIME;
	[SerializeField] private List<GameObject> objectsToFade;
	[SerializeField] private List<GameObject> objectsToDeactivate;
	[SerializeField] private List<GameObject> objectsToDeactivateImmediately;
	[SerializeField] AnimationListController.AnimationInformationList objectsToAnimate; // Simple animations like hiding an object instead of fading or deactivating.
	[SerializeField] private float PRE_GO_INTO_BONUS_DELAY;
	[SerializeField] private float POST_GO_INTO_BONUS_DELAY;
	[SerializeField] private bool SHOULD_SLIDE_OUT_OVERLAY;
	[SerializeField] private bool SHOULD_SLIDE_OUT_OVERLAY_WITH_ANIMAITON;
	[SerializeField] private bool SHOULD_SLIDE_OUT_SPIN_PANEL_WITH_ANIMAITON;
	[SerializeField] private bool SHOULD_HIDE_ALL_UI_DURING_TRANSITION;
	[SerializeField] private GameObject transitionCamera = null;
	[SerializeField] private float PRE_GO_INTO_BIG_WIN_DELAY;
	[SerializeField] private GameObject backgroundMover; //Used to we can put the transition object in the correct position when transitioning back so there isn't a quick flash effect of it resetting itself
	[SerializeField] protected Vector3 transitionBackStartingPosition; //position that we want the background mover to start in when transitioning back to the base game

	/// What kind of Bonues to apply the transitions to
	protected enum TransitionBackBonusType
	{
		FREESPINS 	= 0,
		PICKEM 		= 1,
		BOTH		= 2,
		PORTAL		= 3
	}

	private const string TRANSITION_FROM_PICKEM_KEY = "transition_from_challenge";
	private const string TRANSITION_FROM_FREESPINS_KEY = "transition_from_freespins";
	private Transform baseGame = null;
	private int startingObjectsToFadeCount;
	private ReelGameBackground.WingTypeOverrideEnum originalWingType;

	//Constants used for tweening wings during a transition
	private const float CHALLENGE_SCALE_X = 11.6f;
	private const float CHALLENGE_SCALE_Y = 8.7f;
	private const float CHALLENGE_POS_Y = 0.04f;

	void Start()
	{
		if (objectsToFade.Count > 0)
		{
			startingObjectsToFadeCount = objectsToFade.Count;
		}
		if (background != null)
		{
			originalWingType = background.wingType;
		}
	}

	protected override IEnumerator doTransition()
	{
		SlotBaseGame.instance.createBonus();
		
		playTransitionSounds();

		if (shouldFadeSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
			{
				List<SlotSymbol> symbolList = reel.symbolList;
				foreach (SlotSymbol symbol in symbolList)
				{	
					if (symbol.animator != null)
					{
						if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
						{
							
							symbol.mutateToFlattenedVersion();
						}
						objectsToFade.Add(symbol.animator.gameObject);
					}
				}
			}
		}

		TICoroutine fadeBackgroundsCorotuine = StartCoroutine(fadeOutBackgrounds());

		TICoroutine overlaySlideCoroutine = null;
		if (SHOULD_SLIDE_OUT_OVERLAY_WITH_ANIMAITON)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			overlaySlideCoroutine = RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, OVERLAY_TRANSITION_SLIDE_TIME, false));
			// hide the jackpot/mystery panel before the slide (it should be brought back when the Spin Panel is enabled in the base game again)
			Overlay.instance.jackpotMystery.hide();
		}

		TICoroutine spinPanelSlideCoroutine = null;
		if (SHOULD_SLIDE_OUT_SPIN_PANEL_WITH_ANIMAITON)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			spinPanelSlideCoroutine = RoutineRunner.instance.StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, OVERLAY_TRANSITION_SLIDE_TIME, false));
			SpinPanel.instance.showFeatureUI(false);
		}

		if (SHOULD_HIDE_ALL_UI_DURING_TRANSITION)
		{
			SpinPanel.instance.showSideInfo(false);
			SpinPanel.instance.showFeatureUI(false);
			RoyalRushCollectionModule.showTopMeter(false);
		}

		yield return new TIWaitForSeconds(FADE_TIME);
		
		// ensure that the background fade finished before we proceed
		while (fadeBackgroundsCorotuine != null && !fadeBackgroundsCorotuine.finished)
		{
			yield return null;
		}
		
		// we also need to make sure if we are sliding the overlay or 
		// spin panel that those are indeed finished before proceeding
		// because they don't actually block when they are called
		while (overlaySlideCoroutine != null && !overlaySlideCoroutine.finished)
		{
			yield return null;
		}

		while (spinPanelSlideCoroutine != null && !spinPanelSlideCoroutine.finished)
		{
			yield return null;
		}

		if (PRE_GO_INTO_BONUS_DELAY > 0)
		{
			yield return new TIWaitForSeconds(PRE_GO_INTO_BONUS_DELAY);
		}
		
		if (reelGame is LayeredMultiSlotBaseGame)
		{
			SlotBaseGame.instance.startBonus(); //This will remove the outcome from the layeredBonusOutcomes list and show the bonus
		}
		else
		{
			BonusGameManager.instance.show(null, true);
		}
		startBonusGameNonModuleTransition();
		if (SHOULD_SLIDE_OUT_OVERLAY)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, OVERLAY_TRANSITION_SLIDE_TIME, false));
			// hide the jackpot/mystery panel before the slide (it should be brought back when the Spin Panel is enabled in the base game again)
			Overlay.instance.jackpotMystery.hide();
		}
		if (SHOULD_TWEEN_WINGS_UP && !shouldFadeSpinPanel)
		{
			yield return StartCoroutine (tweenWingsUp ());
		}

		if (POST_GO_INTO_BONUS_DELAY > 0)
		{
			yield return new TIWaitForSeconds(POST_GO_INTO_BONUS_DELAY);
		}

		if (SHOULD_TWEEN_WINGS_UP && !shouldFadeSpinPanel)
		{
			resetWings();
		}

		BonusGameManager.instance.startTransitionedBonusGame();
		BonusGameManager.currentBaseGame.gameObject.SetActive(false);
	}

	protected IEnumerator fadeOutBackgrounds()
	{
		float elapsedTime = 0;
		if (shouldFadeWings)
		{
			objectsToFade.Add(background.wings.gameObject);
		}

		if (shouldFadeTopOverlay)
		{
			// Just hide the jackpot/mystery bar right away, since in some cases, like
			// VIP Revamp it has animated elements that will not fade nicely.
			Overlay.instance.hideJackpotMystery();
			StartCoroutine(Overlay.instance.fadeOut(FADE_TIME));
		}

		if (shouldFadeSpinPanel)
		{
			StartCoroutine(SpinPanel.instance.fadeOut(FADE_TIME));
		}

		if (shouldFadeSpinPanel && shouldFadeTopOverlay && background != null && background.wingType != ReelGameBackground.WingTypeOverrideEnum.Fullscreen)
		{
			//Only need to tweent he wings if they're aren't set to Challenge, which will already be filling the screen.
			//Need to handle tweening wings in here since the bonus game won't been instantied until after the transitiion and we want to cover the space created by fading out the spin panel
			baseWingsCamera.depth = TWEEN_WINGS_CAMERA_DEPTH;
			CommonGameObject.setLayerRecursively(background.wings.gameObject, Layers.ID_SLOT_PAYLINES); //hack to keep wings on top of frame during transition
			viewportRectH = baseWingsCamera.rect.height;
			viewportRectY = baseWingsCamera.rect.y;
			tweenViewportRectToDefault();

			StartCoroutine(tweenWingsUpDuringTransition(FADE_TIME));
		}

		foreach(GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
		{
			objectToDeactivateImmediately.SetActive(false);
		}

		if (objectsToAnimate != null && objectsToAnimate.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(objectsToAnimate));
		}
		
		while (elapsedTime < FADE_TIME)
		{
			elapsedTime += Time.deltaTime;
			foreach(GameObject objectToFade in objectsToFade)
			{
				CommonGameObject.alphaGameObject(objectToFade, 1 - (elapsedTime / FADE_TIME));
			}
			yield return null;
		}

		foreach (GameObject objectToFade in objectsToFade)
		{
			CommonGameObject.alphaGameObject(objectToFade, 0.0f);
		}

		foreach (GameObject objectToDeactivate in objectsToDeactivate)
		{
			objectToDeactivate.SetActive(false);
		}
		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(false);
			Overlay.instance.fadeInNow();
		}

		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.hidePanels();
			SpinPanel.instance.restoreAlpha();
		}

	}
	
	public override IEnumerator executeOnBonusGameEnded()
	{
		fadeInBackground();

		StartCoroutine(base.executeOnBonusGameEnded());
		yield return null;

		//Making sure to set our wings back to regular since we're in the base game
		if (background != null && (originalWingType == ReelGameBackground.WingTypeOverrideEnum.Basegame || originalWingType == ReelGameBackground.WingTypeOverrideEnum.Basegame)) 
		{
			background.wingType = ReelGameBackground.WingTypeOverrideEnum.Basegame;
		}
		resetWings();
	}

	private void fadeInBackground()
	{
		if (shouldFadeSymbols)
		{
			SlotReel[] reelArray = reelGame.engine.getReelArray();

			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				List<SlotSymbol> symbolList = reelGame.engine.getSlotReelAt (reelID).symbolList;
				foreach (SlotSymbol symbol in symbolList)
				{	
					if (symbol.animator != null)
					{
						symbol.animator.gameObject.SetActive(true);
					}
				}
			}
		}

		foreach (GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
		{
			objectToDeactivateImmediately.SetActive(true);
		}

		Debug.LogWarning("BonusGameFadeTransition.fadeInBackground() - Fading the object list back in!");
		foreach (GameObject objectToFade in objectsToFade)
		{
			CommonGameObject.alphaGameObject(objectToFade, 1.0f);
		}

		foreach (GameObject objectToDeactivate in objectsToDeactivate)
		{
			objectToDeactivate.SetActive(true);
		}

		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(true);
		}

		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		}


		if (shouldFadeSymbols)
		{
			//Remove the symbols from our fade objects list so it doesn't continually get larger
			objectsToFade.RemoveRange(startingObjectsToFadeCount, objectsToFade.Count - startingObjectsToFadeCount);
		}

		if (reelGame.GetComponent<HideSpinPanelMetersUIModule>() == null) 
		{
			SpinPanel.instance.showFeatureUI(true);
			RoyalRushCollectionModule.showTopMeter(true);
		}
	}

	//Used to tween the wings while transitioning, to fill in the empty space when fading out the spin panel.

	private IEnumerator tweenWingsUpDuringTransition(float duration)
	{
		ReelGameWings wings = background.wings;
		Vector3 newWingScale = Vector3.one;
		Vector3 newWingPos = Vector3.one;
		iTween.EaseType easeType = iTween.EaseType.linear;
		float parentY = 0;
		newWingScale = new Vector3(CHALLENGE_SCALE_Y * .5f, CHALLENGE_SCALE_Y, 1f);
		float offset = newWingScale.x * 0.5f + CHALLENGE_SCALE_X * 0.5f;
		newWingPos = new Vector3(offset, 0, 0);
		parentY = CHALLENGE_POS_Y;
		iTween.ScaleTo(wings.rightMeshFilter.gameObject, iTween.Hash("scale", newWingScale, "time", duration, "islocal", true, "easetype", easeType));
		iTween.MoveTo(wings.rightMeshFilter.gameObject, iTween.Hash("position", newWingPos, "time", duration, "islocal", true, "easetype", easeType));
		newWingPos.x = -newWingPos.x;
		iTween.ScaleTo(wings.leftMeshFilter.gameObject, iTween.Hash("scale", newWingScale, "time", duration, "islocal", true, "easetype", easeType));
		iTween.MoveTo(wings.leftMeshFilter.gameObject, iTween.Hash("position", newWingPos, "time", duration, "islocal", true, "easetype", easeType));
		// Move the parent object too FS_POS_Y
		iTween.MoveTo(wings.gameObject, iTween.Hash("y", parentY, "time", duration, "islocal", true, "easetype", easeType));
		yield return new TIWaitForSeconds(duration);
	}
}
