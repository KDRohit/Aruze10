using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//
// Animates symbol banners across the the screen like a horizontal spin reel.
// When the symbols land on a reel, this symbols underneath are changed
// into wild symbols or the symbol banner attaches to the reel.
// Used in gen75
//
// Animation states
// Spinning : Keep spinning and adding symbols until it's time to slow down
// Slowing : Slow down the reel to its anticipation speed
// Anticipating : Let the reel keep spinning slowly until it's time to add the stop symbols
// WaitingToAddStopSymbols : Anticipation is finished and we have to add in the final stop symbols where there is space enough
// Ending : Keep spinning until the stop symbols are in position
// Stopped : After all spin completes it enter this final state
//
// Author : nick saito <nsaito@zynga.com>
// Date : march 9th, 2018
//
public class HorizontalWildsReplacementModule : SlotModule
{
	#region public
	[Header("HorizontalWildsReplacement Settings")]
	[SerializeField] private Camera symbolCamera;
	[SerializeField] private string symbolName;
	[SerializeField] private float offScreenBuffer;
	[SerializeField] private Layers.LayerID overReelsLayerId;
	[SerializeField] private Layers.LayerID attachedToReelsLayerId;
	[SerializeField] private Transform symbolParent;

	[Header("Horizontal Animation")]
	[SerializeField]private Direction direction;
	[SerializeField] private float horizontalMoveSpeed;

	[Range(0, 10)] [SerializeField] private int minSymbolClusterSize;
	[Range(0, 10)] [SerializeField] private int maxSymbolClusterSize;
	[Range(0,100)] [SerializeField] private int chanceToAddSpaceInCluster;
	[Range(0, 10)] [SerializeField] private int minNumberOfSpaceBetweenSymbolClusters;
	[Range(0, 10)] [SerializeField] private int maxNumberOfSpaceBetweenSymbolClusters;
	[SerializeField] private float reelSpinTime;

	[Header("Slowing Animation")]
	[SerializeField] private iTween.EaseType slowingEase;
	[SerializeField] private float timeToSlow;
	[SerializeField] private float slowSpeed;

	[Header("Stopping Animation")]
	[SerializeField] private float timeToStop;
	[SerializeField] private bool shouldLoopSymbolAnimationsOnRollup;

	[Header("Feature animations")]
	[SerializeField] private AnimationListController.AnimationInformationList introAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList outroAnimations;

	[Header("Feature Audio")]
	[SerializeField] private string featureBackgroundMusic = "basegame_vertical_wild_bg";
	#endregion

	#region private
	private List<HorizontalReelReEvaluation> horizontalReelReEvaluations;
	private List<SymbolModel> spinningSymbols;
	private List<SymbolModel> symbolsToRelease;
	private float timeSpinning;
	private float screenLeftEdge;
	private float screenRightEdge;
	private float currentHorizontalMoveSpeed;
	private bool stopSymbolsAdded;
	private SpinState spinState;
	private int numberOfReels;
	private float symbolWidth;
	private int numberOfPostionsToMoveUntilNextClusterIsCreated;
	private int previousReelPosition;
	private float maxDeltaX;
	private TICoroutine symbolAnimationCoroutine;
	
	// these are used for scaling and positioning symbols
	private SymbolInfo symbolInfo;
	private SlotReel[] slotReels;
	private	int numVisibleSymbols;
	private float spawnYPosition;

	// Constants used to initialize maxDeltaX. These are arbitrarily large since
	// maxDeltaX tracks the max horizontal movement for the next spin for efficiency,
	// and on the first time through we will never be close to the stop position.
	private const float INIT_MAX_DELTA_LEFT = 10000.0f;
	private const float INIT_MAX_DELTA_RIGHT = -10000.0f;

	// enums
	private enum SpinState
	{
		Spinning,
		Slowing,
		Anticipating,
		WaitingToAddStopSymbols,
		Ending,
		Stopped
	}

