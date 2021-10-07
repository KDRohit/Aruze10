using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using Zynga.Unity.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ReelSetup))]
public class ReelGame : TICoroutineMonoBehaviour
{
	protected const int WINDOW_HANDLE_ID = 42;

	// Safety timeout to ensure the game doesn't get stuck in loading if the background doesn't
	// update during the loading phase.  If this occurs we will stop waiting and just load the
	// game, in editor we will log an error that will get picked up by TRAMP/ZAP so that someone
	// can look at and investigate why a game needed to rely on the timeout. (Something like this
	// could happen if for instance the ReelGameBackground object was disabled for some reason when
	// the game started).
	protected const float REEL_GAME_BACKGROUND_SCALING_COMPLETE_TIMEOUT = 8.0f;
	// Used in the same spot as above, but used to make sure that ReelGameBackground has run an Update
	// loop.  This can be delayed due to frame spiking when a freespin game is initially created, which
	// can cause a significant time gap before the game is able to call Update() after being created.
	protected const float REEL_GAME_BACKGROUND_UPDATE_DELAY_TIMEOUT = 3.0f;

	// Inspector variables
	[System.NonSerialized] public bool testGUI = false;                             // Enable the test panel of Unity GUI
															/*TODO: This should be removed once wonka01 stops using it*/
	[HideInInspector] public bool isNewCentering = false;                       // Use the new viewport scaling/centering technique on this game.
	[HideInInspector] public LayerMask layersToOmitNewCenteringOn = 0;

	[Tooltip("Some games don't get the correct debug symbol info to be validated, in these cases we usually just turn off the validation to avoid mismatch errors")]
	[SerializeField] private bool _isValidatingWithServerDebugSymbols = true;
	public bool isValidatingWithServerDebugSymbols
	{
		get { return _isValidatingWithServerDebugSymbols; }
	}

	// Always play the spin music when you start a spin, and always stop the spin music as soon as all the reels have stopped spinning.
	// Otherwise, it controls the spin music by changing its volume, and it fades the music out.
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public bool shouldPlayAndStopMusicOnEachSpin = false;

	[System.NonSerialized] public bool isLegacyPlopGame = false;        // If true, initialize as a 'plop' game, not a 'spin' game.
	[System.NonSerialized] public bool isLegacyTumbleGame = false;
	[HideInInspector] public JSON[] modifierExports;
	[HideInInspector] public JSON[] reelInfo;
	[HideInInspector] public bool skipPaylines = false;
	public Dictionary<int, string> freeSpinInitialReelSet = null;           // Used for sliding slot freespin games.
	protected JSON reelSetDataJson = null;
	[HideInInspector] public List<SlotModule> cachedAttachedSlotModules = new List<SlotModule>();       // SlotModules cached for later usage (public for use by portals)
	private GameObject symbolCacheGameObject = null;
	[HideInInspector] public GameObject activePaylinesGameObject = null;
	[OmitFromNonDebugInspector][SerializeField] private float activePaylinesGameObjectZOffset = 0.0f; // offset that allows the paylines to be pushed forward in order for them to be in front of other objects like symbols
	[HideInInspector] public GameObject gameScaler = null;

	[OmitFromNonDebugInspector] public Vector2 payBoxSize = new Vector2(1.8f, 1.8f);    // The size of the box used for paylines, clusters, etc.
	[OmitFromNonDebugInspector] public float paylineScaler = 1.0f;                      // Scales the paylines to different sizes
	public bool drawPaylines = true;
	public bool drawPayBoxes = true;
	[HideInInspector] public Vector2 startingPayBoxSize;
	[HideInInspector] public float startingSymbolVerticalSpacingWorld;
	[HideInInspector] public Vector2 reelBoxPadding = new Vector2();            // The space between the reels (taken all together as a single block) and the slot frame.
	[HideInInspector] public float spaceBetweenReels = 0f;                  // The space between reels, it's also the width of a reel divider.
	public bool showSideInfo = true;
	public bool isUsingMysterySymbolOverlay = false;
	public bool persistingMajorSymbols = false; // The overlay expanding symbol persists beyond the animation.
	public bool skipReelStopIntervalsForBlankReels = true; // Set to not wait the delay interval for a reel stop when all symbols are blank

	// In certain situations, free spins won't end until specific json tells it to end. In that scenario, we just spin till we can't.
	public bool endlessMode = false;

	// You only have to play the spin music once (ignore subsequent plays because it's already playing).
	// Note that in SlotBaseGame, it fades the volume out until you press spin again, then it maxes-out the volume (and fades it out again).
	protected bool shouldPlaySpinMusic = true;

	public bool hasMutationsInReeval = false;
	[System.NonSerialized] public TokenCollectionModule tokenBar = null;

	[HideInInspector] public long mutationCreditsAwarded = 0; // tracks mutation credits that should be awarded on this spin, some games have additional win values attached to mutaitons and these are hnadled with this

	[System.NonSerialized] public bool showPaylineCascade = true;
	[System.NonSerialized] public long jackpotWinToPayOut = 0;

	[HideInInspector] public string BASE_GAME_BG_MUSIC_KEY = "prespin_idle_loop";
	[HideInInspector] public string BASE_GAME_SPIN_MUSIC_KEY = "reelspin_base";

	protected bool isPerformingSpin = false;
	protected bool _isSpinComplete = false;

	protected bool isSpinComplete
	{
		get { return _isSpinComplete; }
		set
		{
			_isSpinComplete = value;

			if (_isSpinComplete && !isPerformingSpin)
			{
				Debug.LogError("ReelGame.isSpinComplete - Spin set to complete but game didn't think it was spinning, this needs to be fixed!");
#if !ZYNGA_TRAMP
				Debug.Break();
#endif
			}
		}
	}

	private const string BASE_GAME_INTRO_VO_KEY = "";
	private const float BASE_GAME_MUSIC_IDLE_TIME = 10.0f;
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public string PERSISTING_MAJOR_SOUND_KEY = "basegame_vertical_wild_reveal";

	private const string FREE_SPINS_BG_MUSIC_KEY = "freespin";
	private const string FREE_SPINS_INTRO_MUSIC_KEY = "freespinintro";
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	[SerializeField] private float FREE_SPINS_IN_BASE_MUSIC_DELAY = 0.0f;
	private const string FREE_SPINS_INTRO_VO_KEY = "freespin_intro_vo";

	private const string SPIN_REEL_RESPIN_SOUND_KEY = "spin_reel_respin";
	private const string SPIN_REEL_RESPIN_FREESPIN_SOUND_KEY = "spin_reel_respin_freespin";
	private const string FREESPIN_FIRST_SPIN_SOUND = "spin_reel_freespin";
	private const string FREESPIN_ALREADY_SPIN_SOUND = "spin_reel_already_freespin";

	// In marilyn02 scatter symbols SC1..10 should not play an anticipation sound. This happens when they are on a reel
	// with a BN symbol that has anticipation. Only 'SC' symbol should play its scatter_symbol_fanfare when it lands.
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public bool playSoundsOnlyOnSCScatterSymbols = false;

	// Get one of the reelRoots using an ArrayIndex into it, should really only be needed for ReelSetup stuff, so only while the game isn't running
	protected GameObject getReelRootUsingArrayIndex(int reelRootsArrayIndex)
	{
		if (!Application.isPlaying)
		{
			if (reelRootsArrayIndex >= 0 && reelRootsArrayIndex < reelRoots.Length)
			{
				return reelRoots[reelRootsArrayIndex];
			}
			else
			{
				Debug.LogError("reelRootsArrayIndex will case an index error!");
				return null;
			}
		}
		else
		{
			Debug.LogError("ReelGame.getReelRootUsingArrayIndex() - shouldn't be used while the game is running, it was intended only for use by the ReelSetup script.");
			return null;
		}
	}

	public GameObject getReelRootsAt(int reelID, int row = -1, int layer = -1)
	{
		if (engine == null)
		{
			Debug.LogError("No engine set, unsure how to get reelRoots");
			return null;
		}
		return engine.getReelRootsAt(reelID, row, layer);
	}

	// Same as virtual getReelRootsAtWhileApplicationNotRunning but this exists to allow
	// for games that need to use this for backwards compatible reasons where their
	// data may not exactly match what is expected until the game is run.
	// (See: IndependentReelFreeSpinGame for an example of where it had to be used)
	public GameObject getBasicReelGameReelRootsAtWhileApplicationNotRunning(int reelID)
	{
		if (reelID >= 0 && reelID < reelRoots.Length)
		{
			return reelRoots[reelID];
		}
		else
		{
			Debug.LogError("reelID will case an index error!");
			return null;
		}
	}

	// Special function that will only really be called by the ReelSetup script as a fallback when a ReelEngine doesn't exist
	public virtual GameObject getReelRootsAtWhileApplicationNotRunning(int reelID, int row, int layer, CommonDataStructures.SerializableDictionaryOfIntToIntList independentReelVisibleSymbolSizes)
	{
		return getBasicReelGameReelRootsAtWhileApplicationNotRunning(reelID);
	}

	public int getReelRootsLength()
	{
		if (engine == null)
		{
			Debug.LogError("No engine set, unsure how to get reelRoots.Length");
			return -1;
		}
		return engine.getReelRootsLength();
	}

	[OmitFromNonDebugInspector][SerializeField] private GameObject[] reelRoots;        	// The roots of the reels, in order from left to right
																						// Allows access to reelRoots for Editor purposes only
																						// if the game is running you probably shouldn't be accessing reelRoots
																						// directly since different game types handle them differently


	// Special function made to handle a case where the reel roots need to be read by another class.
	// But we don't want to completely expose reelRoots, so instead I'll make a copy.  In general
	// this should almost not be used, and right now is only used in order for IndependentReelFreeSpinGame
	// that previously didn't have layers to backwards compatibility support them.
	public GameObject[] getReelRootsArrayShallowCopy()
	{
		if (reelRoots != null)
		{
			GameObject[] copy = new GameObject[reelRoots.Length];
			reelRoots.CopyTo(copy, 0);
			return copy;
		}
		else
		{
			return null;
		}
	}
															
	public GameObject[] getReelRootsForEditor()
	{
		if (Application.isPlaying)
		{
			Debug.LogError("ReelGame.getReelRootsForEditor() - This funciton isn't intended to be used when the game is running!");
			return null;
		}
		else
		{
			return reelRoots;
		}
	}

	[OmitFromNonDebugInspector][SerializeField] public List<SymbolInfo> symbolTemplates;                   // The prefab templates of the various symbols
	[SerializeField] public int symbolCacheMax = -1;                            // -1 is uncapped
	[SerializeField] public int symbolCacheMaxLowEndAndroid = -1;           // -1 is uncapped, use this if there needs to be an override if this is a low-end andoird device which might go over on memory
	private SlotSymbolCache slotSymbolCache;
	public bool isProgressive = false;                      // Tags whether or not we should be looking at progressives for this game.
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public bool playSoundsOnExpandingSymbol = false;
	public int progressiveThreshold = 99999999;             // The threshold for our progressive, where if we pass it, we get a jackpot.
	[SerializeField] public Texture2D wildTexture;
	public GameObject wildOverlayGameObject = null;         // GameObject version of the overlaying WILD
	public bool wildHidesSymbol = false;                    // Flag to tell if the wild overlay texture should hide the symbol behind it to prevent strange overlapping
	public bool isWdSymbolUsingWildOverlay = true;          // Some games may not want the wild overlay to play on top of the wild symbol
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool isLayeringOverlappingSymbols = false;       // Flag that controls if overlapping symbols are handled, this ensures that symbols layer top to bottom

	// See this spreadsheet to see how your layers might look
	// https://docs.google.com/spreadsheets/d/1ZwnkVVwLz2fH4eyuuPAn3r7WLOJy5WFNTTK2ojZaQNw/edit#gid=0
	[Tooltip("Controls if the game handles symbol render queues itself")]
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool isHandlingOwnSymbolRenderQueues = false;    // Flag that controls if a game is saying it will handle symbol render queues itself and doesn't want to use the defualt layering code, used by zynga01 for now
	[Tooltip("Controls symbol Z layer by stair step from top to bottom")]
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool isLayeringSymbolsByDepth = false;           // Flag that controls if we need to layer symbols by z-position rather than render queue value
	[Tooltip("Controls symbol Z layer by stair step from left to right")]
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool isLayeringSymbolsByReel = false;            // Flag that controls if we need to stair step the reels from left to right
	[Tooltip("Controls symbol Z layer by adding a cumulative amount to each symbol")]
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool isLayeringSymbolsCumulative = false;        // Flag that controls if we need to layer symbols by adding a cumulative amount to each symbol

	public bool flattenedSymbolsUseSourceLayer = false;     // Flag sets layer to same as source instead of using Layers.ID_SLOT_REELS
															/*TODO: this should probably be removed entirely, but it's hooked up in elvira04 and ani03*/
	[HideInInspector] public UIPanel reelPanel = null;                      // Panels for use with NGUI based symbols
	public bool isPlayingPreWin = true;                     // Controls if a game should play the pre-win sound and choreography
	[Tooltip("Controls if bonus symbols are animated when they are in line/cluster wins in games that have payouts associated with bonus symbols.")]
	public bool isAnimatingBonusSymbolsInWins = false;
	public bool excludeBonusMultiplierInRollup = true;      // If a bonus multiplier is received during a spin, its possible for it to be handled in a choreographed way, or just along with the initial spin. Default to choreographed.
															/*TODO: this is only used for tv02, and can probably be removed entirely if we decide to not port that game*/
	[HideInInspector] public int bufferSymbolCount = -1;                        // Handles how many buffer symbols to use for a game, if -1 then uses default, which is to set it to the height of the tallest symbol template
	public float symbolOverlayStartupTime = 1.5f;           // How long it takes for the overlay symbols in games like duckdyn02, zynga02, gen11 take before they display after the reels stop
	public float symbolOverlayShowTime = 2.5f;              // How long the symbol overlays are displayed for before being hidden, has to at least be long enough for sounds to play

	public float noOutcomeAutospinDelay = 0.0f;             // A delay applied during prespin for autospins with no outcomes, needed in games like pb01 with animations that look crazy if they play too fast between spins
	public bool hasMultipleLinkedReelSets = false; // For games that contain multiple sets of linked reels with different first reels that they're linked to
	[HideInInspector] public List<int> linkedReelStartingReelIndices; // For games with multiple sets of linked (synced) reels. The array contains the first reel for each set of linked reels. 
	[HideInInspector] public float symbolVerticalSpacingWorld = 0.5f;   // The vertical spacing of symbols in world 3d space.	
	[HideInInspector] public SlotEngine engine;
	[HideInInspector] public MutationManager mutationManager = null;
	[HideInInspector] public SlotGameData slotGameData { get; protected set; }
	public virtual long betAmount
	{
		get
		{
			return currentWager;
		}

		protected set
		{
			currentWager = value;
		}
	}
	[HideInInspector] public long currentWager; // Tracks the current wager amount for this game
	[HideInInspector] public SymbolAnimator.AnimatingDelegate symbolAnimatingCallback = null;

	[OmitFromNonDebugInspector][SerializeField] private float symbolVerticalSpacing = 0.5f;                // The vertical spacing of symbols in 3d space local to the reel root.
	public float symbolVerticalSpacingLocal
	{
		get { return symbolVerticalSpacing; }
	}
	[SerializeField] protected bool hasExpandingSymbols = false;                // Symbols should expand when the reels stop (like gen05)
	[SerializeField] private string expandingSymbolSound = "";                  // Sound played as the expanded symbol fades in
	private Dictionary<string, SymbolInfo> _symbolMap = null;
	// @todo : Consider making a way to share the bounds info between base and freespins if possible
	private Dictionary<string, SymbolAnimator.BoundsInfo> _symbolBoundsCache = null;
	[HideInInspector] public int initialWaysLinesNumber = 0;
	public bool isScatterForBonus = false;

	public List<SlotOutcome> reevaluationSpins
	{
		get
		{
			return _reevaluationSpins;
		}
		set
		{
			_reevaluationSpins = value;
		}
	}
	private List<SlotOutcome> _reevaluationSpins = new List<SlotOutcome>();
	public int reevaluationSpinsRemaining // Tracks the number of reevaluated spins remaining
	{
		get
		{
			return _reevaluationSpinsRemaining;
		}
		set
		{
			_reevaluationSpinsRemaining = value;
		}

	}
	private int _reevaluationSpinsRemaining;
	[System.NonSerialized] public long reevaluationSpinMultiplierOverride = -1; // override used for reevaluation spins that don't use the standard multiplier, like a cumulative base game feature like in wonka04
	protected SlotOutcome _currentReevaluationSpin;
	protected Dictionary<int, Dictionary<int, string>> prevHandledReevalStickySymbols = new Dictionary<int, Dictionary<int, string>>(); // Tracks symbosl that are already stuck on the reels
	private List<SlotSymbol> reevalStickySymbols = new List<SlotSymbol>(); // The stuck animators for the sticky symbols
	protected bool hadRevalsLastSpin = false;
	protected GenericDelegate _oldReelStoppedCallback = null;

	[HideInInspector] public float idleTimer;
	[HideInInspector] public bool isSkippingPreWinThisSpin = false;
	[HideInInspector] public bool isSpecialWinActive = false;   // Whether we're currently showing a special win dialog.

	protected const float IDLE_TIME = 5.0f;

	protected string logText = "";
	protected Vector2 logScroll = Vector2.zero;
	protected Rect windowRect = new Rect(100, 100, 350, 500);
	protected Rect dragRect = new Rect(0, 0, 350, 500);

	protected ReelSetData _reelSetData;
	protected string defaultReelSetName = ""; // Store the default reelset name to be reset on each new full spin if it doesn't match the current one
	protected string currentReelSetName = ""; // Track what the current reel set is that we should be using
	protected SlotOutcome _outcome;
	protected OutcomeDisplayController _outcomeDisplayController;
	public StopInfo[][] stopOrder;                                  // Stores the order that we want the reels to stop in. Public because SlotEngine needs it.

	protected const float UNSKIPPABLE_REEVALUTION_SPIN_TIME = 0.25f;    // Need just a little time to make sure that the reels actually appear to spin before youc an stop them
	[SerializeField] protected float REEVALUATION_SPIN_STOP_TIME = 1.25f;           // Simulated time between reevaluation spins starting and stopping
#if UNITY_EDITOR
	private const string SYMBOL_TEMPLATE_PATH2D = "assets/data/common/bundles/initialization/Prefabs/Slots/Symbols/Basic 1x1 Symbol.prefab";
	private const string SYMBOL_TEMPLATE_PATH3D = "assets/data/common/bundles/initialization/Prefabs/Slots/Symbols/Basic 1x1 Symbol 3d.prefab";
#else
	private const string SYMBOL_TEMPLATE_PATH2D = "assets/data/common/bundles/initialization/prefabs/slots/symbols/basic 1x1 symbol.prefab";
	private const string SYMBOL_TEMPLATE_PATH3D = "assets/data/common/bundles/initialization/prefabs/slots/symbols/3d symbols/basic 1x1 symbol 3d.prefab";
#endif
	private GameObject _symbolTemplate = null;

	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public Vector3 anticipationPositionAdjustment;
	public bool allowPositionAdjustmentToBeVectorZero = false; // Controls if an anticipationPositionAdjustment that is Vector3.zero is valid

	[HideInInspector] public List<string> permanentWildReels = new List<string>(); // We need to sometimes ensure every symbol of type X has a wild overlay.

	public bool hasPayboxMutation = false; // do the symbols of this reel game mutate specifically when the paybox is around them? (only zom01 does this for now)
	public bool shouldUseBaseWagerMultiplier = true;
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public ReelGameBackground reelGameBackground = null;

	public bool hasSpotlight = false;                               // Checks if the game has a spotlight feature
	[HideInInspector] public int spotlightReelStartIndex = 0;       // Start index of the reel from where evaluations starts in a spotlight feature (0 based)
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public float bonusSymbolVoDelay = 0.0f; //Delay between the bonus symbol fanfare and VO, if a game has bonus symbol VO. 

	protected long lastPayoutRollupValue = 0; // Stored value of the last payout rollup value, can be used if you need to know this value to continue rolling up from where it was last time, used for features and freespin games
	[System.NonSerialized] protected long runningPayoutRollupValue = 0; // Stored value of all rollups applied to the spin panel winbox, used by free spin games and features
	[System.NonSerialized] protected long runningPayoutRollupAlreadyPaidOut = 0; // Value that is subtracted from runningPayoutRollupValue so that values aren't awarded more than once, for now only used by NewTumbleSlotBaseGame

	//
	// All the viewport scaling stuff required to check and see if the game fits in the view.
	//------------------------------------------------------------------------------------------------------------------------------
	//
	protected const float FULL_HEIGHT_SCALE = 1.405f;               // The scale of the game's contents that would make it fit the full height of the screen,
																	// ignoring the top overlay and the spin panel at the bottom.
	protected const float FS_FULL_HEIGHT_SCALE = 1.275f;            // Same as above, but for free spin games.
	protected const float FULL_HEIGHT = 16.596f;                    // The height of the full screen from top to bottom.

	protected bool isViewportScalingDone = false;
	//
	//------------------------------------------------------------------------------------------------------------------------------
	//


