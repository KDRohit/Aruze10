using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/**
Class for handling bonus game related stuff.
*/

public class BonusGameManager : TICoroutineMonoBehaviour, IResetGame
{
	public static BonusGameManager instance = null;
	
	public BonusGameWings wings;

	[HideInInspector] public static ReelGame currentBaseGame = null;
	[HideInInspector] public static SlotOutcome currentBonusGameOutcome = null;
	[HideInInspector] public Dictionary<BonusGameType, BaseBonusGameOutcome> outcomes;	// Bonus game outcomes, to be used by the game code for presentation.
	[HideInInspector] public string bonusGamePaytableType = "";
//	[HideInInspector] public string bonusGamePaytableName = "";
	[HideInInspector] public JSON[] freeSpinPaytable;
	[HideInInspector] public int bonusGameStopId = -1;
	[HideInInspector] public string paytableSetId = "";
	[HideInInspector] public PayTable currentBonusPaytable;
	[HideInInspector] public long currentMultiplier = -1;
	[HideInInspector] public long betMultiplierOverride = -1;
	[System.NonSerialized] public BonusGameType currentGameType = BonusGameType.UNDEFINED;
	[HideInInspector] public string currentGameKey = null;
	[HideInInspector] public long finalPayout = 0;				// This amount is read by the outcome display controller for the main rollup.
	[System.NonSerialized] public long currentGameFinalPayout = 0;
	[HideInInspector] public long multiBonusGamePayout = 0;
	[HideInInspector] public string bonusGameName = "";		/// This is use for knowing which games to gift.
	[HideInInspector] public string summaryScreenGameName = "";	// Storage location of the current game name for .
	[HideInInspector] public string summaryScreenGameNameOverride = "";	// Storage location of the current game name override
	[HideInInspector] public string challengeProgressEventId = "";
	[HideInInspector] public bool isGiftable = false;
	[HideInInspector] public PayTable.BonusGameChoices possibleBonusGameChoices;
	[HideInInspector] public long lineWinMulitplier = 0;

	private BonusGamePresenter _presenter = null;
	private GameObject bonusGameObject = null;
	private GameObject previousBonusGame;
	private Dictionary<GameObject, int> bonusGameCameraLayerMap = new Dictionary<GameObject, int>();
	public bool isLoaded = false;
	private bool isBonusCreated = false;	// Error tracking to detect critical errors where the bonus tries to show without being created
	private bool needToHideWings = false;
	private bool isHidingBaseGameUIAfterTransition = true; // Only used when the base game is used to animate a transition before the bonus game is made active, right now will always be true
	private Stack<BonusGameStackState> bonusGameStack = new Stack<BonusGameStackState>(); // with the coming of aruze02 Extreme Dragon we need to support stacked bonus games where a bonus game will be active again after the extra one triggers
	private ChallengeGame challengeGameBeforeCreate = null; // Used to track what ChallengeGame.instance was before create was called, in order to handle stacking correctly
	private bool wasChallengeGameModifiedByCreate = false; // tracks if calling create modified the ChallengeGame.instance singleton, and if so we will need to do something special if we need to stack bonuses
	private BonusGameType gameTypeBeforeCreate = BonusGameType.UNDEFINED; // Tracks what the currentGameType was before create() function was called so we can store that in the stack bonus info

	private class BonusGameStackState
	{
		public BonusGameStackState(GameObject bonusGameObject, BonusGamePresenter presenter, ChallengeGame challengeGameInstance, bool isHidingSpinPanelOnPopStack, BonusGameType gameType)
		{
			this.bonusGameObject = bonusGameObject;
			this.presenter = presenter;
			this.challengeGameInstance = challengeGameInstance;
			this.isHidingSpinPanelOnPopStack = isHidingSpinPanelOnPopStack;
			this.gameType = gameType;
		}

		public GameObject bonusGameObject = null;
		public BonusGamePresenter presenter = null;
		public ChallengeGame challengeGameInstance = null;
		public bool isHidingSpinPanelOnPopStack = false;
		public BonusGameType gameType;
	}