	public enum Direction
	{
		LeftToRight = 1,
		RightToLeft = -1
	}

	#endregion

	#region Animation Loop
	// Animate the symbols across the screen
	IEnumerator animateHorizontalReel()
	{
		float horizontalDistanceMoved = 0.0f;
		previousReelPosition = 0;
		numberOfPostionsToMoveUntilNextClusterIsCreated = 0;
		maxDeltaX = (direction == Direction.LeftToRight) ? INIT_MAX_DELTA_LEFT : INIT_MAX_DELTA_RIGHT;

		while (spinState != SpinState.Stopped)
		{
			// move the symbols
			float deltaX = calculateDeltaX();
			updateSymbolPositions(deltaX);
			horizontalDistanceMoved += Mathf.Abs(deltaX);

			// need to check if we locking in the symbols here since each change in deltaX
			// can be small amount
			if (spinState == SpinState.Ending && stopSymbolsAreLocked())
			{
				stopHorizontalReel();
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(outroAnimations));
				yield return StartCoroutine(animateLockedSymbolsOverReels());
				playOutcomeAnimations();
				restoreBackgroundMusic();
				spinState = SpinState.Stopped;				
			}

			// calculate the reel position
			int reelPosition = (int)Mathf.Ceil(horizontalDistanceMoved / symbolWidth);

			if (reelPosition != previousReelPosition && spinState != SpinState.Stopped)
			{
				int amountPositionChanged = reelPosition - previousReelPosition;
				numberOfPostionsToMoveUntilNextClusterIsCreated -= amountPositionChanged;
				advanceReelPosition(reelPosition);
				tryToAddSymbolCluster(reelPosition);
				previousReelPosition = reelPosition;
			}

			timeSpinning += Time.deltaTime;
			yield return null;
		}
	}

	private float calculateDeltaX()
	{
		float deltaX = Time.deltaTime * currentHorizontalMoveSpeed * (float)direction;

		switch (direction)
		{
			case Direction.LeftToRight:
				if (deltaX > maxDeltaX)
				{
					deltaX = maxDeltaX;
				}
				break;
			case Direction.RightToLeft:
				if (deltaX < maxDeltaX)
				{
					deltaX = maxDeltaX;
				}
				break;
		}

		return deltaX;
	}

	// try to add a new cluster of symbols to the reel if there is space enough
	// and we're not waiting to add the final stop symbols.
	private void tryToAddSymbolCluster(int reelPosition)
	{
		if (spinState != SpinState.WaitingToAddStopSymbols)
		{
			if (numberOfPostionsToMoveUntilNextClusterIsCreated <= 0)
			{
				addSymbolCluster(reelPosition);
			}
		}
	}

	private void advanceReelPosition(int reelPosition)
	{
		switch (spinState)
		{
			case SpinState.Spinning:
				//keep spinning the reel until it's time to slow things down
				if (timeSpinning >= reelSpinTime || reelGame.engine.isSlamStopPressed)
				{
					slowHorizontalReel();
				}
				break;
			case SpinState.Slowing:
				if (reelGame.engine.isSlamStopPressed)
				{
					spinState = SpinState.WaitingToAddStopSymbols;
				}
				break;
			case SpinState.Anticipating:
				//keep spinning the reel until it's time to add the stop symbols
				if (timeSpinning >= timeToStop || reelGame.engine.isSlamStopPressed)
				{
					spinState = SpinState.WaitingToAddStopSymbols;
				}
				break;
			case SpinState.WaitingToAddStopSymbols:
				// This spin done with anticipation and want to add in the real symbols that will land in the
				// right place. We just have to make sure any symbols spawned on the left edge
				// have moved out of the way first.
				if (!stopSymbolsAdded && numberOfPostionsToMoveUntilNextClusterIsCreated <= 0)
				{
					addStopSymbolsToReel();
					stopSymbolsAdded = true;
					spinState = SpinState.Ending;
				}
				break;
		}
	}

	private bool stopSymbolsAreLocked()
	{
		foreach (SymbolModel spinningSymbol in spinningSymbols)
		{
			if (spinningSymbol.shouldAttachToReel)
			{
				switch (direction)
				{
					case Direction.LeftToRight:
						if (spinningSymbol.symbolTranform.position.x >= spinningSymbol.symbolEndPosition.x)
						{
							return true;
						}
						break;
					case Direction.RightToLeft:
						if (spinningSymbol.symbolTranform.position.x <= spinningSymbol.symbolEndPosition.x)
						{
							return true;
						}
						break;
				}
			}
		}

		return false;
	}

	private void addSymbolCluster(int reelPosition)
	{
		int numberOfSymbolsInCluster = createSymbolCluster();
		int randomClusterSpace = Random.Range(minNumberOfSpaceBetweenSymbolClusters, maxNumberOfSpaceBetweenSymbolClusters);
		numberOfPostionsToMoveUntilNextClusterIsCreated = randomClusterSpace + numberOfSymbolsInCluster;
	}

	// create a random cluster of symbols to animate on the horizontal reel
	private int createSymbolCluster()
	{
		int numberOfSlotsInCluster = Random.Range(minSymbolClusterSize, maxSymbolClusterSize);

		for (int i = 0; i < numberOfSlotsInCluster; i++)
		{
			float startX = 0.0f;
			float stopX = 0.0f;

			switch (direction)
			{
				case Direction.LeftToRight:
					startX = screenLeftEdge - offScreenBuffer - symbolWidth * i;
					stopX = screenRightEdge + offScreenBuffer;
					break;
				case Direction.RightToLeft:
					startX = screenRightEdge + offScreenBuffer + symbolWidth * i;
					stopX = screenLeftEdge - offScreenBuffer;
					break;
			}

			// chanceToAddSymbolInCluster creates spaces within cluster so they
			// arn't always solid chunks
			if(shouldAddSymbolInCluster(i, numberOfSlotsInCluster))
			{
				spawnSymbolAtPosition(
					new Vector3(startX, spawnYPosition, 0f),
					new Vector3(stopX, spawnYPosition, 0f)
				);
			}
		}

		return numberOfSlotsInCluster;
	}

	// adds a bit of randomness into a cluster by checking against chanceToAddSpaceInCluster
	// to see if we really should add the next symbol into the cluster. The first and last
	// symbols are always added. 
	private bool shouldAddSymbolInCluster(int symbolPosition, int numberOfSlotsInCluster)
	{
		if(symbolPosition == 0 || symbolPosition == (numberOfSlotsInCluster - 1))
		{
			//always add the first and last symbol
			return true;
		}
		else if (Random.Range(0, 100) < chanceToAddSpaceInCluster)
		{
			//create a space in the cluster
			return false;
		}
		else
		{
			return true;
		}
	}

	// moves symbols on our horizontal reel across the screen
	// and release any symbols that have gone off the screen
	private void updateSymbolPositions(float deltaX)
	{
		foreach (SymbolModel spinningSymbol in spinningSymbols)
		{
			spinningSymbol.moveXPositionBy(deltaX);
			updateMaxDeltaX(spinningSymbol);

			switch (direction)
			{
				case Direction.LeftToRight:
					if (spinningSymbol.symbolPosition.x > (screenRightEdge + offScreenBuffer))
					{
						symbolsToRelease.Add(spinningSymbol);
					}
					break;
				case Direction.RightToLeft:
					if (spinningSymbol.symbolPosition.x < (screenLeftEdge - offScreenBuffer))
					{
						symbolsToRelease.Add(spinningSymbol);
					}
					break;
			}
		}

		foreach (SymbolModel spinningSymbol in symbolsToRelease)
		{
			releaseSpinningSymbol(spinningSymbol);
		}

		symbolsToRelease.Clear();
	}

	// keep track of max deltaX symbols can move, so they never move past
	// their stop position subsequent frames.
	private void updateMaxDeltaX(SymbolModel symbolModel)
	{
		if (symbolModel.shouldAttachToReel)
		{
			maxDeltaX = symbolModel.symbolEndPosition.x - symbolModel.symbolPosition.x;

			switch (direction)
			{
				case Direction.LeftToRight:
					if (maxDeltaX < 0.0f)
					{
						maxDeltaX = 0.0f;
					}
					break;
				case Direction.RightToLeft:
					if (maxDeltaX > 0.0f)
					{
						maxDeltaX = 0.0f;
					}
					break;
			}
		}
	}

	private void restoreBackgroundMusic()
	{
		if (reelGame.isFreeSpinGame())
		{
			playBackgroundMusic("freespin");
		}
		else
		{
			playBackgroundMusic(reelGame.BASE_GAME_BG_MUSIC_KEY);
		}
	}

	private void playBackgroundMusic(string musicKey)
	{
		if (Audio.canSoundBeMapped(musicKey) && Audio.soundMap(musicKey) != Audio.defaultMusicKey)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(musicKey));
		}
	}
	#endregion

	#region slot module overrides
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		spinningSymbols = new List<SymbolModel>();
		symbolsToRelease = new List<SymbolModel>();
		horizontalReelReEvaluations = new List<HorizontalReelReEvaluation>();
		screenLeftEdge = symbolCamera.ScreenToWorldBounds().min.x;
		screenRightEdge = symbolCamera.ScreenToWorldBounds().max.x;
		numberOfReels = reelGame.engine.getReelArray().Length - 1;
	}

	public override bool needsToExecuteOnReelsSpinning()
	{
		return true;
	}

	public override IEnumerator executeOnReelsSpinning()
	{
		stopOutcomeAnimations();
		yield return StartCoroutine(attachSymbolsToReels());
		resetHorizontalReelSettings();
		enableSlotReelStopSounds();
	}

	// When we start we always want all the reel stops to play the stop sound
	// until we have the ReEvaluation data
	private void enableSlotReelStopSounds()
	{
		foreach (SlotReel slotReel in reelGame.engine.getAllSlotReels())
		{
			slotReel.shouldPlayReelStopSound = true;
		}
	}

	// Stop the reel_stop sound from playing on reels that will be covered up
	// by a wild banner
	private void disableSlotReelStopSounds()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (HorizontalReelReEvaluation reEvaluation in horizontalReelReEvaluations)
		{
			foreach (int reelId in reEvaluation.reelStops)
			{
				reelArray[reelId].shouldPlayReelStopSound = false;
			}
		}
	}

	// This is the first time that the outcome exists. We extract the reEvaluations from
	// the outcome and determine if horizontal wilds need to be added and animated.
	public override bool needsToExecutePreReelsStopSpinning()
	{
		horizontalReelReEvaluations.Clear();

		SlotOutcome slotOutcome = reelGame.getCurrentOutcome();

		if (slotOutcome != null)
		{
			// extract the reEvaluations into a list
			JSON[] reEvalArray = slotOutcome.getArrayReevaluations();
			foreach (JSON reEvalJSON in reEvalArray)
			{
				HorizontalReelReEvaluation reEvaluation = new HorizontalReelReEvaluation(reEvalJSON);
				if (reEvaluation.isActive && reEvaluation.type == "horizontal_reel_vert_wild")
				{
					horizontalReelReEvaluations.Add(new HorizontalReelReEvaluation(reEvalJSON));
				}
			}

			// Check if we have any horizontal reel to spin
			return horizontalReelReEvaluations.Count > 0;
		}

		return false;
	}

	// Play the intro animations for the horizontal reel feature.
	// Start the horizontal reel animating.
	public override IEnumerator executePreReelsStopSpinning()
	{
		//calculate symbolWidth everytime in case the reel size changes
		symbolWidth = calculateSymbolWidth();
		spawnYPosition = calculateYSpawnPosition();
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimations));
		startHorizontalReelAnimation();

		if (featureBackgroundMusic != "")
		{
			playBackgroundMusic(featureBackgroundMusic);
		}
		
		disableSlotReelStopSounds();

		while (spinState != SpinState.Stopped)
		{
			yield return null;
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return stopSymbolsAdded;
	}

	// When the reels finally stop, we mutate the symbols underneath the banners to be
	// the correct symbol and play their animations, and also turn the old symbols off.
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		//wait for final symbols to arrive in place
		while (spinState != SpinState.Stopped)
		{
			yield return null;
		}

		skipVisibleSymbolsAnimationsOnWildReels();
	}

	// mutate the symbols under the wilds into wilds and set them to the correct
	// layer before they spin off the stage.
	private IEnumerator attachSymbolsToReels()
	{
		foreach (SymbolModel symbolModel in spinningSymbols)
		{
			if (symbolModel.shouldAttachToReel)
			{
				SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(symbolModel.reelId)[0];
				visibleSymbol.mutateTo(symbolName);
				visibleSymbol.mutateToFlattenedVersion();
				CommonGameObject.setLayerRecursively(visibleSymbol.gameObject, (int) attachedToReelsLayerId);
			}
		}

		yield break;
	}

	private IEnumerator animateLockedSymbolsOverReels()
	{
		float delayOutcomeAnimation = 0.0f;
		foreach (SymbolModel symbolModel in spinningSymbols)
		{
			if (symbolModel.shouldAttachToReel)
			{
				delayOutcomeAnimation = symbolModel.slotSymbol.info.customAnimationDurationOverride;

				//this is a hack to reset the animator back to the entry state
				symbolModel.saveCurrentPosition();
				symbolModel.slotSymbol.mutateToUnflattenedVersion();
				// make sure that we parent the slotSymbol under the current game, since it might be under the base game in the symbol cache still
				symbolModel.slotSymbol.gameObject.transform.SetParent(symbolParent);
				symbolModel.slotSymbol.gameObject.SetActive(false);
				symbolModel.slotSymbol.gameObject.SetActive(true);
				symbolModel.slotSymbol.animateAnticipation();
				
				//mutating and animating the symbol forces its position to Vector3.zero and also
				//may actually give us a different symbol, so let's just fix it up again here.
				symbolModel.restoreToSavedPosition();
				fixSymbolScaleAndPosition(symbolModel);
				
				CommonGameObject.setLayerRecursively(symbolModel.slotSymbol.gameObject, (int) overReelsLayerId);
			}
		}

		yield return new WaitForSeconds(delayOutcomeAnimation);
	}

	// play the outcome animations on the symbols that were attached
	private void playOutcomeAnimations()
	{
		if (shouldLoopSymbolAnimationsOnRollup)
		{
			stopOutcomeAnimations();
			symbolAnimationCoroutine = StartCoroutine(playOutcomeAnimationsUntilNextSpin());
		}
		else
		{
			playOutcomeAnimationsForSymbols();
		}
	}

	private void stopOutcomeAnimations ()
	{
		if (symbolAnimationCoroutine != null)
		{
			StopCoroutine(symbolAnimationCoroutine);
		}
	}

	private void playOutcomeAnimationsForSymbols()
	{
		foreach (SymbolModel symbolModel in spinningSymbols)
		{
			if (symbolModel.shouldAttachToReel && !symbolModel.slotSymbol.isAnimating)
			{
				symbolModel.saveCurrentPosition();
				symbolModel.slotSymbol.animateOutcome();
				symbolModel.restoreToSavedPosition();
				fixSymbolScaleAndPosition(symbolModel);
				CommonGameObject.setLayerRecursively(symbolModel.slotSymbol.gameObject, (int) overReelsLayerId);
			}
		}
	}

	// call animateOutcome and then wait until the symbol says it isn't animating anymore
	private IEnumerator playOutcomeAnimationsUntilNextSpin()
	{
		playOutcomeAnimationsForSymbols();

		bool stillAnimatingSymbols = true;
		while (stillAnimatingSymbols)
		{
			stillAnimatingSymbols = isStillAnimatingSymbols();
			yield return null;
		}

		playOutcomeAnimations();
	}

	private bool isStillAnimatingSymbols()
	{
		foreach (HorizontalReelReEvaluation reEvaluation in horizontalReelReEvaluations)
		{
			foreach (int reelId in reEvaluation.reelStops)
			{
				SlotSymbol visibleSymbol = reelGame.engine.getVisibleSymbolsAt(reelId)[0];
				if (visibleSymbol.animator.isAnimating)
				{
					return true;
				}
			}
		}

		return false;
	}

	#endregion

	#region Horizontal Symbol Animations
	private void startHorizontalReelAnimation()
	{
		spinState = SpinState.Spinning;
		StartCoroutine(animateHorizontalReel());
	}

	private void resetHorizontalReelSettings()
	{
		stopSymbolsAdded = false;
		timeSpinning = 0.0f;
		currentHorizontalMoveSpeed = horizontalMoveSpeed;
		returnAllSymbolsToCache();
	}

	// 'spawnPosition' is where to instantiate the symbol.
	// 'stopPosition' is the final position the symbol should stop when the reel stops spinning
	private SymbolModel spawnSymbolAtPosition(Vector3 spawnPosition, Vector3 stopPosition)
	{
		SlotSymbol slotSymbol = new SlotSymbol(reelGame);
		slotSymbol.setupSymbol(symbolName, 0, null, allowFlattenedSymbolSwap: true);
		CommonGameObject.setLayerRecursively(slotSymbol.gameObject, (int) overReelsLayerId);
		
		SymbolModel symbolModel = new SymbolModel(slotSymbol);
		symbolModel.symbolEndPosition = stopPosition;
		symbolModel.symbolTranform.position = spawnPosition;

		symbolModel.saveCurrentPosition();
		fixSymbolScaleAndPosition(symbolModel);
		spinningSymbols.Add(symbolModel);
		return symbolModel;
	}

	// To keep calculations simple we move the symbol under a reel, set it's local scale and position
	// and then move it back out to get the correct size and position relative to a scaled reel.
	private void fixSymbolScaleAndPosition(SymbolModel symbolModel)
	{
		// parent the symbol and scale and position relative to the reel
		symbolModel.slotSymbol.gameObject.transform.SetParent(slotReels[0].getReelGameObject().transform);
		symbolModel.slotSymbol.gameObject.transform.localScale = symbolInfo.scaling;
		symbolModel.symbolTranform.localPosition = new Vector3(symbolInfo.positioning.x, spawnYPosition, symbolInfo.positioning.z);
		
		// remove the parent and use the newly scaled values to set position properly
		symbolModel.slotSymbol.gameObject.transform.SetParent(symbolParent);
		symbolModel.symbolTranform.position = new Vector3(symbolModel.symbolPosition.x, symbolModel.symbolTranform.position.y, symbolModel.symbolPosition.z);
		symbolModel.saveCurrentPosition();
		symbolModel.symbolEndPosition.y = symbolModel.symbolPosition.y;
	}

	// Remove the symbol from our list of symbols that are spinning on the horizontal reel
	// and return it to the symbol cache.
	private void releaseSpinningSymbol(SymbolModel symbolToRelease)
	{
		int symbolIndex = spinningSymbols.IndexOf(symbolToRelease);

		if (symbolIndex >= 0)
		{
			spinningSymbols.RemoveAt(symbolIndex);
		}

		symbolToRelease.releaseSymbol();
	}

	private void returnAllSymbolsToCache()
	{
		symbolsToRelease.AddRange(spinningSymbols);

		foreach (SymbolModel symbolModel in symbolsToRelease)
		{
			releaseSpinningSymbol(symbolModel);
		}

		symbolsToRelease.Clear();
		spinningSymbols.Clear();
	}

	// Add the final symbols to horizontal reels that will be animated to the final end position.
	// Once at the end position, they will not go any further.
	private void addStopSymbolsToReel()
	{
		float symbolOffset = getSymbolOffset();

		foreach (HorizontalReelReEvaluation reEvaluation in horizontalReelReEvaluations)
		{
			foreach (int reelId in reEvaluation.reelStops)
			{
				GameObject reelGameObject = reelGame.getReelRootsAt(reelId);
				float startX = 0.0f;

				switch (direction)
				{
					case Direction.LeftToRight:
						startX = reelGameObject.transform.position.x - symbolOffset;
						break;
					case Direction.RightToLeft:
						startX = reelGameObject.transform.position.x + symbolOffset;
						break;
				}

				SymbolModel symbolModel = spawnSymbolAtPosition(
					new Vector3(startX, spawnYPosition, 0f),
					new Vector3(reelGameObject.transform.position.x, spawnYPosition, 0f)
				);

				symbolModel.shouldAttachToReel = true;
				symbolModel.reelId = reelId;
			}
		}

		numberOfPostionsToMoveUntilNextClusterIsCreated = numberOfReels + 1 + Random.Range(minNumberOfSpaceBetweenSymbolClusters, maxNumberOfSpaceBetweenSymbolClusters);
	}

	// Calculate how far over to move stop symbols over to their offscreen position
	// when they are added to the reel and animated in to place
	private float getSymbolOffset()
	{
		GameObject reelGameObject;
		float symbolOffset = 0.0f;

		switch (direction)
		{
			case Direction.LeftToRight:
				reelGameObject = reelGame.getReelRootsAt(numberOfReels);
				symbolOffset = reelGameObject.transform.position.x - screenLeftEdge + offScreenBuffer;
				break;
			case Direction.RightToLeft:
				reelGameObject = reelGame.getReelRootsAt(0);
				symbolOffset = screenRightEdge - reelGameObject.transform.position.x + symbolWidth + offScreenBuffer;
				break;
		}

		return symbolOffset;
	}

	// This start the process of stopping the reel
	private void slowHorizontalReel()
	{
		spinState = SpinState.Slowing;

		// If the reels were slammed, then we want to add the stop symbols as soon
		// as possible, so go straight into WaitingToAddStopSymbols state.
		if (reelGame.engine.isSlamStopPressed)
		{
			spinState = SpinState.WaitingToAddStopSymbols;
		}
		
		iTween.ValueTo(gameObject,
			iTween.Hash(
				"from", horizontalMoveSpeed,
				"to", slowSpeed,
				"time", timeToSlow,
				"onupdatetarget", gameObject,
				"onupdate", "slowDownReelCallBack",
				"easetype", slowingEase,
				"oncompletetarget", gameObject,
				"oncomplete", "slowDownReelComplete"
			)
		);
	}

	// slow down the horizontal reel
	private void slowDownReelCallBack(float newValue)
	{
		currentHorizontalMoveSpeed = newValue;
	}

	private void slowDownReelComplete()
	{
		if (!reelGame.engine.isSlamStopPressed)
		{
			spinState = SpinState.Anticipating;
		}

		timeSpinning = 0.0f;
	}

	// slowing down is complete and the horizontal reel
	// should stop update now.
	private void stopHorizontalReel()
	{
		//no matter what, at the end of the spin, the real symbols should
		//move to their final position so they can attach to the real reel.
		foreach (SymbolModel spinningSymbol in spinningSymbols)
		{
			if (spinningSymbol.shouldAttachToReel)
			{
				spinningSymbol.symbolTranform.position = spinningSymbol.symbolEndPosition;
				spinningSymbol.saveCurrentPosition();
			}
		}
	}

	// hide symbols behind the wild banners
	private void skipVisibleSymbolsAnimationsOnWildReels()
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (SymbolModel spinningSymbol in spinningSymbols)
		{
			if (spinningSymbol.shouldAttachToReel)
			{
				SlotSymbol[] visibleSymbols = reelArray[spinningSymbol.reelId].visibleSymbols;
				foreach(SlotSymbol visibleSymbol in visibleSymbols)
				{
					visibleSymbol.skipAnimationsThisOutcome();
				}
			}
		}
	}

	// Use the first two reels to calculate the width and spacing of symbols
	private float calculateSymbolWidth()
	{
		GameObject firstReel = reelGame.getReelRootsAt(0);
		GameObject secondReel = reelGame.getReelRootsAt(1);
		return Mathf.Abs(secondReel.transform.position.x - firstReel.transform.position.x);
	}

	private float calculateYSpawnPosition()
	{
		symbolInfo = reelGame.findSymbolInfo(symbolName);
		slotReels = reelGame.engine.getAllSlotReels();
		numVisibleSymbols = reelGame.engine.getVisibleSymbolsAt(0).Length - 1;
		return reelGame.getSymbolVerticalSpacingAt(0) * numVisibleSymbols + symbolInfo.positioning.y;
	}
	#endregion

	#region helper classes

	// a class to deserialize and hold data about the reeval for easy use and to separate it
	// from the actual job of the surrounding class.
	public class HorizontalReelReEvaluation
	{
		public string type;
		public bool isActive;
		public int[] reelStops;

		public HorizontalReelReEvaluation(JSON reEvaluationJSON)
		{
			type = reEvaluationJSON.getString("type", "");
			isActive = reEvaluationJSON.getBool("active", false);
			reelStops = reEvaluationJSON.getIntArray("wild_reels");
		}
	}

	// a class to hold position data for our animating symbols
	// to make updating easy it caches transform and positions
	// so we don't need to allocate each update loop.
	public class SymbolModel
	{
		public SlotSymbol slotSymbol;
		public Vector3 symbolPosition;
		public Vector3 symbolScale;
		public Vector3 symbolEndPosition;
		public Transform symbolTranform;
		public bool shouldAttachToReel;
		public int reelId;
		
		public SymbolModel(SlotSymbol newSlotSymbol)
		{
			slotSymbol = newSlotSymbol;
			symbolTranform = slotSymbol.transform;
			symbolPosition = symbolTranform.position;
		}

		/// stores the current position from the Transform
		public void saveCurrentPosition()
		{
			symbolPosition = symbolTranform.position;
		}

		/// caches the transform and sets the transform position to symbolPosition
		public void restoreToSavedPosition()
		{
			cacheTransform();
			symbolTranform.position = symbolPosition;
		}

		/// updates the symbolPosition and the Transform.position with it.
		public void moveXPositionBy(float deltaX)
		{
			symbolPosition.x += deltaX;
			symbolTranform.position = symbolPosition;
		}

		public void releaseSymbol()
		{
			slotSymbol.cleanUp();
		}

		// make sure we are still operating on the correct transform.
		// if the slotsymbol is mutated, the gameobject can change i think.
		private void cacheTransform()
		{
			if (symbolTranform != slotSymbol.transform)
			{
				symbolTranform = slotSymbol.transform;
			}
		}
	}

	// This is what the reeval looks like
	// "reevaluations": [{
	// "type": "horizontal_reel_vert_wild",
	// "horiz_reel_stop": "4",
	// "wild_reels": [
	//   "0",
	//   "2",
	//   "3",
	//   "4"
	// ],
	// "active": true,
	// "outcomes": [
	//   {
	//     "outcome_type": "line",
	//     "credits": "80",
	//     "win_id": "20127",
	//     "pay_line": "line_4x5_40_01",
	//     "uses_wild": false,
	//     "symbol": "F7"
	//   }
	//  }]
	#endregion
}