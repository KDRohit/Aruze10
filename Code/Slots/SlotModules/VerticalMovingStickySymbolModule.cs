using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class to handle the moving stickies for munsters01 free spin game
*/
public class VerticalMovingStickySymbolModule : LockingStickySymbolModule 
{
	[SerializeField] protected float SYMBOL_MOVEMENT_SPEED = 1.0f;
	[SerializeField] protected float SYMBOL_MOVE_START_DELAY = 0.0f;
	[Tooltip("Use this if the symbol needs to play a muation animation before finally mutating to the symbol that should be on the reels")]
	[SerializeField] protected string AFTER_SYMBOL_SLIDE_MUTATE_POSTFIX = "";
	[Tooltip("Tells if the outcome animation should be looped on the symbol while it is sliding around the reels")]
	[SerializeField] protected bool isLoopingOutcomeAnimWhileMoving = true;
	[Tooltip("Animations to play when new sticky symbols are added.")]
	[SerializeField] private AnimationListController.AnimationInformationList newStickySymbolBackgroundEffectsAnims;
	[Tooltip("Objects to fade out before the newStickySymbolBackgroundEffectsAnims are played")]
	[SerializeField] private List<GameObject> objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims = new List<GameObject>();
	[Tooltip("Should the symbols be faded out before the newStickySymbolBackgroundEffectsAnims are played?")]
	[SerializeField] private bool shouldFadeSymbolsForNewStickyBackgroundEffectsAnims = true;
	[Tooltip("How long should fading take for the objects that will fade before the newStickySymbolBackgroundEffectsAnims are played?")]
	[SerializeField] private float OBJECT_FADE_TIME_FOR_BACKGROUND_EFFECTS_ANIMS = 1.0f;
	[Tooltip("Handle animated objects taht need to change states when fading out in for newStickySymbolBackgroundEffectsAnims")]
	[SerializeField] private AnimationListController.AnimationInformationList fadeOutAnimsForNewStickySymbolEffectsAnims;
	[Tooltip("Handle animated objects taht need to change states when fading in for newStickySymbolBackgroundEffectsAnims")]
	[SerializeField] private AnimationListController.AnimationInformationList fadeInAnimsForNewStickySymbolEffectsAnims;
	[Tooltip("Object to use for the new sticky symbol spawning as part of the effects")]
	[SerializeField] private GameObject objectForNewStickySymbolEffectsPrefab = null;
	[Tooltip("Transform where the symbol object that is part of the new sticky symbol effects will spawn")]
	[SerializeField] private Transform objectForNewStickySymbolEffectsSpawnLocation;
	[Tooltip("Delay before the object for the new sticky symbols start moving")]
	[SerializeField] private float OBJECT_FOR_NEW_STICKY_SYMBOL_EFFECT_MOVE_DELAY = 0.0f;
	[Tooltip("Time it will take for the object to move from the spawn locaiton to target")]
	[SerializeField] private float OBJECT_FOR_NEW_STICKY_SYMBOL_EFFECT_MOVE_DUR = 0.0f;
	[Tooltip("Sound played when a symbol is mutated to another symbol after sliding")]
	[SerializeField] private string AFTER_SYMBOL_SLIDE_MUTATE_SOUND = "";
	[Tooltip("Sound may request a different mutation sound to play, and for the normal post slide mutation sound to be muted")]
	[SerializeField] private bool playSeparateMutateSoundOnNewSticky = false;
	[Tooltip("Sound played when a new symbol mutates on the reels")]
	[SerializeField] private string NEW_SYMBOL_MUTATE_SOUND = "";	
	[Tooltip("If all symbols mutate at the same time, or there is one music piece for symbols mutating then enable this so it happens only once per spin.")]
	[SerializeField] private bool playAfterSymbolSlideMutateSoundOnlyOnce = true;
	[Tooltip("Symbol sliding or flying over to a place on the reels (in munsters01 this is a bat flapping sound for example).")]
	[SerializeField] private string SYMBOL_SLIDE_OR_MOVE_SOUND = "";
	[Tooltip("Use this if you want the symbols under the sticky symbols to be reverted before the next spin starts.")]
	[SerializeField] private bool isRestoringOriginalSymbols = true;
	[Tooltip("Tells if the sticky symbols when mutated to be on the reels should be forced to a specific reel, i.e. changedMutatedStuckSymbolLayer")]
	[SerializeField] private bool isChangingMutatedStuckSymbolLayers = true;
	[Tooltip("What layer to put the sticky symbols on once mutated onto the actual reels, if isChangingMutatedStuckSymbolLayers is true")]
	[SerializeField] private Layers.LayerID changedMutatedStuckSymbolLayer = (Layers.LayerID)Layers.ID_SLOT_OVERLAY;