	// Returns whether there is an active bonus game.
	public static bool isBonusGameActive
	{
		get
		{
			return
				instance != null &&
				BonusGamePresenter.instance != null &&
				BonusGamePresenter.instance.gameObject != null &&
				BonusGamePresenter.instance.isGameActive;
		}
	}
	
	void Awake()
	{
		instance = this;
		outcomes = new Dictionary<BonusGameType, BaseBonusGameOutcome>();
	}
	
	/// Creates a bonus of the given type.
	public void create(BonusGameType bonusType, string gameKey = "")
	{
		gameTypeBeforeCreate = currentGameType;
		currentGameType = bonusType;
		isLoaded = false;
		
		challengeGameBeforeCreate = ChallengeGame.instance;

		if (string.IsNullOrEmpty(gameKey))
		{
			// This must be a bonus game called from a base game, so GameState.game better not be null.
			currentGameKey = GameState.game.keyName;
		}
		else
		{
			currentGameKey = gameKey;
		}

		if (string.IsNullOrEmpty(currentGameKey))
		{
			Debug.LogError("BonusGameManager.create: gameKey is empty");
		}

		if (bonusType == BonusGameType.PORTAL)
		{
			SlotResourceMap.createPortalInstance(currentGameKey, bonusGameLoaded, null);
			isBonusCreated = true;
		}
		else if (bonusType == BonusGameType.GIFTING)
		{
			if (SlotResourceMap.gameHasFreespinsPrefab(GameState.game.keyName))
			{
				SlotResourceMap.createFreeSpinInstance(currentGameKey, bonusGameLoaded, bonusGameFailedToLoad);
			}
			else
			{
				// assuming this game is using freespins in base, since there isn't really anything else it could be
				// this code should only be reached when a gift chest freespins is triggered, since normal
				// free spins in base will go through a different flow which is handled by the currenlty loaded SlotBaseGame
				// for gift chest spins we need to load the game to trigger the free spins in base though
				SlotResourceMap.createSlotInstance(currentGameKey, giftedFreespinsInBaseLoaded, bonusGameFailedToLoad);
			}
			isBonusCreated = true;
		}
		else if (bonusType == BonusGameType.SUPER_BONUS)
		{
			SlotResourceMap.createSuperBonusInstance(currentGameKey, bonusGameLoaded, bonusGameFailedToLoad);
			isBonusCreated = true;
		}
		else if (bonusType == BonusGameType.CHALLENGE)
		{
			SlotResourceMap.createBonusInstance(currentGameKey, bonusGameLoaded, bonusGameFailedToLoad);
			isBonusCreated = true;
		}
		else if (bonusType == BonusGameType.CREDIT)
		{
			SlotResourceMap.createCreditBonusInstance(currentGameKey, bonusGameLoaded, bonusGameFailedToLoad);
			isBonusCreated = true;
		}
		else if (bonusType == BonusGameType.SCATTER)
		{
			SlotResourceMap.createScatterBonusInstance(currentGameKey, bonusGameLoaded, bonusGameFailedToLoad);
			isBonusCreated = true;
		}
		else
		{
			Debug.LogError("BonusGameManager -- tried to create bonus game of type: " + bonusType + " but it wasn't recognized on the client");
		}

		wasChallengeGameModifiedByCreate = challengeGameBeforeCreate != ChallengeGame.instance;
	}
	
	// Show a previously created bonus
	public void show(SlotOutcome outcome = null, bool shouldCreateBonusWithTransition = false)
	{
		StartCoroutine(showCoroutine(outcome, shouldCreateBonusWithTransition, isStackingBonuses:false, isHidingSpinPanelOnPopStack:false));
	}

	// Show a previously created bonus which will be stacked on top of whatever bonus is currently happening (if any)
	public void showStackedBonus(bool isHidingSpinPanelOnPopStack, SlotOutcome outcome = null, bool shouldCreateBonusWithTransition = false)
	{
		StartCoroutine(showCoroutine(outcome, shouldCreateBonusWithTransition, isStackingBonuses:true, isHidingSpinPanelOnPopStack));
	}
	

	// Tells if the BonusGameManager is handling stacked bonuses right now (i.e. a bonus triggered by another bonus)
	public bool hasStackedBonusGames()
	{
		return bonusGameStack.Count > 0;
	}
	