	//Free Spins --------------------------------------------------------------------------------------------------------------------
	[OmitFromNonDebugInspector] public bool playFreespinsInBasegame = false;
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)]
	public float delayBonusSummaryVo = 0.0f;
	protected BonusGamePresenter freespinsInBasegamePresenter = null; // stored BonusGamePresenter for the freespins in base, will be created if it doesn't exist when needed and then stored and reset each time freespins triggers
	[HideInInspector] public bool hasFreespinGameStarted = false;
	protected PayTable freespinsPaytable;
	protected int _numberOfFreespinsRemaining;
	protected bool isFirstSpin = true;
	protected const float FREESPINS_TRANSISTION_TIME = 1.5f;
	protected const float UNSKIPPABLE_FREESPIN_SPIN_TIME = 0.25f;
	protected const float FREESPIN_SPIN_STOP_TIME = 1.25f;
	protected string additionalInfo = "";       // Used if you want BonusSpinPanel.instance.messageLabel to say other info.
	protected bool isFreeSpinInBaseReady = false; // Can't change initFreespins to a coroutine easily because it is overrided so many places, so I'm using this to block the game until freespins in base is fully ready (avoids possibly bad timing issues)
	private bool isFreeSpinGameResultCached = false; // Tells if the result of isFreeSpinGame() is cached in isFreeSpinGameResult
	private bool isFreeSpinGameResult = false; // Used to store a cached result from the isFreeSpinGame() check
	
	protected FreeSpinsOutcome _freeSpinsOutcomes;
	
	public FreeSpinsOutcome freeSpinsOutcomes
	{
		get { return _freeSpinsOutcomes; }
	}
	
	//Free Spins --------------------------------------------------------------------------------------------------------------------


	public virtual long multiplier
	{
		get { return _multiplier; }

		set
		{
			_multiplier = value;
			_outcomeDisplayController.multiplier = value;
		}
	}
	protected long _multiplier;

	/// Helper property to tell if a game is using optimized flattened symbols
	public bool isGameUsingOptimizedFlattenedSymbols
	{
		get { return SlotResourceMap.isGameUsingOptimizedFlattenedSymbols(GameState.game.keyName); }
	}

	/// New wager system needs to show bonus games based on relative multipliers (i.e. wager/minWager)
	/// this is needed for bonus games where values will be calculated as: 
	/// bonusValue * (minWager/100) and the summary shows that total multiplied by (wager/minWager) which I'm calling relativeMultiplier
	public long relativeMultiplier
	{
		get
		{
			return SlotsWagerSets.getRelativeMultiplierForGame(GameState.game.keyName, currentWager);
		}
	}

	/// Determines what the currently active reel game is
	public static ReelGame activeGame
	{
		get
		{
			ReelGame game = null;
			if (FreeSpinGame.instance != null && FreeSpinGame.instance.didInit && FreeSpinGame.instance.hasFreespinGameStarted && FreeSpinGame.instance.gameObject.activeInHierarchy)
			{
				game = FreeSpinGame.instance;
			}
			else
			{
				game = SlotBaseGame.instance;
			}
			return game;
		}
	}

	// Tells if the game is a LayeredSlotBaseGame or LayeredSlotFreeSpinGame
	public bool isLayeredGame()
	{
		return this is LayeredSlotBaseGame || this is LayeredSlotFreeSpinGame;
	}

	// Tells if the game is a IndependentReelBaseGame
	public bool isIndependentReelGame()
	{
		return this is IndependentReelBaseGame || this is IndependentReelFreeSpinGame;
	}

	// Tells if the game is a free spin game, by casting it
	public bool isFreeSpinGame()
	{
		if (!isFreeSpinGameResultCached)
		{
			isFreeSpinGameResult = this is FreeSpinGame;
			isFreeSpinGameResultCached = true;
		}

		return isFreeSpinGameResult;
	}
	
	// Tells if this ReelGame is currently performing freespins in base
	public bool isDoingFreespinsInBasegame()
	{
		return playFreespinsInBasegame && hasFreespinGameStarted;
	}

	public SlotOutcome currentReevaluationSpin
	{
		get { return _currentReevaluationSpin; }
	}

	public SlotOutcome outcome
	{
		get { return _outcome; }
	}
	public OutcomeDisplayController outcomeDisplayController
	{
		get { return _outcomeDisplayController; }
	}

	//The freespins base game overrides the ReelGame's autoSpins however we can't since base games will still need ReelGame's version
	public virtual int numberOfFreespinsRemaining
	{
		get { return _numberOfFreespinsRemaining; }
		set
		{
			_numberOfFreespinsRemaining = value;
			if (BonusSpinPanel.instance != null && numberOfFreespinsRemaining > -1)
			{
				BonusSpinPanel.instance.spinCountLabel.text = numberOfFreespinsRemaining.ToString();
			}
		}
	}
	/// Returns whether there are basegameFreespinsSpins remaining. -1 means infinite.
	public bool hasFreespinsSpinsRemaining
	{
		get
		{
			return (numberOfFreespinsRemaining > 0 || numberOfFreespinsRemaining == -1);
		}
	}

	/// Use the autoSpins setter to keep the spin panel UI updated as it changes.
	public virtual int autoSpins
	{
		get { return _autoSpins; }

		set
		{
			_autoSpins = value;

			if (SpinPanel.instance != null)
			{
				SpinPanel.instance.setAutoCount(_autoSpins);
			}
		}
	}
	protected int _autoSpins;
	protected bool wakeBySpin;

	/// Returns whether there are autospins remaining. -1 means infinite.
	public bool hasAutoSpinsRemaining
	{
		get
		{
			return (autoSpins > 0 || autoSpins == -1);
		}
	}

	/// Returns whether there are reevaluation spins remaining
	public bool hasReevaluationSpinsRemaining
	{
		get { return reevaluationSpinsRemaining > 0; }
	}

	/// Expose a way to find out if the game is spin blocked from outside the game.
	public bool isSpinBlocked
	{
		get { return _outcomeDisplayController.isSpinBlocked(); }
	}

	// Gets the reel gameObject associated with a particular reelID and possible layer.
	public virtual GameObject getReelGameObject(int reelID, int row = -1, int layer = -1)
	{
		if (reelID >= reelRoots.Length || reelID < 0)
		{
			Debug.LogError("There are not enough reel ids in reelRoots: reelID = " + reelID + "; reelRoots.Length = " + reelRoots.Length);
			return null;
		}
		return getReelRootsAt(reelID, row, layer);
	}

	public virtual float getSymbolVerticalSpacingAt(int reelID, int layer = 0)
	{
		return symbolVerticalSpacing;
	}

	// The basic order that most reels get stopped in.
	protected void setReelStopOrder()
	{
		setDefaultReelStopOrder();

		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.needsToSetReelStopOrder())
			{
				stopOrder = module.setReelStopOrder();
			}
		}
	}
	
	// this method is being used by games without SetReelStop module attached to them
	// it can be removed if we start setting all games' reel stop order on their prefabs
	protected virtual void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][]
		{
			new StopInfo[] {new StopInfo(0),},
			new StopInfo[] {new StopInfo(1),},
			new StopInfo[] {new StopInfo(2),},
			new StopInfo[] {new StopInfo(3),},
			new StopInfo[] {new StopInfo(4),},
			new StopInfo[] {new StopInfo(5),},
		};
	}

	// determine the index that should be used when indexing into 
	public int getReelStopTimingIndex(int reelID, int row = 0, int layer = 0)
	{
		for (int stopIndex = 0; stopIndex < stopOrder.Length; stopIndex++)
		{
			for (int stopInfoIndex = 0; stopInfoIndex < stopOrder[stopIndex].Length; stopInfoIndex++)
			{
				StopInfo currentInfo = stopOrder[stopIndex][stopInfoIndex];
				if (currentInfo.reelID == reelID && currentInfo.layer == layer && currentInfo.row == row)
				{
					return stopIndex;
				}
			}
		}

		Debug.LogWarning("ReelGame.getReelStopTimingIndex() - reelID = " + reelID + "; row = " + row + "; layer = " + layer + "; couldn't find matching stop index!");
		return -1;
	}

	public int getReelStopIndex(SlotReel reel)
	{
		if (stopOrder == null)
		{
			Debug.LogError("Trying to get StopIndex when stopOrder isn't set.");
			return -1;
		}
		for (int stopIndex = 0; stopIndex < stopOrder.Length; stopIndex++)
		{
			for (int stop = 0; stop < stopOrder[stopIndex].Length; stop++)
			{
				StopInfo stopInfo = stopOrder[stopIndex][stop];
				if (reel.layer == stopInfo.layer && reel.reelID == (stopInfo.reelID + 1))
				{
					return stopIndex;
				}
			}
		}
		Debug.LogError("Couln't find a reel that matched this reel in stop info!");
		return -1;
	}

	//Add the swipeable reels. ReelDataNeeds to be set before in order for this to work properly.
	protected virtual void setSwipeableReels()
	{
		SlotReel[] reels = engine.getAllSlotReels();

		if (reels == null)
		{
			Debug.LogError("engine.getAllSlotReels is null in setSwipeableReels");
			return;
		}

		foreach (SlotReel reel in reels)
		{
			SwipeableReel sr = reel.getReelGameObject().GetComponent<SwipeableReel>();
			if(sr != null)
			{
				sr.init(reel, this);
			}
			else
			{
				sr = reel.getReelGameObject().AddComponent<SwipeableReel>();
				sr.init(reel, this);
			}
		}
	}

	// Check if this payout will be over the big win threshold
	public virtual bool isOverBigWinThreshold(long payout)
	{
		//Debug.Log("ReelGame.isOverBigWinThreshold() - Checking: payout:" + payout + " >= " + (Glb.BIG_WIN_THRESHOLD * betAmount));
		return payout >= Glb.BIG_WIN_THRESHOLD * betAmount;
	}

	// Helper function which determines the rollupType in order to generate correct rollup sound key
	private string determineRollupType(long payout, bool shouldBigWin)
	{
		// We are in a game. The rollup time is based on bet amount.
		string rollupType = "base";

		// Start our Rollup Clip. Use SoundMaps.
		// Override to big win sound map if we have a big win
		// Make sure to check any amount that was carried over.
		if (shouldBigWin && isOverBigWinThreshold(payout))
		{
			rollupType = "bigwin";
		}

		// Overide to the bonus game's map if we're currently playing a bonus game.
		// The only reliable way to know if we're in free spins or non-free spins bonus game
		// is by checking for the existence of instances of each, since some games
		// use free spins as the challenge game, causing confusion.
		// When coming back from a bonus game we want to be using the base games roll up, so we need
		// to check and make sure that the base game isn't active before we do any of these.
		if (activeGame.isFreeSpinGame())
		{
			rollupType = "freespin";
		}

		if (activeGame.engine.progressivesHit > activeGame.engine.progressiveThreshold && Audio.canSoundBeMapped("rollup_jackpot_loop") && Audio.canSoundBeMapped("rollup_jackpot_end"))
		{
			// If we have hit a jackpot we should do that roll up.
			rollupType = "jackpot";
		}

		return rollupType;
	}

	// Function which gets an override list of clobber replacement symbols
	// which will be used in place of whatever the reel originally was going
	// to use.
	// Returns null if there isn't an override
	public List<string> getClobberSymbolReplacementListOverrideForReel(SlotReel reel)
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.needsToExecuteGetClobberSymbolReplacementListOverride(reel))
			{
				return module.executeGetClobberSymbolReplacementListOverride(reel);
			}
		}

		return null;
	}

	public bool hasPortraitModeOnDeviceModule()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module != null && module is SwitchGameToPortraitModeOnDeviceModule)
			{
				return true;
			}
		}
		return false;
	}

	public void executeOnSetSymbolPosition(SlotReel reel, SlotSymbol symbol, float verticalSpacing)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnSetSymbolPosition(reel, symbol, verticalSpacing))
			{
				module.executeOnSetSymbolPosition(reel, symbol, verticalSpacing);
			}
		}
	}
		
	public bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToOverridePaylineSounds(slotSymbols, winningSymbolName))
			{
				return true;
			}
		}

		return false;
	}

	public void playOverridenPaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			module.executeOverridePaylineSounds(slotSymbols, winningSymbolName);
		}
	}

	public virtual bool needsToPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToPlaySymbolSoundOnAnimateOutcome(symbol))
			{
				return true;
			}
		}

		return false;
	}

	public virtual void playPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			module.executePlaySymbolSoundOnAnimateOutcome(symbol);
		}
	}

	public string getRollupTermSound(long payout, bool shouldBigWin)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteRollupTermSoundOverride(payout, shouldBigWin))
			{
				return module.executeRollupTermSoundOverride(payout, shouldBigWin);
			}
		}

		return Audio.soundMap("rollup_" + determineRollupType(payout, shouldBigWin) + "_end");
	}

	public string getRollupSound(long payout, bool shouldBigWin)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteRollupSoundOverride(payout, shouldBigWin))
			{
				return module.executeRollupSoundOverride(payout, shouldBigWin);
			}
		}

		return Audio.soundMap("rollup_" + determineRollupType(payout, shouldBigWin) + "_loop");
	}

	// Alternate version of getSpecificRollupTime that allows a module to specify a rollup time via payout, used for forced big wins right now
	public float getSpecificRollupTimeForPayout(long payout)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteRollupSoundLengthOverride())
			{
				return module.executeRollupSoundLengthOverride(payout);
			}
		}

		// Return -1 because that's the default value of 'executeRollupSoundLengthOverride' if not used.
		//	A -1 value indicates that there is no rollup time delay.
		return -1.0f;
	}

	public float getSpecificRollupTime(string soundKey)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteRollupSoundLengthOverride())
			{
				return module.executeRollupSoundLengthOverride(soundKey);
			}
		}

		// Return -1 because that's the default value of 'executeRollupSoundLengthOverride' if not used.
		//	A -1 value indicates that there is no rollup time delay.
		return -1.0f;
	}

	protected void populateSymbolReplaceMultiReplacementSymbolData(string reelSetKey, Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		if (reelInfo != null && !doesOutcomeContainSymbolReplaceMulti())
		{
			foreach (JSON info in reelInfo)
			{
				string type = info.getString("type", "");
				string backgroundType = "background";
				if (this is FreeSpinGame)
				{
					backgroundType = "freespin_background";
				}

				if (type == backgroundType || type == "symbol_replace_multi" || type == backgroundType + "_init")
				{
					if (hasSpotlight == true && info.getInt("z_index", 0) == 0)
					{
						_reelSetData = slotGameData.findReelSet(info.getString("reel_set", reelSetKey));
					}

					JSON replaceData = info.getJSON("replace_symbols");

					if (replaceData != null)
					{
						foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
						{
							if (!megaReplacementSymbolMap.ContainsKey(megaReplaceInfo.Key))
							{
								// Check and see if mega and normal have the same values.
								megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
							}
							else if (!hasSpotlight)     //We can have duplicate mappings for games like sinatra01 due to multiple reelsets
							{
								Debug.LogError("The replacement info should not contain duplicate mappings. Investigate!");
							}
						}
						foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
						{
							if (!megaReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key))
							{
								// Check and see if mega and normal have the same values.
								normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
							}
							else
							{
								Debug.LogError("The replacement info should not contain duplicate mappings. Investigate!");
							}
						}
					}
				}
			}
		}

		// check if we are freespins game and don't have any replacements defined yet, in
		// which case we will try to read the first outcome we are going to use and get
		// replacement info from there if possible.  Only do this when the freespins
		// is initing since otherwise it should be reading from the outcomes and we
		// don't want to hide missing RP data in that case.
		if (this is FreeSpinGame)
		{
			FreeSpinGame freespinGame = this as FreeSpinGame;

			if (!freespinGame.didInit && megaReplacementSymbolMap.Count == 0 && normalReplacementSymbolMap.Count == 0 && freespinGame.freeSpinsOutcomes.entries.Count > 0)
			{
				// Read out the first spin info, we are going to use it to determine what to set the replacements to
				SlotOutcome firstOutcome = freespinGame.freeSpinsOutcomes.entries[0];
				JSON[] mutations = firstOutcome.getMutations();

				for (int i = 0; i < mutations.Length; i++)
				{
					JSON mutation = mutations[i];
					JSON replaceData = mutation.getJSON("replace_symbols");

					if (replaceData != null)
					{
						foreach (KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
						{
							megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
						}

						foreach (KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
						{
							normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
						}
					}
				}
			}
		}
	}

	protected void setGameReplacementSymbolData(string reelSetKey, Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		if (_reelSetData != null)
		{
			engine.setReelSetData(_reelSetData, reelRoots, normalReplacementSymbolMap, megaReplacementSymbolMap);
			currentReelSetName = reelSetKey;
			setSpinPanelWaysToWin(reelSetKey);
		}
		else
		{
			Debug.LogError("Unable to find reel set data for key " + reelSetKey);
		}

		if (!doesOutcomeContainSymbolReplaceMulti())
		{
			// Set all the replacement symbols for each reel.
			foreach (SlotReel reel in engine.getReelArray())
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
			}
		}
	}

	/// Virtual function to allow for overriding the handling of setting a reelset, for instance layered games need custom handling
	protected virtual void handleSetReelSet(string reelSetKey)
	{
		_reelSetData = slotGameData.findReelSet(reelSetKey);

		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
		Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();

		populateSymbolReplaceMultiReplacementSymbolData(reelSetKey, normalReplacementSymbolMap, megaReplacementSymbolMap);
		setGameReplacementSymbolData(reelSetKey, normalReplacementSymbolMap, megaReplacementSymbolMap);
	}

	protected void applyInitialReelStops()
	{	
		// Some games (independent reel games) don't have reel info set at this point. So we bail out. We may need to add code
		//	in the future to handle initial reel stops for independent reel games.
		if (reelInfo == null)
		{
			return;
		}

		bool isFreespin = isFreeSpinGame();

		Dictionary<int, Dictionary<int, int>> stopsByLayer = new Dictionary<int, Dictionary<int, int>>();

		List<SlotReel[]> allReelLayers = new List<SlotReel[]>();

		if (isLayeredGame() && !isIndependentReelGame())
		{
			LayeredSlotEngine layerEngine = engine as LayeredSlotEngine;

			for (int i = 0; i < layerEngine.reelLayers.Length; i++)
			{
				SlotReel[] layerReelArray = layerEngine.reelLayers[i].getReelArray();
				if (layerReelArray == null)
				{
					Debug.LogError("ReelGame.applyInitialReelStops() - One of the reel layers wasn't initialized, this probably means Start Game Event Info is missing in SCAT, this is about to cause an NRE.");
				}

				allReelLayers.Add(layerReelArray);
			}
			
			for (int i = 0; i < reelInfo.Length; i++)
			{
				string reelSet = reelInfo[i].getString("reel_set", "");
				string type = reelInfo[i].getString("type", "");
				int zIndex = reelInfo[i].getInt("z_index", 0);
				bool isFreeSpinBackgroundData = (isFreespin && reelSet == defaultReelSetName && type == "freespin_background");
				bool isFreeSpinForegroundData = (isFreespin && type == "freespin_foreground");
				if (type != "data" && (!isFreespin || isFreeSpinBackgroundData || isFreeSpinForegroundData))
				{
					if (!stopsByLayer.ContainsKey(zIndex))
					{
						Dictionary<int, int> stops = reelInfo[i].getIntIntDict("stops");
						if (stops.ContainsKey(0))
						{
							Debug.LogWarning("Game initial stops seem to be starting at reel 0. Please verify that SCAT data starts at reel 1. Skipping initial stops.");
						}
						else
						{
							stopsByLayer.Add(zIndex, stops);
						}
					}
					else
					{
						Debug.LogWarning("Reel layer data contains duplicate zIndex");
					}
				}
			}
		}
		else
		{
			allReelLayers.Add(engine.getReelArray());
			for (int i = 0; i < reelInfo.Length; i++)
			{
				string reelSet = reelInfo[i].getString("reel_set", "");
				string type = reelInfo[i].getString("type", "");
				if (reelSet == defaultReelSetName && type != "data" && (!isFreespin || type.Contains("freespin")))
				{
					Dictionary<int, int> stops = reelInfo[i].getIntIntDict("stops");
					if (stops.ContainsKey(0))
					{
						Debug.LogWarning("Game initial stops seem to be starting at reel 0. Please verify that SCAT data starts at reel 1. Skipping initial stops.");
					}
					else
					{
						stopsByLayer.Add(0, stops);
					}
					break;
				}
			}
		}
		
		if (allReelLayers.Count > 0 && stopsByLayer.Count > 0)
		{
			for (int layerIndex = 0; layerIndex < allReelLayers.Count; layerIndex++)
			{
				Dictionary<int, int> stops = null;
				stopsByLayer.TryGetValue(layerIndex, out stops);
				if (stops != null)
				{
					for (int reelIndex = 0; reelIndex < allReelLayers[layerIndex].Length; reelIndex++)
					{
						int stopId = 0;
						if (stops.TryGetValue(reelIndex + 1, out stopId))
						{
							allReelLayers[layerIndex][reelIndex].setSymbolsToReelStripIndex(allReelLayers[layerIndex][reelIndex].reelData.reelStrip, stopId);
						}
					}
				}
			}
		}
	}

	/// Double check if this game uses symbol_replace_multi in outcomes, 
	/// in which case we aren't going to use the started data to set this 
	/// when changing reel sets
	protected bool doesOutcomeContainSymbolReplaceMulti()
	{
		if (outcome != null)
		{
			JSON[] outcomeMutations = outcome.getMutations();

			foreach (JSON mutation in outcomeMutations)
			{
				string type = mutation.getString("type", "");
				if (type == "symbol_replace_multi")
				{
					return true;
				}
			}
		}

		return false;
	}

	/// Function to handle grabbing reelInfo and then calling a virtual function to let games handle setting the reelset
	protected virtual void setReelSet(string reelSetKey)
	{
		// ensure reelInfo is setup
		setReelInfo();

		//ensure modifiers are setup
		setModifiers();

		// call the virtual function
		handleSetReelSet(reelSetKey);
	}

	public virtual void setReelSet(string reelSetKey, JSON data)
	{
		reelSetDataJson = data;
		setReelSet(reelSetKey);
	}

	protected void setModifiers()
	{
		if (reelSetDataJson != null)
		{
			// this is not a free spin game so grab the reel info directly and store it so the free spin game can access it later
			modifierExports = reelSetDataJson.getJsonArray("modifier_exports");
		}
		else if (modifierExports != null)
		{
			// we have previous reelInfo and no new reelSetDataJson, going to assume
			// that we want to keep the reelInfo whatever it currently is
		}
	}

	/// Get the reel_info data for this game, free spins are a bit unique as they will either pull from the base game startup event
	/// or if accessed via gifting will find the reel_info stored in the outcome
	protected void setReelInfo()
	{
		FreeSpinGame freeSpinCheck = this as FreeSpinGame;

		if (freeSpinCheck != null && reelInfo == null)
		{
			// free spin games must grab their reelinfo from the outcome if they are gited, or from the base game's started event
			if (SlotBaseGame.instance == null)
			{
				// Special case for gifting. Hopefully one day it always gets sent down this way.
				reelInfo = freeSpinCheck.freeSpinsOutcomes.reelInfo;
			}
			else if (SlotBaseGame.instance && SlotBaseGame.instance.reelInfo != null && SlotBaseGame.instance.reelInfo.Length > 0)
			{
				reelInfo = SlotBaseGame.instance.reelInfo;
			}
		}
		else
		{
			if (reelSetDataJson != null)
			{
				// this is not a free spin game so grab the reel info directly and store it so the free spin game can access it later
				reelInfo = reelSetDataJson.getJsonArray("reel_info");
				freeSpinInitialReelSet = reelSetDataJson.getIntStringDict("freespin_initial_reel_sets");
			}
			else if (reelInfo != null)
			{
				// we have previous reelInfo and no new reelSetDataJson, going to assume
				// that we want to keep the reelInfo whatever it currently is
			}
			else
			{
				Debug.LogError("SlotBaseGame is trying to setReelInfo() with a null reelSetDataJson and no previous reelInfo set!");
			}
		}
	}

	/// JSON overload for setting outcome.
	public void setOutcome(JSON data)
	{
		setOutcome(new SlotOutcome(data));
	}

	/// SlotOutcome overload for setting outcome.
	public virtual void setOutcome(SlotOutcome outcome)
	{
		_outcome = outcome;
		_outcome.printOutcome();
		_outcome.processBonus();

		// Handle any setup that needs to happen before the reels are changed, also handle this before linking in case it will have an impact on reel linking
		engine.handleOutcomeBeforeSetReelSet(_outcome);

		// Setup initial linking based on the outcome, note we are setting this in engine before the engine actually updates it's outcome
		engine.updateOutcomeLinkedReelList(_outcome);

		// Grab any information about reevaluations, then check for spins
		reevaluationSpins = _outcome.getReevaluationSpins();
		reevaluationSpinsRemaining = reevaluationSpins.Count;

		if (hasSpotlight)
		{
			spotlightReelStartIndex = _outcome.getSpotlightReelStartIndex() - 1;    // since reel start indices in server data are not zero based
		}

		mutationManager.setMutationsFromOutcome(_outcome.getJsonObject(), hasMutationsInReeval);
		
		// Check for and update reel stop symbol overrides
		setupReelSymbolOverrides(outcome);
	}

	// Update the reels with any symbol overrides that come down in the passed outcome
	// This will happen automatically for each new spin, as will clearing the ones from the previous spin.
	// If you don't want the symbolOverrides cleared before each spin you can use SlotModule.isHandlingSlotReelClearSymbolOverridesWithModule
	// to flag that a module will be in charge of clearing them when it thinks is the right time.
	protected void setupReelSymbolOverrides(SlotOutcome outcome)
	{
		OverrideSymbolData[] overrides = outcome.getOverrideSymbols();
		if (overrides != null)
		{
			foreach (OverrideSymbolData symbolOverride in overrides)
			{
				SlotReel reel = engine.getSlotReelAt(symbolOverride.reel, symbolOverride.position, symbolOverride.layer);
				if (reel != null)
				{
					reel.addSymbolOverride(symbolOverride.toSymbol, symbolOverride.reelStripIndex);
				}
				else
				{
					Debug.LogError("ReelGame.setupReelSymbolOverrides() - Unable to get SlotReel at: reelIndex = " + symbolOverride.reel + "; position = " + symbolOverride.position + "; layer = " + symbolOverride.layer);
				}
			}
		}
	}

	// Removes all of the reevaluations from the outcome so they don't get processed.
	public void clearReevaluationSpins()
	{
		_currentReevaluationSpin = null;
		clearReevalStickySymbolInfo();
		reevaluationSpinsRemaining = 0;
	}

	// Setting the outcome of this Reel Game without doing any of the extra stuff. // USE THIS VERY RARELY AND CAREFULLY
	public virtual void setOutcomeNoExtraProcessing(SlotOutcome outcome)
	{
		_outcome = outcome;
	}

	protected virtual void Awake()
	{
		startingPayBoxSize = payBoxSize;
		symbolAnimatingCallback = SymbolAnimatingCallback;
		// Register this object as disposable so that it clean up at the same time as the spin panel
		DisposableObject.register(gameObject);

		updateVerticalSpacingWorld();
		startingSymbolVerticalSpacingWorld = symbolVerticalSpacingWorld;

		if (GameState.game != null)
		{
			slotGameData = SlotGameData.find(GameState.game.keyName);
		}

		if (slotGameData == null)
		{
			if (GameState.game != null)
			{
				Debug.LogError("Unable to find SlotGameData for game " + GameState.game.keyName);
			}

			return;
		}

		// Cache attached slot modules for performance
		cacheAttachedSlotModules();
		
		// Set up an object to put cached symbols under
		symbolCacheGameObject = NGUITools.AddChild(gameObject);
		symbolCacheGameObject.name = "Symbol Cache";

		// Set up an object to put paylines under.
		createActivePaylinesObject("Active Paylines");

		gameScaler = NGUITools.AddChild(gameObject);
		gameScaler.name = "Game Scaler";

		if (isGameUsingOptimizedFlattenedSymbols)
		{
			Debug.Log("Game: " + GameState.game.keyName + " is using Optimized Flattend symbols!");

			if (Glb.loadOptimizedFlatSymbolsOvertime)
			{
				RoutineRunner.instance.StartCoroutine(createFlattenedSymbolTemplatesOverTime());
			}
			else
			{
				createFlattenedSymbolTemplates();
			}
		}

		if (!isFreeSpinGame() || GameState.giftedBonus != null)
		{
			slotSymbolCache = new SlotSymbolCache(symbolCacheMax, symbolCacheMaxLowEndAndroid, symbolCacheGameObject);
		}
		else if (isFreeSpinGame() && SlotBaseGame.instance != null)
		{
			// We are in freespins and are going to use the base game cache
			slotSymbolCache = SlotBaseGame.instance.getSlotSymbolCache();
		}

		Debug.Log("SlotBaseGame loading for " + slotGameData.keyName + ", symbolVerticalSpacingWorld: " + symbolVerticalSpacingWorld);
	}

	// Check if basegame wins are supposed to be fully paid out before going to
	// Freespin in Base.  This only has any affect if this games uses freespin in base.
	//
	// Normally basegame wins are simply carried over to the freespin in base and
	// everything is paid out at the end.  If there is a reason why you wouldn't want
	// the value carried over, like there is a multiplier that is only applied to
	// freespins wins, you can use this in order to payout the value before freespins.
	//
	// NOTE: If any module sets this to true it will be treated as being used
	// NOTE: Big wins will not occur for the base winnings even if they go over the
	// threshold (since it would be a fairly big interruption for transitioning into
	// freespins).  This also means that the final bonus payout will only big win
	// if it goes over the big win threshold on its own.
	public bool isPayingBasegameWinsBeforeFreespinsInBase()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.isPayingBasegameWinsBeforeFreespinsInBase())
			{
				return true;
			}
		}

		return false;
	}
	
	// Check if basegame wins should be fully paid out before going into bonus games
	// (including delaying their transitions until after base game payout).
	//
	// Normally basegame wins are paid out after bonus games, but for gen97 Cash Tower and maybe
	// some future games we want to pay out the base game wins before the bonus game
	// due to the bonus functioning in a way where we want to get the base payouts done
	// before starting the complciated bonus game flow.
	//
	// NOTE: If any module sets this to true it will be treated as being used
	// NOTE: Big wins will not occur for the base winnings even if they go over the
	// threshold (since it would be a fairly big interruption for transitioning into
	// bonuses).  This also means that the final bonus payout will only big win
	// if it goes over the big win threshold on its own.
	public bool isPayingBasegameWinsBeforeBonusGames()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.isPayingBasegameWinsBeforeBonusGames())
			{
				return true;
			}
		}

		return false;
	}

	// Setup the freespins game before freespins game is played in the basegame
	public virtual void initFreespins()
	{
		// Unmute the game if it was going to be muted.
		Audio.listenerVolume = 1.0f;
		setReelStopOrder();

		_freeSpinsOutcomes = (FreeSpinsOutcome)BonusGameManager.instance.outcomes[BonusGameType.GIFTING];
		if (_freeSpinsOutcomes.numFreespinsOverride > 0)
		{
			numberOfFreespinsRemaining = _freeSpinsOutcomes.numFreespinsOverride;
		}
		else
		{
			numberOfFreespinsRemaining = _freeSpinsOutcomes.paytable.getInt("free_spins", -1);
		}

		JSON paytable = _freeSpinsOutcomes.paytable;
		string freespinsPaytableKey = paytable.getString("free_spin_pay_table", "");
		BonusGameManager.currentBaseGame = activeGame;
		BonusGameManager.instance.currentBonusPaytable = PayTable.find(freespinsPaytableKey);

		clearOutcomeDisplay();

		freespinsPaytable = PayTable.find(freespinsPaytableKey);
		engine.setFreespinsPaytableKey(freespinsPaytableKey);
		_outcomeDisplayController.payTable = freespinsPaytable;

		//Setup the bonus spin panel
		if (BonusSpinPanel.instance != null)
		{
			BonusSpinPanel.instance.messageLabel.text = Localize.text("good_luck");
		}

		// Need to zero the meter out and clear the values if we are paying out the base wins before the freespins,
		// otherwise we will carry over the base winnings and pay them out along with the freespin win
		if (isPayingBasegameWinsBeforeFreespinsInBase())
		{
			BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(0);
			// Clear all the rollup values out since we already paid everything out, so the freespins will start totally clean
			resetAllRollupValues();
		}
		else
		{
			BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(runningPayoutRollupValue);
		}

		if (freespinsInBasegamePresenter == null)
		{
			freespinsInBasegamePresenter = gameObject.AddComponent<BonusGamePresenter>();
		}

		BonusGameManager.instance.bonusGameName = _freeSpinsOutcomes.bonusGameName;
		freespinsInBasegamePresenter.bonusGameName = _freeSpinsOutcomes.bonusGameName;
		BonusGameManager.instance.paytableSetId = _freeSpinsOutcomes.getPaytableSetId();
		freespinsInBasegamePresenter.paytableSetId = BonusGameManager.instance.paytableSetId;

		// setup the freespin game as the current BonusGamePresenter and make it active
		BonusGamePresenter.instance = freespinsInBasegamePresenter;
		freespinsInBasegamePresenter.isGameActive = true;

		freespinsInBasegamePresenter.useMultiplier = false; // Freespins by defualt don't include these multipliers.
															// Set the multiplier, which also sets the outcomeDisplayController's multiplier to match.
															
		if (GameState.giftedBonus != null)
		{
			// Make sure we only use this on a gifted spins.
			multiplier = GiftedSpinsVipMultiplier.playerMultiplier;
		}
		else
		{
			multiplier = SlotBaseGame.instance.multiplier;
		}


		BonusGameManager.instance.playFreespinsInBase(freespinsInBasegamePresenter, runningPayoutRollupValue);

		//Continue on into the freespins
		StartCoroutine(continueToBasegameFreespins());
	}

	// Create the object that stores paylines, normally done on init of a ReelGame but in the editor ReelSetup 
	// will use it to spawn the object that will hold the setup version of the paylines
	public void createActivePaylinesObject(string objectName)
	{
		// Set up an object to put paylines under.
		activePaylinesGameObject = NGUITools.AddChild(gameObject);
		if (activePaylinesGameObjectZOffset != 0.0f)
		{
			Vector3 curLocalPos = activePaylinesGameObject.transform.localPosition;
			activePaylinesGameObject.transform.localPosition = new Vector3(curLocalPos.x, curLocalPos.y, activePaylinesGameObjectZOffset);
		}
		activePaylinesGameObject.name = objectName;
	}

	// Destroy the activePaylinesGameObject, really only should be used by by ReelSetup 
	// when the game isn't running to cleanup the created object so it isn't saved with the prefab
	public void destroyActivePaylinesObject()
	{
		if (activePaylinesGameObject != null)
		{
			if (Application.isPlaying)
			{
				Destroy(activePaylinesGameObject);
			}
			else
			{
				DestroyImmediate(activePaylinesGameObject);
			}

			activePaylinesGameObject = null;
		}
	}

	//Used to transition into basegame Freespins
	protected virtual IEnumerator continueToBasegameFreespins()
	{
		yield return StartCoroutine(executeGameStartModules());
		isFreeSpinInBaseReady = true;
	}

	/// Called when a bonus game ends, usually used to clean up a transition
	public virtual void onBonusGameEnded()
	{
		//Stub these methods for transfer from SlotBaseGame 
	}


	// Cache slot modules for performance; implemented as a separate function so that it can be called directly if necessary
	public void cacheAttachedSlotModules()
	{
		cachedAttachedSlotModules = new List<SlotModule>(GetComponents<SlotModule>());
	}

	/// Built in unity funciton called when the ReelGame is destroyed
	protected virtual void OnDestroy()
	{
		// Need to clean up the symbol pool, in case the symbols aren't parented under the game due to being pre-pooled
		if (slotSymbolCache != null && (!isFreeSpinGame() || SlotBaseGame.instance == null || GameState.giftedBonus != null))
		{
			// Makes sure that after playing a gift chest gift inside a base game it correctly destroys the gifted game's symbols
			// since they will most likely have no relation to the base game.
			slotSymbolCache.clearSymbolCache();
		}
		else if (slotSymbolCache != null && isFreeSpinGame())
		{
			slotSymbolCache.limitIndividualSymbolStackSizes();
		}
	}

	// function used in many places for destroying game objects after waiting a certain amount of time
	// useful in places where we start an object on a path or some other type of tween, but don't want to handle
	// waiting for it to finish in that same coroutine
	public IEnumerator waitThenDestroy(GameObject go, float time)
	{
		yield return new TIWaitForSeconds(time);
		Destroy(go);
	}

	// utility function that simply waits an amount of time before deactivating a game object
	public IEnumerator waitThenDeactivate(GameObject go, float time)
	{
		yield return new TIWaitForSeconds(time);
		go.SetActive(false);
	}

	protected virtual void SymbolAnimatingCallback(SymbolAnimator animator)
	{
		// Overriden in children if specific things need to happen when a symbol animates.
	}

	/// Sets the "ways" or "lines" side boxes, which are part of the spin panel prefab.
	protected virtual void setSpinPanelWaysToWin(string reelSetName)
	{
		if (SpinPanel.instance == null)
		{
			// This could happen during a game that doesn't use the normal spin panel.
			return;
		}

		// One of these should be > 0, and that will tell us which one to use.
		int waysToWin = slotGameData.getWaysToWin(reelSetName);
		int winLines = slotGameData.getWinLines(reelSetName);

		if (initialWaysLinesNumber != 0)
		{
			if (waysToWin > 0)
			{
				SpinPanel.instance.setSideInfo(initialWaysLinesNumber, "ways", showSideInfo);
			}
			else if (winLines > 0)
			{
				SpinPanel.instance.setSideInfo(initialWaysLinesNumber, "lines", showSideInfo);
			}
			return;
		}

		if (waysToWin > 0)
		{
			SpinPanel.instance.setSideInfo(waysToWin, "ways", showSideInfo);
			initialWaysLinesNumber = waysToWin;
		}
		else if (winLines > 0)
		{
			SpinPanel.instance.setSideInfo(winLines, "lines", showSideInfo);
			initialWaysLinesNumber = winLines;
		}

	}

	/// Calculate the symbol vertical spacing in world coords for payline UI usage.
	public void updateVerticalSpacingWorld()
	{
		if (reelRoots != null && reelRoots.Length > 0)
		{
			// Only do this if this is a graphical version with reelRoots defined
			symbolVerticalSpacingWorld = getSymbolVerticalSpacingAt(0) * CommonTransform.getWorldScale(reelRoots[0].transform).y;
		}
	}

	// get next outcome - only works correctly in FreeSpins, since base games need to wait for server response
	public virtual SlotOutcome peekNextOutcome()
	{
		return null;
	}

	// TODO: Once all games have been converted over to use pre flattend symbols this should be removed.
	public bool hasAlreadyPreflattendSymbols()
	{
		foreach (SymbolInfo symbol in symbolTemplates)
		{
			if (symbol.flattenedSymbolPrefab != null)
			{
				return true;
			}
		}

		return false;
	}

	/// Validates that the symbol pool is set up, or sets it up if it hasn't been yet.
	/// This is called at the start of functions that reference either _symbolPool or _symbolMap.
	private void validateSymbolMapMade()
	{
		if (_symbolMap == null)
		{
			// Build the symbol template map and recycling pool from the inspector data
			_symbolMap = new Dictionary<string, SymbolInfo>();
			_symbolBoundsCache = new Dictionary<string, SymbolAnimator.BoundsInfo>();
			foreach (SymbolInfo symbol in symbolTemplates)
			{
				ReadOnlyCollection<string> possibleSymbolNames = symbol.getNameArrayReadOnly();

				foreach (string name in possibleSymbolNames)
				{
					// Add this check first to make sure we don't have any duplicate symbols. Make sure to print a meaningful error.
					if (_symbolMap.ContainsKey(name))
					{
						if (Application.isPlaying)
						{
							Debug.LogError(string.Format("Duplicate symbol {0} found in symbol template for game {1}!", name, GameState.game.keyName));
#if !ZYNGA_TRAMP
							Debug.Break();
#endif
						}
						else
						{
							Debug.LogError(string.Format("Duplicate symbol {0} found in game!", name));
						}
					}
					else
					{
						_symbolMap.Add(name, symbol);
						if (symbol.flattenedSymbolPrefab != null)
						{
							// Lets add the flattened version of the symbol pool here too.
							Vector2 symbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(name);
							string symbolShortName = SlotSymbol.getShortNameFromName(name);
							string symbolNameWithFlattenedExtension = SlotSymbol.constructNameFromDimensions(
								symbolShortName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX,
								(int) symbolSize.x,
								(int) symbolSize.y
							);
							_symbolMap.Add(symbolNameWithFlattenedExtension, symbol);
						}
					}
				}
			}

			if (slotSymbolCache != null)
			{
				slotSymbolCache.createEntriesInSymbolCache(symbolTemplates, isFreeSpinGame());
			}
		}
	}

	// startAutoSpin - called by the spin panel to initiate an auto-spin series.
	public void startAutoSpin(int spins)
	{
		autoSpins = spins;
		startNextAutospin();
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected virtual IEnumerator prespin()
	{
		if (!hasFreespinGameStarted)
		{
			// zero this out every spin unless we are doing a free spin
			resetAllRollupValues();
		}

		isSkippingPreWinThisSpin = false;
		outcomeDisplayController.isPlayingOutcomeAnimSoundsThisSpin = true;

		mutationCreditsAwarded = 0;
		reevaluationSpinMultiplierOverride = -1;

		// add a delay in games that need one when because the previous outcome contained no outcomes but an animation would look crazy if played really fast
		if (hasAutoSpinsRemaining && outcome != null && !outcome.hasSubOutcomes())
		{
			if (noOutcomeAutospinDelay != 0.0f)
			{
				yield return new TIWaitForSeconds(noOutcomeAutospinDelay);
			}
		}
		
		// Reset the symbol overrides that were set in the last spin, unless a module is going to 
		// handle clearing them at some other point.
		if (!isHandlingSlotReelClearSymbolOverridesWithModule())
		{
			engine.clearSymbolOverridesOnAllReels();
		}

		// override to handle changes needed to be done prespin
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPreSpin())
			{
				yield return StartCoroutine(module.executeOnPreSpin());
			}
		}
		// override to handle changes needed to be done prespin without going into a coroutine
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPreSpinNoCoroutine())
			{
				module.executeOnPreSpinNoCoroutine();
			}
		}

		// reset special sounds that were setup on the last spin
		foreach (SlotReel reel in engine.getAllSlotReels())
		{
			reel.reelStopSoundOverride = "";
			reel.reelStopVOSound = "";
		}
	}

	// Resets all the rollup values.  Should mostly be used exclusively during prespin, because
	// resetting these values can cause desyncs and credit display issues.  However, it is also
	// used when entering freespin in base if the base wins are to be paid out before freespins
	// instead of carried over into it.
	protected void resetAllRollupValues()
	{
		// check the value of what was rolled up with what was payed out to see if they match, 
		// if they don't then we probably have some issue
		if (runningPayoutRollupValue != runningPayoutRollupAlreadyPaidOut)
		{
			Debug.LogError("ReelGame.prespin() - Previous spin didn't payout the same amount it supposedly rolled up! runningPayoutRollupValue = " + runningPayoutRollupValue + "; runningPayoutRollupAlreadyPaidOut = " + runningPayoutRollupAlreadyPaidOut);
		}
		
		runningPayoutRollupValue = 0;
		runningPayoutRollupAlreadyPaidOut = 0;
		lastPayoutRollupValue = 0;
	} 

	// Called by the game after the reels start spinning (before the outcome has been set)
	protected virtual IEnumerator reelsSpinning()
	{
		// override to handle changes needed to be done while the reels are spinning
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReelsSpinning())
			{
				yield return StartCoroutine(module.executeOnReelsSpinning());
			}
		}
	}

	// Called by the game after the reels start spinning (before the outcome has been set)
	protected IEnumerator executeGameStartModules()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnSlotGameStartedNoCoroutine(reelSetDataJson))
			{
				module.executeOnSlotGameStartedNoCoroutine(reelSetDataJson);
			}
		}
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnSlotGameStarted(reelSetDataJson))
			{
				yield return StartCoroutine(module.executeOnSlotGameStarted(reelSetDataJson));
			}
		}
	}

	// Called by the engine before the reels stop (after the outcome has been set)
	public virtual IEnumerator preReelsStopSpinning()
	{
		// Make sure all events have been processed before doing this,
		// rather than rushing ahead as soon as the outcome event is processed.
		// This is especially important for any special wins that surface
		// before the reels stop spinning.
		while (Server.waitingForActionsResponse)
		{
			yield return null;
		}
		yield return StartCoroutine(doSpecialWins(SpecialWinSurfacing.PRE_REEL_STOP));

		// override to handle changes needed to be done while the reels are spinning
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecutePreReelsStopSpinning())
			{
				yield return StartCoroutine(module.executePreReelsStopSpinning());
			}
		}
	}

	// Play the background music.
	public virtual void playBgMusic()
	{
		Audio.stopMusic();

		Audio.stopSound(Audio.findPlayingAudio("lobbyambienceloop0"));

		if (hasFreespinGameStarted)
		{
			if (!Audio.isPlaying(Audio.soundMap(FREE_SPINS_INTRO_VO_KEY)))
			{
				Audio.play(Audio.soundMap(FREE_SPINS_INTRO_VO_KEY));
			}

			if (FREE_SPINS_IN_BASE_MUSIC_DELAY > 0.0f)
			{
				StartCoroutine(playBgMusicWithDelay(FREE_SPINS_IN_BASE_MUSIC_DELAY));
			}
			else
			{
				playFreespinBgMusic();
			}

		}
		else
		{
			Audio.switchMusicKey(Audio.soundMap(BASE_GAME_BG_MUSIC_KEY));

			// Set the idle time with an extra ten seconds to allow for the stinger to end.
			idleTimer = Time.time + BASE_GAME_MUSIC_IDLE_TIME;

			// When you press spin, play the spin music.
			shouldPlaySpinMusic = true;
		}
	}

	protected IEnumerator playBgMusicWithDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		playFreespinBgMusic();
	}

	private void playFreespinBgMusic()
	{
		// if a freespin intro is defined, play it with queued background music, else play the BG normally
		if (!string.IsNullOrEmpty(Audio.soundMap(FREE_SPINS_INTRO_MUSIC_KEY)))
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FREE_SPINS_INTRO_MUSIC_KEY), 0.0f);
			Audio.switchMusicKey(Audio.soundMap(FREE_SPINS_BG_MUSIC_KEY), 0.0f);
		}
		else
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(FREE_SPINS_BG_MUSIC_KEY), 0.0f);
		}
	}

	// When you press spin, play the spin music.
	public void playSpinMusic()
	{
		if (shouldPlaySpinMusic || shouldPlayAndStopMusicOnEachSpin)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BASE_GAME_SPIN_MUSIC_KEY), 0.0f);
			shouldPlaySpinMusic = false;
		}
	}

	// Do any special win presentation for the given surface time.
	public IEnumerator doSpecialWins(SpecialWinSurfacing surfaceTime)
	{
//#if !ZYNGA_PRODUCTION
//		if (DevGUIMenuTools.disableFeatures)
//		{
//			yield break;
//		}
//#endif

		isSpecialWinActive = true;


		if (surfaceTime == Glb.SURFACING_PROGRESSIVE_WIN)
		{
			// Show all progressive jackpot win celebrations.
			while (ProgressiveJackpot.winEvents.Count > 0)
			{
				ProgressiveJackpot.showWin(ProgressiveJackpot.winEvents[0]);
				yield return StartCoroutine(waitForSpecialWinDialog());
			}
		}

		if (surfaceTime == Glb.SURFACING_MYSTERY_GIFT_WIN)
		{
			// Show all mystery gift minigames. Includes all mystery game types, such as big win.
			while (MysteryGift.outcomes.Count > 0)
			{
				MysteryGift.showGift(MysteryGift.outcomes[0]);
				yield return StartCoroutine(waitForSpecialWinDialog());
			}
		}

		if (surfaceTime == SpecialWinSurfacing.POST_NORMAL_OUTCOMES)
		{
			//Play the level up sequence if we have one queued up
			if (Overlay.instance.topHIR.levelUpSequence != null)
			{
				Overlay.instance.topHIR.levelUpSequence.startLevelUp();
				yield return StartCoroutine(waitForLevelUpAnimations());
			}
			
			// This is actually a possible special loss.
			// Daily challenge spin count increments after each spin, then checks for whether the challenge has expired.
			if (Quest.activeQuest is DailyChallenge &&
				GameState.game != null &&
				DailyChallenge.gameKey == GameState.game.keyName &&
				DailyChallenge.checkExpired())
			{
				// Only do this if we don't show the dialog, becuase if we show the dialog then 
				// we dont want to mark it as seen unless we have actually shown it.
				PlayerAction.saveCustomTimestamp(DailyChallenge.LAST_SEEN_OVER_TIMESTAMP_KEY);
				DailyChallenge.lastSeenOverDialog = GameTimer.currentTime;
			}

			// Process Challenge completion event.
			if (CampaignDirector.campaigns != null)
			{
				foreach (ChallengeCampaign campaign in CampaignDirector.campaigns.Values)
				{
					if (campaign != null && campaign.shouldProcess)
					{
						campaign.processCompletionInQueueAsync();

						if (campaign.shouldShowDialog)
						{
							yield return StartCoroutine(waitForSpecialWinDialog());
						}
					}
					else if (campaign == null)
					{
						Bugsnag.LeaveBreadcrumb("ReelGame: Campaign is null in doSpecialWins()");
					}
				}
			}

			if (Overlay.instance.topHIR != null && PartnerPowerupStatusButton.instance != null && PartnerPowerupStatusButton.instance.shouldPlayAnimation)
			{
				PartnerPowerupStatusButton.instance.playAnimation();
			}

			if (Overlay.instance.jackpotMystery != null && Overlay.instance.jackpotMystery.tokenBar != null)
			{
				if (Overlay.instance.jackpotMystery.tokenBar.tokenWon)
				{
					if (tokenBar.hasDialogCelebration)
					{
						Overlay.instance.jackpotMystery.tokenBar.startTokenCelebration();
						yield return StartCoroutine(waitForSpecialWinDialog());
					}
					yield return StartCoroutine(Overlay.instance.jackpotMystery.tokenBar.addTokenAfterCelebration());
				}
			}
			else if (Overlay.instance.jackpotMystery == null)
			{
				Bugsnag.LeaveBreadcrumb("ReelGame: JackpotMystery instance is null! This should never happen!");
			}
		}

		isSpecialWinActive = false;
		idleTimer = Time.time;
	}

	// Waits for a special win dialog to appear then disappear before continuing.
	protected IEnumerator waitForSpecialWinDialog()
	{
		while (!Dialog.instance.isShowing || Dialog.instance.isDownloadingTexture)
		{
			yield return null;
		}

		while (Dialog.instance.isShowing)
		{
			yield return null;
		}
	}

	private IEnumerator waitForLevelUpAnimations()
	{
		while (Overlay.instance.topHIR.levelUpSequence != null)
		{
			yield return null;
		}
	}

	protected virtual void startSpin()
	{
		wakeBySpin = true;
		StartCoroutine(startSpinCoroutine());
	}

	protected virtual void startSpin(bool isFromSwipe = false, SlotReel.ESpinDirection direction = SlotReel.ESpinDirection.Down)
	{
		wakeBySpin = true;
		StartCoroutine(startSpinCoroutine(isFromSwipe, direction));
	}

	protected virtual IEnumerator startSpinCoroutine(bool isFromSwipe = false, SlotReel.ESpinDirection direction = SlotReel.ESpinDirection.Down, bool requestServerOutcome = true)
	{
		yield break;
	}

	// stopSpin - handler for button player can press that stops the reels
	public void stopSpin()
	{
		// Don't change the autoSpins count if this is a freespingame or we are doing freespins in base
		if (!isFreeSpinGame() && !isDoingFreespinsInBasegame())
		{
			stopAutoSpin();
		}

		engine.slamStop();
	}

	public void stopAutoSpin()
	{
		autoSpins = 0;
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected virtual void reelsStoppedCallback()
	{
		StartCoroutine(reelGameReelsStoppedCoroutine());
	}

	/// Giving this a kind of funny name so it doesn't overlap with games which have created their own funcitons called reelsStoppedCoroutine()
	private IEnumerator reelGameReelsStoppedCoroutine()
	{
		yield return StartCoroutine(waitForEngineAnimations());
		yield return StartCoroutine(waitForModulesAtReelsStoppedCallback());

		StartCoroutine(handleNormalReelStop());
	}

	/// Need to wait on mystery symbol animations, so we don't display the outcome till they are actually revealed 
	private IEnumerator waitForEngineAnimations()
	{
		// wait for the mystery symbols to finish changing
		while (engine.animationCount > 0)
		{
			yield return null;
		}
	}

	private IEnumerator waitForModulesAtReelsStoppedCallback()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReelsStoppedCallback())
			{
				yield return StartCoroutine(module.executeOnReelsStoppedCallback());
			}
		}

	}

	public IEnumerator checkModulesAtRollupStart (long bonusPayout, long basePayout)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnStartPayoutRollup(bonusPayout, basePayout))
			{
				yield return StartCoroutine(module.executeOnStartPayoutRollup(bonusPayout, basePayout));
			}
		}
	}

	public IEnumerator checkModulesAtBonusPoolCoroutineStop(long bonusPayout, long basePayout)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnStartPayoutRollupEnd(bonusPayout, basePayout))
			{
				yield return StartCoroutine(module.executeOnStartPayoutRollupEnd(bonusPayout, basePayout));
			}
		}
	}

	/// Handle the normal reel stopping along with doing sticky symbols that might be on the reels
	public virtual IEnumerator handleNormalReelStop()
	{
		if (hasExpandingSymbols)
		{
			string expandingSymbolName = getExpandingSymbolName();
			if (expandingSymbolName != null)
			{
				yield return StartCoroutine(overlayExpandingSymbol(expandingSymbolName));
			}

		}

		if (_outcome != null && _outcome.hasStickySymbols())
		{
			yield return StartCoroutine(handleReevaluationStickySymbols(_outcome));
		}

		bool isAllowingContinueWhenReadyToEndSpin = true;
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			isAllowingContinueWhenReadyToEndSpin &= cachedAttachedSlotModules[i].onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin();
		}

		yield return StartCoroutine(doReelsStopped(isAllowingContinueWhenReadyToEndSpin));
	}

	// Checks a reel for a stack of symbols, returns a reference to the symbol if found, otherwise null
	private SlotSymbol getReelStackSymbol(int reelID)
	{
		SlotSymbol[] visibleSymbols = engine.getVisibleSymbolsAt(reelID);
		SlotSymbol symbol = visibleSymbols[0];
		for (int i = 1; i < visibleSymbols.Length; i++)
		{
			if (symbol.name != visibleSymbols[i].name)
			{
				return null;
			}
		}
		return symbol;
	}

	// Gets the name of the expanding symbol based off how much of the reels has been filled.
	private string getExpandingSymbolName()
	{
		// Number of stacks founds
		int stacksFound = 0;

		// Grab the stacking symbol from the first reel
		SlotSymbol searchSymbol = getReelStackSymbol(0);

		// Check to see if there even is a stacking symbol
		if (searchSymbol != null)
		{
			// Loop through all the remaining reels
			int reelArrayLength = engine.getReelArray().Length;
			for (int reelID = 1; reelID < reelArrayLength; reelID++)
			{
				// Grab the stacking symbol
				SlotSymbol symbol = getReelStackSymbol(reelID);

				// Check if it is not null, otherwise break
				if (symbol != null)
				{
					// If the symbol has the same name as the search symbol or it's a wild symbol, then count it
					if (symbol.name == searchSymbol.name || symbol.isWildSymbol)
					{
						stacksFound = reelID + 1;
					}
					// If the search symbol is a wild symbol, replace it with whatever symbol we found and count it
					else if (searchSymbol.isWildSymbol)
					{
						searchSymbol = symbol;
						stacksFound = reelID + 1;
					}
					else
					{
						// Break if the symbol names don't match, the searchSymbol wasn't wild, and the current symbol wasn't wild
						break;
					}
				}
				else
				{
					break;
				}
			}
		}

		if (stacksFound > 1)
		{
			SlotReel leftReel = engine.getSlotReelAt(0);
			return SlotSymbol.constructNameFromDimensions(searchSymbol.name, stacksFound, leftReel.visibleSymbols.Length);
		}

		return null;
	}

	private void playExpandingSymbolSounds(string sym)
	{
		string symbolSound = Audio.soundMap("symbol_animation_" + sym);

		PlayingAudio animSound = null;
		if (FreeSpinGame.instance != null)
		{
			// This freespin animation sound isn't defined in every game.
			animSound = Audio.play(Audio.soundMap("freespin_symbol_animation_" + sym));
		}

		if (animSound == null)
		{
			animSound = Audio.play(symbolSound);
		}

		string ambientSound = Audio.soundMap("symbol_ambient_" + sym);
		if (ambientSound != null && ambientSound != "")
		{
			Audio.play(ambientSound);
		}

		if (persistingMajorSymbols)
		{
			Audio.play(Audio.soundMap(PERSISTING_MAJOR_SOUND_KEY));
		}
	}

	// Fades in the symbol and the
	protected virtual IEnumerator overlayExpandingSymbol(string name, float FADE_IN_TIME = 1.0f, float FADE_OUT_TIME = 1.0f)
	{
		if (!SlotSymbol.isMajorFromName(name))
		{
			// We only normaly expand major symbols by design
			yield break;
		}
		else if (string.IsNullOrEmpty(name))
		{
			Debug.LogError("Trying to make a null overlay symbol");
			yield break;
		}
		else if (findSymbolInfo(name) == null)
		{
			Debug.LogWarning("Couldn't find a symbol to expand with for " + name);
			yield break;
		}

		// Make the overlaySymbol
		SlotReel leftReel = engine.getSlotReelAt(0);
		// Make a new symbol in the same place as the first visible symbols.
		SlotSymbol overlaySymbol = new SlotSymbol(this);
		overlaySymbol.setupSymbol(name, engine.getVisibleSymbolsAt(0)[0].index, leftReel);
		// Fade in, animate, fade out, clean up.
		CommonGameObject.setLayerRecursively(overlaySymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		overlaySymbol.gameObject.name = "overlay_" + name;

		Vector2 symbolSize = overlaySymbol.getWidthAndHeightOfSymbol();
		fadeOutUnderSymbols(null, (int)symbolSize.x, (int)symbolSize.y, FADE_IN_TIME);
		// Set alpha map for overlaySymbol.
		overlaySymbol.fadeSymbolOutImmediate();
		Audio.playSoundMapOrSoundKey(expandingSymbolSound);
		yield return StartCoroutine(overlaySymbol.fadeInSymbolCoroutine(FADE_IN_TIME));

		// Check if this symbol needs to swap to an _Outcome version
		// find out if we have a custom bonus _Outcome symbol to swap in and play
		string symbolNameWithOutcomeExtension = SlotSymbol.constructNameFromDimensions(
			overlaySymbol.shortServerName + SlotSymbol.OUTCOME_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y
		);
		SymbolInfo info = findSymbolInfo(symbolNameWithOutcomeExtension);

		if (info != null && !overlaySymbol.isOutcomeSymbol && !isLegacyTumbleGame)   // tumble games do their own mutating
		{
			overlaySymbol.cleanUp();
			overlaySymbol.setupSymbol(symbolNameWithOutcomeExtension, engine.getVisibleSymbolsAt(0)[0].index, leftReel);
			CommonGameObject.setLayerRecursively(overlaySymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		}
		else if (isGameUsingOptimizedFlattenedSymbols)
		{
			// no outcome symbol, now check if this is a flattened version that isn't going to have animations on it, and swap it to a normal version if it is
			overlaySymbol.cleanUp();
			overlaySymbol.setupSymbol(overlaySymbol.serverName, engine.getVisibleSymbolsAt(0)[0].index, leftReel);
			CommonGameObject.setLayerRecursively(overlaySymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		}

		if (playSoundsOnExpandingSymbol)
		{
			playExpandingSymbolSounds(SlotSymbol.getShortServerNameFromName(name));
			outcomeDisplayController.isPlayingOutcomeAnimSoundsThisSpin = false;
		}

		overlaySymbol.animateOutcome();

		while (overlaySymbol.isAnimatorDoingSomething)
		{
			yield return null;
		}

		if (isGameUsingOptimizedFlattenedSymbols)
		{
			overlaySymbol.cleanUp();
			overlaySymbol.setupSymbol(name, engine.getVisibleSymbolsAt(0)[0].index, leftReel);
			CommonGameObject.setLayerRecursively(overlaySymbol.gameObject, Layers.ID_SLOT_FOREGROUND);
		}

		// If we want the overlay to actually be a symbol, mutate to create it.
		// Skip the fade since the symbol doesn't go away but still cleanup the overlay.
		if (persistingMajorSymbols)
		{
			engine.getVisibleSymbolsAt(0)[0].mutateTo(name);

			// we also need to link the reels, otherwise swipping the reel doesn't work correct
			SlotReel leftMostReel = engine.getSlotReelAt(0);
			for (int i = 1; i < symbolSize.x; ++i)
			{
				engine.linkReelsInLinkedReelsOverride(leftMostReel, engine.getSlotReelAt(i));
			}
		}
		else
		{
			fadeInUnderSymbols(null, (int)symbolSize.x, (int)symbolSize.y, FADE_OUT_TIME);
			yield return StartCoroutine(overlaySymbol.fadeOutSymbolCoroutine(FADE_OUT_TIME));
		}
		// Clean up
		overlaySymbol.cleanUp();
		isSkippingPreWinThisSpin = true;
	}
	
	// Get the number of sub outcomes that are in the outcome and will be used to determine
	// how displaying the outcome will proceed in doReelsStopped()
	public int getSubOutcomeCount()
	{
		if (_outcome == null)
		{
			Debug.LogError("ReelGame.getSubOutcomeCount() - Trying get suboutcome count when _outcome is NULL!");
			return 0;
		}
	
		int subOutcomeCount = 0;
		List<SlotOutcome> layeredOutcomes = _outcome.getReevaluationSubOutcomesByLayer();

		if (currentReevaluationSpin != null)
		{
			subOutcomeCount = currentReevaluationSpin.getSubOutcomesReadOnly().Count;
		}
		else
		{
			subOutcomeCount = _outcome.getSubOutcomesReadOnly().Count;
		}
		subOutcomeCount += layeredOutcomes.Count;

		return subOutcomeCount;
	}

	// Handle what is performed after the reels are stopped
	protected virtual IEnumerator doReelsStopped(bool isAllowingContinueWhenReadyToEndSpin = true)
	{
		int subOutcomeCount = getSubOutcomeCount();

		if (subOutcomeCount > 0)
		{
			// hide the sticky overlays during pay box stuff
			setStickyOverlaysVisible(false);

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
			if (subOutcomeCount > 0)
			{
				if (currentReevaluationSpin != null)
				{
					_outcomeDisplayController.displayOutcome(currentReevaluationSpin, true);
				}
				else
				{
					_outcomeDisplayController.displayOutcome(_outcome, true);
				}
			}
			else
			{
				yield return StartCoroutine(startNextReevaluationSpin());
			}
		}
		else
		{
			if (subOutcomeCount > 0 || engine.progressivesHit > engine.progressiveThreshold || mutationCreditsAwarded > 0)
			{
				if (currentReevaluationSpin != null)
				{
					_outcomeDisplayController.displayOutcome(currentReevaluationSpin, true);
				}
				else
				{
					_outcomeDisplayController.displayOutcome(_outcome, true);
				}
			}
			else if (hasFreespinsSpinsRemaining && engine.animationCount == 0 && !engine.effectInProgress)
			{
				// Check if onOutcomeSpinBlockRelease callback is going to start the next autospin itself, in which case don't start it here
				if (!_outcomeDisplayController.isSpinBlocked())
				{
					// clear the reevaluated spin if we were doing one
					_currentReevaluationSpin = null;
					startNextFreespin();
				}
			}
			else if (numberOfFreespinsRemaining == 0 && hasFreespinGameStarted)
			{
				gameEnded();
			}
		}
	}

	/// The free spins game ended.
	protected virtual void gameEnded()
	{
		StartCoroutine(waitForModulesThenEndGame());
	}

	protected virtual IEnumerator waitForModulesThenEndGame()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnFreespinGameEnd())
			{
				yield return StartCoroutine(module.executeOnFreespinGameEnd());
			}
		}
		BonusGamePresenter.instance.gameEnded();
	}

	// startNextAutospin - starts a spin and decrements that auto-spin counter.
	protected virtual void startNextAutospin()
	{

	}

	/// startReevaluationSpins - start reevaluation spins
	public virtual void startReevaluationSpins()
	{

	}

	/// Function to add sticky symbols to a list that will be cleaned up
	public void addStickySymbol(SlotSymbol stickySymbol)
	{
		reevalStickySymbols.Add(stickySymbol);
	}

	/// Handle the reevaulation sticky symbols
	protected virtual IEnumerator handleReevaluationStickySymbols(SlotOutcome passedOutcome)
	{
		if (passedOutcome != null)
		{
			Dictionary<int, Dictionary<int, string>> stickySymbols = passedOutcome.getStickySymbols();

			yield return StartCoroutine(handleStickySymbols(stickySymbols));
		}
	}

	protected virtual IEnumerator handleReevaluationStickySCSymbols(SlotOutcome passedOutcome)
	{
		if (passedOutcome != null)
		{
			Dictionary<int, Dictionary<int, string>> stickySCSymbols = passedOutcome.getStickySCSymbols();
			if (stickySCSymbols.Count != 0)
			{
			}
			yield return StartCoroutine(handleStickySymbols(stickySCSymbols));
		}
	}

	// Tells if a specific symbol location is covered by something like a sticky symbol or a module that might
	// do something like a sticky symbol or banner
	public bool isSymbolLocationCovered(SlotReel reel, int symbolIndex)
	{
		if (reel == null)
		{
			Debug.LogError("ReelGame.isSymbolLocationCovered() - Called with a NULL reel, this shouldn't happen!");
			return false;
		}

		int reelIndex = reel.reelID - 1;

		// compile a list of all built in sticky symbols
		Dictionary<int, Dictionary<int, string>> stickySymbolList = outcome.getStickySymbols();

		if (prevHandledReevalStickySymbols.Count != 0)
		{
			SlotReel[] reelArray = engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				if (prevHandledReevalStickySymbols.ContainsKey(i))
				{
					if (!stickySymbolList.ContainsKey(i))
					{
						stickySymbolList.Add(i, new Dictionary<int, string>());
					}

					for (int j = 0; j < reelArray[i].visibleSymbolsBottomUp.Count; j++)
					{
						if (prevHandledReevalStickySymbols[i].ContainsKey(j))
						{
							if (!stickySymbolList[i].ContainsKey(j))
							{
								stickySymbolList[i].Add(j, prevHandledReevalStickySymbols[i][j]);
							}
						}
					}
				}
			}
		}

		// for the sticky symbol stuff we need to deal with inverted visibile symbol indexes
		if (stickySymbolList.ContainsKey(reelIndex))
		{
			int visibleSymbolsLength = engine.getVisibleSymbolsAt(reelIndex).Length - 1;
			int invertedVisibleIndex = visibleSymbolsLength - (symbolIndex - reel.numberOfTopBufferSymbols);
			if (stickySymbolList[reelIndex].ContainsKey(invertedVisibleIndex))
			{
				return true;
			}
		}

		// if a built in sticky symbol isn't covering it, a module may still have something which is covering the symbol location
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];
			if (module.isSymbolLocationCovered(reel, symbolIndex))
			{
				return true;
			}
		}

		return false;
	}

	public virtual IEnumerator handleStickySymbols(Dictionary<int, Dictionary<int, string>> stickySymbols)
	{
		SlotReel[] reelArray = null;

		// Handle just mutating the reels to know what should already be stuck from previous spins
		if (prevHandledReevalStickySymbols.Count != 0)
		{
			reelArray = engine.getReelArray();
			for (int i = 0; i < reelArray.Length; i++)
			{
				if (prevHandledReevalStickySymbols.ContainsKey(i))
				{
					for (int j = 0; j < reelArray[i].visibleSymbolsBottomUp.Count; j++)
					{
						if (prevHandledReevalStickySymbols[i].ContainsKey(j))
						{
							SlotSymbol symbol = reelArray[i].visibleSymbolsBottomUp[j];
							if (symbol.name != prevHandledReevalStickySymbols[i][j])
							{
								// We only need to change the symbol if it's not already right.
								reelArray[i].visibleSymbolsBottomUp[j].mutateTo(prevHandledReevalStickySymbols[i][j]);
							}
						}
					}
				}
			}
		}

		if (stickySymbols != null && stickySymbols.Count != 0)
		{
			if (reelArray == null)
				reelArray = engine.getReelArray();

			for (int i = 0; i < reelArray.Length; i++)
			{
				if (stickySymbols.ContainsKey(i))
				{
					for (int j = 0; j < reelArray[i].visibleSymbolsBottomUp.Count; j++)
					{
						if (stickySymbols[i].ContainsKey(j))
						{
							// Get the name of the sticky symbol.
							string stickySymbolName = reelArray[i].visibleSymbolsBottomUp[j].serverName;
							if (stickySymbols[i][j] != "")
							{
								stickySymbolName = stickySymbols[i][j];
							}

							// store out what symbols are being stuck on this spin for future spins if needed
							if (!prevHandledReevalStickySymbols.ContainsKey(i))
							{
								prevHandledReevalStickySymbols.Add(i, new Dictionary<int, string>());
							}

							if (!prevHandledReevalStickySymbols[i].ContainsKey(j))
							{
								prevHandledReevalStickySymbols[i].Add(j, stickySymbolName);
							}
							else
							{
								prevHandledReevalStickySymbols[i][j] = stickySymbolName;
							}

							yield return StartCoroutine(changeSymbolToSticky(reelArray[i].visibleSymbolsBottomUp[j], stickySymbolName, j));
						}
					}
				}
			}
		}
	}

	// Handles modules that need to do things to symbols after they have all been advanced for a frame
	// for instance if a module in a layered game wanted to hide covered symbols while spinning
	// for instance like in aruze04
	public void doOnSlotEngineFrameUpdateAdvancedSymbolsModules()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnSlotEngineFrameUpdateAdvancedSymbols())
			{
				module.executeOnSlotEngineFrameUpdateAdvancedSymbols();
			}
		}
	}

	/// overridable function for handling a symbol becoming stuck on the reels, may become stuck as different symbol, passed in by stuckSymbolName
	protected virtual IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickySymbolName, int row)
	{

		bool playedModule = false;
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnChangeSymbolToSticky())
			{
				playedModule = true;
				yield return StartCoroutine(module.executeOnChangeSymbolToSticky(symbol, stickySymbolName));
			}
		}

		if (!playedModule)
		{
			symbol.mutateTo(stickySymbolName);
			// Add a stuck symbol
			SlotSymbol newSymbol = createSlotSymbol(stickySymbolName, symbol.index, symbol.reel);
			SymbolAnimator symbolAnimator = newSymbol.animator;
			if (symbolAnimator != null)
			{
				if (symbolAnimator.material != null)
				{
					symbolAnimator.material.shader = SymbolAnimator.defaultShader("Unlit/GUI Texture (+100)");
				}
				else
				{
					// This is a custom symbol
					//CommonGameObject.setLayerRecursively(symbolAnimator.gameObject, Layers.ID_SLOT_OVERLAY);
					symbolAnimator.addRenderQueue(100);
				}

				symbolAnimator.gameObject.name = "sticky_" + stickySymbolName;
				addStickySymbol(newSymbol);
			}
			else
			{
				Debug.LogWarning("No symbols animator for " + stickySymbolName);
			}
		}
	}

	public SlotSymbol createSlotSymbol(string symbolName, int index, SlotReel reel)
	{
		SlotSymbol newSymbol = new SlotSymbol(this);
		newSymbol.setupSymbol(symbolName, index, reel);
		return newSymbol;
	}

	public SlotSymbol createStickySymbol(string stickySymbolName, int index, SlotReel reel)
	{
		SlotSymbol symbol = createSlotSymbol(stickySymbolName, index, reel);
		
		// may want to add optional flag to disable these layering calls in the future, but currently all stickies use this
		CommonGameObject.setLayerRecursively(symbol.animator.gameObject, Layers.ID_SLOT_REELS_OVERLAY);
		symbol.animator.addRenderQueue(100);
		
		symbol.animator.gameObject.name = "sticky_" + stickySymbolName;
		addStickySymbol(symbol);
		return symbol;
	}

	/// Hide the sticky overlays which cover the reels while they spin
	public void setStickyOverlaysVisible(bool isVisible)
	{
		foreach (SlotSymbol stickyOverlay in reevalStickySymbols)
		{
			stickyOverlay.gameObject.SetActive(isVisible);
		}
	}

	/// Cleanup the sticky symbols when a new spin start
	protected void clearReevalStickySymbolInfo()
	{
		setStickyOverlaysVisible(false);

		prevHandledReevalStickySymbols.Clear();

		for (int i = 0; i < reevalStickySymbols.Count; i++)
		{
			reevalStickySymbols[i].cleanUp();
		}
		reevalStickySymbols.Clear();
	}

	/// Allows any sort of cleanup that may need to happen on the symbol animator
	protected virtual void preReleaseStickySymbolAnimator(SymbolAnimator animator)
	{
		// override to handle stuff to do before release
	}

	/// Allows for the caching of a number of the same symbol
	public void cacheSymbolsToPool(string symbolName, int symbolCount, bool useCoroutineVersion = true)
	{
		if (useCoroutineVersion)
		{
			// Spread the caching out over multiple frames
			StartCoroutine(cacheSymbolsToPoolCoroutine(symbolName, symbolCount));
		}
		else
		{
			// Cache flattened version if the game is using flattened symbols
			SymbolInfo info;
			string finalSymbolName = symbolName;

			if (isGameUsingOptimizedFlattenedSymbols)
			{
				Vector2 symbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolName);
				finalSymbolName = SlotSymbol.constructNameFromDimensions(SlotSymbol.getShortNameFromName(symbolName) + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);

				// double check we have a template to create from, otherwise default back to non flattened
				info = findSymbolInfo(finalSymbolName);
				if (info == null)
				{
					// no template match, using the original name
					finalSymbolName = symbolName;
				}
			}

			// make sure we have a name that links to a symbol template otherwise bad stuff will happen
			info = findSymbolInfo(finalSymbolName);
			if (info != null)
			{
				// Do everything on the same frame, may lockup the game if you cache too much!
				for (int i = 0; i < symbolCount; i++)
				{
					SymbolAnimator createdSymbol = getSymbolAnimatorInstance(finalSymbolName, -1, true);
					releaseSymbolInstance(createdSymbol);
				}
			}
			else
			{
				Debug.LogWarning("Unable to cache: " + finalSymbolName + " because there isn't a symbol template for it!");
			}

			//logPoolInfo();
		}
	}

	/// A coroutine version of caching so it doesn't block program execution
	public IEnumerator cacheSymbolsToPoolCoroutine(string symbolName, int symbolCount, int numberOfSymbolsAtOnce = 1)
	{
		if (numberOfSymbolsAtOnce <= 0)
		{
			numberOfSymbolsAtOnce = symbolCount;
		}
		if (symbolCount <= 0)
		{
			yield break;
		}
		int timesToLoop = ((symbolCount - 1) / numberOfSymbolsAtOnce) + 1;

		// Cache flattened version if the game is using flattened symbols
		SymbolInfo info;
		string finalSymbolName = symbolName;
		bool isGameUsingOptimizedFlattenedSymbols = SlotResourceMap.isGameUsingOptimizedFlattenedSymbols(GameState.game.keyName);
		if (isGameUsingOptimizedFlattenedSymbols)
		{
			Vector2 symbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolName);
			finalSymbolName = SlotSymbol.constructNameFromDimensions(SlotSymbol.getShortNameFromName(symbolName) + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);

			// double check we have a template to create from, otherwise default back to non flattened
			info = findSymbolInfo(finalSymbolName);
			if (info == null)
			{
				// no template match, using the original name
				finalSymbolName = symbolName;
			}
		}

		// make sure we have a name that links to a symbol template otherwise bad stuff will happen
		info = findSymbolInfo(finalSymbolName);
		if (info != null)
		{
			for (int i = 0; i < timesToLoop; i++)
			{
				for (int j = 0; j < numberOfSymbolsAtOnce; j++)
				{
					SymbolAnimator createdSymbol = getSymbolAnimatorInstance(finalSymbolName, -1, true);
					releaseSymbolInstance(createdSymbol);
				}
				// don't make this block
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning("Unable to cache: " + finalSymbolName + " because there isn't a symbol template for it!");
		}

		//slotSymbolCache.logCacheInfo();
	}

	/// Gets a symbol instance, possibly from a recycled pool.
	public virtual SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		Profiler.BeginSample("getSymbolInstance");
		validateSymbolMapMade();

		SymbolAnimator symbol = null;
		SymbolInfo info = findSymbolInfo(name, canSearchForMegaIfNotFound);
		if (info != null)
		{
			bool isFlattened = SlotSymbol.isFlattenedSymbolFromName(name);

#if UNITY_EDITOR
			if (slotSymbolCache != null && !slotSymbolCache.isCachingSymbol(info, isFreeSpinGame()))
			{
				Debug.LogWarning("ReelGame.getSymbolInstance() - The following symbol is not being cached! name = " + name
					+ "; isFreeSpinGame() = " + isFreeSpinGame());
			}
#endif

			if (slotSymbolCache != null && slotSymbolCache.isCachedInstanceAvailable(info, isFlattened) && !forceNewInstance)
			{
				// Use a recycled symbol instance
				symbol = slotSymbolCache.getCachedInstance(info, isFlattened);

				// Need to set the symbol info every time we pull a symbol out of the cache because
				// we don't know whether it will have basegame or freespins info stored form the last
				// time it was used.
				symbol.info = info;
				// Update the name info as well, since multiple names may be sharing the same Animator
				// pool, but we'd like the name to be what the symbol is currently.
				symbol.symbolInfoName = name;
				symbol.gameObject.name = "Symbol " + name + ((info.symbol3d != null) ? " 3D" : "");
			}
			else
			{
				// Make a new instance

				// Check to see if we should be using the 3d template or the 2d template.
				if (info.symbol3d == null)
				{
					if (_symbolTemplate == null)
					{
						_symbolTemplate = SkuResources.getObjectFromMegaBundle<GameObject>(SYMBOL_TEMPLATE_PATH2D);
					}
				}

				// Attempt to instantiate the right version of the symbol template
				if ((info.symbol3d != null) ||
					(info.symbol3d == null && _symbolTemplate != null))
				{
					// Pick between the 2d or the 3d template
					GameObject symbolObject = CommonGameObject.instantiate((info.symbol3d != null) ? info.symbol3d : _symbolTemplate) as GameObject;

					symbol = symbolObject.GetComponent<SymbolAnimator>();

					if (symbol == null)
					{
						// Cleanup a bad symbol
						Debug.LogWarning("Symbol prefab is missing SymbolAnimator: " + ((info.symbol3d != null) ? SYMBOL_TEMPLATE_PATH3D : SYMBOL_TEMPLATE_PATH2D));
						Destroy(symbolObject);
					}
					else
					{
						symbol.info = info;
						symbol.symbolInfoName = name;
						symbol.gameObject.name = "Symbol " + name + ((info.symbol3d != null) ? " 3D" : "");

						// Provide our ReelGame-specific symbolBoundsCache for the SymbolAnimator to use
						symbol.symbolBoundsCache = _symbolBoundsCache;
					}
				}
				else
				{
					Debug.LogError("Unable to find symbol prefab: " + ((info.symbol3d != null) ? SYMBOL_TEMPLATE_PATH3D : SYMBOL_TEMPLATE_PATH2D));
				}
			}
		}