	protected List<SlotSymbol> movedStickySymbol = new List<SlotSymbol>();
	private List<TICoroutine> stickySymbolOsciallationCoroutines = new List<TICoroutine>();
	private Dictionary<SlotSymbol, TICoroutine> loopingOutcomeAnimCoroutine = new Dictionary<SlotSymbol, TICoroutine>();
	private GameObjectCacher newStickySpawnEffectCacher = null;
	private List<GameObject> spawnedNewStickyEffectObjects = new List<GameObject>();
	private int numberOfNewStickyEffectObjectsAnimating = 0; // track the animating sticky symbols
	private PlayingAudio symbolMovingPlayingAudio = null; // used to track the playing audio for the symbol moving and cancel it once the symbols have stopped moving
	private List<StandardMutation.ReplacementCell> originalSymbolList = new List<StandardMutation.ReplacementCell>(); // used if isRestoringOriginalSymbols is true
	private bool hasPlayedAfterSymbolSlideMutateSound = false; // tracks if the AFTER_SYMBOL_SLIDE_MUTATE_SOUND has been played
	private bool newStickyAddedThisSpin = false;

	protected override void OnDestroy()
	{
		// make sure we cleanup the coroutines on the off chance any looping ones are still going
		stopOscillatingAllStickySymbols();

		foreach (KeyValuePair<SlotSymbol, TICoroutine> kvp in loopingOutcomeAnimCoroutine)
		{
			StopCoroutine(kvp.Value);
		}
		loopingOutcomeAnimCoroutine.Clear();

		base.OnDestroy();
	}

	public override void Awake()
	{
		base.Awake();

		if (objectForNewStickySymbolEffectsPrefab != null)
		{
			newStickySpawnEffectCacher = new GameObjectCacher(this.gameObject, objectForNewStickySymbolEffectsPrefab);
		}
	}

	// Handle the moving stickies, and then do the normal sticky code
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// Check and handle the moving stickies if we have them
		List<StandardMutation> stickyMutationsList = getCurrentStickyMutations();

		if (stickyMutationsList.Count > 0)
		{
			yield return StartCoroutine(moveStickySymbols(stickyMutationsList));

			if (areNewStickiesAddedThisSpin(stickyMutationsList))
			{
				// we will be adding new stickies so we should play the animation for new stickies being added
				yield return StartCoroutine(playNewStickiesBackgroundEffectsAnims(stickyMutationsList));
			}
		}

