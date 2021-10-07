using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TumbleFreeSpinGame : FreeSpinGame
{	
	// inspector variables
	public GameObject symbolCamera;
	public GameObject revealPrefab;
	public Vector3 revealPrefabScale;
	public float tumbleTime;

	[SerializeField] public Camera paylineCamera = null;						// payline camera y poisiton has to adjust by the ReelGameBackground.getVerticalSpacingModifier
	[SerializeField] protected  float TIME_BETWEEN_REMOVALS = 0.05f;
	[SerializeField] protected bool  playDestroySymbolAudioOnce = true;	
	[SerializeField] protected bool  playDestroySymbolAudioOnPreSpin = false;	
	[SerializeField] protected float WINNING_SYMBOL_DESTROY_INTERVAL = 0.1f;
	[SerializeField] protected float TIME_TO_DESTROY_REVEAL = 0.83f;
	[SerializeField] protected float TIME_TO_DESTROY_SYMBOL = 0.2f;
	[SerializeField] protected float TUMBLE_SYMBOL_SPEED = 10.0f;
	[SerializeField] protected float TIME_TO_REMOVE_SYMBOL = 0.0f;
	[SerializeField] protected float SYMBOL_ANIMATION_LENGTH = 1.0f;
	[SerializeField] protected float TIME_BETWEEN_PLOPS = 0.05f;

	// Bonus symbols
	[SerializeField] protected float BONUS_SYMBOL_FANFARE_DELAY = 0.15f;
	[SerializeField] protected float BONUS_SYMBOL_ANIMATE_SOUND_DELAY = 0.0f;
	[SerializeField] private bool alwaysPlayBonusSymbolFanfare = false;
	[SerializeField] private bool shouldPlayBonusSymbolFanfareOnBringDown = false;
	[SerializeField] private bool shouldPlayBonusSymbolFanfareWithAntAnim = false; // play the fanfare when the symbol actually animates, overrides other types of playing of the fanfare
	[SerializeField] private bool shouldPlayBonusSymbolAnticipationOncePerSymbol = false; // allows for BN anticipation animations to only be played once per symbol

	[Tooltip("If this string is set then removeAllWinningSymbols will postfix numbers to it for generating destory sound audio.")]
	[SerializeField] private  string DESTROY_SYMBOLS_SOUND_ROOT;
	[SerializeField] private string DESTROY_SYMBOLS_SOUND_SUFFIX = "";

	[Tooltip("If > 0 preSpin will play the DESTROY_SYMBOLS_SOUND_ROOT sounds in order.")]
	[SerializeField] private  float	 CASCADE_AUDIO_DELAY;

	[Tooltip("If > 0 preSpin will fade out the symbols")]
	[SerializeField] private  float SYMBOL_FADE_TIME;

	[SerializeField] private  bool WAIT_EXTRA_TIME_IN_PRESPIN = true;
	[SerializeField] protected float TIME_EXTRA_WAIT_BEAT = 0.5f;


	[SerializeField] protected float MAGIC_DISTANCE = 6.0f;

	protected List<List<bool>> willBeRemoved;
	protected List<List<bool>> nowEmptySymbolPositions;
	public List<List<SlotSymbol>> visibleSymbolClone;
	protected Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>> symbolsToRemove = new Dictionary<ClusterOutcomeDisplayModule.Cluster, List<KeyValuePair<int,int>>>();
	protected Dictionary<KeyValuePair<int,int>, Vector3> originalPositions = new Dictionary<KeyValuePair<int, int>, Vector3>();

	// holds references to symbols that have fallen to new positiosn (that were part of visibleSymbols when they fell)
	protected Dictionary<KeyValuePair<int,int>, SlotSymbol> fallenSymbols = new Dictionary<KeyValuePair<int, int>, SlotSymbol>(); 

	// timing variables

	protected const float TIME_TO_ROLLUP_PER_CLUSTER = 0.5f;
	protected const float TIME_TO_WAIT_AT_END = 0.5f;
	protected const float TIME_TO_PLOP_DOWN = 0.5f;
	protected const float TIME_MOVE_SYMBOL_UP = 0.25f;
	protected const float TIME_MOVE_SYMBOL_DOWN = 0.25f;
	protected const float TIME_FADE_SHOW_IN = 0.125f;
	protected const float TIME_FADE_SHOW_OUT = 0.125f;
	protected const float TIME_SHOW_DURATION = 0.5f;
	protected const float TIME_POST_SHOW = 0.3f;
	protected const float TIME_EXTRA_WAIT_ON_SPIN = 0.3f;
	protected const float TIME_EXTRA_WAIT_AFTER_PAYBOXES = 0.1f;
	[SerializeField] protected float TIME_ROLLUP_TERMINATING_WAIT = 0.6f;
	public const float BASE_ROLLUP_TIME = 0.5f;
	protected const float WIN_SYMBOL_RAISE_DISTANCE = -0.6f;	
	private const float DEFAULT_NO_OUTCOME_AUTOSPIN_DELAY = 0.75f;	// Default value for this delay if it wasn't set on the prefab


	private bool isReadyForAnimations = false;
	protected bool useVisibleSymbolsCloneForScatter = true;

	protected DeprecatedPlopAndTumbleOutcomeDisplayController deprecatedPlopAndTumbleOutcomeDisplayController = null;


	protected const string DESTROY_SYMBOLS_SOUND = "tumble_symbol_disappear";
	private const string TUMBLE_SYMBOL_HIT_PREFIX = "tumble_symbol_hit_";
	private const string PAYLINE_FREESPIN_SOUND_MAP = "show_payline_freespin";
	protected const string INTRO_MUSIC = "freespin_intro";

	// Keep track of the tumble outcome we are currently processing
	protected JSON currentTumbleOutcome;

	private int bonusHits = 1;

	private HashSet<SlotSymbol> bonusSymbolsWhichHaveAnimatedAnticipation = new HashSet<SlotSymbol>();
	private HashSet<SlotSymbol> symbolsBeingTumbled = new HashSet<SlotSymbol>();

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
	}

	public override void initFreespins()
	{
		base.initFreespins();

		if (deprecatedPlopAndTumbleOutcomeDisplayController == null)
		{
			// ensure that we have this setup, I think in some cases initFreespins is called before the game has Awake() called
			deprecatedPlopAndTumbleOutcomeDisplayController = _outcomeDisplayController as DeprecatedPlopAndTumbleOutcomeDisplayController;
		}

		SpinPanel.instance.showSideInfo(showSideInfo);

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
		deprecatedPlopAndTumbleOutcomeDisplayController.setTumbleOutcomeCoroutine(tumbleAfterRollup);

		if (paylineCamera != null)
		{
			Vector3 currentPaylinCameraPos = paylineCamera.transform.localPosition;

			if (reelGameBackground != null)
			{
				paylineCamera.transform.localPosition = new Vector3(currentPaylinCameraPos.x, currentPaylinCameraPos.y * reelGameBackground.getVerticalSpacingModifier() / reelGameBackground.scalePercent, currentPaylinCameraPos.z);
			}
		}
	}

	protected override void startNextFreespin()
	{
		bonusHits = 1;
		base.startNextFreespin();
	}

	public void startSpinSkipExtra()
	{
		symbolCamera.SetActive(false);
		base.startSpin(false);
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
			yield return new TIWaitForSeconds(SYMBOL_FADE_TIME);	
		}
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

	protected override IEnumerator prespin()
	{
		reevaluationSpinMultiplierOverride = -1;

		if (symbolsBeingTumbled.Count > 0)
		{
			Debug.LogError("TumbleFreeSpinGame.prespin() - symbolsBeingTumbled was not fully cleared.  That means some symbols didn't finish tweens/anims and visual issues may occur. symbolsBeingTumbled.Count = " + symbolsBeingTumbled.Count);
			symbolsBeingTumbled.Clear();
		}
		
		// clear this each full spin
		if (bonusSymbolsWhichHaveAnimatedAnticipation.Count > 0)
		{
			bonusSymbolsWhichHaveAnimatedAnticipation.Clear();
		}

		yield return StartCoroutine(base.prespin());

		if (CASCADE_AUDIO_DELAY > 0)
		{
			StartCoroutine(cascadeDestroyAudio());
		}

		if (SYMBOL_FADE_TIME > 0)
		{
			yield return StartCoroutine(fadeOutSymbols());
		}

		if (WAIT_EXTRA_TIME_IN_PRESPIN)
		{
			yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_BEAT);
		}

		if (visibleSymbolClone != null)
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
			
		}

		clearOutHelperDataStructures();
		yield return null;
		symbolCamera.SetActive(false);

		bonusHits = 1;

		if(SpinPanel.instance != null && SpinPanel.instance.stopButton != null && numberOfFreespinsRemaining == 0)
		{
			//setting the "STOP" button as disabled except during autospins to mimic web.
			SpinPanel.instance.stopButton.isEnabled = false;
		}

		if (playDestroySymbolAudioOnPreSpin)		
		{
			Audio.play (Audio.soundMap(DESTROY_SYMBOLS_SOUND));
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
			if (symbol != null && symbol.animator != null)
			{
				Destroy(symbol.animator.gameObject);
			}
			yield return null;
		}
		else
		{
			if (revealPrefab != null)
			{
				GameObject reveal = CommonGameObject.instantiate(revealPrefab, symbol.animator.transform.position, Quaternion.identity) as GameObject;
				reveal.transform.localScale = revealPrefabScale;
				StartCoroutine(waitThenDestroy(reveal, TIME_TO_DESTROY_REVEAL));
				StartCoroutine(waitThenDestroy(symbol.animator.gameObject, TIME_TO_DESTROY_SYMBOL));
				yield return new TIWaitForSeconds(WINNING_SYMBOL_DESTROY_INTERVAL);
			}
		}
		//symbol = null;
	}

	/// replace normal rollup with this tumbling logic
	private IEnumerator tumbleAfterRollup(JSON[] tumbleOutcomeJson, long basePayout, long bonusPayout, RollupDelegate rollupDelegate, bool doBigWin, BigWinDelegate bigWinDelegate, long rollupStart, long rollupEnd)
	{
		//yield return new WaitForSeconds(.3f);
		long payout = 0;
		// First need to find out what the key name of the bonus_pool is.
		if (tumbleOutcomeJson.Length == 0)
		{
			//Debug.LogWarning("Done!");
			yield break;
		}
		else
		{
			bool shouldBigWin = ((rollupEnd-rollupStart) >= Glb.BIG_WIN_THRESHOLD * SpinPanel.instance.betAmount);
			float rollupTime =  BASE_ROLLUP_TIME;
			if (shouldBigWin)
			{
				rollupTime *= 2.0f;
			}
			bool firstGo = true; // is this the first time through the loop
			TICoroutine rollupRoutine = StartCoroutine(SlotUtils.rollup(rollupStart, rollupEnd, rollupDelegate, true, rollupTime));			
			currentlyTumbling = true;
			foreach(JSON tumbleOutcome in tumbleOutcomeJson)
			{
				currentTumbleOutcome = tumbleOutcome;
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

				// in base game the outcome is set before calling plopNewSymbols, to support optimized symbols we need freespins to behave the same way
				// the question is why is it different between freespins and basegame? Copy/Paste error?
				if (isGameUsingOptimizedFlattenedSymbols)   
				{
					SlotOutcome outcome = new SlotOutcome(tumbleOutcome);
					setOutcome(outcome);
				}

				yield return StartCoroutine(plopNewSymbols());

				if (!isGameUsingOptimizedFlattenedSymbols)
				{
					SlotOutcome outcome = new SlotOutcome(tumbleOutcome);
					setOutcome(outcome);
				}

				if (_outcomeDisplayController.rollupsRunning.Count > 0)
				{
					_outcomeDisplayController.rollupsRunning[_outcomeDisplayController.rollupsRunning.Count - 1] = false;	// Set it to false to flag it as done rolling up, but don't remove it until finalized.
				}

				yield return new TIWaitForSeconds(TIME_EXTRA_WAIT_ON_SPIN);
				yield return StartCoroutine(doReelsStopped());
				
				clearOutHelperDataStructures();
				// When we pause this coroutine after doReelsStopped we need to wait a frame here so it knows where to
				firstGo = false;
			}
			currentlyTumbling = false;


			if (doBigWin && bigWinDelegate != null)
			{
				// We need to handle the big win calls ourselves.
				bigWinDelegate(payout, false);
			}

			yield return new TIWaitForSeconds(TIME_TO_WAIT_AT_END);
			currentTumbleOutcome = null;
			// We're finally done with this whole outcome.
			yield return StartCoroutine(_outcomeDisplayController.finalizeRollup());
			
		}
	}

	// build list of winning symbols from scatter outcome
	protected virtual void findScatterWinningSymbols()
	{
		string[] scatterList = null;
		
		if (_outcome.hasSubOutcomes()) // check for suboutcomes instead of just base outcome because it might be the 'win' of getting 3 bonus symbols.
		{
			scatterList = _outcomeDisplayController.getScatterWinSymbols(_outcome.getSubOutcomesReadOnly()[0]);
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
						if (symbol != null && symbol.name == symbolName)
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
	
	// Loop through the cluster outcomes
	protected virtual void findClusterWinningSymbols()
	{
		Dictionary<SlotOutcome,ClusterOutcomeDisplayModule.Cluster> clusterWins = _outcomeDisplayController.getClusterDisplayDictionary(); //grab dictionary of clusters
		
		if (clusterWins != null) // if there are any cluster wins (which for FarmVille2, at least, we know there will be. But check just to be safe)
		{
			foreach (ClusterOutcomeDisplayModule.Cluster cluster in clusterWins.Values)
			{
				SlotReel[] reelArray = engine.getReelArray();
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
		foreach (ClusterOutcomeDisplayModule.Cluster cluster in symbolsToRemove.Keys)
		{
			Audio.play (Audio.soundMap(PAYLINE_FREESPIN_SOUND_MAP));
			List<KeyValuePair<int,int>> symbolList = symbolsToRemove[cluster];
			yield return StartCoroutine(showWinningCluster(symbolList, cluster));
			hasShownCluster = true;
		}
		
		
		if (!hasShownCluster)
		{
			// wait until payline cascade is done
			while(!isReadyForAnimations)
			{
				yield return null;
			}
			bool shouldSkip = false;
			for (int i = 0; i < _outcomeDisplayController.getNumLoopedOutcomes() && i < 2; i++)
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
			SlotSymbol symbol = reelArray[pair.Key].visibleSymbols[slotGameData.numVisibleSymbols-pair.Value-1];
			StartCoroutine(doWinMovementAndPaybox(symbol, pair, cluster, symbolNum, hasShownCluster));
			symbolNum++;
			hasShownCluster = true;
		}
		yield return new WaitForSeconds(getWaitTimePerCluster());
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

		if (shouldPlayBonusSymbolFanfareOnBringDown || shouldPlayBonusSymbolFanfareWithAntAnim)
		{
			bonusHits = TumbleSlotBaseGame.calculateNumberOfBonusHits(this, visibleSymbolClone, willBeRemoved);
		}

		SlotReel[] reelArray = engine.getReelArray();


		List<TICoroutine> coroutineList = new List<TICoroutine>();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			int startingSymbolMinimum = 0;
			for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
			{	
				if(willBeRemoved[reelIndex][symbolIndex])
				{
					SlotSymbol symbolToBringDown = null;
					for (int nextSymbolIndex = System.Math.Max(symbolIndex + 1, startingSymbolMinimum) ; nextSymbolIndex < visibleSymbols.Length; nextSymbolIndex++)
					{
						if(visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1] != null && visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1].animator != null)
						{
							
							symbolToBringDown = visibleSymbolClone[reelIndex][slotGameData.numVisibleSymbols-nextSymbolIndex-1];
							SymbolInfo symInfo = symbolToBringDown.info;

							if (symbolToBringDown.isBonusSymbol && shouldPlayBonusSymbolFanfareOnBringDown)
							{
								bonusHits = TumbleSlotBaseGame.playBonusSymbolFanfare(reelIndex, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
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

		// let a derived class handle the plopped symbols
		yield return StartCoroutine(onPloppingFinished(true));
	}

	protected virtual IEnumerator tumbleSymbolAt(int row, int column)
	{
		SlotReel[] reelArray = engine.getReelArray();

		float reelBackgroundVertAdjust = 1.0f;
		if (reelGameBackground != null)
		{
			reelBackgroundVertAdjust = reelGameBackground.getVerticalSpacingModifier();
		}

		SlotSymbol symbolToTumble = visibleSymbolClone[row][reelArray[row].visibleSymbols.Length - column - 1];
		symbolsBeingTumbled.Add(symbolToTumble);
		
		StartCoroutine(symbolToTumble.tumbleDown(column, TUMBLE_SYMBOL_SPEED, iTween.EaseType.easeOutBounce, -MAGIC_DISTANCE * reelBackgroundVertAdjust, onFinish: onSymbolTweenFinish));
		
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		if (GameState.game.keyName == "gen09" || GameState.game.keyName == "gen39")
		{
			coroutineList.Add(StartCoroutine(symbolToTumble.doTumbleSquashAndSquish()));
		}

		if (symbolToTumble.name == "TW")
		{
			coroutineList.Add(StartCoroutine(doSpecialTWAnims(symbolToTumble)));
		}

		if (symbolToTumble.isBonusSymbol && !shouldPlayBonusSymbolFanfareWithAntAnim)
		{
			bonusHits = TumbleSlotBaseGame.playBonusSymbolFanfare(row, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
		}

		yield return new TIWaitForSeconds(tumbleTime);

		Audio.play (Audio.soundMap(TUMBLE_SYMBOL_HIT_PREFIX + (row+1) + "_" + (column+1)));
		
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

	public void onSymbolTweenFinish(SlotSymbol symbol)
	{
		bool isAnimatingAnticipation = symbol.isBonusSymbol && (!shouldPlayBonusSymbolAnticipationOncePerSymbol || !bonusSymbolsWhichHaveAnimatedAnticipation.Contains(symbol));

		if (isAnimatingAnticipation)
		{
			if (shouldPlayBonusSymbolFanfareWithAntAnim)
			{
				bonusHits = TumbleSlotBaseGame.playBonusSymbolFanfare(symbol.reel.reelID - 1, bonusHits, BONUS_SYMBOL_FANFARE_DELAY, alwaysPlayBonusSymbolFanfare);
			}

			if (shouldPlayBonusSymbolAnticipationOncePerSymbol)
			{
				bonusSymbolsWhichHaveAnimatedAnticipation.Add(symbol);
			}

			TumbleSlotBaseGame.staticOnSymbolTweenFinish(symbol, wasAnticipationPlayedOnTumble: false, onAnimationDoneCallback: onAnticipationAnimationComplete);
		}
		else
		{
			// Remove the symbol from the list of the ones being tumbled right now because it isn't going to animate
			if (symbol != null && symbolsBeingTumbled.Contains(symbol))
			{
				symbolsBeingTumbled.Remove(symbol);
			}
		}
	}

	// Callback function for after a bonus symbol animates to ensure that it gets
	// removed from the list of tumbling symbols, so that the game will unlock
	// correctly
	private void onAnticipationAnimationComplete(SlotSymbol symbol)
	{
		if (symbol != null && symbolsBeingTumbled.Contains(symbol))
		{
			symbolsBeingTumbled.Remove(symbol);
		}
	}

	protected virtual IEnumerator doSpecialTWAnims(SlotSymbol symbol)
	{
		yield return null;
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{
		StartCoroutine(plopSymbols());
	}

	// Iterate through all symbols, setup our columns GameObject arrays
	protected virtual IEnumerator plopSymbols()
	{
		symbolCamera.SetActive(true);
		visibleSymbolClone = new List<List<SlotSymbol>>();
		SlotReel[] reelArray = engine.getReelArray();

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

		List<TICoroutine> coroutineList = new List<TICoroutine>();

		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			SlotSymbol[] visibleSymbols = reelArray[reelIndex].visibleSymbols;
			coroutineList.Add(StartCoroutine(tumbleColumn(visibleSymbols, reelIndex)));
			yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));

		// let a derived class handle the plopped symbols
		yield return StartCoroutine(onPloppingFinished());

		// let other stuff know that the reels are done
		StartCoroutine(doReelsStopped());
	}

	// handle plopping for a specific column
	protected virtual IEnumerator tumbleColumn(SlotSymbol[] visibleSymbols, int reelIndex)
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		
		for (int symbolIndex = 0; symbolIndex < visibleSymbols.Length; symbolIndex++)
		{
			if (originalPositions != null)
			{
				SlotSymbol symbol = visibleSymbols[visibleSymbols.Length-symbolIndex-1];
				if (symbol != null)
				{
					if (symbol.transform != null)
					{
						SymbolInfo symInfo = symbol.info; 
						if (symInfo != null)
						{
							originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), symbol.transform.localPosition - symInfo.positioning + new Vector3(0.0f, -MAGIC_DISTANCE/gameScaler.transform.localScale.y, 0.0f));
						}
						else
						{
							originalPositions.Add(new KeyValuePair<int, int>(reelIndex, symbolIndex), symbol.transform.localPosition + new Vector3(0.0f, -MAGIC_DISTANCE/gameScaler.transform.localScale.y, 0.0f));
						}
					}
					else
					{
						Debug.LogWarning("No transform for symbol " + symbol.name);
					}
				}
			}
			coroutineList.Add(StartCoroutine(tumbleSymbolAt(reelIndex, symbolIndex)));
			yield return new TIWaitForSeconds(TIME_BETWEEN_PLOPS);
		}
		
		// Make sure all symbols have fully tumbled in this column before saying the column is done
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	/**
	Handle anything you need to do post plopping in a derived class
	*/
	protected virtual IEnumerator onPloppingFinished(bool useTumbleOutcome = false)
	{
		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnPloppingFinished())
			{
				yield return StartCoroutine(module.executeOnPloppingFinished(currentTumbleOutcome, useTumbleOutcome));
			}
		}
		yield break;
	}

	/**
	Handle visual changes which might occur because a symbol is being removed, for instance if something is attached to the symbol
	*/
	protected virtual void onWinningSymbolRemoved(SlotSymbol symbolRemoved)
	{
		// Handle in derived class
	}

	/// SlotOutcome overload for setting outcome.
	/// Needs to be overridable on the off-chance that a game (for instance the legacy Zynga01) 
	/// doesn't want automatic handling of reevaluation spins
	public override void setOutcome(SlotOutcome outcome)
	{
		_outcome = outcome;
		_outcome.printOutcome();
		_outcome.processBonus();

		// removed the automatic reevaluation spin stuff since Zynga01 handles this stuff itself

		mutationManager.setMutationsFromOutcome(outcome.getJsonObject());
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