#if UNITY_EDITOR
		else
		{
			// Log a warning if the symbol wasn't found in the game's symbol definitions,
			// but ignore the warning if it's not the first row in a multi-row symbol.
			bool shouldWarn = true;
			if (name.Contains('-'))
			{
				string[] parts = name.Split('-');

				for (int i = 1; i < parts.Length; i++)
				{
					if (parts[i].Length > 1 && parts[i].Substring(1, 1) != "A")
					{
						shouldWarn = false;
						break;
					}
				}
			}

			if (SlotSymbol.isReplacementSymbolFromName(name) || SlotSymbol.isBlankSymbolFromName(name) || name == "")
			{
				// if this is a replacement symbol or blank symbol ignore warning, those don't have visual symbol instances
				shouldWarn = false;
			}

			if (shouldWarn)
			{
				Debug.LogWarning("Could not find symbol definition on game: " + name);
			}
		}
#endif

		if (symbol != null)
		{
			// handle per game symbol vs all symbol overrides for wild texture
			if (symbol.info != null && symbol.info.wildTexture != null)
			{
				symbol.wildTexture = symbol.info.wildTexture;
				symbol.wildHidesSymbol = symbol.info.wildHidesSymbol;
			}
			else
			{
				symbol.wildTexture = wildTexture;
				symbol.wildHidesSymbol = wildHidesSymbol;
			}

			// handle per game symbol vs all symbol overrides for wild overlay game objects
			if (symbol.info != null && symbol.info.wildOverlayGameObject != null)
			{
				symbol.wildOverlayGameObjectPrefab = symbol.info.wildOverlayGameObject;
				symbol.wildHidesSymbol = symbol.info.wildHidesSymbol;
				symbol.disableWildOverlayGameObject = symbol.info.disableWildOverlayGameObject;
			}
			else
			{
				symbol.wildOverlayGameObjectPrefab = wildOverlayGameObject;
				symbol.wildHidesSymbol = wildHidesSymbol;
				if (symbol.info != null)
				{
					symbol.disableWildOverlayGameObject = symbol.info.disableWildOverlayGameObject;
				}
			}

			// Run symbol activation code if applicable
			bool isFlattened = SlotSymbol.isFlattenedSymbolFromName(name);
			symbol.activate(isFlattened);

			// The symbol is legit, populate its data
			this.handleSymbolAnimatorCreated(symbol);
		}

		Profiler.EndSample();
		return symbol;
	}

	private void symbolTemplateLoadSuccess(string path, Object obj, Dict data = null)
	{
		_symbolTemplate = obj as GameObject;
	}

	private void symbolTemplateLoadFailure(string path, Dict args = null)
	{
		Debug.LogError("Failed to load the symbol template prefab from path: " + path);
	}

	/// Releases a symbol instance into its respective pool.
	public void releaseSymbolInstance(SymbolAnimator symbol)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReleaseSymbolInstance())
			{
				module.executeOnReleaseSymbolInstance(symbol);
			}
		}

		symbol.deactivate();
		validateSymbolMapMade();
		string symbolName = symbol.symbolInfoName;
		// Check to see if this is a flattend symbol
		if (symbol.isFlattened)
		{
			Vector2 symbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolName);
			string shortServerNameWithVariant = SlotSymbol.getShortServerNameWithVariant(symbolName);
			symbolName = SlotSymbol.constructNameFromDimensions(
				shortServerNameWithVariant + SlotSymbol.FLATTENED_SYMBOL_POSTFIX,
				(int)symbolSize.x,
				(int)symbolSize.y
			);
		}

		// If this is a game using layers or independent reel game make sure we store the
		// symbol back on the normal SLOT_REEL layer to ensure that if another reel sharing
		// symbol cache with this one uses a basic reel type it will still be able to 
		// render the symbols when pulling them from the cache.
		if (engine is LayeredSlotEngine || engine.reelSetData.isIndependentReels)
		{
			CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS);
		}
		
		// Only change the render queue if the setting is on which modifies it on the symbols
		// otherwise leave render queue alone.
		if (isLayeringOverlappingSymbols)
		{
			symbol.changeRenderQueue(symbol.getBaseRenderLevel());
		}

		bool tempIsFreeSpinGame = isFreeSpinGame();
		if (slotSymbolCache != null && slotSymbolCache.isCachingSymbol(symbol.info, tempIsFreeSpinGame) && !symbol.skipSymbolCaching)
		{
			bool isFlattened = SlotSymbol.isFlattenedSymbolFromName(symbolName);
			bool wasCached = slotSymbolCache.releaseSymbolToCache(symbol, isFlattened, tempIsFreeSpinGame);
			if (!wasCached)
			{
				// Destroy the GameObject the symbol is attached to because we weren't able to cache it
				Destroy(symbol.gameObject);
			}
		}
		else
		{
			// Destroy the GameObject the symbol is attached to.
			Destroy(symbol.gameObject);
		}
	}

	/// Returns symbol info for a particular symbol on this game.
	/// Only the base symbol name is passed in, so multi-line symbols always check for the "A" symbol.
	public SymbolInfo findSymbolInfo(string keyName, bool canSearchForMegaIfNotFound = false)
	{
		validateSymbolMapMade();

		SymbolInfo symbolInfo;
		if (_symbolMap.TryGetValue(keyName, out symbolInfo))
		{
			return symbolInfo;
		}

		if (canSearchForMegaIfNotFound)
		{
			// No direct match, look for multi-row symbols that might match.
			foreach (SymbolInfo info in symbolTemplates)
			{
				ReadOnlyCollection<string> possibleSymbolNames = info.getNameArrayReadOnly();
				foreach (string name in possibleSymbolNames)
				{
					if (name.Contains('-'))
					{
						// If a multi-row symbol, check for it matching and ending in A.
						string[] parts = name.Split('-');
						if (parts[0] == keyName && parts[1].Substring(1, 1) == "A")
						{
							return info;
						}
					}
				}
			}
		}

		return null;
	}

	/// Returns the base symbol info for a particular symbol on this game.
	/// Only the base symbol will be returned, if a sub symbol in a multi symbol is passed (i.e M1-4B") it will return the base (e.e "M1-4A").
	public SymbolInfo findBaseSymbolInfo(string keyName)
	{
		if (keyName.Contains('-'))
		{
			string[] keyParts = keyName.Split('-');
			// Break apart the symbol and if it's a high symbol then return the top most symbols i.e M1-2A
			if (char.IsNumber(keyParts[1][0]))
			{
				keyName = keyParts[0] + "-" + keyParts[1].Substring(0, keyParts[1].Length - 1) + "A";
			}
			else
			{
				keyName = keyParts[0];
			}
		}
		return findSymbolInfo(keyName);
	}

	/// Mutates all visible instances of the given symbol to the other given symbol.
	/// Returns the last symbol that was animated, so you can check .isAnimating
	/// to know when the symbols are finished mutating.
	protected SlotSymbol mutateAll(string fromSymbol, string toSymbol)
	{
		SlotSymbol animatingSymbol = null;
		foreach (SlotReel reel in engine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (SlotUtils.getBaseSymbolName(symbol.name) == fromSymbol)
				{
					// Found a symbol to change.
					symbol.mutateTo(toSymbol);
					animatingSymbol = symbol;
				}
			}
		}
		return animatingSymbol;
	}

	/// Turns on the wild overlay for all visible instances of the given symbol.
	public void showWilds(string fromSymbol, int excludeColumn = -1)
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			foreach (SlotSymbol symbol in reelArray[i].visibleSymbols)
			{
				if (symbol.animator != null && symbol.serverName == fromSymbol && excludeColumn != i)
				{
					// Found a symbol to change.
					symbol.animator.showWild();
				}
			}
		}
	}

	/// Allows derived classes to define when to use a feature specific feature anticipation
	public virtual string getFeatureAnticipationName()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToGetFeatureAnicipationNameFromModule())
			{
				return module.getFeatureAnticipationNameFromModule();
			}
		}
		return "";
	}

	//Called when a new SymbolAnimator is instantiated
	protected virtual void handleSymbolAnimatorCreated(SymbolAnimator symbol)
	{
	}

	///Gets a collection of all SymbolAnimators
	protected IEnumerable<SymbolAnimator> getAllCachedSymbolAnimators()
	{
		validateSymbolMapMade();
		return slotSymbolCache.getAllCachedSymbolAnimators();
	}


	// Clears the symbol cache
	public void clearSymbolCache()
	{
		if (slotSymbolCache != null)
		{
			slotSymbolCache.clearSymbolCache();
		}
	}

	public void createCachedSymbols(List<SymbolInfo> symbol, bool isFreespinTemplate = false)
	{
		if (slotSymbolCache != null)
		{
			slotSymbolCache.createEntriesInSymbolCache(symbol, isFreespinTemplate);
		}
	}

	// Clears the symbol map
	public void clearSymbolMap()
	{
		if (_symbolMap != null)
		{
			_symbolMap.Clear();
			_symbolMap = null;
		}
	}

	// Clears the symbol bounds cache
	public void clearSymbolBoundsCache()
	{
		if (_symbolBoundsCache != null)
		{
			_symbolBoundsCache.Clear();
			_symbolBoundsCache = null;
		}
	}

	/// Clear the currently displayed outcomes.
	public void clearOutcomeDisplay()
	{
		// Handle on clearOutcomeDisplay modules, note that this module
		// hook is not a coroutine since it isn't really safe in all
		// cases to have this cause the game to block.
		// Ideally this hook should only be used for basic cleanup
		// that needs to happen at the same time that the outcome display
		// is cleaned.
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnClearOutcomeDisplay())
			{
				module.executeOnClearOutcomeDisplay();
			}
		}
		
		_outcomeDisplayController.clearOutcome();
		
		// Always halt all symbol animations here.  This ensures that any animations tied to
		// symbol payouts get stopped, as well as any animations that aren't related to symbol
		// payouts, like anticipation landing animations.
		foreach (SlotReel reel in engine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				symbol.haltAnimation();
			}
		}
	}

	/// Handle a reevluation spin whose data was sent down in the original spin outcome
	protected virtual IEnumerator startNextReevaluationSpin()
	{
		clearOutcomeDisplay();

		// save out callback info so it can be restored
		hadRevalsLastSpin = true; // Setting this for the next spin.
		if (_oldReelStoppedCallback == null)
		{
			_oldReelStoppedCallback = engine.getReelsStoppedCallback();
		}

		setStickyOverlaysVisible(true);

		engine.setReelsStoppedCallback(reevaluationReelStoppedCallback);
		_currentReevaluationSpin = reevaluationSpins[reevaluationSpins.Count - reevaluationSpinsRemaining];

		// setup mutations for this reevaluation (which could include replacement symbols)
		mutationManager.setMutationsFromOutcome(_currentReevaluationSpin.getJsonObject(), hasMutationsInReeval);

		// reevaluation spins may change the reel set
		if (currentReevaluationSpin.getReelSet() != currentReelSetName)
		{
			setReelSet(currentReevaluationSpin.getReelSet());
		}

		// Set the replacement symbols for this reevaluation from the replacements in the outcome
		Dictionary<string, string> outcomeNormalReplacementMap = currentReevaluationSpin.getNormalReplacementSymbols();
		Dictionary<string, string> outcomeMegaReplacementMap = currentReevaluationSpin.getMegaReplacementSymbols();
		// Note that we are checking these and if they aren't set we will just leave them be, since
		// they should be cleared before the next spin.  If we ever need replacements to come from
		// both mutations and the spin outcome root we will have to rethink this slightly.
		if (outcomeNormalReplacementMap.Count > 0 || outcomeMegaReplacementMap.Count > 0)
		{
			engine.setReplacementSymbolMap(outcomeNormalReplacementMap, outcomeMegaReplacementMap, isApplyingNow: true);
		}

		// Get the scatter stickies for this reevaulation
		StartCoroutine(handleReevaluationStickySCSymbols(currentReevaluationSpin));
		
		// Reset the symbol overrides that were set in the last spin, unless a module is going to 
		// handle clearing them at some other point.
		if (!isHandlingSlotReelClearSymbolOverridesWithModule())
		{
			engine.clearSymbolOverridesOnAllReels();
		}

		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnReevaluationPreSpin())
			{
				yield return StartCoroutine(module.executeOnReevaluationPreSpin());
			}
		}
		
		// Check for and update reel stop symbol overrides
		setupReelSymbolOverrides(currentReevaluationSpin);
		
		// Play a sound for the reels spinning for the respin if one is defined here
		string spinReelRespinAudioValue = "";
		if (isFreeSpinGame() || isDoingFreespinsInBasegame())
		{
			// Check if we need to use a freespin version
			if (Audio.canSoundBeMapped(SPIN_REEL_RESPIN_FREESPIN_SOUND_KEY))
			{
				spinReelRespinAudioValue = Audio.soundMap(SPIN_REEL_RESPIN_FREESPIN_SOUND_KEY);
			}
		}

		if (spinReelRespinAudioValue == "")
		{
			// Check if the standard respin spin version can be mapped
			if (Audio.canSoundBeMapped(SPIN_REEL_RESPIN_SOUND_KEY))
			{
				spinReelRespinAudioValue = Audio.soundMap(SPIN_REEL_RESPIN_SOUND_KEY);
			}
		}

		if (spinReelRespinAudioValue != "")
		{
			Audio.play(spinReelRespinAudioValue);
		}

		engine.spinReevaluatedReels(currentReevaluationSpin);
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnReevaluationSpinStart())
			{
				yield return StartCoroutine(module.executeOnReevaluationSpinStart());
			}
		}

		reevaluationSpinsRemaining--;

		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToExecuteOnReevaluationPreReelsStopSpinning())
			{
				yield return StartCoroutine(module.executeOnReevaluationPreReelsStopSpinning());
			}
		}

		yield return StartCoroutine(autoStopReevaluatioinSpin());
	}

	protected IEnumerator showAdditionalInformation()
	{
		// The additionalInfo must be pre-localized.
		if (additionalInfo != "")
		{
			yield return new WaitForSeconds(1f);
			BonusSpinPanel.instance.messageLabel.text = additionalInfo;
		}
	}
	
	// Play a sound when the reel spin starts.
	protected virtual void playFreespinSpinSound()
	{
		// Note: Freespin in base will skip this initial sound right now and always use
		// FREESPIN_ALREADY_SPIN_SOUND.  That is because isFirstSpin is not reset when starting
		// freespins in base, we can look more into that later if we really want to have this first
		// spin sound play.  For actual FreeSpinGame based classes this will work as expected.
		if (isFirstSpin)
		{
			if (Audio.canSoundBeMapped(FREESPIN_FIRST_SPIN_SOUND))
			{
				Audio.playSoundMapOrSoundKey(FREESPIN_FIRST_SPIN_SOUND);
			}
		}
		else if (Audio.canSoundBeMapped(FREESPIN_ALREADY_SPIN_SOUND))
		{
			Audio.playSoundMapOrSoundKey(FREESPIN_ALREADY_SPIN_SOUND);			
		}
	}

	protected virtual void startNextFreespin()
	{
		lastPayoutRollupValue = 0;
		if (hasFreespinsSpinsRemaining)
		{
			BonusSpinPanel.instance.slideOutPaylineMessageBox();

			if (numberOfFreespinsRemaining == 1)
			{
				// Joe requested to remove the "good luck" text, which was localization key "last_spin_good_luck". HIR-3821
				BonusSpinPanel.instance.messageLabel.text = Localize.text("last_spin");
			}
			else
			{
				if (additionalInfo == "")
				{
					BonusSpinPanel.instance.messageLabel.text = Localize.text("good_luck");
				}
			}
			
			playFreespinSpinSound();

			if (!endlessMode)
			{
				numberOfFreespinsRemaining--;
			}

			StartCoroutine(startNextFreespinCoroutine());

			StartCoroutine(showAdditionalInformation());

		}
		else if (hasFreespinGameStarted)
		{
			gameEnded();
		}
	}

	protected IEnumerator startNextFreespinCoroutine()
	{
		clearOutcomeDisplay();

		yield return StartCoroutine(prespin());
		if (isFirstSpin && playFreespinsInBasegame)
		{
			isFirstSpin = false;
			//Before the first spin we dont want to start spinning until after the transistion is complete
			yield return new TIWaitForSeconds(FREESPINS_TRANSISTION_TIME);
		}

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

		// Reset to default if not using it
		if (currentReelSetName != defaultReelSetName)
		{
			setReelSet(defaultReelSetName);
		}

		engine.spinReels();

		yield return StartCoroutine(reelsSpinning());

		// clear reevaluationSpins now that they should all be handled
		reevaluationSpins.Clear();

		_outcome = _freeSpinsOutcomes.getNextEntry();

		// if reelset update is needed, perform it prior to the spin landing
		if (!string.IsNullOrEmpty(_outcome.getReelSet()) && currentReelSetName != _outcome.getReelSet())
		{
			setReelSet(_outcome.getReelSet());
		}

		// swap paytable if necessary
		if (!string.IsNullOrEmpty(_outcome.getPayTable()) && engine.freeSpinsPaytableKey != _outcome.getPayTable())
		{
			engine.setFreespinsPaytableKey(_outcome.getPayTable());
		}

		setOutcome(_outcome);
		engine.setOutcome(_outcome);
	}

	//This will need to be filled in to remove SlotBaseGame/FreeSpinGame
	protected virtual void onOutcomeSpinBlockRelease()
	{

	}

	/// reevaluationReelStoppedCallback - called when all reels stop, only on reevaluated spins
	protected virtual void reevaluationReelStoppedCallback()
	{

	}

	// Sets the winnings display amount to zero.
	protected void zeroWinningsDisplay()
	{
		setWinningsDisplay(0);
	}

	// Sets the winnings display amount to zero after the passed delay.
	protected IEnumerator zeroWinningsDisplayAfterDelay(float delay)
	{
		if (delay > 0)
		{
			yield return new TIWaitForSeconds(delay);
		}

		zeroWinningsDisplay();
	}

	// Sets the winning display value in the spin panel as well as the big win effect if applicable
	public virtual void setWinningsDisplay(long amount)
	{
		if (!hasFreespinGameStarted)
		{
			SpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(amount);
		}
		else
		{
			BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(amount);
		}
	}

	public void onPayoutRollup(long payoutValue)
	{
		setWinningsDisplay(payoutValue + runningPayoutRollupValue);
		lastPayoutRollupValue = payoutValue;
	}

	// Rollup the winnings from a bonus which isn't the last one in the queue (since that one can rollup with the base game)
	// For this we will rollup, and then trigger the next bonus
	public IEnumerator rollupBonusWinBeforeStartingNextQueuedBonus()
	{
		if (_outcome.hasQueuedBonuses)
		{
			long rollupStart = runningPayoutRollupValue;
			long bonusPayout = outcomeDisplayController.calculateBonusPayout();
			long rollupEnd = bonusPayout;

			yield return StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, onPayoutRollup));

			// reset the final payout since we've rolled up this winning value now
			BonusGameManager.instance.finalPayout = 0;

			// trigger the end rollup
			yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));

			StartCoroutine(SlotBaseGame.instance.triggerQueuedBonusGames());
		}
		else
		{
			Debug.LogError("ReelGame.rollupBonusWinBeforeStartingNextQueuedBonus() - Called when bonuses weren't queued or when this was the last bonus in the queue, this shouldn't happen!");
		}
	}

	protected virtual void onBigWinNotification(long payout, bool isSettingStartingAmountToPayout = false)
	{

	}

	// This function is called inside of onEndRollup functions in order to move the payout that was awarded into the runningPayoutRollupValue
	protected void moveLastPayoutIntoRunningPayoutRollupValue()
	{
		runningPayoutRollupValue += lastPayoutRollupValue;
		lastPayoutRollupValue = 0;

		// If freespins in base is happening, then we need to update the value we have stored for the bonus game presenter that will award once this is over
		if (hasFreespinGameStarted)
		{
			if (playFreespinsInBasegame)
			{
				freespinsInBasegamePresenter.currentPayout = runningPayoutRollupValue;
			}
			else
			{
				FreeSpinGame.instance.bonusGamePresenter.currentPayout = runningPayoutRollupValue;
			}
		}
	}

	public virtual IEnumerator onEndRollup(bool isAllowingContinueWhenReady, bool isAddingRollupToRunningPayout = true)
	{
		if (isAddingRollupToRunningPayout)
		{
			moveLastPayoutIntoRunningPayoutRollupValue();
		}
		else
		{
			lastPayoutRollupValue = 0;
		}

		yield break;
	}

	/// Handle a coroutine after a reevaluation spin stops
	protected virtual IEnumerator handleReevaluationReelStop()
	{
		yield return StartCoroutine(waitForModulesAtReevaluationReelsStoppedCallback());

		bool isAllowingContinueWhenReadyToEndSpin = true;
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			isAllowingContinueWhenReadyToEndSpin &= cachedAttachedSlotModules[i].onReelStoppedCallbackIsAllowingContinueWhenReadyToEndSpin();
		}

		yield return StartCoroutine(doReelsStopped(isAllowingContinueWhenReadyToEndSpin));
	}

	private IEnumerator waitForModulesAtReevaluationReelsStoppedCallback()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnReevaluationReelsStoppedCallback())
			{
				yield return StartCoroutine(module.executeOnReevaluationReelsStoppedCallback());
			}
		}
	}

	protected IEnumerator waitForModulesAfterPaylines(bool winsShown)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteAfterPaylines())
			{
				yield return StartCoroutine(module.executeAfterPaylinesCallback(winsShown));
			}
		}
	}

	public void doModulesOnPaylines(bool winsShown, TICoroutine rollupCoroutine = null)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPaylinesPayoutRollup())
			{
				module.executeOnPaylinesPayoutRollup(winsShown, rollupCoroutine);
			}
		}
	}

	// Allows something like another module to grab the BuiltInProgressiveJackpotBaseGameModule which
	// contains a lot of data about what jackpot is currently selected and what labels are tied to it.
	// Useful to use this instead of having to recreate a lot of what it does in another module.
	public BuiltInProgressiveJackpotBaseGameModule getBuiltInProgressiveJackpotBaseGameModule()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			BuiltInProgressiveJackpotBaseGameModule progJackpotModule = module as BuiltInProgressiveJackpotBaseGameModule;
			if (progJackpotModule != null)
			{
				return progJackpotModule;
			}
		}

		return null;
	}

	/// Handle the automatic stopping of a reevaluation spin after a set amount of time
	protected IEnumerator autoStopReevaluatioinSpin()
	{
		if (outcome.getTumbleOutcomes().Length == 0) //Don't want this wait in tumble games or else we get a pause between tumbles and paylines
		{
			yield return StartCoroutine(artificialSpinWait());
		}

		engine.stopReels();
	}

	protected IEnumerator artificialSpinWait()
	{
		// unskipppable time first, need this so the reels appear to spin a bit
		yield return new WaitForSeconds(UNSKIPPABLE_REEVALUTION_SPIN_TIME);
		float timer = 0;

		// Wait for a default simulated spin time, but allow a slam stop
		while (timer < REEVALUATION_SPIN_STOP_TIME && !engine.isSlamStopPressed)
		{
			timer += Time.deltaTime;
			yield return null;
		}
	}

	// Allows the game to know when a reel is stopping.
	public virtual void onSpecificReelStopping(SlotReel stoppingReel)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnSpecificReelStopping(stoppingReel))
			{
				module.executeOnSpecificReelStopping(stoppingReel);
			}
		}
	}

	/// Allows game to respond to the stopping of a specific reel
	public void onSpecificReelStop(SlotReel stoppedReel)
	{
		StartCoroutine(handleSpecificReelStop(stoppedReel));
	}

	protected virtual IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnSpecificReelStop(stoppedReel))
			{
				yield return StartCoroutine(module.executeOnSpecificReelStop(stoppedReel));
			}
		}
		yield break;
	}

	/// Resetting the animations on all symbols so that when coming back from a bonus game all symbols 
	/// that were part way through an animation before leaving will now be displayed properly.
	protected override void OnEnable()
	{
		base.OnEnable();

		if (engine != null && engine.isReelArraySetup())
		{
			List<SlotSymbol> allVisibleSymbols = engine.getAllVisibleSymbols();
			for (int i = 0; i < allVisibleSymbols.Count; i++)
			{
				SlotSymbol symbol = allVisibleSymbols[i];

				if (symbol != null)
				{
					symbol.haltAnimation(true);
				}
			}
		}
	}

	// Tells if the passed in BonusGamePresenter matches the one set for freespinsInBasegamePresenter.
	// Right now this is being used to check if a stacked bonus state on BonusGameManager.bonusGameStack
	// is a freespin in base game.
	public bool isBonusGamePresenterFreespinInBasePresenter(BonusGamePresenter presenterToCheck)
	{
		return presenterToCheck == freespinsInBasegamePresenter;
	}

	/// Virtual function for games that need to do special mutations on outcome display (called only from 
	/// ClusterOutcomeDisplayModule as of now because only zom01 uses this at the moment
	public virtual void mutateSymbolOnOutcomeDisplay(string symbol)
	{
	}

	/// return a set of symbol template names that can be cross checked with data
	public HashSet<string> getListOfSymbolTemplateNames()
	{
		HashSet<string> symbolsUsedByReelGame = new HashSet<string>();

		foreach (SymbolInfo symbolInfo in symbolTemplates)
		{
			ReadOnlyCollection<string> possibleSymbolNames = symbolInfo.getNameArrayReadOnly();
			foreach (string name in possibleSymbolNames)
			{
				symbolsUsedByReelGame.Add(name);
			}
		}

		return symbolsUsedByReelGame;
	}


	// Tells if a game uses synced reels in which case the starting and stopping needs to be
	// synchronized. We can just grab this information from the ReelData now.
	public virtual bool isGameWithSyncedReels()
	{

		foreach (SlotReel reel in engine.getReelArray())
		{
			if (reel.reelSyncedTo > -1)
			{
				return true;
			}
		}

		return false;
	}

	// Return a set with every linked reel index (if reels 3 & 4 are linked to reel 2, set has {2, 3, 4})
	public HashSet<int> getSyncedReelsIndices()
	{
		HashSet<int> syncedReelIndecies = new HashSet<int>();

		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			SlotReel reel = reelArray[i];
			if (reel.reelSyncedTo > -1)
			{
				syncedReelIndecies.Add(reel.reelSyncedTo - 1); // Add the reel it is linked to, adjust for stupid 1-indexed SCAT data
				syncedReelIndecies.Add(i); // Add this reel itself
			}
		}

		return syncedReelIndecies;
	}

	/// Hooks up modules that function off of a payline being shown, using the module funciton executeOnPaylineDisplay()
	public IEnumerator onPaylineDisplayed(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPaylineDisplay())
			{
				yield return StartCoroutine(module.executeOnPaylineDisplay(outcome, lineWin, paylineColor));
			}
		}
	}

	/// Hooks up modules that function off of a payline being hidden, using the module funciton executeOnPaylineHide()
	public IEnumerator onPaylineHidden(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPaylineHide(symbolsAnimatedDuringCurrentWin))
			{
				yield return StartCoroutine(module.executeOnPaylineHide(symbolsAnimatedDuringCurrentWin));
			}
		}
	}
	[System.Serializable]
	// A class that holds reel information that we use to determine the order that we want to stop the reels in.
	public class StopInfo
	{
		public int layer = 0;
		public int reelID;
		public int row = 0;
		public StopInfo(int reelID, int row = 0, int layer = 0)
		{
			this.reelID = reelID;
			this.layer = layer;
			this.row = row;
		}
	}

	//Handles the overlay symbol on the reels
	public IEnumerator doOverlay()
	{
		if (engine.getVisibleSymbolsAt(0)[0] != null && engine.getVisibleSymbolsAt(0)[0].animator != null 
			&& engine.getVisibleSymbolsAt(0)[0].animator.info.expandingSymbolOverlay != null)
		{
			SlotReel[] reelArray = engine.getReelArray();
			Vector2int overlaySize = checkForOverlay(reelArray[0].visibleSymbols[0]);
			if (overlaySize.x > 0 && overlaySize.y > 0)
			{
				yield return new TIWaitForSeconds(symbolOverlayStartupTime);

				Vector2 pos = getOverlayPosFromSize(overlaySize.x, overlaySize.y);

				ExpandingReelSymbolBase overlaySymbol = reelArray[0].visibleSymbols[0].animator.info.expandingSymbolOverlay;
				overlaySymbol.transform.position = new Vector3(pos.x, pos.y, -0.01f);
				overlaySymbol.setSize(this, overlaySize.x, overlaySize.y);
				fadeOutUnderSymbols(overlaySymbol, overlaySize.x, overlaySize.y);
				yield return overlaySymbol.StartCoroutine(overlaySymbol.doShow());

				yield return new TIWaitForSeconds(symbolOverlayShowTime);

				fadeInUnderSymbols(overlaySymbol, overlaySize.x, overlaySize.y);
				yield return overlaySymbol.StartCoroutine(overlaySymbol.doHide());

				isSkippingPreWinThisSpin = true;
			}
		}
	}

	// fade out the symbols that are covered by an expanding symbol
	private void fadeOutUnderSymbols(ExpandingReelSymbolBase symbol, int width, int height, float altFadeTime = -1.0f)
	{
		float fadeTime = 0.0f;

		if (altFadeTime > -1.0f)
		{
			fadeTime = altFadeTime;
		}
		else if (symbol != null)
		{
			fadeTime = symbol.fadeTime;
		}

		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				StartCoroutine(reelArray[i].visibleSymbols[j].animator.fadeSymbolOutOverTime(fadeTime));
			}
		}
	}

	// fade in the symbols that were covered by an expanding symbol
	private void fadeInUnderSymbols(ExpandingReelSymbolBase symbol, int width, int height, float altFadeTime = -1.0f)
	{
		float fadeTime = 0.0f;

		if (altFadeTime > -1.0f)
		{
			fadeTime = altFadeTime;
		}
		else if (symbol != null)
		{
			fadeTime = symbol.fadeTime;
		}


		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				if (reelArray[i].visibleSymbols[j].animator != null)
				{
					StartCoroutine(reelArray[i].visibleSymbols[j].animator.fadeSymbolInOverTime(fadeTime));
				}
			}
		}
	}
	
	public IEnumerator fadeOutSymbols(float duration)
	{
		List<SlotSymbol> allSymbols = engine.getAllSymbolsOnReels();
		List<TICoroutine> symbolFadeOutCoroutines = new List<TICoroutine>();
		foreach (SlotSymbol symbol in allSymbols)
		{
			symbolFadeOutCoroutines.Add(StartCoroutine(symbol.fadeOutSymbolCoroutine(duration)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(symbolFadeOutCoroutines));
	}

	/// Recursive function for finding the overlay size
	public Vector2int checkForOverlay(SlotSymbol symbol, int reelIndex = 0)
	{
		int finalReelIndex = 0;
		bool symbolFound = false; // Let's at least make sure 1 instance of the symbol is found in the wilds to qualify.
		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			int count = 0;

			foreach (SlotSymbol ss in reelArray[i].visibleSymbols)
			{
				if (ss.name.Contains(symbol.name) || ss.name.Contains("WD"))
				{
					count++;
				}

				if (ss.name.Contains(symbol.name))
				{
					symbolFound = true;
				}
			}

			if (count == reelArray[i].visibleSymbols.Length)
			{
				finalReelIndex = i;
			}
			else
			{
				break;
			}
		}

		finalReelIndex += 1; // Since we're indexing off of x originally, which doesn't map 1 to 1, let's add 1 here to get them in line.

		return (finalReelIndex == 1 || symbolFound == false) ? Vector2int.zero : new Vector2int(finalReelIndex, reelArray[0].visibleSymbols.Length);
	}

	// Separating this out so ExpandingReelSymbol can put oversized symbols in the right place for testing and previewing.
	public Vector2 getOverlayPosFromSize(int overlaySizeX, int overlaySizeY)
	{
		SlotReel[] reelArray = engine.getReelArray();
		float leftX = getReelRootsAt(0).transform.localPosition.x;
		float rightX = getReelRootsAt(overlaySizeX - 1).transform.localPosition.x;
		float topY = reelArray[0].visibleSymbolsBottomUp[overlaySizeY - 1].animator.transform.position.y;
		float bottomY = reelArray[0].visibleSymbolsBottomUp[0].animator.transform.position.y;

		float xPos = ((rightX - leftX) / 2) + leftX;
		float yPos = ((topY - bottomY) / 2) + bottomY;
		return new Vector2(xPos, yPos);
	}

	/// Return the credit bonus value from a credits bonus win, default to 0 if there is an issue
	public long getCreditBonusValue()
	{
		if (_outcome.isBonus && _outcome.isCredit)
		{
			return _outcome.winAmount * relativeMultiplier;
		}
		else
		{
			return 0;
		}
	}

	/// Function for getting credit bonus text, or a random value if another bonus is triggered 
	/// (NOTE: Only call this once, since it could generate a random value if this isn't a credit bonus)
	public string getCreditBonusValueText(bool isVertical = true)
	{
		if (_outcome.isBonus)
		{
			long winAmount = 0;

			if (_outcome.isCredit)
			{
				winAmount = _outcome.winAmount * relativeMultiplier;
			}
			else
			{
				// generatea a random value to display on the unselected choice
				//caluclate it base on formula, totalBet * Rand[5,10,15,25].  Since we want to bias it slightly to the lower end, i used a square of a number < 1, which will bias these to the lower end.
				int mult = 5 * Mathf.RoundToInt(1 + 4 * Random.value * Random.value);
				winAmount = mult * SlotBaseGame.instance.betAmount;
			}

			if (isVertical)
			{
				return CommonText.makeVertical(CreditsEconomy.convertCredits(winAmount, false));
			}
			else
			{
				return CreditsEconomy.convertCredits(winAmount);
			}
		}
		else
		{
			return "";
		}
	}

	// Return a fake credit bonus value, but not a formatted string
	public long getCreditMadeupValue()
	{
		long winAmount = 0;
		if (_outcome.isBonus)
		{
			if (_outcome.isCredit)
			{
				winAmount = _outcome.winAmount * relativeMultiplier;
			}
			else
			{
				// generatea a random value to display on the unselected choice
				//caluclate it base on formula, totalBet * Rand[5,10,15,25].  Since we want to bias it slightly to the lower end, i used a square of a number < 1, which will bias these to the lower end.
				int mult = 5 * Mathf.RoundToInt(1 + 4 * Random.value * Random.value);
				winAmount = mult * SlotBaseGame.instance.betAmount;
			}
		}
		return winAmount;
	}
	
	// Tells if any name in the list of names for a SymbolInfo contains an _Outcome name
	// In which case it is assumed to be only used when animating so will not need a flattened version
	private static bool isAnySymbolNameOutcomeForSymbolInfo(SymbolInfo info)
	{
		ReadOnlyCollection<string> possibleSymbolNames = info.getNameArrayReadOnly();
		foreach (string name in possibleSymbolNames)
		{
			if (SlotSymbol.isOutcomeSymbolFromName(name))
			{
				return true;
			}
		}

		return false;
	} 

	/// Generate a symbolTemplate list with unanimated flattened versions of all of the symbols over time
	private IEnumerator createFlattenedSymbolTemplatesOverTime()
	{
		// validate the symbol pool now, so they are setup so we can add new entries to them
		validateSymbolMapMade();

		// Create a new game object to store the new flattened template prefab objects in the scene
		GameObject flattendMeshPrefabStorageObject = new GameObject();
		flattendMeshPrefabStorageObject.name = "FlattenedSymbolPrefabs";
		flattendMeshPrefabStorageObject.transform.parent = this.gameObject.transform;

		int originalTemplateCount = symbolTemplates.Count;
		for (int i = 0; i < originalTemplateCount; i++)
		{
			SymbolInfo info = symbolTemplates[i];

			bool isAnyOutcomeSymbolInSymbolInfo = isAnySymbolNameOutcomeForSymbolInfo(info);

			// Skip _Outcome symbols, since those are animted versions that don't need flatening
			// and only create flattened versions for symbolPrefab symbols
			if (info.symbolPrefab != null && info.flattenedSymbolPrefab == null && !isAnyOutcomeSymbolInSymbolInfo)
			{
				createFlattenedSymbolInfoForSymbol(flattendMeshPrefabStorageObject, info);

				// wait a frame between each creation
				yield return null;
			}
		}
	}

	/// Generate a symbolTemplate list with unanimated flattened versions of all of the symbols
	private void createFlattenedSymbolTemplates()
	{
		// Create a new game object to store the new flattened template prefab objects in the scene
		GameObject flattendMeshPrefabStorageObject = new GameObject();
		flattendMeshPrefabStorageObject.name = "FlattenedSymbolPrefabs";
		flattendMeshPrefabStorageObject.transform.parent = this.gameObject.transform;

		foreach (SymbolInfo info in symbolTemplates)
		{
			bool isAnyOutcomeSymbolInSymbolInfo = isAnySymbolNameOutcomeForSymbolInfo(info);
		
			// Check if we already have a flattened version in the templates and if so we will skip making a new one
			if (info.symbolPrefab != null && info.flattenedSymbolPrefab == null && !isAnyOutcomeSymbolInSymbolInfo)
			{
				// Symbol hasn't been flattened yet, so lets make it
				createFlattenedSymbolInfoForSymbol(flattendMeshPrefabStorageObject, info);
			}
		}
	}

	/// Generate a symbolTemplate list with unanimated flattened versions of all of the symbols
	public void createFlattenedSymbolTemplatesWhileNotRunning()
	{
		List<SymbolInfo> newSymbolTemplateList = new List<SymbolInfo>();

		// Create a new game object to store the new flattened template prefab objects in the scene
		Transform oldFlattendMeshPrefabStorageObjectTransform = transform.Find("Symbol Prefabs");
		GameObject oldFlattendMeshPrefabStorageObject = null;
		if (oldFlattendMeshPrefabStorageObjectTransform != null)
		{
			oldFlattendMeshPrefabStorageObject = oldFlattendMeshPrefabStorageObjectTransform.gameObject;
		}
		GameObject flattendMeshPrefabStorageObject = new GameObject();
		flattendMeshPrefabStorageObject.name = "Symbol Prefabs";
		flattendMeshPrefabStorageObject.transform.parent = this.gameObject.transform;

		foreach (SymbolInfo info in symbolTemplates)
		{
			// Skip _Outcome symbols, since those are animated versions that don't need flattening
			// and only create flattened versions for symbolPrefab symbols
			bool isAnyOutcomeSymbolInSymbolInfo = isAnySymbolNameOutcomeForSymbolInfo(info);
			
			if (!isAnyOutcomeSymbolInSymbolInfo && info.symbolPrefab != null)
			{
				SymbolInfo flattenedSymbolInfo = createFlattenedSymbolInfoForSymbol(flattendMeshPrefabStorageObject, info, flattendMeshPrefabStorageObject);

				if (flattenedSymbolInfo != null)
				{
					newSymbolTemplateList.Add(flattenedSymbolInfo);
				}
			}
			else
			{
				newSymbolTemplateList.Add(info); // Keep outcome symbols in the info since we could still use them.
			}
		}

		// assign out the newly created list back to the inspector value
		symbolTemplates = newSymbolTemplateList;
		// Get rid of the old template
		DestroyImmediate(oldFlattendMeshPrefabStorageObject);
		flattendMeshPrefabStorageObject.SetActive(false);
	}

	/// Shared function between an overtime and immediate version of creating optimized flattened symbol info
	private SymbolInfo createFlattenedSymbolInfoForSymbol(GameObject flattendMeshPrefabStorageObject, SymbolInfo info, GameObject templateParent = null)
	{
		// Flatten the meshes
		GameObject symbolInstance = CommonGameObject.instantiate(info.symbolPrefab) as GameObject;
		List<GameObject> objectsToNotFlatten = new List<GameObject>();
		FlattenSymbolOmitList flattenObjectsToOmitComponent = symbolInstance.GetComponent<FlattenSymbolOmitList>();
		if (flattenObjectsToOmitComponent != null)
		{
			objectsToNotFlatten = flattenObjectsToOmitComponent.objectsToNotFlatten;
		}

		string symbolInfoNameToUse = info.getFirstElementInNameArray();
		
		// Grab the first name and use that for the GameObject names
		symbolInstance.name = symbolInfoNameToUse;
		Animator symbolAnimator = symbolInstance.GetComponent<Animator>();
		if (symbolAnimator != null)
		{
			// disable the animator to avoid weird stuff happening while I'm trying to render it out
			symbolAnimator.enabled = false;
		}

		// If we are dealing with SpriteRenderer then we can't optimize this symbol because it doesn't use meshes, which sucks :(
		SpriteRenderer[] spriteRenderers = symbolInstance.GetComponentsInChildren<SpriteRenderer>();
		if (spriteRenderers.Length > 0)
		{
			if (!Application.isPlaying)
			{
				Destroy(symbolInstance);
			}
			else
			{
				DestroyImmediate(symbolInstance);
			}
			return null;
		}

		// Place the symbol at (0,0,0) so that it's positioning doesn't affect how the meshes are positioned in the final product
		symbolInstance.transform.position = Vector3.zero;

		// Ensure the symbol is at scale (1,1,1) otherwise strange rendnering issues are going to occur with how scale is applied to the dynmaicly created symbols
		symbolInstance.transform.localScale = Vector3.one;

		// make sure the instance is turned on before we look at all it's rendering stuff
		symbolInstance.SetActive(true);

		// This class is kind of hacky and creates meshes, so force it to create the meshes it will use when things are actually running
		UVManipulator[] uvManipulators = symbolInstance.GetComponentsInChildren<UVManipulator>();
		foreach (UVManipulator uvManip in uvManipulators)
		{
			uvManip.Update();
		}

		// Force update any TMPro text objects so they will hopefully look correct when they get merged in
		TextMeshPro[] textMeshPros = symbolInstance.GetComponentsInChildren<TextMeshPro>();
		foreach (TextMeshPro textMesh in textMeshPros)
		{
			textMesh.ForceMeshUpdate();
		}

		SpriteMask[] spriteMasks = symbolInstance.GetComponentsInChildren<SpriteMask>();
		foreach (SpriteMask spriteMask in spriteMasks)
		{
			spriteMask.Awake();
			spriteMask.Start();
		}

		MeshFilter[] meshFilters = symbolInstance.GetComponentsInChildren<MeshFilter>();
		SkinnedMeshRenderer[] skinnedMeshRenderers = symbolInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
		// combine the info from the MeshFilters and SkinnedMeshRenderers together
		List<MeshRenderedObjectInfo> meshRenderedObjectInfoList = new List<MeshRenderedObjectInfo>();

		foreach (MeshFilter meshFilter in meshFilters)
		{
			if (!objectsToNotFlatten.Contains(meshFilter.gameObject))
			{
				MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

				// make sure that the material on this renderer is actually setup to render an alpha value, otherwise it should be invisible and we should ignore it
				if (!CommonMaterial.canAlphaMaterial(meshRenderer.sharedMaterial) || CommonMaterial.getAlphaOnMaterial(meshRenderer.sharedMaterial) > 0)
				{
					MeshRenderedObjectInfo meshRenderedInfo = new MeshRenderedObjectInfo();
					meshRenderedInfo.objectTransform = meshFilter.transform;
					meshRenderedInfo.mesh = meshFilter.sharedMesh;
					meshRenderedInfo.sharedMaterial = meshRenderer.sharedMaterial;

					Renderer renderer = meshFilter.GetComponent<Renderer>();
					meshRenderedInfo.bounds = renderer.bounds;

					meshRenderedObjectInfoList.Add(meshRenderedInfo);
				}
			}
		}

		foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
		{
			if (!objectsToNotFlatten.Contains(skinnedMeshRenderer.gameObject))
			{
				// make sure that the material on this renderer is actually setup to render an alpha value, otherwise it should be invisible and we should ignore it
				if (!CommonMaterial.canAlphaMaterial(skinnedMeshRenderer.sharedMaterial) || CommonMaterial.getAlphaOnMaterial(skinnedMeshRenderer.sharedMaterial) > 0)
				{
					MeshRenderedObjectInfo meshRenderedInfo = new MeshRenderedObjectInfo();
					meshRenderedInfo.objectTransform = skinnedMeshRenderer.transform;
					Mesh bakedMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(bakedMesh);
					meshRenderedInfo.mesh = bakedMesh;
					meshRenderedInfo.sharedMaterial = skinnedMeshRenderer.sharedMaterial;

					Renderer renderer = skinnedMeshRenderer.GetComponent<Renderer>();
					meshRenderedInfo.bounds = renderer.bounds;

					meshRenderedObjectInfoList.Add(meshRenderedInfo);
				}
			}
		}

		// sort the mesh info by z-depth to ensure correct draw order
		meshRenderedObjectInfoList.Sort((obj1, obj2) => obj2.bounds.center.z.CompareTo(obj1.bounds.center.z));

		CombineInstance[] firstPassCombine = new CombineInstance[meshRenderedObjectInfoList.Count];
		List<CombineInstance> finalCombineList = new List<CombineInstance>();
		List<Material> materialList = new List<Material>();

		for (int i = 0; i < meshRenderedObjectInfoList.Count; i++)
		{
			firstPassCombine[i].mesh = meshRenderedObjectInfoList[i].mesh;
			firstPassCombine[i].transform = meshRenderedObjectInfoList[i].objectTransform.localToWorldMatrix;
			materialList.Add(meshRenderedObjectInfoList[i].sharedMaterial);
		}

		// Generate the new symbol to attach the flattened mesh to
		GameObject flattenedSymbolInstance = new GameObject();
		Vector2 symbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(symbolInfoNameToUse);
		string symbolShortName = SlotSymbol.getShortNameFromName(symbolInfoNameToUse);
		string symbolNameWithFlattenedExtension = SlotSymbol.constructNameFromDimensions(symbolShortName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		flattenedSymbolInstance.name = symbolNameWithFlattenedExtension;
		flattenedSymbolInstance.transform.parent = flattendMeshPrefabStorageObject.transform;
		flattenedSymbolInstance.transform.localPosition = symbolInstance.transform.localPosition;
		//flattenedSymbolInstance.transform.localRotation = symbolInstance.transform.localRotation;
		flattenedSymbolInstance.transform.localScale = symbolInstance.transform.localScale;
		MeshFilter flattenedMeshFilter = flattenedSymbolInstance.AddComponent<MeshFilter>();
		MeshRenderer flattenedMeshRenderer = flattenedSymbolInstance.AddComponent<MeshRenderer>();

		// now clean up the material list and combine meshes that share materials to not include the duplicates we combined
		List<Material> finalMaterialList = new List<Material>();

		int currentMaterialIndex = 0;
		while (currentMaterialIndex < materialList.Count)
		{
			Material currentMaterial = materialList[currentMaterialIndex];
			finalMaterialList.Add(currentMaterial);

			List<CombineInstance> subCombine = new List<CombineInstance>();
			subCombine.Add(firstPassCombine[currentMaterialIndex]);

			int numMaterialsSkipped = 0;
			if (currentMaterialIndex + 1 < materialList.Count)
			{
				Material nextMaterial = materialList[currentMaterialIndex + 1];

				while (nextMaterial == currentMaterial)
				{
					subCombine.Add(firstPassCombine[currentMaterialIndex + (numMaterialsSkipped + 1)]);
					numMaterialsSkipped++;

					if (currentMaterialIndex + (numMaterialsSkipped + 1) < materialList.Count)
					{
						nextMaterial = materialList[currentMaterialIndex + (numMaterialsSkipped + 1)];
					}
					else
					{
						nextMaterial = null;
					}
				}
			}

			if (subCombine.Count > 1)
			{
				// combine the submeshes that share a material
				CombineInstance subCombinedMeshes = new CombineInstance();
				subCombinedMeshes.mesh = new Mesh();
				subCombinedMeshes.mesh.CombineMeshes(subCombine.ToArray());
				subCombinedMeshes.transform = Matrix4x4.identity;

				finalCombineList.Add(subCombinedMeshes);
			}
			else if (subCombine.Count == 1)
			{
				// we only have one mesh so just add it
				finalCombineList.Add(subCombine[0]);
			}

			currentMaterialIndex += numMaterialsSkipped + 1;
		}

		//int originalSubMeshCount = materialList.Count;

		materialList = finalMaterialList;

		//Debug.LogWarning(info.name + ": WAS - originalSubMeshCount = " + originalSubMeshCount + "; NOW - materialList.Count = " + materialList.Count + "; finalCombineList.Count = " + finalCombineList.Count);

		// Combine the meshes onto this new object
		flattenedMeshFilter.sharedMesh = new Mesh();
		flattenedMeshFilter.sharedMesh.name = symbolInfoNameToUse + "_flattened";
		flattenedMeshFilter.sharedMesh.CombineMeshes(finalCombineList.ToArray(), false);

		// copy the material list for the submeshes
		flattenedMeshRenderer.sharedMaterials = materialList.ToArray();

		for (int i = 0; i < objectsToNotFlatten.Count; i++)
		{
			objectsToNotFlatten[i].transform.SetParent(flattenedSymbolInstance.transform);
		}

		// copy any boxColliders, as our SymbolAnimator will want to use them when creating bounding boxes
		BoxCollider[] colliders = symbolInstance.GetComponentsInChildren<BoxCollider>(false);
		foreach (var collider in colliders)
		{
			var newCollider = flattenedSymbolInstance.AddComponent<BoxCollider>();
			newCollider.center = collider.center;
			newCollider.size = collider.size;
		}

		//UnityEditor.PrefabUtility.ConnectGameObjectToPrefab(info.flattenedSymbolPrefab, flatProjectPrefab);
		// ensure the flattened symbol is on the right layer
		CommonGameObject.setLayerRecursively(flattenedSymbolInstance, flattenedSymbolsUseSourceLayer ? symbolInstance.gameObject.layer : Layers.ID_SLOT_REELS);

		// hide the prefab
		flattenedSymbolInstance.SetActive(false);

		// clean up the symbol instance we use to make the flattened version
		if (Application.isPlaying)
		{
			Destroy(symbolInstance);
		}
		else
		{
			DestroyImmediate(symbolInstance);
		}

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			// Optimize the mesh, which is only possible to do in the editor
			MeshUtility.Optimize(flattenedMeshFilter.sharedMesh);

			// Create all of the prefabs needed to create these symbols on the fly.
			string originalPathOfSymbols = UnityEditor.AssetDatabase.GetAssetPath(info.symbolPrefab);
			string path = originalPathOfSymbols.Substring(0, originalPathOfSymbols.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
			// We want to create the symbols folder for the flattened symbols.
			string folderName = "Flattened Symbols " + name;
			string flattenedSymbolPath = path + System.IO.Path.DirectorySeparatorChar + folderName;

			string meshesFolderName = "Meshes";
			string meshFolderPath = flattenedSymbolPath + System.IO.Path.DirectorySeparatorChar + meshesFolderName;

			string materialsFolderName = "Materials";
			string materialFolderPath = flattenedSymbolPath + System.IO.Path.DirectorySeparatorChar + materialsFolderName;

			if (!UnityEditor.AssetDatabase.IsValidFolder(flattenedSymbolPath))
			{
				Debug.Log("Created a file at " + path);
				string guid = UnityEditor.AssetDatabase.CreateFolder(path, folderName);
				path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid) + System.IO.Path.DirectorySeparatorChar;
			}


			if (!UnityEditor.AssetDatabase.IsValidFolder(meshFolderPath))
			{
				Debug.Log("Created a file at " + path);
				string guid = UnityEditor.AssetDatabase.CreateFolder(flattenedSymbolPath, meshesFolderName);
				path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid) + System.IO.Path.DirectorySeparatorChar;
			}

			if (!UnityEditor.AssetDatabase.IsValidFolder(materialFolderPath))
			{
				Debug.Log("Created a file at " + path);
				string guid = UnityEditor.AssetDatabase.CreateFolder(flattenedSymbolPath, materialsFolderName);
				path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid) + System.IO.Path.DirectorySeparatorChar;
			}

			// Save out any of the instanced materials that are being used for this symbol.
			foreach (Material mat in materialList)
			{
				if (mat.shader != null && mat.shader.name.Contains("SpriteMask")) // Sprite mask makes dynamic materials so we need to save them.
				{
					mat.name = mat.name.Replace(System.IO.Path.DirectorySeparatorChar, '-');
					mat.name = mat.name.Replace(":", "");
					UnityEditor.AssetDatabase.CreateAsset(
						mat,
						materialFolderPath + System.IO.Path.DirectorySeparatorChar + mat.name + ".mat");
				}
			}

			// Save the mesh for the flattned symbols
			UnityEditor.AssetDatabase.CreateAsset(
				flattenedMeshFilter.sharedMesh,
				meshFolderPath + System.IO.Path.DirectorySeparatorChar + flattenedMeshFilter.sharedMesh.name + ".mat");

			// Save out the flattened symbol.
			// TODO:UNITY2018:nestedprefab:confirm//old
			// GameObject flatProjectPrefab = UnityEditor.PrefabUtility.CreatePrefab(
			// 	flattenedSymbolPath + System.IO.Path.DirectorySeparatorChar + info.name + "_flattened.prefab", flattenedSymbolInstance,
			// 	UnityEditor.ReplacePrefabOptions.ConnectToPrefab);
			// Object preobj = UnityEditor.PrefabUtility.GetPrefabParent(flattenedSymbolInstance);
			// TODO:UNITY2018:nestedprefab:confirm//new
			GameObject flatProjectPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(flattenedSymbolInstance, flattenedSymbolPath + System.IO.Path.DirectorySeparatorChar + symbolInfoNameToUse + "_flattened.prefab", InteractionMode.AutomatedAction);
			Object preObj = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(flattenedSymbolInstance);

			flattenedSymbolInstance = preObj as GameObject;
		}
