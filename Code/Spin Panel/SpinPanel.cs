using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using QuestForTheChest;
using TMPro;
using TMProExtensions;
using UnityEngine.SceneManagement;

/**
Controls the appearance and behaviour of the spin panel.
*/

public class SpinPanel : TICoroutineMonoBehaviour, IResetGame
{
	public const int OFFSCREEN_BUTTON_LOCATION = -10000;
	private const int SPIN_BUTTON_THROB_DELAY = 15;	// How long, in seconds, to wait before starting to throb the spin button after no spinning.
	
	public enum Type
	{
		NORMAL,
		FREE_SPINS
	}

	public enum SpinPanelSlideOutDirEnum
	{
		Left = 0,
		Right = 1,
		Up = 2,
		Down = 3,
	};

	public static SpinPanel instance = null;
	public static SpinPanelHIR hir = null;
	

	public UIAnchor bonusSpinPanelAnchor;
	public UIAnchor spinPanelAnchor;
	public SpinPanelLeftIconHandler featureButtonHandler;
	
	public Transform backgroundWingsWidth;
	public Transform freeSpinsBackgroundWingsWidth;
	public Transform freeSpinsTiledBackground;
	public GameObject normalSpinPanel;
	public GameObject bonusSpinPanel;
	public TextMeshPro bonusGameTypeLabel;
	public GameObject betUpButton;
	public GameObject betDownButton;
	public UIImageButton autoSpinButton;
	public GameObject autoSpinCountPanel;   /// The panel that holds all the auto spin count buttons.
	public GameObject autoSpinFlyout;
	public Collider autoSpinCountMask;      /// Enabled when the auto spin count panel is visible, so touching anywhere else cancels the panel.
	public GameObject autoSpinActive;
	public TextMeshPro autoSpinCountLabel;
	public UIImageButton spinButton;
	public UIImageButton stopButton;
	public Animator messageBoxAnimator;
	public Animator autoSpinPanelAnimator;
	public TextMeshPro messageLabel;
	public TextMeshPro totalBetTitleLabel;
	public TextMeshPro totalBetAmountLabel;
	public TextMeshPro winningsAmountLabel;
	public GameObject testGUICheckbox;
	public GameObject spinButtonControlParent;
	public ParticleSystem maxBetSparkleVFX;
	public ParticleSystem maxBetSparkleVFXSecondary;
	public GameObject sideInfoParent;
	public GameObject lowerLeftSideInfoParent;
	public GameObject lowerRightSideInfoParent;
	public GameObject upperLeftSideInfoParent;
	public GameObject upperRightSideInfoParent;
	public TextMeshPro[] sideInfoNumber;
	public TextMeshPro[] sideInfoText;
	public TextMeshPro[] upperSideInfoNumber;
	public TextMeshPro[] upperSideInfoText;
	public Transform topEdge;   // An empty GameObject at the top edge of the spin panel. Used for determining reel area space.
	public Transform normalWinningsObjectTransform; // transform for the winnings object, can be used to position game effects over the winning box
	public Transform bonusWinningsObjectTransform; // transform for the winnings object of the bonus spin panel, can be used to position game effects over the winning box

	public ClickAndHoldHandler autoSpinHandler;
	public ClickHandler spinButtonHandler;
	public MultiClickHandler multiClickHandler;
	public ImageButtonHandler stopButtonHandler;
	public ButtonHandler betUpButtonHandler;
	public ButtonHandler betDownButtonHandler;

	[Header("Swappers for Tall/Short Panel States")]
	public ObjectSwapper normalSpinPanelSwapper;
	public ObjectSwapper bonusSpinPanelSwapper;
	[Space]
	public BoxCollider2D reelBoundsLimit;
	public TextCycler autoSpinTextCycler;
	public Animator spinButtonSheenAnimator;
	public UISprite featureButtonSprite;
	public GameObject featuresOnTheSidesGameObject;
	
	[SerializeField] private GameObject collectionsPanelParent;

	[SerializeField] private GameObject eliteItemParent;
	[System.NonSerialized] public EliteSpinPanelItem eliteItem;

	[System.NonSerialized] public bool isButtonsEnabled = true;
	[System.NonSerialized] public bool isAutoSpinCountPanelActive = false;
	[System.NonSerialized] protected bool isAutoSpinSelectorActive = false;
	[System.NonSerialized] public bool shouldShowJackpot = false;
	[System.NonSerialized] public bool shouldShowMysteryGift = false;
	[System.NonSerialized] public bool shouldShowBigSlice = false;
	[System.NonSerialized] public bool shouldShowVIPRevampOverlay = false;
	[System.NonSerialized] public bool shouldShowMaxVoltageOverlay = false;
	[System.NonSerialized] public bool shouldShowRoyalRushOverlay = false;
	[System.NonSerialized] public static Vector3 reelBoundsMax = Vector3.zero;
	[System.NonSerialized] public Camera uiCamera;

	[System.NonSerialized] public CollectionsBetPanel collectionsPanel = null;
	protected VirtualPetSpinButton petSpinButton;

#if RWR
	[System.NonSerialized] public RWRSweepstakesMeter rwrSweepstakesMeter = null;
#endif

	private Dictionary<UIAnchor, bool> normalAnchorEnabledMap = new Dictionary<UIAnchor, bool>(); // store a UI anchor map for the normal spin panel, that can be restored after it is moved somewhere
	private Dictionary<UIAnchor, bool> bonusAnchorEnabledMap = new Dictionary<UIAnchor, bool>(); // stores a UI anchor map for the bonus spin panel, that can be restored after it is moved somewhere
	private GameTimer spinButtonThrobTimer = null;

	private const string INCREASE_BET_SOUND_PREFIX = "increasebet";
	private const string INCREASE_BET_MAX_SOUND = "increasebetMax";
	private const string DEFAULT_BUTTON_SOUND_KEY = "default_button";
	private const string MAX_BET_0_SOUND = "maxbet0";

	private const string MAX_BET_LOC_KEY = "max_bet";
	private const string TOTAL_BET_LOC_KEY = "total_bet";
	private const string BET_LOC_KEY = "bet";
	private const int SLIDE_PADDING = 50;

	private AlphaRestoreData spinPanelAlphaRestoreData = null;

	private bool isSlidingNormalSpinPanel = false;
	private bool isSlidingBonusSpinPanel = false;
	protected bool hasOffsetNormalSpinPanel = false;
	private bool hasOffsetBonusSpinPanel = false;
	private bool isHoldingMessageBox = false; //On non-orthographic games that don't scale up we don't hide/show the message box
	protected string lastSelectedAutoSpinAmount;
	protected bool isAutoSpinHeld;
	protected Transform specialButtonTransformToMatch;
	
	[HideInInspector] public bool isFaded = false;

	protected const string DEFAULT_STOP_BUTTON_SPRITE = "Button Stop Stretchy";
	protected const string DEFAULT_STOP_BUTTON_PRESSED_SPRITE = "Button Stop Pressed Stretchy";
	
	protected const string SPECIAL_BUTTON_SPRITE = "Button Spin Special Stretchy";
	protected const string SPECIAL_BUTTON_PRESSED_SPRITE = "Button Spin Special Pressed Stretchy";

	/// Returns the autoSpinActive object currently in use.
	public virtual GameObject effectiveAutoSpinActive
	{
		get { return autoSpinActive; }
	}

	/// Returns the autoSpinCountLabel object currently in use.
	public virtual TextMeshPro effectiveAutoSpinCountLabel
	{
		get { return autoSpinCountLabel; }
	}

	public long betAmount
	{
		get
		{
			if (SlotBaseGame.instance != null)
			{
				return SlotBaseGame.instance.betAmount;
			}

			return 0;
		}
	}

	private int changeBetIterator; /// Used for scripty audio logic with what sound to play when upping/lowering bet.

	protected List<long> multiplierList;
	protected int currentMultiplier;

	protected List<long> wagerList;
	protected int currentWagerIndex;

	public static long backupWagerAmount = 0; //Used for setting the starting wager amount when entering a game. Based on the wager the player was last seen spinning at

	// Returns whether we're showing any kind of special win overlay.
	public bool isShowingSpecialWinOverlay
	{
		get
		{
			return
				shouldShowJackpot ||
				shouldShowMysteryGift ||
				shouldShowBigSlice ||
				isShowingCollectionOverlay;
		}
	}

	public bool isShowingCollectionOverlay
	{
		get
		{
			return
				shouldShowVIPRevampOverlay ||
				shouldShowMaxVoltageOverlay ||
				shouldShowRoyalRushOverlay;
		}
	}

	protected ReelGame slot
	{
		get
		{
			return ReelGame.activeGame;
		}
	}

	public long currentWager
	{
		get
		{
			if (slot != null)
			{
				return slot.currentWager;
			}

			return 0;
		}
	}

	public string selectedAutoSpin
	{
		get
		{
			if (ExperimentWrapper.SpinPanelV2.hasAutoSpinOptions)
			{
				return lastSelectedAutoSpinAmount == null ? "" : lastSelectedAutoSpinAmount;
			}
			else
			{
				return "infinite";
			}
		}
	}

