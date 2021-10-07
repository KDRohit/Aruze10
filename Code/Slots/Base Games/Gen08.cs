using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for Gen08 game Sugar Palace
*/
public class Gen08 : SlotBaseGame
{
	[SerializeField] private Animator[] m1LargeSymbols = null;					// large M1 side bar symbols for the M1 feautre
	[SerializeField] private Animator[] m2LargeSymbols = null;					// large side bar symbols for the M2 feature
	[SerializeField] private Animator[] m3LargeSymbols = null;					// large side bar symbols for the M3 feature
	[SerializeField] private GameObject m3FeatureAnticipation = null;			// played when the mega symbol is spinning during the m3 feature
	[SerializeField] private GameObject gameBackground = null;					// Background for the game, turned off when the transition is turned on
	[SerializeField] private GameObject normalFrame = null;						// Frame used when normal symbols are spinning
	[SerializeField] private GameObject cakeFrame = null;						// Frame used when the large cake is in the middle
	[SerializeField] private Animator freeSpinTransitionAnimator = null;		// Free spin background used for a transition
	[SerializeField] private Animator[] featureTexts = new Animator[3];			// The array of texts that show when a feature is triggered
	[SerializeField] private GameObject chocolateRainEffectsParent = null;		// The parent object that will hold all the chocolate rain effects
	[SerializeField] private Animator[] ambientRainAnimators = null;			// List of ambient rain animators
	[SerializeField] private GameObject chocolateWildRevealPrefab = null;		// Effect prefab for the chocolate splash that reveals a wild 
	[SerializeField] private Animator sugarFrostingRespinStartAnim = null;		// Starting animation for the M3 Suage Frosting Respin
	[SerializeField] private GameObject lollipopEffectPrefab = null;			// Prefab object to make lollipop effect clones from

	private static bool isRaining = false;														// Flag to tell if rain should still be happening
	private static List<GameObject> freeChocolateWildRevealEffects = new List<GameObject>();	// List of cached chocolate effects, should clean this up
	private static int numEffectsBeingWaitedOn = 0;												// Tracks how many effects are blocking continuing on
	private static List<GameObject> freeLollipopEffects = new List<GameObject>();				// Lollipop animations need their own effects
	private static List<GameObject> usedLollipopEffects = new List<GameObject>();				// Need this so we can hide all the lollipop effects once the reels fully stop

	public enum ReelFeatureEnum
	{
		RegularSpin = -1,
		LollipopWildRespinsM1 = 0,
		ChocolateRainWildsM2 = 1,
		SugarFrostingM3 = 2
	}

	public enum LargeSymbolLocEnum
	{
		Left = 0,
		Right = 1
	}

	public static readonly string[] REEL_FEATURE_SYMBOL_NAMES = { "M1", "M2", "M3" };
	public const string LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME = "anim";	// Name key for the symbol to do an animation
	public const string LARGE_SYMBOL_IDLE_ANIMATION_NAME = "idle";		// Name key for the symbol to do go to idle
	public const string M1_FEATURE_START_VO_SOUND_NAME = "HPWholeRainbowOfFlavors";
	public const string CHOCOLATE_RAIN_VO = "ChocolateWildVO";

	private const string LARGE_SYMBOL_REVEAL_ANIMATION_NAME = "reveal";	// Name key for the symbol to play a reveal
	private const string BACKGROUND_MUSIC_SOUND_KEY = "reelspin_base";						// Basic background music, used to restore the background music
	private const string STACKED_REEL_1_EXPAND_SOUND_KEY = "stacked_reel_expand_1"; 		// Sound for the 1st reel expanding
	private const string STACKED_REEL_5_EXPAND_SOUND_KEY = "stacked_reel_expand_5";			// Sound for the 5th reel expanding
	private const string FS_STACKED_REEL_1_EXPAND_SOUND_KEY = "stacked_reel_freespin_expand_1";	// Sound for the free spin 1st reel expanding
	private const string FS_STACKED_REEL_5_EXPAND_SOUND_KEY = "stacked_reel_freespin_expand_5";	// Sound for the free spin 5th reel expanding
	private const string M1_FEATURE_REEL_5_TRIGGER_SOUND_NAME = "HardCandyWildBG";
	private const string M2_FEATURE_REEL_5_TRIGGER_SOUND_NAME = "ChocolateWildBG";
	private const string M3_FEATURE_REEL_5_TRIGGER_SOUND_NAME = "CupcakeWildBg";
	private const string SPARKLY_WAND_WAVE_SOUND_NAME = "SparklyWandWave";
	private const string M1_WILD_STICK_SOUND = "HardCandyTransformWild";
	private const string FROSTING_RESPIN_MUSIC = "CupcakeRespin";
	private const string UMBRELLA_OPEN_SOUND = "UmbrellaOpen";
	private const string CHOCOLATE_RAIN_PLOP_SOUND = "ChocolatePlop";
	private const string CHOCOLATE_WILD_TRANSFORM_SOUND = "ChocolateTransformWild";
	private const string CUPCAKE_MEGA_VO = "CupcakeWildVO";
	private const string SPARKLY_FALLS_CANDY_SOUND = "SparklyFallsCandy";
	private const string CUPCAKE_WILD_TRANSFORM_SOUND = "CupcakeTransformWild";
	private const string IDLE_BACKGROUND_TRANSITION_ANIM_NAME = "gen08_backgroundTransition_base";
	private const string BACKGROUND_TRANSITION_ANIM_NAME = "transition";
	private const string CHOCOLATE_WILD_REVEAL_ANIM_NAME = "reveal";
	private const string CHOCOLATE_RAIN_DRIP_ANIM_NAME = "drip";		// Animation name for the dripping chocolate rain
	private const string SUGAR_FROSTING_START_ANIM_NAME = "anim";		// name of the sugar frosting start anim
	private const string FEATURE_TEXT_ANIM_NAME = "anim";
	private const string IDLE_ANIM_NAME = "idle";

