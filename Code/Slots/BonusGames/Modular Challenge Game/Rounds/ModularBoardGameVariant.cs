using System;
using System.Collections;
using System.Collections.Generic;
using FeatureOrchestrator;
using UnityEngine;

public class ModularBoardGameVariant : ModularPickingGameVariant
{
	private const string PROGRESS_TEXT_FORMAT = "{0}/{1}";

	// use this delegate to get callbacks to be forwarded to dialog/proton level 
	public delegate void BoardGameDelegate(Dict args = null);
	public event BoardGameDelegate onItemClick;
	
	// Invoked immediately when all spaces are landed
	public event BoardGameDelegate onRoundEnd;
	
	// Invoked after all board completion animations are completed and new round can begin
	public event BoardGameDelegate onBoardReadyForNextRound;
	
	// HIR-92844: An edge case where all spaces are landed but board complete overlay is not fired. Using this for userflow logging.
	public event BoardGameDelegate onBoardCompletionDesync;
	
	// Using this to ensure the desync error is sent to server only the first time, other picks can be detected using other steps tracking. 
	private bool desyncDataLogged;
	
	[Tooltip("All the spaces in the board, includes corners too")]
	[SerializeField] public BoardGameSpace[] boardSpaces;
	
	[Tooltip("All the mini-slots on board")]
	[SerializeField] public BoardGameSpaceMiniSlot[] boardSpaceMiniSlots;

	[SerializeField] LabelWrapperComponent progressLabel;
	[SerializeField] BoardGameProgressMeterPip[] progressMeterPips;
	[SerializeField] LabelWrapperComponent creditsLabel;

	[Tooltip("Overlay: Board completion overlay")]
	[SerializeField] private BoardCompleteOverlay boardCompleteOverlay;
	
	[NonSerialized] public int currentSpaceIndex;

	[Tooltip("All token prefabs")]
	[Space(10)] [SerializeField] BoardGameModule.BoardGameTokenData[] tokenPrefabs;
	
	[Tooltip("Time for token to move from Space A to B")]
	[SerializeField] private float tokenMoveTime;
	
	[Tooltip("Time for token to move from Space A to B after tap to speed")]
	[SerializeField] private float tokenMoveTimeFast;

	[NonSerialized] public BoardGamePlayerToken playerToken;

	private int loopMultiplier = 0;
	public long loopCreditsAmount { get; private set; }
	
	private int allRungsLandedMultiplier;
	public long allRungsLandedCreditsAmount = 0;

	private ModularChallengeGameOutcomeEntry currentPickData;

	private PickByPickClaimableBonusGameOutcome currentBoardGameOutcomeData = null;
	private PickByPickClaimableBonusGameOutcome updatedBoardGameOutcomeData = null;

	private ProgressCounter saleProgress;

	private bool isBoardSetupDone = false;
	private bool isPickInProgress;
	private bool speedUpTokenMovementAnimation = false;
	private GameTimerRange gameTimer;
	
	public bool isPurchaseOfferAvailable
	{
		get
		{
			if (saleProgress == null)
			{
				return false;
			}

			return saleProgress.currentValue <= saleProgress.completeValue;
		}
	}