#endif

		info.flattenedSymbolPrefab = flattenedSymbolInstance;

		return info;
	}

	// Tells if a module is currently blocking a spin from continuing, this may be for instance because the module is triggering a feature
	// that will extend past when the game would normally unlock 
	public bool isModuleCurrentlySpinBlocking()
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.isCurrentlySpinBlocking())
			{
				return true;
			}
		}

		return false;
	}

	// Returns the running rollup starting point, which is used by SlotUtils.rollup to determine if a payout is over the big win threshold
	public long getCurrentRunningPayoutRollupValue()
	{
		return runningPayoutRollupValue;
	}

	// Allows runningPayoutRollupValue to be set
	// NOTE: This should be used sparingly! For the most part this value shouldn't need 
	// to be modified by something that isn't a ReelGame class, unless it is doing something
	// strange with payment awards/rollups
	public void setRunningPayoutRollupValue(long newValue)
	{
		runningPayoutRollupValue = newValue;
	}

	// Increment the runningPayoutRollupValue by the passed in value
	public void incrementRunningPayoutRollupValueBy(long amountToIncrementBy)
	{
		runningPayoutRollupValue += amountToIncrementBy;
	}

	// Get the amount of runningPayoutRollupValue that has already been paid out to the player
	public long getRunningPayoutRollupAlreadyPaidOut()
	{
		return runningPayoutRollupAlreadyPaidOut;
	}

	// Increments runningPayoutRollupAlreadyPaidOut by the passed in value
	public void incrementRunningPayoutRollupAlreadyPaidOutBy(long amountToIncrementBy)
	{
		if (amountToIncrementBy < 0)
		{
			Debug.LogWarning("ReelGame.incrementRunningPayoutRollupAlreadyPaidOutBy() - Trying to increment by a negative number!");
		}
		else
		{
			runningPayoutRollupAlreadyPaidOut += amountToIncrementBy;
		}
	}

	// This will now be the prefered way to add credits to the player for games, this ensures
	// that all credit adds for games funnel here, and that what is already paid out can be tracked
	// via runningPayoutRollupAlreadyPaidOut
	public void addCreditsToSlotsPlayer(long amount, string reason, bool shouldPlayCreditsRollupSound = true, bool isIncrementingRunningPayoutRollupAlreadyPaidOut = true)
	{
		//Debug.Log("ReelGame.addCreditsToSlotsPlayer() - amount = " + amount + "; reason = " + reason + "; shouldPlayCreditsRollupSound = " + shouldPlayCreditsRollupSound + "; isIncrementingRunningPayoutRollupAlreadyPaidOut = " + isIncrementingRunningPayoutRollupAlreadyPaidOut);

		SlotsPlayer.addCredits(amount, reason, shouldPlayCreditsRollupSound);
		GameEvents.trackCreditsWonDuringSpin(amount, GameState.game);
		if (isIncrementingRunningPayoutRollupAlreadyPaidOut)
		{
			incrementRunningPayoutRollupAlreadyPaidOutBy(amount);
		}
	}

	// Helper function to tell if a value will trigger a big win, placing this in a standard location 
	// since there are a number of places now all checking the same things (this way we maintain it in one)
	public bool willPayoutTriggerBigWin(long payout)
	{
		bool shouldBigWin = false;
		bool isDoingFreeSpinsInBase = isDoingFreespinsInBasegame();
		bool isActiveGameBaseGame = (this is SlotBaseGame) && !isDoingFreeSpinsInBase;
		bool isGoingToLaunchFreeSpinsInBase = (playFreespinsInBasegame && outcome != null && outcome.isBonus && outcome.isGifting);
		bool isDelayingBigWin = (this is SlotBaseGame) && (this as SlotBaseGame).areModulesDelayingBigWin;
		shouldBigWin = isActiveGameBaseGame
			&& !isGoingToLaunchFreeSpinsInBase
			&& !isDelayingBigWin
			&& isOverBigWinThreshold(payout + getCurrentRunningPayoutRollupValue())
			&& (!hasReevaluationSpinsRemaining || (hasReevaluationSpinsRemaining && outcome != null && outcome.getTumbleOutcomes().Length > 0));
		return shouldBigWin;
	}

	// Helper function for running rollups for reel games which require two coroutines to run to finish
	public IEnumerator rollupCredits(long endAmount,
		RollupDelegate rollupDelegate,
		bool isPlayingRollupSounds,
		float specificRollupTime = 0.0f,
		bool shouldSkipOnTouch = true,
		bool allowBigWin = true,
		bool isAddingRollupToRunningPayout = true)
	{
		// start the rollup from 0 since onPayoutRollup should handle adding in what has already rolled up
		yield return StartCoroutine(rollupCredits(0,
			endAmount,
			rollupDelegate,
			isPlayingRollupSounds,
			specificRollupTime,
			shouldSkipOnTouch,
			allowBigWin,
			isAddingRollupToRunningPayout));
	}

	// Helper function for running rollups for reel games which require two coroutines to run to finish
	public IEnumerator rollupCredits(long startAmount,
		long endAmount,
		RollupDelegate rollupDelegate,
		bool isPlayingRollupSounds,
		float specificRollupTime = 0.0f,
		bool shouldSkipOnTouch = true,
		bool allowBigWin = true,
		bool isAddingRollupToRunningPayout = true,
		string rollupOverrideSound = "",
		string rollupTermOverrideSound = "")
	{
		if (rollupDelegate == null)
		{
			rollupDelegate = onPayoutRollup;
		}

		long payout = endAmount - startAmount;

		bool shouldBigWin = false;
		if (allowBigWin)
		{
			shouldBigWin = willPayoutTriggerBigWin(payout);
		}

		bool isAllowingContinueWhenReady = false;

		if (shouldBigWin)
		{
			NotificationAction.sendJackpotNotifications(GameState.currentStateName);
			foreach (SlotModule module in cachedAttachedSlotModules)
			{
				if (module.needsToExecuteOnPreBigWin())
				{
					yield return StartCoroutine(module.executeOnPreBigWin());
				}
			}
			// @note : Not sure this is the best value to pass here, since we 
			// might want what the final big win will be which might be different
			// if we've already rolled something up
			onBigWinNotification(payout);

			// Force the isAllowingContinueWhenReady flag to true, because we need that on to ensure that
			// the game will unlock after the big win is over
			isAllowingContinueWhenReady = true;
		}

		yield return StartCoroutine(SlotUtils.rollup(startAmount, endAmount, rollupDelegate, isPlayingRollupSounds, specificRollupTime, shouldSkipOnTouch, shouldBigWin, rollupOverrideSound, rollupTermOverrideSound));
		yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: isAllowingContinueWhenReady, isAddingRollupToRunningPayout: isAddingRollupToRunningPayout));
	}

	// Run modules for taking care of something right before BonusGamePresenter call final cleanup
	// like a playing a transition animation attached to the bonus as we head back into the base game
	public IEnumerator handleNeedsToExecuteOnBonusGamePresenterFinalCleanupModules()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule currentModule = cachedAttachedSlotModules[i];
			if (currentModule.needsToExecuteOnBonusGamePresenterFinalCleanup())
			{
				yield return StartCoroutine(currentModule.executeOnBonusGamePresenterFinalCleanup());
			}
		}
	}

	// Check if the top Overlay should be enabled when returning from a bonus, some modules like transition
	// modules may disable this since they will want to turn on the Overlay at the correct time for
	// the transition being done
	public bool isEnablingOverlayWhenBonusGameEnds()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule currentModule = cachedAttachedSlotModules[i];
			if (!currentModule.isEnablingOverlayWhenBonusGameEnds())
			{
				return false;
			}
		}

		return true;
	}

	// Check if the Spin Panel should be enabled when returning from a bonus, some modules like transition
	// modules may disable this since they will want to turn on the Spin Panel at the correct time
	// for the transition being done
	public bool isEnablingSpinPanelWhenBonusGameEnds()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule currentModule = cachedAttachedSlotModules[i];
			if (!currentModule.isEnablingSpinPanelWhenBonusGameEnds())
			{
				return false;
			}
		}

		return true;
	}

	// If this returns true, the ReelGame will *NOT* automatically clear all of the
	// SlotReel.symbolOverrides after a spin, and it will be up to a module to do
	// the clearing when it thinks is appropriate.
	public bool isHandlingSlotReelClearSymbolOverridesWithModule()
	{
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule currentModule = cachedAttachedSlotModules[i];
			if (currentModule.isHandlingSlotReelClearSymbolOverridesWithModule())
			{
				return true;
			}
		}

		return false;
	}
	
	// Tells if a valid spin could be made right now, used by the SpinPanel to determine
	// if interactions are valid or whether it should ignore them until the game is in
	// a valid state where it can spin
	public virtual bool isAbleToValidateSpin()
	{
		return true;
	}

	protected virtual void Update()
	{
		if (isNewCentering && !isViewportScalingDone && SpinPanel.instance != null)
		{
			StartCoroutine(fitViewport());
		}
	}

	protected virtual IEnumerator fitViewport()
	{
		// Need to wait a frame for the parent object to get situated by Unity before we can parent anything to it,
		// otherwise all the children end up at 0,0,0 scale, which is really weird and shitty.
		yield return null;

		float height = SpinPanel.getNormalizedReelsAreaHeight(isFreeSpinGame());
		float center = SpinPanel.getNormalizedReelsAreaCenter(isFreeSpinGame());

		Rect viewport = new Rect(
			0.0f,                               // x (left)
			(1.0f - height) * 0.5f + center,    // y (bottom)
			1.0f,                               // width
			height                              // height (duh)
		);

		// Need to put all the objects into a sub-object so that they can all be scaled and positioned as a single unit.
		foreach (GameObject go in CommonGameObject.findDirectChildren(gameObject, true))
		{
			if (go == gameScaler)
			{
				// Don't parent the parent to itself.
				continue;
			}
			go.transform.parent = gameScaler.transform;
		}

		// Set the viewport for all of the cameras in the game. Scaling doesn't effect what a camera is rendering.
		foreach (Camera cam in gameObject.GetComponentsInChildren<Camera>(true))
		{
			if ((cam.cullingMask & layersToOmitNewCenteringOn) == 0)
			{
				cam.rect = viewport;
			}
		}

		foreach (Transform trans in CommonGameObject.getObjectsByLayerMask(gameScaler, layersToOmitNewCenteringOn))
		{
			int layerMask = 1 << trans.parent.gameObject.layer;
			if ((layerMask & layersToOmitNewCenteringOn) == 0)
			{
				// If the parent has been moved then we don't want to move this object too, or else we might break some animations.
				trans.parent = gameObject.transform;
			}
		}

		// Scale the contents to fill the viewport height.
		float scale = (isFreeSpinGame() ? FS_FULL_HEIGHT_SCALE : FULL_HEIGHT_SCALE);
		gameScaler.transform.localScale = new Vector3(
			scale,
			scale,
			1.0f
		);

		isViewportScalingDone = true;

		updateVerticalSpacingWorld();
	}

	/// Helper class to store info about mesh rendering objects so they can be sorted and combined
	private class MeshRenderedObjectInfo
	{
		public Bounds bounds;
		public Transform objectTransform;
		public Mesh mesh = null;
		public Material sharedMaterial = null;
	}

	public SlotOutcome getCurrentOutcome()
	{
		if (currentReevaluationSpin != null)
		{
			return currentReevaluationSpin;
		}

		return outcome;
	}

	// Function to allow for the removal of a cachedAttachedSlotModule when it is destroyed
	// this should ensure that the game isn't hanging onto a ghost if one is destroyed
	public bool removeModuleFromCachedAttachedSlotModules(SlotModule module)
	{
		return cachedAttachedSlotModules.Remove(module);
	}

	// Get the symbol cache being used by this ReelGame
	private SlotSymbolCache getSlotSymbolCache()
	{
		return slotSymbolCache;
	}

	protected IEnumerator waitForReelGameBackgroundScalingUpdate()
	{
		// (Scott) Make sure that the background is active and then wait for it to update (since the
		// update will only happen if the background is active).  If it isn't active when the
		// game starts we'll skip the wait, since we'll assume that whatever effects the base
		// game is going to do before making the background active will mask the resize.
		// If we ever find we need to put this wait in the middle of some animation sequence
		// we could consider making this function public so that modules could call it in
		// order to ensure the resize is waited on once the reelGameBackground was active but
		// before it is actually shown.  For the most part I don't think we need that though,
		// as it would be better to leave the background active where possible and just ensure
		// something renders over it at the start or it drops in from offscreen, as that ensures
		// that all resizing will be done right away.
		if (reelGameBackground != null && reelGameBackground.gameObject.activeInHierarchy)
		{
			// (Scott) Because when freespin games are created we get a huge frame spike which
			// can substantially delay the first Update() call, we should wait until we are sure that
			// Update has been called before doing a full timeout.  We will do a safety timeout to
			// ensure that Update will be called (and that the reelGameBackground didn't somehow get
			// disabled or something like that before it could Update).
			
			// Wait and use an initial timeout that is a bit long to ensure that Update is going to be called.
			// if it isn't we'll just abort.
			float reelGameBackgroundUpdateTimeoutCheck = 0.0f;
			while (!reelGameBackground.isFirstUpdateCalled && reelGameBackgroundUpdateTimeoutCheck < REEL_GAME_BACKGROUND_UPDATE_DELAY_TIMEOUT)
			{
				yield return null;
				reelGameBackgroundUpdateTimeoutCheck += Time.unscaledDeltaTime;
			}
			
			if (reelGameBackgroundUpdateTimeoutCheck >= REEL_GAME_BACKGROUND_UPDATE_DELAY_TIMEOUT)
			{
#if UNITY_EDITOR
				Debug.LogError("ReelGame.playGameStartModules() - Game timed out waiting for reelGameBackground to run its Update loop, proceeding anyways. (We should find out why and fix this). reelGameBackgroundUpdateTimeoutCheck = " + reelGameBackgroundUpdateTimeoutCheck);
#endif
				yield break;
			}
			
			// Wait for the resize to finish, now that Update has been called
			float reelGameBackgroundResizeTimeoutCheck = 0.0f;
			while (!reelGameBackground.isScalingComplete && reelGameBackgroundResizeTimeoutCheck < REEL_GAME_BACKGROUND_SCALING_COMPLETE_TIMEOUT)
			{
				yield return null;
				reelGameBackgroundResizeTimeoutCheck += Time.unscaledDeltaTime;
			}
			
			if (reelGameBackgroundResizeTimeoutCheck >= REEL_GAME_BACKGROUND_SCALING_COMPLETE_TIMEOUT)
			{
#if UNITY_EDITOR
				Debug.LogError("ReelGame.playGameStartModules() - Game timed out waiting for reelGameBackground to size, proceeding anyways. (We should find out why and fix this). reelGameBackgroundResizeTimeoutCheck = " + reelGameBackgroundResizeTimeoutCheck);
#endif
				yield break;
			}
		}
	}
	
	// Render out the info from SlotSymbolCache into SlotSymbolCacheEditorWindow
	public void drawOnGuiSlotSymbolCacheInfo()
	{
		if (slotSymbolCache != null)
		{
			slotSymbolCache.drawOnGuiSlotSymbolCacheInfo(isGameUsingOptimizedFlattenedSymbols);
		}
		else
		{
			GUILayout.Label("SlotSymbolCache not setup yet.");
		}
	}
}

public enum SpecialWinSurfacing
{
	PRE_REEL_STOP = 0,
	POST_REEL_STOP = 10,
	POST_NORMAL_OUTCOMES = 20
}