	public const float TIME_BETWEEN_STICKY_M1_WILDS = 0.35f;				// Introduce a small delay as each wild appears and animates

	private const float WAIT_AND_SHOW_MEGA_SYMBOL = 1.5f;					// Small wait so the user can see the large 3x3 mega symbol that is now on the reels
	private const float FEATURE_SOUND_INTRO_DELAY_TIME = 1.2f;				// Delay before the feature sound starts playing which goes after STACKED_REEL_5_EXPAND_SOUND_KEY
	private const float BACKGROUND_TRANSITION_LENGTH = 4.0f;				// Time it takes for the transition animation to complete
	private const float LOLLIPOP_SYMBOL_ANIM_LENGTH = 1.5f;
	private const float CHOCOLATE_WILD_REVEAL_ANIM_LENGTH = 0.767f;
	private const float TIME_BETWEEN_CHOCOLATE_REVEALS = 0.35f;
	private const float CHOCOLATE_RAIN_DRIP_ANIM_LENGTH = 0.8f;			// Animation length for the dripping anim
	private const float MIN_DELAY_BETWEEN_RAIN_WAVES = 0.2f;			// Min time before a new wave of rain starts 
	private const float MAX_DELAY_BETWEEN_RAIN_WAVES = 0.5f;			// Max time before a new wave of rain starts
	private const float SUGAR_FROSTING_START_ANIM_LENGTH = 0.6f;		// Length of the sugar frosting start anim
	private const float UMBRELLA_OEPN_SOUND_DEALY = 0.6f;				// Delay for the sound of the umbrella opening
	private const float CHOCOLATE_RAIN_STARTUP_TIME = 1.0f;				// Time to delay while rain starts and VO plays
	private const float SHOW_CAKE_SYMBOL_TIME = 1.2f;					// Time to show the large cake symbol before the cake respin starts
	private const float CAKE_MUSIC_SYNC_DELAY = 0.3f;					// Music doesn't sync perfectly with the actual spin, so introduce a slight delay once the music starts
	private const float TRANSITION_WAIT_AFTER_SYMBOL_FADE = 1.0f;		// Wait time after the symbols fade out
	private const float WAIT_BETWEEN_LOLLIPOP_RESPIN = 0.5f;			// wait a small amount of time before starting the next spin so the sounds don't all overlap

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// make sure we cleanup the static effects shared 
		// by the base and freespins when the base game is exited
		Gen08.freeChocolateWildRevealEffects.Clear();
		Gen08.freeLollipopEffects.Clear();
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// put the cake frame away if we were using it
		setCakeFrameActive(false);

		// make sure that the WD1 symbols aren't on the overlay layer
		Gen08.resetWD1SymbolsLayer(this);