	protected virtual void Awake()
	{
		// Critical game startup error, game reset
		if (GameState.game == null ||
			SlotsPlayer.instance == null ||
			this == null)
		{
			Bugsnag.LeaveBreadcrumb("SpinPanel: Critical error when validating essential data");
			
			// Only reset when playing the game normally, in Art Setup scenes skip this
			if (!SceneManager.GetActiveScene().name.Contains("Art Setup"))
			{
				Glb.resetGame("Critical game error during spin panel Awake()");
			}

			return;
		}

		instance = this;
		hir = this as SpinPanelHIR;

		// Only show the progressive bar if the game is a progressive game 
		// (which isn't a built in progressive feature of the game) and 
		// we're not in a VIP game using the new vip revamp bar
		if (GameState.game.isProgressive && !GameState.game.isBuiltInProgressive) 
		{
			if ((GameState.game.isVIPGame && !ExperimentWrapper.VIPLobbyRevamp.isInExperiment) ||
				!GameState.game.isVIPGame)
			{
				shouldShowJackpot = true;
			}
			else
			{
				shouldShowJackpot = false;
			}
		}
		shouldShowVIPRevampOverlay = GameState.game.isVIPGame && ExperimentWrapper.VIPLobbyRevamp.isInExperiment; //Show the VIP Revamp bar if the game is a vip game and we're in the experiment
		shouldShowMaxVoltageOverlay = LoLaLobby.maxVoltage != null && GameState.game.eosControlledLobby == LoLaLobby.maxVoltage;

		LobbyGame currentGame = LobbyGame.find(GameState.game.keyName);
		shouldShowRoyalRushOverlay = ExperimentWrapper.RoyalRush.isInExperiment &&
			currentGame != null &&
			currentGame.isRoyalRush &&
			RoyalRushEvent.instance != null &&
			RoyalRushEvent.instance.getInfoByKey(GameState.game.keyName) != null;

		if (!shouldShowJackpot)
		{
			// Only show mystery gift if not also progressive.
			shouldShowMysteryGift = (GameState.game.mysteryGiftType == MysteryGiftType.MYSTERY_GIFT);
			shouldShowBigSlice = (GameState.game.mysteryGiftType == MysteryGiftType.BIG_SLICE);
		}

		if (!GameState.hasEventId &&
		    SlotsPlayer.instance.socialMember != null)
		{
			long baseWagerModifier = ExperimentWrapper.SmartBetSelector.isInExperiment ? ExperimentWrapper.SmartBetSelector.nonTopperModifier : Glb.DEFAULT_BANKROLL_BET_PCT;
			long targetValue = (SlotsPlayer.creditAmount * baseWagerModifier) / 100L;
			wagerList = SlotsWagerSets.getWagersUnlockedAtLevelFromGameWagerSet(GameState.game.keyName, SlotsPlayer.instance.socialMember.experienceLevel);

			currentWagerIndex = 0;
			if (!ExperimentWrapper.SmartBetSelector.isInExperiment)
			{
				if (wagerList != null)
				{
					for (int i = 0; i < wagerList.Count; i++)
					{
						if (wagerList[i] > targetValue)
						{
							break;
						}
						else
						{
							currentWagerIndex = i;
						}
					}
				}
				else
				{
					Bugsnag.LeaveBreadcrumb("SpinPanel -- Awake() -- wagerList was null");
				}
			}
			else
			{
				long defaultBetValue = GameState.game.getDefaultBetValue();
				string wagerSet = SlotsWagerSets.getWagerSetForGame(GameState.game.keyName);

				if (defaultBetValue >= wagerList[wagerList.Count - 1])
				{
					currentWagerIndex = wagerList.Count - 1;
				}
				else
				{
					currentWagerIndex = GameState.game.getSmartBetStartingIndex(wagerSet, wagerList.ToArray());
				}
			}
		}
		else
		{
			if (GameState.hasEventId)
			{
				Bugsnag.LeaveBreadcrumb("SpinPanel -- Awake() -- GameState has eventID, wager list will be null.");
			}

			if (SlotsPlayer.instance.socialMember == null)
			{
				Bugsnag.LeaveBreadcrumb("SpinPanel -- Awake() -- Social member is null, wager list might be null.");
			}
		}

		// Always turn of the TestGUI Checkbox. The functionality to see symbols has moved in the debug menu.
		if (testGUICheckbox != null)
		{
			testGUICheckbox.SetActive(false);
		}

		if (totalBetTitleLabel != null)
		{
			totalBetTitleLabel.text = Localize.textUpper(BET_LOC_KEY);
		}

		if (winningsAmountLabel != null)
		{
			winningsAmountLabel.text = "0";
		}

		// Show the nornal panel and hide the bonus panel by default.
		if (normalSpinPanel != null && bonusSpinPanel != null)
		{
			normalSpinPanel.SetActive(true);    // They must both be active, even when hidden.
			bonusSpinPanel.SetActive(true);     // They must both be active, even when hidden.
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("SpinPanel: Critical error when toggling spin panels.");
			Glb.resetGame("Critical game error during spin panel Awake()");
			return;
		}

		if (GameState.giftedBonus == null)
		{
			// Only show the normal spin panel if we know we are not going into a gifted bonus.
			showPanel(Type.NORMAL);
		}

		if (!GameState.hasEventId)
		{
			if (backupWagerAmount > 0)
			{
				// If a wager amount was stored when leaving a game earlier, then restore it now.
				setWager(getNearestWagerIndex(backupWagerAmount));
			}
			else
			{
				// Default wager setup.
				handleWagerChange(-1);  //use negative index because we no current wager
			}
		}

		// Since both the spin and stop buttons are always active,
		// set the initial visibility of them, which moves the stop button off screen.
		// Make sure the buttons are both active, just in case they were saved as inactive in the prefab.
		setButtons(true);

		// Set up a timer to throb the spin button if not spinning within a certain amount of time.
		// Throb the spin button at the start of each game too, so pass in 0 for time amount by default.
		spinButtonThrobTimer = new GameTimer(0);

		resetAutoSpinUI();

		if (TicketTumblerFeature.instance != null)
		{
			TicketTumblerFeature.instance.handleWagerChange();
		}

		if (autoSpinTextCycler != null)
		{
			autoSpinTextCycler.delay = ExperimentWrapper.SpinPanelV2.autoSpinTextCycleTime;
		}
		if (autoSpinHandler != null)
		{
			autoSpinHandler.holdTime = ExperimentWrapper.SpinPanelV2.autoSpinHoldDuration;
		}

		if (MainLobby.instance != null || ChallengeLobby.instance != null)
		{
			Debug.LogError("Trying to create a spin panel while in a lobby which shouldn't happen. Hiding ourself now");
			hidePanels();
		}

		if (Collectables.isActive() && wagerList != null && wagerList.Count > 0)
		{
			AssetBundleManager.load(this, "Features/Collections/Prefabs/Spin Panel/Collections Spin Panel Item", collectionsPanelLoadSuccess, collectionsPanelLoadFailed);
		}

		//Do not load the elite spin panel if this is gift bonus game
		if (EliteManager.isActive && GameState.giftedBonus == null)
		{
			AssetBundleManager.load(this, "Features/Elite/Prefabs/Instanced Prefabs/Elite Spin Panel Item", elitePanelLoadSuccess, elitePanelLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
		}

		
	}

	private void collectionsPanelLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			GameObject collectionsPanelObject = NGUITools.AddChild(collectionsPanelParent, obj as GameObject, true);
			collectionsPanel = collectionsPanelObject.GetComponent<CollectionsBetPanel>();
			if (collectionsPanel != null)
			{
				collectionsPanel.init(currentWagerIndex, wagerList.Count);
				collectionsPanel.gameObject.SetActive(false);
			}
		}
	}

	private void collectionsPanelLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load the collections spin panel panel");
	}

	private void elitePanelLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && gameObject != null)
		{
			GameObject item = NGUITools.AddChild(eliteItemParent, obj as GameObject, true);
			eliteItem = item.GetComponent<EliteSpinPanelItem>();
		}
	}

	private void elitePanelLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load the elite pass spin panel item");
	}
	
	

	protected virtual void Update()
	{
		if (isAutoSpinHeld && TouchInput.didTap)
		{
			isAutoSpinHeld = false;
		}
		else if (isAutoSpinSelectorActive)
		{
			if (TouchInput.didTap && !isAutoSpinHeld)
			{
				closeAutoSpinSelector();
			}
			else
			{
				AndroidUtil.checkBackButton(closeAutoSpinSelector);
			}
		}
		else if (isAutoSpinCountPanelActive)
		{
			if (!slot.hasAutoSpinsRemaining && autoSpinPanelAnimator != null)
			{
				hideAutoSpinPanel(true);
			}
		}
		else if (spinButtonThrobTimer != null && spinButtonThrobTimer.isExpired && !TouchInput.isTouchDown)
		{
			spinButtonThrobTimer = new GameTimer(SPIN_BUTTON_THROB_DELAY);
			if (spinButtonSheenAnimator != null)
			{
				spinButtonSheenAnimator.Play("sheen_on");
			}
		}
#if RWR
		if ((rwrSweepstakesMeter != null) && !SlotsPlayer.instance.getIsRWRSweepstakesActive())
		{
			Destroy(rwrSweepstakesMeter.gameObject);
			rwrSweepstakesMeter = null;
		}
#endif
	}

	/// Set the side UI things to show how many ways or lines to win.
	public void setSideInfo(int number, string localizationKey, bool show)
	{
		showSideInfo(false); //Always hide the ways boxes with the larger reels

		if (sideInfoText.Length >= 2)
		{
			sideInfoText[0].text = sideInfoText[1].text = Localize.textUpper(localizationKey);
			sideInfoNumber[0].text = sideInfoNumber[1].text = CommonText.formatNumber(number);
		}
	}

	/// Set the side UI things to show how many ways or lines to win.
	public void setDualSideInfo(int topNumber, int bottomNumber, string topLocalizationKey, string bottomLocalizationKey, bool show)
	{
		if (GameState.game.isChallengeLobbyGame)
		{
			// If this is a Land of Oz game, then we need to show the in-game UI on the left side,
			// so force this to not show the normal side info.
			show = false;
		}
		showDualSideInfo(show);

		if (upperSideInfoText.Length >= 2)
		{
			upperSideInfoText[1].text = upperSideInfoText[0].text = Localize.textUpper(topLocalizationKey);
			sideInfoText[0].text = sideInfoText[1].text = Localize.textUpper(bottomLocalizationKey);
			sideInfoNumber[0].text = sideInfoNumber[1].text = CommonText.formatNumber(bottomNumber);
			upperSideInfoNumber[1].text = upperSideInfoNumber[0].text = CommonText.formatNumber(topNumber);
		}
	}

	/// Hide or show the side "ways/lines" boxes by changing layers.
	public void showSideInfo(bool doShow)
	{
		CommonGameObject.setLayerRecursively(sideInfoParent, (doShow ? Layers.ID_NGUI : Layers.ID_HIDDEN));
		StartCoroutine(waitForGameThenSetSideInfo());
	}

	// this is necessary for side info to be correctly hidden in some games, need to wait until the game is there
	private IEnumerator waitForGameThenSetSideInfo()
	{
		while (slot == null)
		{
			yield return null;
		}

		if (GameState.game.isChallengeLobbyGame)
		{
			// If this is a Land of Oz game, then we need to show the in-game UI on the left side,
			// so force this to not show the normal side info.
			slot.showSideInfo = false;
		}
		SafeSet.gameObjectActive(lowerRightSideInfoParent, slot.showSideInfo);
		SafeSet.gameObjectActive(lowerLeftSideInfoParent, slot.showSideInfo);
	}

	/// Hide or show the side "ways/lines" boxes by changing layers.
	public void showDualSideInfo(bool doShow)
	{

		CommonGameObject.setLayerRecursively(sideInfoParent, (doShow ? Layers.ID_NGUI : Layers.ID_HIDDEN));
		if (upperLeftSideInfoParent != null)
		{
			upperLeftSideInfoParent.SetActive(doShow);
		}

		if (upperRightSideInfoParent != null)
		{
			upperRightSideInfoParent.SetActive(doShow);
		}

		Vector3 lowerSideOffset = new Vector3(0, -260.0f, 0);

		if (lowerLeftSideInfoParent != null)
		{
			lowerLeftSideInfoParent.transform.localPosition += lowerSideOffset;
		}

		if (lowerRightSideInfoParent != null)
		{
			lowerRightSideInfoParent.transform.localPosition += lowerSideOffset;
		}
	}

	/// Sets the message area message.
	public void setMessageText(string msg)
	{
		messageLabel.text = msg;
	}

	public void updateMaxBet()
	{
		if (GameState.game != null && SlotsPlayer.instance.socialMember != null)
		{
			wagerList = SlotsWagerSets.getWagersUnlockedAtLevelFromGameWagerSet(GameState.game.keyName, SlotsPlayer.instance.socialMember.experienceLevel);
			handleWagerChange(currentWagerIndex);
		}
	}

	/// Shows one of the spin panel options and hides the others.
	public virtual void showPanel(Type type)
	{
		hidePanels();

		switch (type)
		{
			case Type.NORMAL:
				CommonGameObject.setLayerRecursively(normalSpinPanel, gameObject.layer);
				if (isShowingSpecialWinOverlay)
				{
					StartCoroutine(Overlay.instance.showJackpotMysteryWhenNotNull());
				}
				if (hir != null)
				{
					bool isRobustChallengesGame = RobustCampaign.hasActiveRobustCampaignInstance && CampaignDirector.robust.currentMission != null;
					if (hir.featureButtonHandler != null)
					{
						hir.featureButtonHandler.showRobustChallengesInGame(isRobustChallengesGame);
					}
					ChallengeCampaign campaign = CampaignDirector.findWithGame(GameState.game.keyName);
					bool isChallengeUIActive = campaign != null && campaign.currentMission != null && GameState.game != null && GameState.game.isChallengeLobbyGame;
					hir.showSlotventuresUIInGame(isChallengeUIActive);
					InGameFeatureContainer.showFeatureUI(true);
					if (Dialog.instance.currentDialog == VirtualPetRespinOverlayDialog.instance && VirtualPetRespinOverlayDialog.instance != null)
					{
						VirtualPetRespinOverlayDialog.instance.show();
					}
				}
				
				break;
			case Type.FREE_SPINS:
				CommonGameObject.setLayerRecursively(bonusSpinPanel, gameObject.layer);
				if (hir != null)
				{
					if (hir.featureButtonHandler != null)
					{
						hir.featureButtonHandler.showRobustChallengesInGame(false);
					}
					hir.showSlotventuresUIInGame(false);
					InGameFeatureContainer.showFeatureUI(false);
					if (Dialog.instance.currentDialog == VirtualPetRespinOverlayDialog.instance && VirtualPetRespinOverlayDialog.instance != null)
					{
						VirtualPetRespinOverlayDialog.instance.hide();
					}
				}
				if (FreeSpinGame.instance != null)
				{
					string freespinLocalizationKey = FreeSpinGame.instance.spinsRemainingLabelLocalizationKey;
					BonusSpinPanel.instance.spinsRemainingLabel.text = Localize.textUpper(freespinLocalizationKey);
				}
				break;
		}

		CommonGameObject.setLayerRecursively(featuresOnTheSidesGameObject, gameObject.layer);
		InGameFeatureContainer.toggleLayer(gameObject.layer == Layers.ID_HIDDEN);
	}

	// Shows/hides the UI that's on Spinpanel, this doesn't inclue the spin panel itself, just the feature stuff.
	public virtual void showFeatureUI(bool show)
	{
	}

	// A cheat for hiding all feature UI. showFeatureUI appears to be used very specifically in some places 
	// for legit reasons. DO NOT use outside of testing
	public virtual void forceShowFeatureUI(bool show)
	{
	}

	public void modifyBonusGameLabel(string customText)
	{
		bonusGameTypeLabel.text = Localize.textUpper(customText);
	}

	/// Hides both panel options.
	public void hidePanels()
	{
		CommonGameObject.setLayerRecursively(normalSpinPanel, Layers.ID_HIDDEN);
		CommonGameObject.setLayerRecursively(bonusSpinPanel, Layers.ID_HIDDEN);
		CommonGameObject.setLayerRecursively(sideInfoParent, Layers.ID_HIDDEN);
		CommonGameObject.setLayerRecursively(featuresOnTheSidesGameObject, Layers.ID_HIDDEN);

		InGameFeatureContainer.toggleLayer(true);

		if (isShowingSpecialWinOverlay)
		{
			StartCoroutine(Overlay.instance.hideJackpotMysteryWhenNotNull());
		}
		
		if (Dialog.instance.currentDialog == VirtualPetRespinOverlayDialog.instance && VirtualPetRespinOverlayDialog.instance != null)
		{
			VirtualPetRespinOverlayDialog.instance.hide();
		}
	}
	
	void OnDestroy()
	{
		hidePanels();
		InGameFeatureContainer.removeAllObjects();
	}

	public virtual void clickSpin()
	{
		StatsManager.Instance.LogCount(
			counterName : "game_actions",
			kingdom : "spin",
			phylum : "hir_spin_panel_v2",
			klass : GameState.game.keyName,
			family : "spin",
			val : betAmount
		);

		(slot as SlotBaseGame).validateSpin();
	}

	public void slideInPaylineMessageBox()
	{
		if (messageBoxAnimator != null && !isHoldingMessageBox && !messageBoxAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.on"))
		{
			messageBoxAnimator.Play("on");
		}
	}

	public void slideOutPaylineMessageBox()
	{
		if (messageBoxAnimator != null && !isHoldingMessageBox && messageBoxAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.on"))
		{
			messageBoxAnimator.Play("off");
		}
	}

	public void holdPaylineMessageBox()
	{
		if (messageBoxAnimator != null)
		{
			messageBoxAnimator.Play("hold");
			isHoldingMessageBox = true;
		}
	}

	public void clickStop()
	{
		if (stopButtonHandler != null)
		{
			stopButtonHandler.button.UpdateColor(false, true);
		}

		if (slot.hasAutoSpinsRemaining && autoSpinPanelAnimator != null)
		{
			hideAutoSpinPanel(true);
		}

		if (stopButton != null)
		{
			stopButton.isEnabled = false;
		}

		//Audio.play(Audio.soundMap("slam_reels"));
		if (slot != null)
		{
			slot.stopSpin();
		}

	}

	public void clickMaxBet()
	{
		if (slot == null || slot.slotGameData == null)
		{
			// Do nothing if there is no bet data
		}
		else if (wagerList == null || wagerList.Count == 0)
		{
			// Using flat wagers but they aren't setup
			Debug.LogError("Using flat wagers but they aren't setup");
		}
		else
		{
			confirmMaxBet();
		}
	}
	
	private void confirmMaxBet()
	{
		// If the game is idle, the audio won't play, so we force it to no longer be idle
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = Time.time;
		}
		
		// Store the current multiplier before we change it to max bet multiplier
		int oldWagerIndex = currentWagerIndex;
		currentWagerIndex = wagerList.Count - 1;
		// For restoring wager amount when returning from gift bonus game, don't use handleWagerChange. Zhennian.
		setWager(oldWagerIndex, currentWagerIndex);

		StatsManager.Instance.LogCount("game_actions", "spin", StatsManager.getGameTheme(), StatsManager.getGameName(),"betmax","click" , betAmount);
		changeBetIterator = 0;
		Audio.play("maxbet0");
		maxBetSparkleVFX.Play();

		if (Overlay.instance.jackpotMystery != null)
		{
			if (isShowingCollectionOverlay && Overlay.instance.jackpotMystery.tokenBar != null)
			{
				Overlay.instance.jackpotMystery.tokenBar.betChanged(true);
			}
		}
	}

	public void clickBetUp()
	{
		if ((betUpButton != null && !isBetButtonEnabled(betUpButton)) ||
			(betUpButtonHandler != null && !betUpButtonHandler.isEnabled) ||
			(SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy))
		{
			// This check is here so that key presses don't call this when they shouldn't be able to.
			return;
		}

		StatsManager.Instance.LogCount(
			counterName : "game_actions",
			kingdom : "bet_higher",
			phylum : "hir_spin_panel_v2",
			klass : GameState.game.keyName,
			genus : "click"
		);

		// If the game is idle, the audio won't play, so we force it to no longer be idle
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = Time.time;
		}

		if (wagerList == null)
		{
			Debug.LogError("Wager set is empty for game: " + GameState.game.keyName);
		}

		int oldWagerIndex = currentWagerIndex;
		currentWagerIndex = Mathf.Min(wagerList.Count - 1, currentWagerIndex + 1);
		// For restoring wager amount when returning from gift bonus game, don't use handleWagerChange. Zhennian.
		setWager(oldWagerIndex, currentWagerIndex);

		if (changeBetIterator < 0 )
		{
			changeBetIterator = 0;
		}

		if (currentWagerIndex == wagerList.Count - 1)
		{
			// If maximum, play the max bet sound.
			Audio.play(MAX_BET_0_SOUND);

			maxBetSparkleVFX.Play();
			if (maxBetSparkleVFXSecondary != null)
			{
				maxBetSparkleVFXSecondary.Play();
			}
		}
		else
		{
			if (!shouldShowMaxVoltageOverlay)
			{
				Audio.play(getBetChangeSound(changeBetIterator, true));
			}
		}

		if (isShowingSpecialWinOverlay &&
			Overlay.instance != null &&
			Overlay.instance.jackpotMystery != null)
		{
			if (isShowingCollectionOverlay && Overlay.instance.jackpotMystery.tokenBar != null)
			{
				Overlay.instance.jackpotMystery.tokenBar.betChanged(true);
			}
		}
		
		checkFeaturesPanel();

		TicketTumblerFeature.instance.handleWagerChange();
		changeBetIterator++;
	}

	private void checkFeaturesPanel()
	{
		if (collectionsPanel != null && (eliteItem == null || eliteItem != null && !eliteItem.isShowing))
		{
			collectionsPanel.onBetChanged(currentWagerIndex);
		}

		//Check if the Elite panel needs to be shown.
		//If so, check if collectionspanel is not null and close it if its already open
		if (eliteItem != null && eliteItem.onBetChanged(wagerList[currentWagerIndex]))
		{
			if (collectionsPanel != null)
			{
				collectionsPanel.closePanel();
			}
		}
		
		InGameFeatureContainer.onBetChagned(wagerList[currentWagerIndex]);
	}

	/// get the bet increment/decrement sound to be played (since apparently it skips over increasebet1, so I still need a case statement in each direction)
	private string getBetChangeSound(int changeBetIterator, bool isIncreasingBet)
	{
		if (isIncreasingBet)
		{
			int betSoundIndex = 0;

			switch (changeBetIterator)
			{
				case 0:
					betSoundIndex = 0;
					break;
				case 1: 
					betSoundIndex = 2;
					break;
				case 2: 
					betSoundIndex = 3;
					break;
				case 3:
					betSoundIndex = 4;
					break;
				case 4:
					return INCREASE_BET_MAX_SOUND;
				default:
					return Audio.soundMap(DEFAULT_BUTTON_SOUND_KEY);
			}

			return INCREASE_BET_SOUND_PREFIX + betSoundIndex;
		}
		else
		{
			int betSoundIndex = 0;

			switch (changeBetIterator)
			{
				case 0:
					return INCREASE_BET_MAX_SOUND;
				case -1:
					betSoundIndex = 4;
					break;
				case -2:
					betSoundIndex = 3;
					break;
				case -3:
					betSoundIndex = 2;
					break;
				case -4:
					betSoundIndex = 0;
					break;
				default:
					return Audio.soundMap(DEFAULT_BUTTON_SOUND_KEY);
			}

			return INCREASE_BET_SOUND_PREFIX + betSoundIndex;
		}
	}

	public void clickBetDown()
	{
		if ((betDownButton != null && !isBetButtonEnabled(betDownButton)) ||
			(betDownButtonHandler != null && !betDownButtonHandler.isEnabled) ||
			(SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy))
		{
			// This check is here so that key presses don't call this when they shouldn't be able to.
			return;
		}

		StatsManager.Instance.LogCount(
			counterName : "game_actions",
			kingdom : "bet_lower",
			phylum : "hir_spin_panel_v2",
			klass : GameState.game.keyName,
			genus : "click"
		);

		// If the game is idle, the audio won't play, so we force it to no longer be idle
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = Time.time;
		}

		int oldWagerIndex = currentWagerIndex;
		currentWagerIndex = Mathf.Max(0, currentWagerIndex - 1);
		// For restoring wager amount when returning from gift bonus game, don't use handleWagerChange. Zhennian.
		setWager(oldWagerIndex, currentWagerIndex);

		if (changeBetIterator > 0 )
		{
			changeBetIterator = 0;
		}
		if (currentWagerIndex == 0)
		{
			// If minimum, play the lowest bet sound.
			Audio.play("decreasebet0");
		}
		else
		{
			if (shouldShowMaxVoltageOverlay )
			{
				
			}
			else
			{
				if (!shouldShowMaxVoltageOverlay)
				{
					Audio.play(getBetChangeSound(changeBetIterator, false));
				}
			}
		}

		if (Overlay.instance.jackpotMystery != null)
		{
			if (isShowingCollectionOverlay && Overlay.instance.jackpotMystery.tokenBar != null)
			{
				Overlay.instance.jackpotMystery.tokenBar.betChanged(false);
			}
		}
		
		checkFeaturesPanel();

		changeBetIterator--;

		TicketTumblerFeature.instance.handleWagerChange();

	}

	private void clickPaytable()
	{
		PaytablesDialog.showDialog();
		Audio.play("minimenuclose0");
	}

	private void clickAutoSpin()
	{
		// Show the auto spin count panel.
		autoSpinCountPanel.SetActive(true);

		isAutoSpinCountPanelActive = true;
		autoSpinCountMask.enabled = true;
		autoSpinCountPanel.transform.localScale = new Vector3(1.0f, .01f, 1);
		iTween.ScaleTo(autoSpinCountPanel, iTween.Hash("y", 1.0f, "time", .25f, "easetype", iTween.EaseType.easeOutElastic));

		Audio.play(Audio.soundMap("autospinopen"));
	}


	protected void clickAutoSpinAmount(int amount)
	{
		//set the last selected amount for logging purposes
		lastSelectedAutoSpinAmount = amount < 0 ? "infinite" : amount.ToString();

		StatsManager.Instance.LogCount(
			counterName : "game_actions",
			kingdom : "autospin",
			phylum : betAmount.ToString(),
			klass : GameState.game.keyName,
			family : amount.ToString(),
			genus : "click"
		);

		if (hir != null && hir.objectivesGrid != null)
		{
			hir.objectivesGrid.onSelectAutoSpin();
		}

		// Show the auto spin active counter.
		// Position the hidden one off screen instead of activating/deactivating, due to visible lag on slow devices when doing that, thanks to NGUI.
		if (autoSpinButton != null)
		{
			CommonTransform.setY(autoSpinButton.transform, OFFSCREEN_BUTTON_LOCATION);
		}

		if (effectiveAutoSpinActive != null)
		{
			CommonTransform.setY(effectiveAutoSpinActive.transform, 0);
		}

		// If you don't have enough coins, startAutoSpin will immediately re-enable the auto spin button
		// (by positioning it back onto the screen).
		slot.startAutoSpin(amount);

		if (effectiveAutoSpinActive != null)
		{
			// Give it a little animated bounce for flare.
			StartCoroutine(CommonEffects.throb(effectiveAutoSpinActive, 1.1f, .25f));
		}

		Audio.play(Audio.soundMap("autospin"));
	}

	public virtual void closeAutoSpinSelector()
	{
		hideAutoSpinPanel();
	}

	/// The second half of closeAutoSpinCount().
	private void finishCloseAutoSpinCount()
	{
		autoSpinCountPanel.SetActive(false);
		autoSpinCountMask.enabled = false;
		isAutoSpinCountPanelActive = false;
	}

	/// Changes the auto spin active UI back to the normal auto spin button.
	public void resetAutoSpinUI()
	{
		// Show the auto spin active counter.
		// Position the hidden one off screen instead of activating/deactivating, due to visible lag on slow devices when doing that, thanks to NGUI.
		if (autoSpinButton != null && autoSpinButton.transform != null)
		{
			CommonTransform.setY(autoSpinButton.transform, 0);
		}

		if (effectiveAutoSpinActive != null && effectiveAutoSpinActive.transform != null)
		{
			CommonTransform.setY(effectiveAutoSpinActive.transform, OFFSCREEN_BUTTON_LOCATION);
		}

		if (slot != null && autoSpinPanelAnimator != null && autoSpinPanelAnimator.gameObject != null)
		{
			if (isAutoSpinCountPanelActive && !slot.hasAutoSpinsRemaining)
			{
				isAutoSpinSelectorActive = false;
				isAutoSpinCountPanelActive = false;
				autoSpinPanelAnimator.Play("selected off");
			}
			else if (slot.hasAutoSpinsRemaining && !isAutoSpinCountPanelActive)
			{
				isAutoSpinSelectorActive = false;
				isAutoSpinCountPanelActive = true;
				autoSpinPanelAnimator.Play("on hold");

			}
		}
		
		if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isHyper)
		{
			hir.turnOnPetsSpinButton(VirtualPetSpinButton.TrickMode.HYPER);
		}
	}

	// The bet up/down buttons have two different UIImageButton scripts attached to each.
	// This function sets each one to enabled or not.
	protected void setBetButtonEnabled(GameObject button, bool isEnabled)
	{
		if (button == null)
		{
			return;
		}
		UIImageButton[] scripts = button.GetComponentsInChildren<UIImageButton>();
		foreach (UIImageButton script in scripts)
		{
			script.isEnabled = isEnabled;
			script.target.spriteName = isEnabled ? script.normalSprite : script.disabledSprite;
		}
	}
	
	// Is the bet button enabled? Checks using same logic that sets it. Would be better if we tracked this, but meh.
	protected bool isBetButtonEnabled(GameObject button)
	{
		if (button == null)
		{
			return false;
		}
		UIImageButton[] scripts = button.GetComponentsInChildren<UIImageButton>();
		foreach (UIImageButton script in scripts)
		{
			return script.isEnabled;
		}

		return false;
	}

	// Set the multiplier to any arbitary one in the list.
	public void setMultiplier(int newIndex)
	{
		currentMultiplier = Mathf.Clamp(newIndex, 0, multiplierList.Count - 1);
		handleMultiplierChange();
	}

	protected virtual void handleMultiplierChange()
	{
		if (multiplierList.Count == 0)
		{
			// Don't bother reporting anything, because that is already done in a couple other places when this issue happens.
			return;
		}

		if (betUpButton != null)
		{
			setBetButtonEnabled(betUpButton, isButtonsEnabled && currentMultiplier < (multiplierList.Count - 1));
		}
		else if (betUpButtonHandler != null)
		{
			betUpButtonHandler.button.isEnabled = isButtonsEnabled && currentMultiplier < (multiplierList.Count - 1);
		}

		if (betDownButton != null)
		{
			setBetButtonEnabled(betDownButton, isButtonsEnabled && currentMultiplier > 0);
		}
		else if (betDownButtonHandler != null)
		{
			betDownButtonHandler.button.isEnabled = isButtonsEnabled && currentMultiplier > 0;
		}


		long oldMultiplier = slot.multiplier;

		if (oldMultiplier != multiplierList[(int)currentMultiplier])
		{
			slot.multiplier = multiplierList[(int)currentMultiplier];
		}
		
		if (totalBetTitleLabel != null)
		{
			if (currentMultiplier == multiplierList.Count - 1)
			{
				// If maximum, set the label to MAX BET
				totalBetTitleLabel.text = Localize.textUpper(MAX_BET_LOC_KEY);
			}
			else
			{
				totalBetTitleLabel.text = Localize.textUpper(BET_LOC_KEY);
			}
		}
		if (isShowingSpecialWinOverlay)
		{
			long min = GameState.game.specialGameMinQualifyingAmount;
		
			if ((oldMultiplier < min && slot.multiplier >= min) ||
				(oldMultiplier >= min && slot.multiplier < min)
				)
			{
				// The player has crossed over the valid bet range for the jackpot.
				Overlay.instance.jackpotMystery.setQualifiedStatus();
			}
		}
	}

	/// Set the wager value from somewhere external, some dialogs may hook into this, or something may force the value when loading into a game
	public void setWager(int newWagerIndex)
	{
		setWager(currentWagerIndex, newWagerIndex);
	}
	private void setWager(int oldWagerIndex, int newWagerIndex)
	{
		currentWagerIndex = Mathf.Clamp(newWagerIndex, 0, wagerList.Count - 1);
		handleWagerChange(oldWagerIndex);
	}

	protected virtual void handleWagerChange(int oldWagerIndex)
	{
		if (wagerList.Count == 0)
		{
			// Don't bother reporting anything, because that is already done in a couple other places when this issue happens.
			return;
		}
		// protected from level up events causing indexing errors
		else if (currentWagerIndex >= wagerList.Count)
		{
			currentWagerIndex = wagerList.Count - 1;
		}
		// this will probably never happen, but extra safe
		else
		{
			currentWagerIndex = Mathf.Max(currentWagerIndex, 0);
		}

		int indexChange = currentWagerIndex - oldWagerIndex;

		backupWagerAmount = wagerList[currentWagerIndex];

		if (betUpButton != null)
		{
			setBetButtonEnabled(betUpButton, isButtonsEnabled && currentWagerIndex < (wagerList.Count - 1));
		}
		else if (betUpButtonHandler != null)
		{
			betUpButtonHandler.button.isEnabled = isButtonsEnabled && currentWagerIndex < (wagerList.Count - 1);
		}

		if (betDownButton != null)
		{
			setBetButtonEnabled(betDownButton, isButtonsEnabled && currentWagerIndex > 0);
		}
		else if (betDownButtonHandler)
		{
			betDownButtonHandler.button.isEnabled = isButtonsEnabled && currentWagerIndex > 0;
		}

		long oldWager = slot.currentWager;

		if (currentWagerIndex >= 0 && oldWager != wagerList[currentWagerIndex])
		{
			slot.currentWager = wagerList[currentWagerIndex];
			totalBetAmountLabel.text = CreditsEconomy.convertCredits(slot.currentWager);
		}
		
		if (totalBetTitleLabel != null)
		{
			if (currentWagerIndex == wagerList.Count - 1)
			{
				// If maximum, set the label to MAX BET
				totalBetTitleLabel.text = Localize.textUpper(MAX_BET_LOC_KEY);
			}
			else
			{
				totalBetTitleLabel.text = Localize.textUpper(BET_LOC_KEY);
			}
		}
		if (isShowingSpecialWinOverlay && !isShowingCollectionOverlay)
		{
			long min = GameState.game.specialGameMinQualifyingAmount;

			if ((oldWager < min && slot.currentWager >= min) ||
				(oldWager >= min && slot.currentWager < min)
				)
			{
				if (Overlay.instance.jackpotMystery != null)
				{
					// The player has crossed over the valid bet range for the jackpot.
					Overlay.instance.jackpotMystery.setQualifiedStatus();
				}
			}
		}

		slot.multiplier = SlotsWagerSets.getMultiplierForGameWagerSetValue(GameState.game.keyName, slot.currentWager);

		if (QuestForTheChestFeature.instance != null && indexChange != 0)
		{
			QuestForTheChestFeature.instance.handleWagerChange();
		}

		foreach (SlotModule module in ReelGame.activeGame.cachedAttachedSlotModules) 
		{
			if (module.needsToExecuteOnWagerChange(slot.currentWager))
			{
				module.executeOnWagerChange(slot.currentWager);
			}
		}
	}

	/// Enables or disables the UI buttons based on whether spinning is allowed.
	public virtual void setButtons(bool isEnabled)
	{
		if (isButtonsEnabled != isEnabled)
		{
			// The status has changed.
			if (!isEnabled)
			{
				// Make sure no spin button throbbing happens while spinning.
				spinButtonThrobTimer = null;
			}
			else
			{
				// Spinning has stopped, so start this timer.
				spinButtonThrobTimer = new GameTimer(SPIN_BUTTON_THROB_DELAY);
				spinButton.transform.localScale = Vector3.one;
			}
		}
		
		isButtonsEnabled = isEnabled;
		if (betUpButton != null)
		{
			setBetButtonEnabled(betUpButton, isEnabled && wagerList != null && currentWagerIndex < (wagerList.Count - 1));
		}
		else if (betUpButtonHandler != null)
		{
			betUpButtonHandler.button.isEnabled = isEnabled && wagerList != null && currentWagerIndex < (wagerList.Count - 1);
		}

		if (betDownButton != null)
		{
			setBetButtonEnabled(betDownButton, isEnabled && currentWagerIndex > 0);
		}
		else if (betDownButtonHandler != null)
		{
			betDownButtonHandler.button.isEnabled = isEnabled && currentWagerIndex > 0;
		}


		if (autoSpinButton != null)
		{
 			autoSpinButton.isEnabled = isEnabled;
		}
		// Position off screen instead of inactivating, due to NGUI delay when reactivating.
		CommonTransform.setY(spinButton.transform, (isEnabled ? 0 : OFFSCREEN_BUTTON_LOCATION));
		CommonTransform.setY(stopButton.transform, (isEnabled ? OFFSCREEN_BUTTON_LOCATION : 0));

		if (petSpinButton != null)
		{
			CommonTransform.setY(petSpinButton.transform, specialButtonTransformToMatch.localPosition.y);
		}
#if RWR
		if (rwrSweepstakesMeter != null)
		{
			rwrSweepstakesMeter.setEnabled(isButtonsEnabled);
		}
	#endif

		Overlay.instance.topHIR.setButtons(isEnabled);
		// Always make sure the stop button is enabled at this point.
		// It only gets disabled when clicked, then re-enabled here.
		if (stopButtonHandler != null)
		{
			stopButtonHandler.button.UpdateColor(true, true);
		}

		if (stopButton != null)
		{
			stopButton.isEnabled = true;
		}
	}

	private void toggleTestGUI(bool isChecked)
	{
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.testGUI = isChecked;
		}
	}
	
	// This function removes bet multipliers from the end of the list if the resulting bet amount is too high.
	// Since the indexing of the list is important, we MUST only remove items from the END of the list.
	private void capMultiplierList()
	{
		long maxBet = Glb.MAX_BET_AMOUNT;

		if (maxBet < 0L)
		{
			return;
		}
	
		if (multiplierList.Count == 0)
		{
			Data.showIssue("No multipliers are defined for game " + GameState.game.keyName + ". This means the game won't work right.");
			return;
		}
	
		List<long> cappedList = new List<long>();
		
		// Always add the lowest multiplier
		cappedList.Add(multiplierList[0]);
		
		// Add additional options as long as they meet our requirement
		int i = 1;
		while (i < multiplierList.Count && slot.slotGameData.getBetAmount(multiplierList[i]) <= maxBet)
		{
			cappedList.Add(multiplierList[i]);
			i++;
		}
		
		multiplierList = cappedList;
	}

	public void setSpinPanelPosition(Type type, SpinPanelSlideOutDirEnum direction, bool isWingsDistance)
	{
		if (type == Type.FREE_SPINS)
		{
			if (FreeSpinGame.instance != null)
			{
				freeSpinsBackgroundWingsWidth.gameObject.SetActive(!FreeSpinGame.instance.reelGameBackground.isUsingOrthoCameras && !ReelGame.activeGame.reelGameBackground.hasStaticReelArea);
			}
			else
			{
				freeSpinsBackgroundWingsWidth.gameObject.SetActive(backgroundWingsWidth.gameObject.activeSelf);
			}
		}
		else if (type == Type.NORMAL)
		{
			backgroundWingsWidth.gameObject.SetActive(!ReelGame.activeGame.reelGameBackground.isUsingOrthoCameras && !ReelGame.activeGame.reelGameBackground.hasStaticReelArea);
		}

		if (type == Type.NORMAL)
		{
			normalAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(normalSpinPanel);
			CommonGameObject.disableUIAnchorsForGameObject(normalSpinPanel);
			Vector3 targetPosition = getSlidePosition(type, direction, isWingsDistance);
			normalSpinPanel.transform.localPosition = targetPosition;
		}
		else if (type == Type.FREE_SPINS)
		{
			bonusAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(bonusSpinPanel);
			CommonGameObject.disableUIAnchorsForGameObject(bonusSpinPanel);
			Vector3 targetPosition = getSlidePosition(type, direction, isWingsDistance);
			bonusSpinPanel.transform.localPosition = targetPosition;
		}

		if (Overlay.instance != null)
		{
			Overlay.instance.setBackingSpriteVisible(backgroundWingsWidth.gameObject.activeSelf);
		}
	}

	/// Slides the top bar off the screen
	public IEnumerator slideSpinPanelOut(Type type, SpinPanelSlideOutDirEnum direction, float duration, bool isWingsDistance)
	{
		GameObject spinPanel = null;
		
		if (type == Type.NORMAL)
		{
			spinPanel = normalSpinPanel;
		}
		else if (type == Type.FREE_SPINS)
		{
			spinPanel = bonusSpinPanel;
		}

		if (spinPanel != null)
		{
			if (type == Type.NORMAL)
			{
				normalAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(spinPanel);
			}
			else if (type == Type.FREE_SPINS)
			{
				bonusAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(spinPanel);
			}

			CommonGameObject.disableUIAnchorsForGameObject(spinPanel);
			Vector3 targetPosition = getSlidePosition(type, direction, isWingsDistance);

			if (duration > 0.0f)
			{
				iTween.MoveTo(spinPanel, iTween.Hash("position", targetPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
				yield return new WaitForSeconds(duration);
			}
			else
			{
				// go straight to the out position
				spinPanel.transform.localPosition = new Vector3(targetPosition.x, targetPosition.y, spinPanel.transform.localPosition.z);
			}
		}
	}

	/// Slides the top bar off the screen
	public IEnumerator slideSpinPanelInFrom(Type type, SpinPanelSlideOutDirEnum direction, float duration, bool isWingsDistance)
	{
		Dictionary<UIAnchor, bool> anchorMap = null;

		if (type == Type.NORMAL)
		{
			anchorMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(normalSpinPanel);
			CommonGameObject.disableUIAnchorsForGameObject(normalSpinPanel);
		}
		else if (type == Type.FREE_SPINS)
		{
			anchorMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(bonusSpinPanel);
			CommonGameObject.disableUIAnchorsForGameObject(bonusSpinPanel);
		}

		if (type == Type.NORMAL)
		{
			setSpinPanelPosition(type, direction, isWingsDistance);
			yield return new TITweenYieldInstruction(iTween.MoveTo(normalSpinPanel, iTween.Hash("position", Vector3.zero, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear)));
			CommonGameObject.restoreUIAnchorActiveMapToGameObject(normalSpinPanel, anchorMap);
		}
		else if (type == Type.FREE_SPINS)
		{
			bonusSpinPanelAnchor.pixelOffset = Vector2.zero;
			setSpinPanelPosition(type, direction, isWingsDistance);
			yield return new TITweenYieldInstruction(iTween.MoveTo(bonusSpinPanel, iTween.Hash("position", Vector3.zero, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear)));
			CommonGameObject.restoreUIAnchorActiveMapToGameObject(bonusSpinPanel, anchorMap);
		}
	}

	private Vector2 getSlidePosition(Type type, SpinPanelSlideOutDirEnum direction, bool isWingsDistance)
	{
		Vector3 targetPosition = Vector3.zero;

		switch (direction)
		{
			case SpinPanelSlideOutDirEnum.Left:
				targetPosition.x = -(isWingsDistance ? backgroundWingsWidth.localScale.x : NGUIExt.effectiveScreenWidth);
				break;
			case SpinPanelSlideOutDirEnum.Right:
				targetPosition.x = (isWingsDistance ? backgroundWingsWidth.localScale.x : NGUIExt.effectiveScreenWidth);
				break;
			case SpinPanelSlideOutDirEnum.Up:
				targetPosition.y = NGUIExt.effectiveScreenHeight;
				break;
			case SpinPanelSlideOutDirEnum.Down:
				if (type == Type.NORMAL)
				{
					// adjust by a full screen height to make sure we move anything attached off the screen as well, like the tournements panel
					targetPosition.y = -NGUIExt.effectiveScreenHeight;
				}
				else if (type == Type.FREE_SPINS)
				{
					targetPosition.y = -(freeSpinsTiledBackground.transform.localScale.y + SLIDE_PADDING);
				}
				break;
		}
	
		return targetPosition;
	}

	// Get the winnings transform which can be used to position an effect over the win box
	public Transform getWinningsObjectTransform(Type type)
	{
		if (type == Type.FREE_SPINS)
		{
			return bonusWinningsObjectTransform;
		}
		else
		{
			return normalWinningsObjectTransform;
		}
	}

	public IEnumerator swapSpinPanels(Type swapTo, SpinPanelSlideOutDirEnum direction, float duration, bool isWingsDistance)
	{
		//Initialize Variables
		Vector3 normalSpinPanelPosition = Vector3.zero;
		Vector3 bonusSpinPanelPosition = Vector3.zero;
		SpinPanelSlideOutDirEnum oppositeDirection;

		//Get the opposite of the tween direction so we know where the other panel is coming in from
		switch (direction)
		{
			case SpinPanelSlideOutDirEnum.Left:
				oppositeDirection = SpinPanelSlideOutDirEnum.Right;
				break;
			case SpinPanelSlideOutDirEnum.Right:
				oppositeDirection = SpinPanelSlideOutDirEnum.Left;
				break;
			case SpinPanelSlideOutDirEnum.Up:
				oppositeDirection = SpinPanelSlideOutDirEnum.Down;
				break;
			case SpinPanelSlideOutDirEnum.Down:
				oppositeDirection = SpinPanelSlideOutDirEnum.Up;
				break;
			default:
				oppositeDirection = SpinPanelSlideOutDirEnum.Right;
				break;
		}

		//Because both panels are going to tween at the same time we need them both shown
		CommonGameObject.setLayerRecursively(normalSpinPanel, Layers.ID_NGUI);
		normalAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(normalSpinPanel);
		CommonGameObject.disableUIAnchorsForGameObject(normalSpinPanel);

		CommonGameObject.setLayerRecursively(bonusSpinPanel, Layers.ID_NGUI);
		bonusAnchorEnabledMap = CommonGameObject.getUIAnchorEnabledMapForGameObject(bonusSpinPanel);
		CommonGameObject.disableUIAnchorsForGameObject(bonusSpinPanel);

		if (swapTo == Type.NORMAL)
		{
			setSpinPanelPosition(swapTo, oppositeDirection, isWingsDistance);
			bonusSpinPanelPosition = getSlidePosition(swapTo, direction, isWingsDistance);
		}
		else if (swapTo == Type.FREE_SPINS)
		{
			setSpinPanelPosition(swapTo, oppositeDirection, isWingsDistance);
			normalSpinPanelPosition = getSlidePosition(swapTo, direction, isWingsDistance);
		}

		//Move the panels
		isSlidingNormalSpinPanel = true;
		isSlidingBonusSpinPanel = true;
		iTween.MoveTo(normalSpinPanel, iTween.Hash("position", normalSpinPanelPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear, "oncomplete", "onNormalSpinPanelSlideComplete", "oncompletetarget", this.gameObject));
		iTween.MoveTo(bonusSpinPanel, iTween.Hash("position", bonusSpinPanelPosition, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear, "oncomplete", "onBonusSpinPanelSlideComplete", "oncompletetarget", this.gameObject));

		// wait for the panels to finish sliding
		while (isSlidingNormalSpinPanel || isSlidingBonusSpinPanel)
		{
			yield return null;
		}

		// force the spin panels to be at the exact locations they should be as a safety precaution
		normalSpinPanel.transform.localPosition = normalSpinPanelPosition;
		bonusSpinPanel.transform.localPosition = bonusSpinPanelPosition;

		//Hide the unneeded panel
		showPanel(swapTo);
		
		// Restore the position of the spin panel that we slide off
		// otherwise it will be in the wrong position if someone tries
		// to turn it on without restoring it themselves
		if (swapTo == Type.NORMAL)
		{
			restoreSpinPanelPosition(Type.FREE_SPINS);
		}
		else if (swapTo == Type.FREE_SPINS)
		{
			restoreSpinPanelPosition(Type.NORMAL);
		}
	}

	// Callback function used for iTween call to slide the spin panel
	private void onNormalSpinPanelSlideComplete()
	{
		isSlidingNormalSpinPanel = false;
	}

	private void onBonusSpinPanelSlideComplete()
	{
		isSlidingBonusSpinPanel = false;
	}

	/// Use this to put the top bar back in the right place after it moves
	public void restoreSpinPanelPosition(Type type)
	{
		if (type == Type.NORMAL)
		{
			normalSpinPanel.transform.localPosition = Vector3.zero;
			CommonGameObject.restoreUIAnchorActiveMapToGameObject(normalSpinPanel, normalAnchorEnabledMap);
			normalAnchorEnabledMap = null;
		}
		else if (type == Type.FREE_SPINS)
		{
			bonusSpinPanel.transform.localPosition = Vector3.zero;
			CommonGameObject.restoreUIAnchorActiveMapToGameObject(bonusSpinPanel, bonusAnchorEnabledMap);
			bonusAnchorEnabledMap = null;
		}
	}
	
	// Sets the label to show how many autospins remain, and reset the UI if no more remain.
	public void setAutoCount(int count)
	{
		if (effectiveAutoSpinCountLabel != null)
		{
			string autoSpinOn = Localize.textUpper("auto_spin_on");
			if (count < 0)
			{
				effectiveAutoSpinCountLabel.text = Localize.textUpper("auto_spin_on");
			}
			else if (count != 0 || autoSpinOn != effectiveAutoSpinCountLabel.text)  //Don't change in the case where infinite auto spins have been manually stopped
			{
				effectiveAutoSpinCountLabel.text = Localize.textUpper("auto_spin_#", count);
			}
			
		}

		if (count == 0)
		{
			resetAutoSpinUI();
		}

		if (BonusSpinPanel.instance != null)
		{
			BonusSpinPanel.instance.spinCountLabel.text = count.ToString();
		}
	}
	
	// Returns the size of the area between the overlay and the spin panel
	// as a normalized value, where 1.0 represents the full height of the screen.
	public static float getNormalizedReelsAreaHeight(bool isFreeSpinGame)
	{
		// Get the y position of the top edge of the spin panel (the bottom of the reels area).
		float bottomEdge = instance.topEdge.localPosition.y + instance.topEdge.localScale.y;
		Vector2 overlayPos = Overlay.instance.top.getOverlayReelSize();
		overlayPos = Overlay.instance.transform.TransformPoint(overlayPos);
		overlayPos = instance.transform.InverseTransformPoint(overlayPos);
		float topEdge = overlayPos.x - overlayPos.y;

		if (isFreeSpinGame)
		{
			// The overlay is hidden.
			topEdge = NGUIExt.effectiveScreenHeight * 0.5f;
		}
		float areaHeight = topEdge - bottomEdge;
		return areaHeight / NGUIExt.effectiveScreenHeight;
	}

	// Returns the vertical center position of the area between the overlay and the spin panel
	// as a normalized value, where 0.0 is the center of the screen, -0.5 is bottom, 0.5 is top.
	public static float getNormalizedReelsAreaCenter(bool isFreeSpinGame)
	{
		float halfScreenHeight = NGUIExt.effectiveScreenHeight * 0.5f;
		float bottomEdge = instance.topEdge.localPosition.y + instance.topEdge.localScale.y/2f;
		Vector2 overlayPos = Overlay.instance.top.getOverlayReelSize();
		overlayPos = Overlay.instance.transform.TransformPoint(overlayPos);
		overlayPos = instance.transform.InverseTransformPoint(overlayPos);
		float topEdge = overlayPos.x - overlayPos.y/2f;

		if (isFreeSpinGame)
		{
			// The overlay is hidden.
			topEdge = halfScreenHeight;
		}
		float topSize = halfScreenHeight - Mathf.Abs(topEdge);
		float bottomSize = halfScreenHeight - Mathf.Abs(bottomEdge);
		
		// Return half the normalized difference between the top and bottom sizes.
		return (bottomSize - topSize) / NGUIExt.effectiveScreenHeight * 0.5f;
	}

	public IEnumerator fadeOut(float fadeDur)
	{
		spinPanelAlphaRestoreData = CommonGameObject.getAlphaRestoreDataForGameObject(gameObject);

		isFaded = true;

		// Fade out the rest of the game objects.

		float elapsedTime = 0;

		while (elapsedTime < fadeDur)
		{
			elapsedTime += Time.deltaTime;
			setAlphaOnSpinPanelGameObjects(1 - (elapsedTime / fadeDur));
			yield return null;
		}

		setAlphaOnSpinPanelGameObjects(0f);
	}

	public void fadeOutNow()
	{
		spinPanelAlphaRestoreData = CommonGameObject.getAlphaRestoreDataForGameObject(gameObject);
		isFaded = true;
		setAlphaOnSpinPanelGameObjects(0f);
	}

	private void setAlphaOnSpinPanelGameObjects(float alpha)
	{
		CommonGameObject.alphaGameObject(gameObject, alpha);
		NGUIExt.fadeGameObject(gameObject, alpha);
		TMProFunctions.fadeGameObject(gameObject, alpha);
	}

	public IEnumerator fadeIn(float fadeDur)
	{
		isFaded = false;
		yield return StartCoroutine(CommonGameObject.fadeGameObjectToOriginalAlpha(gameObject, spinPanelAlphaRestoreData, fadeDur));
	}

	public void setSpinPanelBounds()
	{
		if (hir.reelBoundsLimit != null)
		{
			hir.reelBoundsLimit.enabled = true;
			reelBoundsMax = hir.reelBoundsLimit.bounds.max;
			hir.reelBoundsLimit.enabled = false;
		}
	}

	public void restoreAlpha()
	{
		isFaded = false;

		if (spinPanelAlphaRestoreData != null)
		{
			CommonGameObject.fadeGameObjectToOriginalAlpha(gameObject, spinPanelAlphaRestoreData);
		}
	}

	public void setNormalPanelToSmallVersion()
	{
		backgroundWingsWidth.gameObject.SetActive(false);
		freeSpinsBackgroundWingsWidth.gameObject.SetActive(false);

		if (Overlay.instance != null)
		{
			Overlay.instance.setBackingSpriteVisible(false);
		}

		if (!hasOffsetNormalSpinPanel)
		{
			normalSpinPanelSwapper.setState("short");
			hasOffsetNormalSpinPanel = true;
		}
		
		reelBoundsMax = reelBoundsLimit.bounds.max;
		
		InGameFeatureContainer.onSpinPanelResize(true);
	}

	public void setBonusPanelToSmallVersion()
	{
		freeSpinsBackgroundWingsWidth.gameObject.SetActive(false);

		if (Overlay.instance != null)
		{
			Overlay.instance.setBackingSpriteVisible(backgroundWingsWidth.gameObject.activeSelf);
		}

		if (!hasOffsetBonusSpinPanel)
		{
			bonusSpinPanelSwapper.setState("short");
			hasOffsetBonusSpinPanel = true;
		}
		
		reelBoundsMax = reelBoundsLimit.bounds.max;
	}

	//Finds the wager index of a given wager or the index of the nearest wager amount without going above the given wager
	public int getNearestWagerIndex(long wager)
	{
		for (int i = 0; i < wagerList.Count; i++)
		{
			if (wagerList[i] > wager)
			{
				return i > 0 ? i-1 : 0; //If we've gone above the given wager then return the previous wager or the lowest wager possible one if we're on the first index
			}
			else if (wagerList[i] == wager || i == wagerList.Count-1)
			{
				return i; //Return the index of the wager if it exists in the list or return the final index if the given wager was larger than all the wagers in the current game's wager list
			}
		}
		return 0;
	}

	public void activateFeatureButton(UIImageButton button)
	{
		if (featureButtonSprite != null && button != null)
		{
			button.target = featureButtonSprite;
			string spriteName = featureButtonSprite.spriteName;
			button.normalSprite = spriteName;
			button.hoverSprite = spriteName;
			string pressed = spriteName;
			//The 9-sliced sprites are automatically appended with the "Stretchy" word by the sprite tool
			//See NineSlicer.cs
			if (spriteName.EndsWith("Stretchy"))
			{
				pressed = spriteName.Insert(spriteName.Length - 8, "Pressed ");
			}
			else //else just append the Pressed word at the end since that is the naming convention used by TA
			{
				pressed = spriteName.Insert(spriteName.Length, " Pressed");
			}
			button.pressedSprite = pressed;
			button.disabledSprite = pressed;
		}
	}

	public void hideAutoSpinPanel(bool forceOff = false)
	{
		if (autoSpinPanelAnimator == null)
		{
			return;
		}

		if (isAutoSpinSelectorActive)
		{
			bool canSpin = (SlotBaseGame.instance != null && SpinPanel.instance != null && SpinPanel.instance.isButtonsEnabled); // Base Game
			SwipeableReel.canSwipeToSpin = canSpin;

			isAutoSpinSelectorActive = false;
			isAutoSpinCountPanelActive = slot.hasAutoSpinsRemaining && !forceOff;
			if (isAutoSpinCountPanelActive)
			{
				autoSpinPanelAnimator.Play("selected");
				Audio.play("autospin_select");
			}
			else
			{
				autoSpinPanelAnimator.Play("off");
			}
		}
		else if (isAutoSpinCountPanelActive && (!slot.hasAutoSpinsRemaining || forceOff))
		{
			isAutoSpinCountPanelActive = false;
			autoSpinPanelAnimator.Play("selected off");
		}
		else if (!isAutoSpinCountPanelActive && slot.hasAutoSpinsRemaining && !forceOff)
		{
			isAutoSpinCountPanelActive = true;
			autoSpinPanelAnimator.Play("on hold");
		}
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		backupWagerAmount = 0;
	}

#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public IEnumerator automateChangeWagerForZap()
	{
		int prevWagerIndex = currentWagerIndex;
		int randomWagerIndex = Random.Range(0, wagerList.Count);

		if (prevWagerIndex != randomWagerIndex)
		{
			string buttonPressAction = "";

			if (randomWagerIndex < prevWagerIndex)
			{
				buttonPressAction = "SpinPanelBetDown";
			}
			else
			{
				buttonPressAction = "SpinPanelBetUp";
			}

			// Adjust the bet until we reach the target value
			while (currentWagerIndex != randomWagerIndex)
			{
				// Make sure we skip pressing the button if a dialog becomes active so that we don't
				// simulate multiple key pressed
				if (CommonAutomation.IsDialogActive())
				{
					yield return null;
				}
				else
				{
					yield return RoutineRunner.instance.StartCoroutine(Zap.Automation.ActionController.doAction(buttonPressAction));
				}
			}
		}
	}
	
	// Used by AutoSpinTest to perform an autospin action of a given amount. 
	public void automateAutoSpinForZap(int autoSpinCount)
	{
		clickAutoSpinAmount(autoSpinCount);
	}
#endif

#if ZYNGA_TRAMP
	/// Used by AutomatedPlayer to simulate the changing of the bet amount (to a random value) before each automated spin
	public IEnumerator automateChangeWager()
	{
		int prevWagerIndex = currentWagerIndex;
		int randomWagerIndex = Random.Range(0, wagerList.Count);

		if (prevWagerIndex == randomWagerIndex)
		{
			// already at value
			yield break;
		}
		else
		{
			GameObject targetButton = null;

			if (randomWagerIndex < prevWagerIndex)
			{
				targetButton = betDownButtonHandler.gameObject;
			}
			else
			{
				targetButton = betUpButtonHandler.gameObject;
			}

			while (currentWagerIndex != randomWagerIndex)
			{
				yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.automateOpenDialog());
				yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(targetButton));
			}
		}
	}

	// Used by AutomatedPlayer to perform an autospin action of a given amount. 
	public IEnumerator automateAutoSpin(int autoSpinCount)
	{
		Collider spinButton = CommonAutomation.getSpinButton();
		if (spinButton != null && spinButton.gameObject != null)
		{
			yield return StartCoroutine(CommonAutomation.holdRandomColliderIn(spinButton.gameObject, 10.0f)); //TODO: Get this value from the button.
		}
		else
		{
			throw new System.Exception("When spinning the BaseGame, somehow the spinButton's gameobject is null!");
		}

		yield return null;
	}

	// Used by AutomatedPlayer to perform a slam stop action by clicking on a random reel.
	public IEnumerator automateSlamStop(float delay = 0.0f)
	{
		if (ReelGame.activeGame is TumbleSlotBaseGame || ReelGame.activeGame is NewTumbleSlotBaseGame)
		{
			// don't bother with tumble games since they don't have swipeable reels
			yield break;
		}

		SwipeableReel[] swipeableReels = FindObjectsOfType<SwipeableReel>();
	
		if (swipeableReels.Length > 0)
		{
			int randomIdx = Random.Range(0, swipeableReels.Length);
			SwipeableReel selectedReel = swipeableReels[randomIdx];

			if (delay > 0)
			{
				yield return new WaitForSeconds(delay);
			}
			yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(Input.simulateMouseClickOn(selectedReel, cameraForButton: selectedReel.cameraOnReel));
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Trying to slam stop but there are no SwipeableReels to select!</color>",
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}
		yield return null;
	}

	// Used by AutomatedPlayer to stop a spin early or to terminate autospins when a game test is forced to be done.
	public IEnumerator automateStop()
	{
		Collider stopButtonCollider = stopButton.GetComponent<Collider>();
		
		if (stopButtonCollider != null)
		{
			yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(Input.simulateMouseClickOn(stopButtonCollider, 0));
		}
		else
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Trying to stop the base game but cannot find the stop button.</color>",
					AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}
		yield return null;
	}
#endif // ZYNGA_TRAMP
}
