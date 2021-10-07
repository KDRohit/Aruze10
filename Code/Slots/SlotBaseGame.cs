using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;
using Zynga.Unity.Attributes;
using System.Collections.ObjectModel;
using Com.HitItRich.Feature.OOCRebound;
using Zynga.Zdk;
using Facebook.Unity;
using TMPro;

#if UNITY_WSA_10_0 && NETFX_CORE
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
#endif


/**
SlotBaseGame
Handles gameplay logic for a "base game" - a slot game that is available directly off the lobby.  This script should be placed
in the slot prefab in the editor.

Prior to the player playing the game:
1. A SlotEngine instance is added to handle the actual running of the reels.
2. An OutcomeDisplayController instance is added to process the outcome from the server (held in a SlotOutcome instance) and run the display of user wins.
3. SlotAction.startGame is sent. Because of the reels "tiers" mechanism, we have to wait for the server to respond with a
	"slots_game_started" event -> slotStartedEventCallback, which tells us which reelset to load.

The play loop is generally as follows:
1. User triggers a spin.  This can be from the spin button or as part of the auto-spin counter.
  * SlotEngine starts spinning the reels visually.
  * SlotAction.spin is sent.
2. The player can hit the "STOP" button, which can occur before or after the server outcome arrives.
3. A "slot_outcome" event calls slotOutcomeEventCallback.  The accompanying JSON object gets stored in a SlotOutcome, and the SlotEngine is told to stop the reels at
   The specified reel positions.
4. When the reels come to a complete stop, reelsStoppedCallback gets called and the OutcomeDisplayController takes over to show any resulting wins (if player won anything).
5. If the player is using autospins, a callback is set in the OutcomeDisplayController to automatically start the next spin.
*/
public class SlotBaseGame : ReelGame
{
	public static SlotBaseGame instance = null;
		
	protected TextAsset debugReelMessageTextFile;

	/// Internal class used for inspector values
	[System.Serializable]
	public class BannerTextInfo 
	{
		public string localizationKey;	// Key used for look up in SCAT.
		public UILabel label; 			// To be removed when prefabs are updated.
		public LabelWrapperComponent labelWrapperComponent;	// The label that contains the properties that we want to use i.e. gradient, max width / height, color, font type.

		public Vector3 bannerTextAdjustment;	// How much to move each UILabel. Used to fiddle with the positioning of the UILabels.
		public TextDirectionEnum textDirection;	// The direction that you want the text to be rendered
		public TextLocationEnum textLocation;	// Which possition in the banner overlay you want to render the text.
		public bool insertNewLines = false;		// Will tag it so that a new line character is inserted at the end of each word. 

		public LabelWrapper labelWrapper
		{
			get
			{
				if (_labelWrapper == null)
				{
					if (labelWrapperComponent != null)
					{
						_labelWrapper = labelWrapperComponent.labelWrapper;
					}
					else
					{
						_labelWrapper = new LabelWrapper(label);
					}
				}
				return _labelWrapper;
			}
		}
		private LabelWrapper _labelWrapper = null;

		public enum TextDirectionEnum
		{
			HORIZONTAL,
			VERTICAL,
		}
		public enum TextLocationEnum
		{
			HEADER,
			CENTER,
			FOOTER
		}
		public string localizedText
		{
			get
			{
				if (!stringLocalized)
				{
					if (localizationKey.Trim() == "")
					{
						_localizedText = "";
					}
					else
					{
						_localizedText = Localize.textUpper(localizationKey);
					}
					stringLocalized = true;
				}
				return _localizedText;
			}
			set
			{
				localizedText = value;
				stringLocalized = false;
			}
		}
		private string _localizedText;
		private bool stringLocalized = false;
	}

	/// Internal class used for inspector values
	[System.Serializable] public class BannerInfo
	{
		public string name;
		public BannerTypeEnum bannerType; // < One of 4 types of banners used by Portal Scripts evaluation
		public Vector3 bannerPosAdjustment = Vector3.zero;
		public Vector3 bannerScaleAdjustment = Vector3.one; // Note: Using to set directly, not a delta
		public GameObject template; // < The template of the banner that you want to use
		public GameObject revealVfx;
		public GameObject pickMePrefab;
		public enum BannerTypeEnum
		{
			CLICKME,
			CHALLENGE,
			GIFTING,
			OTHER,
			CREDITS,
			CLICKME2, // Use a different prefab for the second banner
			CLICKME3 // Use a different prefab for the third banner
		}

		public BannerTextInfo[] textInfo;
		
	}
	
	protected GameObject bigWin;
	protected GameObject superWin;
	protected GameObject megaWin;

	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public string editorGameKey = "";		// Use this to fill in the game key of the game for use by editor scripts where the game key isn't loaded elsewhere
	
	protected bool isExecutingBonusAfterPayout = false; // Tells if we need to try and launch a bonus that was delayed until after the base payout due to SlotModules controlling isPayingBasegameWinsBeforeBonusGames()
	protected bool isNonBonusWinningsAlreadyPaid = false; // Tracks when non bonus wins have already been paid out, so that we don't try and pay them again when coming back from a bonus.  Only used in conjunction with isPayingBasegameWinsBeforeBonusGames()

	// Portal related scripts
	public GameObject[] bannerRoots;
	public BannerInfo[] banners;
	public GameObject bannerTextOverlay;
	public PortalScript portal;

	// when displaying an outcome, should we skip the payboxes (we want them on (set this false) for new games with big symbols & animations)
	public bool shouldSkipPayboxDisplay = true; 
	
	protected BigWinEffect bigWinEffect;
	protected GameObject bigWinGameObject;
	public float timeOutTimer;

	[HideInInspector] public int waysToWin = 0;
	[HideInInspector] public int winLines = 0;
	[HideInInspector] public bool currentlyTumbling = false;	// Needed for tumble outcomes so info isn't reset on every setOutcome
	[HideInInspector] public int lastSpinXPMultiplier = 0;	// Reset to 0 after processing the spin, so we know it's not a bet-related XP increase if an event comes in for some other reason.

	[System.NonSerialized] public bool isVipRevampGame = false;
	[System.NonSerialized] public bool isMaxVoltageGame = false;
	[System.NonSerialized] public bool isRoyalRushGame = false;
	[System.NonSerialized] string statFamily = "";	// used for tracking ooc purchase stats
	
	public static string lastBigWinHash = "";
	public static event GenericDelegate onSpinPressed;	// Reset to 0 after processing the spin, so we know it's not a bet-related XP increase if an event comes in for some other reason.

	private bool isCheckingSpinTimeout = false;
	protected bool isSpinTimedOut = false;
	private float spinTimeoutTimer = 0;
	private bool needstoWaitForJackpotsToLoad = false;
	private bool needsToWaitForCollectionOverlayToLoad = false; //Used when we're in a game with a special collection UI that hasn't been loaded yet, so we don't hide the loading screen too early
	private BuiltInProgressiveJackpotBaseGameModule builtInProgressiveModule = null;
	private PlayAnimationListOnSlotGameStartModule animOnStartModule = null;
	[System.NonSerialized] public long spinsStartingCredits = 0; // Number of credits player had at the start of a spin.
	
	private Dictionary<string, ForcedOutcome> freeSpinForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> challengeForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> otherBonusForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> scatterBonusForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> secondaryScatterBonusForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> mutationForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> secondaryMutationForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> bigWinForcedOutcomes = null;
	private Dictionary<string, ForcedOutcome> feature0ForcedOutcomes = null; // could be anything unique to a game, keyed by '0' key.
	private Dictionary<string, ForcedOutcome> feature1ForcedOutcomes = null; // could be anything unique to a game, keyed by '1' key.
	private Dictionary<string, ForcedOutcome> feature2ForcedOutcomes = null; // could be anything unique to a game, keyed by '2' key.
	private Dictionary<string, ForcedOutcome> feature3ForcedOutcomes = null; // could be anything unique to a game, keyed by '3' key.
	private Dictionary<string, ForcedOutcome> feature4ForcedOutcomes = null; // could be anything unique to a game, keyed by '4' key.
	private Dictionary<string, ForcedOutcome> feature5ForcedOutcomes = null; // could be anything unique to a game, keyed by '5' key.
	private Dictionary<string, ForcedOutcome> feature6ForcedOutcomes = null; // could be anything unique to a game, keyed by '6' key.
	private Dictionary<string, ForcedOutcome> feature7ForcedOutcomes = null; // could be anything unique to a game, keyed by '7' key.
	private Dictionary<string, ForcedOutcome> feature8ForcedOutcomes = null; // could be anything unique to a game, keyed by '8' key.
	private Dictionary<string, ForcedOutcome> feature9ForcedOutcomes = null; // could be anything unique to a game, keyed by '9' key.
	private Dictionary<string, ForcedOutcome> featureQForcedOutcomes = null; // could be anything unique to a game, keyed by 'q' key.
	private Dictionary<string, ForcedOutcome> featureRForcedOutcomes = null; // could be anything unique to a game, keyed by 'r' key.
	private Dictionary<string, ForcedOutcome> featureEForcedOutcomes = null; // could be anything unique to a game, keyed by 'e' key.
	private Dictionary<string, ForcedOutcome> featureTForcedOutcomes = null; // could be anything unique to a game, keyed by 't' key.
	private Dictionary<string, ForcedOutcome> featureSForcedOutcomes = null; // could be anything unique to a game, keyed by 's' key.
	private Dictionary<string, ForcedOutcome> ftueBigWinForcedOutcomes = null; // Mapped to the 'n' key.  Forces the ftue (First Time User Experience) big win outcome to trigger.
	private Dictionary<string, ForcedOutcome> queuedForcedOutcome = null; // Buffer a forced outcome during autospins

	private JSON petRespinData;
	private bool didPetRespin;

	public enum ForcedOutcomeTypeEnum
	{
		FREESPIN_FORCED_OUTCOME = 0,
		CHALLENGE_FORCED_OUTCOME,
		OTHER_BONUS_FORCED_OUTCOME,
		SCATTER_BONUS_FORCED_OUTCOME,
		SECONDARY_SCATTER_BONUS_FORCED_OUTCOME,
		MUTATION_FORCED_OUTCOME,
		SECONDARY_MUTATION_FORCED_OUTCOME,
		BIG_WIN_FORCED_OUTCOME,
		FEATURE_0_FORCED_OUTCOME,
		FEATURE_1_FORCED_OUTCOME,
		FEATURE_2_FORCED_OUTCOME,
		FEATURE_3_FORCED_OUTCOME,
		FEATURE_4_FORCED_OUTCOME,
		FEATURE_5_FORCED_OUTCOME,
		FEATURE_6_FORCED_OUTCOME,
		FEATURE_7_FORCED_OUTCOME,
		FEATURE_8_FORCED_OUTCOME,
		FEATURE_9_FORCED_OUTCOME,
		FEATURE_Q_FORCED_OUTCOME,
		FEATURE_R_FORCED_OUTCOME,
		FEATURE_E_FORCED_OUTCOME,
		FEATURE_T_FORCED_OUTCOME,
		FEATURE_S_FORCED_OUTCOME,
		FTUE_BIG_WIN_FORCED_OUTCOME,
		UNDEFINED
	}

	protected List<SlotOutcome> layeredBonusOutcomes = new List<SlotOutcome>();

	protected bool isBaseBonusOutcomeProcessed = false;
	public bool isBonusOutcomePlayed = false;	// Tracks if a bonus outcome has already been played for this spin, needed for the Evlira type games because they need to play their in a different spot
	public SlotOutcome bonusGameOutcomeOverride = null;
	private long petBonus = 0;
	
	/// Sets or gets whether the game is busy, allowing or disallowing the Scheduler to do something.
	public virtual bool isGameBusy
	{
		get { return isPerformingSpin; }
	}

	public virtual bool isBigWinBlocking
	{
		get
		{
			if (bigWinEffect != null)
			{
				if (bigWinEffect.isAnimating == true)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			return false;
		}
	}

	//Condiitionals for features that might block spins
	public virtual bool isFeatureBlockingSpins
	{
		get { return isRoyalRushGame && RoyalRushEvent.waitingForSprintSummary || Scheduler.hasBlockingTask; }
	}
	//
	// Check if a module is making the game busy while it isn't spinning
	public bool isModuleBlockingWebGLKeyboardInputForSlotGame()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			if (cachedAttachedSlotModules[i].isBlockingWebGLKeyboardInputForSlotGame())
			{
				return true;
			}
		}

