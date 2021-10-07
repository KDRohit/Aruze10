using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Zynga.Unity.Attributes;

/**
Handles a transition to a bonus game that involves a prefab with an animator attached.

TODO - add more functionality for transitioning UI elements.

NOTE - This module ONLY works on ReelGames that derive from SlotBaseGame!

Original Author: Nick Reynolds
*/
public class BonusGameAnimatedTransition : BaseTransitionModule
{
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] AnimationListController.AnimationInformationList preFadeAnimationList; // Animation list that triggers before the fading happens
	
	[SerializeField] AnimationListController.AnimationInformationList additionalAnimationList;
	[SerializeField] AnimationListController.AnimationInformationList additionalIdleAnimationList; // If you need to reset your animations when returning to base, this is a good place to put them
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private GameObject transitionObject = null;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldActivateTransitionObject; //Use this if the transition object is deactived in the inspector
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldActivateTransitionObjectOnReturn; //As above, but specific to return animations
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private string transitionAnimName = "";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private string transitionIdleAnimName = "";
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private float TRANSITION_ANIMATION_LENGTH_OVERRIDE = -1.0f;	// used to override how long the animation as, can be used to create two parts to the animation
	[SerializeField] private float CONTINUE_IN_BONUS_ANIMATION_LENGTH_OVERRIDE = -1.0f;			// if using states with transitions may need to just set the total time to wait for the full amount of your animation
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldFadeSymbols;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldFinishFadeBeforeAnimation;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldFadeWings;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool shouldFadeTopOverlay;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[Tooltip("may also hide some feature Ui ex. VIP revamp lobby token meter")]
	[SerializeField] private bool shouldFadeSpinPanel;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private bool shouldPlayTransitionSound = true;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private float FADE_TIME;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private List<GameObject> objectsToFade;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private List<GameObject> objectsToDeactivate;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private List<GameObject> objectsToDeactivateImmediately;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private float PRE_GO_INTO_BONUS_DELAY;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private float POST_GO_INTO_BONUS_DELAY;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool SHOULD_SLIDE_OUT_OVERLAY;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool SHOULD_SLIDE_OUT_OVERLAY_WITH_ANIMAITON;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool SHOULD_SLIDE_OUT_SPIN_PANEL_WITH_ANIMAITON;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private bool SHOULD_HIDE_ALL_UI_DURING_TRANSITION;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	[SerializeField] private GameObject transitionCamera = null;
	
	[SerializeField] private bool continueAnimationInBonusGame;
	[SerializeField] private bool hasTransitionBackToBaseGame;
	[SerializeField] private bool isFadeBackInstant = false; // some games may not want to fade the symbols back in over time when returning
	[SerializeField] private TransitionBackBonusType transitionBackBonusType = TransitionBackBonusType.BOTH;
	[SerializeField] protected string backToBaseGameAnimName = "";
	[SerializeField] private float TRANSITION_BACK_ANIMATION_LENGTH_OVERRIDE = -1.0f;
	[SerializeField] AnimationListController.AnimationInformationList additionalTransitionBackAnimationList;
	[SerializeField] private float PRE_GO_INTO_BIG_WIN_DELAY;
	[SerializeField] private GameObject backgroundMover; //Used to we can put the transition object in the correct position when transitioning back so there isn't a quick flash effect of it resetting itself
	[SerializeField] protected Vector3 transitionBackStartingPosition; //position that we want the background mover to start in when transitioning back to the base game
	[SerializeField] protected Layers.LayerID layerToSwitchSymbolsToForViewportSpecificCameras = Layers.LayerID.ID_HIDDEN; // Viewport specific cameras don't work for certain transitions involving moving the symbols, so we need to move them to a full screen camera and restore them when we come back

	[System.NonSerialized] public bool isSkippingShowNonBonusOutcomes = false; // use this if the show non bonus outcomes needs to be skipped, for instance when using bonus game queuing

	[SerializeField] private string TRANSITION_FROM_BONUS_KEY = "transition_from_bonus";
	[SerializeField] private string TRANSITION_FROM_FREESPINS_KEY = "transition_from_freespins";

	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private AudioListController.AudioInformationList transitionFromBonusAudioList;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private AudioListController.AudioInformationList transitionFromFreespinsAudioList;
	private bool areTransitionFromSoundsMergedToAudioLists = false; // controls if the TRANSITION_FROM_BONUS_KEY and TRANSITION_FROM_FREESPINS_KEY have been merged into the audio lists for a single audio call

	private Dictionary<GameObject, Dictionary<Material, float>> initialAlphaValueMaps = new Dictionary<GameObject, Dictionary<Material, float>>();

	/// What kind of bonuses to apply the transitions to
	protected enum TransitionBackBonusType
	{
		FREESPINS 	= 0,
		PICKEM 		= 1,
		BOTH		= 2,
		PORTAL		= 3
	}

	private Transform baseGame = null;
	private bool animatedAfterThisBonusGameAlready = false; //Used to make sure we only animate once per transition back to the basegame
	private bool cameFromBonusGame = false; //Don't want to execute the transition back to the basegame if we aren't coeming back from a bonus game
	private Animator transitionAnimator;
	private int startingObjectsToFadeCount;
	private ReelGameBackground.WingTypeOverrideEnum originalWingType;

	private Dictionary<SymbolAnimator, Dictionary<Transform, int>> symbolLayerRestoreMaps = new Dictionary<SymbolAnimator, Dictionary<Transform, int>>(); // when using viewport specific cameras and swapping symbol layers before the transition, we want to be able to correctly restore the symbols after the transition is done
	private Dictionary<LabelWrapperComponent, float> symbolLabelAlphaRestoreMap = new Dictionary<LabelWrapperComponent, float>(); //TMP label alphas are handled differently from other materials so they don't get added to the initial value map, need to restore manually

	//Constants used for tweening wings during a transition
	private const float CHALLENGE_SCALE_X = 11.6f;
	private const float CHALLENGE_SCALE_Y = 8.7f;
	private const float CHALLENGE_POS_Y = 0.04f;

	void Start()
	{
		if (transitionObject != null)
		{
			transitionAnimator = transitionObject.GetComponent<Animator> ();
		}
		else
		{
			Debug.LogError("Module is missing an animator. Going to destroy myself since theres to animations to play.");
			Destroy (this);
		}

		if (background != null)
		{
			originalWingType = background.wingType;
		}

		initializeObjectsToFade();
	}

	// Add to our initial the list objectsToFade that can be faded out and save the 
	// starting alpha in corresponding initialAlphaValueMaps
	private void initializeObjectsToFade()
	{
		if (shouldFadeWings)
		{
			objectsToFade.Add(background.wings.gameObject);
		}

		startingObjectsToFadeCount = objectsToFade.Count;
		
		for (int i = 0; i < objectsToFade.Count; i++)
		{
			initialAlphaValueMaps.Add(objectsToFade[i], CommonGameObject.getAlphaValueMapForGameObject(objectsToFade[i]));
		}
	}

	// Add reel symbols to the list of objectsToFade in and out during transitions
	private void addReelSymbolsToFadeList()
	{
		SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
		for (int i = 0; i < slotReels.Length; i++)
		{
			List<SlotSymbol> symbolList = slotReels[i].symbolList;

			for (int j = 0; j < symbolList.Count; j++)
			{
				addSymbolToFadeList(symbolList[j]);
			}
		}
	}

	// Add a symbol to objectsToFade and save its starting alpha value so it can be faded back in.
	// Optionally the symbols alpha value can be set (after starting alpha value is saved of course)
	private void addSymbolToFadeList(SlotSymbol symbol, bool setAlphaValue = false, float alphaValue = 0.0f)
	{
		if (symbol.animator != null)
		{
			if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
			{
				symbol.mutateToFlattenedVersion();
			}

			if (!objectsToFade.Contains(symbol.animator.gameObject))
			{
				objectsToFade.Add(symbol.animator.gameObject);
				addSymbolAlphaMap(symbol);
			}
			
			if (setAlphaValue)
			{
				CommonGameObject.alphaGameObject(symbol.animator.gameObject, alphaValue);
			}
		}
	}	

	// Add the starting alpha of a symbol to the initialAlphaValueMaps and
	// verify that is not already faded.
	private void addSymbolAlphaMap(SlotSymbol symbol)
	{
		if (!initialAlphaValueMaps.ContainsKey(symbol.animator.gameObject))
		{
			initialAlphaValueMaps.Add(symbol.animator.gameObject, CommonGameObject.getAlphaValueMapForGameObject(symbol.animator.gameObject));
			verifyInitialAlphaMapIsVisible(initialAlphaValueMaps[symbol.animator.gameObject]);
		}

		LabelWrapperComponent[] labelWrappers = symbol.getAllLabels();
		foreach (LabelWrapperComponent label in labelWrappers)
		{
			if (!symbolLabelAlphaRestoreMap.ContainsKey(label))
			{
				symbolLabelAlphaRestoreMap.Add(label, label.alpha);
			}
		}
	}

	// Check if the symbol is already faded out - i've never been able to trigger this warning
	// but we have it here just in case because of some issue with elvira03.
	private void verifyInitialAlphaMapIsVisible(Dictionary<Material, float> alphaMap)
	{
		foreach (KeyValuePair<Material, float> item in alphaMap)
		{
			if (item.Value == 0)
			{
				Debug.LogError("verifyInitialAlphaMapIsVisible : You cannot initialize an object faded to 0 from the start");
			}
		}
	}

	// Remove all the symbols that are already in here because we're starting fresh.
	private void resetObjectsToFade()
	{
		if (objectsToFade.Count > startingObjectsToFadeCount)
		{
			objectsToFade.RemoveRange(startingObjectsToFadeCount, objectsToFade.Count - startingObjectsToFadeCount);

			Dictionary<GameObject, Dictionary<Material, float>> cleanAlphaMap = new Dictionary<GameObject, Dictionary<Material, float>>();
			for (int i = 0; i < objectsToFade.Count; i++)
			{
				cleanAlphaMap.Add(objectsToFade[i], initialAlphaValueMaps[objectsToFade[i]]);
			}

			initialAlphaValueMaps = cleanAlphaMap;
		}
		symbolLabelAlphaRestoreMap.Clear();
	}

	protected override IEnumerator doTransition()
	{
		// Make sure this gets reset when doing a new transition, in case
		// it got set but wasn't cleared.
		if (isSkippingShowNonBonusOutcomes)
		{
			isSkippingShowNonBonusOutcomes = false;
		}
	
		cameFromBonusGame = true; //Since we're transition, we know we're entering a bonus game
		animatedAfterThisBonusGameAlready = false; //Need to animate back, if this game has a transition back to the basegame

		// if this game is using viewport specific cameras then swap the symbols onto a different layer for a full size camera in case the animation will move the symbols
		changeSymbolLayersForViewportSpecificCameras();

		if (shouldActivateTransitionObject)
		{
			if (transitionObject != null)
			{
				transitionObject.SetActive (true);
			}
			else
			{
				Debug.LogError("transitionObject is null and you're trying to activate it.");
			}
		}

		if (continueAnimationInBonusGame)
		{
			if (transitionCamera != null)
			{
				baseGame = transitionCamera.transform.parent;
				transitionCamera.transform.parent = null;
			}
			else
			{
				Debug.LogError("You want to continueAnimationInBonusGame, but there's no transition camera defined.");
			}
		}
		
		// Grab the animator
		//Animator anim = transitionObject.GetComponent<Animator>();
		if (shouldPlayTransitionSound)
		{
			playTransitionSounds();
		}

		if (preFadeAnimationList != null && preFadeAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preFadeAnimationList));
		}
		
		if (shouldFadeSymbols)
		{
			addReelSymbolsToFadeList();
			// Add a wait here in case we are creating new flattened symbols
			// since those symbols will have destroyed something that will
			// still exist if we continue to do fading on them in the same frame
			// before those objects are truly destroyed
			yield return null;
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

		if (shouldFinishFadeBeforeAnimation)
		{
			while (fadeBackgroundsCorotuine != null && !fadeBackgroundsCorotuine.finished)
			{
				yield return null;
			}
		}

		// Play the transition anim
		if (TRANSITION_ANIMATION_LENGTH_OVERRIDE > 0)
		{
			if (transitionAnimator != null)
			{
				if (additionalAnimationList.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(additionalAnimationList));
				}

				if (!string.IsNullOrEmpty(transitionAnimName))
				{
					// force the speed to 1 before we play the animation, in case it was
					// set to 0 but not reset by one of the cases where the animation continues
					transitionAnimator.speed = 1.0f;
					transitionAnimator.Play(transitionAnimName);
					yield return new TIWaitForSeconds(TRANSITION_ANIMATION_LENGTH_OVERRIDE);
					transitionAnimator.speed = 0.0f;
				}
				else
				{
					// adding this delay intentionally here since it used to do it even if the anim name wasn't set, 
					// so some game may have relied on this functionality
					yield return new TIWaitForSeconds(TRANSITION_ANIMATION_LENGTH_OVERRIDE);
				}
			}
		}
		else
		{	
			if (transitionAnimator != null)
			{
				if (additionalAnimationList.Count > 0)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(additionalAnimationList));
				}

				if (!string.IsNullOrEmpty(transitionAnimName))
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(transitionAnimator, transitionAnimName));
				}
			}
		}

		// even if we aren't waiting on the backgorund fade before the animation, 
		// we should make sure it is wrapped up before we proceed into the bonus
		// so values are correctly reset
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

		SlotBaseGame.instance.createBonus();
		if (reelGame is LayeredMultiSlotBaseGame)
		{
			SlotBaseGame.instance.startBonus(); //This will remove the outcome from the layeredBonusOutcomes list and show the bonus
		}
		else
		{
			if (SlotBaseGame.instance.isDoingFreespinsInBasegame())
			{
				BonusGameManager.instance.showStackedBonus(isHidingSpinPanelOnPopStack:false, null, shouldCreateBonusWithTransition: true);
			}
			else
			{
				BonusGameManager.instance.show(null, shouldCreateBonusWithTransition: true);
			}
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

		if (BonusGamePresenter.instance != null && BonusGamePresenter.instance.isHidingBaseGame)
		{
			BonusGameManager.currentBaseGame.gameObject.SetActive(false);
		}

		if(continueAnimationInBonusGame)
		{
			transitionCamera.transform.position += BonusGameManager.instance.transform.position;
			if(transitionAnimator != null)
			{
				transitionAnimator.speed = 1.0f;
				if (CONTINUE_IN_BONUS_ANIMATION_LENGTH_OVERRIDE > 0)
				{
					yield return new TIWaitForSeconds(CONTINUE_IN_BONUS_ANIMATION_LENGTH_OVERRIDE);
				}
				else
				{
					yield return new TIWaitForSeconds(transitionAnimator.GetCurrentAnimatorStateInfo(0).length - TRANSITION_ANIMATION_LENGTH_OVERRIDE);
				}
				if (transitionCamera != null)
				{
					transitionCamera.SetActive(false);
				}
				else
				{
					Debug.LogError("transitionCamera is null, but you want to continueAnimationInBonusGame, so we can't deactivate it.");
				}
			}
		}

		// Put the anim in idle mode
		if (transitionAnimator != null) 
		{
			if (additionalIdleAnimationList.animInfoList.Count > 0)
			{
				if (gameObject.activeInHierarchy)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(additionalIdleAnimationList));
				}
			}
			if (!string.IsNullOrEmpty(transitionIdleAnimName)) 
			{
				transitionAnimator.Play(transitionIdleAnimName);
				transitionAnimator.speed = 1.0f;
			}
			
		}

		if (shouldActivateTransitionObject)
		{
			if (transitionObject != null)
			{
				transitionObject.SetActive(false);
			}
			else
			{
				Debug.LogError("shouldActivateTransitionObject is set to true, but transitionObject is null!");
			}
		}
	}

	protected IEnumerator fadeOutBackgrounds()
	{
		float elapsedTime = 0;

		TICoroutine overlayFadeCorotuine = null;
		if (shouldFadeTopOverlay)
		{
			// Just hide the jackpot/mystery bar right away, since in some cases, like
			// VIP Revamp it has animated elements that will not fade nicely.
			Overlay.instance.hideJackpotMystery();
			overlayFadeCorotuine = StartCoroutine(Overlay.instance.fadeOut(FADE_TIME));
		}

		TICoroutine spinPanelFadeCoroutine = null;
		if (shouldFadeSpinPanel)
		{
			spinPanelFadeCoroutine = StartCoroutine(SpinPanel.instance.fadeOut(FADE_TIME));
		}

		if (shouldFadeSpinPanel && shouldFadeTopOverlay && background != null && background.wingType != ReelGameBackground.WingTypeOverrideEnum.Fullscreen && background.wings != null)
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
			if (objectToDeactivateImmediately != null)
			{
				objectToDeactivateImmediately.SetActive(false);
			}
			else
			{
				Debug.LogWarning("objectToDeactivateImmediately was null! Please adjust the size of the list you're using.");
			}
		}

		if (objectsToFade != null && objectsToFade.Count > 0)
		{
			yield return StartCoroutine(CommonGameObject.fadeGameObjectsToFromCurrent(objectsToFade.ToArray(), 0f, FADE_TIME, false));

			foreach (GameObject objectToFade in objectsToFade)
			{
				if (objectToFade != null)
				{
					CommonGameObject.alphaGameObject(objectToFade, 0.0f);
				}
				else
				{
					Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
				}
			}
		}

		foreach (GameObject objectToDeactivate in objectsToDeactivate)
		{
			if (objectToDeactivate != null)
			{
				objectToDeactivate.SetActive(false);
			}
			else
			{
				Debug.LogWarning("objectToDeactivate was null! Please adjust the size of the list you're using.");
			}
		}

		if (shouldFadeTopOverlay)
		{
			// make sure the corotuine is complete before reseting
			while (overlayFadeCorotuine != null && !overlayFadeCorotuine.finished)
			{
				yield return null;
			}

			Overlay.instance.top.show(false);
			Overlay.instance.fadeInNow();
		}

		if (shouldFadeSpinPanel)
		{
			// make sure the corotuine is complete before reseting
			while (spinPanelFadeCoroutine != null && !spinPanelFadeCoroutine.finished)
			{
				yield return null;
			}

			SpinPanel.instance.hidePanels();
			SpinPanel.instance.restoreAlpha();
		}
	}
	
	public override IEnumerator executeOnBonusGameEnded()
	{
		if (continueAnimationInBonusGame)
		{
			if (transitionAnimator != null)
			{
				if (transitionCamera != null)
				{
					transitionCamera.transform.parent = baseGame;
					transitionCamera.transform.position -= BonusGameManager.instance.transform.position;
					transitionCamera.SetActive(true);
				}
				else
				{
					Debug.LogError("transitionCamera is null, but you want to continueAnimationInBonusGame.");
				}
				transitionAnimator.Play(transitionIdleAnimName);
			}
		}

		if (!hasTransitionBackToBaseGame)
		{
			foreach (GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
			{
				if (objectToDeactivateImmediately != null)
				{
					objectToDeactivateImmediately.SetActive(true);
				}
				else
				{
					Debug.LogWarning("objectToDeactivateImmediately was null! Please adjust the size of the list you're using.");
				}
			}

			// change the visible symbol layers back if they were modified here, if there is a transition back then we will wait till that is done
			// in case that will slide the reels back onto the screen
			restoreSymbolLayersChangedForViewportSpecificCameras();
			restoreFadedObjectsToOriginalAlphaImmediately();

			foreach (GameObject objectToDeactivate in objectsToDeactivate)
			{
				if (objectToDeactivate != null)
				{
					objectToDeactivate.SetActive (true);
				}
				else
				{
					Debug.LogWarning("objectToDeactivate was null! Please adjust the size of the list you're using.");
				}
			}

			if (shouldFadeTopOverlay)
			{
				Overlay.instance.top.show (true);
			}

			bool isDoingFreespinsInBase = (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame());

			if (shouldFadeSpinPanel)
			{
				if (isDoingFreespinsInBase)
				{
					SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
				}
				else
				{
					SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
				}
			}

			if (isDoingFreespinsInBase)
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.FREE_SPINS);
			}
			else
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
			}
		}

		SpinPanel.instance.resetAutoSpinUI();
		
		if (shouldFadeSymbols)
		{
			//If theres no transition back to the basegame then we can go ahead and turn any effects back on 
			if (!hasTransitionBackToBaseGame)
			{
				SlotReel[] reelArray = reelGame.engine.getReelArray();

				for (int reelID = 0; reelID < reelArray.Length; reelID++)
				{
					List<SlotSymbol> symbolList = reelGame.engine.getSlotReelAt (reelID).symbolList;
					foreach (SlotSymbol symbol in symbolList)
					{	
						if (symbol.animator != null)
						{
							symbol.animator.gameObject.SetActive (true);
						}
					}
				}				
			} 
			else
			{
				addUnflattenedSymbols();
			}
		}

		if (!hasTransitionBackToBaseGame && reelGame.GetComponent<HideSpinPanelMetersUIModule>() == null) 
		{
			SpinPanel.instance.showFeatureUI(true);
			RoyalRushCollectionModule.showTopMeter(true);
		}
		StartCoroutine(base.executeOnBonusGameEnded());

		//Need to make sure we didn't already animate before a big win, if one was created. 
		if (hasTransitionBackToBaseGame && !animatedAfterThisBonusGameAlready) 
		{
			yield return StartCoroutine(playTransitionBackToBaseGameAnimation());
		}
		else
		{
			//Make sure wait at least one frame before NonBonusOutcomes().
			yield return null;
		}

		if (hasTransitionBackToBaseGame && cameFromBonusGame && !isSkippingShowNonBonusOutcomes)
		{
			//Now that the transition has happened we can safely display paylines and animate any outcome symbols. 
			SlotBaseGame.instance.doShowNonBonusOutcomes();
		}

		// now that we've skipped, reset this value
		if (isSkippingShowNonBonusOutcomes)
		{
			isSkippingShowNonBonusOutcomes = false;
		}

		//Making sure to set our wings back to regular since we're in the base game
		if (background != null && (originalWingType == ReelGameBackground.WingTypeOverrideEnum.Basegame || originalWingType == ReelGameBackground.WingTypeOverrideEnum.Basegame)) 
		{
			background.wingType = ReelGameBackground.WingTypeOverrideEnum.Basegame;
		}
		resetWings();
		cameFromBonusGame = false; //Now that the bonus game is over, we can reset this flag.
		isTransitionStarted = false;
	}


	// This exists because of past issues with un-flattend symbols after freespins in elvira03
	// I have refactored the code to properly track alpha maps for symbols so this code is not needed anymore. 
	// However, we keep this here because maybe i'm wrong and TRAMP will let us know if we really still need this.
	private void addUnflattenedSymbols()
	{
		SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
		for (int i = 0; i < slotReels.Length; i++)
		{
			List<SlotSymbol> symbolList = slotReels[i].symbolList;

			for (int j = 0; j < symbolList.Count; j++)
			{
				SlotSymbol symbol = symbolList[j];
				if (symbol.animator != null)
				{
					if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
					{
						addSymbolToFadeList(symbol, true, 0.0f);
						Debug.LogError("unfadedSymbolsDetected - this shouldn't be happenning at this part");
					}
				}
			}
		}
	}

	public override bool needsToExecuteOnPreBigWin()
	{
		return (hasTransitionBackToBaseGame && isTransitionStarted);
	}
	
	public override IEnumerator executeOnPreBigWin()
	{
		//Need to go through our transition before the bigwin and rollup
		if (!animatedAfterThisBonusGameAlready)
		{
			yield return StartCoroutine (playTransitionBackToBaseGameAnimation ());
		}
		yield return new TIWaitForSeconds (PRE_GO_INTO_BIG_WIN_DELAY);
	}

	private bool shouldTransitionBackFromCurrentBonusGame()
	{
		if(transitionBackBonusType == TransitionBackBonusType.FREESPINS && BonusGameManager.instance.currentGameType == BonusGameType.GIFTING
			|| transitionBackBonusType == TransitionBackBonusType.PICKEM && BonusGameManager.instance.currentGameType == BonusGameType.CHALLENGE
			|| transitionBackBonusType == TransitionBackBonusType.BOTH)
		{
			return true;
		}

		return false;
	}

	protected virtual IEnumerator playTransitionBackToBaseGameAnimation()
	{
		animatedAfterThisBonusGameAlready = true;
		if (cameFromBonusGame) 
		{
			if (transitionAnimator != null && shouldTransitionBackFromCurrentBonusGame())
			{
				if (shouldActivateTransitionObject || shouldActivateTransitionObjectOnReturn)
				{
					transitionObject.SetActive(true);
				}
				if (shouldFadeTopOverlay)
				{
					Overlay.instance.top.show(false);
					Overlay.instance.hideJackpotMystery();
				}

				if (shouldFadeSpinPanel)
				{
					SpinPanel.instance.hidePanels();
				}

				if (shouldFadeSpinPanel && shouldFadeTopOverlay && background != null)
				{
					//Set our wings to fullsize so we can transition back without gaps on the sides
					background.wingType = ReelGameBackground.WingTypeOverrideEnum.Fullscreen;
				}
				if (backgroundMover != null)
				{
					backgroundMover.transform.localPosition = transitionBackStartingPosition;
				}

				Overlay.instance.setButtons(false); //Turn off the buttons so someone doesnt spin while transitioning
				reelGame.activePaylinesGameObject.SetActive (false); //Hide the paylines so they don't go over the transition

				// Check if we need to merge the old transition back sounds into the audio lists
				if (!areTransitionFromSoundsMergedToAudioLists)
				{
					if (!string.IsNullOrEmpty(TRANSITION_FROM_BONUS_KEY))
					{
						transitionFromBonusAudioList.addSound(name:TRANSITION_FROM_BONUS_KEY);
					}

					if (!string.IsNullOrEmpty(TRANSITION_FROM_FREESPINS_KEY))
					{
						transitionFromFreespinsAudioList.addSound(name:TRANSITION_FROM_FREESPINS_KEY);
					}

					areTransitionFromSoundsMergedToAudioLists = true;
				}

				if (transitionBonusType == TransitionBonusType.PICKEM)
				{
					if (transitionFromBonusAudioList.Count > 0)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(transitionFromBonusAudioList));
					}
				}
				else
				{
					if (transitionFromFreespinsAudioList.Count > 0)
					{
						yield return StartCoroutine(AudioListController.playListOfAudioInformation(transitionFromFreespinsAudioList));
					}
				}

				yield return StartCoroutine(playBackToBaseGameTransitionAnimations());
			}
		
			//Wait till our animation is over before turning stuff back on, so that they don't interfere with the animation.
			foreach (GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
			{
				if (objectToDeactivateImmediately != null)
				{
					objectToDeactivateImmediately.SetActive(true);
				}
				else
				{
					Debug.LogWarning("objectToDeactivateImmediately was null! Please adjust the size of the list you're using.");
				}
			}

			// now that the animation should be over it should be safe to put the symbols back on their original layers
			// if they were modified so they would appear correctly in games using the viewport specific cameras
			restoreSymbolLayersChangedForViewportSpecificCameras();

			if (shouldFadeSymbols)
			{
				if (isFadeBackInstant)
				{
					fadeInBackgroundImmediate();
				}
				else
				{
					yield return StartCoroutine(fadeInBackground());
				}
			}
			else
			{
				// We need to make sure that the UI is still turned back on
				// even if we don't need to fade the symbols back
				restoreOverlay();
				restoreSpinPanel();
			}

			foreach (GameObject objectToDeactivate in objectsToDeactivate)
			{
				if (objectToDeactivate != null)
				{
					objectToDeactivate.SetActive(true);
				}
				else
				{
					Debug.LogWarning("objectToDeactivate was null! Please adjust the size of the list you're using.");
				}
			}

			if (shouldActivateTransitionObject || shouldActivateTransitionObjectOnReturn)
			{
				if (transitionObject != null)
				{
					transitionObject.SetActive(false);
				}
				else
				{
					Debug.LogError("transitionObject is null, and you're trying to deactivate it.");
				}
			}
			reelGame.activePaylinesGameObject.SetActive(true);
			if (reelGame.GetComponent<HideCharmsMeterModule>() == null)
			{
				SpinPanel.instance.showFeatureUI(true);
				RoyalRushCollectionModule.showTopMeter(true);
			}

			SlotBaseGame baseGameCheck = reelGame as SlotBaseGame;
			if (baseGameCheck != null && !baseGameCheck.isGameBusy)
			{
				//Now that everything is back to normal, we can allow spins again (only if the base game isn't busy)
				Overlay.instance.setButtons(true); 
			}

			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame())
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.FREE_SPINS);
			}
			else
			{
				SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
			}
			
			SpinPanel.instance.showSideInfo(reelGame.showSideInfo);
		}
	}

	private IEnumerator playBackToBaseGameTransitionAnimations()
	{
		List<TICoroutine> backAnimationCoroutines = new List<TICoroutine>();

		backAnimationCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(additionalTransitionBackAnimationList)));

		if (!string.IsNullOrEmpty(backToBaseGameAnimName))
		{
			if (TRANSITION_BACK_ANIMATION_LENGTH_OVERRIDE < 0)
			{
				backAnimationCoroutines.Add(StartCoroutine(CommonAnimation.playAnimAndWait(transitionAnimator, backToBaseGameAnimName)));
			}
			else
			{
				transitionAnimator.Play(backToBaseGameAnimName);
				transitionAnimator.speed = 1.0f;
				yield return new TIWaitForSeconds(TRANSITION_BACK_ANIMATION_LENGTH_OVERRIDE);
			}
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(backAnimationCoroutines));
	}

	// Change symbol layers for transition in case it slides while using a viewport specific camera, these will then be restored before the game starts
	private void changeSymbolLayersForViewportSpecificCameras()
	{
		// if this game is using viewport specific cameras then swap the symbols onto a different layer for a full size camera in case the animation will move the symbols
		if (reelGame.reelGameBackground != null 
			&& reelGame.reelGameBackground.isUsingReelViewportSpecificCameras 
			&& layerToSwitchSymbolsToForViewportSpecificCameras != Layers.LayerID.ID_HIDDEN)
		{
			// clear any layer maps from previous runs of this transition module
			symbolLayerRestoreMaps.Clear();

			List<SlotSymbol> allReelSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				allReelSymbols.AddRange(reelArray[i].symbolList);
			}

			for (int i = 0; i < allReelSymbols.Count; i++)
			{
				if (!symbolLayerRestoreMaps.ContainsKey(allReelSymbols[i].getAnimator()))
				{
					symbolLayerRestoreMaps.Add(allReelSymbols[i].getAnimator(), CommonGameObject.getLayerRestoreMap(allReelSymbols[i].gameObject));
					CommonGameObject.setLayerRecursively(allReelSymbols[i].gameObject, (int)layerToSwitchSymbolsToForViewportSpecificCameras);
				}
			}
		}
	}

	// For viewport specific cameras we need to make sure during transitions we switch the symbol layers to a full size camera
	// in case the transition will slide the symbols, this function will restore the symbol layers after the animations are complete
	private void restoreSymbolLayersChangedForViewportSpecificCameras()
	{
		// if this game is using viewport specific cameras then we will restore the symbol layers
		if (reelGame.reelGameBackground != null 
			&& reelGame.reelGameBackground.isUsingReelViewportSpecificCameras 
			&& layerToSwitchSymbolsToForViewportSpecificCameras != Layers.LayerID.ID_HIDDEN)
		{
			List<SlotSymbol> allReelSymbols = new List<SlotSymbol>();
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				allReelSymbols.AddRange(reelArray[i].symbolList);
			}

			for (int i = 0; i < allReelSymbols.Count; i++)
			{
				// @todo : consider making this so it doesn't restore on more than 1 mega symbol part, right now it restores on all parts
				if (symbolLayerRestoreMaps.ContainsKey(allReelSymbols[i].getAnimator()))
				{
					CommonGameObject.restoreLayerMap(allReelSymbols[i].gameObject, symbolLayerRestoreMaps[allReelSymbols[i].getAnimator()]);
				}
			}
		}
	}

	// Restore the top overlay if it was disabled when it was
	// faded out
	private void restoreOverlay()
	{
		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(true);
			Overlay.instance.showJackpotMystery();
		}
	}

	// Restore the spinpanel if it was disabled when it was
	// faded out
	private void restoreSpinPanel()
	{
		if (shouldFadeSpinPanel)
		{
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame())
			{
				SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
			}
			else
			{
				SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
			}
		}
	}

	// Variation on fadeInBackground that doesn't use delays, controlled by isFadeBackInstant
	private void fadeInBackgroundImmediate()
	{
		restoreOverlay();
		restoreSpinPanel();

		restoreFadedObjectsToOriginalAlphaImmediately();
	}

	private void restoreFadedObjectsToOriginalAlphaImmediately()
	{
		for (int i = 0; i < objectsToFade.Count; i++)
		{
			if (objectsToFade[i] != null && initialAlphaValueMaps.ContainsKey(objectsToFade[i]))
			{
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(objectsToFade[i], initialAlphaValueMaps[objectsToFade[i]]);
			}
			else if (objectsToFade[i] == null)
			{
				Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
			}
			else
			{
				Debug.LogErrorFormat("BonusGameAnimatedTransition.restoreFadedObjectsToOriginalAlphaImmediately() gameObject : {0} is missing initialAlphaValueMaps", objectsToFade[i].name);
			}
		}

		// restore TMP label alphas that were faded out during CommonGameObject.fadeGameObjectsToFromCurrent call
		foreach (KeyValuePair<LabelWrapperComponent, float> kvp in symbolLabelAlphaRestoreMap)
		{
			kvp.Key.alpha = kvp.Value;
		}

		resetObjectsToFade();
	}
	
	private IEnumerator fadeInBackground()
	{
		restoreOverlay();
		restoreSpinPanel();

		for (int i = 0; i < objectsToFade.Count; i++)
		{
			if (objectsToFade[i] != null)
			{
				if(!initialAlphaValueMaps.ContainsKey(objectsToFade[i])) 
				{
					Debug.LogError("-=-=-=-= initialAlphaValueMaps does not have value for " + objectsToFade[i].name);
				}
				StartCoroutine(CommonGameObject.restoreAlphaValuesToGameObjectFromMapOverTime(objectsToFade[i], initialAlphaValueMaps[objectsToFade[i]], FADE_TIME));
			}
		}
		
		// restore TMP label alphas that were faded out during CommonGameObject.fadeGameObjectsToFromCurrent call
		foreach (KeyValuePair<LabelWrapperComponent, float> kvp in symbolLabelAlphaRestoreMap)
		{
			StartCoroutine(CommonGameObject.interpolateLabelWrapperComponentAlphaOverTime(kvp.Key, kvp.Value, FADE_TIME));
		}

		yield return new TIWaitForSeconds(FADE_TIME);
		resetObjectsToFade();
	}

	public override bool needsToLetModuleTransitionBeforePaylines ()
	{
		return (cameFromBonusGame && hasTransitionBackToBaseGame);
	}

	// Control if the reel game wants the Overlay and SpinPanel turned back on when returning from a bonus
	// you may want to skip that step if for instance you have a transition that will do it for you
	// NOTE: Must be attached to SlotBaseGame
	public override bool isEnablingOverlayWhenBonusGameEnds()
	{
		return !shouldFadeTopOverlay;
	}

	public override bool isEnablingSpinPanelWhenBonusGameEnds()
	{
		return !shouldFadeSpinPanel;
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