	// Tells if the thing on the top of the stack is a freespins in base, by comparing the BonusGamePresenter against the one owned by the active SlotBaseGame
	public bool isTopOfBonusGameStackFreespinsInBase()
	{
		if (SlotBaseGame.instance != null)
		{
			if (hasStackedBonusGames())
			{
				BonusGameStackState topState = bonusGameStack.Peek();
				return SlotBaseGame.instance.isBonusGamePresenterFreespinInBasePresenter(topState.presenter);
			}
		}

		return false;
	}

	// Push the current bonus to the bonus stack
	private void pushCurrentBonusToStack(bool isHidingPreviousBonus, ChallengeGame currentChallengeGame, bool isHidingSpinPanelOnPopStack, BonusGameType gameType)
	{
		if (_presenter != null)
		{
			// we are going to stack the bonuses, so add the current bonus onto the stack so we can restore it after the new bonus is done
			BonusGameStackState previousBonusGameState = new BonusGameStackState(previousBonusGame, _presenter, currentChallengeGame, isHidingSpinPanelOnPopStack, gameType);
			bonusGameStack.Push(previousBonusGameState);

			if (isHidingPreviousBonus)
			{
				// hide the previous bonus for now, as we are about to play another bonus and then come back
				if (previousBonusGame != null)
				{
					previousBonusGame.SetActive(false);
				}
				else
				{
					_presenter.gameObject.SetActive(false);
				}
			}
		}
		else
		{
			Debug.LogError("BonusGameManager.pushCurrentBonusToStack() - Function called with no current _presenter to push, this shouldn't happen!");
		}
	}

	// special funciton for triggering the freespins in base so that it is correctly represented as running in the BonusGameManager
	public void playFreespinsInBase(BonusGamePresenter freespinsInBasegamePresenter, long startingValue)
	{
		if (freespinsInBasegamePresenter != null)
		{
			swapToPassedInBonus(freespinsInBasegamePresenter, true, isHidingSpinPanelOnPopStack:false);

			// If our free spin game has a multiplier decided from somewhere else, let's make sure we use that for our end game summary screen.
			if (BonusGamePresenter.carryoverMultiplier > 0)
			{
				BonusGamePresenter.instance.useMultiplier = true;
				BonusGameManager.instance.currentMultiplier = BonusGamePresenter.carryoverMultiplier + 1;
			}
			BonusGamePresenter.instance.currentPayout = startingValue;
		}
		else
		{
			Debug.LogWarning("BonusGameManager.playFreespinsInBase() - the freespinsInBasegamePresenter passed to this function was NULL, this shouldn't happen!");
		}
	}

	// This function will swap the game to be setup to play the passed in BonusGamePresenter, this
	// includes pushing the currently running bonus to the bonusGameStack if a bonus was already in
	// progress.  This can be used if you want to stack a bonus but don't need to create it since
	// it is part of another prefab.
	public void swapToPassedInBonus(BonusGamePresenter bonusToPlay, bool isHidingPreviousBonus, bool isHidingSpinPanelOnPopStack)
	{
		if (bonusToPlay != null)
		{
			if (_presenter != null)
			{
				// Need to do this here, since the bonus isn't loaded in the standard way which would set this
				previousBonusGame = bonusGameObject;
				pushCurrentBonusToStack(isHidingPreviousBonus, ChallengeGame.instance, isHidingSpinPanelOnPopStack, currentGameType);
				// Null this out once we save the state we are leaving,
				// so that it doesn't sit there while another bonus is happening
				// which may not be a ChallengeGame
				ChallengeGame.instance = null;
			}

			BonusGamePresenter.instance = bonusToPlay;
			_presenter = bonusToPlay;
		}
		else
		{
			Debug.LogWarning("BonusGameManager.pushBonusToBonusGameStack() - The bonusToPlay passed to this function was NULL, this shouldn't happen!");
		}
	}