		return false;
	}

	public bool areModulesDelayingBigWin
	{
		get {
			// Check if the big win is being handled and triggered by a module
			foreach (SlotModule module in cachedAttachedSlotModules)
			{
				if (module.isModuleHandlingBigWin())
				{
					// a module is saying to keep delaying the big win
					return true;
				}
			}

			return false;
		}
	}

	private bool isInTheMiddleOfAnAutoSpinSpin = false;
	protected bool isGoingIdle = false; // true if the background music is muted or in the process of getting muted.
	private List<GameObject> hiddenChildren;
	
	[System.NonSerialized] public bool needsToShowBigWinEndDialogs = false; // tells if the game hasn't shown the big win end dialogs yet and still needs a module to do it

	protected Dictionary<string, GameObject> cached3dPaytableSymbols = new Dictionary<string, GameObject>(); // Loading symbols in is causing major slowdown on device, going to cache them so that switching pages is faster

	[HideInInspector] public bool isExecutingGameStartModules = true;

	private SlotOutcome outcomeBeforeFreespinsInBase = null; // store the outcome that triggered free spins in base so that it can be restored afterwards
	
	protected override void Awake()
	{
		base.Awake();
		instance = this;

		// reset first spin here, seems Chris wants this sound to happen once per first spin of each game
		Audio.instance.firstSpin = true;

		mutationManager = new MutationManager(false);	
				
		setEngine();
		engine.setReelsStoppedCallback(reelsStoppedCallback);

		if (isLegacyTumbleGame || isLegacyPlopGame)
		{
			_outcomeDisplayController = gameObject.AddComponent<DeprecatedPlopAndTumbleOutcomeDisplayController>() as DeprecatedPlopAndTumbleOutcomeDisplayController;
		}
		else
		{
			_outcomeDisplayController = gameObject.AddComponent<OutcomeDisplayController>() as OutcomeDisplayController;
		}

		_outcomeDisplayController.init(engine);
		_outcomeDisplayController.setSpinBlockReleaseCallback(onOutcomeSpinBlockRelease);

		// onEndRollup is called when the rollup ends
		// onPayoutRollup is called every frame with the updated value
		_outcomeDisplayController.setPayoutRollupCallback(onPayoutRollup, onEndRollup);

		// onBigWinNotification is called when OutcomeDisplayController sees a big win
		_outcomeDisplayController.setBigWinNotificationCallback(onBigWinNotification);
		SlotResourceMap.getBigWin(GameState.game.keyName,
								(string asset, UnityEngine.Object obj, Dict data) => {
									  bigWin = obj as GameObject;
								},
								(string asset, Dict data) => {
									Debug.LogError("SlotResourceMap.getBigWin() - Failed to load asset = " + asset);
								});

		SlotResourceMap.getSuperWin(GameState.game.keyName,
								(string asset, UnityEngine.Object obj, Dict data) => {
									superWin = obj as GameObject;
								},
								(string asset, Dict data) => {
									Debug.LogError("SlotResourceMap.getSuperWin() - Failed to load asset = " + asset);
								});

		SlotResourceMap.getMegaWin(GameState.game.keyName,
								(string asset, UnityEngine.Object obj, Dict data) => {
									megaWin = obj as GameObject;
								},  
								(string asset, Dict data) => {
									Debug.LogError("SlotResourceMap.getMegaWin() - Failed to load asset = " + asset);
								});
		
		//If we don't have a freespins prefab play freespins in basegame
		if (!SlotResourceMap.gameHasFreespinsPrefab(GameState.game.keyName))
		{
			playFreespinsInBasegame = true;
		}
		
		autoSpins = 0;
		
		SlotAction.startGame(GameState.game.keyName, slotStartedEventCallback);
		
		playBgMusic();
	}

	/// Register a forced outcome via a ForcedOutcomeModule using the SerializedForcedOutcomeData
	/// This version assumes gameKey is the current gameKey and will be the one called by the registration module
	public void registerForcedOutcome(SerializedForcedOutcomeData forcedOutcomeData)
	{
		registerForcedOutcome(GameState.game.keyName, forcedOutcomeData);
	}

	/// Register a forced outcome via a ForcedOutcomeModule using the SerializedForcedOutcomeData
	public void registerForcedOutcome(string gameKey, SerializedForcedOutcomeData forcedOutcomeData)
	{
		if (GameState.game != null)
		{
			if (GameState.game.keyName != gameKey)
			{
				// may as well not register forced outcomes for other games, since we rebuild this for each game
				return;
			}
		}
		else if (editorGameKey != "")
		{
			if (editorGameKey != gameKey)
			{
				// may as well not register forced outcomes for other games, since we rebuild this for each game
				return;
			}
		}
		else
		{
			// abort, we don't know what game this is
			Debug.LogError("SlotBaseGame::registerForcedOutcome() - Don't know what game this is, so can't register forced outcomes!  Try fillin in editorGameKey.");
			return;
		}

		switch (forcedOutcomeData.forcedOutcomeType)
		{
			case ForcedOutcomeTypeEnum.FREESPIN_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref freeSpinForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.CHALLENGE_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref challengeForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.OTHER_BONUS_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref otherBonusForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.SCATTER_BONUS_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref scatterBonusForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.SECONDARY_SCATTER_BONUS_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref secondaryScatterBonusForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.MUTATION_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref mutationForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.SECONDARY_MUTATION_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref secondaryMutationForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.BIG_WIN_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref bigWinForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_0_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature0ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_1_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature1ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_2_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature2ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_3_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature3ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_4_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature4ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_5_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature5ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_6_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature6ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_7_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature7ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_8_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature8ForcedOutcomes, forcedOutcomeData);
				break;

			case ForcedOutcomeTypeEnum.FEATURE_9_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref feature9ForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FEATURE_Q_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref featureQForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FEATURE_R_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref featureRForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FEATURE_E_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref featureEForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FEATURE_T_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref featureTForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FEATURE_S_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref featureSForcedOutcomes, forcedOutcomeData);
				break;
			
			case ForcedOutcomeTypeEnum.FTUE_BIG_WIN_FORCED_OUTCOME:
				registerForcedOutcome(gameKey, ref ftueBigWinForcedOutcomes, forcedOutcomeData);
				break;

			default:
				Debug.LogError("Unknown type: " + forcedOutcomeData.forcedOutcomeType + ". Forced outcome NOT added.");
				return;
		}
	}

	// Function to get the correct ForcedOutcomeRegistrationModule for a game that might have more than one and be using a game key to identify them
	private ForcedOutcomeRegistrationModule getForcedOutcomeRegistrationModuleForGameKey(string gameKey)
	{
		List<ForcedOutcomeRegistrationModule> registrationModules = new List<ForcedOutcomeRegistrationModule>();

		// First find all of the cached forced outcome modules, which there might be more than one
		// if this is a game that uses more than one game key for different versions.
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module != null && module is ForcedOutcomeRegistrationModule)
			{
				registrationModules.Add(module as ForcedOutcomeRegistrationModule);
			}
		}

		if (registrationModules.Count == 0)
		{
			// no forced outcome modules attached
			return null;
		}
		else if (registrationModules.Count == 1)
		{
			// only one forced outcome module attached, return it
			return registrationModules[0];
		}
		else
		{
			// more than one forced outcome module detected, try and match the gameKey to the one on the modules
			for (int i = 0; i < registrationModules.Count; i++)
			{
				if (registrationModules[i].targetGameKey == gameKey)
				{
					return registrationModules[i];
				}
			}

			// if we found no match, warn and return null
			Debug.LogWarning("SlotBaseGame.getForcedOutcomeRegistrationModuleForGameKey() - gameKey = " + gameKey + "; SlotBaseGame contained multiple ForcedOutcomeRegistrationModules, but none of them matched the gameKey!");
			return null;
		}
	}

	/// Handles adding to the dictionary once we know which one the forced outcome is for
	private void registerForcedOutcome(string gameKey, ref Dictionary<string, ForcedOutcome> targetDictionary, SerializedForcedOutcomeData forcedOutcomeData)
	{
		if (targetDictionary == null)
		{
			targetDictionary = new Dictionary<string, ForcedOutcome>();
		}

		// ensure the outcome is fully built, which it will need additional processing if it was specified using symbol names
		if (forcedOutcomeData.forcedOutcome.isUsingSymbolNames)
		{
			forcedOutcomeData.forcedOutcome.buildForcedOutcomeUsingSymbolNames();
		}

		// Check if we should build a registation module at run time because this game doesn't have one yet
		ForcedOutcomeRegistrationModule registrationModule = getForcedOutcomeRegistrationModuleForGameKey(gameKey);
		if (registrationModule == null)
		{
			// attach a registration module
			registrationModule = gameObject.AddComponent(typeof(ForcedOutcomeRegistrationModule)) as ForcedOutcomeRegistrationModule;
			// set the targetGameKey on it so that we can find it if there happens to be more 
			// than one attached due to a mistake (the mistake will warn you, see getForcedOutcomeRegistrationModuleForGameKey())
			registrationModule.targetGameKey = gameKey;
		}

		// now check if the registration module has this outcome data yet, and if not, add it
		if (!registrationModule.forcedOutcomeList.Contains(forcedOutcomeData))
		{
			registrationModule.forcedOutcomeList.Add(forcedOutcomeData);

			// make sure the dictionaries are updated in editor mode
			forcedOutcomeData.forcedOutcome.convertForcedLayerOutcomeInfoDictionariesToSerializedLists();
		}

		if (!targetDictionary.ContainsKey(gameKey))
		{
			targetDictionary.Add(gameKey, forcedOutcomeData.forcedOutcome);
		}
		else
		{
			targetDictionary[gameKey] = forcedOutcomeData.forcedOutcome;
		}
	}

	// Sets the engine in the awake method, so that subclasses can use their own version of SlotEngine.
	protected virtual void setEngine()
	{
		engine = new SlotEngine(this);
	}

	// helper function for creating ForcedOutcomes without having to look up reel stops
	protected ForcedOutcome getForcedOutcomeBySymbolNames(string[] symbols, string reelSetName, int tierId)
	{
		ReelSetData reelSetData = engine.gameData.findReelSet(reelSetName);
		if (reelSetData == null)
		{
			return null;
		}

		int symbolIndex = 0;
		int[] reelStops = new int[symbols.Length];
		foreach (ReelData reelData in reelSetData.reelDataList)
		{
			for (int i = 0; i < reelData.reelStrip.symbols.Length; i++)
			{
				if (symbols[symbolIndex] == reelData.reelStrip.symbols[i])
				{
					reelStops[symbolIndex] = i;
					symbolIndex++;
					break;
				}
			}
		}

		return new ForcedOutcome(tierId, reelStops);
	}
	
	protected override void Update()
	{
		base.Update();

		// check for spin timeout
		if (isCheckingSpinTimeout && !isSpinTimedOut && Dialog.instance != null)
		{
			spinTimeoutTimer += Time.unscaledDeltaTime;
			if (spinTimeoutTimer >= Glb.LIVE_SPIN_SAFETY_TIMEOUT)
			{
				isCheckingSpinTimeout = false;
				isSpinTimedOut = true;

				// log an error log that should be added to the spin Userflow
				string errorMsg = "Exceeded timeout of: " + Glb.LIVE_SPIN_SAFETY_TIMEOUT;
				Glb.failSpinTransaction(errorMsg, "spin-timeout");
				string userMsg = "";
				if (Data.debugMode)
				{
					userMsg = "Spin timed out after " + spinTimeoutTimer + " seconds.";
				}
				Server.forceGameRefresh(
					"Spin request timed out.", 
					userMsg,
					reportError: false,
					doLocalization: false);
			}
		}

		if (engine != null)
		{
			engine.frameUpdate();
			
			if (Application.isEditor)
			{
				// If you add a keycode here, also remember to add the associated code in DevGUI.tabGames().
				gameKey("c", KeyCode.C);
				gameKey("g", KeyCode.G);
				gameKey("y", KeyCode.Y);
				gameKey("b", KeyCode.B);
				gameKey("m", KeyCode.M);
				gameKey("w", KeyCode.W);
				gameKey("u", KeyCode.U);
				gameKey("i", KeyCode.I);
				gameKey(";", KeyCode.Semicolon);
				gameKey("'", KeyCode.Quote);
				gameKey(",", KeyCode.Comma);
				gameKey(".", KeyCode.Period);

				gameKey("1", KeyCode.Alpha1);
				gameKey("2", KeyCode.Alpha2);
				gameKey("3", KeyCode.Alpha3);
				gameKey("4", KeyCode.Alpha4);
				gameKey("5", KeyCode.Alpha5);						
				gameKey("6", KeyCode.Alpha6);
				gameKey("7", KeyCode.Alpha7);
				gameKey("8", KeyCode.Alpha8);
				gameKey("9", KeyCode.Alpha9);
				gameKey("0", KeyCode.Alpha0);
				gameKey("q", KeyCode.Q);
				gameKey("r", KeyCode.R);
				gameKey("e", KeyCode.E);
				gameKey("t", KeyCode.T);
				gameKey("s", KeyCode.S);
				gameKey("n", KeyCode.N);
			}
			
			// Handle idling state. We only idle if in the base game.
			// Don't idle out the music if we are in a special state or the big win effect is going.
			if (!BonusGameManager.isBonusGameActive && !isSpecialWinActive && bigWinEffect == null)
			{
				if (isGameBusy || isModulePreventingBaseGameAudioFade())
				{
					idleTimer = Time.time;
				}
				// Are we in an idle state?
				if (Time.time - idleTimer > IDLE_TIME) 
				{
					isGoingIdle = true;
					// Fade the music's volume down to silence.
					if (AudioListener.volume > (0.01f * Audio.maxGlobalVolume))
					{
				
						// 1/300, to fade over 5 seconds.
						Audio.listenerVolume -= (0.003f * Audio.maxGlobalVolume);
					}
					else
					{
						// Set the volume to zero so we don't hear any thing noise when it should be muted.
						Audio.listenerVolume = 0;
					}
				}
				// If we're coming back from idle, unmute the music.
				else
				{
					restoreAudio(wakeBySpin);
					if (wakeBySpin)
					{
						wakeBySpin = false;
					}
				}
			}
		}

		if (isPerformingSpin && isSpinComplete)
		{
			isPerformingSpin = false;
			isSpinComplete = false;
			OOCReboundFeature.incrementSpinCount();
			if (petRespinData == null)
			{
				StartCoroutine(waitForSchedulerThenFinishSpin());
			}
			else
			{
				StartCoroutine(startPetSpinCoroutine(petRespinData));
				petRespinData = null;
				didPetRespin = true;
			}
		}
	}

	public void restoreAudio(bool restoreInstantly)
	{
		isGoingIdle = false;
		if (restoreInstantly)
		{
			Audio.listenerVolume = Audio.maxGlobalVolume;
		}
		else if (Audio.maxGlobalVolume > Audio.listenerVolume)
		{
			//to fade in over 1 second
			Audio.listenerVolume = Mathf.Clamp01(Audio.listenerVolume + (0.015f * Audio.maxGlobalVolume));
		}
	}
	
	// Check if a module wants to prevent the sound from fading out like it normally does after a set amount of time
	private bool isModulePreventingBaseGameAudioFade()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			if (cachedAttachedSlotModules[i].isModulePreventingBaseGameAudioFade())
			{
				return true;
			}
		}

		return false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		// Unregister any of the delegates that might be out for the game.
		Server.unregisterEventDelegate("slots_outcome", slotOutcomeEventCallback);
		Server.unregisterEventDelegate("slots_game_started");
		Server.unregisterEventDelegate("big_win");
	}

	private IEnumerator showPetsRespinOverlay()
	{
		VirtualPetRespinOverlayDialog.showDialog();
		
		//Wait for the pet to appear or for it to fail to load
		while (VirtualPetRespinOverlayDialog.instance == null)
		{
			yield return null;
			if (!Scheduler.hasTaskWith("virtual_pets_respin_dialog"))
			{
				break;
			}
		}
	}

	private IEnumerator showPetBonusIntroForGamesWithPersistentData(JSON data)
	{
		yield return StartCoroutine(showPetsRespinOverlay());

		if (VirtualPetRespinOverlayDialog.instance != null)
		{
			yield return StartCoroutine(VirtualPetRespinOverlayDialog.instance.playNoRespinAnimations());
			
		}
		
		sendOutcomeToEngine(data);
	}

	private IEnumerator startPetSpinCoroutine(JSON respinData)
	{
		isPerformingSpin = true;
		petBonus = respinData.getLong("pet_bonus", 0);
		yield return StartCoroutine(showPetsRespinOverlay());

		if (VirtualPetRespinOverlayDialog.instance != null)
		{
			yield return StartCoroutine(VirtualPetRespinOverlayDialog.instance.playRespinAnimations());
		}
		
		yield return StartCoroutine(startSpinCoroutine(requestServerOutcome:false)); //Start Spinning the reels
		yield return new TIWaitForSeconds(REEVALUATION_SPIN_STOP_TIME);
		
		slotOutcomeEventCallback(respinData); //Stop the reels
	}
	
	// Execute the ToDoList between BaseGame spins to allow features to trigger
	private IEnumerator waitForSchedulerThenFinishSpin()
	{
		if (VirtualPetRespinOverlayDialog.instance != null)
		{
			yield return StartCoroutine(VirtualPetRespinOverlayDialog.instance.awardCoins(petBonus * multiplier, didPetRespin));
			petBonus = 0;
		}

		// if we are doing freespins in base then we aren't going to handle the todo list until we are back to the base game again
		if (!hasFreespinGameStarted)
		{
			Scheduler.run();
			//Block spins from happening while we have tasks that can still execute and while dialogs are on screen
			while (Scheduler.hasTaskCanExecute || Dialog.instance.isBusy)
			{
				yield return null;
			}
		}

		//tell the feature ui we've completed a spin
		InGameFeatureContainer.onSpinComplete();

		// double check we should still be auto spinning before starting another
		if (hasAutoSpinsRemaining)
		{
			startNextAutospin();
		}
		else
		{
			if (Glb.spinTransactionInProgress)
			{
				Glb.endSpinTransaction();
			}

			Overlay.instance.setButtons(true);
			resetSlotMessage();
			lastSpinXPMultiplier = 0;
			SwipeableReel.canSwipeToSpin = true;
			queuedForcedOutcome = null;
		}
	}

	/// Checks if a keyboard key was pressed while a game is running, and reporting it to the game.
	protected void gameKey(string character, KeyCode keyCode)
	{
		if (Input.GetKeyDown(character))
		{
			touchKey(keyCode);
		}
	}
	
#if UNITY_EDITOR
	// Check the testGUI checkbox in the Editor to turn on a cute little ASCII version of the slots.
	void OnGUI()
	{
		if (testGUI)
		{
			GUI.skin = MobileUIUtil.shouldUseLowRes ? GUIScript.instance.devSkinLow : GUIScript.instance.devSkinHi;
			windowRect = GUILayout.Window(WINDOW_HANDLE_ID, windowRect, drawWindow, "Slot Test Controls");
		}
	}
