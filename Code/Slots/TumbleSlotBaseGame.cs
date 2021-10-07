using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Base Class for tumble-games (com05 and zynga01 both extend from this class)
 * Tumble games remove winning symbols and "tumble" in new symbols
 * Author: Nick Reynolds
 */ 
public class TumbleSlotBaseGame : SlotBaseGame
{	
	// inspector variables
	public GameObject symbolCamera;
	public GameObject revealPrefab;
	public Vector3 revealPrefabScale;
	public float tumbleTime;
	public bool playRollupSoundsWithBigWinAnimation = false; // Some games like wicked01 use music tracks for rollup, so we need to not play rollup sounds, and instead start and stop them with the animation

	[SerializeField] public Camera paylineCamera = null;						// payline camera y poisiton has to adjust by the ReelGameBackground.getVerticalSpacingModifier
	[SerializeField] protected float WINNING_SYMBOL_DESTROY_INTERVAL = 0.1f;
	[SerializeField] protected float TIME_TO_DESTROY_REVEAL = 0.83f;
	[SerializeField] protected float TIME_TO_DESTROY_SYMBOL = 0.2f;
	[SerializeField] protected float SYMBOL_ANIMATION_LENGTH = 2.5f;
	[SerializeField] protected float TUMBLE_SYMBOL_SPEED = 10.0f;
	[SerializeField] protected float TIME_BETWEEN_PLOPS = 0.05f;	
	[SerializeField] protected bool  playDestroySymbolAudioOnce = true;	
	[SerializeField] protected float TIME_BETWEEN_REMOVALS = 0.1f;
	[SerializeField] protected float BONUS_SYMBOL_FANFARE_DELAY = 0.15f;
	[SerializeField] protected float BONUS_SYMBOL_ANIMATE_SOUND_DELAY = 0.0f;

	[SerializeField] public bool shouldTumblePlayAnticipation = true; // Start the anticipation anim as soon as a symbol starts falling down the reels?
	[Tooltip("If this string is set then removeAllWinningSymbols will postfix numbers to it for generating destory sound audio. Otherwise DESTROY_SYMBOLS_SOUND will be used")]
	[SerializeField] private  string DESTROY_SYMBOLS_SOUND_ROOT;
	[SerializeField] private string DESTROY_SYMBOLS_SOUND_SUFFIX = "";

	[Tooltip("If > 0 preSpin will play the DESTROY_SYMBOLS_SOUND_ROOT sounds in order.")]
	[SerializeField] private  float	 CASCADE_AUDIO_DELAY;

	[Tooltip("If > 0 preSpin will fade out the symbols")]
	[SerializeField] private  float SYMBOL_FADE_TIME;

	[SerializeField] protected  float TIME_EXTRA_WAIT_BEAT = 0.5f;


	[Tooltip("If this is true prespin will disable the stop button if autospins is 0.")]
	[SerializeField] protected bool  disableStopButtonOnPreSpin = false;	
	[Tooltip("If this is true prespin will call clearOutHelperDataStructures")]
	[SerializeField] protected bool  clearHelperStructsOnPreSpin = false;	
	[SerializeField] protected float MAGIC_DISTANCE = 6.0f;
	// timing constants
	[SerializeField] protected  float TRANSITION_WAIT_TIME_1 = 3.0f;
	[SerializeField] protected  float TRANSITION_WAIT_TIME_2 = 1.25f;	

	[SerializeField] private bool alwaysPlayBonusSymbolFanfare = false;
	[SerializeField] private bool shouldPlayBonusSymbolFanfareOnBringDown = false;
	[SerializeField] private bool shouldPlayBonusSymbolFanfareWithAntAnim = false; // play the fanfare when the symbol actually animates, overrides other types of playing of the fanfare
	[SerializeField] private bool doEngineBonusFx = false;

	[Tooltip("If this is true big win prefab will scale up on each call to onBigWinNotification, can get pretty big!")]
	[SerializeField] bool doBigWinScaling = true;

	protected List<List<bool>> willBeRemoved;
	protected List<List<bool>> nowEmptySymbolPositions;
	public List<List<SlotSymbol>> visibleSymbolClone;
	protected Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>> symbolsToRemove = new Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>>();
	protected Dictionary<KeyValuePair<int,int>, Vector3> originalPositions = new Dictionary<KeyValuePair<int, int>, Vector3>();

	// holds references to symbols that have fallen to new positiosn (that were part of visibleSymbols when they fell)
	protected Dictionary<KeyValuePair<int,int>, SlotSymbol> fallenSymbols = new Dictionary<KeyValuePair<int, int>, SlotSymbol>(); 

	// timing variables // fake constants //
	// we don't make some constant so we can override them in other classes
	protected readonly float TIME_TO_WAIT_AT_END = .5f;
	[SerializeField]  bool isBigWinWaitAtEndForSound = false; // we may only want to use this wait time if we have a long audio that needs to play before the big win is over
	public float BIG_WIN_WAIT_AT_END = -1.0f;
	
	protected const float TIME_MOVE_SYMBOL_UP = .35f;
	protected const float TIME_MOVE_SYMBOL_DOWN = .35f;
	protected const float TIME_FADE_SHOW_IN = .35f;
	protected const float TIME_FADE_SHOW_OUT = .35f;
	protected const float TIME_SHOW_DURATION = .5f;
	protected const float TIME_POST_SHOW = 0.3f;
	protected const float TIME_EXTRA_WAIT_ON_SPIN = 0.8f;
	protected const float TIME_EXTRA_WAIT_AFTER_PAYBOXES = 0.1f;
	[SerializeField] protected float TIME_ROLLUP_TERMINATING_WAIT = 0.6f;
	public const float BASE_ROLLUP_TIME = 0.5f;
	public float BIG_WIN_ROLLUP_TIME = -1.0f;
	protected float WIN_SYMBOL_RAISE_DISTANCE = -0.6f;

	private const float DEFAULT_NO_OUTCOME_AUTOSPIN_DELAY = 0.75f;	// Default value for this delay if it wasn't set on the prefab

	private bool isReadyForAnimations = false; // are we ready to do symbol animations (this is true after payline cascade has been shown)
	protected bool useVisibleSymbolsCloneForScatter = true;

	private int bonusHits = 1;

	private bool isBigWinShown = false; // tracks if the big win is being shown, used to ensure we don't trigger sounds at the wrong time if we are syncing sound to the big win
	private float bigWinLoopBeatStartTimestamp = 0; // use this to track the timestamp of the start of the loop sound so we can cancel it with the end sound on beat

	// sound mapping constants
	protected const string DESTROY_SYMBOLS_SOUND = "tumble_symbol_disappear";
	protected const string TRANSITION_SOUND = "bonus_challenge_wipe_transition";
	protected const string TRANSITION_FREESPIN_PT1 = "transition_welcome";
	protected const string TRANSITION_FREESPIN_PT2 = "transition_welcome_outro";
	protected const string FREESPINS_BONUS_SOUND = "bonus_symbol_freespins_animate";
	protected const string PICKEM_BONUS_SOUND = "bonus_symbol_pickem_animate";
	protected const string FREESPIN_VO = "freespin_intro_vo";
	protected const string BONUS_SYMBOL_MAP_KEY = "bonus_symbol_animate";