	protected IEnumerator showCoroutine(SlotOutcome outcome, bool shouldCreateBonusWithTransition, bool isStackingBonuses, bool isHidingSpinPanelOnPopStack)
	{
		BonusGameWings wingsToHide = null;

		if (!isBonusCreated)
		{
			abortBonusGame();
			Debug.LogError("BonusGameManager::showCoroutine() - Trying to show a bonus when one hasn't been created yet!  Aborting bonus!");
			yield break;
		}
		else
		{
			// go ahead and reset this flag now that we've verified that a bonus exists to show
			isBonusCreated = false;
		}

		while (!isLoaded)
		{
			yield return null;
		}

		bool isPushingCurrentBonusToStack = false;
		if (isStackingBonuses && _presenter != null)
		{
			isPushingCurrentBonusToStack = true;
			pushCurrentBonusToStack(isHidingPreviousBonus:true, challengeGameBeforeCreate, isHidingSpinPanelOnPopStack, gameTypeBeforeCreate);

			// If the ChallengeGame.instance was modified by create then what it is
			// currently set to is the current ChallengeGame we want to play, so
			// we should leave it as is, otherwise we want to null out what is currently set
			// there so the game doesn't think it is in a ChallengGame currently.
			if (!wasChallengeGameModifiedByCreate)
			{
				// Null this out once we save the state we are leaving,
				// so that it doesn't sit there while another bonus is happening
				// which may not be a ChallengeGame
				ChallengeGame.instance = null;
			}
		}
		
		if (!isPushingCurrentBonusToStack && previousBonusGame != null)
		{
			Object.Destroy(previousBonusGame);
		}

		wingsToHide = wings;

		if (bonusGameObject == null)
		{
			Debug.LogError("Failed to show bonus game. Game object is missing");
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.doShowNonBonusOutcomes();
			}
			yield break;
		}

		needToHideWings = false;
		
		if (!shouldCreateBonusWithTransition || SlotBaseGame.instance == null) // no transition if there's no base game
		{
			createBonusFromWelcomeDialog();
		}
		else
		{
			createBonusWithEntireGameTransition();
		}

		if (needToHideWings && wingsToHide != null)
		{
			// yield so the bonus game wings have a frame to render before hiding our wings so there is no frame
			// without any wings, causing a flicker, especially if the wings are identical
			yield return null;    
			wingsToHide.hide();
		}
		