		// handle normal sticky stuff
		yield return StartCoroutine(base.executeOnReelsStoppedCallback());
	}

	// @todo : This will be removed after triggerSymbolNames is refactored, but until then I need to loop over all the symbols to determine if new stickies are added
	public bool areNewStickiesAddedThisSpin(List<StandardMutation> stickyMutationsList)
	{
		SlotReel[]reelArray  = reelGame.engine.getReelArray();
		for (int reelID = 0; reelID < reelGame.getReelRootsLength(); reelID++)
		{
			for (int position = 0; position < reelGame.engine.getVisibleSymbolsCountAt(reelArray, reelID, -1); position++)
			{
				for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
				{
					StandardMutation mutation = stickyMutationsList[mutationIndex];

					// Check and see if that visible symbol needs to be changed into something.
					if (!string.IsNullOrEmpty(mutation.triggerSymbolNames[reelID, position]))
					{
						return newStickyAddedThisSpin = true;					
					}
				}
			}
		}

		return newStickyAddedThisSpin = false;
	}

	// @todo : Temporary function to turn triggerSymbolNames into List<StandardMutation.ReplacementCell> until I get a chance to fully convert it
	public List<StandardMutation.ReplacementCell> convertMutationTriggerSymbolNamesToReplacementCellList(List<StandardMutation> stickyMutationsList)
	{
		List<StandardMutation.ReplacementCell> triggerSymbolReplacementCells = new List<StandardMutation.ReplacementCell>();

		SlotReel[] reelArray = reelGame.engine.getReelArray();
		for (int reelID = 0; reelID < reelGame.getReelRootsLength(); reelID++)
		{
			for (int position = 0; position < reelGame.engine.getVisibleSymbolsCountAt(reelArray, reelID, -1); position++)
			{
				for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
				{
					StandardMutation mutation = stickyMutationsList[mutationIndex];

					// Check and see if that visible symbol needs to be changed into something.
					if (!string.IsNullOrEmpty(mutation.triggerSymbolNames[reelID, position]))
					{
						StandardMutation.ReplacementCell replacementCell = new StandardMutation.ReplacementCell();
						replacementCell.reelIndex = reelID;
						replacementCell.symbolIndex = position;
						replacementCell.replaceSymbol = mutation.triggerSymbolNames[reelID, position];
						triggerSymbolReplacementCells.Add(replacementCell);
					}
				}
			}
		}

		return triggerSymbolReplacementCells;
	}

	protected virtual IEnumerator moveStickySymbols(List<StandardMutation> stickyMutationsList)
	{
		if (stickyMutationsList.Count == 0)
		{
			Debug.LogError("Trying to move sticky symbols with no mutation!");
			yield break;
		}

		stopOscillatingAllStickySymbols();

		movedStickySymbol.Clear();

		List<TICoroutine> movingSymbolCoroutines = new List<TICoroutine>();

		for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
		{
			StandardMutation mutation = stickyMutationsList[mutationIndex];

			for (int i = 0; i < mutation.movingStickySymbolList.Count; i++)
			{
				StandardMutation.ReplacementCell movingCell = mutation.movingStickySymbolList[i];

				// We need to find the current sticky that already exists for this cell, we'll just match up the reels they are on
				for (int k = 0; k < currentStickySymbols.Count; k++)
				{
					SlotSymbol stickySymbol = currentStickySymbols[k];

					if (!movedStickySymbol.Contains(stickySymbol))
					{
						int reelIndex = stickySymbol.reel.reelID - 1;
						if (reelIndex == movingCell.reelIndex)
						{
							// found the matching symbol to move
							movingSymbolCoroutines.Add(StartCoroutine(moveSymbolToFinalLocation(stickySymbol, movingCell, SYMBOL_MOVEMENT_SPEED, SYMBOL_MOVE_START_DELAY)));
							movedStickySymbol.Add(stickySymbol);
						}
					}
				}
			}
		}

		if (movingSymbolCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(movingSymbolCoroutines));
		}

		// stop the looped movement sound
		if (symbolMovingPlayingAudio != null)
		{
			Audio.stopSound(symbolMovingPlayingAudio, 0);
			symbolMovingPlayingAudio = null;
		}

		loopingOutcomeAnimCoroutine.Clear();
	}

	// Move a sticky symbol to its final location
	private IEnumerator moveSymbolToFinalLocation(SlotSymbol stickySymbol, StandardMutation.ReplacementCell targetCell, float movementSpeed, float startDelay)
	{
		if (startDelay > 0.0f)
		{
			yield return new TIWaitForSeconds(startDelay);
		}

		Vector3 currentPosition = stickySymbol.gameObject.transform.localPosition - stickySymbol.info.positioning;
		Vector3 targetPosition = stickySymbol.reel.getSymbolPositionForSymbolAtIndex((stickySymbol.reel.reelData.visibleSymbols - 1) - targetCell.symbolIndex, 0, isUsingVisibleSymbolIndex: true, isLocal: true);
		//Debug.Log("VerticalMovingStickySymbolModule.moveSymbolToFinalLocation() - currentPosition = " + currentPosition + "; targetPosition = " + targetPosition);

		Vector3 positionDelta = targetPosition - currentPosition;
		float timeToMove = Mathf.Abs(positionDelta.y / movementSpeed);
		//Debug.Log("VerticalMovingStickySymbolModule.moveSymbolToFinalLocation() - timeToMove = " + timeToMove + "; positionDelta = " + positionDelta);

		float elapsedTime = 0.0f;
		while (elapsedTime < timeToMove)
		{
			stickySymbol.getAnimator().positioning = currentPosition + (elapsedTime / timeToMove) * positionDelta;
			yield return null;
			elapsedTime += Time.deltaTime;
		}

		stickySymbol.getAnimator().positioning = targetPosition;

		// cancel the looping animation
		if (loopingOutcomeAnimCoroutine.ContainsKey(stickySymbol))
		{
			StopCoroutine(loopingOutcomeAnimCoroutine[stickySymbol]);
			stickySymbol.haltAnimation();
		}

		// now that the symbol is at the correct position transfer it so that the locaiton change is reflected in the symbol's information
		int symbolPos = (stickySymbol.reel.reelData.visibleSymbols - 1) - targetCell.symbolIndex + stickySymbol.reel.numberOfTopBufferSymbols;
		stickySymbol.transferSymbol(stickySymbol, symbolPos, stickySymbol.reel);
	
		// also hide the underlying visible symbol at this location so it doesn't appear behind the symbol which is now stopped where it will mutate
		// and add it to the list of symbols to be restored once the next spin is starting
		SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(stickySymbol.reel.reelID - 1)[stickySymbol.visibleSymbolIndex];
		if (visibleSymbol != null)
		{
			visibleSymbol.gameObject.SetActive(false);

			if (isRestoringOriginalSymbols)
			{
				// store info about this symbol
				StandardMutation.ReplacementCell newCell = new StandardMutation.ReplacementCell();
				newCell.reelIndex = visibleSymbol.reel.reelID - 1;
				newCell.symbolIndex = visibleSymbol.visibleSymbolIndex;
				newCell.replaceSymbol = visibleSymbol.name;

				originalSymbolList.Add(newCell);
			}
		}
	}

	public override IEnumerator executeOnPreSpin()
	{
		hasPlayedAfterSymbolSlideMutateSound = false;

		yield return StartCoroutine(base.executeOnPreSpin());

		if (isRestoringOriginalSymbols)
		{
			// need to swap out the current sticky symbols for the original symbols that they replaced
			for (int i = 0; i < originalSymbolList.Count; i++)
			{
				StandardMutation.ReplacementCell currentCell = originalSymbolList[i];

				SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(currentCell.reelIndex)[currentCell.symbolIndex];

				if (visibleSymbol != null)
				{
					visibleSymbol.mutateTo(currentCell.replaceSymbol,  null, false, true);

					if (isChangingMutatedStuckSymbolLayers)
					{
						// need to restore these to the SLOT_REELS layer
						CommonGameObject.setLayerRecursively(visibleSymbol.gameObject, Layers.ID_SLOT_REELS);
					}
				}
			}

			originalSymbolList.Clear();
		}

		// start the symbols oscillating and track the coroutines so they can be stopped once the reels are stopping
		for (int i = 0; i < currentStickySymbols.Count; i++)
		{
			SlotSymbol symbol = currentStickySymbols[i];
			loopingOutcomeAnimCoroutine.Add(symbol, StartCoroutine(loopOutcomeAnimationWhileSymbolMoving(symbol)));
			stickySymbolOsciallationCoroutines.Add(StartCoroutine(oscillateStickySymbolVertically(symbol)));

			// if the looped symbol movement sound isn't started, start it now
			if (symbolMovingPlayingAudio == null)
			{
				if (!string.IsNullOrEmpty(SYMBOL_SLIDE_OR_MOVE_SOUND))
				{
					symbolMovingPlayingAudio = Audio.playSoundMapOrSoundKey(SYMBOL_SLIDE_OR_MOVE_SOUND);
				}
			}
		}
	}

	// cancels sticky symbol oscillation 
	private void stopOscillatingAllStickySymbols()
	{
		for (int i = 0; i < stickySymbolOsciallationCoroutines.Count; i++)
		{
			TICoroutine oscillatingCoroutine = stickySymbolOsciallationCoroutines[i];
			StopCoroutine(oscillatingCoroutine);
		}

		stickySymbolOsciallationCoroutines.Clear();
	}

	// Coroutine that will continue until it is stopped by calling StopCoroutine on the list of coroutines for the oscillating symbols
	private IEnumerator oscillateStickySymbolVertically(SlotSymbol symbol)
	{
		if (symbol == null)
		{
			Debug.LogError("VerticalMovingStickySymbolModule.oscillateStickySymbolVertically() - symbol is NULL!");
			yield break;
		}

		Vector3 topPosition = symbol.reel.getSymbolPositionForSymbolAtIndex(symbol.reel.reelData.visibleSymbols - 1, 0, isUsingVisibleSymbolIndex: true, isLocal: true);
		Vector3 bottomPosition = symbol.reel.getSymbolPositionForSymbolAtIndex(0, 0, isUsingVisibleSymbolIndex: true, isLocal: true);

		bool isGoingTowardsTop = true;
		float elapsedTime = 0.0f;
		Vector3 targetPosition = (isGoingTowardsTop) ? topPosition : bottomPosition;
		Vector3 currentPosition = symbol.gameObject.transform.localPosition - symbol.info.positioning;
		Vector3 positionDelta = targetPosition - currentPosition;
		float timeToMove = Mathf.Abs(positionDelta.y / SYMBOL_MOVEMENT_SPEED);

		while (true)
		{
			while (elapsedTime < timeToMove)
			{
				symbol.getAnimator().positioning = currentPosition + (elapsedTime / timeToMove) * positionDelta;
				yield return null;
				elapsedTime += Time.deltaTime;
			}

			symbol.getAnimator().positioning = targetPosition;
			currentPosition = symbol.gameObject.transform.localPosition - symbol.info.positioning;

			// switch directions
			isGoingTowardsTop = !isGoingTowardsTop;
			elapsedTime = 0.0f;
			targetPosition = (isGoingTowardsTop) ? topPosition : bottomPosition;
			positionDelta = targetPosition - currentPosition;
			timeToMove = Mathf.Abs(positionDelta.y / SYMBOL_MOVEMENT_SPEED);
		}
	}

	protected override IEnumerator mutateBaseSymbols(int specificReelID = -1)
	{
		List<TICoroutine> symbolChangeCoroutines = new List<TICoroutine>();

		for (int i = 0; i < currentStickySymbols.Count; i++)
		{
			// make sure we only mutate if the sticky is showing, otherwise we can assume the sticky has already been converted
			bool shouldMutate = false;
			if (attachStickiesToReels)
			{
				if (currentStickySymbols[i].gameObject.activeSelf)
				{
					shouldMutate = true;
				}
			}
			else
			{
				if (stickySymbolsParent != null && stickySymbolsParent.activeSelf)
				{
					shouldMutate = true;
				}
			}

			if (shouldMutate)
			{
				symbolChangeCoroutines.Add(StartCoroutine(mutateSlidingStickyToVisibleSymols(currentStickySymbols[i])));
			}
		}

		if (!attachStickiesToReels)
		{
			// just turn off the parent of all the sticky symbols
			stickySymbolsParent.SetActive(false);
		}

		if (symbolChangeCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolChangeCoroutines));
		}
	}

	// mutate the sliding sticky to the visible symbols underneath
	private IEnumerator mutateSlidingStickyToVisibleSymols(SlotSymbol symbol)
	{
		int reelID = symbol.reel.reelID - 1;
		int position = symbol.visibleSymbolIndex;

		string symbolName = symbol.serverName;
		if (STICKY_SYMBOL_NAME_POSTFIX != "")
		{
			symbolName = symbolName.Replace(STICKY_SYMBOL_NAME_POSTFIX, "");
		}

		SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(reelID)[position];

		if (attachStickiesToReels)
		{
			// if the sticky is attached to the reels hide it now that we've replaced it on the reels
			symbol.gameObject.SetActive(false);
		}

		if (AFTER_SYMBOL_SLIDE_MUTATE_POSTFIX != "")
		{
			// do a mutation animation first
			visibleSymbol.mutateTo(symbolName + AFTER_SYMBOL_SLIDE_MUTATE_POSTFIX,  null, false, true);

			if (isChangingMutatedStuckSymbolLayers)
			{
				CommonGameObject.setLayerRecursively(visibleSymbol.gameObject, (int)changedMutatedStuckSymbolLayer);
			}

			if (playSeparateMutateSoundOnNewSticky && newStickyAddedThisSpin)
			{
				if (!string.IsNullOrEmpty(NEW_SYMBOL_MUTATE_SOUND))
				{
					if (!playAfterSymbolSlideMutateSoundOnlyOnce || (playAfterSymbolSlideMutateSoundOnlyOnce && !hasPlayedAfterSymbolSlideMutateSound))
					{
						Audio.playSoundMapOrSoundKey(NEW_SYMBOL_MUTATE_SOUND);
						hasPlayedAfterSymbolSlideMutateSound = true;
					}
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(AFTER_SYMBOL_SLIDE_MUTATE_SOUND))
				{
					if (!playAfterSymbolSlideMutateSoundOnlyOnce || (playAfterSymbolSlideMutateSoundOnlyOnce && !hasPlayedAfterSymbolSlideMutateSound))
					{
						Audio.playSoundMapOrSoundKey(AFTER_SYMBOL_SLIDE_MUTATE_SOUND);
						hasPlayedAfterSymbolSlideMutateSound = true;
					}
				}
			}
			yield return StartCoroutine(visibleSymbol.playAndWaitForAnimateOutcome());
		}

		// mutate to the standard version of symbol
		if (reelGame.engine.getVisibleSymbolsAt(reelID)[position].name != symbolName)
		{
			visibleSymbol.mutateTo(symbolName, null, false, true);

			if (isChangingMutatedStuckSymbolLayers)
			{
				CommonGameObject.setLayerRecursively(visibleSymbol.gameObject, (int)changedMutatedStuckSymbolLayer);
			}
		}
	}

	// loop the outcome animation until the sticky stops moving around
	private IEnumerator loopOutcomeAnimationWhileSymbolMoving(SlotSymbol symbol)
	{
		while (true)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateOutcome());
		}
	}

	// handle fading in/out objects as part of the background effects anims
	private IEnumerator fadeObjectsInOrOutForBackgroundEffectsAnims(float fadeTime, bool isFadingOut)
	{
		TICoroutine objectFadeCoroutine = StartCoroutine(handleObjectListFadeForBackgroundEffectsAnims(fadeTime, isFadingOut));

		AnimationListController.AnimationInformationList fadeAnims = (isFadingOut) ? fadeOutAnimsForNewStickySymbolEffectsAnims : fadeInAnimsForNewStickySymbolEffectsAnims;

		if (fadeAnims.Count > 0)
		{
			List<TICoroutine> objectFadeCoroutineList = new List<TICoroutine>();
			objectFadeCoroutineList.Add(objectFadeCoroutine);
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(fadeAnims, objectFadeCoroutineList));
		}
		else
		{
			// no anims to play so just wait on the object fades
			yield return objectFadeCoroutine;
		}
	}

	// handle fading the list of objects that need to change when the background effects anim plays (includes symbols as option)
	private IEnumerator handleObjectListFadeForBackgroundEffectsAnims(float fadeTime, bool isFadingOut)
	{
		float elapsedTime = 0.0f;

		while (elapsedTime < fadeTime)
		{
			elapsedTime += Time.deltaTime;

			float alphaValue = (isFadingOut) ? 1 - (elapsedTime / fadeTime) : (elapsedTime / fadeTime);

			for (int fadeIndex = 0; fadeIndex < objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Count; fadeIndex++)
			{
				CommonGameObject.alphaGameObject(objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims[fadeIndex], alphaValue);
			}

			yield return null;
		}

		float endAlphaValue = (isFadingOut) ? 0.0f : 1.0f;

		for (int fadeIndex = 0; fadeIndex < objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Count; fadeIndex++)
		{
			CommonGameObject.alphaGameObject(objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims[fadeIndex], endAlphaValue);
		}
	}

	// Spawn an effects object that will be moved from objectForNewStickySymbolEffectsSpawnLocation to the location of the new sticky
	private IEnumerator createAndMoveEffectObjectToSymbolSpawnLocation(float startDelay, StandardMutation.ReplacementCell replaceCell)
	{
		numberOfNewStickyEffectObjectsAnimating++;
	
		if (startDelay != 0.0f)
		{
			yield return new TIWaitForSeconds(startDelay);
		}

		GameObject effectsObject = newStickySpawnEffectCacher.getInstance();
		spawnedNewStickyEffectObjects.Add(effectsObject);
		SlotReel stickySymbolReel = reelGame.engine.getSlotReelAt(replaceCell.reelIndex);
		effectsObject.transform.parent = stickySymbolReel.getReelGameObject().transform;
		effectsObject.transform.position = objectForNewStickySymbolEffectsSpawnLocation.position;
		CommonGameObject.setLayerRecursively(effectsObject, Layers.ID_SLOT_OVERLAY);
		effectsObject.SetActive(true);

		Vector3 symbolPosOffset = Vector3.zero;
		SymbolInfo targetSymbolInfo = reelGame.findSymbolInfo(replaceCell.replaceSymbol);
		if (targetSymbolInfo != null)
		{
			symbolPosOffset = targetSymbolInfo.positioning;
		}

		Vector3 targetPosition = stickySymbolReel.getSymbolPositionForSymbolAtIndex((stickySymbolReel.reelData.visibleSymbols - 1) - replaceCell.symbolIndex, 0, isUsingVisibleSymbolIndex: true, isLocal: true) + symbolPosOffset;
		
		iTween.MoveTo(effectsObject, iTween.Hash("position", targetPosition, "islocal", true, "time", OBJECT_FOR_NEW_STICKY_SYMBOL_EFFECT_MOVE_DUR, "easetype", iTween.EaseType.linear, "oncompletetarget", this.gameObject, "oncomplete", "moveNewStickyObjectComplete"));
	}

	// Hook to iTween completion so that this object can be waited on in a coroutine
	private void moveNewStickyObjectComplete()
	{
		numberOfNewStickyEffectObjectsAnimating--;
	}

	// fade out anything that needs fading, play the background effects anims, and then fade everything back in
	private IEnumerator playNewStickiesBackgroundEffectsAnims(List<StandardMutation> stickyMutationsList)
	{
		int startingObjectsToFadeCount = objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Count;

		if (shouldFadeSymbolsForNewStickyBackgroundEffectsAnims)
		{
			SlotReel[] slotReels = reelGame.engine.getAllSlotReels();
			for (int reelIndex = 0; reelIndex < slotReels.Length; reelIndex++)
			{
				SlotReel reel = slotReels[reelIndex];

				List<SlotSymbol> symbolList = reel.symbolList;
				for (int symbolIndex = 0; symbolIndex < symbolList.Count; symbolIndex++)
				{	
					SlotSymbol symbol = symbolList[symbolIndex];

					if (symbol.animator != null)
					{
						if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
						{
							symbol.mutateToFlattenedVersion();
						}
						objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Add(symbol.gameObject);
					}
				}
			}

			// put the stickies in as well since we don't want them hanging around during the background effect
			for (int stickyIndex = 0; stickyIndex < currentStickySymbols.Count; stickyIndex++)
			{
				SlotSymbol currentSticky = currentStickySymbols[stickyIndex];

				if (currentSticky.animator != null)
				{
					currentSticky.mutateToFlattenedVersion();
					objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Add(currentSticky.gameObject);
				}
			}
		}

		yield return StartCoroutine(fadeObjectsInOrOutForBackgroundEffectsAnims(OBJECT_FADE_TIME_FOR_BACKGROUND_EFFECTS_ANIMS, isFadingOut: true));

		// grant the extra spins here since they are implied to be awarded by the background animation, don't yield since they need to award during the animation
		if (!areFreeSpinGranted)
		{
			int totalFreespinsGranted = 0;
			for (int mutationIndex = 0; mutationIndex < stickyMutationsList.Count; mutationIndex++)
			{
				totalFreespinsGranted += stickyMutationsList[mutationIndex].numberOfFreeSpinsAwarded;
			}

			StartCoroutine(grantFreeSpins(totalFreespinsGranted, GRANT_FREESPINS_DELAY));
		}

		if (objectForNewStickySymbolEffectsPrefab != null)
		{
			// create the effect that will be moved from spawn location to where the new symbol will be on the reels
			List<StandardMutation.ReplacementCell> triggerSymbolReplacementCells = convertMutationTriggerSymbolNamesToReplacementCellList(stickyMutationsList);
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			for (int i = 0; i < triggerSymbolReplacementCells.Count; i++)
			{
				StandardMutation.ReplacementCell replaceCell = triggerSymbolReplacementCells[i];

				// add the symbol at the target location to the list of symbols to be restored once the next spin is starting
				if (isRestoringOriginalSymbols)
				{
					int visibleSymbolIndex = (reelArray[replaceCell.reelIndex].reelData.visibleSymbols - 1) - replaceCell.symbolIndex;
					SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(replaceCell.reelIndex)[visibleSymbolIndex];

					if (visibleSymbol != null)
					{
						StandardMutation.ReplacementCell newCell = new StandardMutation.ReplacementCell();
						newCell.reelIndex = replaceCell.reelIndex;
						newCell.symbolIndex = visibleSymbolIndex;
						newCell.replaceSymbol = visibleSymbol.name;

						originalSymbolList.Add(newCell);
					}
				}

				StartCoroutine(createAndMoveEffectObjectToSymbolSpawnLocation(OBJECT_FOR_NEW_STICKY_SYMBOL_EFFECT_MOVE_DELAY, replaceCell));

				// if the looped symbol movement sound isn't started, start it now
				if (symbolMovingPlayingAudio == null)
				{
					if (!string.IsNullOrEmpty(SYMBOL_SLIDE_OR_MOVE_SOUND))
					{
						symbolMovingPlayingAudio = Audio.playSoundMapOrSoundKey(SYMBOL_SLIDE_OR_MOVE_SOUND);
					}
				}
			}
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(newStickySymbolBackgroundEffectsAnims));

		// wait for the new spawning animation to finish reaching their destinations
		while (numberOfNewStickyEffectObjectsAnimating > 0)
		{
			yield return null;
		}

		// stop the looped movement sound
		if (symbolMovingPlayingAudio != null)
		{
			Audio.stopSound(symbolMovingPlayingAudio, 0);
			symbolMovingPlayingAudio = null;
		}

		yield return StartCoroutine(fadeObjectsInOrOutForBackgroundEffectsAnims(OBJECT_FADE_TIME_FOR_BACKGROUND_EFFECTS_ANIMS, isFadingOut: false));

		StartCoroutine(removeNewStickyEffectObjects());

		if (shouldFadeSymbolsForNewStickyBackgroundEffectsAnims)
		{
			// remove the symbols from the list of fade objects since we'll need to grab the new set of symbols the next time
			objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.RemoveRange(startingObjectsToFadeCount, objectsToFadeDuringNewStickySymbolBackgroundEffectsAnims.Count - startingObjectsToFadeCount);
		}
	}

	private IEnumerator removeNewStickyEffectObjects()
	{
		yield return null; // delayed to prevent blank visual hiccup

		// Hide all of the animated objects that were spawned
		for (int i = 0; i < spawnedNewStickyEffectObjects.Count; i++)
		{
			newStickySpawnEffectCacher.releaseInstance(spawnedNewStickyEffectObjects[i]);
		}
		spawnedNewStickyEffectObjects.Clear();
	}
}