		// hide large symbols that might be left over from a feature occuring on the previous spin
		Gen08.turnOffLargeSymbols(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols);
		Gen08.splitAnyLargeSideSymbols(this);
	}

	/// Reset WD1 symbols back to default layer so they are under the frame before spinning
	public static void resetWD1SymbolsLayer(ReelGame reelGame)
	{
		foreach (SlotReel reel in reelGame.engine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (symbol.name == "WD1")
				{
					CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_REELS);
				}
			}
		}
	}

	/// Place the WD1 symbols onto overlay so the animate over the frame
	public static void placeWD1SymbolsOnOverlayLayer(ReelGame reelGame)
	{
		foreach (SlotReel reel in reelGame.engine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (symbol.name == "WD1")
				{
					CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_SLOT_OVERLAY);
				}
			}
		}
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// want to ignore and not re-play the effects on reevaluation spins
		if (currentReevaluationSpin == null)
		{
			yield return StartCoroutine(Gen08.checkAndPlayReelFeature(this, stoppedReel, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, false));
		}
	}

	/// Overriding to handle what to do before the next reevaluation spin starts
	protected override IEnumerator startNextReevaluationSpin()
  	{
  		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);

		if (triggeredFeature == ReelFeatureEnum.SugarFrostingM3)
		{
  			// clear pay boxes
			clearOutcomeDisplay();

			// swap the frame to the cake frame
			setCakeFrameActive(true);

			// do the frosting spin
			yield return StartCoroutine(Gen08.doFrostingLinkedReels(this, base.startNextReevaluationSpin, m3FeatureAnticipation, sugarFrostingRespinStartAnim, m3LargeSymbols, featureTexts, false));
		}
		else if (triggeredFeature == ReelFeatureEnum.LollipopWildRespinsM1)
		{
			yield return StartCoroutine(Gen08.startLollipopRespin(m1LargeSymbols, base.startNextReevaluationSpin));
		}
		else
		{
			yield return StartCoroutine(base.startNextReevaluationSpin());
		}
  	}

	/// overridable function for handling a symbol becoming stuck on the reels, may become stuck as different symbol, passed in by stuckSymbolName
	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickSymbolName, int row)
	{
		Gen08.changeSymbolToLollipop(this, symbol, stickSymbolName, row, lollipopEffectPrefab);
		yield return new TIWaitForSeconds(Gen08.TIME_BETWEEN_STICKY_M1_WILDS);	
	}

	/// Allows any sort of cleanup that may need to happen on the symbol animator
    protected override void preReleaseStickySymbolAnimator(SymbolAnimator animator)
    {
    	CommonGameObject.setLayerRecursively(animator.gameObject, Layers.ID_SLOT_REELS);
    }

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// Control which frame is used, cake one only used during the m3 mega cake spin
	private void setCakeFrameActive(bool isActive)
	{
		cakeFrame.SetActive(isActive);
		normalFrame.SetActive(!isActive);
	}

	/// handle stuff a derived class needs to do after a reevaluation spin, this occurs AFTER sticky symbols are handled and data is validated
	protected override IEnumerator handleReevaluationReelStop()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain
		bool isFinalReevaluationSpin = false;

		if (!hasReevaluationSpinsRemaining)
		{
			isFinalReevaluationSpin = true;

			Gen08.turnOffLargeSymbols(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols);

			// make sure the mega symbol anticipation is hidden
			if (m3FeatureAnticipation.activeSelf)
			{
				m3FeatureAnticipation.SetActive(false);
			}
		}

		ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);
		if (isFinalReevaluationSpin && triggeredFeature == ReelFeatureEnum.LollipopWildRespinsM1)
		{
			hideAllLollipopOverlays();
			Gen08.placeWD1SymbolsOnOverlayLayer(this);
		}

		yield return StartCoroutine(base.handleReevaluationReelStop());

		// turn off the music from the M1 or M3 feature and go back to the regular music
		if (isFinalReevaluationSpin && (triggeredFeature == ReelFeatureEnum.SugarFrostingM3 || triggeredFeature == ReelFeatureEnum.LollipopWildRespinsM1))
		{
			switchBackToNormalBkgMusic();
		}
	}

	/// Hide all the lollipop overlays that appear during the M1 feature
	public static void hideAllLollipopOverlays()
	{
		foreach (GameObject lollipop in usedLollipopEffects)
		{
			lollipop.SetActive(false);
			freeLollipopEffects.Add(lollipop);
		}

		usedLollipopEffects.Clear();
	}

	/// Transition the game music back to the standard background
	private void switchBackToNormalBkgMusic()
	{
		Gen08.playLoopedMusic(Audio.soundMap(BACKGROUND_MUSIC_SOUND_KEY));
	}

	/// Play looped music
	private static void playLoopedMusic(string musicKey)
	{
		Audio.switchMusicKeyImmediate(musicKey, 0.0f);
	}

	/// Handles custom transition stuff for this game as well as standard
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		if (_outcome.isBonus)
		{
			// handle playing this early, so that it happens before the transition starts
			yield return StartCoroutine(doPlayBonusAcquiredEffects());

			// Do the transition before going to the free spins game.
			yield return StartCoroutine(doFreeSpinsTransition());
		}

		// need to wait for the reveal animations to finish before moving on
		while (Gen08.areLargeSymbolOverlaysAnimating(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols))
		{
			yield return null;
		}

		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Gen08.getTriggeredFeature(this);

		switch (triggeredFeature)
		{
			case ReelFeatureEnum.RegularSpin:
				// just a normal spin
				Gen08.swapOverlaysForSymbolInstance(this, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, false);
				base.reelsStoppedCallback();
				break;
			case ReelFeatureEnum.LollipopWildRespinsM1:
				Audio.play(Gen08.M1_FEATURE_START_VO_SOUND_NAME);
				yield return StartCoroutine(Gen08.playFeatureTextAnimation(featureTexts[(int)ReelFeatureEnum.LollipopWildRespinsM1]));
				StartCoroutine(Gen08.doLollipopRespins(this, base.reelsStoppedCallback));
				break;
			case ReelFeatureEnum.ChocolateRainWildsM2:
				Audio.play(Gen08.CHOCOLATE_RAIN_VO);
				yield return StartCoroutine(Gen08.playFeatureTextAnimation(featureTexts[(int)ReelFeatureEnum.ChocolateRainWildsM2]));
				StartCoroutine(Gen08.doChocolateRainWilds(this, base.reelsStoppedCallback, chocolateRainEffectsParent, chocolateWildRevealPrefab, ambientRainAnimators, m2LargeSymbols, false));
				break;
			case ReelFeatureEnum.SugarFrostingM3:
				// handled in startNextReevaluationSpin() override
				Gen08.swapOverlaysForSymbolInstance(this, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, false);
				base.reelsStoppedCallback();
				break;
		}
	}

	protected override IEnumerator onBonusGameEndedCorroutine()
	{
		yield return StartCoroutine(base.onBonusGameEndedCorroutine());

		// Now put the base game back in a good state to return to
		
		// turn the symbols back on
		SlotReel[] reelArray = engine.getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			foreach (SlotSymbol slotSymbol in reelArray[i].symbolList)
			{
				if (slotSymbol.animator != null)
				{
					RoutineRunner.instance.StartCoroutine(slotSymbol.animator.fadeSymbolInOverTime(0.0f));
				}
			}
		}
		
		gameBackground.SetActive(true);
		freeSpinTransitionAnimator.gameObject.SetActive(false);
		
		// put the top bar back where it came from and turn the UIAnchors back on
		Overlay.instance.top.restorePosition();
		
		// put the extra wings we were using to hide the part behind the nav bar away
		BonusGameManager.instance.wings.hide();
	}

	/// Do the transition of the base game into the free spin game, in this case this involves a background slide
	private IEnumerator doFreeSpinsTransition()
	{
		// fade the symbols since they will cause issues when the background moves
		SlotReel[] reelArray = engine.getReelArray();
	
		for (int i = 0; i < reelArray.Length; i++)
		{
			foreach (SlotSymbol slotSymbol in reelArray[i].symbolList)
			{
				if (slotSymbol.animator != null)
				{
					StartCoroutine(slotSymbol.animator.fadeSymbolOutOverTime(1.0f));
				}
			}
		}

		yield return new TIWaitForSeconds(TRANSITION_WAIT_AFTER_SYMBOL_FADE);

		gameBackground.SetActive(false);

		freeSpinTransitionAnimator.gameObject.SetActive(true);
		freeSpinTransitionAnimator.Play(BACKGROUND_TRANSITION_ANIM_NAME);
		
		// show larger wings to hide the spots behind the top bar
		BonusGameManager.instance.wings.forceShowNormalWings(true);

		// move the top bar over and off the screen
		yield return StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Right, BACKGROUND_TRANSITION_LENGTH, false));
	}

	/// Change a symbol into a lollipop
	public static void changeSymbolToLollipop(ReelGame reelGame, SlotSymbol symbol, string stickSymbolName, int row, GameObject lollipopEffectPrefab)
	{
		symbol.mutateTo(stickSymbolName);

		GameObject lollipopInstance = null;

		if (freeLollipopEffects.Count > 0)
		{
			// have a stored wild reveal, so use that one
			lollipopInstance = freeLollipopEffects[freeLollipopEffects.Count - 1];
			freeLollipopEffects.RemoveAt(freeLollipopEffects.Count - 1);
		}
		else
		{
			// need to create a new reveal effect
			lollipopInstance = CommonGameObject.instantiate(lollipopEffectPrefab) as GameObject;
		}

		// place the lollipop in the correct place
		lollipopInstance.transform.parent = symbol.reel.getReelGameObject().transform;
		lollipopInstance.transform.localPosition = new Vector3(0, (row) * reelGame.getSymbolVerticalSpacingAt(symbol.reel.reelID - 1), -40 + (row * 3));

		usedLollipopEffects.Add(lollipopInstance);

		lollipopInstance.SetActive(true);

		Animator lollipopAnimator = lollipopInstance.GetComponent<Animator>();

		lollipopAnimator.Play("populate");

		Audio.play(Gen08.M1_WILD_STICK_SOUND);
	}

	/// Coroutine to handle what happens when the LollipopWildRespinsM1 feature is triggered
	public static IEnumerator doLollipopRespins(ReelGame reelGame, GenericDelegate gameReelStoppedCallback)
	{
		gameReelStoppedCallback();

		yield break;
	}

	/// Coroutine to handle what happens when the Chocolate Rain Wilds feature is triggered
	public static IEnumerator doChocolateRainWilds(ReelGame reelGame, GenericDelegate gameReelStoppedCallback, GameObject chocolateRainEffectsParent, GameObject chocolateWildRevealPrefab, Animator[] ambientRainAnimators, Animator[] m2LargeSymbols, bool isFreeSpins)
	{
		Audio.play(UMBRELLA_OPEN_SOUND);
		yield return new TIWaitForSeconds(UMBRELLA_OEPN_SOUND_DEALY);

		isRaining = true;

		RoutineRunner.instance.StartCoroutine(doAmbientChocolateRain(ambientRainAnimators));

		// wait a little to show the rain and get through the VO
		yield return new TIWaitForSeconds(CHOCOLATE_RAIN_STARTUP_TIME);

		SlotReel[] reelArray = reelGame.engine.getReelArray();

		// make sure actual mutations are going to occur due to the witch fireballs just in case
		if (reelGame.mutationManager.mutations.Count != 0)
		{
			StandardMutation currentMutation = reelGame.mutationManager.mutations[0] as StandardMutation;

			for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
			{
				for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
				{
					if (currentMutation.triggerSymbolNames[i,j] != null && currentMutation.triggerSymbolNames[i,j] != "")
					{
						numEffectsBeingWaitedOn++;
						RoutineRunner.instance.StartCoroutine(doChocolateWildReplaceSymbol(chocolateRainEffectsParent, chocolateWildRevealPrefab, reelArray[i].visibleSymbolsBottomUp[j]));
						yield return new TIWaitForSeconds(TIME_BETWEEN_CHOCOLATE_REVEALS);
					}
				}
			}
		}

		// Wait for all the effects to finish
		while (numEffectsBeingWaitedOn != 0)
		{
			yield return null;
		}

		// turn the rain off
		isRaining = false;

		if (isFreeSpins)
		{
			reelArray[0].visibleSymbols[0].mutateTo("M2_Left-3A");
			reelArray[4].visibleSymbols[0].mutateTo("M2_Right-3A");
		}
		else
		{
			reelArray[0].visibleSymbols[0].mutateTo("M2_Left-4A");
			reelArray[4].visibleSymbols[0].mutateTo("M2_Right-4A");
		}

		foreach (Animator symbol in m2LargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		gameReelStoppedCallback();
		yield break;
	}

	/// Function to play the ambient chocolate rain effects
	public static IEnumerator doAmbientChocolateRain(Animator[] ambientRainAnimators)
	{
		Audio.play("ChocolateRainfall");

		int maxNumDropsToPlayAtOnce = ambientRainAnimators.Length / 2;

		List<Animator> freeAmbientRainAnimators = new List<Animator>();
		foreach (Animator animator in ambientRainAnimators)
		{
			freeAmbientRainAnimators.Add(animator);
		}

		while (isRaining)
		{
			if (freeAmbientRainAnimators.Count > 0)
			{
				// have free rain animators, so determine a random number of them to play
				int randomNumDrops = Random.Range(1, Mathf.Min(freeAmbientRainAnimators.Count - 1, maxNumDropsToPlayAtOnce));

				for (int i = 0; i < randomNumDrops; i++)
				{
					int randomDropIndex = Random.Range(0, freeAmbientRainAnimators.Count - 1);
					Animator dropAnimator = freeAmbientRainAnimators[randomDropIndex];
					freeAmbientRainAnimators.RemoveAt(randomDropIndex);

					RoutineRunner.instance.StartCoroutine(playAmbientRainDropAnim(dropAnimator, freeAmbientRainAnimators));
				}

				// Wait a random amount of time before starting another rain wave
				float randomTimeTillNextRainWave = Random.Range(MIN_DELAY_BETWEEN_RAIN_WAVES, MAX_DELAY_BETWEEN_RAIN_WAVES);
				yield return new TIWaitForSeconds(randomTimeTillNextRainWave);
			}
		}
	}

	/// Function that plays a rain drop drip anim, waits for it to finish, then puts it back in a free list of drops)
	public static IEnumerator playAmbientRainDropAnim(Animator rainDropAnimator, List<Animator> freeAmbientRainAnimators)
	{
		rainDropAnimator.Play(CHOCOLATE_RAIN_DRIP_ANIM_NAME);
		Audio.play(CHOCOLATE_RAIN_PLOP_SOUND);
		yield return new TIWaitForSeconds(CHOCOLATE_RAIN_DRIP_ANIM_LENGTH);
		freeAmbientRainAnimators.Add(rainDropAnimator);
	}

	/// Function to handle the chocolate falling in and replacing symbols with wilds
	public static IEnumerator doChocolateWildReplaceSymbol(GameObject chocolateRainEffectsParent, GameObject chocolateWildRevealPrefab, SlotSymbol symbol)
	{
		GameObject chocolateWildReveal = null;

		if (freeChocolateWildRevealEffects.Count > 0)
		{
			// have a stored wild reveal, so use that one
			chocolateWildReveal = freeChocolateWildRevealEffects[freeChocolateWildRevealEffects.Count - 1];
			freeChocolateWildRevealEffects.RemoveAt(freeChocolateWildRevealEffects.Count - 1);
		}
		else
		{
			// need to create a new reveal effect
			chocolateWildReveal = CommonGameObject.instantiate(chocolateWildRevealPrefab) as GameObject;
		}

		// place the chocolate reveal in the correct place
		chocolateWildReveal.transform.parent = chocolateRainEffectsParent.transform;
		chocolateWildReveal.transform.position = symbol.animator.gameObject.transform.position;

		// play the animation
		chocolateWildReveal.GetComponent<Animator>().Play(CHOCOLATE_WILD_REVEAL_ANIM_NAME);
		Audio.play(CHOCOLATE_RAIN_PLOP_SOUND);
		yield return new TIWaitForSeconds(CHOCOLATE_WILD_REVEAL_ANIM_LENGTH / 2.0f);
		// replace the symbol
		Audio.play(CHOCOLATE_WILD_TRANSFORM_SOUND);
		symbol.mutateTo("WD2");
		yield return new TIWaitForSeconds(CHOCOLATE_WILD_REVEAL_ANIM_LENGTH / 2.0f);

		freeChocolateWildRevealEffects.Add(chocolateWildReveal);

		numEffectsBeingWaitedOn--;
	}

	/// Coroutine to handle what happens when the SugarFrostingM3 feature is triggered
	public static IEnumerator doFrostingLinkedReels(ReelGame reelGame, GenericIEnumeratorDelegate startNextReevaluationSpin, GameObject m3FeatureAnticipation, Animator sugarFrostingRespinStartAnim, Animator[] m3LargeSymbols, Animator[] featureTexts, bool isFreeSpins)
	{
		Audio.play(CUPCAKE_MEGA_VO);
		yield return RoutineRunner.instance.StartCoroutine(Gen08.playFeatureTextAnimation(featureTexts[(int)ReelFeatureEnum.SugarFrostingM3]));

		Audio.play(SPARKLY_FALLS_CANDY_SOUND);
		m3LargeSymbols[(int)LargeSymbolLocEnum.Left].gameObject.SetActive(true);
		m3LargeSymbols[(int)LargeSymbolLocEnum.Left].Play("m3_" + LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME);
		m3LargeSymbols[(int)LargeSymbolLocEnum.Right].gameObject.SetActive(true);
  		m3LargeSymbols[(int)LargeSymbolLocEnum.Right].Play("m3_" + LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME);

		sugarFrostingRespinStartAnim.Play(SUGAR_FROSTING_START_ANIM_NAME);

		yield return new TIWaitForSeconds(SUGAR_FROSTING_START_ANIM_LENGTH);

		// Change the middle reels into the Wild slipper
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		if (isFreeSpins)
		{
			reelGame.engine.getReelArray()[1].visibleSymbols[0].mutateTo("WD-4A-3A", null, true, false, true);
		}
		else
		{
			reelGame.engine.getReelArray()[1].visibleSymbols[0].mutateTo("WD-3A-3A", null, true, false, true);
		}

		// wait a frame so the symbol animation system doesn't complain
		yield return null;
		
		Audio.play(CUPCAKE_WILD_TRANSFORM_SOUND);
		reelGame.engine.getReelArray()[1].visibleSymbols[0].animateOutcome();
		yield return new TIWaitForSeconds(SHOW_CAKE_SYMBOL_TIME);
		Audio.play(FROSTING_RESPIN_MUSIC);
		yield return new TIWaitForSeconds(CAKE_MUSIC_SYNC_DELAY);

		yield return new TIWaitForSeconds(WAIT_AND_SHOW_MEGA_SYMBOL);

		m3FeatureAnticipation.SetActive(true);

		// need to explicitly call this since we showed the outcomes but told the ReelGame we didn't want it to immediatly start the next reeval spin
		yield return RoutineRunner.instance.StartCoroutine(startNextReevaluationSpin());
	}

	/// Checks if a feature is being triggered, or if this is just a basic spin
	public static ReelFeatureEnum getTriggeredFeature(ReelGame reelGame)
	{
		if (Gen08.isTriggeringRealFeature(reelGame, ReelFeatureEnum.LollipopWildRespinsM1))
		{
			return ReelFeatureEnum.LollipopWildRespinsM1;
		}
		else if (Gen08.isTriggeringRealFeature(reelGame, ReelFeatureEnum.ChocolateRainWildsM2))
		{
			return ReelFeatureEnum.ChocolateRainWildsM2;
		}
		else if (Gen08.isTriggeringRealFeature(reelGame, ReelFeatureEnum.SugarFrostingM3))
		{
			return ReelFeatureEnum.SugarFrostingM3;
		}
		else
		{
			return ReelFeatureEnum.RegularSpin;
		}
	}

	/// Used to determine if a specific reel should be triggering a feature
	public static ReelFeatureEnum getReelStopFeature(ReelGame reelGame, int reelNum)
	{
		if (Gen08.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.LollipopWildRespinsM1))
		{
			return ReelFeatureEnum.LollipopWildRespinsM1;
		}
		else if (Gen08.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.ChocolateRainWildsM2))
		{
			return ReelFeatureEnum.ChocolateRainWildsM2;
		}
		else if (Gen08.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.SugarFrostingM3))
		{
			return ReelFeatureEnum.SugarFrostingM3;
		}
		else
		{
			return ReelFeatureEnum.RegularSpin;
		}
	}

	/// Check the symbols on reels 1 and 5 to see if they are all the same major and match the feature passed in
	public static bool isTriggeringRealFeature(ReelGame reelGame, ReelFeatureEnum feature)
	{
		bool isFeatureTriggered = true;

		isFeatureTriggered &= Gen08.doesReelContainAllFeatureSymbol(reelGame, 0, feature);
		isFeatureTriggered &= Gen08.doesReelContainAllFeatureSymbol(reelGame, 4, feature);

		return isFeatureTriggered;
	}

	/// Checks if a feature should be anticipated
	public static ReelFeatureEnum getAnticipatedFeature(ReelGame reelGame)
	{
		if (Gen08.isAnticipatingFeature(reelGame, ReelFeatureEnum.LollipopWildRespinsM1))
		{
			return ReelFeatureEnum.LollipopWildRespinsM1;
		}
		else if (Gen08.isAnticipatingFeature(reelGame, ReelFeatureEnum.ChocolateRainWildsM2))
		{
			return ReelFeatureEnum.ChocolateRainWildsM2;
		}
		else if (Gen08.isAnticipatingFeature(reelGame, ReelFeatureEnum.SugarFrostingM3))
		{
			return ReelFeatureEnum.SugarFrostingM3;
		}
		else
		{
			return ReelFeatureEnum.RegularSpin;
		}
	}

	/// Check the symbols on reels 1 see if they are all the same major and match the feature passed in
	/// We are assuming that reel 1 will stop before the anticipation needs to be triggered on a later reel
	public static bool isAnticipatingFeature(ReelGame reelGame, ReelFeatureEnum feature)
	{
		return Gen08.doesReelContainAllFeatureSymbol(reelGame, 0, feature);
	}

	/// Check if all the symbols on the passed in reel number match the feature we are checking for
	public static bool doesReelContainAllFeatureSymbol(ReelGame reelGame, int reelNum, ReelFeatureEnum feature)
	{
		foreach (SlotSymbol slotSymbol in reelGame.engine.getReelArray()[reelNum].visibleSymbols)
		{
			if (!slotSymbol.name.Contains(REEL_FEATURE_SYMBOL_NAMES[(int)feature]))
			{
				// a symbol in this reel doesn't match the feature trigger, so NOT triggered
				return false;
			}
		}

		return true;
	}

	/// Allows derived classes to define when to use a feature specific feature anticipation
	public override string getFeatureAnticipationName()
	{
		return Gen08.getFeatureAnticipationName(this);
	}

	/// Generic function to be shared with free spins to determine the anticipation to use for a feature
	public static string getFeatureAnticipationName(ReelGame reelGame)
	{
		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Gen08.getAnticipatedFeature(reelGame);

		switch (triggeredFeature)
		{
			case ReelFeatureEnum.RegularSpin:
				// must be anticipating the bonus
				return "BN";
			case ReelFeatureEnum.LollipopWildRespinsM1:
				return "M1";
			case ReelFeatureEnum.ChocolateRainWildsM2:
				return "M2";
			case ReelFeatureEnum.SugarFrostingM3:
				return "M3";
		}

		return "BN";
	}

	/// Try to split the side symbols
	public static void splitAnyLargeSideSymbols(ReelGame reelGame)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();
		SlotSymbol topLeftSymbol = reelArray[0].visibleSymbols[0];
		if (topLeftSymbol.canBeSplit())
		{
			string splitSymbolName = "";
			if (topLeftSymbol.name.Contains("M1"))
			{
				splitSymbolName = "M1";
			}
			else if (topLeftSymbol.name.Contains("M2"))
			{
				splitSymbolName = "M2";
			}
			else if (topLeftSymbol.name.Contains("M3"))
			{
				splitSymbolName = "M3";
			}

			// can't use splitSymbol due to the right and left names being applied so just mutate the entire row based off what major symbol it is
			if (topLeftSymbol.name != splitSymbolName)
			{
				foreach (SlotSymbol symbol in reelArray[0].visibleSymbols)
				{
					symbol.mutateTo(splitSymbolName);
				}
			}
		}

		SlotSymbol topRightSymbol = reelArray[4].visibleSymbols[0];
		if (topRightSymbol.canBeSplit())
		{
			string splitSymbolName = "";
			if (topRightSymbol.name.Contains("M1"))
			{
				splitSymbolName = "M1";
			}
			else if (topRightSymbol.name.Contains("M2"))
			{
				splitSymbolName = "M2";
			}
			else if (topRightSymbol.name.Contains("M3"))
			{
				splitSymbolName = "M3";
			}

			// can't use splitSymbol due to the right and left names being applied so just mutate the entire row based off what major symbol it is
			if (topRightSymbol.name != splitSymbolName)
			{
				foreach (SlotSymbol symbol in reelArray[4].visibleSymbols)
				{
					symbol.mutateTo(splitSymbolName);
				}
			}
		}
	}

	/// Turn off the large symbols
	public static void turnOffLargeSymbols(Animator[] m1LargeSymbols, Animator[] m2LargeSymbols, Animator[] m3LargeSymbols)
	{
		foreach (Animator symbol in m1LargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		foreach (Animator symbol in m2LargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		foreach (Animator symbol in m3LargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}
	}

	/// Double check if any animators are still going, need to check this so we make sure they are done before processing the end of a spin
	public static bool areLargeSymbolOverlaysAnimating(Animator[] m1LargeSymbols, Animator[] m2LargeSymbols, Animator[] m3LargeSymbols)
	{
		foreach (Animator animator in m1LargeSymbols)
		{
			if (animator.gameObject.activeSelf && animator.GetCurrentAnimatorStateInfo(0).IsName("m1_" + LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		foreach (Animator animator in m2LargeSymbols)
		{
			if (animator.gameObject.activeSelf && animator.GetCurrentAnimatorStateInfo(0).IsName("m2_" + LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		foreach (Animator animator in m3LargeSymbols)
		{
			if (animator.gameObject.activeSelf && animator.GetCurrentAnimatorStateInfo(0).IsName("m3_" + LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		return false;
	}

	/// Swap the large symbol overlays with symbols on the reels that can animate
	public static void swapOverlaysForSymbolInstance(ReelGame reelGame, Animator[] m1LargeSymbols, Animator[] m2LargeSymbols, Animator[] m3LargeSymbols, bool isFreeSpins)
	{
		Gen08.swapOverlaysForSymbolInstanceOnReel(reelGame, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, isFreeSpins, 0);

		// only convert the right most reel if the first reel triggered a feature
		ReelFeatureEnum reelOneTriggeredFeature = Gen08.getReelStopFeature(reelGame, 0);
		ReelFeatureEnum reelFiveTriggeredFeature = Gen08.getReelStopFeature(reelGame, 4);
		if (reelOneTriggeredFeature == reelFiveTriggeredFeature)
		{
			Gen08.swapOverlaysForSymbolInstanceOnReel(reelGame, m1LargeSymbols, m2LargeSymbols, m3LargeSymbols, isFreeSpins, 4);
		}
	}

	/// Check and swap the overaly symbol on a specific reel if it qualified for a feature (may handle cases where only the first reel qualified)
	public static void swapOverlaysForSymbolInstanceOnReel(ReelGame reelGame, Animator[] m1LargeSymbols, Animator[] m2LargeSymbols, Animator[] m3LargeSymbols, bool isFreeSpins, int reelIndex)
	{
		ReelFeatureEnum reelTriggeredFeature = Gen08.getReelStopFeature(reelGame, reelIndex);

		if (reelTriggeredFeature != ReelFeatureEnum.RegularSpin)
		{
			string featureSymbolName = "";
			switch (reelTriggeredFeature)
			{
				case ReelFeatureEnum.LollipopWildRespinsM1:
					featureSymbolName = "M1";
					break;
				case ReelFeatureEnum.ChocolateRainWildsM2:
					featureSymbolName = "M2";
					break;
				case ReelFeatureEnum.SugarFrostingM3:
					featureSymbolName = "M3";
					break;
			}

			if (isFreeSpins)
			{
				if (reelIndex == 0)
				{
					reelGame.engine.getReelArray()[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "_Left-3A");
				}
				else
				{
					reelGame.engine.getReelArray()[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "_Right-3A");
				}
			}
			else
			{
				if (reelIndex == 0)
				{
					reelGame.engine.getReelArray()[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "_Left-4A");
				}
				else
				{
					reelGame.engine.getReelArray()[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "_Right-4A");
				}
			}

			turnOffLargeSymbols(m1LargeSymbols, m2LargeSymbols, m3LargeSymbols);
		}
	}

	/// Check if we need to, and if so, play the sounds and effects for a feature of this reel
	public static IEnumerator checkAndPlayReelFeature(ReelGame reelGame, SlotReel stoppedReel, Animator[] m1LargeSymbols, Animator[] m2LargeSymbols, Animator[] m3LargeSymbols, bool isFreeSpins)
	{
		int reelId = stoppedReel.reelID;
		int reelIndex = reelId - 1;

		// this game only has special features on the 1st and 5th reels
		if (reelId == 1)
		{
			ReelFeatureEnum triggeredFeature = Gen08.getReelStopFeature(reelGame, reelIndex);

			if (triggeredFeature != ReelFeatureEnum.RegularSpin)
			{
				if (isFreeSpins)
				{
					Audio.play(Audio.soundMap(FS_STACKED_REEL_1_EXPAND_SOUND_KEY));
				}
				else
				{
					Audio.play(Audio.soundMap(STACKED_REEL_1_EXPAND_SOUND_KEY));
				}

				Animator largeSymbolAnimator = null;
				string animationPrefix = "";

				switch (triggeredFeature)
				{
					case ReelFeatureEnum.LollipopWildRespinsM1:
						animationPrefix = "m1_";
						largeSymbolAnimator = m1LargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
					case ReelFeatureEnum.ChocolateRainWildsM2:
						animationPrefix = "m2_";
						largeSymbolAnimator = m2LargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
					case ReelFeatureEnum.SugarFrostingM3:
						animationPrefix = "m3_";
						largeSymbolAnimator = m3LargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
				}

				if (largeSymbolAnimator != null)
				{
					largeSymbolAnimator.gameObject.SetActive(true);
					largeSymbolAnimator.Play(animationPrefix + LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
				}
			}
		}
		else if (reelId == 5)
		{
			// need to make sure that the reel feature is the same as the triggered one
			ReelFeatureEnum reelOnetriggeredFeature = Gen08.getReelStopFeature(reelGame, 0);
			ReelFeatureEnum triggeredFeature = Gen08.getReelStopFeature(reelGame, reelIndex);

			if (triggeredFeature != ReelFeatureEnum.RegularSpin && reelOnetriggeredFeature == triggeredFeature)
			{
				if (isFreeSpins)
				{
					Audio.play(Audio.soundMap(FS_STACKED_REEL_5_EXPAND_SOUND_KEY));
				}
				else
				{
					Audio.play(Audio.soundMap(STACKED_REEL_5_EXPAND_SOUND_KEY));
				}

				string featureSound = "";
				Animator largeSymbolAnimator = null;
				string animationPrefix = "";

				switch (triggeredFeature)
				{
					case ReelFeatureEnum.LollipopWildRespinsM1:
						animationPrefix = "m1_";
						largeSymbolAnimator = m1LargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = M1_FEATURE_REEL_5_TRIGGER_SOUND_NAME;
						break;
					case ReelFeatureEnum.ChocolateRainWildsM2:
						animationPrefix = "m2_";
						largeSymbolAnimator = m2LargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = M2_FEATURE_REEL_5_TRIGGER_SOUND_NAME;
						break;
					case ReelFeatureEnum.SugarFrostingM3:
						animationPrefix = "m3_";
						largeSymbolAnimator = m3LargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = M3_FEATURE_REEL_5_TRIGGER_SOUND_NAME;
						break;
				}

				if (largeSymbolAnimator != null)
				{
					largeSymbolAnimator.gameObject.SetActive(true);
					largeSymbolAnimator.Play(animationPrefix + LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
				}

				// Wait before playing the next sound
				yield return new TIWaitForSeconds(FEATURE_SOUND_INTRO_DELAY_TIME);

				if (triggeredFeature == ReelFeatureEnum.LollipopWildRespinsM1)
				{
					// need to play a loopped background music for the Glinda feature
					playLoopedMusic(featureSound);
				}
				else
				{
					Audio.play(featureSound);
				}
			}
		}
		else
		{
			// Not a reel we care about
			yield break;
		}
	}

	/// Play the feature text when a feature is acquired
	public static IEnumerator playFeatureTextAnimation(Animator featureText)
	{
		featureText.gameObject.SetActive(true);

		featureText.Play(FEATURE_TEXT_ANIM_NAME);

		// wait till we enter into the animation state
		while (featureText.GetCurrentAnimatorStateInfo(0).IsName(IDLE_ANIM_NAME))
		{
			yield return null;
		}

		// now wait for the animation to finish and go back to the idle state
		while (!featureText.GetCurrentAnimatorStateInfo(0).IsName(IDLE_ANIM_NAME))
		{
			yield return null;
		}

		featureText.gameObject.SetActive(false);
	}

	/// Used by the free spin game so it can unparent the bombs so they aren't destroyed
	public static void unparentAnimationsObjects()
	{
		foreach (GameObject chocolateAnimObj in freeChocolateWildRevealEffects)
		{
			if (SlotBaseGame.instance != null)
			{
				// try to parent back to the base game from the free spin game
				chocolateAnimObj.transform.parent = SlotBaseGame.instance.gameObject.transform;
			}
			// else this is gifted bonus, so leave them attached to the free spin game so they will be cleaned up
		}

		foreach (GameObject lollipopAnimObj in freeLollipopEffects)
		{
			if (SlotBaseGame.instance != null)
			{
				// try to parent back to the base game from the free spin game
				lollipopAnimObj.transform.parent = SlotBaseGame.instance.gameObject.transform;
			}
			// else this is gifted bonus, so leave them attached to the free spin game so they will be cleaned up
		}

		if (SlotBaseGame.instance == null)
		{
			// gifted free spins, need to clear the static effects because they will be left around and cause issues if they aren't
			Gen08.freeChocolateWildRevealEffects.Clear();
			Gen08.freeLollipopEffects.Clear();
		}
	}

	public static IEnumerator startLollipopRespin(Animator[] m1LargeSymbols, GenericIEnumeratorDelegate startNextReevaluationSpinCallback)
	{
		// wait a small amount of time before starting the next spin so the sounds don't all overlap
		yield return new TIWaitForSeconds(WAIT_BETWEEN_LOLLIPOP_RESPIN);

		Audio.play(SPARKLY_WAND_WAVE_SOUND_NAME);
		m1LargeSymbols[(int)LargeSymbolLocEnum.Left].Play("m1_" + LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME);
		m1LargeSymbols[(int)LargeSymbolLocEnum.Right].Play("m1_" + LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME);

		yield return RoutineRunner.instance.StartCoroutine(startNextReevaluationSpinCallback());
	}
}