		if (currentGameType == BonusGameType.PORTAL)
		{
			PortalScript portalScript = bonusGameObject.GetComponentInChildren<PortalScript>();
			if (portalScript != null)
			{
				SlotBaseGame baseGame = SlotBaseGame.instance;
				portalScript.beginPortal(baseGame.bannerRoots, baseGame.banners, baseGame.bannerTextOverlay, outcome, baseGame.relativeMultiplier);
			}
		}
	}

	// special case for when the freespins in base base game is loaded in for a gifted spin
	private void giftedFreespinsInBaseLoaded(string asset, Object obj, Dict data)
	{
		previousBonusGame = bonusGameObject;
		bonusGameObject = obj as GameObject;

		isLoaded = true;
	}

	// Normal handling for what to do with the bonusGame when it is first loaded in
	private void bonusGameLoaded(string asset, Object obj, Dict data)
	{
		previousBonusGame = bonusGameObject;
		bonusGameObject = obj as GameObject;

		bonusGameCameraLayerMap = CommonGameObject.getCameraLayerMap(bonusGameObject);
		CommonGameObject.setLayerRecursively(bonusGameObject, Layers.ID_HIDDEN);

		isLoaded = true;
	}

	// We want to call this so that the game doesn't get into a state where nothing can be done if a bonus game doesn't work.
	private void bonusGameFailedToLoad(string assetPath, Dict data = null)
	{
		Debug.LogError("We failed to load bonus game: " + assetPath);
		bonusGameObject = null;
		previousBonusGame = null;
		isLoaded = false;
	}
		
	private void createBonusWithEntireGameTransition()
	{
		_presenter = null;
		
		if (bonusGameObject == null)
		{
			Debug.LogError(string.Format("Failed to load bonus game prefab."));
			return;
		}

		_presenter = bonusGameObject.GetComponent<BonusGamePresenter>();
		if (_presenter == null)
		{
			Debug.LogError("Bonus prefab does not contain a BonusGamePresenter class, failing out");
			Object.Destroy(bonusGameObject);
			return;
		}
		if (FreeSpinGame.instance == null && wings != null)
		{
			if(ChallengeGame.instance != null && !ChallengeGame.instance.wingsIncludedInBackground)
			{
				wings.show();
			}
		}
		else if (FreeSpinGame.instance != null)
		{
			needToHideWings = FreeSpinGame.instance.shouldHideBonusGamePresenterWings;
		}
		else if (wings != null)
		{
			needToHideWings = true;
		}

		// Hide the normal top overlay for full-screen bonus games.
		if (Overlay.instance != null)
		{
			//Overlay.instance.top.show(false);
		}
		
		// Set the bonus game as the child of this controller object in the NGUI hierarchy.
		bonusGameObject.transform.parent = transform;
		bonusGameObject.transform.localPosition = Vector3.zero;
		bonusGameObject.transform.localScale = Vector3.one;
		CommonGameObject.restoreCameraLayerMap(bonusGameCameraLayerMap, bonusGameObject);

		// We need to force the background script to run again if this bonus is a reel game, 
		// as it now relies on the cameras being active to actually set everything up correctly
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.reelGameBackground != null)
		{
			FreeSpinGame.instance.reelGameBackground.forceUpdate();
		}
		
		// Register this object as something to clean up when leaving this scene (if going back to the lobby).
		DisposableObject.register(bonusGameObject);
		
		if (GameState.hasEventId)
		{
			// For bonus games with event id's, we tell the server about the game after we've
			// finished downloading the asset bundles and creating the game instance.
			challengeProgressEventId = "";
			Server.registerEventDelegate("slots_outcome", giftedSlotOutcomeEventCallback);
			
			if (GameState.giftedBonus.type.Contains("challenge"))
			{
				Server.registerEventDelegate("challenge_in_progress", challengeProgressEventCallback);
			}
			
			SlotAction.playBonusGame(GameState.giftedBonus.eventId, GameState.giftedBonus.bonusGameType);
			// Once we've sent the action to the server for starting the game,
			// there's no turning back, so hide the "CANCEL DOWNLOAD" button on the loading screen.
			Loading.instance.setDownloadingStatus(false);
		}
		else
		{
			startBonusGame();
		}
	}	
	
	public void startTransitionedBonusGame()
	{
		// Hide the normal top overlay for full-screen bonus games.
		if (Overlay.instance != null)
		{
			if (isHidingBaseGameUIAfterTransition)
			{
				// this is the override that BonusGameManager will use for now to force it to hide the UI after these types of transitions happen
				// if this isn't what we want we'll need to add a way to intelligently control isHidingBaseGameUIAfterTransition
				Overlay.instance.top.show(false);
			}
			else
			{
				// if isHidingBaseGameUIAfterTransition were to be off then we'll check if the BonusGamePresenter thinks the base game should be on or off
				if (_presenter == null || _presenter.isHidingBaseGame)
				{
					Overlay.instance.top.show(false);
				}
			}
		}

		// Must do this stuff after creating the game, so the FreeSpinGame.instance may exist.
		// Show the bonus spin panel if it's a bonus spins game, else hide all spin panel types.			
		if (FreeSpinGame.instance != null && !FreeSpinGame.instance.hideFreeSpinPannel)
		{
			SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		}
		else
		{
			if (isHidingBaseGameUIAfterTransition)
			{
				// this is the override that BonusGameManager will use for now to force it to hide the UI after these types of transitions happen
				// if this isn't what we want we'll need to add a way to intelligently control isHidingBaseGameUIAfterTransition
				SpinPanel.instance.hidePanels();
			}
			else
			{
				// if isHidingBaseGameUIAfterTransition were to be off then we'll check if the BonusGamePresenter thinks the base game should be on or off
				if (_presenter == null || _presenter.isHidingBaseGame)
				{
					SpinPanel.instance.hidePanels();
				}
			}
		}
	}
		
	private void createBonusFromWelcomeDialog()
	{
		_presenter = null;
		
		if (bonusGameObject == null)
		{
			Debug.LogError(string.Format("Failed to load bonus game prefab."));
			return;
		}

		_presenter = bonusGameObject.GetComponent<BonusGamePresenter>();
		
		if (_presenter == null)
		{
			Debug.LogError("Bonus prefab does not contain a BonusGamePresenter class, failing out");
			Object.Destroy(bonusGameObject);
			return;
		}
		if (FreeSpinGame.instance == null && wings != null)
		{
			if(ChallengeGame.instance != null && !ChallengeGame.instance.wingsIncludedInBackground)
			{
				wings.show();
			}
		}
		else if (FreeSpinGame.instance != null)
		{
			needToHideWings = FreeSpinGame.instance.shouldHideBonusGamePresenterWings;
		}
		else if (wings != null)
		{
			needToHideWings = true;
		}

		// Hide the normal top overlay for full-screen bonus games.
		if (Overlay.instance != null)
		{
			// only hide the UI if the BonusGamePresenter will hid the base game, otherwise it is assumed that the bonus game will handle hiding these UI elements
			if (_presenter.isHidingBaseGame)
			{
				Overlay.instance.top.show(false);
			}
		}
		
		// Set the bonus game as the child of this controller object in the NGUI hierarchy.
		bonusGameObject.transform.parent = transform;
		bonusGameObject.transform.localPosition = Vector3.zero;
		bonusGameObject.transform.localScale = Vector3.one;
		CommonGameObject.restoreCameraLayerMap(bonusGameCameraLayerMap, bonusGameObject);

		// We need to force the background script to run again if this bonus is a reel game, 
		// as it now relies on the cameras being active to actually set everything up correctly
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.reelGameBackground != null)
		{
			FreeSpinGame.instance.reelGameBackground.forceUpdate();
		}
		
		// Register this object as something to clean up when leaving this scene (if going back to the lobby).
		DisposableObject.register(bonusGameObject);

		if (GameState.giftedBonus != null)
		{
			Userflows.flowStart("gifted-" + GameState.giftedBonus.slotsGameKey);

			// For gifted bonus games, we tell the server about the game after we've
			// finished downloading the asset bundles and creating the game instance.
			challengeProgressEventId = "";
			if (GameState.giftedBonus.outcomeJSON != null)
			{
				giftedSlotOutcomeEventCallback(GameState.giftedBonus.outcomeJSON);
				GameState.giftedBonus.outcomeJSON = null;
			}
			else
			{
				Server.registerEventDelegate("slots_outcome", giftedSlotOutcomeEventCallback);
			}

			// Once we've sent the action to the server for starting the game,
			// there's no turning back, so hide the "CANCEL DOWNLOAD" button on the loading screen.
			Loading.instance.setDownloadingStatus(false);

			// For gifted spins make sure the overlay is hidden, since it probably isn't going to be handled since
			// we are going right into the freespins.
			if (Overlay.instance != null)
			{
				Overlay.instance.top.show(false);
			}
		}
		else
		{
			// Not a gifted bonus game, so just start it immediately.
			startBonusGame();
		}

		// Must do this stuff after creating the game, so the FreeSpinGame.instance may exist.
		// Show the bonus spin panel if it's a bonus spins game, else hide all spin panel types.			
		if (FreeSpinGame.instance != null && !FreeSpinGame.instance.hideFreeSpinPannel)
		{
			SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		}
		else
		{
			// only hide the UI if the BonusGamePresenter will hid the base game, otherwise it is assumed that the bonus game will handle hiding these UI elements
			if (_presenter.isHidingBaseGame)
			{
				SpinPanel.instance.hidePanels();
			}
		}
	}

	private void startBonusGame()
	{
		if (GameState.giftedBonus == null)
		{
			// this isn't a gift, so it was triggered by the base game so log a step for the base game about it
			Userflows.logStep("start-" + currentGameType.ToString().ToLower(), "slot-" + GameState.game.keyName);
		}

		// Hide the loading screen just in case we came straight to the bonus game from the inbox in the lobby.
		// We wait to do this until the bonus game is ready to be presented.
		if (Loading.isLoading)
		{
			Loading.hide(Loading.LoadingTransactionResult.SUCCESS);
		}

		_presenter.init(isCheckingReelGameCarryOverValue:true);	// The presenter must be initialized before the game, since the presenter resets the currentPayout, and the game may set the currentPayout.

		if (FreeSpinGame.instance != null)
		{
			FreeSpinGame.instance.initFreespins();
		}
		else if (ChallengeGame.instance != null)
		{
			ChallengeGame.instance.init();
		}
	}

	/// Track when the bonus game has ended, so isBonusGameLoaded() returns a valid flag
	public void bonusGameEnded()
	{
		// make sure we aren't in a gift chest spin before calling stuff for the base game, because it could be base in freespins
		if (GameState.giftedBonus == null && currentBaseGame != null)
		{
			currentBaseGame.onBonusGameEnded();
		}

		_presenter = null;

		// ensure the challenge game instance is nulled out, so another object with a BonusGamePresenter that isn't a ChallengeGame doesn't use it's info
		if (ChallengeGame.instance != null)
		{
			ChallengeGame.instance = null;
		}

		// attempt to restore a previous bonus game if we had one in the stack
		if (hasStackedBonusGames())
		{
			BonusGameStackState previousBonusGameState = bonusGameStack.Pop();
			bonusGameObject = previousBonusGameState.bonusGameObject;
			_presenter = previousBonusGameState.presenter;
			currentGameType = previousBonusGameState.gameType;
			BonusGamePresenter.instance = _presenter;
			ChallengeGame.instance = previousBonusGameState.challengeGameInstance;

			bonusGameName = previousBonusGameState.presenter.bonusGameName;
			paytableSetId = previousBonusGameState.presenter.paytableSetId;

			// show the previous bonus game
			if (bonusGameObject != null)
			{
				bonusGameObject.SetActive(true);
			}
			else
			{
				_presenter.gameObject.SetActive(true);
			}
			
			// Determine if we should force hide the SpinPanel based on the setting
			// stored when this bonus was pushed onto the stack
			if (previousBonusGameState.isHidingSpinPanelOnPopStack)
			{
				SpinPanel.instance.hidePanels();
			}
		}
		else
		{
			// clear these out
			BonusGameManager.instance.bonusGameName = "";
			BonusGameManager.instance.paytableSetId = "";
		}
		
		if (GameState.giftedBonus == null)
		{
			// add a flow step for the base game since this isn't a gift
			if (GameState.game != null)
			{
				Userflows.logStep("end-" + currentGameType.ToString().ToLower(), "slot-" + GameState.game.keyName);
			}
			else if (Dialog.instance.isShowing)
			{
				Userflows.logStep("end-" + currentGameType.ToString().ToLower(), "dialog-" + Dialog.instance.currentDialog.name);
			}
		}
		else
		{
			// ending a gifted spin, so end that flow instead
			Userflows.addExtraFieldToFlow("gifted-" + GameState.giftedBonus.slotsGameKey, "win_amount", finalPayout.ToString());

			Userflows.flowEnd("gifted-" + GameState.giftedBonus.slotsGameKey);
		}
	}

	/// Tells if a bonus game is loaded
	public bool isBonusGameLoaded()
	{
		return _presenter != null;
	}

	/// Callback for starting a gifted bonus game, to process the outcome and start playing.
	private void giftedSlotOutcomeEventCallback(JSON data)
	{
		SlotOutcome outcome = new SlotOutcome(data);
		outcome.printOutcome();
		outcome.processBonus();
		
		startBonusGame();
		GameState.giftedBonus.outcomeJSON = null;
	}

	/// Additional callback for starting a challenge bonus game, to store info for the challenge completion
	private void challengeProgressEventCallback(JSON data)
	{
		challengeProgressEventId = data.getString("event", "");
	}

	// Text needs to attach sometimes when things related to bonus games are occurring. We're hijacking this manager for it.
	public void attachTextOverlay(GameObject textOverlay)
	{
		textOverlay.transform.parent = transform;
		textOverlay.transform.localPosition = Vector3.zero;
		textOverlay.transform.localScale = Vector3.one;
	}

	/// Call this in an emergency to abort the entire bonus, should only be used to try and recover from a major bonus game issue
	private void abortBonusGame()
	{
		// tell the bonus game manager we're done
		bonusGameEnded();

		if (GameState.giftedBonus == null)
		{
			// This wasn't a gifted bonus game, so go back to the base game now.
			
			// Need to handle the case where a Freespin uses one way of scoring (lines for instance) and the main slot base 
			// game uses a different one (clusters for instance), which occurs in the Splendor of Rome game.
			// So reset the ways to win when going back to the SlotBaseGame.
			
			// Show the normal spin panel
			SpinPanel.instance.gameObject.SetActive(true);
			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
			SpinPanel.instance.showSideInfo(true);
			if (FreeSpinGame.instance != null)
			{
				SlotBaseGame.instance.resetSpinPanelWaysToWin();
			}

			SlotBaseGame.instance.gameObject.SetActive(true);

			// call this before we show outcomes so we stop animations already going but don't screw up outcome animations
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.doSpecialOnBonusGameEnd();
			}

			// If there are other outcomes from the main spin, show them now.
			// This re-enables the UI buttons immediately if there are no other outcomes.
			SlotBaseGame.instance.doShowNonBonusOutcomes();
		}
		else
		{
			// NOTE : Untested, not 100% sure that gifted bonus games will abort correctly

			// Since we aren't going through rollup via showNonBonusOutcome() add the win value directly
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.addCreditsToSlotsPlayer(BonusGameManager.instance.finalPayout, "bonus game payout", shouldPlayCreditsRollupSound: false);
			}
			BonusGameManager.instance.finalPayout = 0;
			// This was a gifted bonus game, so no base game or spin panel exists to go back to.
			GameState.pop();
			
			if (GameState.game != null)
			{
				// If the player was in a game when launching this free bonus game,
				// re-load the game that the player was in before. It's still on the top of the stack.
				Glb.loadGame();
				Loading.show(Loading.LoadingTransactionTarget.GAME);
			}
			else
			{
				// Go back to the lobby.
				Glb.loadLobby();
				Loading.show(Loading.LoadingTransactionTarget.LOBBY);
			}
			
			// Re-open the gift chest automatically after returning to wherever we're going.
			Scheduler.addTask(new InboxTask(Dict.create(D.KEY, InboxDialog.SPINS_STATE)));

			// make sure symbols are stopped when you come back from playing a gift game if you were in a game
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.doSpecialOnBonusGameEnd();
			}
		}
	}

	/// Tells if the betMultiplierOverride is in play
	public bool isUsingBetMultiplierOverride()
	{
		return betMultiplierOverride != -1;
	}

	public static void resetStaticClassData()
	{
		if (BonusGameManager.instance != null)
		{
			Transform root = BonusGameManager.instance.transform;
			Transform wings = (BonusGameManager.instance.wings == null) ? null : BonusGameManager.instance.wings.transform;
			
			foreach (Transform child in root)
			{
				if (child != root && child != wings)
				{
					Destroy(child.gameObject);
				}
			}
			
			// Clear some of the variables that could still contain stuff from past bonus games which didn't end normally
			BonusGameManager.instance.finalPayout = 0;
			BonusGameManager.instance.currentMultiplier = -1;
		}

   		if (ChallengeGame.instance != null)
   		{
   			ChallengeGame.instance = null;
   		}
	}

	// Function to convert a string to the enum type we use for the BonusGameType
	// In some places in code these are still referenced as string types due to
	// being sent to and recieved from the server, which is why we need this.
	// For the vast majority of cases in client code we should prefer the enum.
	public static BonusGameType getBonusGameTypeForString(string bonusGameTypeStr)
	{
		switch (bonusGameTypeStr)
		{
			case "portal":
				return BonusGameType.PORTAL;
			
			case "gifting":
				return BonusGameType.GIFTING;
			
			case "challenge":
				return BonusGameType.CHALLENGE;
			
			case "credit":
				return BonusGameType.CREDIT;
			
			case "scatter":
				return BonusGameType.SCATTER;
			
			case "super_bonus":
				return BonusGameType.SUPER_BONUS;
		}

		return BonusGameType.UNDEFINED;
	}
}

public enum BonusGameType
{
	UNDEFINED,
	PORTAL,
	GIFTING,
	CHALLENGE,
	CREDIT, // Used for secondary bonus games, or credit awards
	SCATTER,
	SUPER_BONUS
}
