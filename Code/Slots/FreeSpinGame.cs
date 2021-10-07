using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Unity.Attributes;

public class FreeSpinGame : ReelGame
{
	public GameObject freeSpinAnimation;
	
	[FoldoutHeaderGroup(FoldoutHeaderGroup.STANDARD_OPTIONS_GROUP)]
	public bool hideFreeSpinPannel = false;     // Hides the spin panel at the start of the game.
	
	public float reelDelay = -1;                // Setting this to 0 or higher will override the base game's reelDelay from the SlotGameData.	
	public bool shouldHideBonusGamePresenterWings = true;                   // Most of the time freespin games want to use their own reel wings. PickMajorGames need to hide show.
	public string spinsRemainingLabelLocalizationKey = "spins_left"; // localization key used for spin panel (overridden in prefab for games like elvira02)
	[SerializeField] private bool hasLingeringMutations = true;
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)] [Tooltip("Should the freespinintro sound be played?")]
	[SerializeField] private bool shouldPlayFreespinIntro = false;
	[FoldoutHeaderGroup(FoldoutHeaderGroup.AUDIO_OPTIONS_GROUP)] [Tooltip("If true, all built in music play calls will be skipped.  Useful if custom music handling is being handled somewhere else.")]
	[SerializeField] private bool isIgnoringBuiltInMusicPlayCalls = false;
	[Tooltip("Should the freespin music track be queued after freespinintro finishes? NOTE: FREESPIN_INTRO_DELAY will be ignored if this is true.")]
	[SerializeField] private bool shouldQueueFreespinAfterFreespinIntro = false;
	[SerializeField] private float FREESPIN_INTRO_DELAY;

	[SerializeField]  protected float reelSpinDelay = 0.0f;

	[Tooltip("Should defaultReelSet come from reevaluations array in outcome?")]
	[SerializeField] private bool setDefaultReelSetFromReevaluations = false;
	
	[Tooltip("If false, the game will not automatically go into Endless mode if 1 spin is detected for the freespin paytable. You will need to turn on Endless mode yourself from code if you need it.")]
	[SerializeField] private bool isAutoDetectingEndlessMode = true;
	
	protected long _lastRollupValue = 0;
	protected int _payTableFreeSpinCount = 0;
	public int payTableFreeSpinCount
	{
		get { return _payTableFreeSpinCount; }
	}

	protected BonusGamePresenter _bonusGamePresenter;

	public BonusGamePresenter bonusGamePresenter
	{
		get { return _bonusGamePresenter; }
	}

	protected long _lastOutcomePayout = 0;      // The full amount of the last payout from the outcome display controller.

	// Constants
	protected const string FREESPIN_INTRO = "freespinintro";
	private const string RETRIGGER_BANNER_SOUND = "retrigger_banner";

	public bool didInit
	{
		get { return _didInit; }
	}
	protected bool _didInit = false;

	protected bool _didStart = false;
	protected bool cameFromTransition = false;

	[HideInInspector] public bool currentlyTumbling = false;  // Needed for tumble outcomes so info isn't reset on every setOutcome

	public static FreeSpinGame instance = null;

	protected const string BASE_FREESPIN_REEL_SET_KEY = "free_spin_reel_set"; // Base reelset used by free spin games

	protected override void Awake()
	{
		base.Awake();
		instance = this;
		if (reelGameBackground != null && SpinPanel.instance != null)
		{
			SpinPanel.instance.freeSpinsBackgroundWingsWidth.gameObject.SetActive(!reelGameBackground.isUsingOrthoCameras && !isNewCentering);
		}	
		CommonGameObject.disableCameras(gameObject);
	}

	public override void initFreespins()
	{
		if (_didInit)
		{
			Debug.LogError("Trying to init freespins game twice!");
			return;
		}

		// Make sure the bonus game spin panel message is set when starting the game
		if (BonusSpinPanel.instance != null && BonusSpinPanel.instance.messageLabel != null)
		{
			BonusSpinPanel.instance.messageLabel.text = Localize.text("good_luck");
		}

		// Unmute the game if it was going to be muted.
		Audio.listenerVolume = Audio.maxGlobalVolume;
		setReelStopOrder();

		mutationManager = new MutationManager(hasLingeringMutations);

		_freeSpinsOutcomes = (FreeSpinsOutcome)BonusGameManager.instance.outcomes[BonusGameType.GIFTING];

		/// ===============================
		/// SetReels
		JSON paytable = _freeSpinsOutcomes.paytable;
		string payTableKey = paytable.getString("free_spin_pay_table", "");
		BonusGameManager.instance.currentBonusPaytable = PayTable.find(payTableKey);
		defaultReelSetName = paytable.getString(BASE_FREESPIN_REEL_SET_KEY, "");
		_payTableFreeSpinCount = paytable.getInt("free_spins", -1);

		//Some games use a non default reelsetname which comes down in the reevaluations. 
		if (setDefaultReelSetFromReevaluations)
		{
			JSON[] reevaluationsArray = _freeSpinsOutcomes.entries[0].getJsonObject().getJsonArray("reevaluations", true);
			if (reevaluationsArray != null && reevaluationsArray[0].hasKey("reel_set")) //If we have any reevaluations and it has a special reelset then we can set it here
			{
				defaultReelSetName = reevaluationsArray[0].getString("reel_set", "");
			}
		}

		numberOfFreespinsRemaining = _payTableFreeSpinCount;

		// Override Freespin count
		if (_freeSpinsOutcomes.numFreespinsOverride > 0)
		{
			numberOfFreespinsRemaining = _freeSpinsOutcomes.numFreespinsOverride;
		}

		if (isAutoDetectingEndlessMode && _payTableFreeSpinCount <= 1 && numberOfFreespinsRemaining <= 1)
		{
			endlessMode = true;
			BonusSpinPanel.instance.spinCountLabel.text = "-";
		}
		setEngine(payTableKey);
		engine.progressiveThreshold = progressiveThreshold;
		setReelSet(defaultReelSetName);

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
		_outcomeDisplayController.setPayoutRollupCallback(onPayoutRollup, onEndRollup);

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

		BonusGameManager.instance.bonusGameName = _freeSpinsOutcomes.bonusGameName;
		BonusGamePresenter.instance.bonusGameName = _freeSpinsOutcomes.bonusGameName;
		_bonusGamePresenter = BonusGamePresenter.instance;
		BonusGameManager.instance.paytableSetId = _freeSpinsOutcomes.getPaytableSetId();
		BonusGamePresenter.instance.paytableSetId = BonusGameManager.instance.paytableSetId;

		// If our free spin game has a multiplier decided from somewhere else, let's make sure we use that for our end game summary screen.
		if (BonusGamePresenter.carryoverMultiplier > 0)
		{
			BonusGamePresenter.instance.useMultiplier = true;
			BonusGameManager.instance.currentMultiplier = BonusGamePresenter.carryoverMultiplier + 1;
		}

		setSwipeableReels();

		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(0);

		if (hasMultipleLinkedReelSets)
		{
			foreach (SlotReel reel in engine.getAllSlotReels())
			{
				int rawReelId = reel.reelSyncedTo - 1;
				if (rawReelId >= 0 && !linkedReelStartingReelIndices.Contains(rawReelId))
				{
					linkedReelStartingReelIndices.Add(rawReelId);
				}
			}
		}
		CommonGameObject.enableCameras(gameObject);
		applyInitialReelStops();
		StartCoroutine(playGameStartModules());
	}

	protected virtual IEnumerator playGameStartModules()
	{
		// Wait and make sure that the ReelGameBackground has updated
		yield return StartCoroutine(waitForReelGameBackgroundScalingUpdate());

		yield return StartCoroutine(executeGameStartModules());
		_didInit = true;
	}

	// Sets the engine in the awake method, so that subclasses can use their own version of SlotEngine.
	protected virtual void setEngine(string payTableKey)
	{
		engine = new SlotEngine(this, payTableKey);
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

		yield return StartCoroutine(waitForModulesAfterPaylines(true));
	}

	protected virtual void startGame()
	{
		// Start the spins in Start instead of Awake so it doesn't start prematurely when first instantiating the object.
		hasFreespinGameStarted = true;

		if (shouldPlayFreespinIntro && !isIgnoringBuiltInMusicPlayCalls)
		{
			if (Audio.canSoundBeMapped(FREESPIN_INTRO))
			{
				if (shouldQueueFreespinAfterFreespinIntro)
				{
					// play this as a music track so we can queue the freespin track after it
					// ignoring FREESPIN_INTRO_DELAY because it doesn't make sense if we want this to be the starting music track
					Audio.switchMusicKeyImmediate(Audio.soundMap(FREESPIN_INTRO), 0.0f);
				}
				else
				{
					Audio.play(Audio.soundMap(FREESPIN_INTRO), 1.0f, 0.0f, FREESPIN_INTRO_DELAY);
				}
			}
			else
			{
				Debug.LogWarningFormat("The isFreespinIntro flag is true but the sound key {0} is not mapped!", FREESPIN_INTRO);
			}
		}

		_didStart = true;
		StartCoroutine(startAnimationAndSpin());
	}

	protected IEnumerator startAnimationAndSpin()
	{
		// Wait for the loading screen to disappear before starting spins.
		while (Loading.isLoading || Loading.showingCustomLoading)
		{
			yield return null;
		}

		//this is getting removed due to HIR-373, might come back later

		if (freeSpinAnimation != null)
		{
			StartCoroutine(showAdditionalInformation());
		}

		yield return new TIWaitForSeconds(reelSpinDelay);

		// Forces the overlay to update a little sooner than previously.
		//FreeSpinGame.instance.autoSpins = _payTableFreeSpinCount;
		//BonusSpinPanel.instance.messageLabel.text = Localize.text("good_luck");

		// play an intro animation if playIntroAnimation has been overrided by a derived class
		yield return StartCoroutine(playIntroAnimation());

		if (!isIgnoringBuiltInMusicPlayCalls)
		{
			beginFreeSpinMusic();
		}

		while (engine.effectInProgress)
		{
			yield return null;
		}
		
		startNextFreespin();
	}

	protected virtual void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			if (!Audio.isPlaying(Audio.soundMap("freespin_intro_vo")))
			{
				Audio.play(Audio.soundMap("freespin_intro_vo"));
			}

			if (shouldQueueFreespinAfterFreespinIntro)
			{
				// queue the freespin track so it plays after the intro music track
				Audio.switchMusicKey(Audio.soundMap("freespin"), 0.0f);
			}
			else
			{
				// switch to the freespin track right away
				Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
			}
		}
	}

	/// Allows the user to play an intro animation that will cause the reels to wait to spin until it is over, called from startAnimationAndSpin()
	protected virtual IEnumerator playIntroAnimation()
	{
		yield break;
	}

	public virtual IEnumerator showUpdatedSpins(int numberOfSpins)
	{
		Audio.play("cheer_a");
		Audio.play(Audio.soundMap("freespin_spins_added"));
		Audio.play(Audio.soundMap("freespin_spins_added_vo"));

		GrantFreespinsModule[] grantFreeSpinsModules = gameObject.GetComponents<GrantFreespinsModule>();
		if (grantFreeSpinsModules != null && grantFreeSpinsModules.Length > 0)
		{
			//do nothing. handled in GrantFreespinsModule but this prevents from throwing the error in the logs
		}
		else if (freeSpinAnimation != null)
		{
			GameObject gb = (GameObject)CommonGameObject.instantiate(freeSpinAnimation);
			gb.SetActive(true);
			BonusGameManager.instance.attachTextOverlay(gb);
			AnimatorFreespinEffect animatorFreespinEffect = gb.GetComponent<AnimatorFreespinEffect>();
			FreeSpinEffect fse = gb.GetComponentInChildren<FreeSpinEffect>();

			// play the retrigger sound if it is mapped
			Audio.tryToPlaySoundMap(RETRIGGER_BANNER_SOUND);

			if (animatorFreespinEffect != null)
			{
				engine.effectInProgress = true;
				animatorFreespinEffect.numberOfSpinsToAdd = numberOfSpins;
				yield return StartCoroutine(animatorFreespinEffect.doAdditionalEffects());
				Destroy(animatorFreespinEffect.gameObject);
				engine.effectInProgress = false;
				if (!Audio.isPlaying(Audio.soundMap("freespin")))
				{
					Audio.play(Audio.soundMap("freespin"));
				}
			}
			else if (fse != null)
			{
				FreeSpinGame.instance.numberOfFreespinsRemaining += numberOfSpins;
				yield return StartCoroutine(fse.startTextAnimation(FreeSpinEffect.AnimationType.SpinIncrease, numberOfSpins));
			}
		}
		else
		{
			Debug.LogError("Should be updating number of freespins, but unsure how.");
		}

		yield return null;
	}
	
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}

		if (!_didStart)
		{
			startGame();
		}

		if (engine != null)
		{
			engine.frameUpdate();
		}

		if (isPerformingSpin && isSpinComplete)
		{
			isPerformingSpin = false;
			isSpinComplete = false;
			StartCoroutine(waitAndStartNextFreespin());
		}
	}

	/// Use the numberOfFreespinsRemaining setter to keep the spin panel UI updated as it changes.
	public override int numberOfFreespinsRemaining
	{
		get { return _numberOfFreespinsRemaining; }

		set
		{
			_numberOfFreespinsRemaining = value;

			if (BonusSpinPanel.instance != null)
			{
				if (!endlessMode)
				{
					BonusSpinPanel.instance.spinCountLabel.text = _numberOfFreespinsRemaining.ToString();
				}
			}
		}
	}

	protected override void startNextFreespin()
	{
		lastPayoutRollupValue = 0;

		if (hasFreespinsSpinsRemaining || (endlessMode && numberOfFreespinsRemaining != 0))
		{
			isPerformingSpin = true;

			BonusSpinPanel.instance.slideOutPaylineMessageBox();

			if (BonusSpinPanel.instance.messageLabel != null)
			{
				if (numberOfFreespinsRemaining == 1 && !endlessMode)
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
			}

			playFreespinSpinSound();

			if (!endlessMode)
			{
				numberOfFreespinsRemaining--;
			}

			StartCoroutine(startNextFreespinCoroutine());

			StartCoroutine(showAdditionalInformation());
			isFirstSpin = false;
		}
		else if (hasFreespinGameStarted)
		{
			gameEnded();
		}
	}

	// coroutine for starting the next freespin that will wait until the animationCount is 0 and no effects are going before proceeding
	private IEnumerator waitAndStartNextFreespin()
	{
		// wait for stuff to end before going to the next spin
		while (engine.animationCount > 0 || engine.effectInProgress)
		{
			yield return null;
		}

		startNextFreespin();
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

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("X"))
		{
			testGUI = false;
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical();

		engine.drawReelWindow();

		logScroll = GUILayout.BeginScrollView(logScroll);

		string finalLogTest = logText + _outcomeDisplayController.getLogText();

		GUILayout.TextArea(finalLogTest, GUILayout.Width(1000), GUILayout.Height(2000));
		GUILayout.EndScrollView();

		GUILayout.EndVertical();
		GUI.DragWindow(dragRect);
	}



	/// The free spins game ended.
	protected override void gameEnded()
	{
		StartCoroutine(waitForModulesThenEndGame());
	}

	protected override IEnumerator waitForModulesThenEndGame()
	{
		// make sure that all rollups are complete, this is mainly for tumble games 
		// which do strange things with rollups and don't fully block on them
		// so we want to make sure everything is rolled up before continuing
		while (_outcomeDisplayController.rollupsRunning.Count > 0)
		{
			yield return null;
		}

		foreach (SlotModule module in cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnFreespinGameEnd())
			{
				yield return StartCoroutine(module.executeOnFreespinGameEnd());
			}
		}
		BonusGamePresenter.instance.gameEnded();
	}

	protected override void onOutcomeSpinBlockRelease()
	{
		if (hasReevaluationSpinsRemaining)
		{
			StartCoroutine(startNextReevaluationSpin());
		}
		else
		{
			isSpinComplete = true;
		}
	}

	/// Handle a reevluation spin whose data was sent down in the original spin outcome
	protected override IEnumerator startNextReevaluationSpin()
	{
		lastPayoutRollupValue = 0;
		yield return StartCoroutine(base.startNextReevaluationSpin());
	}
	
	// get the next outcome without changing the current outcome
	public override SlotOutcome peekNextOutcome()
	{
		return _freeSpinsOutcomes.lookAtNextEntry();
	}

	protected virtual void startSpinReels()
	{
		engine.spinReels();
	}

	/// reevaluationReelStoppedCallback - called when all reels stop, only on reevaluated spins
	protected override void reevaluationReelStoppedCallback()
	{
		StartCoroutine(doStickySymbolValidateAndStop());
	}

	/// Handle sticky symbols and validation after the sticky symbols are applied
	private IEnumerator doStickySymbolValidateAndStop()
	{
		if (currentReevaluationSpin != null)
		{
			yield return StartCoroutine(handleReevaluationStickySymbols(currentReevaluationSpin));
		}

		// Validate data now that the sticky symbols have been swapped over
		engine.validateVisibleSymbolsAgainstData(currentReevaluationSpin.getReevaluatedSymbolMatrix());

		StartCoroutine(handleReevaluationReelStop());
	}

}