	// sound constants	
	private const string TUMBLE_SYMBOL_HIT_PREFIX = "tumble_symbol_hit_";
	private const string PAYLINE_BASE_SOUND_MAP = "show_payline_base";


	protected const float TRANSITION_SLIDE_TIME = 0.5f;
	protected const float WING_EXPAND_TIME = TRANSITION_SLIDE_TIME * 0.75f;
	protected const float BACKGROUND_SLIDE_TIME = 2.0f;
	protected const float ANIMATION_WAIT_TIME = 1.5f;

	protected DeprecatedPlopAndTumbleOutcomeDisplayController deprecatedPlopAndTumbleOutcomeDisplayController = null;

	protected bool isDoingFirstTumble = true;
	private HashSet<SlotSymbol> symbolsBeingTumbled = new HashSet<SlotSymbol>();
	private HashSet<SlotSymbol> symbolsAnimatingAnticipationsWhileTumbling = new HashSet<SlotSymbol>();

	// Sets or gets whether the game is busy, allowing or disallowing the Scheduler to do something.
	// Adding additional support here for blocking while the plop game is initially dropping the symbols in
	//
	public override bool isGameBusy
	{
		get { return isPerformingSpin || isDoingFirstTumble; }
	}

	private bool needsToSetRootOutcome = true;
	private SlotOutcome _rootOutcome;

	protected override void Awake()
	{
		isLegacyTumbleGame = true;

		// Set this value if the game isn't overriding the default, since all tumble games need to wait on no outcome autospins
		if (noOutcomeAutospinDelay == 0.0f)
		{
			noOutcomeAutospinDelay = DEFAULT_NO_OUTCOME_AUTOSPIN_DELAY;
		}

		base.Awake();

		deprecatedPlopAndTumbleOutcomeDisplayController = _outcomeDisplayController as DeprecatedPlopAndTumbleOutcomeDisplayController;
		deprecatedPlopAndTumbleOutcomeDisplayController.setTumbleOutcomeCoroutine(tumbleAfterRollup);
	}

	// Overriding this so that tumble games provide accurate information about their big win
	public override bool isBigWinBlocking
	{
		get
		{
			return isBigWinShown;
		}
	}

	// startNextAutospin - starts a spin and decrements that auto-spin counter.
	protected override void startNextAutospin()
	{
		bonusHits = 1;
		base.startNextAutospin();
	}

	/// slotStartedEventCallback - called by Server when we first enter the slot game.
	protected override void slotStartedEventCallback(JSON data)
	{
		base.slotStartedEventCallback(data);
		
		willBeRemoved = new List<List<bool>>();
		for (int reelIndex = 0; reelIndex < getReelRootsLength(); reelIndex++)
		{
			List<bool> list = new List<bool>();
			for (int symbolIndex = 0; symbolIndex < slotGameData.numVisibleSymbols; symbolIndex++)
			{	
				list.Add(false);
			}
			willBeRemoved.Add(list);
		}

		if (paylineCamera != null)
		{
			Vector3 currentPaylinCameraPos = paylineCamera.transform.localPosition;

			if (reelGameBackground != null)
			{
				paylineCamera.transform.localPosition = new Vector3(currentPaylinCameraPos.x, currentPaylinCameraPos.y * reelGameBackground.getVerticalSpacingModifier() / reelGameBackground.scalePercent, currentPaylinCameraPos.z);
			}
		}

		isDoingFirstTumble = true;
		Overlay.instance.setButtons(false);
		StartCoroutine(plopSymbols(true));
	}


	private  IEnumerator cascadeDestroyAudio()
	{
		if (!string.IsNullOrEmpty(DESTROY_SYMBOLS_SOUND_ROOT))
		{
			for (int i=0; i < 5; i++)
			{
				Audio.play(DESTROY_SYMBOLS_SOUND_ROOT + (i+1) + DESTROY_SYMBOLS_SOUND_SUFFIX);
				yield return new TIWaitForSeconds(CASCADE_AUDIO_DELAY);
			}
		}
	}		