	public bool isGameExpired
	{
		get
		{
			return gameTimer != null && gameTimer.isExpired;
		}
	}

	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame parentGame)
	{
		if (outcome.roundCount <= 0)
		{
			_isOutcomeExpected = false;
		}

		base.init(outcome, roundIndex, parentGame);
		setupDataFromPaytable(outcome);
	}
	
	private void setupDataFromPaytable(ModularChallengeGameOutcome outcome)
	{
		JSON boardGamePaytableJson = BonusGamePaytable.findPaytable(outcome.getPayTableName());
		JSON[] allLaddersData = boardGamePaytableJson.getJsonArray("ladders");

		//Currently an array in the paytable but we only support a single ladder so just grab the data from the first one
		JSON ladderData = allLaddersData[0];
		loopMultiplier = ladderData.getInt("on_loop_multiplier", 0);
		loopCreditsAmount = loopMultiplier * outcome.dynamicBaseCredits;
		
		allRungsLandedMultiplier = ladderData.getInt("all_rungs_landed_multiplier", 0);
		allRungsLandedCreditsAmount = allRungsLandedMultiplier * outcome.dynamicBaseCredits;
		creditsLabel.text = CreditsEconomy.convertCredits(allRungsLandedCreditsAmount);
	}
	
	/// <summary>
	/// Unlike other bonus games, Boardgame is only over when we get to the jackpot.
	/// This means even if we use up all the available picks, the round is not finished.
	/// Users can launch the board without any available picks.  
	/// </summary>
	/// <returns></returns>
	protected override bool isRoundOver()
	{
		if (currentPickData == null)
		{
			return false;
		}

		//HIR-92844: Some users are reporting they have completed the board but not getting board completion reward
		if (currentBoardGameOutcomeData.currentLandedRungs.Length >= progressMeterPips.Length && !currentPickData.isJackpot && !desyncDataLogged)
		{
			// this means all spaces are marked as landed, but data does not indicate the pick is jackpot
			if (onBoardCompletionDesync != null)
			{
				onBoardCompletionDesync(Dict.create(D.DATA, currentBoardGameOutcomeData, D.OPTION, currentPickData));
			}
			desyncDataLogged = true; // Make sure data is sent to splunk only once
		}

		return currentPickData.isJackpot;
	}

	protected override IEnumerator itemClicked(PickingGameBasePickItem pickItem,
		ModularChallengeGameOutcomeEntry pickData)
	{
		isPickInProgress = true;
		currentPickData = pickData;
		
		if (onItemClick != null)
		{
			onItemClick.Invoke();
		}
		
		yield return StartCoroutine(base.itemClicked(pickItem, currentPickData));
		
		isPickInProgress = false;

		refreshData(); // get data for new pick
		
		List<TICoroutine> coroutines = new List<TICoroutine>();
		// Update meter after data is updated
		coroutines.Add(StartCoroutine(setProgressMeterState()));

		// If the round is over, the playerToken can be null until it is re-initialized
		if (playerToken != null)
		{
			// Highlight the token by playing default animations
			coroutines.Add(StartCoroutine(playerToken.readyForNextRoll()));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		
		inputEnabled = true;
	}

	public bool willTokenLandOnNewSpace(int spacesToMove)
	{
		int newSpaceIndex = (currentSpaceIndex + spacesToMove) % boardSpaces.Length;
		return boardSpaces[newSpaceIndex].willBeNewlyLandedOnTokenArrival;
	}

	public override IEnumerator roundEnd()
	{
		addCredits(allRungsLandedCreditsAmount);
		if (onRoundEnd != null)
		{
			onRoundEnd.Invoke();
		}
		yield return StartCoroutine(animateBoardCompletion());
	}
	
	// Just keeps a local copy. Actual update happens with refreshData call
	// This is because data update call comes from server right on item click.
	// But we need data to updated only after modules have processed current outcome
	public void setBoardGameData(PickByPickClaimableBonusGameOutcome boardGameData, ProgressCounter saleProgress, GameTimerRange gameDuration)
	{
		this.saleProgress = saleProgress;
		updatedBoardGameOutcomeData = boardGameData;
		if (gameDuration != null)
		{
			gameTimer = gameDuration;
			gameTimer.registerFunction(onGameExpire);
		}
		if (!isPickInProgress)
		{
			refreshData();
		}
	}

	/// <summary>
	/// set board data along with token initialization.
	/// Call this only once during initialization.
	/// </summary>
	/// <param name="boardGameData"></param>
	/// <param name="saleProgress"></param>
	/// <param name="gameDuration"></param>
	/// <param name="tokenType"></param>
	public void setBoardGameData(PickByPickClaimableBonusGameOutcome boardGameData, ProgressCounter saleProgress,
		GameTimerRange gameDuration, BoardGameModule.BoardTokenType tokenType)
	{
		setBoardGameData(boardGameData, saleProgress, gameDuration);
		initializeToken(tokenType);
	}

	private void onGameExpire(Dict args, GameTimerRange caller)
	{
		if (this == null || isPickInProgress)
		{
			return;
		}
		
		for (int i = 0; i < cachedAttachedModules.Count; i++)
		{
			BoardGameModule currentModule = cachedAttachedModules[i] as BoardGameModule;
			if (currentModule != null && currentModule.needsToExecuteOnDataUpdate())
			{
				currentModule.executeOnDataUpdate(currentBoardGameOutcomeData);
			}
		}
	}

	private void initializeToken(BoardGameModule.BoardTokenType type)
	{
		for (int i = 0; i < tokenPrefabs.Length; i++)
		{
			if (tokenPrefabs[i].type == type)
			{
				GameObject playerTokenGameObject =
					CommonGameObject.instantiate(tokenPrefabs[i].tokenPrefab, boardSpaces[currentSpaceIndex].tokenMoveTarget) as
						GameObject;
				if (playerTokenGameObject != null)
				{
					playerToken = playerTokenGameObject.GetComponent<BoardGamePlayerToken>();
					StartCoroutine(playerToken.setParentSpaceAndAnimate(boardSpaces[currentSpaceIndex], true));
					break;
				}
				else
				{
					Debug.LogError("Token prefab instantiation failed");
				}
			}
		}
	}

	private void refreshData()
	{
		if (updatedBoardGameOutcomeData == null)
		{
			return;
		}

		currentBoardGameOutcomeData = updatedBoardGameOutcomeData;
		updatedBoardGameOutcomeData = null;
		outcome = currentBoardGameOutcomeData.picks;
		pickIndex = 0; // set pick index to 0 as the outcome was reset
		if (outcome.roundCount > 0)
		{
			_isOutcomeExpected = true;
		}

		for (int i = 0; i < cachedAttachedModules.Count; i++)
		{
			BoardGameModule currentModule = cachedAttachedModules[i] as BoardGameModule;
			if (currentModule != null && currentModule.needsToExecuteOnDataUpdate())
			{
				currentModule.executeOnDataUpdate(currentBoardGameOutcomeData);
			}
		}
		setupDataFromPaytable(outcome);
		setBoardSpaceAndMiniSlotData();
		StartCoroutine(setProgressMeterState());
	}	

	private void setBoardSpaceAndMiniSlotData()
	{
		if (isBoardSetupDone)
		{
			// Spaces and Minislots are is already setup.
			// No need to re-init it
			return;
		}

		if (currentBoardGameOutcomeData == null)
		{
			Debug.LogError("Setup board called, but data is not available");
			currentSpaceIndex = 0;
			return;
		}

		currentSpaceIndex = currentBoardGameOutcomeData.currentLadderPosition;
		List<int> landedRungs = new List<int>(currentBoardGameOutcomeData.currentLandedRungs);
		if (landedRungs.Count > boardSpaces.Length)
		{
			Debug.LogError("Landed rungs greater that actual rung(space) count");
			return;
		}
		landedRungs.Sort();
		int landedListIndex = 0;
		for (int i = 0; i < boardSpaces.Length; i++)
		{
			bool isLanded = false;
			if (landedListIndex < landedRungs.Count && landedRungs[landedListIndex] == i)
			{
				isLanded = true;
				landedListIndex++;
			}
			boardSpaces[i].init(isLanded, i == currentSpaceIndex, i);
		}

		StartCoroutine(playBoardInitialAnimations());
		isBoardSetupDone = true;
	}

	private IEnumerator playBoardInitialAnimations()
	{
		List<TICoroutine> coroutines = new List<TICoroutine>();
		for (int i = 0; i < boardSpaces.Length; i++)
		{
			coroutines.Add(StartCoroutine(boardSpaces[i].playIdleAnimations()));
		}
		
		for (int i = 0; i < boardSpaceMiniSlots.Length; i++)
		{
			coroutines.Add(StartCoroutine(boardSpaceMiniSlots[i].playIdleAnimations()));
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
	}
	
	public IEnumerator setProgressMeterState(bool isRoundOver = false)
	{
		if (currentBoardGameOutcomeData != null)
		{
			int totalLandedRungs = currentBoardGameOutcomeData.currentLandedRungs.Length;
			if (isRoundOver)
			{
				// When board is completed, the last pip never gets set because the new data received from last pick resets the board.
				// Hence, we need to set the meter as full manually and make sure to call this method along side board completion animations
				totalLandedRungs = progressMeterPips.Length;
			}

			progressLabel.text = Localize.text(PROGRESS_TEXT_FORMAT,
				totalLandedRungs.ToString(), progressMeterPips.Length);

			List<TICoroutine> coroutines = new List<TICoroutine>();
			for (int i = 0; i < progressMeterPips.Length; i++)
			{
				coroutines.Add(StartCoroutine(progressMeterPips[i]
					.playAnimation(i < totalLandedRungs)));
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		}
	}
	
	#region Token Stuff
	
	public IEnumerator updatePlayerTokenPosition(int steps)
	{
		List<TICoroutine> coroutines = new List<TICoroutine>();
		
		// remove highlighted animation on the current space
		coroutines.Add(StartCoroutine(boardSpaces[currentSpaceIndex].playOnTokenMovedAwayAnimations()));
		
		// copy animating spaces to a temp array
		BoardGameSpace[] animatingSpaces = new BoardGameSpace[steps];
		
		int stepIndex = currentSpaceIndex;
		int payoutSpaceIndex = -1;
		for (int i = 0; i < steps; i++)
		{
			stepIndex = (stepIndex + 1) % boardSpaces.Length;
			animatingSpaces[i] = boardSpaces[stepIndex];
			
			if (stepIndex == 0)
			{
				// This means board will loop (i.e token will pass the 'go' space).
				// store the index of this space in the animatingSpaces array, to be used when token reaches it.
				payoutSpaceIndex = i;
			}
		}
		currentSpaceIndex = stepIndex;

		StartCoroutine(tapToSpeedUpCheck());
		
		// animate the selected spaces
		for (int i = 0; i < steps; i++)
		{
			coroutines.Add(StartCoroutine(animatingSpaces[i].playPathTrackingAnimation()));
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));

		playerToken.prepareToMove();
		
		// Do not yield on this
		StartCoroutine(tapToSpeedUpCheck());
		TICoroutine boardLoopEventCoroutine = null;
		for (int i = 0; i < steps; i++)
		{
			float timeToMove = speedUpTokenMovementAnimation ? tokenMoveTimeFast : tokenMoveTime;
			yield return StartCoroutine(playerToken.moveToSpace(animatingSpaces[i], timeToMove, speedUpTokenMovementAnimation));
			yield return StartCoroutine(animatingSpaces[i].playStepOverAnimation(i == steps-1));
			if (i == payoutSpaceIndex)
			{
				boardLoopEventCoroutine = StartCoroutine(boardLoopEvent());
			}
		}
		
		yield return StartCoroutine(playerToken.onMoveComplete());
		
		// wait for modules that handle loop events to finish
		yield return boardLoopEventCoroutine;
	}
	
	private IEnumerator tapToSpeedUpCheck()
	{
		speedUpTokenMovementAnimation = false;
		// waits until screen is tapped
		while (isPickInProgress && !speedUpTokenMovementAnimation)
		{
			speedUpTokenMovementAnimation = TouchInput.didTap;
			yield return null;
		}
	}

	private IEnumerator boardLoopEvent()
	{
		for (int i = 0; i < cachedAttachedModules.Count; i++)
		{
			BoardGameModule currentModule = cachedAttachedModules[i] as BoardGameModule;
			if (currentModule != null && currentModule.needsToExecuteOnBoardLoop())
			{
				yield return StartCoroutine(currentModule.executeOnBoardLoop());
			}
		}
	}

	#endregion

	private IEnumerator animateBoardCompletion()
	{	
		// board completion animations
		List<TICoroutine> coroutines = new List<TICoroutine>();
		coroutines.Add(StartCoroutine(playerToken.playCelebrationAnimation()));
		
		// set progress meter to completed state
		coroutines.Add(StartCoroutine(setProgressMeterState(true)));
		
		for (int i = 0; i < boardSpaces.Length; i++)
		{
			coroutines.Add(StartCoroutine(boardSpaces[i].playBoardCompleteAnimation()));
		}
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		if (boardCompleteOverlay != null)
		{
			boardCompleteOverlay.init(allRungsLandedCreditsAmount, startNextRound);
		}

		isBoardSetupDone = false;
		
		// reset board here
		refreshData();
		coroutines = new List<TICoroutine>();
		for (int i = 0; i < boardSpaces.Length; i++)
		{
			coroutines.Add(StartCoroutine(boardSpaces[i].playIdleAnimations()));
		}
		for (int i = 0; i < boardSpaceMiniSlots.Length; i++)
		{
			coroutines.Add(StartCoroutine(boardSpaceMiniSlots[i].playIdleAnimations()));
		}
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		Destroy(playerToken.gameObject);
	}

	// When all animations are overlays are done, and new data can be received
	private void startNextRound(Dict args)
	{
		if (onBoardReadyForNextRound != null)
		{
			onBoardReadyForNextRound.Invoke();
		}
	}

	public void OnDestroy()
	{
		if (gameTimer != null)
		{
			gameTimer.removeFunction(onGameExpire);
		}
	}
}