#endif
	
	/// Function to draw a Unity GUI window
	private void drawWindow(int handle)
	{
		GUILayout.BeginVertical();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("SYMBOLS"))
		{
			log(engine.getSymbolList());
		}
		else if (GUILayout.Button("X"))
		{
			testGUI = false;
		}
		
		GUILayout.EndHorizontal();
		
		engine.drawReelWindow();
		
		logScroll = GUILayout.BeginScrollView(logScroll);
		
		string finalLogTest = logText + _outcomeDisplayController.getLogText();
		
		GUILayout.TextArea(finalLogTest, GUILayout.Width(1000), GUILayout.Height(2000));
		GUILayout.EndScrollView();
		
		GUILayout.EndVertical();
		GUI.DragWindow(dragRect);
	}
	
	/// A log function so other classes can go SlotController.instance.log("whatever")
	public void log(string message)
	{
		logText = string.Format("[{0:0.0}] {1}\n", Time.time, message) + logText;
		logScroll.y = 0f;
	}
	
	// Tells if a valid spin could be made right now, used by the SpinPanel to determine
	// if interactions are valid or whether it should ignore them until the game is in
	// a valid state where it can spin
	public override bool isAbleToValidateSpin()
	{
		if (ServerAction.isSpinActionQueued)
		{
			Debug.LogError("SlotBaseGame.validateSpin() - Attempting to start another spin action while there is already one queued, this shouldn't be happening and needs to be investigated!");
			return false;
		}

		if (isGameBusy)
		{
			Debug.LogWarning("Pressing spin button twice in a row. Not validating spin.");
			return false;
		}

		if (isFeatureBlockingSpins)
		{
			Debug.LogWarning("Spin being blocked by a feature");
			return false;
		}

		return engine.isStopped && !_outcomeDisplayController.isSpinBlocked();
	}
	
	// The spin button has been clicked, so validate the wager against the player's credits
	// before actually starting the spin. If the spin is not from a swipe then the direction is always going to be down
	// if it is from a spin then all reels start at the same time.
	public virtual bool validateSpin(bool forcedOutcome = false, bool isFromSwipe = false, SlotReel.ESpinDirection direction = SlotReel.ESpinDirection.Down)
	{
#if ZYNGA_TRAMP
		AutomatedPlayer.spinClicked();
#endif
		bool isValid = false;
		if (GameExperience.totalSpinCount == 0) 
		{
			//TODO: Girish
			//ZyngaFacebookEvents.trackCompletedTutorial();
		}
		
		if (engine == null)
		{
			Bugsnag.LeaveBreadcrumb("SlotBaseGame.validateSpin(): engine is null!");
		}
		
		if (_outcomeDisplayController == null)
		{
			Bugsnag.LeaveBreadcrumb("SlotBaseGame.validateSpin(): _outcomeDisplayController is null!");
		}

		bool isAllowedToSpin = isAbleToValidateSpin();
		
		if (isAllowedToSpin)
		{
			if (!checkAndHandleOutOfCoins())
			{
				// The spin is valid. Lets start the spin transaction here.
				spinsStartingCredits = SlotsPlayer.creditAmount;
				Glb.beginSpinTransaction(betAmount);

				// Choose which spin sound that we should be making. The first spin coming from the idle state is different.
				if (!(this is TumbleSlotBaseGame))
				{
					if (isGoingIdle || Audio.instance.firstSpin)
					{
						Audio.play(Audio.soundMap("spin_reel"));
					}
					else
					{
						Audio.play(Audio.soundMap("spin_already"));
					}
				}

				SlotsPlayer.subtractCredits(betAmount, "spinWager");

				// Remember the spin multiplier just in case it changes between the time
				// the action is sent and the backend processes the outcome. If we get a
				// multiplier change in the same batch as the outcome, then we need to adjust
				// the client XP amount appropriately.
				lastSpinXPMultiplier = XPMultiplierEvent.instance.xpMultiplier;
				
				long xpAmount = lastSpinXPMultiplier * SlotBaseGame.instance.betAmount;
				SlotsPlayer.instance.xp.add(xpAmount, "spin");	// Only add it on the client. The backend automatically adds it with the spin action.

				GameExperience.addSpin(GameState.game.keyName);
				
				// Ensure that all swipe to spin stuff is correctly cancelled and reset.
				// This should happen regardless of if the spin actually started from a swipe.
				// Since a player could have swiped more than one reel but only one will start
				// the spin, but we should cancel the swiping on all of them.  Also, if a player
				// is swiping and then starts a spin via spacebar we also want to cancel the swiping.
				engine.cancelSwipeAndRestoreSymbolsToOriginalLayersForSwipeableReels();

				if (!forcedOutcome)
				{
					startSpin(isFromSwipe, direction);
				}

				// mark that we should be waiting for a timeout that will either runout or will be canceled when
				// we recieve an outcome from the server
				setIsCheckingSpinTimeout(true);

				isValid = true;

				engine.clearLinkedReelDataOnAllSwipeableReels();
			}
		}

		if (isValid)
		{
			didPetRespin = false; //reset flag
			
			if (onSpinPressed != null)
			{
				onSpinPressed();
			}

			if (Overlay.instance != null)
			{
				if (Overlay.instance.jackpotMystery == null)
				{
					Bugsnag.LeaveBreadcrumb("SlotBaseGame.validateSpin(): Overlay.instance.jackpotMystery is null!");
				}

				Overlay.instance.jackpotMystery.hideTooltip();
				if (SpinPanel.instance.isShowingCollectionOverlay && tokenBar != null)
				{
					tokenBar.spinPressed();
				}

				TicketTumblerFeature.instance.onSpin();



				if (SpinPanel.instance == null)
				{
					Bugsnag.LeaveBreadcrumb("SlotBaseGame.validateSpin(): SpinPanel.instance is null!");
				}

				if (SpinPanel.hir == null)
				{
					Bugsnag.LeaveBreadcrumb("SlotBaseGame.validateSpin(): SpinPanel.hir is null!");
				}

				SpinPanel.instance.slideOutPaylineMessageBox();
				if (SpinPanel.hir.objectivesGrid != null)
				{
					SpinPanel.hir.objectivesGrid.playSpinAnimations();
				}

				if (SpinPanel.instance.collectionsPanel != null)
				{
					SpinPanel.instance.collectionsPanel.closePanel();
				}

				if (EliteManager.hasActivePass && Overlay.instance != null && Overlay.instance.topV2 != null)
				{
					Overlay.instance.topV2.onEliteRebate();
				}
				
				InGameFeatureContainer.onStartNextSpin(betAmount);
			}
			
			UserActivityManager.instance.onSpinReels();
			isPerformingSpin = true;
			lastBigWinHash = "";
		}

		return isValid;
	}

	public bool notEnoughCoinsToBet()
	{
		long amountToCheck = SlotsPlayer.creditAmount;

		if (Data.debugMode && DevGUIMenuOutOfCoins.outOfCoinsOnNextSpin)
		{
			amountToCheck = 0;
		}

		return (amountToCheck < betAmount);
	}

	// returns true if player does not have enough coins for bet
	private bool checkAndHandleOutOfCoins()
	{
		if (notEnoughCoinsToBet())
		{
			showOutOfCoinsUI();
			autoSpins = 0;
			SpinPanel.instance.hideAutoSpinPanel();
			return true;
		}
		return false;
	}

	public void showOutOfCoinsUI()
	{
		// watch to earn always comes first, it makes the most money
		// is is offered by NeedCreditsSTUDDialog
		if (OOCReboundFeature.isAvailableForCollect)
		{
			//do special ooc action.  Event will determine if user sees a dialog or inbox message (or nothing at all)
			//using a closure because slotbase game could potentially be destroyed before this returns if the user clicks fast enough
			OOCReboundFeature.triggerSpecialOOCEvent();
		}
		else if (WatchToEarn.isEnabled && ExperimentWrapper.WatchToEarn.shouldShowOutOfCredits)
		{
			statFamily = "w2e";
			NeedCreditsSTUDDialog.showDialog();
		}
		else if (ExperimentWrapper.FirstPurchaseOffer.isInExperiment)
		{
			StatsManager.Instance.LogCount(counterName: "dialog", 
				kingdom: "buy_page_v3", 
				phylum:"auto_surface", 
				klass: "first_purchase_offer",
				genus: "view");

			statFamily = "first_purchase_offer";
			BuyCreditsDialog.showDialog();
		}
		else if (StarterDialog.isActive) //Show the starter dialog if its active
		{
			statFamily = "starter_pack";
			StarterDialog.showDialog();
		}
		else if (ExperimentWrapper.OutOfCoinsBuyPage.showIntermediaryDialog)
		{
			NeedCredtisNotifyDialog.showDialog();
		}
		else if (STUDSale.isSaleActive(SaleType.POPCORN) && ExperimentWrapper.OutOfCoinsBuyPage.shouldShowSale)
		{
			statFamily = "popcorn";
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.POPCORN), "");
		}
		else if (ExperimentWrapper.OutOfCoinsBuyPage.isInExperiment)
		{
			if (ExperimentWrapper.OutOfCoinsBuyPage.shouldShowSale)
			{
				if (PurchaseFeatureData.isSaleActive)
				{
					statFamily = "ooc_buy_page";
					BuyCreditsDialog.showDialog("", skipOOCTitle:true, statsName:"");
				}
				else
				{
					statFamily = "ooc_buy_page";
					BuyCreditsDialog.showDialog("", skipOOCTitle:false, statsName:"ooc_buy_page");
				}
			}
			else
			{
				statFamily = "ooc_buy_page";
				BuyCreditsDialog.showDialog("", skipOOCTitle:false, statsName:"ooc_buy_page");
			}
		}
		else
		{
			statFamily = "";
			NeedCreditsSTUDDialog.showDialog();
		}

		if (GameState.game != null)
		{
			StatsManager.Instance.LogCount("dialog", "out_of_coins", statFamily, GameState.game.keyName, "", "view");
		}
	}

	public static void logOutOfCoinsPurchaseStat(bool didPurchase)
	{
		if (instance != null && GameState.game != null && instance.notEnoughCoinsToBet())
		{
			if (didPurchase)
			{
				StatsManager.Instance.LogCount("dialog", "out_of_coins", instance.statFamily, GameState.game.keyName, "", "cta");
			}
			else
			{
				StatsManager.Instance.LogCount("dialog", "out_of_coins", instance.statFamily, GameState.game.keyName, "", "close");
			}		
		}
	}
	
	// startSpin - both fetches an outcome from the server and starts the reel spin visuals.
	// If the spin is not from a swipe then the direction is always going to be down, if it is from a spin then all reels start at the same time.
	protected override IEnumerator startSpinCoroutine(bool isFromSwipe = false, SlotReel.ESpinDirection direction = SlotReel.ESpinDirection.Down, bool requestServerOutcome = true)
	{
		if (engine.isStopped && !_outcomeDisplayController.isSpinBlocked())
		{
			isBaseBonusOutcomeProcessed = false;
			Overlay.instance.setButtons(false);
			engine.animationCount = 0;
			
			clearOutcomeDisplay();

			// give the derived game a chance to handle cleanup or updates that need to happen before the next spins starts
			yield return StartCoroutine(prespin());

			setMessageText(Localize.text("good_luck"));
			
			Overlay.instance.setButtons(false);

			// make sure we reset everything to not be doing stuff for reevaluated spins
			reevaluationSpinsRemaining = 0;
			_currentReevaluationSpin = null;
			clearReevalStickySymbolInfo();
			if (hadRevalsLastSpin)
			{
				hadRevalsLastSpin = false;
				engine.setReelsStoppedCallback(_oldReelStoppedCallback);
				_oldReelStoppedCallback = null;
			}
			
			if (isFromSwipe)
			{
				engine.spinReelsFromSwipe(direction);
			}
			else
			{
				engine.spinReels();
			}

			_outcome = null;

			// clear reevaluationSpins now that they should all be handled
			reevaluationSpins.Clear();
			
			// Only reset the winnings display with each spin with a delay so that players can see what they won.
			StartCoroutine(zeroWinningsDisplayAfterDelay(0.5f));
			
			playSpinMusic();
			yield return StartCoroutine(reelsSpinning());
			
			// Send the spin action after resetting all the stuff above first.
			// This is critical for games that immediately get local test data
			// using outcomes from local resources, where there is no delay.
			if (requestServerOutcome)
			{
				SlotAction.spin(slotGameData.keyName, (int) _multiplier, (int) betAmount, currentWager, XPMultiplierEvent.instance.isEnabled, slotOutcomeEventCallback);
			}

			// Use the following line instead of the previous line to test the game_locked event without actually having the game locked.
			// SlotAction.spin(slotGameData.keyName, (int)_multiplier, gameLockedEventCallback);
			Bugsnag.LeaveBreadcrumb("Start spin in base game was triggered for " + slotGameData.keyName);
		}
	}
	
	/// Sets or gets the wager multiplier provided by the spin panel.
	// @todo : making an assumption that we can just calculate the multiplier needed for bonus games base on the wager_sets now instead of using data
	public override long multiplier
	{
		get 
		{
			if (GameState.giftedBonus != null)
			{
				// getting the multiplier for a gifted freespins in base game
				return GiftedSpinsVipMultiplier.playerMultiplier;
			}
			else
			{
				return SlotsWagerSets.getMultiplierForGameWagerSetValue(GameState.game.keyName, currentWager);
			}
		}
		
		set
		{
			base.multiplier = value;
			resetSlotMessage();
		}
	}

	// special funciton for doing forced big win rollups that only rolls up the big win value
	public void onBigWinOnlyRollup(long payoutValue)
	{
		if (bigWinEffect != null)
		{
			bigWinEffect.setAmount(payoutValue + runningPayoutRollupValue);
		}

		lastPayoutRollupValue = payoutValue;
	}
	
	// Sets the winning display value in the spin panel as well as the big win effect if applicable
	public override void setWinningsDisplay(long amount)
	{
		// set the value in the correct spin panel
		base.setWinningsDisplay(amount);

		// If big win is present update the big win label too
		if (bigWinEffect != null)
		{
			bigWinEffect.setAmount(amount);
		}
	}

	protected GameObject getBigWinPrefab(long payout)
	{
		if (megaWin != null && payout >= Glb.MEGA_WIN_THRESHOLD * SpinPanel.instance.betAmount)
		{
			return megaWin;
		} 
		else if (superWin != null && payout >= Glb.SUPER_WIN_THRESHOLD * SpinPanel.instance.betAmount)
		{
			return superWin;
		} 
		else if (bigWin != null)
		{
			return bigWin;
		} 
		else
		{
			return null;
		}
	}

	// Called by OutcomeDisplayController when a big win is triggered
	protected override void onBigWinNotification(long payout, bool isSettingStartingAmountToPayout = false)
	{

		// check if we have queued bonuses, and skip the big win until the queue is cleared
		if (_outcome.hasQueuedBonuses)
		{
			return;
		}

		if (areModulesDelayingBigWin)
		{
			return;
		}

		Overlay.instance.setButtons(false); 

		if (bigWinGameObject != null)
		{
			Destroy(bigWinGameObject);
		}
		
		// Choose from big/super/mega (if exists)
		GameObject bigWinPrefab = getBigWinPrefab(payout);              
		if (bigWinPrefab != null)
		{

#if UNITY_WSA_10_0 && NETFX_CORE

			var isInExperiment = ExperimentWrapper.Win10LiveTile.isInExperiment;

			Debug.Log("SlotBaseGame::onBigWinNotification() - win10 live tile experiment enabled: " + isInExperiment);
			if (isInExperiment)
			{
				sendWin10BigWinLiveTile(payout);
			}

#endif

			if (Dialog.instance != null && GameState.game.keyName == "bbh01")
			{
				Dialog.instance.keyLight.SetActive(false);
			}

			bigWinGameObject = (GameObject)CommonGameObject.instantiate(bigWinPrefab, bigWinPrefab.transform.position, this.transform.rotation);
			bigWinGameObject.transform.parent = this.transform;
			bigWinGameObject.SetActive(true);
			bigWinEffect = bigWinGameObject.GetComponent<BigWinEffect>();
			if (bigWinEffect != null)
			{
				bigWinEffect.bigWinEndCallback = bigWinEndCallback;
				bigWinEffect.payout = payout;
				if (isSettingStartingAmountToPayout)
				{
					bigWinEffect.setAmount(payout);
				}
				else
				{
					bigWinEffect.setAmount(lastPayoutRollupValue);
				}
			}
			bool letModuleHandleSounds = false;
			foreach (SlotModule module in cachedAttachedSlotModules)
			{
				if (module.needsToOverrideBigWinSounds())
				{
					letModuleHandleSounds = true;
					module.overrideBigWinSounds();
				}
			}

			if (!letModuleHandleSounds)
			{
				if (Audio.canSoundBeMapped("bigwin_intro"))
				{
					Audio.playSoundMapOrSoundKey("bigwin_intro");
				}
				string bigWinVOSound = Audio.soundMap("bigwin_vo_sweetener");
				if (bigWinVOSound != null && bigWinVOSound != "")
				{
					Audio.play(bigWinVOSound, 1.0f, 0.0f, 1.5f);
				}
			}
		}
	}

	void sendWin10BigWinLiveTile(long payout)
	{
#if UNITY_WSA_10_0 && NETFX_CORE

		if(payout > 0)
		{
			//Send Windows 10 Live Tile update, expiring in 2 hours
			Debug.Log("SlotBaseGame::sendWin10BigWinLiveTile() - sending Windows 10 live tile update");

			string actualCoins = CreditsEconomy.convertCredits(payout);
			string bigWinNotifHeaderText = "BIG WIN";
			string bigWinNotifBodyTextShort = "You won " + actualCoins + " coins!";
			string bigWinNotifBodyText = "You won " + actualCoins + " coins on " + GameState.game.name + " slots! Way to Go!";
			string imageSrcMed = "Assets/Square150x150Logo.png";
			string imageSrcWide = "Assets/Wide310x150Logo.png";
			string imageSrcLarge = "Assets/Square310x310Logo.png";

			var updater = TileUpdateManager.CreateTileUpdaterForApplication();

			var tileContent = new TileContent()
			{
				Visual = new TileVisual()
				{
					TileMedium = new TileBinding()
					{
						Branding = TileBranding.None,

						Content = new TileBindingContentAdaptive()
						{
							TextStacking = TileTextStacking.Center,

							BackgroundImage = new TileBackgroundImage()
							{
								Source = imageSrcMed,
								HintOverlay = 60,
							},

							Children =
							{
								new AdaptiveText()
								{
									Text = bigWinNotifHeaderText,
									HintStyle = AdaptiveTextStyle.Subtitle,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},

								new AdaptiveText()
								{
									Text = bigWinNotifBodyTextShort,
									HintStyle = AdaptiveTextStyle.Caption,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},
							},
						}
					},

					TileWide = new TileBinding()
					{
						Branding = TileBranding.None,

						Content = new TileBindingContentAdaptive()
						{
							TextStacking = TileTextStacking.Center,

							BackgroundImage = new TileBackgroundImage()
							{
								Source = imageSrcWide,
								HintOverlay = 60,
							},

							Children =
							{
								new AdaptiveText()
								{
									Text = bigWinNotifHeaderText,
									HintStyle = AdaptiveTextStyle.Title,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},

								new AdaptiveText()
								{
									Text = bigWinNotifBodyTextShort,
									HintStyle = AdaptiveTextStyle.Base,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},
							},
						}
					},

					TileLarge = new TileBinding()
					{
						Branding = TileBranding.None,

						Content = new TileBindingContentAdaptive()
						{
							TextStacking = TileTextStacking.Center,

							BackgroundImage = new TileBackgroundImage()
							{
								Source = imageSrcLarge,
								HintOverlay = 80,
							},

							Children =
							{
								new AdaptiveText()
								{
									Text = bigWinNotifHeaderText,
									HintStyle = AdaptiveTextStyle.Header,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},

								new AdaptiveText()
								{
									Text = bigWinNotifBodyTextShort,
									HintStyle = AdaptiveTextStyle.Base,
									HintWrap = true,
									HintAlign = AdaptiveTextAlign.Center
								},
							},
						}
					}
				}
			};

			//clear existing tile update first
			updater.Clear();

			//send the update
			var tileNotif = new TileNotification(tileContent.GetXml());
			tileNotif.ExpirationTime = DateTimeOffset.Now.AddHours(2);
			updater.Update(tileNotif);
		}
#endif
	}

	// Allow the big win to be force ended, only really needed for stuff like the ReshuffleModule where
	// a big win occurs before the OutcomeDisplayController is able to finalizeRollup which would normally
	// end the big win
	public IEnumerator forceEndBigWinEffect(bool isDoingContinueWhenReady)
	{
		if (bigWinEffect != null)
		{
			yield return StartCoroutine(bigWinEffect.endBigWin());

			if (isDoingContinueWhenReady)
			{
				yield return StartCoroutine(continueWhenReady());
			} 
		}
	}

	// Called when the rollup finishes, uses the endCallback in SlotUtils.doRollup
	public override IEnumerator onEndRollup(bool isAllowingContinueWhenReady, bool isAddingRollupToRunningPayout = true)
	{
		if (isAddingRollupToRunningPayout)
		{
			moveLastPayoutIntoRunningPayoutRollupValue();
		}
		else
		{
			lastPayoutRollupValue = 0;
		}

		if (bigWinEffect != null && !bigWinEffect.isEnding)
		{
			yield return StartCoroutine(bigWinEffect.endBigWin());

			yield return StartCoroutine(waitForModulesAfterPaylines(true));

			if (isAllowingContinueWhenReady)
			{
				yield return StartCoroutine(continueWhenReady());
			}
		}
		else if (isAllowingContinueWhenReady)
		{
			yield return StartCoroutine(waitForModulesAfterPaylines(true));
			// If our effect is null, let's not have it stop progression. Just a safety check.
			yield return StartCoroutine(continueWhenReady());
		}
	}

	// Called by BigWinEffect.cs when the complete animation is ended and the game can resume again
	protected virtual void bigWinEndCallback(long payout)
	{
		Destroy(bigWinGameObject);
		bigWinEffect = null;
		bigWinGameObject = null;

		if (Dialog.instance != null && GameState.game.keyName == "bbh01")
		{
			Dialog.instance.keyLight.SetActive(true);
		}

		bool shouldShowBigWinEndDialogs = true;
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.willHandleBigWinEndDialogs())
			{
				shouldShowBigWinEndDialogs = false;
				break;
			}
		}

		if (shouldShowBigWinEndDialogs)
		{
			showBigWinEndDialogs();
		}
		else
		{
			needsToShowBigWinEndDialogs = true;
		}

		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.needsToExecuteOnBigWinEnd())
			{
				module.executeOnBigWinEnd();
			}
		}

		//Since the Big Win was possibly blocking dialogs, lets check if we're able to open a dialog once this is over
		Scheduler.run();
	}

	// special handling for gifted games that use playFreespinsInBasegame
	public void startGiftedFreeSpinsInBaseGame(InboxItem inboxItem)
	{
		Debug.Assert(playFreespinsInBasegame, "playFreespinsInBasegame must be enabled.");

		Overlay.instance.setButtons(false);
		Overlay.instance.top.show(false);

		// ensure that the _multiplier variable matches the value that SlotBaseGame will return from calling the multiplier property
		_multiplier = multiplier;

		// swap to the bonus game spin panel immediately
		// and zero out the winning amount and spin count since they can't be set until we get the outcome
		SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(0);
		BonusSpinPanel.instance.spinCountLabel.text = "0";

		Server.registerEventDelegate("slots_outcome", giftedSlotOutcomeEventCallback);
		SlotAction.playBonusGame(inboxItem.eventId, "gifting");
	}

	// callback for running gifted free spins in base
	private void giftedSlotOutcomeEventCallback(JSON data)
	{
		StartCoroutine(giftedSlotOutcomeEventCallbackCoroutine(data));
	}

	// callback coroutine for running gifted free spins in base
	private IEnumerator giftedSlotOutcomeEventCallbackCoroutine(JSON data)
	{
		setOutcome(data);

		Debug.Assert(_outcome.isBonus, "Outcome must be a bonus.");
		Debug.Assert(_outcome.isGifting, "Outcome must be from a gift.");

		hasFreespinGameStarted = true;
		base.initFreespins();

		// wait for the freespins in base to be ready before proceeding,
		// otherwise things might happen incorrectly
		while (!isFreeSpinInBaseReady)
		{
			yield return null;
		}
		// reset this for the next spin
		isFreeSpinInBaseReady = false;

		startNextFreespin();
	}

	// Default just returns runningPayoutRollupValue but tumble/plop games have to return a different value to show the full total
	public virtual long getBigWinDialogWinAmount()
	{
		return runningPayoutRollupValue;
	}

	public void showBigWinEndDialogs()
	{
		needsToShowBigWinEndDialogs = false;

		if (!SlotsPlayer.isFacebookUser && !Sharing.isAvailable)
		{
			// Do this here since logged-in users get it when closing the big win share dialog.
			RateMe.checkAndPrompt(RateMe.RateMeTrigger.BIG_WIN);
		}
	}

	// trigger any queued bonus game if they are present
	public IEnumerator triggerQueuedBonusGames()
	{
		// if bonuses are queued up then we will launch them here
		if (_outcome.hasQueuedBonuses)
		{
			// don't build and try and start a new bonus if we are doing freespins in base and this is a freespins bonus
			// since that will be handled in a section below, if it isn't that then we'll try and launch the next bonus here
			bool isBonusFreespinInBase = playFreespinsInBasegame && _outcome.isBonus && _outcome.isGifting;
			if (!isBonusFreespinInBase)
			{
				yield return StartCoroutine(attemptBonusCreation());

				bool shouldModuleCreateBonus = false;
				foreach (SlotModule module in cachedAttachedSlotModules)
				{
					//Let the module create the bonus if there isn't going to be a portal created by the banners
					//Need the banners check to prevent the portal from being skipped and us going straight to the transition.
					if (module.needsToLetModuleCreateBonusGame() && banners.Length == 0)
					{
						shouldModuleCreateBonus = true;
					}
				}

				if (!shouldModuleCreateBonus)
				{
					startBonus();
					yield break;
				}
			}
		}
	}

	// triggers the freespins in base
	private IEnumerator triggerFreespinsInBase()
	{
		if (playFreespinsInBasegame)
		{
			if (hasFreespinGameStarted)
			{
				if (!hasFreespinsSpinsRemaining)
				{
					//Pause before the popup
					yield return new WaitForSeconds(FREESPINS_TRANSISTION_TIME);
					showFreespinsEndDialog(runningPayoutRollupValue, basegameFreespinsSummaryClosed);
					
					// re-enable the top panel if the freespins is complete
					Overlay.instance.top.show(true);
					Overlay.instance.setButtons(true);
				}
			}
			else
			{
				if (_outcome.isBonus && _outcome.isGifting)
				{
					hasFreespinGameStarted = true;
					outcomeBeforeFreespinsInBase = _outcome;
					
					base.initFreespins();

					// wait for the freespins in base to be ready before proceeding,
					// otherwise things might happen incorrectly
					while (!isFreeSpinInBaseReady)
					{
						yield return null;
					}
					// reset this for the next spin
					isFreeSpinInBaseReady = false;

					// hide the top panel as with a normal freespins
					Overlay.instance.top.show(false);
					Overlay.instance.setButtons(false);	
				}
			}
		}
	}
		
	// Commonly used code to continue normal operation when the game is ready.
	protected IEnumerator continueWhenReady()
	{
		checkAndHandleOutOfCoins();

		// make sure this is reset so that if there is a queued bonus it will be able to trigger bonus acquired effects
		isBonusOutcomePlayed = false;

		// check if we've paid out, but still have bonus games to trigger, this can happen if freespins in base is one of the queued bonuses
		if (_outcome.hasQueuedBonuses)
		{
			yield return StartCoroutine(triggerQueuedBonusGames());
		}

		// triggerQueuedBonusGames only checks bonus games we have to create, we also need to check if a freespins in base was triggered
		yield return StartCoroutine(triggerFreespinsInBase());
		
		// Trigger a bonus that was delayed because we wanted to pay out the base game winnings before the bonus
		if (isExecutingBonusAfterPayout)
		{
			// Clear out all the wins that should have already been rolled up to the player account.  The bonus
			// is going to be treated as its own thing.
			isNonBonusWinningsAlreadyPaid = true;
			resetAllRollupValues();
			_outcomeDisplayController.clearOutcome();
			yield return StartCoroutine(attemptBonusCreation());
			isExecutingBonusAfterPayout = false;
			yield return StartCoroutine(presentCurrentBonusOutcome());
			yield break;
		}

		// Trigger special stuff that happen after spins are finished out but before the player spins again.
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteDuringContinueWhenReady())
			{
				yield return StartCoroutine(module.executeDuringContinueWhenReady());
			}
		}

		// Check if we need to trigger a big win that got delayed by a module, and if so exit out of this function, assuming that we will unlock the game after the big win
		if (isOverBigWinThreshold(runningPayoutRollupValue))
		{
			foreach (SlotModule module in cachedAttachedSlotModules)
			{
				if (module.needsToTriggerBigWinBeforeSpinEnd())
				{
					yield return StartCoroutine(module.executeTriggerBigWinBeforeSpinEnd());
					// exit out now and wait for the big win to finish
					yield break;
				}
			}
		}

		if (hasReevaluationSpinsRemaining)
		{
			// Check if onOutcomeSpinBlockRelease callback is going to start the next autospin itself, in which case don't start it here
			if (!_outcomeDisplayController.isSpinBlocked())
			{
				StartCoroutine(startNextReevaluationSpin());
			}
		}
		else if (hasFreespinsSpinsRemaining)
		{	
			// Check if onOutcomeSpinBlockRelease callback is going to start the next autospin itself, in which case don't start it here
			if (!_outcomeDisplayController.isSpinBlocked())
			{
				startNextFreespin();
			}			
		}
		else if (hasFreespinGameStarted)
		{
			// Need to not continue here just yet, because freespinInBase needs to end before we finish our spin
			// so just skip doing anything
		}
		else
		{
			yield return StartCoroutine(doSpecialWins(SpecialWinSurfacing.POST_NORMAL_OUTCOMES));

			// mark that the spin is completel, whether we start another autospin 
			// or just unlock the game will be handeled via the unlock code which
			// live in the Update() loop
			isSpinComplete = true;
			
			// Moving this here, since I feel like it wasn't handling things correctly
			// for cases where a reevaluation spin could trigger
			isInTheMiddleOfAnAutoSpinSpin = false;
		}
	}

	//Used to transition into basegame Freespins
	protected override IEnumerator continueToBasegameFreespins()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++) 
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnContinueToBasegameFreespins())
			{
				yield return StartCoroutine(module.executeOnContinueToBasegameFreespins());
			}
		}

		// Force the bonus acquired effects to trigger if they haven't already
		if (!isBonusOutcomePlayed)
		{
			// handle playing the BN symbol animations and the bonus acquired sound before moving into the freespins in base if we haven't done them yet
			yield return StartCoroutine(doPlayBonusAcquiredEffects());
		}

		// call what the base version would call as far as executing game start modules
		yield return StartCoroutine(executeGameStartModules());

		// Change the spin panel out if we aren't doing gifted spins (in which case the spin panel has already been swapped)
		if (GameState.giftedBonus == null)
		{
			// Standard freespins in base, slide the base game spin panel over to reveal the bonus game spin panel
			yield return StartCoroutine(SpinPanel.instance.swapSpinPanels(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Left, 0.5f, false));
		}

		playBgMusic();

		isBonusOutcomePlayed = false;
		isFreeSpinInBaseReady = true;
	}

	// The Q Key, can be used for any unique feature for a game
	public void forceFeatureQ()
	{
		forceOutcome(featureQForcedOutcomes);
	}

	// The R Key, can be used for any unique feature for a game
	public void forceFeatureR()
	{
		forceOutcome(featureRForcedOutcomes);
	}

	// The E Key, can be used for any unique feature for a game
	public void forceFeatureE()
	{
		forceOutcome(featureEForcedOutcomes);
	}

	// The T Key, can be used for any unique feature for a game
	public void forceFeatureT()
	{
		forceOutcome(featureTForcedOutcomes);
	}

	// The S Key, can be used for any unique feature for a game
	public void forceFeatureS()
	{
		forceOutcome(featureSForcedOutcomes);
	}

	// The 0 Key, can be any unique feature for a game
	public void forceFeature0()
	{
		forceOutcome(feature0ForcedOutcomes);
	}

	// The 1 Key, can be any unique feature for a game
	public void forceFeature1()
	{
		forceOutcome(feature1ForcedOutcomes);
	}

	// The 2 Key, can be any unique feature for a game
	public void forceFeature2()
	{
		forceOutcome(feature2ForcedOutcomes);
	}

	// The 3 Key, can be any unique feature for a game
	public void forceFeature3()
	{
		forceOutcome(feature3ForcedOutcomes);
	}

	// The 4 Key, can be any unique feature for a game
	public void forceFeature4()
	{
		forceOutcome(feature4ForcedOutcomes);
	}

	// The 5 Key, can be any unique feature for a game
	public void forceFeature5()
	{
		forceOutcome(feature5ForcedOutcomes);
	}	

	// The 6 Key, can be any unique feature for a game
	public void forceFeature6()
	{
		forceOutcome(feature6ForcedOutcomes);
	}	

	// The 7 Key, can be any unique feature for a game
	public void forceFeature7()
	{
		forceOutcome(feature7ForcedOutcomes);
	}	

	// The 8 Key, can be any unique feature for a game
	public void forceFeature8()
	{
		forceOutcome(feature8ForcedOutcomes);
	}	

	// The 9 Key, can be any unique feature for a game
	public void forceFeature9()
	{
		forceOutcome(feature9ForcedOutcomes);
	}

	// Basically, the C key from web
	public void forceBonus()
	{
		forceOutcome(challengeForcedOutcomes);
	}
	
	// G Key From Web
	public void forceFreeSpin()
	{
		forceOutcome(freeSpinForcedOutcomes);
	}
	
	// This third bonus varies on all games, forced by the Y key.
	public void forceOtherBonus()
	{
		forceOutcome(otherBonusForcedOutcomes);
	}
	
	public void forceMutation()
	{
		forceOutcome(mutationForcedOutcomes);
	}
	
	public void forceSecondaryMutation()
	{
		forceOutcome(secondaryMutationForcedOutcomes);
	}
	
	public void forceScatterBonus()
	{
		forceOutcome(scatterBonusForcedOutcomes);
	}
	
	public void forceSecondaryScatterBonus()
	{
		forceOutcome(secondaryScatterBonusForcedOutcomes);
	}

	public void forceBigWin()
	{
		forceOutcome(bigWinForcedOutcomes);
	}

	// Force the server to send down the ftue (First Time User Experience) big win outcome
	public void forceFtueBigWin()
	{
		forceOutcome(ftueBigWinForcedOutcomes);
	}
	
	/// Trigger a forced outcome.
	protected virtual void forceOutcome(Dictionary<string, ForcedOutcome> outcomes)
	{
		StartCoroutine(forceOutcomeCoroutine(outcomes));
	}

	protected virtual IEnumerator forceOutcomeCoroutine(Dictionary<string, ForcedOutcome> outcomes)
	{
		if (isGameBusy && hasAutoSpinsRemaining)
		{
			queuedForcedOutcome = outcomes;
			yield break;
		}
		else if (GameState.game == null ||
			isGameBusy ||
			Dialog.instance.isShowing ||
			outcomes == null ||
			!outcomes.ContainsKey(GameState.game.keyName)
			)
		{
			yield break;
		}

		if (!validateSpin(true))
		{
			yield break;
		}

		Overlay.instance.setButtons(false);


		// make sure we reset everything to not be doing stuff for reevaluated spins
		reevaluationSpinsRemaining = 0;
		_currentReevaluationSpin = null;
		clearReevalStickySymbolInfo();
		if (hadRevalsLastSpin)
		{
			hadRevalsLastSpin = false;
			engine.setReelsStoppedCallback(_oldReelStoppedCallback);
			_oldReelStoppedCallback = null;
		}
		isBaseBonusOutcomeProcessed = false;
		// give the derived game a chance to handle cleanup or updates that need to happen before the next spins starts
		yield return StartCoroutine(prespin());
			
		engine.spinReels();
		_outcome = null;
		_outcomeDisplayController.clearOutcome();
		
		StartCoroutine(zeroWinningsDisplayAfterDelay(0.5f));

		yield return StartCoroutine(reelsSpinning());

		ForcedOutcome outcome = outcomes[GameState.game.keyName];

		// If there's a fake server message, set it in the base game so that it will be used.
		if (outcome.isUsingFakeServerMessage())
		{

			// If there are also forced outcomes for this same outcome, they will conflict and may behave unexpectedly.
			if (outcome.isUsingForcedOutcomes())
			{
				Debug.LogError("There are forced outcomes AND a fake server message. Defaulting to fake server message. This may cause problems.");
			}

			debugReelMessageTextFile = outcome.fakeServerMessage;

		}

		if (outcome.hasServerCheatKey && outcome.isUsingServerCheat)
		{
			// server cheat overrides client side forced outcomes if using this specific server cheat is enabled
			SlotAction.forceServerCheat(slotGameData.keyName, (int)_multiplier, (int)betAmount, currentWager, XPMultiplierEvent.instance.isEnabled, outcome.serverCheatKey, slotOutcomeEventCallback);
		}
		else if (outcome.focedLayerOutcomeInfos != null && outcome.focedLayerOutcomeInfos.Length > 0)
		{
			SlotAction.forceOutcome(slotGameData.keyName, (int)_multiplier, (int)betAmount, currentWager, XPMultiplierEvent.instance.isEnabled,  outcome.tierId, outcome.outcomes, outcome.getLayerToReelToStopDict(), slotOutcomeEventCallback);
		}
		else if (outcome.forcedIndependentReelInfos != null && outcome.forcedIndependentReelInfos.Length > 0)
		{
			SlotAction.forceOutcome(slotGameData.keyName, (int)_multiplier, (int)betAmount, currentWager, XPMultiplierEvent.instance.isEnabled,  outcome.tierId, outcome.outcomes, outcome.getIndependentEndData(), slotOutcomeEventCallback);
		}
		else
		{
			SlotAction.forceOutcome(slotGameData.keyName, (int)_multiplier, (int)betAmount, currentWager, XPMultiplierEvent.instance.isEnabled, outcome.tierId, outcome.outcomes, slotOutcomeEventCallback);
		}
		
		playSpinMusic();
	}
		
	/// slotStartedEventCallback - called by Server when we first enter the slot game.
	protected virtual void slotStartedEventCallback(JSON data)
	{
		setReelStopOrder();
		
		defaultReelSetName = data.getString("base_reel_set", "");
		setReelSet(defaultReelSetName, data);

		setupServerCheatKeys(data);
		
		addTokenBars(data);

		setupJackpotBars();

		setSwipeableReels();

		applyInitialReelStops();

		StartCoroutine(playGameStartModules(data));
	}

	// Reads out server scripted cheat keys which can be used in place of forced outcomes
	// which are setup on the client using ForcedOutcomeRegistrationModule
	private void setupServerCheatKeys(JSON slotGameStartedData)
	{
		string[] cheatsArray = slotGameStartedData.getStringArray("cheats");

		// Update ForcedOutcomeRegistrationModule to let it know about these keys
		// making entries for any it doesn't have them for and updating duplicates
		// to mark that there is a server option which will be toggleable
		ForcedOutcomeRegistrationModule registrationModule = getForcedOutcomeRegistrationModuleForGameKey(GameState.game.keyName);
		if (registrationModule == null)
		{
			Debug.LogWarning("SlotBaseGame.setupServerCheatKeys() - Game did not have a ForcedOutcomeRegistrationModule attached, creating and attaching one.");
			registrationModule = gameObject.AddComponent<ForcedOutcomeRegistrationModule>() as ForcedOutcomeRegistrationModule;
			registrationModule.targetGameKey = GameState.game.keyName;
		}

		registrationModule.updateWithServerCheats(cheatsArray);
	}

	protected virtual void addTokenBars(JSON data)
	{
		if (Overlay.instance == null || SpinPanel.instance == null)
		{
			Debug.LogError("Tried to add token bar but missing spin panel or overlay");
			return;
		}

		if (Overlay.instance.jackpotMystery == null)
		{
			needstoWaitForJackpotsToLoad = true;
		}
		
		if (SpinPanel.instance.isShowingSpecialWinOverlay && 
		    (ExperimentWrapper.VIPLobbyRevamp.isInExperiment || 
		     ExperimentWrapper.RoyalRush.isInExperiment))
		{
			if (Overlay.instance.jackpotMystery != null)
			{
				tokenBar = Overlay.instance.jackpotMystery.tokenBar;
			}
			
			isVipRevampGame = data.getBool("is_vip_revamp", false);
			isMaxVoltageGame = data.getBool("is_max_voltage", false);
			isRoyalRushGame = SpinPanel.instance.shouldShowRoyalRushOverlay;
			
			if (isVipRevampGame || isMaxVoltageGame || isRoyalRushGame)
			{
				if (tokenBar == null) //Set up the appropriate token bar here if it wasn't already by something else
				{
					if (Overlay.instance.jackpotMystery != null)
					{
						Overlay.instance.jackpotMysteryHIR.setUpTokenBar();	
					}
					else
					{
						Overlay.instance.pendingTokenBar = true;
						Overlay.instance.addJackpotOverlay();
					}
					needsToWaitForCollectionOverlayToLoad = true;
				}
				else if (!isCorrectTokenBarSetup()) //If the incorrect bar is set up, destroy the old one and create the correct one. Should only be needed when entering games through the dev menus
				{
					Destroy(tokenBar.gameObject);
					Overlay.instance.jackpotMysteryHIR.tokenBar = null;
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
					needsToWaitForCollectionOverlayToLoad = true;
				}
				else if (tokenBar != null)
				{
					tokenBar.setupBar(); //If we have a token bar loaded and its the correct one, then go ahead and set it up
				}

				if (SlotsPlayer.instance != null)
				{
					if (isVipRevampGame)
					{
						SlotsPlayer.instance.vipTokensCollected = data.getInt("vip_revamp_tokens", 0);
					}
				}
			}
			else
			{
				if (tokenBar != null && Overlay.instance.jackpotMystery != null)
				{
					Overlay.instance.jackpotMystery.tokenAnchor.SetActive(false); //If we have a token bar loading but are going into a game without a token feature, hide it
				}
			}
		}
	}

	private bool isCorrectTokenBarSetup()
	{
		if (isVipRevampGame && tokenBar as VIPTokenCollectionModule != null)
		{
			return true;
		}

		if (isMaxVoltageGame && tokenBar as MaxVoltageTokenCollectionModule != null)
		{
			return true;
		}

		if (isRoyalRushGame && tokenBar as RoyalRushCollectionModule != null)
		{
			return true;
		}
		return false;
	}

	public void onTokenBarLoadFinished()
	{
		//Called if we needed to load the token bar on the fly. Lets us know we're done loading it and can hide the loading screen and set up the bar correctly
		tokenBar = Overlay.instance.jackpotMystery.tokenBar;

		needsToWaitForCollectionOverlayToLoad = false;
		setupJackpotBars();

		if (tokenBar == null)
		{
			Debug.LogError("We should be setting up a token bar now but its null");
			needsToWaitForCollectionOverlayToLoad = false;
			needstoWaitForJackpotsToLoad = false;
		}
		else
		{
			tokenBar.setupBar();
		}
	}

	public void onJackpotBarsLoaded()
	{
		// Make sure we do this setup if the jackpot bar needed to be loaded,
		// since it is very likely that the attempt to call it from slotStartedEventCallback
		// will fail and happen before the jackpot bars are loaded if we have to download the
		// bundle and create the object, since needstoWaitForJackpotsToLoad doesn't block until
		// after that has happened.
		setupJackpotBars();
		
		needstoWaitForJackpotsToLoad = false;
	}

	public void onJackpotBarsLoadFailed()
	{
		needstoWaitForJackpotsToLoad = false;

		//Generic Dialog notifying the player that the game encountered an error loading. 
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.textTitle("actions_error_title"),
#if WEB_GL
				D.MESSAGE, Localize.textOr("load_game_error_msg_webgl","Something went wrong trying to load your game. Please try another browser."),
#else
				D.MESSAGE, Localize.textOr("load_game_error_msg","Something went wrong trying to load your game. Please try to reload."),
#endif 
				D.REASON, "jackpot-overlay-failed-to-load",
				D.CALLBACK, new DialogBase.AnswerDelegate((args) => 
					{ 
						GameState.pop();
						Loading.show(Loading.LoadingTransactionTarget.LOBBY); 
						Glb.loadLobby(); 
					})
			),
			SchedulerPriority.PriorityType.BLOCKING
		);
	}

	private void setupJackpotBars()
	{
		if (Overlay.instance.jackpotMystery != null)
		{
			// only show ProgressiveSelectBetDialog inside the game when using multipliers
			if (GameState.game.isProgressive || GameState.game.mysteryGiftType != MysteryGiftType.NONE)
			{
				// using flat wagers and the user already should have seen the bet selection dialog
				// Only set the wager to this value if it was set to something previously.
				if (GameState.game.progressiveMysteryBetAmount != 0)
				{
					setInitialProgressiveMysteryBetAmount(GameState.game.progressiveMysteryBetAmount);
				}
				// Set to variable to 0 after setting it above, so it only happens once (doesn't happen again when returning from bonus game).
				GameState.game.progressiveMysteryBetAmount = 0;
			}
		}
	}

	private IEnumerator playGameStartModules(JSON slotGameStartedData)
	{
		// Wait and make sure that the ReelGameBackground has updated
		yield return StartCoroutine(waitForReelGameBackgroundScalingUpdate());

		yield return StartCoroutine(executeGameStartModules());
		StartCoroutine(finishLoading(slotGameStartedData));
	}

	protected virtual IEnumerator finishLoading(JSON slotGameStartedData)
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnBaseGameLoad(slotGameStartedData))
			{
				runningCoroutines.Add(StartCoroutine(module.executeOnBaseGameLoad(slotGameStartedData)));
			}
		}
		// Wait for all the coroutines to end.
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
		if (needsToWaitForCollectionOverlayToLoad || needstoWaitForJackpotsToLoad)
		{
			while (needstoWaitForJackpotsToLoad && Overlay.instance.jackpotMystery == null)
			{
				yield return null;
			}
			
			while (needsToWaitForCollectionOverlayToLoad && tokenBar == null) //Lets not hide the loading screen until our token bar has been instantiated
			{
				yield return null;
			}

			if (needsToWaitForCollectionOverlayToLoad)
			{
				if (tokenBar == null)
				{
					Overlay.instance.jackpotMysteryHIR.setUpTokenBar();
				}
			}
		}
		Loading.hide(Loading.LoadingTransactionResult.SUCCESS, loadingScreenEnded);
		yield return null;
	}

	private void loadingScreenEnded()
	{
		if (SpinPanel.instance.isShowingCollectionOverlay && tokenBar != null)
		{
			StartCoroutine(playTokenBarIntroAnimations());
		}

		StartCoroutine(executePostLoadingScreenModules());

		if (tokenBar == null)
		{
			playGoodLuckSound();
		}

		// handle gifted freespins in base games
		
		if (GameState.giftedBonus != null)
		{
			startGiftedFreeSpinsInBaseGame(GameState.giftedBonus.inboxItem);
		}
		else
		{
			getBuiltInProgressiveBaseGameModule();
			if (builtInProgressiveModule != null && builtInProgressiveModule.betSelectorHideEvent != null)
			{
				if (EUEManager.shouldDisplayChallengeIntro)
				{
					builtInProgressiveModule.betSelectorHideEvent.AddListener(onBetSelectorHideShowChallengeIntro);	
				}

				if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
				{
					builtInProgressiveModule.betSelectorHideEvent.AddListener(onBetSelctorHideRefreshRichPassIcon);	
				}
			}
			else if (EUEManager.shouldDisplayChallengeIntro)
			{
				getAnimOnStartModule();
				if (animOnStartModule != null && animOnStartModule.onAnimationCompleteEvent != null)
				{
					animOnStartModule.onAnimationCompleteEvent.AddListener(playFtueOnIntroAnimationFinsihed);
				}
				else
				{
					EUEManager.showChallengeIntro();
				}
			}
		}

	}

	private PlayAnimationListOnSlotGameStartModule getAnimOnStartModule()
	{
		if (animOnStartModule != null)
		{
			return animOnStartModule;
		}
		foreach (SlotModule cacheduleModule in cachedAttachedSlotModules)
		{
			PlayAnimationListOnSlotGameStartModule module = cacheduleModule as PlayAnimationListOnSlotGameStartModule;
			if (module != null)
			{
				animOnStartModule = module;
				return module;
			}
		}

		return null;
	}

	private BuiltInProgressiveJackpotBaseGameModule getBuiltInProgressiveBaseGameModule()
	{
		if (builtInProgressiveModule != null)
		{
			return builtInProgressiveModule;
		}
		foreach (SlotModule cacheduleModule in cachedAttachedSlotModules)
		{
			BuiltInProgressiveJackpotBaseGameModule module = cacheduleModule as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				builtInProgressiveModule = module;
				return module;
			}
		}

		return null;
	}

	private void playFtueOnIntroAnimationFinsihed()
	{
		if (animOnStartModule != null && animOnStartModule.onAnimationCompleteEvent != null)
		{
			animOnStartModule.onAnimationCompleteEvent.RemoveListener(playFtueOnIntroAnimationFinsihed);	
		}
		EUEManager.showChallengeIntro();
	}

	private void onBetSelectorHideShowChallengeIntro()
	{
		if (builtInProgressiveModule != null && builtInProgressiveModule.betSelectorHideEvent != null)
		{
			builtInProgressiveModule.betSelectorHideEvent.RemoveListener(onBetSelectorHideShowChallengeIntro);
		}
		EUEManager.showChallengeIntro();
	}

	private void onBetSelctorHideRefreshRichPassIcon()
	{
		if (builtInProgressiveModule != null && builtInProgressiveModule.betSelectorHideEvent != null)
		{
			builtInProgressiveModule.betSelectorHideEvent.RemoveListener(onBetSelctorHideRefreshRichPassIcon);
		}
		InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.BET_CREDITS, true));
	}
	

	private IEnumerator playTokenBarIntroAnimations()
	{
		yield return StartCoroutine(tokenBar.slotStarted());
		playGoodLuckSound();
	}

	private IEnumerator executePostLoadingScreenModules()
	{
		List<TICoroutine> postLoadingScreenCoroutines = new List<TICoroutine>();
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteAfterLoadingScreenHidden())
			{
				postLoadingScreenCoroutines.Add(StartCoroutine(module.executeAfterLoadingScreenHidden()));
			}
		}
		
		// Wait for all the coroutines to end.
		if (postLoadingScreenCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(postLoadingScreenCoroutines));
		}
		
		isExecutingGameStartModules = false;
	}

	private void playGoodLuckSound()
	{
		string goodLuckSoundKey = Audio.soundMap("good_luck_first");
		if (goodLuckSoundKey != "dogoodluck" || GameState.game.keyName.Contains("oz"))
		{
			// The default sound for this sound is the oz sound.
			// Ideally we want to make the defualt not map to anything and just have the sounds in SCAT.
			Audio.play(goodLuckSoundKey);
		}
	}

	// Determine the matching index for the selected bet value and set it in the spin panel
	public void setInitialBetAmount(long betValue)
	{
		long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(GameState.game.keyName);

		if (betValue == 0)
		{
			// If, for some reason, there was no initial bet amount chosen,
			// then we need to automatically choose one.
			betValue = allBetAmounts[0];
		}
		
		for (int i = 0; i < allBetAmounts.Length; i++)
		{
			if (allBetAmounts[i] == betValue)
			{
				SpinPanel.instance.setWager(i);
				break;
			}
		}
		
		InGameFeatureContainer.onBetChagned(betValue);	
	}

	// Set the initial bet value and show the progressive amount
	private void setInitialProgressiveMysteryBetAmount(long betValue)
	{
		setInitialBetAmount(betValue);
		
		// Show the progressive amount when the game first starts.
		Overlay.instance.jackpotMystery.setQualifiedStatus();
	}

	/// Allow the slot to refresh the Ways to Win, needed for Splendor of Rome where Free-Spins use Line wins but the main game uses Clusters
	public void resetSpinPanelWaysToWin()
	{
		if (currentReelSetName != null)
		{
			setSpinPanelWaysToWin(currentReelSetName);
		}
		else
		{
			Debug.LogError("slotStartedEventCallback hasn't been called yet, or the reelSetName was somehow null");
		}
	}

	/// slotOutcomeEventCallback - after a spin occurs, Server calls this with the results.
	protected virtual void slotOutcomeEventCallback(JSON data)
	{
		Server.unregisterEventDelegate("slots_outcome", slotOutcomeEventCallback);

		// cancel handling this if we already timed out since we are going to force the player to refresh anyways
		// doing this in case the player left it spinning and did finally get a response but the dialog is already shown
		if (!isSpinTimedOut)
		{
			// cancel the spin timeout since we recieved a response from the server
			setIsCheckingSpinTimeout(false);

			// if a debug message is provided, use it.
			if (debugReelMessageTextFile != null)
			{

				string jsonString = debugReelMessageTextFile.text;
				// chop off the desctiption
				if (jsonString.FastStartsWith("//"))
				{
					jsonString = jsonString.Substring(jsonString.IndexOf('\n') + 1);
				}
				if (jsonString.FastStartsWith("/*"))
				{
					jsonString = jsonString.Substring(jsonString.IndexOf("*/") + 2);
					jsonString = jsonString.Substring(jsonString.IndexOf('\n') + 1); // Separated from */ in case someone accidentally left some spaces after their comment before their line break.
				}
				
				data = new JSON(jsonString);
				
				if (data.hasKey("events"))
				{			
					JSON[] eventsJSON = data.getJsonArray("events");
					data = eventsJSON[0];
				}

				debugReelMessageTextFile = null;
			}

			if (Data.debugMode)
			{
				DevGUIMenuInGameOnly.slotOutcomeString = data.ToString();
			}
			
			//save out bonus so we don't run the nospin animations when we do a fake spin
			//don't override pet bonus from the outcome we already parsed if it's not there
			bool hasPetBonusData = data.hasKey("pet_bonus")
#if !ZYNGA_PRODUCTION
				|| DevGUIMenuVirtualPets.bonusPayoutOverride > 0
#endif
				;
			if (data.hasKey("pet_respin"))
			{
				petRespinData = data; //Store the original winning spin to play after the extra generated losing spin
				JSON fakeSpin = data.getJSON("pet_respin"); //Losing spin
				data = fakeSpin;
				petRespinData.jsonDict.Remove("pet_respin"); //Remove this so its not read again when we parse the original spin
				sendOutcomeToEngine(data);
			}
			else if (hasPetBonusData && !didPetRespin)
			{
				//show the pet overlay
				petBonus = data.getLong("pet_bonus", 0);
#if !ZYNGA_PRODUCTION
				if (DevGUIMenuVirtualPets.bonusPayoutOverride > 0)
				{
					petBonus = DevGUIMenuVirtualPets.bonusPayoutOverride;
				}
#endif
				StartCoroutine(showPetBonusIntroForGamesWithPersistentData(data));
			}
			else
			{
				sendOutcomeToEngine(data);
			}
		}
	}

	private void sendOutcomeToEngine(JSON data)
	{
		this.setOutcome(data);
			
		this.setEngineOutcome(_outcome);

#if ZYNGA_TRAMP
			AutomatedPlayer.spinReceived();
#endif
	}
	
	/// We want to validate the symbols and change the tier when we set the outcome for the base game.
	public override void setOutcome(SlotOutcome outcome)
	{
		base.setOutcome(outcome);
		// Perform some basic validations on the outcome:
		long oldWager = outcome.getWager();

		long wagerForSpin = currentWager;

		if ((oldWager != 0) && (oldWager != wagerForSpin))
		{
			Debug.LogError("Wager amount desync detected! The old wager detected was " + oldWager + " against the bet amount of " + wagerForSpin);
		}

		string reelSetName = _outcome.getReelSet();
		// change the reel set if the tier changes
		if (!string.IsNullOrEmpty(reelSetName) && reelSetName != currentReelSetName)
		{
			setReelSet(_outcome.getReelSet());
		}
	}
	
	/// Tries to set the reel set data based on the given key.
	protected override void handleSetReelSet(string reelSetKey)
	{
		base.handleSetReelSet(reelSetKey);
		resetSlotMessage();
	}
	
	public void setEngineOutcome(SlotOutcome outcome)
	{
		engine.setOutcome(_outcome);
	}
	
	/// Sets the message area text.
	public virtual void setMessageText(string message)
	{
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.setMessageText(message);
		}
	}

	/// reevaluationReelStoppedCallback - called when all reels stop, only on reevaluated spins
	protected override void reevaluationReelStoppedCallback()
	{
		StartCoroutine(doStickySymbolsValidateAndStop());
	}

	/// Handle sticky symbols and validation after the sticky symbols are applied
	private IEnumerator doStickySymbolsValidateAndStop()
	{
		if (currentReevaluationSpin != null)
		{
			yield return StartCoroutine(handleReevaluationStickySymbols(currentReevaluationSpin));
			// We need to do a bit of weirdness here because this is actually validating the symbols after the mutaitons happen.
			// *** I think *** -Leo
			int reelArrayLength = engine.getReelArray().Length;
			for (int reelID = 0; reelID < reelArrayLength; reelID++)
			{
				foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(reelID))
				{
					if (symbol != null)
					{
						symbol.debugName = symbol.serverName;
					}
				}
			}
			// Validate data now that the sticky symbols have been swapped over
			if (currentReevaluationSpin != null)
			{
				engine.validateVisibleSymbolsAgainstData(currentReevaluationSpin.getReevaluatedSymbolMatrix());
			}
			else
			{
				Debug.LogError("SlotBaseGame.doStickySymbolsValidateAndStop() - currentReevaluationSpin became null before this function finished, we should try and figure out how this happens.");
			}
		}

		StartCoroutine(handleReevaluationReelStop());
	}
	
	// handleNormalReelStop() - Called when all reels have come to a stop and engine animations, symbol exapansions have completed. 
	public override IEnumerator handleNormalReelStop()
	{
		if (_outcome == null)
		{
			if (GameState.game != null && GameState.game.keyName != null)
			{
				Debug.LogError("SlotBaseGame::handleNormalReelStop doesn't have an outcome for " + GameState.game.keyName + " autospins = " + autoSpins);

			}
			else
			{
				Debug.LogError("SlotBaseGame::handleNormalReelStop doesn't have an outcome for " + "ERR" + " autospins = " + autoSpins);
			}
		}
		if (GameState.game != null && GameState.game.keyName != null)
		{
			Bugsnag.LeaveBreadcrumb(GameState.game.keyName + ": SlotBaseGame::reelsStoppedCallback. _outcome.isBonus: " + _outcome.isBonus + " reelStops: " + _outcome.printReelStops() + " autospins: " + autoSpins);
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("ERR" + ": SlotBaseGame::reelsStoppedCallback. _outcome.isBonus: " + _outcome.isBonus + " reelStops: " + _outcome.printReelStops() + " autospins:" + autoSpins);
			Debug.LogError("Inside reelsStoppedCallback, but no game state is defined.");
		}

		// Stop the mechanical spin music.
		if (shouldPlayAndStopMusicOnEachSpin)
		{
			Audio.switchMusicKeyImmediate("", 0.0f);
		}

		yield return StartCoroutine(attemptBonusCreation());

		StartCoroutine(base.handleNormalReelStop());

#if ZYNGA_TRAMP
		AutomatedPlayer.spinFinished();
#endif
	}

	// Handles an attempted bonus creation, either by module or by calling the createBonus() function
	private IEnumerator attemptBonusCreation()
	{
		layeredBonusOutcomes = new List<SlotOutcome>();
		List<SlotOutcome> layeredOutcomes = _outcome.getReevaluationSubOutcomesByLayer();
		foreach (SlotOutcome subOutcome in layeredOutcomes)
		{
			if (subOutcome.isBonus)
			{
				layeredBonusOutcomes.Add(subOutcome);
			}
		}

		if (_outcome.isBonus || layeredBonusOutcomes.Count > 0)
		{
			if (isRoyalRushGame && tokenBar != null && tokenBar as RoyalRushCollectionModule != null)
			{
				(tokenBar as RoyalRushCollectionModule).pauseTimers();
			}

			if (!playFreespinsInBasegame || !_outcome.isGifting)
			{
				// This will be called a second time if we are delaying the bonus until
				// after the base wins are paid out
				if (!isPayingBasegameWinsBeforeBonusGames() || isExecutingBonusAfterPayout)
				{
					bool shouldModuleCreateBonus = false;
					foreach (SlotModule module in cachedAttachedSlotModules)
					{
						if (module.needsToLetModuleCreateBonusGame())
						{
							shouldModuleCreateBonus = true;
						}
					}

					// handle the pre bonus created modules, needed for some transitions
					//Don't handle the pre bonus here if we want to create a portal first. 
					if (banners.Length <= 0)
					{
						foreach (SlotModule module in cachedAttachedSlotModules)
						{
							if (module.needsToExecuteOnPreBonusGameCreated())
							{
								yield return StartCoroutine(module.executeOnPreBonusGameCreated());
							}
						}
					}

					if (!shouldModuleCreateBonus)
					{
						bool isUsingBannerPortalScript = banners != null && banners.Length > 0;
						// ignore games which are MultiSlotBaseGames as they create bonuses in their own flow (because they might have to create more than one bonus per reels stopping)
						// also ignore portal script since that also creates the bonus itself, except for credits which will still be created before it is shown
						if (!(this is MultiSlotBaseGame) && !isUsingBannerPortalScript)
						{
							createBonus();
						}
					}
				}
			}
		}
	}
	
	// Secondary version of createBonus which can take an outcome,
	// needed for a few cases where PortalScript want to create a
	// nested bonus instead of the root one.
	public void createBonus(SlotOutcome bonusOutcome, bool isIgnoringPortal = false)
	{
		BonusGameManager.instance.currentMultiplier = relativeMultiplier;
		BonusGameManager.currentBaseGame = SlotBaseGame.instance;

		if (bonusOutcome == null)
		{
			bonusOutcome = getBonusOutcome();
		}

		if (bonusOutcome != null && bonusOutcome.isCredit && bonusOutcome.winAmount != 0)
		{
			// We don't go into a bonus game from the lucky ladies credit result.
			return;
		}

		// if this is a bonus game presenter portal, then terminate it in a way to chain it into the next game
		if (BonusGamePresenter.instance != null && outcome.isPortal && !isIgnoringPortal)
		{
			BonusGamePresenter.instance.endBonusGameImmediately();
		}

		if (bonusOutcome.isPortal && !isIgnoringPortal)
		{
			BonusGameManager.instance.create(BonusGameType.PORTAL);
		}
		else if (bonusOutcome.isChallenge)
		{
			BonusGameManager.instance.create(BonusGameType.CHALLENGE);
		}
		else if (bonusOutcome.isGifting)
		{
			BonusGameManager.instance.create(BonusGameType.GIFTING);
		}
		else if (bonusOutcome.isCredit)
		{
			BonusGameManager.instance.create(BonusGameType.CREDIT);
		}
		else if (bonusOutcome.isScatter)
		{
			BonusGameManager.instance.create(BonusGameType.SCATTER);
		}
	}

	public void createBonus(bool isIgnoringPortal = false)
	{
		createBonus(null, isIgnoringPortal);
	}

	public void startBonus()
	{

		setMessageText(Localize.text("bonus!"));

		// If there are banners available, let's do a banner portal. If not, cut right to the bonus.
		if (banners != null && banners.Length > 0)
		{
			engine.stopAllAnimations();
			portal = gameObject.GetComponent(typeof(PortalScript)) as PortalScript; 
			if (portal == null)
			{
				portal = gameObject.AddComponent(typeof(PortalScript)) as PortalScript;
			}

			//disable stop button
			SpinPanel.instance.stopButton.isEnabled = false;
			
			portal.beginPortal(bannerRoots, banners, bannerTextOverlay, _outcome, relativeMultiplier);
		}
		else
		{
			goIntoBonus();
		}
	}

	public virtual void setupBonusOutcome(SlotOutcome bonusOutcome)
	{
	}

	public virtual SlotOutcome getBonusOutcome(bool shouldRemoveLayeredOutcome = false)
	{
		SlotOutcome bonusOutcome = null;

		SlotOutcome outcomeToCheck = getCurrentOutcome();
		
		// @todo : Add module hook here for returning a bonus outcome to read instead of doing the default 
		// of reading directly from the current outcome being used (for games like got01 where 
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToUseOverridenBonusOutcome())
			{
				outcomeToCheck = module.getOverridenBonusOutcome();
			}
		}

		if (outcomeToCheck.isBonus && !(isBaseBonusOutcomeProcessed && layeredBonusOutcomes.Count > 0))
		{
			bonusOutcome = outcomeToCheck;
			isBaseBonusOutcomeProcessed = true;
		}
		else
		{
			Debug.LogError("Bonus game processing is broken --- isBonus: " + outcomeToCheck.isBonus + " --- outcomeProcessed: " + isBaseBonusOutcomeProcessed);
		}
		return bonusOutcome;
	}

	// Starts up the BonusGameMannager.
	public virtual void goIntoBonus()
	{
		SlotOutcome bonusOutcome = getBonusOutcome(true);

		if (bonusOutcome.isCredit && bonusOutcome.winAmount != 0)
		{
			// We don't go into a bonus game from the lucky ladies credit result.
			
			// if we are in a bonus game presenter portal then close the portal which will handle the remaining outcomes, otherwise handle them here
			if (BonusGamePresenter.instance != null)
			{
				BonusGamePresenter.instance.gameEnded();
			}
			else
			{
				BonusGameManager.instance.finalPayout = bonusOutcome.winAmount * relativeMultiplier;

				doSpecialOnBonusGameEnd();
				doShowNonBonusOutcomes();
			}

			return;
		}

		if (bonusOutcome.isPortal)
		{
			BonusGameManager.instance.show(bonusOutcome);
		}
		else
		{
			if (isDoingFreespinsInBasegame())
			{
				// if a bonus triggers when we are doing freespins in base, we need to stack the bonus so the freespins will be restored
				BonusGameManager.instance.showStackedBonus(isHidingSpinPanelOnPopStack:false);
			}
			else
			{
				BonusGameManager.instance.show();
			}
		}
	}

	protected virtual void doSpecialOnBonusGameStart()
	{
	}
	
	public virtual void doSpecialOnBonusGameEnd()
	{
		playBgMusic();

		// Make sure all of the visible symbols are not playing any aniamtions.
		List<SlotSymbol> allVisibleSymbols = engine.getAllVisibleSymbols();
		for (int i = 0; i < allVisibleSymbols.Count; i++)
		{
			SlotSymbol symbol = allVisibleSymbols[i];

			if (symbol != null)
			{
				symbol.haltAnimation(true);
			}
		}

		if (BonusGameManager.instance != null)
		{
			// We only want to reset the Override if we're actually ending the game and going into the basegame.
			// And if we're not in the middle of freespins.
			BonusGameManager.instance.betMultiplierOverride = -1;
		}
		
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnDoSpecialOnBonusGameEnd())
			{
				module.executeOnDoSpecialOnBonusGameEnd();
			}
		}
	}

	/// Called when a bonus game ends, usually used to clean up a transition
	public override void onBonusGameEnded()
	{
		if (SpinPanel.instance.isShowingCollectionOverlay && tokenBar != null)
		{
			StartCoroutine(tokenBar.setTokenState());
		}
		StartCoroutine(onBonusGameEndedCorroutine());
	}

	// if we were in freespins in base then we need to restore the original outcome so we can trigger additional bonuses if they are queued to go after the freespins
	public void restoreOutcomeFromBeforeFreespinsInBase()
	{
		if (outcomeBeforeFreespinsInBase != null)
		{
			_outcome = outcomeBeforeFreespinsInBase;
			outcomeBeforeFreespinsInBase = null;
		}
	}

	/// Coroutine used for handling cleanup when a bonus game ends, always call this base method so that modules can execute correctly!
	protected virtual IEnumerator onBonusGameEndedCorroutine()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnBonusGameEnded())
			{
				yield return StartCoroutine(module.executeOnBonusGameEnded());
			}
		}
	}
	
	public virtual IEnumerator doPlayBonusAcquiredEffects()
	{
		bool isModuleOverridingBonusAcquiredEffects = false;	
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecutePlayBonusAcquiredEffectsOverride())
			{
				yield return StartCoroutine(module.executePlayBonusAcquiredEffectsOverride());
				isModuleOverridingBonusAcquiredEffects = true;
			}
		}

		if (!isModuleOverridingBonusAcquiredEffects)
		{
			// module isn't handling this, so allow the game to trigger it normally
			yield return StartCoroutine(playBonusAcquiredEffects());
		}
	}

	/**
	Function to play the bonus acquired effects (bonus symbol animations and an audio
	appluase for getting the bonus), can be overridden to handle games that need or
	want to handle this bonus transition differently
	*/
	public virtual IEnumerator playBonusAcquiredEffects()
	{
		yield return StartCoroutine(engine.playBonusAcquiredEffects());
		isBonusOutcomePlayed = true;
	}

	// Present the bonus contained in the current outcome to the player.  Note
	// attemptBonusCreation() should have been called before this is called.
	protected IEnumerator presentCurrentBonusOutcome()
	{
		bool shouldModuleCreateBonus = false;
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			//Let the module create the bonus if there isn't going to be a portal created by the banners
			//Need the banners check to prevent the portal from being skipped and us going straight to the transition.
			if (module.needsToLetModuleCreateBonusGame() && banners.Length == 0)
			{
				shouldModuleCreateBonus = true;
			}
		}

		// handle bonus outcome here if it hasn't been done early
		if (!isBonusOutcomePlayed)
		{
			// handle playing the BN symbol animations and the bonus acquired sound before starting the bonus entry stuff
			yield return StartCoroutine(doPlayBonusAcquiredEffects());
		}

		doSpecialOnBonusGameStart();
		// mark the bonus outcome has not played for the next spin
		isBonusOutcomePlayed = false;
		if (!shouldModuleCreateBonus)
		{
			startBonus();
		}
	}

	protected override IEnumerator doReelsStopped(bool isAllowingContinueWhenReadyToEndSpin = true)
	{
		if (_outcome == null)
		{
			Debug.LogError("Trying to stop the reels with a null outcome");
			yield break;
		}
		
		int subOutcomeCount = getSubOutcomeCount();
		isInTheMiddleOfAnAutoSpinSpin = false;
		layeredBonusOutcomes = new List<SlotOutcome>();
		List<SlotOutcome> layeredOutcomes = _outcome.getReevaluationSubOutcomesByLayer();

		foreach (SlotOutcome subOutcome in layeredOutcomes)
		{
			if (subOutcome.isBonus)
			{
				layeredBonusOutcomes.Add(subOutcome);
			}
		}

		if (subOutcomeCount > 0 && !skipPaylines)
		{
			//lets do those overlays
			yield return RoutineRunner.instance.StartCoroutine(doOverlay());
		}
		else
		{
			yield return StartCoroutine(waitForModulesAfterPaylines(false));
		}

		if (hasReevaluationSpinsRemaining)
		{
			// handle outcomes before and during reevaluation spins
			if (subOutcomeCount > 0 && !skipPaylines)
			{
				showNonBonusOutcomes();
			}
			else
			{
				StartCoroutine(continueWhenReady());
			}
		}
		else
		{
			if (hasFreespinsSpinsRemaining)
			{
				// need to handle bonus games here if they can trigger when we are doing freespins in base
				if (_outcome.isBonus && currentReevaluationSpin == null)
				{
					if (BonusGameManager.instance != null)
					{
						BonusGameManager.instance.finalPayout = 0;
					}

					yield return StartCoroutine(presentCurrentBonusOutcome());
				}
				else
				{
					if (subOutcomeCount > 0 && !skipPaylines)
					{
						showNonBonusOutcomes();
					}
					else
					{
						if (isDoingFreespinsInBasegame())
						{
							startNextFreespin();
						}
					}
				}
			}
			else
			{
				setStickyOverlaysVisible(false);
				// In tumble games, once we come back from a bonus game, we need to stop at the final 
				// tumble outcome (in which case hasReevaluationSpins is false and currentReevaluation spins is not null)
				// instead of trying to go into a bonus game again
				if (_outcome.isBonus && currentReevaluationSpin == null)
				{
					if (!playFreespinsInBasegame || !_outcome.isGifting)
					{
						// Check if we are delaying a bonus until the base game pays out
						if (isPayingBasegameWinsBeforeBonusGames())
						{
							bool hasNonBonusSubOutcomes = false;
							ReadOnlyCollection<SlotOutcome> subOutcomes = _outcome.getSubOutcomesReadOnly();
							foreach (SlotOutcome subOutcome in subOutcomes)
							{
								if (subOutcome.getOutcomeType() != SlotOutcome.OutcomeTypeEnum.BONUS_GAME)
								{
									hasNonBonusSubOutcomes = true;
									break;
								}
							}
							
							// Flag that we should execute the bonus when the payouts are done
							isExecutingBonusAfterPayout = true;

							// Make sure that we actually have something to payout, if we don't just head
							// straight to continueWhenReady() which should launch the delayed bonus
							if (hasNonBonusSubOutcomes)
							{
								showNonBonusOutcomes();
							}
							else
							{
								StartCoroutine(continueWhenReady());
							}
						}
						else
						{
							yield return StartCoroutine(presentCurrentBonusOutcome());
						}
					}
					else
					{
						showNonBonusOutcomes();
						if (hasFreespinGameStarted && _outcome.getSubOutcomesReadOnly().Count == 0)
						{
							StartCoroutine(continueWhenReady());
						}
					}
				}
				else if (subOutcomeCount > 0 && !skipPaylines)
				{
					showNonBonusOutcomes();
				}
				else
				{
					// if the big win is animating don't unlock everything yet, the big win finishing should unlock things
					if (!isBigWinBlocking)
					{
						if (isAllowingContinueWhenReadyToEndSpin)
						{
							StartCoroutine(continueWhenReady());
						}
					}
				}
			}			
		}
	}
	
	public void showFreespinsEndDialog(long payout, GenericDelegate callback)
	{
		//This sets up the Values the dialog needs				
		BonusGameManager.instance.currentGameType = BonusGameType.GIFTING;
		BonusGameManager.instance.currentGameKey = GameState.game.keyName;
		BonusGameManager.instance.finalPayout = payout;
		BonusGameManager.instance.currentGameFinalPayout = payout;
			
		//Show the bonus game summary
		BonusSummary.handlePresentation(
			Dict.create(
				D.CALLBACK, new DialogBase.AnswerDelegate((noArgs) => { callback(); })
			)
		);
	}

	private void basegameFreespinsSummaryClosed()
	{
		StartCoroutine(basegameFreespinsSummaryClosedCoroutine(GameState.giftedBonus != null));	
	}

	private IEnumerator basegameFreespinsSummaryClosedCoroutine(bool isGiftChestGiftedSpins)
	{
		hasFreespinGameStarted = false;
		isFirstSpin = true;

		// Make sure something was actually won before awarding the credits so we don't end up awarding zero credits
		// which will produce an error message.
		if (BonusGameManager.instance.finalPayout > 0)
		{
			addCreditsToSlotsPlayer(BonusGameManager.instance.finalPayout, "freespins in base payout", shouldPlayCreditsRollupSound: false);
		}

		SpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGameManager.instance.finalPayout);
		
		if (!isGiftChestGiftedSpins)
		{
			if (bigWinEffect != null)
			{
				bigWinEffect.setAmount(BonusGameManager.instance.finalPayout);
			}
		}

		long finalPayout = BonusGameManager.instance.finalPayout;
		BonusGameManager.instance.finalPayout = 0;

		_outcomeDisplayController.clearOutcome();		

		lastPayoutRollupValue = 0;
		runningPayoutRollupValue = 0;

		if (!isGiftChestGiftedSpins)
		{
			//Reset the paytable
			string payTableKey = engine.gameData.basePayTable;
			engine.setFreespinsPaytableKey(payTableKey);
			engine.switchToBaseGame();
			PayTable paytable = PayTable.find(payTableKey);
			_outcomeDisplayController.payTable = paytable;
		}

		// cleanup the BonusGamePresenter before telling BonusGameManager the bonus is over
		freespinsInBasegamePresenter.isGameActive = false;
		if (BonusGamePresenter.instance == freespinsInBasegamePresenter)
		{
			BonusGamePresenter.instance = null;
		}

		BonusGameManager.instance.bonusGameEnded();

		if (!isGiftChestGiftedSpins)
		{
			playBgMusic();

			//Trigger modules before sliding spin panels back to base game
			for (int i = 0; i < cachedAttachedSlotModules.Count; i++) 
			{
				SlotModule module = cachedAttachedSlotModules[i];

				if (module.needsToExecuteOnReturnToBasegameFreespins())
				{
					yield return StartCoroutine(module.executeOnReturnToBasegameFreespins());
				}
			}

			// restore the spin panel, but ensure the buttons are disabled
			SpinPanel.instance.setButtons(false);
			SpinPanel.instance.resetAutoSpinUI();
			yield return StartCoroutine(SpinPanel.instance.swapSpinPanels(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Right, 0.5f, false));

			if (isOverBigWinThreshold(finalPayout))
			{
				// add just a tiny delay here, because otherwise the big win loading overlaps ever so slightly with the spin panel slide and causes a weird pop
				yield return new TIWaitForSeconds(0.1f);

				// grab the specific rollup time from a module, otherwise just calculate it
				float rollupTime = getSpecificRollupTimeForPayout(finalPayout);
				if (rollupTime < 0.0f)
				{
					rollupTime = Mathf.Ceil((float)((double)finalPayout / betAmount)) * Glb.ROLLUP_MULTIPLIER;
				}

				// Block on the big win, and don't run the continue block when done, since we'll continue once
				// everything here is done.
				yield return StartCoroutine(forceTriggerBigWin(finalPayout, rollupTime, false));
			}

			if (!_outcome.hasQueuedBonuses)
			{
				if ((GameState.giftedBonus != null) && (freespinsInBasegamePresenter != null))
				{
					// This will return us to the lobby/inbox after accepting a gift and completing the free spins.
					freespinsInBasegamePresenter.summaryClosed(); 
				}
				else if (SlotsPlayer.isSocialFriendsEnabled &&
						 GameState.giftedBonus == null &&
						 BonusGameManager.instance.isGiftable &&
				         MFSDialog.shouldSurfaceSendSpins())
				{
					// This bonus game was from a base game spin,
					// so offer the player the opportunity to gift it
					// or to challenge friends.
					if (BonusGameManager.instance.currentGameType == BonusGameType.GIFTING && finalPayout > 0)
					{
						MFSDialog.showDialog(
							Dict.create(
								D.GAME_KEY, GameState.game.keyName,
								D.BONUS_GAME, BonusGameManager.instance.summaryScreenGameName,
								D.TYPE, MFSDialog.Mode.SPINS
							), SchedulerPriority.PriorityType.LOW
						);
					}
				}
			}

			// because freespins in base doesn't use the BonusGamePresenter finalCleanup() function we need to perform the queued bonus outcome update here
			restoreOutcomeFromBeforeFreespinsInBase();

			// make sure we remove the flags on that previous outcome so the game doesn't think it needs to trigger the freespins again
			_outcome.isGifting = false;

			// if bonuses are queued up, then remove the current one we just finished, and check if we have another to load in to be played next
			if (_outcome.hasQueuedBonuses)
			{
				_outcome.removeBonusFromQueue();
				// check if we have another bonus after the one we just finished
				if (_outcome.hasQueuedBonuses)
				{
					_outcome.processNextBonusInQueue();
				}
			}

			if (_outcome.hasQueuedBonuses)
			{
				// make sure this is reset so that the next bonus game in the queued will be able to trigger bonus acquired effects
				isBonusOutcomePlayed = false;

				yield return StartCoroutine(triggerQueuedBonusGames());
				// check if we have a freespins in base that was queued to trigger
				yield return StartCoroutine(triggerFreespinsInBase());
			}
			
			yield return StartCoroutine(doSpecialWins(SpecialWinSurfacing.POST_NORMAL_OUTCOMES));

			// Now that we are fully done with everything, we can mark the spin complete so things can continue
			isSpinComplete = true;
		}
		else
		{
			// handle getting out of the gift chest gifted spins

			// Make sure the overlay is visible - check to ensure we aren't returning from a nested bonus round (aruze03)
			if (!hasFreespinsSpinsRemaining)
			{
				Overlay.instance.top.show(true);				
			}
			

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

	// Forces a big win to be displayed with a static value for a set duration (this duration can be skipped by tapping)
	public IEnumerator forceTriggerBigWinWithStaticValueForSetDuration(long payout, float durationToShowBigWin, bool isDoingContinueWhenReady)
	{
		// First create the big win and force the value to be the final payout amount
		onBigWinNotification(payout, true);

		if (bigWinEffect != null)
		{
			// Now instead of doing a rollup, we are just going to wait a fixed duration (which can be skipped by tapping)
			float elapsedTime = 0;
			while (elapsedTime < durationToShowBigWin)
			{
				yield return null;
				elapsedTime += Time.deltaTime;
				if (TouchInput.didTap)
				{
					elapsedTime = durationToShowBigWin;
				}
			}

			// Force the big win to end, since we aren't doing a rollup that would normally cause it to end
			yield return StartCoroutine(forceEndBigWinEffect(isDoingContinueWhenReady));

			// Wait for the Big Win to be destroyed, since if we don't it will actually block the game from proceeding
			while (bigWinEffect != null)
			{
				yield return null;
			}
		}
	}

	// This can be used to force the triggering of a big win
	// NOTE : Be careful with when/how you call this as it could cause strange stuff as big wins stop interaction with the game
	public IEnumerator forceTriggerBigWin(long payout, float specificRollupTime = 0.0f, bool isAllowingContinueWhenReady = true)
	{
		// Check if the big win is being delayed and will most likely be triggered by a module
		if (areModulesDelayingBigWin)
		{
			// a module is saying to keep delaying the big win
			yield break;
		}

		// Clear these out since to do the big win we'll have to re-rollup everything from 0
		lastPayoutRollupValue = 0;
		runningPayoutRollupValue = 0;

		onBigWinNotification(payout);

		// in order to update the value on the big win we have to do a rollup
		yield return StartCoroutine(SlotUtils.rollup(0, payout, onBigWinOnlyRollup, playSound: true, specificRollupTime: specificRollupTime, shouldSkipOnTouch: true, shouldBigWin: true));
		yield return StartCoroutine(ReelGame.activeGame.onEndRollup(isAllowingContinueWhenReady: isAllowingContinueWhenReady, isAddingRollupToRunningPayout: true));

		// Only wait for this if we are going to unlock later
		if (!isAllowingContinueWhenReady)
		{
			// Wait until the big win effect is gone before proceeding so that the game can unlock normally
			// since the big win can block allowing the game to unlock
			while (isBigWinBlocking)
			{
				yield return null;
			}
		}
	}
	
	// Function that will get what the key for the current PJP tier the player is playing for is
	// NOTE : For now this is based on a commonly used PJP module, however in the future it might make
	// sense to have this stored as a string and actually have modules update it when it is changed. This
	// would then remove the need to search for a module here to extract the value from.
	public string getCurrentProgressiveJackpotKey()
	{
		string progJackpotKey = "";
		foreach (SlotModule cacheduleModule in cachedAttachedSlotModules)
		{
			BuiltInProgressiveJackpotBaseGameModule module = cacheduleModule as BuiltInProgressiveJackpotBaseGameModule;
			if (module != null)
			{
				progJackpotKey = module.getCurrentJackpotTierKey();
			}
		}

		return progJackpotKey;
	}

	protected virtual void resetSlotMessage()
	{
		if (engine.reelSetData == null)
		{
			// If the reel set data hasn't been defined yet, then don't do anything.
			// This can happen if setting the multiplier before the game is finished setting up.
			return;
		}

		if ( GameState.game == null )
		{
			Debug.Log( "Game state is null." );
			return;  // if we don't return we will crash below
		}

		setMessageText(Localize.text("good_luck"));
	}
	
	// Check if this game is using an override for the passed symbol name to be used in the paytable
	public string getPaytableSymbolName(string symbolName)
	{
		string finalSymbolName = symbolName;
		
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToOverridePaytableSymbolName(symbolName))
			{
				finalSymbolName = module.getOverridePaytableSymbolName(symbolName);
			}
		}

		return finalSymbolName;
	}

	/**
	Grab a new non-pooled instance of a 3D leveled symbol which can be used by the paytable, 
	derived games will have to handle how to setup the symbol in such a way 
	that it has the correct appearance
	*/
	public virtual GameObject get3dSymbolInstanceForPaytableAtLevel(string symbolName, int symbolLevel, bool isUsingSymbolCaching)
	{
		// by default going to ignore the level and just grab the base symbol

		if (symbolLevel > 1)
		{
			Debug.LogError("Trying to get symbolLevel greater than 1 using the default non-override function.  This isn't going to give you a leveled up symbol, you need to override!");
		}

		SymbolInfo info = findSymbolInfo(symbolName);

		if ((info != null))
		{
			if (info.symbol3d != null)
			{
				if (isUsingSymbolCaching)
				{
					// check if this symbol was already created for the paytable
					if (!cached3dPaytableSymbols.ContainsKey(symbolName))
					{
						// no symbol created yet, handle 3D reel symbol creation and then cache it
						GameObject reel3dSymbol = CommonGameObject.instantiate(info.symbol3d) as GameObject;
						cached3dPaytableSymbols.Add(symbolName, reel3dSymbol);
					}

					cached3dPaytableSymbols[symbolName].SetActive(true);

					return cached3dPaytableSymbols[symbolName];
				}
				else
				{
					// no symbol caching being used, so just create a new symbol and pass it back
					return CommonGameObject.instantiate(info.symbol3d) as GameObject;
				}
			}
			else
			{
				// only handling 3D symbols here, will need to make a 2D function for 2D leveled symbols
				Debug.LogError("SymbolInfo doesn't symbol3d for symbol: " + symbolName);
				return null;
			}
		}
		else
		{
			Debug.LogError("SymbolInfo was null for PayTable symbol: " + symbolName);
			return null;
		}
	}

	/**
	Reset the symbols on the reels as the paytable closes, since it
	may have altered some shared materials which need to be put back
	to the state they were when the paytable was opened
	*/
	public virtual void resetSymbolsOnPaytableClose()
	{
		// Override to handle what may need to happen here
	}
	
	public void doShowNonBonusOutcomes()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecutePreShowNonBonusOutcomes())
			{
				module.executePreShowNonBonusOutcomes();
			}
		}
		
		showNonBonusOutcomes();	
	}
	
	/// Shows the normal outcome wins.
	/// This either happens immediately if there is no bonus game,
	/// or after returning from the bonus game.
	public virtual void showNonBonusOutcomes()
	{
		// hide the sticky overlays during pay box stuff
		setStickyOverlaysVisible(false);
		
		// Detect if already paid out the winnings and go straight to continueWhenReady()
		if (isNonBonusWinningsAlreadyPaid)
		{
			isNonBonusWinningsAlreadyPaid = false;
			StartCoroutine(continueWhenReady());
			return;
		}

		if (currentReevaluationSpin != null)
		{
			if (currentReevaluationSpin.getSubOutcomesReadOnly().Count == 0)
			{
				bool isMarkingSpinComplete = true;
				foreach (SlotModule module in cachedAttachedSlotModules)
				{
					// Modules can block marking a spin complete here if they are going to handle doing
					// that in a different way.  Got01 is a game that does this because it triggers
					// bonus games via a reevaluation feature.
					isMarkingSpinComplete &= module.isAllowingShowNonBonusOutcomesToSetIsSpinComplete();
				}

				if (isMarkingSpinComplete)
				{
					// Nothing to show, so end the spin right now
					isSpinComplete = true;
				}

				return;
			}
			_outcomeDisplayController.displayOutcome(currentReevaluationSpin, hasAutoSpinsRemaining || hasReevaluationSpinsRemaining);
		}
		else
		{
			// unlock the game if we don't have anything won on this outcome, but if we are doing freespins in base 
			// we still need to process the outcome because we can't just unlock the game and since it could contain 
			// a bonus triggered in the freespins (like in aruze02 Extreme Dragon)
			// determine if bonus games were played via bonus games nested inside of reevaluations
			bool isOutcomeWithReevaluationBonuses = _outcome.isOutcomeWithReevaluationBonusGames();
			
			if (_outcome.getSubOutcomesReadOnly().Count == 0 && !isOutcomeWithReevaluationBonuses && _outcome.getReevaluationSubOutcomesByLayer().Count == 0 && !hasFreespinGameStarted)
			{
				// Nothing to show, so end the spin right now
				isSpinComplete = true;
				return;
			}
			_outcomeDisplayController.displayOutcome(_outcome, hasAutoSpinsRemaining || hasReevaluationSpinsRemaining || hasFreespinsSpinsRemaining);
		}
	}
	
	protected override void onOutcomeSpinBlockRelease()
	{
		StartCoroutine(continueWhenReady());
	}
	
	// startNextAutospin - starts a spin and decrements that auto-spin counter.
	protected override void startNextAutospin()
	{
		InGameFeatureContainer.onStartNextAutoSpin();
		StartCoroutine(startNextAutospinCoroutine());
	}

	// coroutine for starting the next autospin that will wait until the animationCount is 0 before proceeding
	private IEnumerator startNextAutospinCoroutine()
	{
		if (!hasAutoSpinsRemaining)
		{
			Debug.LogError("SlotBaseGame.startNextAutospinCoroutine() - Called when hasAutoSpinsRemaining was FALSE!");
		}

		if (isVipRevampGame && tokenBar != null)
		{
			tokenBar.spinPressed();
		}

		if (isInTheMiddleOfAnAutoSpinSpin)
		{
			Debug.LogError("Trying to starting a new auto spin while already in the middle of one!");
		}

		while (!Glb.isNothingHappening || engine.animationCount != 0 || engine.effectInProgress)
		{
			yield return null;
		}

		if (hasAutoSpinsRemaining && !isInTheMiddleOfAnAutoSpinSpin)
		{
			if (Glb.spinTransactionInProgress)
			{
				Glb.endSpinTransaction();
			}

			StatsManager.Instance.LogCount(
				counterName : "game_actions",
				kingdom : "spin",
				phylum : "hir_spin_panel_v2",
				klass : GameState.game.keyName,
				family : "autospin",
				genus: SpinPanel.instance.selectedAutoSpin,
				val : betAmount
			);
			
			// Allows testing of forced outcomes during autospins
			if (queuedForcedOutcome != null)
			{
				isInTheMiddleOfAnAutoSpinSpin = true;
				forceOutcome(queuedForcedOutcome);	
				queuedForcedOutcome = null;
			}
			else if (validateSpin() && (autoSpins > 0 || autoSpins == -1))
			{

				// If TRAMP is active, mark this spin as an autospin.
#if ZYNGA_TRAMP
				AutomatedPlayer.autospinRequested();
#endif

				isInTheMiddleOfAnAutoSpinSpin = true; //Setting this flag if we've started an auto-spin to prevent multiple spins from trying to happen
				// Only decrement if not in infinite mode (-1),
				// and only if the spin validation passed and started a spin.
				if (autoSpins > 0)
				{
					autoSpins--;
				}
			}
		}
		else
		{
			// Got into an odd state, with the most likely scenario that you've run out of coins. Let's just re-enable the UI to be safe
			if (Glb.spinTransactionInProgress)
			{
				Glb.endSpinTransaction();
			}
			Overlay.instance.setButtons(true);
		}
	}

	/// Hides all of the objects in the SlotBaseGame, and all of the paylines. (can pass in bigWinAnimation which isn't going to be hidden)
	public void hideGame(GameObject bigWinAnimation = null)
	{
		hiddenChildren = CommonGameObject.findDirectChildren(SlotBaseGame.instance.gameObject, true);
		List<GameObject> objectsToRemove = new List<GameObject>();
		foreach (GameObject go in hiddenChildren)
		{
			if (go != null)
			{
				if (go.activeSelf)
				{
					if (go != bigWinAnimation)
					{
						go.SetActive(false);
					}
					else
					{
						// Found a big win animation which should just stay visible
						objectsToRemove.Add(go);
					}
				}
				else
				{
					// If it wasn't active before then we should keep it that way.
					objectsToRemove.Add(go);
				}
			}
		}
		foreach (GameObject go in objectsToRemove)
		{
			hiddenChildren.Remove(go);
		}
		_outcomeDisplayController.hideAllPaylines();
	}

	/// Unhides all of the objects in SlotBaseGame, and unhides the paylines, optionally ignores the hidden children check
	/// which is used when we've "fake hidden" a game and just need to notify the cachedAttachedSlotModules to pay attention again
	public void showGame(bool ignoreHiddenChildrenCheck = false)
	{
		// If hideGame hasn't been called then we don't want to do anything.
		if (!ignoreHiddenChildrenCheck)
		{
			if (hiddenChildren == null)
			{
				Debug.LogError("There were no children to restore. Aborting.");
				return;
			}

			foreach (GameObject go in hiddenChildren)
			{
				if (go != null)
				{
					go.SetActive(true);
				}
			}
		}

		_outcomeDisplayController.showAllPaylines();
		hiddenChildren = null; // No need to hold onto these.

		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.needsToExecuteOnShowSlotBaseGame())
			{
				module.executeOnShowSlotBaseGame();
			}
		}
	}
	
	/// Each game can override this function to do custom cheat stuff based on the touched key.
	/// Overrides should also call base.touchKey() to make sure this general functionality preserved.
	public virtual void touchKey(KeyCode keyCode)
	{
#if WEB_GL || !ZYNGA_PRODUCTION
		if (!Data.debugMode || !(Glb.dataUrl.Contains("dev") || Glb.dataUrl.Contains("stag") || Glb.dataUrl.Contains("vii") || Glb.dataUrl.Contains("192.168.63.129")))
		{
			return;
		}

		switch (keyCode)
		{
			case KeyCode.C:
				forceBonus();
				DevGUI.isActive = false;
				break;

			case KeyCode.G:
				forceFreeSpin();
				DevGUI.isActive = false;
				break;

			case KeyCode.Y:
				forceOtherBonus();
				DevGUI.isActive = false;
				break;

			case KeyCode.B:
				forceBigWin();
				DevGUI.isActive = false;
				break;
				
			case KeyCode.M:
				forceMutation();
				DevGUI.isActive = false;
				break;

			case KeyCode.Semicolon:
				mysteryGiftForceOutcome(0);
				break;
				
			case KeyCode.Quote:
				mysteryGiftForceOutcome(1);
				break;

			case KeyCode.Comma:
				mysteryGiftForceOutcome(2);
				break;

			case KeyCode.Period:
				mysteryGiftForceOutcome(3);
				break;

			case KeyCode.W:
				forceSecondaryMutation();
				DevGUI.isActive = false;
				break;
				
			case KeyCode.U:
				forceScatterBonus();
				DevGUI.isActive = false;
				break;
				
			case KeyCode.I:
				forceSecondaryScatterBonus();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.Q:
				forceFeatureQ();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.R:
				forceFeatureR();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.E:
				forceFeatureE();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.T:
				forceFeatureT();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.S:
				forceFeatureS();
				DevGUI.isActive = false;
				break;
			
			case KeyCode.N:
				forceFtueBigWin();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha0:
			case KeyCode.Keypad0:
				forceFeature0();
				DevGUI.isActive = false;
				break;
				
			case KeyCode.Alpha1:
			case KeyCode.Keypad1:
				forceFeature1();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha2:
			case KeyCode.Keypad2:
				forceFeature2();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha3:
			case KeyCode.Keypad3:
				forceFeature3();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha4:
			case KeyCode.Keypad4:
				forceFeature4();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha5:
			case KeyCode.Keypad5:
				forceFeature5();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha6:
			case KeyCode.Keypad6:
				forceFeature6();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha7:
			case KeyCode.Keypad7:
				forceFeature7();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha8:
			case KeyCode.Keypad8:
				forceFeature8();
				DevGUI.isActive = false;
				break;

			case KeyCode.Alpha9:
			case KeyCode.Keypad9:
				forceFeature9();
				DevGUI.isActive = false;
				break;
		}
#endif
	}
	
	// If the game has mystery gift data (including big slice),
	// force an outcome if one is found in the given index.
	private void mysteryGiftForceOutcome(int dataIndex)
	{
		if (GameState.game.mysteryGiftType == MysteryGiftType.NONE)
		{
			return;
		}
		
		if (slotGameData == null || slotGameData.mysteryGiftForcedOutcomeData.Count < dataIndex + 1)
		{
			return;
		}
		
		int id = slotGameData.mysteryGiftForcedOutcomeData[dataIndex].getInt("id", 0);
		if (id == 0)
		{
			return;
		}
		
		SlotAction.forceMysteryGiftWin = id;
		validateSpin();
		DevGUI.isActive = false;
	}

	// Register persistent event delegates.
	public static void registerEventDelegates()
	{
		Server.registerEventDelegate("game_locked", gameLockedEventCallback, true);	
		Server.registerEventDelegate("slot_maintenance_block", slotMaintenanceEventCallback, true);
		Server.registerEventDelegate("big_win", onBigWin, true);
	}

	// Server event callback for when a game is locked in an attempted spin.
	public static void gameLockedEventCallback(JSON data)
	{
		Glb.failSpinTransaction("Spun game is locked.");
		// Refund the wager amount on the client, since the spin didn't actually do anything.
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.addCreditsToSlotsPlayer(instance.betAmount, "wager refund", shouldPlayCreditsRollupSound: true, isIncrementingRunningPayoutRollupAlreadyPaidOut: false);
		}

		// Cleanup the outcome callback since we aren't going to be recieving one
		Server.unregisterEventDelegate("slots_outcome");
				
		// Force the player back to the lobby after notifying with a dialog.
		// This dialog won't show until after Scheduler.run() is called below,
		// due to the Glb.isNothingHappening check.
		PreviewExpiredDialog.showDialog();

		instance.isPerformingSpin = false;
		instance.isSpinComplete = false;

		Scheduler.run();
	}

	// Server event callback for when a game is locked in an attempted spin.
	public static void slotMaintenanceEventCallback(JSON data)
	{
		Glb.failSpinTransaction("Spun game is under maintenance.");

		// Refund the wager amount on the client, since the spin didn't actually do anything.
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.addCreditsToSlotsPlayer(instance.betAmount, "wager refund", shouldPlayCreditsRollupSound: true, isIncrementingRunningPayoutRollupAlreadyPaidOut: false);
		}

		// Cleanup the outcome callback since we aren't going to be recieving one
		Server.unregisterEventDelegate("slots_outcome");

		// Force the player back to the lobby after notifying with a dialog.
		// This dialog won't show until after Scheduler.run() is called below,
		// due to the Glb.isNothingHappening check.

		//Generic Dialog notifying the player that the game is under maintenance. Will maybe have localized text later on?
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, "Game Maintenance",
				D.MESSAGE, Data.liveData.getString("SLOT_MAINTENANCE_MSG", "GAME IS UNDER MAINTENANCE. PLEASE RETURN TO THE LOBBY"),
				D.REASON, "slot-base-game-maintenance",
				D.CALLBACK, new DialogBase.AnswerDelegate((args) => 
					{ 
						GameState.pop();
						Loading.show(Loading.LoadingTransactionTarget.LOBBY); 
						Glb.loadLobby(); 
					})
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);

		instance.isPerformingSpin = false;
		instance.isSpinComplete = false;

		Scheduler.run();
	}


	/// <summary>
	///   Server is now sending "big_win" event, with an associated hash. We are going to set the hash aside
	///   so we can attach in the big win dialog later for CS verifications
	/// </summary>
	public static void onBigWin(JSON data)
	{
		lastBigWinHash = data.getString("event", "");
	}
	
	public bool checkForcedOutcomes(string keycode)
	{
		return getForcedOutcome(keycode) != null;
	}

	public bool hasFakeServerMessageInForcedOutcomes(string keycode)
	{
		ForcedOutcome outcome = getForcedOutcome(keycode);
		if (outcome != null && outcome.fakeServerMessage != null)
		{
			return true;
		}
		return false;
	}

	public bool isUsingServerCheatForForcedOutcome(string keycode)
	{
		ForcedOutcome outcome = getForcedOutcome(keycode);
		if (outcome != null && outcome.hasServerCheatKey && outcome.isUsingServerCheat)
		{
			return true;
		}
		return false;
	}

	public ForcedOutcome getForcedOutcome(string keycode)
	{
		ForcedOutcome outcome = null;
		switch (keycode) 
		{
		case "G":
			if (freeSpinForcedOutcomes != null && freeSpinForcedOutcomes.Count > 0 && freeSpinForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = freeSpinForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "C":
			if (challengeForcedOutcomes != null && challengeForcedOutcomes.Count > 0 && challengeForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = challengeForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "Y":
			if (otherBonusForcedOutcomes != null && otherBonusForcedOutcomes.Count > 0 && otherBonusForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = otherBonusForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "U":
			if (scatterBonusForcedOutcomes != null && scatterBonusForcedOutcomes.Count > 0 && scatterBonusForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = scatterBonusForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "I":
			if (secondaryScatterBonusForcedOutcomes != null && secondaryScatterBonusForcedOutcomes.Count > 0 && secondaryScatterBonusForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = secondaryScatterBonusForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "M":
			if (mutationForcedOutcomes != null && mutationForcedOutcomes.Count > 0 && mutationForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = mutationForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "W":
			if (secondaryMutationForcedOutcomes != null && secondaryMutationForcedOutcomes.Count > 0 && secondaryMutationForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = secondaryMutationForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "B":
			if (bigWinForcedOutcomes != null && bigWinForcedOutcomes.Count > 0 && bigWinForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = bigWinForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "0":
			if (feature0ForcedOutcomes != null && feature0ForcedOutcomes.Count > 0 && feature0ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature0ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "1":
			if (feature1ForcedOutcomes != null && feature1ForcedOutcomes.Count > 0 && feature1ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature1ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "2":
			if (feature2ForcedOutcomes != null && feature2ForcedOutcomes.Count > 0 && feature2ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature2ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "3":
			if (feature3ForcedOutcomes != null && feature3ForcedOutcomes.Count > 0 && feature3ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature3ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "4":
			if (feature4ForcedOutcomes != null && feature4ForcedOutcomes.Count > 0 && feature4ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature4ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "5":
			if (feature5ForcedOutcomes != null && feature5ForcedOutcomes.Count > 0 && feature5ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature5ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "6":
			if (feature6ForcedOutcomes != null && feature6ForcedOutcomes.Count > 0 && feature6ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature6ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "7":
			if (feature7ForcedOutcomes != null && feature7ForcedOutcomes.Count > 0 && feature7ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature7ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "8":
			if (feature8ForcedOutcomes != null && feature8ForcedOutcomes.Count > 0 && feature8ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature8ForcedOutcomes[GameState.game.keyName];
			}
			break;

		case "9":
			if (feature9ForcedOutcomes != null && feature9ForcedOutcomes.Count > 0 && feature9ForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = feature9ForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "Q":
			if (featureQForcedOutcomes != null && featureQForcedOutcomes.Count > 0 && featureQForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = featureQForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "R":
			if (featureRForcedOutcomes != null && featureRForcedOutcomes.Count > 0 && featureRForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = featureRForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "E":
			if (featureEForcedOutcomes != null && featureEForcedOutcomes.Count > 0 && featureEForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = featureEForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "T":
			if (featureTForcedOutcomes != null && featureTForcedOutcomes.Count > 0 && featureTForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = featureTForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "S":
			if (featureSForcedOutcomes != null && featureSForcedOutcomes.Count > 0 && featureSForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = featureSForcedOutcomes[GameState.game.keyName];
			}
			break;
		
		case "N":
			if (ftueBigWinForcedOutcomes != null && ftueBigWinForcedOutcomes.Count > 0 && ftueBigWinForcedOutcomes.ContainsKey(GameState.game.keyName))
			{
				outcome = ftueBigWinForcedOutcomes[GameState.game.keyName];
			}
			break;
		}
		return outcome;
	}

	public bool hasActiveFeatureMiniGame()
	{
		//Conditionals can be added for feature triggered mini games in the future
		return isVipRevampGame || isMaxVoltageGame;
	}

	// Function to control when spin timeout is enabled or disabled
	protected void setIsCheckingSpinTimeout(bool isCheckingTimeout)
	{
		if (isCheckingTimeout)
		{
			isCheckingSpinTimeout = true;
		}
		else
		{
			isCheckingSpinTimeout = false;
			spinTimeoutTimer = 0;
		}
	}

	/// Private data structure for forcing outcomes.
	[System.Serializable]
	public class ForcedOutcome
	{
		public int tierId;
		public int[] outcomes;
		public ForcedLayerOutcomeInfo[] focedLayerOutcomeInfos;
		public ForcedIndependentReelInfo[] forcedIndependentReelInfos;
		public TextAsset fakeServerMessage;

		// Data for building outcome via symbol names
		public bool isUsingSymbolNames = false; // Tells if outcome should be built using symbol names
		public string[] symbolNames;					// Symbol names to build from
		public string reelSetName;						// Reelset to search for the passed symbol names

		public bool isUsingServerCheat = false;	// If a server cheat came down this will be enabled to be toggled so you can pick if you want to use a custom or the server cheat
		public string serverCheatKey = null; 	// Value to store what the cheat key that this forced outcome uses, if a server cheat key exists
		public string serverCheatName = null;   // Value to display on the button that either replaces or is next to the key name
		public int spinTestRunCount = 1;
		
		public ForcedOutcome()
		{}

		// Old method.
		public ForcedOutcome(int tierId, int[] outcomes)
		{
			updateForcedOutcome(tierId, outcomes);
		}

		/// Shared by old way of doing forced outcomes, and building them via symbol names
		private void updateForcedOutcome(int tierId, int[] outcomes)
		{
			this.tierId = tierId;
			this.outcomes = outcomes;

			// if this was being built from symbol names it no longer is and is fully built, so turn this flag off
			isUsingSymbolNames = false;
		}

		// Old method using symbol names.
		public ForcedOutcome(int tierId, string[] symbolNames, string reelSetName)
		{
			this.tierId = tierId;
			this.symbolNames = symbolNames;
			this.reelSetName = reelSetName;
			isUsingSymbolNames = true;

			buildForcedOutcomeUsingSymbolNames();
		}

		// New method.
		public ForcedOutcome(int tierId, ForcedLayerOutcomeInfo[] focedLayerOutcomeInfos)
		{
			this.tierId = tierId;
			this.focedLayerOutcomeInfos = focedLayerOutcomeInfos;
		}

		// Method for independent reels
		public ForcedOutcome(int tierId, ForcedIndependentReelInfo[] forcedIndependentReelInfos)
		{
			this.tierId = tierId;
			this.forcedIndependentReelInfos = forcedIndependentReelInfos;
		}

		// Old + New method.
		public ForcedOutcome(int tierId, int[] outcomes, ForcedLayerOutcomeInfo[] focedLayerOutcomeInfos)
		{
			this.tierId = tierId;
			this.outcomes = outcomes;
			this.focedLayerOutcomeInfos = focedLayerOutcomeInfos;
		}

		public Dictionary<string, Dictionary<string, string>> getLayerToReelToStopDict()
		{
			Dictionary<string, Dictionary<string, string>> layerToReelToStop = new Dictionary<string, Dictionary<string, string>>();
			foreach (ForcedLayerOutcomeInfo info in focedLayerOutcomeInfos)
			{
				layerToReelToStop.Add(info.layer, info.reelToStops.dictionary);
			}
			return layerToReelToStop;
		}

		public Dictionary<string, int>[] getIndependentEndData()
		{
			Dictionary<string, int>[] data = new Dictionary<string, int>[forcedIndependentReelInfos.Length];
			for (int i = 0; i < forcedIndependentReelInfos.Length; i++)
			{
				Dictionary<string, int> dataForStop = new Dictionary<string, int>();
				dataForStop.Add("stop", forcedIndependentReelInfos[i].stop);
				dataForStop.Add("reel", forcedIndependentReelInfos[i].reel);
				dataForStop.Add("position", forcedIndependentReelInfos[i].position);
				data[i] = dataForStop;
			}
			return data;
		}

		// helper function for creating ForcedOutcomes without having to look up reel stops
		public void buildForcedOutcomeUsingSymbolNames()
		{
			if (!isUsingSymbolNames)
			{
				Debug.LogError("buildForcedOutcomeUsingSymbolNames() - Trying to build a forced outcome that didn't say it was using symbol names!");
				return;
			}

			ReelSetData reelSetData = null;
			if (SlotBaseGame.instance != null && SlotBaseGame.instance.engine != null)
			{
				reelSetData = SlotBaseGame.instance.engine.gameData.findReelSet(reelSetName);
			}

			if (reelSetData == null)
			{
				Debug.LogError("buildForcedOutcomeUsingSymbolNames() - Couldn't find data for reelSetName: " + reelSetName);
				return;
			}

			int symbolIndex = 0;
			int[] reelStops = new int[symbolNames.Length];
			bool wasSymbolFound = false;
			for (int reelSetIndex = 0; reelSetIndex < reelSetData.reelDataList.Count; reelSetIndex++)
			{
				ReelData reelData = reelSetData.reelDataList[reelSetIndex];
				wasSymbolFound = false;

				for (int i = 0; i < reelData.reelStrip.symbols.Length; i++)
				{
					if (symbolNames[symbolIndex] == reelData.reelStrip.symbols[i])
					{
						reelStops[symbolIndex] = i;
						symbolIndex++;
						wasSymbolFound = true;
						break;
					}
				}

				if (!wasSymbolFound)
				{
					Debug.LogError("buildForcedOutcomeUsingSymbolNames() - Couldn't find symbol: " + symbolNames[symbolIndex] + " for reel: " + reelSetIndex + " of reelSetName: " + reelSetName);
					return;
				}
			}

			updateForcedOutcome(tierId, reelStops);
		}

		/// Code to handle the case where we need to update the data 
		public void convertForcedLayerOutcomeInfoDictionariesToSerializedLists()
		{
			if (focedLayerOutcomeInfos != null)
			{
				foreach (ForcedLayerOutcomeInfo layerOutcomeInfo in focedLayerOutcomeInfos)
				{
					layerOutcomeInfo.convertDictionaryToSerializedList();
				}
			}
		}

		// Checks if a fake server message is currently dragged in.
		// This can conflict with forced outcomes or cause mismatches if forgotten.
		public bool isUsingFakeServerMessage()
		{
			return fakeServerMessage != null;
		}

		// Adds a server cheat key to this outcome, note that the cheat key and forcedOutcomeType
		// should ideally match, this should be the case for now when it is called from
		// ForcedOutcomeRegistrationModule
		public void addServerCheatKey(string serverCheatKey, string serverCheatName, int spinTestRunCount)
		{
			this.isUsingServerCheat = true;
			this.serverCheatName = serverCheatName;
			this.serverCheatKey = serverCheatKey;
			this.spinTestRunCount = spinTestRunCount;
		}

		// Tell if this forced outcome has a server cheat key which can be used.
		// Whether it is actually used or not will depend on the variable isUsingServerCheat.
		public bool hasServerCheatKey
		{
			get { return !string.IsNullOrEmpty(serverCheatKey); }
		}

		// Ensures that server cheat key info is cleared on the off chance that someone
		// managed to save the prefab while the game was running with server cheat info
		// loaded.  (Since the server cheat info only comes down from the server while
		// the game is running)
		public void clearServerCheatInfo()
		{
			this.isUsingServerCheat = false;
			this.serverCheatKey = null;
			this.serverCheatName = null;
		}

		// Checks if there are any forced outcomes for this game.
		// These can very easily conflict with fake server messages.
		public bool isUsingForcedOutcomes()
		{

			if (outcomes != null && outcomes.Length > 0)
			{
				return true;
			}

			if (focedLayerOutcomeInfos != null && focedLayerOutcomeInfos.Length > 0)
			{
				return true;
			}

			if (forcedIndependentReelInfos != null && forcedIndependentReelInfos.Length > 0)
			{
				return true;
			}

			return false;
		}
	}

	[System.Serializable]
	public class ForcedLayerOutcomeInfo
	{
		public string layer;
		[SerializeField] public CommonDataStructures.SerializableDictionaryOfStringToString reelToStops;

		public ForcedLayerOutcomeInfo(int layer, Dictionary<int, int>  reelToStops)
		{
			this.layer = layer.ToString();
			CommonDataStructures.SerializableDictionaryOfStringToString reelToStopsString = new CommonDataStructures.SerializableDictionaryOfStringToString();
			foreach (KeyValuePair<int, int> kvp in reelToStops)
			{
				reelToStopsString.Add(kvp.Key.ToString(), kvp.Value.ToString());
			}
			this.reelToStops = reelToStopsString;
		}

		/// convert the dictionary to a serialized list, needed when the game isn't playing and doesn't do this automatically,
		/// because the automatic function goes crazy and gets called WAY too often in the editor when in edit mode
		public void convertDictionaryToSerializedList()
		{
			reelToStops.convertDictionaryToSerializedList();
		}
	}

	[System.Serializable]
	public class ForcedIndependentReelInfo
	{
		public int stop;
		public int reel;
		public int position;

		public ForcedIndependentReelInfo(int stop, int reel, int position)
		{
			this.stop = stop;
			this.reel = reel;
			this.position = position;
		}
	}

	/// Serialized class for handling forced outcomes
	[System.Serializable]
	public class SerializedForcedOutcomeData : ISerializationCallbackReceiver
	{
		public ForcedOutcomeTypeEnum forcedOutcomeType;		// The type the forced outcome will be bound to
		public ForcedOutcome forcedOutcome;					// The forced outcome
		[Tooltip("Increase this value if TRAMP has to test a forced outcome more than once in order to trigger a bonus/feature.")]
		public int spinTestRunCount = 1;		// A value TRAMP uses to determine how many times it needs to force the same key in order to trigger the bonus/feature.  For games like wonka04 where you collect pips to trigger feature/bonus.
		public bool isIgnoredByTramp;						// TRAMP is having some issues dealing with forced outcomes that are purely for testing features, like ones that generate near infinite spins, so we should allow TRAMP to ignore forced outcomes that can cause issues
		public string note;									// User specified note about what this outcome does

		public SerializedForcedOutcomeData(ForcedOutcomeTypeEnum forcedOutcomeType, ForcedOutcome forcedOutcome, int spinTestRunCount)
		{
			this.forcedOutcomeType = forcedOutcomeType;
			this.forcedOutcome = forcedOutcome;
			this.spinTestRunCount = spinTestRunCount;
		}

		// Since I had to add OnAfterDeserialize() to handle the unity issue, I'll add this
		// as well to make sure user error doesn't cause 0 or a negative number to be input
		// for spinTestRunCount
		public void OnBeforeSerialize()
		{
			if (spinTestRunCount < 0)
			{
				spinTestRunCount = 0;
			}
		}

		// Due to following Unity issue: https://issuetracker.unity3d.com/issues/wrong-default-value-for-array-elements-added-in-inspector
		// spinTestRunCount is not using the default value assigned above because we are using it in an array/list so
		// we will just force it to be 1 unless it has been defined to be a larger number
		public void OnAfterDeserialize()
		{
			if (spinTestRunCount < 0)
			{
				spinTestRunCount = 0;
			}
		}

		public SerializedForcedOutcomeData(string serverCheatKey, string serverCheatName, int spinTestRunCount)
		{
			this.forcedOutcomeType = convertServerCheatKeyToForcedOutcomeTypeEnum(serverCheatKey);
			this.forcedOutcome = new ForcedOutcome();
			addServerCheatKey(serverCheatKey, serverCheatName, spinTestRunCount);
		}

		// Ensures that server cheat key info is cleared on the off chance that someone
		// managed to save the prefab while the game was running with server cheat info
		// loaded.  (Since the server cheat info only comes down from the server while
		// the game is running)
		public void clearServerCheatInfo()
		{
			forcedOutcome.clearServerCheatInfo();
		}

		// Adds a server cheat key to this outcome, note that the cheat key and forcedOutcomeType
		// should ideally match, this should be the case for now when it is called from
		// ForcedOutcomeRegistrationModule
		public void addServerCheatKey(string serverCheatKey, string serverCheatName, int spinTestRunCount)
		{
			forcedOutcome.addServerCheatKey(serverCheatKey, serverCheatName, spinTestRunCount);
		}

		// Get the key code for this specific forced outcome (uses the static version)
		public string getKeyCodeForForcedOutcomeType(bool isForTramp = false)
		{
			return SerializedForcedOutcomeData.getKeyCodeForForcedOutcomeType(forcedOutcomeType, isForTramp);
		}

		// Tell if this forced outcome has a server cheat key which can be used.
		// Whether it is actually used or not will depend on the variable isUsingServerCheat.
		public bool hasServerCheatKey
		{
			get { return forcedOutcome.hasServerCheatKey; }
		}

		public static ForcedOutcomeTypeEnum convertServerCheatKeyToForcedOutcomeTypeEnum(string serverCheatKey)
		{
			serverCheatKey = serverCheatKey.ToLower();
			switch (serverCheatKey)
			{
				case "g":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FREESPIN_FORCED_OUTCOME;
				case "c":
					return SlotBaseGame.ForcedOutcomeTypeEnum.CHALLENGE_FORCED_OUTCOME;
				case "m":
					return SlotBaseGame.ForcedOutcomeTypeEnum.MUTATION_FORCED_OUTCOME;
				case "b":
					return SlotBaseGame.ForcedOutcomeTypeEnum.BIG_WIN_FORCED_OUTCOME;
				case "u":
					return SlotBaseGame.ForcedOutcomeTypeEnum.SCATTER_BONUS_FORCED_OUTCOME;
				case "y":
					return SlotBaseGame.ForcedOutcomeTypeEnum.OTHER_BONUS_FORCED_OUTCOME;
				case "i":
					return SlotBaseGame.ForcedOutcomeTypeEnum.SECONDARY_SCATTER_BONUS_FORCED_OUTCOME;
				case "w":
					return SlotBaseGame.ForcedOutcomeTypeEnum.SECONDARY_MUTATION_FORCED_OUTCOME;
				case "0":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_0_FORCED_OUTCOME;
				case "1":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_1_FORCED_OUTCOME;
				case "2":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_2_FORCED_OUTCOME;
				case "3":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_3_FORCED_OUTCOME;
				case "4":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_4_FORCED_OUTCOME;
				case "5":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_5_FORCED_OUTCOME;
				case "6":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_6_FORCED_OUTCOME;
				case "7":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_7_FORCED_OUTCOME;
				case "8":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_8_FORCED_OUTCOME;
				case "9":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_9_FORCED_OUTCOME;
				case "q":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_Q_FORCED_OUTCOME;
				case "r":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_R_FORCED_OUTCOME;
				case "e":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_E_FORCED_OUTCOME;
				case "t":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_T_FORCED_OUTCOME;
				case "s":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_S_FORCED_OUTCOME;
				case "n":
					return SlotBaseGame.ForcedOutcomeTypeEnum.FTUE_BIG_WIN_FORCED_OUTCOME;
				default:
					Debug.LogError("SlotBaseGame.SerializedForcedOutcomeData.convertServerCheatKeyToForcedOutcomeTypeEnum() - Don't know how to convert serverCheatKey = " + serverCheatKey);
					return SlotBaseGame.ForcedOutcomeTypeEnum.UNDEFINED;
			}
		}

		public static string getKeyCodeForForcedOutcomeType(ForcedOutcomeTypeEnum forcedOutcomeType, bool isForTramp = false)
		{
			switch (forcedOutcomeType)
			{
				case SlotBaseGame.ForcedOutcomeTypeEnum.FREESPIN_FORCED_OUTCOME:
					return "g";
				case SlotBaseGame.ForcedOutcomeTypeEnum.CHALLENGE_FORCED_OUTCOME:
					return "c";
				case SlotBaseGame.ForcedOutcomeTypeEnum.MUTATION_FORCED_OUTCOME:
					return "m";
				case SlotBaseGame.ForcedOutcomeTypeEnum.BIG_WIN_FORCED_OUTCOME:
					return "b";
				case SlotBaseGame.ForcedOutcomeTypeEnum.SCATTER_BONUS_FORCED_OUTCOME:
					return "u";
				case SlotBaseGame.ForcedOutcomeTypeEnum.OTHER_BONUS_FORCED_OUTCOME:
					return "y";
				case SlotBaseGame.ForcedOutcomeTypeEnum.SECONDARY_SCATTER_BONUS_FORCED_OUTCOME:
					return "i";
				case SlotBaseGame.ForcedOutcomeTypeEnum.SECONDARY_MUTATION_FORCED_OUTCOME:
					return "w";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_0_FORCED_OUTCOME:
					return "0";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_1_FORCED_OUTCOME:
					return "1";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_2_FORCED_OUTCOME:
					return "2";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_3_FORCED_OUTCOME:
					return "3";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_4_FORCED_OUTCOME:
					return "4";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_5_FORCED_OUTCOME:
					return "5";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_6_FORCED_OUTCOME:
					return "6";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_7_FORCED_OUTCOME:
					return "7";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_8_FORCED_OUTCOME:
					return "8";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_9_FORCED_OUTCOME:
					return "9";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_Q_FORCED_OUTCOME:
					return "q";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_R_FORCED_OUTCOME:
					return "r";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_E_FORCED_OUTCOME:
					return "e";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_T_FORCED_OUTCOME:
					return "t";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FEATURE_S_FORCED_OUTCOME:
					return "s";
				case SlotBaseGame.ForcedOutcomeTypeEnum.FTUE_BIG_WIN_FORCED_OUTCOME:
					return "n";
				default:
					if (isForTramp)
					{
						// return a spin for TRAMP and then TRAMP will log info about the failed forced outcome
						return "spin";
					}
					else
					{
						Debug.LogWarning("SerializedForcedOutcomeData.getKeyCodeForForcedOutcomeType() - Unknown forcedOutcomeType: " + forcedOutcomeType + "; don't know keycode so returning empty string!");
						return "";
					}
			}
		}
	}
}