	private  IEnumerator fadeOutSymbols()
	{
		if (visibleSymbolClone != null)
		{		
			SlotReel[] reelArray = engine.getReelArray();
			SlotSymbol[] visibleSymbols = reelArray[0].visibleSymbols;
			for (int symbolIndex = visibleSymbols.Length-1; symbolIndex >= 0; symbolIndex--)
			{
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{				
					if(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].animator != null)
					{
						visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].fadeOutSymbol(SYMBOL_FADE_TIME);
					}

					if(fallenSymbols.ContainsKey(new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)) && fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)] != null)
					{
						fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)].fadeOutSymbol(SYMBOL_FADE_TIME);
					}
				}
			}
		}
		yield return new TIWaitForSeconds(SYMBOL_FADE_TIME);	
	}

	private  IEnumerator fadeInSymbols()
	{	
		if (visibleSymbolClone != null)
		{			
			SlotReel[] reelArray = engine.getReelArray();
			SlotSymbol[] visibleSymbols = reelArray[0].visibleSymbols;
			for (int symbolIndex = visibleSymbols.Length-1; symbolIndex >= 0; symbolIndex--)
			{
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{				
					if(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].animator != null)
					{
						StartCoroutine(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].fadeInSymbolCoroutine(SYMBOL_FADE_TIME));
					}

					if(fallenSymbols.ContainsKey(new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)) && fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)] != null)
					{
						StartCoroutine(fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)].fadeInSymbolCoroutine(SYMBOL_FADE_TIME));
					}
				}
			}
		}
		yield return new TIWaitForSeconds(SYMBOL_FADE_TIME);	
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		if (symbolsBeingTumbled.Count > 0)
		{
			Debug.LogError("TumbleSlotBaseGame.prespin() - symbolsBeingTumbled was not fully cleared.  That means some symbols didn't finish tweens/anims and visual issues may occur. symbolsBeingTumbled.Count = " + symbolsBeingTumbled.Count);
			symbolsBeingTumbled.Clear();
		}
		
		needsToSetRootOutcome = true;
		yield return StartCoroutine(base.prespin());

		if (CASCADE_AUDIO_DELAY > 0)
		{
			StartCoroutine(cascadeDestroyAudio());
		}

		if (SYMBOL_FADE_TIME > 0)
		{
			yield return StartCoroutine(fadeOutSymbols());
		}

		// override to handle changes needed to be done prespin
		SlotReel[] reelArray = engine.getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].animator != null)
				{
					Destroy(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].animator.gameObject);
				}
				if(fallenSymbols.ContainsKey(new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)) && fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)] != null)
				{
					Destroy(fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)].animator.gameObject);
				}
			}
		}
		

		if(disableStopButtonOnPreSpin && SpinPanel.instance != null && SpinPanel.instance.stopButton != null && autoSpins == 0)
		{
			//setting the "STOP" button as disabled except during autospins to mimic web.
			SpinPanel.instance.stopButton.isEnabled = false;
		}

		if (clearHelperStructsOnPreSpin)
		{
			clearOutHelperDataStructures();
		}
	}

	
	protected virtual void startSpinSkipExtra()
	{
		//Debug.LogWarning("start spin skip extra");
		symbolCamera.SetActive(false);
		base.startSpin(false);
	}

	// Destroy all visible symbols, then start the spin
	// currently not used by any games
	protected virtual IEnumerator removeSymbolsAndSpin()
	{
		SlotReel[] reelArray = engine.getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1].animator != null)
				{
					StartCoroutine(removeASymbol(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1]));
				}
				else
				{
					//Debug.LogWarning("this visible symbols was null: " + reelIndex + " -- " + symbolIndex);
				}

				if(fallenSymbols.ContainsKey(new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)) && fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)] != null)
				{
					Destroy(fallenSymbols[new KeyValuePair<int, int>(reelIndex, visibleSymbols.Length-symbolIndex-1)].animator.gameObject);
				}
			}
		}

		clearOutHelperDataStructures();
		yield return null;
		symbolCamera.SetActive(false);
		
		bonusHits = 1;
		base.startSpin(false);
		if(SpinPanel.instance != null && SpinPanel.instance.stopButton != null && autoSpins == 0)
		{
			//setting the "STOP" button as disabled except during autospins to mimic web.
			SpinPanel.instance.stopButton.isEnabled = false;
		}
	}

	protected virtual void clearOutHelperDataStructures()
	{
		// clear out our helper data structures, so the next spin is super-fresh!
		fallenSymbols.Clear();
		willBeRemoved = new List<List<bool>>();
		nowEmptySymbolPositions = new List<List<bool>>();

		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			List<bool> tempList = new List<bool>();
			for (int j = 0; j < reelArray[i].visibleSymbols.Length; j++)
			{
				tempList.Add(false);
			}
			willBeRemoved.Add(tempList);
			nowEmptySymbolPositions.Add(tempList);
		}
	}

	protected virtual IEnumerator removeASymbol(SlotSymbol symbol, bool shouldUseRevealPrefab = false)
	{
		if (!shouldUseRevealPrefab)
		{
			if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
			{
				Destroy(symbol.animator.gameObject);
			}
			yield return null;
		}
		else
		{
			if (revealPrefab != null)
			{
				if (symbol != null && symbol.animator != null && symbol.animator.gameObject != null)
				{
					GameObject reveal = CommonGameObject.instantiate(revealPrefab, symbol.animator.transform.position, Quaternion.identity) as GameObject;
					reveal.transform.localScale = revealPrefabScale;
					StartCoroutine(waitThenDestroy(reveal, TIME_TO_DESTROY_REVEAL));

					StartCoroutine(waitThenDestroy(symbol.animator.gameObject, TIME_TO_DESTROY_SYMBOL));

					yield return new TIWaitForSeconds(WINNING_SYMBOL_DESTROY_INTERVAL);
				}
			}
		}
		//symbol = null;
	}
	
	protected override void doSpecialOnBonusGameStart()
	{
		deprecatedPlopAndTumbleOutcomeDisplayController.pauseTumble();
	}
	
	public override void doSpecialOnBonusGameEnd()
	{
		playBgMusic();
		
		returnSymbolsAfterBonusGame();
		deprecatedPlopAndTumbleOutcomeDisplayController.resumeTumble();
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelID = 0; reelID < reelArray.Length; reelID++)
		{
			foreach (SlotSymbol symbol in visibleSymbolClone[reelID])
			{
				if (symbol != null && symbol.animator != null)
				{
					symbol.animator.stopAnimation(true);
				}
			}
		}
		
		SpinPanel.instance.showFeatureUI(true);
	}

	public float getBaseRollUpTime(long rollupStart, long rollupEnd)
	{
		bool shouldBigWin = isOverBigWinThreshold((rollupEnd - rollupStart) + runningPayoutRollupValue);
		float rollupTime = BASE_ROLLUP_TIME;
		
		if (shouldBigWin)
		{
			if (!Audio.muteMusic && BIG_WIN_ROLLUP_TIME >= 0.0f)
			{
				rollupTime = BIG_WIN_ROLLUP_TIME;
			}
			else
			{
				rollupTime *= 2.0f;
			}
		}
		
		return rollupTime;
	}

	/// replace normal rollup with this tumbling logic
	protected virtual IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJsonArray, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		//yield return new WaitForSeconds(.3f);
		// First need to find out what the key name of the bonus_pool is.
		if (tumbleOutcomeJsonArray.Length == 0)
		{
			yield break;
		}
		else
		{
			bool firstGo = true; // is this the first time through the loop
			TICoroutine rollupRoutine = null;
			if (playRollupSoundsWithBigWinAnimation && isOverBigWinThreshold(rollupEnd - rollupStart))
			{
				// don't play big win rollup sounds here even if a big win was triggered, 
				// we want to sync the sounds with the animation instead of the rollup
				// the rollup sounds will be triggered with the call to onBigWinNotification
				rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, rollupDelegate, false, getBaseRollUpTime(rollupStart, rollupEnd)));
			}
			else
			{
				rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, rollupDelegate, true, getBaseRollUpTime(rollupStart, rollupEnd)));
			}

			currentlyTumbling = true;
			foreach (JSON tumbleOutcomeJson in tumbleOutcomeJsonArray)
			{
				findClusterWinningSymbols();
				findScatterWinningSymbols();
				if (GameState.game.keyName != "osa06" && GameState.game.keyName != "gen09" && GameState.game.keyName != "gen39" && GameState.game.keyName != "gen21" && GameState.game.keyName != "cesar01")
				{
					findLineWinningSymbols();
				}
				yield return StartCoroutine(displayWinningSymbols());
				// Wait for the rollups to happen.
				yield return rollupRoutine;

				// wait on the pre win to be cleared, so we ensure the rollup starts
				while (_outcomeDisplayController.hasPreWinShowOutcome())
				{
					yield return null;
				}

				if (!firstGo) // only if it's not the first iteration, we should wait for spawned rollups
				{
					while (_outcomeDisplayController.rollupRoutine == null)
					{
						yield return null;
					}
				}

				if (_outcomeDisplayController.rollupRoutine != null) 
				{
					yield return _outcomeDisplayController.rollupRoutine; // this will make sure we wait until all rollups are done, whether they were started by PlopSlotBaseGame or DisplayOutcomeController
				}

				// Trigger a rollup end, since we've completed a rollup
				yield return StartCoroutine(onEndRollup(isAllowingContinueWhenReady: false));

				yield return new TIWaitForSeconds (TIME_ROLLUP_TERMINATING_WAIT);
				clearOutcomeDisplay();
				yield return StartCoroutine(removeWinningSymbols());
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_BEAT);
				yield return StartCoroutine(doExtraBeforePloppingNewSymbols());

				SlotOutcome tumbleOutcome = new SlotOutcome(tumbleOutcomeJson);
				setOutcome(tumbleOutcome);
				
				if (tumbleOutcome.isBonus)
				{
					createBonus();
				}

				yield return StartCoroutine(plopNewSymbols());
				
				if(_outcomeDisplayController.rollupsRunning.Count > 0)
				{
					_outcomeDisplayController.rollupsRunning[_outcomeDisplayController.rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
				}
				
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_ON_SPIN);
				yield return StartCoroutine(doReelsStopped(isAllowingContinueWhenReadyToEndSpin: false));
				
				clearOutHelperDataStructures();
				// When we pause this coroutine after doReelsStopped we need to wait a frame here so it knows where to
				firstGo = false;
			}
			currentlyTumbling = false;
			
			float waitAtEnd = TIME_TO_WAIT_AT_END;
			bool shouldBigWin = isOverBigWinThreshold(0);

			if (playRollupSoundsWithBigWinAnimation && isBigWinShown && !Audio.muteSound)
			{
				// end the big win rollup loop sound using the beat to ensure we cancel it at the right time
				float timeSinceBigWinLoopStarted = Time.realtimeSinceStartup - bigWinLoopBeatStartTimestamp;
				float bigWinLoopBeat = Audio.getBeat(Audio.soundMap("rollup_bigwin_loop"));

				if (bigWinLoopBeat > 0.0f)
				{
					long numberOfLoopBeatsPlayed = (long)(timeSinceBigWinLoopStarted / bigWinLoopBeat);
					float timeTillNextBeat = bigWinLoopBeat - (timeSinceBigWinLoopStarted - (numberOfLoopBeatsPlayed * bigWinLoopBeat));
					//Debug.Log("TumbleSlotBaseGame.tumbleAfterRollup() - timeSinceBigWinLoopStarted = " + timeSinceBigWinLoopStarted + "; bigWinLoopBeat = " + bigWinLoopBeat + "; numberOfLoopBeatsPlayed = " + numberOfLoopBeatsPlayed + "; timeTillNextBeat = " + timeTillNextBeat);
					yield return new TIWaitForSeconds(timeTillNextBeat);
				}

				Audio.play(Audio.soundMap("rollup_bigwin_end"));
			}
			
			// add the extra big win delay override, but only if sound is enabled if we are using this time for sounds
			if (shouldBigWin && BIG_WIN_WAIT_AT_END >= 0.0f && (!isBigWinWaitAtEndForSound || (isBigWinWaitAtEndForSound && !Audio.muteSound)))
			{
				waitAtEnd = BIG_WIN_WAIT_AT_END;
			}

			yield return new TIWaitForSeconds(waitAtEnd);

			handleBigWinEnd();
			// We're finally done with this whole outcome.
			yield return StartCoroutine(_outcomeDisplayController.finalizeRollup());
			
		}
	}

	// build list of winning symbols from scatter outcome
	protected virtual void findScatterWinningSymbols()
	{
		string[] scatterList = null;
		
		if (_outcome.isBonus && _outcome.hasSubOutcomes()) // check for suboutcomes instead of just base outcome because it might be the 'win' of getting 3 bonus symbols.
		{
			List<string> allScattersInOutcome = new List<string>();
			foreach (SlotOutcome slotOutcome in _outcome.getSubOutcomesReadOnly())
			{
				if (_outcomeDisplayController.getScatterWinSymbols(slotOutcome) != null)
				{
					allScattersInOutcome.AddRange(_outcomeDisplayController.getScatterWinSymbols(slotOutcome));
				}
			}
			if (allScattersInOutcome.Count != 0)
			{
				scatterList = allScattersInOutcome.ToArray();
			}
		}
		else // not used yet but it's possible that some game would replace Cluster outcomes with Scatter for normal outcomes in a PlopSlot
		{
			scatterList = _outcomeDisplayController.getScatterWinSymbols(_outcome); 
		}
		ClusterOutcomeDisplayModule.Cluster cluster = new ClusterOutcomeDisplayModule.Cluster(); // just use a blank cluster for keying

		if (scatterList != null) // if there are any scatter wins (for FarmVille2 this means only bonus outcomes)
		{
			SlotReel[] reelArray = engine.getReelArray();

			foreach (string symbolName in scatterList)
			{
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					for (int rowIndex = 0; rowIndex < reelArray[reelIndex].visibleSymbols.Length; rowIndex++)
					{
						SlotSymbol symbol;
						if (useVisibleSymbolsCloneForScatter && visibleSymbolClone != null)
						{
							symbol = visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-rowIndex-1];
						}
						else
						{
							symbol = reelArray[reelIndex].visibleSymbols[slotGameData.numVisibleSymbols-rowIndex-1];
						}
						if (symbol != null && symbol.serverName == symbolName)
						{
							if(!symbolsToRemove.ContainsKey(cluster))
							{
								symbolsToRemove.Add(cluster, new List<KeyValuePair<int, int>>());
							}
							KeyValuePair<int, int> pair = new KeyValuePair<int,int>(reelIndex,rowIndex);
							if (!symbolsToRemove[cluster].Contains(pair))
							{
								symbolsToRemove[cluster].Add(pair);								
								willBeRemoved[reelIndex][rowIndex] = true;
							}
						}
					}
				}
			}
		}
	}
		
	public bool hasScatterWins()
	{
		string[] scatterList = null;
		
		if (_outcome.isBonus && _outcome.hasSubOutcomes()) // check for suboutcomes instead of just base outcome because it might be the 'win' of getting 3 bonus symbols.
		{
			scatterList = _outcomeDisplayController.getScatterWinSymbols(_outcome.getSubOutcomesReadOnly()[0]);
		}
		else // not used yet but it's possible that some game would replace Cluster outcomes with Scatter for normal outcomes in a PlopSlot
		{
			scatterList = _outcomeDisplayController.getScatterWinSymbols(_outcome); 
		}
		
		return (scatterList != null && scatterList.Length > 0);
	}

	// Loop through the cluster outcomes
	protected virtual void findClusterWinningSymbols()
	{
		Dictionary<SlotOutcome,ClusterOutcomeDisplayModule.Cluster> clusterWins = _outcomeDisplayController.getClusterDisplayDictionary(); //grab dictionary of clusters

		if (clusterWins != null) // if there are any cluster wins (which for FarmVille2, at least, we know there will be. But check just to be safe)
		{
			SlotReel[] reelArray = engine.getReelArray();
			foreach (ClusterOutcomeDisplayModule.Cluster cluster in clusterWins.Values)
			{
				Dictionary<int,int[]> clusterDict = cluster.reelSymbols;
				for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
				{
					if (clusterDict.ContainsKey(reelIndex))
					{
						for (int rowIndex = 0; rowIndex < reelArray[reelIndex].visibleSymbols.Length; rowIndex++)
						{
							for(int i=0; i < clusterDict[reelIndex][rowIndex]; i++)
							{
								if(!symbolsToRemove.ContainsKey(cluster))
								{
									symbolsToRemove.Add(cluster, new List<KeyValuePair<int, int>>());
								}

								if(!symbolsToRemove[cluster].Contains(new KeyValuePair<int,int>(reelIndex,rowIndex-i)))
								{
									symbolsToRemove[cluster].Add(new KeyValuePair<int,int>(reelIndex,rowIndex-i));								
									willBeRemoved[reelIndex][rowIndex-i] = true;
								}
							}
						}
					}
				}
			}
		}
	}

	// Loop through the line outcomes and grab the winning symbols
	protected virtual void findLineWinningSymbols()
	{
		bool[,] winningSymbols = _outcomeDisplayController.getLineWinSymbols();
		if (winningSymbols != null)
		{
			for(int i = 0; i < winningSymbols.GetLength(0); i++)
			{
				for(int j = 0; j < winningSymbols.GetLength(1); j++)
				{
					if (winningSymbols[i,j])
					{
						willBeRemoved[i][j] = true;
					}
				}
			}
		}

	}

	// the payline cascade is done, so we can start the symbol animations
	public void isReadyForSymbolAnimations()
	{
		isReadyForAnimations = true;
	}
	
	protected virtual IEnumerator displayWinningSymbols()
	{
		bool hasShownCluster = false;
		
		// Make a copy of the dictionary in case the original is edited during the yields.
		Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>> tempRemoveDict =
			new Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>>(symbolsToRemove);
		
		foreach (KeyValuePair<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>> p in tempRemoveDict)
		{
			ClusterOutcomeDisplayModule.Cluster cluster = p.Key;
			List<KeyValuePair<int,int>> symbolList = p.Value;
			
			// Null checking because we yield, which makes it possible for the collection to change.
			// Attempt to address https://app.crittercism.com/developers/crash-details/5616f9118d4d8c0a00d07cf0/a95f2af1c137c06e0c7526c1d0c7210905c4428d9b4a8656bc5b12cf
			if (symbolList != null)
			{
				Audio.play (Audio.soundMap(PAYLINE_BASE_SOUND_MAP));
				yield return StartCoroutine(showWinningCluster(symbolList, cluster));
				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_AFTER_PAYBOXES);
				doSpecialOnSymbolsBelowAfterPayboxes();
			}
			
			hasShownCluster = true;
		}

		
		if (!hasShownCluster)
		{
			// Wait until payline cascade is done
			while (!isReadyForAnimations)
			{
				yield return null;
			}
			bool shouldSkip = false;
			for (int i=0; i < _outcomeDisplayController.getNumLoopedOutcomes() && i < 2; i++)
			{
				deprecatedPlopAndTumbleOutcomeDisplayController.playOneOutcome(i);
				isReadyForAnimations = false;
				yield return null;

				while(!isReadyForAnimations && !shouldSkip)
				{
					if (TouchInput.didTap)
					{
						shouldSkip = true;
					}
					yield return null;
				}
				
				if (shouldSkip)
				{
					break;
				}
			}
			
			isReadyForAnimations = false;
		}
	}
	
	protected virtual IEnumerator removeWinningSymbols()
	{
		yield return StartCoroutine(removeAllWinningSymbols());
	}
	
	protected virtual IEnumerator doExtraBeforePloppingNewSymbols()
	{
		yield return null;
	}

	// go through all the winning symbols and remove them one by one regardless of which cluster they're in
	protected virtual IEnumerator removeAllWinningSymbols()
	{
		if (playDestroySymbolAudioOnce)
		{
			Audio.play(Audio.soundMap(DESTROY_SYMBOLS_SOUND));
		}

		for (int i=0; i < willBeRemoved.Count; i++)
		{
			for (int j=0; j < willBeRemoved[i].Count; j++)
			{
				if (willBeRemoved[i][j])
				{
					SlotSymbol symbol = visibleSymbolClone[i][visibleSymbolClone[i].Count-j-1];
					StartCoroutine(removeASymbol(symbol, true));

					if (!string.IsNullOrEmpty(DESTROY_SYMBOLS_SOUND_ROOT))
					{
						Audio.play (DESTROY_SYMBOLS_SOUND_ROOT + (i+1) + DESTROY_SYMBOLS_SOUND_SUFFIX);
					}
					else if (!playDestroySymbolAudioOnce)
					{
						Audio.play (Audio.soundMap(DESTROY_SYMBOLS_SOUND));
					}
				}
			}
			yield return new TIWaitForSeconds(TIME_BETWEEN_REMOVALS);
		}
		yield return null;
	}

	// Function to get the wait time between clusters, this normally uses a default legacy formula
	// but in Cesar01 Sean wanted to see if we could speed things up a bit, so I've added the ability
	// for a slot module to override this delay to be whatever someone wants
	protected float getWaitTimePerCluster()
	{
		// Check if the big win is being delayed and will most likely be triggered by a module
		for (int i = 0; i < cachedAttachedSlotModules.Count; i++)
		{
			SlotModule module = cachedAttachedSlotModules[i];

			if (module.needsToOverrideLegacyPlopTumbleWaitTimePerCluster())
			{
				// a module is overriding the default value, so return this altered value
				return module.getLegacyPlopTumbleWaitTimePerClusterOverride();
			}
		}

		return TIME_MOVE_SYMBOL_UP + TIME_MOVE_SYMBOL_DOWN + TIME_FADE_SHOW_IN + TIME_FADE_SHOW_OUT + TIME_SHOW_DURATION + TIME_POST_SHOW + TIME_EXTRA_WAIT_AFTER_PAYBOXES;
	}

	// for this symbolList (1 specific cluster outcome) raise the symbols, show the paybox and lower them back down
	protected virtual IEnumerator showWinningCluster(List<KeyValuePair<int,int>> symbolList, ClusterOutcomeDisplayModule.Cluster cluster)
	{
		int symbolNum = 0;
		bool hasShownCluster = false;

		SlotReel[] reelArray = engine.getReelArray();

		foreach (KeyValuePair<int,int> pair in symbolList)
		{
			for (int j = 0; j < pair.Value; j++)
			{
				if (! symbolList.Contains(new KeyValuePair<int, int>(pair.Key, j)))
				{
					SlotSymbol symbolBelow = reelArray[pair.Key].visibleSymbols[slotGameData.numVisibleSymbols-j-1];
					doSpecialOnSymbolBelow(symbolBelow);
				}
			}
			SlotSymbol symbol = reelArray[pair.Key].visibleSymbols[slotGameData.numVisibleSymbols-pair.Value-1];
			StartCoroutine(doWinMovementAndPaybox(symbol, pair, cluster, symbolNum, hasShownCluster));
			symbolNum++;
			hasShownCluster = true;
		}
		yield return new WaitForSeconds(getWaitTimePerCluster());
	}

	// if we need to do something special to symbols that appear below winning symbols during paybox showing
	protected virtual void doSpecialOnSymbolBelow(SlotSymbol symbol)
	{
	}

	// do we need to do something special to symbols that appear below winning symbols after paybox showing?
	protected virtual void doSpecialOnSymbolsBelowAfterPayboxes()
	{
	}
	
	// Wait for the symbol to plop away and then deactivate it
	protected virtual IEnumerator doWinMovementAndPaybox(SlotSymbol symbol, KeyValuePair<int,int> pair, ClusterOutcomeDisplayModule.Cluster cluster, int symbolNum = 0, bool hasDoneCluster = false )
	{
		PlopClusterScript plopCluster = null;
		if (cluster.clusterScript != null && !hasDoneCluster) // could be null if we're dealing with scatter outcome (like from Bonus Game)
		{
			plopCluster = cluster.clusterScript as PlopClusterScript;
			if (plopCluster != null)
			{
				yield return StartCoroutine(plopCluster.specialShow(TIME_FADE_SHOW_IN, 0.0f, WIN_SYMBOL_RAISE_DISTANCE));
			}

		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_IN);
		}

		yield return new TIWaitForSeconds(TIME_SHOW_DURATION);

		if (plopCluster != null && !hasDoneCluster)
		{
			yield return StartCoroutine(plopCluster.specialHide(TIME_FADE_SHOW_OUT));
		}
		else
		{
			yield return new TIWaitForSeconds(TIME_FADE_SHOW_OUT);
		}
		yield return new TIWaitForSeconds(TIME_POST_SHOW);
	}

	// Static function to calculate the number of bonus symbols hit so the correct bonus init sound is played
	// exists so that TumbleSlotBaseGame and TumbleFreeSpinGame can share the same code
	// NOTE: will only be used if shouldPlayBonusSymbolFanfareOnBringDown is true
	public static int calculateNumberOfBonusHits(ReelGame reelGame, List<List<SlotSymbol>> visibleSymbolClone, List<List<bool>> willBeRemoved)
	{
		// Add up how many visible bonus symbols there are.
		// Subtract how many visible bonus symbols it's going to bring down.
		
		int numVisibleBonusSymbols = 0;
		int numVisibleBonusSymbolsItsBringingDown = 0;

		SlotReel[] reelArray = reelGame.engine.getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			int startingSymbolMinimum = 0;
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				SlotSymbol visibleSymbol = visibleSymbolClone[reelIndex][reelGame.slotGameData.numVisibleSymbols-symbolIndex-1];
				bool willRemove = willBeRemoved[reelIndex][symbolIndex];
				
				if (visibleSymbol.isBonusSymbol)
				{
					// If it's not going to remove this bonus symbol, then add it to the number of visible bonus symbols.
					// (If it is going to remove this bonus symbol, then it's not really visible, so don't add it).
					
					if (!willRemove)
					{						
						numVisibleBonusSymbols++;
					}
				}
				
				if (willRemove)
				{
					for (int nextSymbolIndex = System.Math.Max(symbolIndex + 1, startingSymbolMinimum) ; nextSymbolIndex < visibleSymbols.Length; nextSymbolIndex++)
					{
						SlotSymbol symbolToBringDown = visibleSymbolClone[reelIndex][reelGame.slotGameData.numVisibleSymbols-nextSymbolIndex-1];

						if (symbolToBringDown != null && symbolToBringDown.animator != null)
						{
							if (symbolToBringDown.isBonusSymbol)
							{
								numVisibleBonusSymbolsItsBringingDown++;
							}
							
							startingSymbolMinimum = nextSymbolIndex + 1;
						}
					}
				}
			}
		}
		
		// Calculate bonus hits.
		return 1 + numVisibleBonusSymbols - numVisibleBonusSymbolsItsBringingDown;
	}

	// Loop through all symbols in the visible area. If we need to replace it, find the replacement symbol, create it and update
	protected virtual IEnumerator plopNewSymbols()
	{
		nowEmptySymbolPositions = new List<List<bool>>();
		for (int reelIndex = 0; reelIndex < getReelRootsLength(); reelIndex++)
		{
			List<bool> list = new List<bool>();
			for (int symbolIndex = 0; symbolIndex < slotGameData.numVisibleSymbols; symbolIndex++)
			{	
				list.Add(false);
			}
			nowEmptySymbolPositions.Add(list);
		}

		if (shouldPlayBonusSymbolFanfareOnBringDown)
		{
			bonusHits = calculateNumberOfBonusHits(this, visibleSymbolClone, willBeRemoved);
		}

		SlotReel[] reelArray = engine.getReelArray();
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			int startingSymbolMinimum = 0;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if (willBeRemoved[reelIndex][symbolIndex])
				{
					SlotSymbol symbolToBringDown = null;
					for (int nextSymbolIndex = System.Math.Max(symbolIndex + 1, startingSymbolMinimum) ; nextSymbolIndex < visibleSymbols.Length; nextSymbolIndex++)
					{
						if (visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1].animator != null)
						{							
							symbolToBringDown = visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1];
							SymbolInfo symInfo = symbolToBringDown.info;
							
							if (symbolToBringDown.isBonusSymbol && shouldPlayBonusSymbolFanfareOnBringDown && !shouldPlayBonusSymbolFanfareWithAntAnim)
							{
								bonusHits = playBonusSymbolFanfare(reelIndex, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
							}

							if (symInfo != null)
							{
								coroutineList.Add(StartCoroutine(symbolToBringDown.fallDown(symbolIndex, 10.0f, iTween.EaseType.easeOutBounce, originalPositions[new KeyValuePair<int,int>(reelIndex, symbolIndex)].y + symInfo.positioning.y, onSymbolTweenFinish)));
							}
							else
							{
								coroutineList.Add(StartCoroutine(symbolToBringDown.fallDown(symbolIndex, 10.0f, iTween.EaseType.easeOutBounce, originalPositions[new KeyValuePair<int,int>(reelIndex, symbolIndex)].y, onSymbolTweenFinish)));
							}

							KeyValuePair<int, int> symbolPosition = new KeyValuePair<int, int>(reelIndex, slotGameData.numVisibleSymbols-nextSymbolIndex-1);
							if (!fallenSymbols.ContainsKey(symbolPosition))
							{
								fallenSymbols.Add(symbolPosition, symbolToBringDown);
							}

							//Debug.LogWarning("Adding to fallenSymbols: " + reelIndex + " -- " + (slotGameData.numVisibleSymbols-nextSymbolIndex-1) + " and it was: " + symbolToBringDown.name);
							Audio.play (Audio.soundMap(TUMBLE_SYMBOL_HIT_PREFIX + (reelIndex+1) + "_" + (symbolIndex+1)));
							visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-symbolIndex-1] = symbolToBringDown;
							visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1] = null;
							nowEmptySymbolPositions[reelIndex][symbolIndex] = false;
							nowEmptySymbolPositions[reelIndex][nextSymbolIndex] = true;
							willBeRemoved[reelIndex][nextSymbolIndex] = true;
							yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
							startingSymbolMinimum = nextSymbolIndex + 1;
							break;
						}
					}

					if (symbolToBringDown == null)
					{
						//nowEmptySymbolPositions[reelIndex][symbolIndex] = true;
						string nextSymbolName = reelArray[reelIndex].getNextSymbolName(slotGameData.numVisibleSymbols);
						
						SlotSymbol newSymbol = visibleSymbolClone[reelIndex][reelArray[reelIndex].visibleSymbols.Length-symbolIndex-1] = new SlotSymbol(this);
  						newSymbol.setupSymbol(nextSymbolName, reelArray[reelIndex].visibleSymbols.Length-symbolIndex-1, reelArray[reelIndex]);

						nowEmptySymbolPositions[reelIndex][symbolIndex] = false;
						coroutineList.Add(StartCoroutine(tumbleSymbolAt(reelIndex, symbolIndex)));
						yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
					}
				}
			}
		}
		yield return null;
		symbolsToRemove.Clear();
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	public void onSymbolTweenFinish(SlotSymbol symbol)
	{
		if (symbol.isBonusSymbol && shouldPlayBonusSymbolFanfareWithAntAnim)
		{
			bonusHits = playBonusSymbolFanfare(symbol.reel.reelID - 1, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
		}

		TumbleSlotBaseGame.staticOnSymbolTweenFinish(symbol, shouldTumblePlayAnticipation, onAnticipationAnimationAfterTweenComplete);
	}

	// Callback function for after a symbol animations an anticipating while tumbling
	// which will block the overall tumble coroutine at the end until it is done
	private void onAnticipationAnimationDuringTumbleComplete(SlotSymbol symbol)
	{
		if (symbol != null && symbolsAnimatingAnticipationsWhileTumbling.Contains(symbol))
		{
			symbolsAnimatingAnticipationsWhileTumbling.Remove(symbol);
		}
	}
	
	// Callback function for after a bonus symbol animates to ensure that it gets
	// removed from the list of tumbling symbols, so that the game will unlock
	// correctly
	private void onAnticipationAnimationAfterTweenComplete(SlotSymbol symbol)
	{
		if (symbol != null && symbolsBeingTumbled.Contains(symbol))
		{
			symbolsBeingTumbled.Remove(symbol);
		}
	}

	public static void staticOnSymbolTweenFinish(SlotSymbol symbol, bool wasAnticipationPlayedOnTumble, SlotSymbol.AnimateDoneDelegate onAnimationDoneCallback)
	{
		if (!wasAnticipationPlayedOnTumble && symbol != null && symbol.isBonusSymbol)
		{
			if (ReelGame.activeGame != null && ReelGame.activeGame.isGameUsingOptimizedFlattenedSymbols)
			{
				//The symbols move when being unflattened. This is to make sure they stay in the correct spot when they animate
				Vector3 originalPos = symbol.transform.position;
				symbol.mutateToUnflattenedVersion();
				symbol.transform.position = originalPos;
			}

			symbol.animateAnticipation(onAnimationDoneCallback);
		}
		else
		{
			// If we don't actually do the animation still trigger the callback to ensure
			// that the symbol is removed from the list of symbols which are still tumbling
			if (onAnimationDoneCallback != null)
			{
				onAnimationDoneCallback(symbol);
			}
		}
	}

	protected virtual IEnumerator tumbleSymbolAt(int row, int column)
	{
		SlotReel[] reelArray = engine.getReelArray();
		SlotSymbol symbolToTumble = visibleSymbolClone[row][reelArray[row].visibleSymbols.Length - column - 1];
		symbolsBeingTumbled.Add(symbolToTumble);

		float reelBackgroundVertAdjust = 1.0f;
		if (reelGameBackground != null)
		{
			reelBackgroundVertAdjust = reelGameBackground.getVerticalSpacingModifier();
		}

		StartCoroutine(symbolToTumble.tumbleDown(column, TUMBLE_SYMBOL_SPEED, iTween.EaseType.easeOutBounce, -MAGIC_DISTANCE * reelBackgroundVertAdjust, onSymbolTweenFinish));

		// Only squish gen09 tumbles for now. Need to make this cleaner and more consistent.
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		if (GameState.game.keyName == "gen09" || GameState.game.keyName == "gen39")
		{
			coroutineList.Add(StartCoroutine(symbolToTumble.doTumbleSquashAndSquish()));
		}

		if (symbolToTumble.name == "TW")
		{
			coroutineList.Add(StartCoroutine(doSpecialTWAnims(symbolToTumble)));
		}

		if (visibleSymbolClone[row][reelArray[row].visibleSymbols.Length-column-1].isBonusSymbol && !shouldPlayBonusSymbolFanfareWithAntAnim)
		{
			bonusHits = playBonusSymbolFanfare(row, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
		}

		SlotSymbol tumbledSymbol = visibleSymbolClone[row][reelArray[row].visibleSymbols.Length-column-1];
		// make sure the newly tumbled symbol knows what reel it should be on
		symbolToTumble.transferSymbol(tumbledSymbol, tumbledSymbol.index, reelArray[row]);

		yield return new TIWaitForSeconds(tumbleTime);
		
		if (shouldTumblePlayAnticipation)
		{
			symbolsAnimatingAnticipationsWhileTumbling.Add(symbolToTumble);
			symbolToTumble.animateAnticipation(onAnticipationAnimationDuringTumbleComplete);
		}

		Audio.play (Audio.soundMap(TUMBLE_SYMBOL_HIT_PREFIX + (row+1) + "_" + (column+1)));
		
		// Ensure that anims triggered while the symbol is tumbling are finished
		while (symbolsAnimatingAnticipationsWhileTumbling.Contains(symbolToTumble))
		{
			yield return null;
		}
		
		// Ensure that the tween finishes before we end this coroutine
		while (symbolsBeingTumbled.Contains(symbolToTumble))
		{
			yield return null;
		}
		
		// Ensure additional coroutines launched during this coroutine are also finished
		if (coroutineList.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
	}

	// Static function to play the bonus symbol fanfare for TumbleSlotBaseGame or TumbleFreeSpinGame
	public static int playBonusSymbolFanfare(int row, int bonusHits, float bonusSymbolFanfareDelay, bool alwaysPlayBonusSymbolFanfare)
	{
		if ((row == (bonusHits - 1)) || alwaysPlayBonusSymbolFanfare)
		{
			float timeDelay = (bonusHits - 1) * bonusSymbolFanfareDelay;
			if ((GameState.game.keyName == "gen09" || GameState.game.keyName == "gen39") && bonusHits == 4)
			{
				// Leaving a note in here, as we need to explicitly not have a symbol fanfare on the 3rd one in this game.
			}
			else
			{
				if (ReelGame.activeGame.isFreeSpinGame() && Audio.canSoundBeMapped("freespin_bonus_symbol_fanfare" + bonusHits))
				{
					Audio.play(Audio.soundMap("freespin_bonus_symbol_fanfare" + bonusHits), 1f, 0f, timeDelay);
				}
				else
				{
					Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits), 1f, 0f, timeDelay);
				}
			}
			
			// bonus hits increased
			return bonusHits + 1;
		}

		// bonus hits didn't change, return the same value
		return bonusHits;
	}

	protected virtual IEnumerator doSpecialTWAnims(SlotSymbol symbol)
	{
		yield return null;
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		if (_outcome.isBonus)
		{
			createBonus();
		}

		StartCoroutine(plopSymbols());
	}

	// Iterate through all symbols, setup our columns GameObject arrays
	protected virtual IEnumerator plopSymbols(bool firstTime = false)
	{
		// Wait for background size to update, otherwise we will grab a slightly wrong position
		yield return StartCoroutine(waitForReelGameBackgroundScalingUpdate());
		
		symbolCamera.SetActive(true);
		visibleSymbolClone = new List<List<SlotSymbol>>();
		SlotReel[] reelArray = engine.getReelArray();
		
		List<TICoroutine> coroutineList = new List<TICoroutine>();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			List<SlotSymbol> symbolList = new List<SlotSymbol>();
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{
				symbolList.Add(visibleSymbols[symbolIndex]);
			}
			visibleSymbolClone.Add(symbolList);
		}
		originalPositions.Clear();  
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			coroutineList.Add(StartCoroutine(tumbleColumn(visibleSymbols, reelIndex)));
			yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
		}
		
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));

		if (!firstTime)
		{
			StartCoroutine(doReelsStopped());
#if ZYNGA_TRAMP
			// TumbleSlotBaseGame does custom stuff on reelsStoppedCallback so
			// we have to make sure spinFinished event hits when TRAMP plays these games
			AutomatedPlayer.spinFinished();
#endif
		}
		else
		{
			isDoingFirstTumble = false;
			Overlay.instance.setButtons(true);
		}
	}

	// handle plopping for a specific column
	protected virtual IEnumerator tumbleColumn(SlotSymbol[] visibleSymbols, int reelIndex)
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		
		for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
		{
			SlotSymbol symbol = visibleSymbols[visibleSymbols.Length-symbolIndex-1];
			SymbolInfo symInfo = symbol.info;
			if (symInfo != null)
			{
				originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator.gameObject.transform.localPosition - symInfo.positioning + new Vector3(0.0f, -MAGIC_DISTANCE, 0.0f));
			}
			else
			{
				originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), visibleSymbols[visibleSymbols.Length-symbolIndex-1].animator.gameObject.transform.localPosition + new Vector3(0.0f, -MAGIC_DISTANCE, 0.0f));
			}
			coroutineList.Add(StartCoroutine(tumbleSymbolAt(reelIndex, symbolIndex)));
			yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
		}
		
		// Make sure all symbols have fully tumbled in this column before saying the column is done
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	// most games would not have to clear symbols before showing portals.
	// FarmVille2 has to due to the 3D nature of the symbols
	public virtual IEnumerator clearSymbolsForBonusGame()
	{
		yield return null;
	}

	// return hidden symbols. not necessary in standard 2D plop games with normal portals
	protected virtual void returnSymbolsAfterBonusGame()
	{
		return;
	}

	protected virtual IEnumerator animateAllBonusSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int symbolIndex = 0; symbolIndex < visibleSymbolClone[reelIndex].Count; symbolIndex++)
			{
				SlotSymbol symbol = visibleSymbolClone[reelIndex][symbolIndex];
				
				if (symbol.isBonusSymbol && symbol.hasAnimator)
				{
					while (symbol.animator.isDoingSomething)
					{
						yield return null;
					}
				}
			}
		}

		float waitTime = 2.0f;
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int symbolIndex = 0; symbolIndex < visibleSymbolClone[reelIndex].Count; symbolIndex++)
			{
				SlotSymbol symbol = visibleSymbolClone[reelIndex][symbolIndex];
				
				if (symbol.isBonusSymbol && symbol.hasAnimator)
				{
					// find out if we have a custom bonus _Outcome symbol to swap in and play
					string symbolNameToMutateTo = symbol.name + "_Outcome";
					SymbolInfo info = findSymbolInfo(symbolNameToMutateTo);
					if (info != null)
					{
						Vector3 localPos = symbol.gameObject.transform.localPosition;
						symbol.mutateTo(symbolNameToMutateTo, null, true, true);
						symbol.transform.localPosition = localPos;
						symbol.animateOutcome(null);
						waitTime = info.customAnimationDurationOverride;
					}
					else
					{
						// if we don't have a custom one to play, then just animate the outcome on the current one
						symbol.animateOutcome(null);
					}
				}
			}
		}

		yield return new TIWaitForSeconds(waitTime);
	}

	
	// Called by OutcomeDisplayController when a big win is triggered
	protected override void onBigWinNotification(long payout, bool isSettingStartingAmountToPayout = false)
	{
		Overlay.instance.setButtons(false);
		if (SpinPanel.instance.isShowingSpecialWinOverlay)
		{
			Overlay.instance.jackpotMystery.hide();
		}
		if (bigWin == null)
		{
			Debug.LogWarning("no big win set for " + GameState.game.keyName);
		}
		
		if (bigWin && bigWinGameObject == null)
		{
			bigWinGameObject = (GameObject)CommonGameObject.instantiate(bigWin, bigWin.transform.position, this.transform.rotation);
			bigWinGameObject.transform.parent = this.transform;
			bigWinGameObject.SetActive(true);
		}
		else if (bigWin && doBigWinScaling)
		{
			Vector3 originalScale = bigWinGameObject.transform.localScale;
			iTween.ScaleTo(bigWinGameObject, iTween.Hash("scale", originalScale * 1.2f, "time", 1f, "easetype", iTween.EaseType.linear));
		}

		string bigWinVOSound = Audio.soundMap("bigwin_vo_sweetener");
		if (!isBigWinShown && bigWinVOSound != null && bigWinVOSound != "")
		{
			Audio.play(bigWinVOSound, 1.0f, 0.0f, 1.5f);
		}

		if (playRollupSoundsWithBigWinAnimation && !isBigWinShown && !Audio.muteSound)
		{
			// trigger the rollup loop here, will be canceled before handleBigWinEnd()
			Audio.play(Audio.soundMap("rollup_bigwin_loop"));
			bigWinLoopBeatStartTimestamp = Time.realtimeSinceStartup;
		}

		isBigWinShown = true;
	}

	// We get some weird anticipation information in tumble games so we need to do this.
	public override IEnumerator playBonusAcquiredEffects()
	{
		if (doEngineBonusFx)
		{
			yield return StartCoroutine(base.playBonusAcquiredEffects());
		}
		else if (outcome.isBonus)
		{
			Audio.play(Audio.soundMap(BONUS_SYMBOL_MAP_KEY), 1, 0, BONUS_SYMBOL_ANIMATE_SOUND_DELAY);
			yield return StartCoroutine(animateAllBonusSymbols());
			isBonusOutcomePlayed = true;
		}
	}

	
	// called by PlopSlotBaseGame when the outcome is all done
	protected virtual void handleBigWinEnd()
	{
		if (bigWinGameObject != null)
		{
			bigWinEndCallback(runningPayoutRollupValue);
			if (SpinPanel.instance.isShowingSpecialWinOverlay)
			{
				Overlay.instance.jackpotMystery.show();
			}
			if (tokenBar != null && SpinPanel.instance.isShowingCollectionOverlay)
			{
				StartCoroutine(tokenBar.setTokenState());
			}
		}

		isBigWinShown = false;
	}
	
	/// SlotOutcome overload for setting outcome.
	/// Needs to be overridable on the off-chance that a game (for instance the legacy Zynga01) 
	/// doesn't want automatic handling of reevaluation spins
	public override void setOutcome(SlotOutcome outcome)
	{
		bonusHits = 1;

		_outcome = outcome;
		_outcome.printOutcome();
		_outcome.processBonus();
		//Only need to set this once at the beginning of the spin when we get a outcome from the server
		if (needsToSetRootOutcome)
		{
			_rootOutcome = outcome;
			needsToSetRootOutcome = false;
		}
		else //If we're tumbling in new symbols then lets set the parent outcome to what our original thing from the server was
		{
			_outcome.setParentOutcome(_rootOutcome);
		}
		
		// change the reel set if the tier changes
		if (_outcome.getReelSet() != currentReelSetName)
		{
			setReelSet(_outcome.getReelSet());
		}
		
		// removed the automatic reevaluation spin stuff since Zynga01 handles this stuff itself
		mutationManager.setMutationsFromOutcome(outcome.getJsonObject());
	}

	
	// this game doesn't need swipeable reels, so don't do anything
	protected override void setSwipeableReels()
	{}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
	}

	public override IEnumerator onEndRollup(bool isAllowingContinueWhenReady, bool isAddingRollupToRunningPayout = true)
	{
		if (isAllowingContinueWhenReady)
		{
			yield return StartCoroutine(base.onEndRollup(isAllowingContinueWhenReady, isAddingRollupToRunningPayout));
		}
		else
		{
			if (isAddingRollupToRunningPayout)
			{
				moveLastPayoutIntoRunningPayoutRollupValue();
			}
			else
			{
				lastPayoutRollupValue = 0;
			}
		}

		// since the tumble game will payout the running rollup as it tumbles we need to make sure that we track what is already paid out
		runningPayoutRollupAlreadyPaidOut = runningPayoutRollupValue;
	}
}
