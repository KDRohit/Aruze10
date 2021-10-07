using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Implementation for the Oz game Munchkinland / Witches of Oz / Dueling Witches
*/
public class Osa01 : SlotBaseGame
{
	[SerializeField] private Animator[] glindaLargeSymbols = null;				// large glinda side bar symbols for the glinda feautre

	[SerializeField] private Animator[] wickedWitchLargeSymbols = null;			// large side bar symbols for the wicked witch feature
	[SerializeField] private GameObject monkey = null;							// flying monkey used for the wicked witch featere
	[SerializeField] private GameObject monkeyHeldSymbol = null;				// the WD2 symbol that the monkey is holding

	[SerializeField] private Animator[] dorothyLargeSymbols = null;				// large side bar symbols for the dorathy feature
	[SerializeField] private GameObject rubySlipperAnticipation = null;			// played when the ruby slipper is spinning during the dorathy feature

	[SerializeField] private GameObject gameBackground = null;					// Background for the game, moved in sync with the frame
	[SerializeField] private GameObject gameFrame = null;						// Frame for the game, moved in sync with the background 
	[SerializeField] private GameObject freeSpinBackground = null;				// Free spin background used for a transition

	[SerializeField] private GameObject glindaFeatureTextObj = null;			// The text that shows when you get the glinda feature
	[SerializeField] private GameObject wickedWitchFeatureTextObj = null;		// The text that shows when you get the wicked witch feature
	[SerializeField] private GameObject dorothyFeatureTextObj = null;			// The text that shows when you get the dorothy feature
	[SerializeField] private Animation featureTextAnimation = null;				// Animation for the feature text that calls attention to the text

	private bool isTransitionComplete = false;									// Tells if the transition is complete

	public enum ReelFeatureEnum
	{
		RegularSpin = -1,
		GlindaBubbleRespinsM1 = 0,
		WickedWitchFireBallWildsM2 = 1,
		RubySlippersLinkedReelsM3 = 2
	}

	public enum LargeSymbolLocEnum
	{
		Left = 0,
		Right = 1
	}

	public static readonly string[] REEL_FEATURE_SYMBOL_NAMES = { "M1", "M2", "M3" };

	private const float WAIT_AND_SHOW_RUBY_SLIPPER_TIME = 1.0f;				// Small wait so the user can see the large 3x3 ruby slipper that is now on the reels
	private const float SHOW_FIRE_WILD_STAGGER_TIME = 0.4f;					// Time value to stagger the witch fire wilds by so they hit at slightly different times
	private const float FEATURE_SOUND_INTRO_DELAY_TIME = 1.2f;				// Delay before the feature sound starts playing which goes after STACKED_REEL_5_EXPAND_SOUND_KEY
	public const float TIME_BETWEEN_GLINDA_BUBBLES = 0.35f;					// Introduce a small delay as each glinda bubble appears and animates

	private const float MONKEY_FLY_IN_DURATION = 0.7f;						// How long the monkey should take to reach a symbol
	private const float MONKEY_FLY_OUT_DURATION = 0.7f;					// How long the monkey should take to reach a symbol
	private const float MONKEY_END_Y_POS = 7.5f;							// Y position for the monkey to end up off the top of the screen
	private const float MONKEY_START_Y_POS = -7.5f;							// Y position for the monkey to begin at off the bottom of the screen, x will be zero

	private const float BACKGROUND_TRANS_START_X_POS = 0.0f;				// Position that the background starts at during the transition
	private const float BACKGROUND_TRANS_FINISH_X_POS = 1.0f;				// Position that the background moves to during the transition
	private const float FREE_SPIN_BKG_START_X_POS = -22.3f;					// Position that the free spin background for the transition moves in from
	private const float FREE_SPIN_BKG_END_X_POS = 0.0f;						// Position that the free spin background ends up in when the transition is complete
	private const float TRANSITION_SLIDE_TIME = 1.75f;						// How long the slide over transition takes

	private const string LARGE_SYMBOL_REVEAL_ANIMATION_NAME = "reveal";		// Name key for the symbol to play a reveal
	private const string LARGE_SYMBOL_ANIAMTE_ANIMATION_NAME = "animate";	// Name key for the symbol to do an animation

	private const string GLINDA_BUBBLE_ANIM_NAME = "anticipation_small_v01";	// Bubble animation for the glinda wilds

	private const string BACKGROUND_MUSIC_SOUND_KEY = "reelspin_base";						// Basic background music, used to restore the background music
	
	private const string STACKED_REEL_1_EXPAND_SOUND_KEY = "stacked_reel_expand_1"; 		// Sound for the 1st reel expanding
	private const string STACKED_REEL_5_EXPAND_SOUND_KEY = "stacked_reel_expand_5";			// Sound for the 5th reel expanding

	private const string FS_STACKED_REEL_1_EXPAND_SOUND_KEY = "stacked_reel_freespin_expand_1";	// Sound for the free spin 1st reel expanding
	private const string FS_STACKED_REEL_5_EXPAND_SOUND_KEY = "stacked_reel_freespin_expand_5";	// Sound for the free spin 5th reel expanding

	private const string GLINDA_REEL_5_TRIGGER_SOUND_NAME = "GlindaWildBg";					// Sound for the glinda feature triggering on 5th reel
	private const string WITCH_REEL_5_TRIGGER_SOUND_NAME = "WitchWildBg";					// Sound for the wicked witch feature triggering on the 5th reel
	private const string DOROTHY_REEL_5_TRIGGER_SOUND_NAME = "DorothyWildBg";				// Sound for the dorothy feature triggering on the 5th reel
	private const string DOROTHY_RESPIN_MUSIC = "DorothyRespin";							// Music for the dorothy respin
	
	private const string MONKEY_SHRIEK_SOUND_NAME = "MonkeyShriekOSA01";					// Sound the monkeys make when they fly in
	private const string MONKEY_SYMBOL_DROP_SOUND_NAME = "DropWildSparklyThud";				// Sound when the monkey drops the wild symbol he is carrying

	private const string RUBY_SLIPPER_CLICK_SOUND_NAME = "DorothyWildClickHeelsSparkly";	// Sound when the large ruby slipper symbol appears

	private const string GLINDA_BUBBLE_SOUND_NAME = "GlindaWildBubble";						// Sound for the bubble symbols of the glinda feature

	protected override void Awake()
	{
		base.Awake();
		
		monkey.SetActive(false);
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());
		
		// hide large symbols that might be left over from a feature occuring on the previous spin
		Osa01.turnOffLargeSymbols(glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols);
		Osa01.splitAnyLargeSideSymbols(this);
	}

	/// Custom handling for specific reel features
	protected override IEnumerator handleSpecificReelStop(SlotReel stoppedReel)
	{
		// want to ignore and not re-play the effects on reevaluation spins
		if (currentReevaluationSpin == null)
		{
			yield return StartCoroutine(Osa01.checkAndPlayReelFeature(this, stoppedReel, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, false));
		}
	}

	/// Overriding to handle what to do before the ruby slipper spin starts
	protected override IEnumerator startNextReevaluationSpin()
  	{
  		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);

		if (triggeredFeature == ReelFeatureEnum.RubySlippersLinkedReelsM3)
		{
			// turn side symbols back on so matrix validation doesn't have an issue
			dorothyLargeSymbols[(int)LargeSymbolLocEnum.Left].gameObject.SetActive(true);
  			dorothyLargeSymbols[(int)LargeSymbolLocEnum.Right].gameObject.SetActive(true);
  			Osa01.splitAnyLargeSideSymbols(this);

  			// clear pay boxes
			clearOutcomeDisplay();

			// do the ruby slipper spin
			yield return StartCoroutine(Osa01.doRubySlippersLinkedReels(this, base.startNextReevaluationSpin, rubySlipperAnticipation, false));
		}
		else
		{
			yield return StartCoroutine(base.startNextReevaluationSpin());
		}
  	}

	/// overridable function for handling a symbol becoming stuck on the reels, may become stuck as different symbol, passed in by stuckSymbolName
	protected override IEnumerator changeSymbolToSticky(SlotSymbol symbol, string stickSymbolName, int row)
	{
		Osa01.changeSymbolToBubble(this, symbol, stickSymbolName, row);
		yield return new TIWaitForSeconds(Osa01.TIME_BETWEEN_GLINDA_BUBBLES);	
	}

	protected override void reelsStoppedCallback()
	{
		// Must use the RoutineRunner.instance to start this coroutine,
		// since this gameObject might get disabled before the coroutine can finish.
		RoutineRunner.instance.StartCoroutine(reelsStoppedCoroutine());
	}

	/// handle stuff a derived class needs to do after a reevaluation spin, this occurs AFTER sticky symbols are handled and data is validated
	protected override IEnumerator handleReevaluationReelStop()
	{
		// using this to track if this is the last spin after the base class goes, since it might decrement how many spins remain
		bool isFinalReevaluationSpin = false;

		if (!hasReevaluationSpinsRemaining)
		{
			isFinalReevaluationSpin = true;

			Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, false);

			// make sure the ruby slipper anticipation is hidden
			if (rubySlipperAnticipation.activeSelf)
			{
				rubySlipperAnticipation.SetActive(false);
			}
		}

		yield return StartCoroutine(base.handleReevaluationReelStop());

		// turn off the music from the ruby slipper or glinda spin and go back to the regular music
		ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);
		if (isFinalReevaluationSpin && (triggeredFeature == ReelFeatureEnum.RubySlippersLinkedReelsM3 || triggeredFeature == ReelFeatureEnum.GlindaBubbleRespinsM1))
		{
			switchBackToNormalBkgMusic();
		}
	}

	/// Transition the game music back to the standard background
	private void switchBackToNormalBkgMusic()
	{
		Osa01.playLoopedMusic(Audio.soundMap(BACKGROUND_MUSIC_SOUND_KEY));
	}

	/// Play looped music
	private static void playLoopedMusic(string musicKey)
	{
		Audio.switchMusicKeyImmediate(musicKey);
	}

	/// Handles custom transition stuff for this game as well as standard
	/// reel stop override stuff
	private IEnumerator reelsStoppedCoroutine()
	{
		bool didTransition = false;

		if (_outcome.isBonus)
		{
			didTransition = true;

			// handle playing this early, so that it happens before the transition starts
			yield return StartCoroutine(doPlayBonusAcquiredEffects());

			// Do the transition before going to the free spins game.
			yield return StartCoroutine(doFreeSpinsTransition());
		}

		// need to wait for the reveal animations to finish before moving on
		while (Osa01.areLargeSymbolOverlaysAnimating(glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols))
		{
			yield return null;
		}

		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Osa01.getTriggeredFeature(this);

		switch (triggeredFeature)
		{
			case ReelFeatureEnum.RegularSpin:
				// just a normal spin
				Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, false);
				base.reelsStoppedCallback();
				break;
			case ReelFeatureEnum.GlindaBubbleRespinsM1:
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(glindaFeatureTextObj, featureTextAnimation));
				StartCoroutine(Osa01.doGlindaBubbleRespins(this, base.reelsStoppedCallback));
				break;
			case ReelFeatureEnum.WickedWitchFireBallWildsM2:
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(wickedWitchFeatureTextObj, featureTextAnimation));
				StartCoroutine(Osa01.doWickedWitchFireballWilds(this, base.reelsStoppedCallback, monkey, monkeyHeldSymbol, wickedWitchLargeSymbols, false));
				break;
			case ReelFeatureEnum.RubySlippersLinkedReelsM3:
				// handled in startNextReevaluationSpin() override
				yield return StartCoroutine(Osa01.playFeatureTextAnimation(dorothyFeatureTextObj, featureTextAnimation));
				Osa01.swapOverlaysForSymbolInstance(this, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, false);
				base.reelsStoppedCallback();
				break;
		}

		if (didTransition)
		{
			// Wait for the free spins game to finish loading before cleaning up this transition,
			// so we don't see the base game between the transition and the free spins game.
			while (FreeSpinGame.instance == null)
			{
				yield return null;
			}

			// Wait for the reparent, otherwise you will get a frame of the base game still rendering
			// before the freespins renders
			while (BonusGamePresenter.instance.gameObject.transform.parent != BonusGameManager.instance.transform)
			{
				yield return null;
			}

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

			Vector3 currentBkgPosition = gameBackground.transform.localPosition;
			gameBackground.transform.localPosition = new Vector3(BACKGROUND_TRANS_START_X_POS, currentBkgPosition.y, currentBkgPosition.z);
			gameFrame.transform.localPosition = new Vector3(BACKGROUND_TRANS_START_X_POS, currentBkgPosition.y, currentBkgPosition.z);

			Vector3 currentFreeSpinBkgPos = freeSpinBackground.transform.localPosition;
			freeSpinBackground.transform.localPosition = new Vector3(FREE_SPIN_BKG_START_X_POS, currentFreeSpinBkgPos.y, currentFreeSpinBkgPos.z);

			// put the top bar back where it came from and turn the UIAnchors back on
			Overlay.instance.top.restorePosition();

			// put the extra wings we were using to hide the part behind the nav bar away
			BonusGameManager.instance.wings.hide();
		}

		yield break;
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

		yield return new TIWaitForSeconds(1.0f);

		isTransitionComplete = false;
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", TRANSITION_SLIDE_TIME, "onupdate", "slideBackgrounds", "oncomplete", "onBackgroundSlideComplete"));
		
		// show larger wings to hide the spots behind the top bar
		BonusGameManager.instance.wings.forceShowNormalWings(true);

		// move the top bar over and off the screen
		yield return StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Right, TRANSITION_SLIDE_TIME, false));

		// wait for the backgrounds to get in place
		while (!isTransitionComplete)
		{
			yield return null;
		}
	}

	/// Function called by iTween to slide the backgrounds, slideAmount is 0.0f-1.0f
	public void slideBackgrounds(float slideAmount)
	{
		float targetBkgXPos = ((BACKGROUND_TRANS_FINISH_X_POS - BACKGROUND_TRANS_START_X_POS) * slideAmount) + BACKGROUND_TRANS_START_X_POS;
		Vector3 currentBkgPosition = gameBackground.transform.localPosition;
		gameBackground.transform.localPosition = new Vector3(targetBkgXPos, currentBkgPosition.y, currentBkgPosition.z);
		gameFrame.transform.localPosition = new Vector3(targetBkgXPos, currentBkgPosition.y, currentBkgPosition.z);

		float targetFreeSpinBkgXPos = ((FREE_SPIN_BKG_END_X_POS - FREE_SPIN_BKG_START_X_POS) * slideAmount) + FREE_SPIN_BKG_START_X_POS;
		Vector3 currentFreeSpinBkgPos = freeSpinBackground.transform.localPosition;
		freeSpinBackground.transform.localPosition = new Vector3(targetFreeSpinBkgXPos, currentFreeSpinBkgPos.y, currentFreeSpinBkgPos.z);
	}

	/// Callback for when the background slide is complete
	public void onBackgroundSlideComplete()
	{
		Vector3 currentBkgPosition = gameBackground.transform.localPosition;
		gameBackground.transform.localPosition = new Vector3(BACKGROUND_TRANS_FINISH_X_POS, currentBkgPosition.y, currentBkgPosition.z);
		gameFrame.transform.localPosition = new Vector3(BACKGROUND_TRANS_FINISH_X_POS, currentBkgPosition.y, currentBkgPosition.z);

		Vector3 currentFreeSpinBkgPos = freeSpinBackground.transform.localPosition;
		freeSpinBackground.transform.localPosition = new Vector3(FREE_SPIN_BKG_END_X_POS, currentFreeSpinBkgPos.y, currentFreeSpinBkgPos.z);

		isTransitionComplete = true;
	}

	/// Change a symbol into a bubble
	public static void changeSymbolToBubble(ReelGame reelGame, SlotSymbol symbol, string stickSymbolName, int row)
	{
		symbol.mutateTo(stickSymbolName);

		// Add a stuck symbol
		SlotSymbol newSymbol = reelGame.createStickySymbol(stickSymbolName, symbol.index, symbol.reel);
		SymbolAnimator symbolAnimator = newSymbol.animator;
		symbolAnimator.material.shader = SymbolAnimator.defaultShader("Unlit/GUI Texture (+100)");
		CommonGameObject.setLayerRecursively(symbolAnimator.gameObject, Layers.ID_SLOT_OVERLAY);

		symbolAnimator.scalingSymbolPart.GetComponent<Animation>().enabled = true;
		symbolAnimator.staticRendererEnabled = false;
		symbolAnimator.skinnedRendererEnabled = true;
		AnimationState animationState = symbolAnimator.scalingSymbolPart.GetComponent<Animation>()[GLINDA_BUBBLE_ANIM_NAME];
		if (animationState != null)
		{
			animationState.wrapMode = WrapMode.Once;
			animationState.time = 0f;
			symbolAnimator.scalingSymbolPart.GetComponent<Animation>().Play(Osa01.GLINDA_BUBBLE_ANIM_NAME);
		}
		else
		{
			Debug.LogError("Can't find glinda bubble anim!");
		}

		Audio.play(Osa01.GLINDA_BUBBLE_SOUND_NAME);
	}

	/// Coroutine to handle what happens when the GlindaBubbleRespinsM1 feature is triggered
	public static IEnumerator doGlindaBubbleRespins(ReelGame reelGame, GenericDelegate gameReelStoppedCallback)
	{
		gameReelStoppedCallback();

		yield break;
	}

	/// Coroutine to handle what happens when the WickedWitchFireBallWildsM2 feature is triggered
	public static IEnumerator doWickedWitchFireballWilds(ReelGame reelGame, GenericDelegate gameReelStoppedCallback, GameObject monkey, GameObject monkeyHeldSymbol, Animator[] wickedWitchLargeSymbols, bool isFreeSpins)
	{
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
						yield return RoutineRunner.instance.StartCoroutine(doMonkeyReplaceSymbol(monkey, monkeyHeldSymbol, reelArray[i].visibleSymbolsBottomUp[j]));
					}
				}
			}
		}
			
		if (isFreeSpins)
		{
			reelArray[0].visibleSymbols[0].mutateTo("M2-3A");
			reelArray[4].visibleSymbols[0].mutateTo("M2-3A");
		}
		else
		{
			reelArray[0].visibleSymbols[0].mutateTo("M2-4A");
			reelArray[4].visibleSymbols[0].mutateTo("M2-4A");
		}

		foreach (Animator symbol in wickedWitchLargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		gameReelStoppedCallback();
		yield break;
	}

	/// Function to handle the monkey flying in and replacing symbols with wilds
	public static IEnumerator doMonkeyReplaceSymbol(GameObject monkey, GameObject monkeyHeldSymbol, SlotSymbol symbol)
	{
		// Reset the monkey
		monkey.transform.localPosition = new Vector3(0, MONKEY_START_Y_POS, -3);
		monkeyHeldSymbol.SetActive(true);
		monkey.SetActive(true);

		// Move the monkey to the symbol that will be replaced
		Vector3 endPos = symbol.animator.gameObject.transform.position;
		endPos.z = monkey.transform.position.z;
		iTween.MoveTo(monkey, iTween.Hash("position", endPos, "islocal", false, "time", MONKEY_FLY_IN_DURATION, "easetype", iTween.EaseType.easeInOutQuad));

		// Play monkey sound in the middle of the monkey flying in
		yield return new TIWaitForSeconds(MONKEY_FLY_IN_DURATION / 2);
		Audio.play(MONKEY_SHRIEK_SOUND_NAME);
		yield return new TIWaitForSeconds(MONKEY_FLY_IN_DURATION / 2);

		// replace the symbol
		symbol.mutateTo("WD2");
		monkeyHeldSymbol.SetActive(false);
		Audio.play(MONKEY_SYMBOL_DROP_SOUND_NAME);

		// Move the monkey off the screen
		yield return new TITweenYieldInstruction(iTween.MoveTo(monkey, iTween.Hash("y", MONKEY_END_Y_POS, "islocal", true, "time", MONKEY_FLY_OUT_DURATION, "easetype", iTween.EaseType.linear)));

		monkey.SetActive(false);
	}

	/// Coroutine to handle what happens when the RubySlippersLinkedReelsM3 feature is triggered
	public static IEnumerator doRubySlippersLinkedReels(ReelGame reelGame, GenericIEnumeratorDelegate startNextReevaluationSpin, GameObject rubySlipperAnticipation, bool isFreeSpins)
	{
		// Change the middle reels into the Wild slipper
		if (isFreeSpins)
		{
			reelGame.engine.getReelArray()[1].visibleSymbols[0].mutateTo("WD-4A-3A");
		}
		else
		{
			reelGame.engine.getReelArray()[1].visibleSymbols[0].mutateTo("WD-3A-3A");
		}

		Audio.play(RUBY_SLIPPER_CLICK_SOUND_NAME);

		yield return new TIWaitForSeconds(WAIT_AND_SHOW_RUBY_SLIPPER_TIME);

		rubySlipperAnticipation.SetActive(true);

		Audio.play(DOROTHY_RESPIN_MUSIC);

		// need to explicitly call this since we showed the outcomes but told the ReelGame we didn't want it to immediatly start the next reeval spin
		yield return RoutineRunner.instance.StartCoroutine(startNextReevaluationSpin());
	}

	/// Checks if a feature is being triggered, or if this is just a basic spin
	public static ReelFeatureEnum getTriggeredFeature(ReelGame reelGame)
	{
		if (Osa01.isTriggeringRealFeature(reelGame, ReelFeatureEnum.GlindaBubbleRespinsM1))
		{
			return ReelFeatureEnum.GlindaBubbleRespinsM1;
		}
		else if (Osa01.isTriggeringRealFeature(reelGame, ReelFeatureEnum.WickedWitchFireBallWildsM2))
		{
			return ReelFeatureEnum.WickedWitchFireBallWildsM2;
		}
		else if (Osa01.isTriggeringRealFeature(reelGame, ReelFeatureEnum.RubySlippersLinkedReelsM3))
		{
			return ReelFeatureEnum.RubySlippersLinkedReelsM3;
		}
		else
		{
			return ReelFeatureEnum.RegularSpin;
		}
	}

	/// Used to determine if a specific reel should be triggering a feature
	public static ReelFeatureEnum getReelStopFeature(ReelGame reelGame, int reelNum)
	{
		if (Osa01.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.GlindaBubbleRespinsM1))
		{
			return ReelFeatureEnum.GlindaBubbleRespinsM1;
		}
		else if (Osa01.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.WickedWitchFireBallWildsM2))
		{
			return ReelFeatureEnum.WickedWitchFireBallWildsM2;
		}
		else if (Osa01.doesReelContainAllFeatureSymbol(reelGame, reelNum, ReelFeatureEnum.RubySlippersLinkedReelsM3))
		{
			return ReelFeatureEnum.RubySlippersLinkedReelsM3;
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

		isFeatureTriggered &= Osa01.doesReelContainAllFeatureSymbol(reelGame, 0, feature);
		isFeatureTriggered &= Osa01.doesReelContainAllFeatureSymbol(reelGame, 4, feature);

		return isFeatureTriggered;
	}

	/// Checks if a feature should be anticipated
	public static ReelFeatureEnum getAnticipatedFeature(ReelGame reelGame)
	{
		if (Osa01.isAnticipatingFeature(reelGame, ReelFeatureEnum.GlindaBubbleRespinsM1))
		{
			return ReelFeatureEnum.GlindaBubbleRespinsM1;
		}
		else if (Osa01.isAnticipatingFeature(reelGame, ReelFeatureEnum.WickedWitchFireBallWildsM2))
		{
			return ReelFeatureEnum.WickedWitchFireBallWildsM2;
		}
		else if (Osa01.isAnticipatingFeature(reelGame, ReelFeatureEnum.RubySlippersLinkedReelsM3))
		{
			return ReelFeatureEnum.RubySlippersLinkedReelsM3;
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
		return Osa01.doesReelContainAllFeatureSymbol(reelGame, 0, feature);
	}

	/// Check if all the symbols on the passed in reel number match the feature we are checking for
	public static bool doesReelContainAllFeatureSymbol(ReelGame reelGame, int reelNum, ReelFeatureEnum feature)
	{
		SlotReel[] reelArray = reelGame.engine.getReelArray();

		foreach (SlotSymbol slotSymbol in reelArray[reelNum].visibleSymbols)
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
		return Osa01.getFeatureAnticipationName(this);
	}

	/// Generic function to be shared with free spins to determine the anticipation to use for a feature
	public static string getFeatureAnticipationName(ReelGame reelGame)
	{
		// need to detect if a special mode should be triggered based on major symbols present in reels 1 and 5
		ReelFeatureEnum triggeredFeature = Osa01.getAnticipatedFeature(reelGame);

		switch (triggeredFeature)
		{
			case ReelFeatureEnum.RegularSpin:
				// must be anticipating the bonus
				return "BN";
			case ReelFeatureEnum.GlindaBubbleRespinsM1:
				return "M1";
			case ReelFeatureEnum.WickedWitchFireBallWildsM2:
				return "M2";
			case ReelFeatureEnum.RubySlippersLinkedReelsM3:
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
			topLeftSymbol.splitSymbol();
		}

		SlotSymbol topRightSymbol = reelArray[4].visibleSymbols[0];
		if (topRightSymbol.canBeSplit())
		{
			topRightSymbol.splitSymbol();
		}
	}

	/// Turn off the large symbols
	public static void turnOffLargeSymbols(Animator[] glindaLargeSymbols, Animator[] wickedWitchLargeSymbols, Animator[] dorothyLargeSymbols)
	{
		foreach (Animator symbol in glindaLargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		foreach (Animator symbol in wickedWitchLargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}

		foreach (Animator symbol in dorothyLargeSymbols)
		{
			symbol.gameObject.SetActive(false);
		}
	}

	/// Double check if any animators are still going, need to check this so we make sure they are done before processing the end of a spin
	public static bool areLargeSymbolOverlaysAnimating(Animator[] glindaLargeSymbols, Animator[] wickedWitchLargeSymbols, Animator[] dorothyLargeSymbols)
	{
		foreach (Animator animator in glindaLargeSymbols)
		{
			if (animator.GetCurrentAnimatorStateInfo(0).IsName(LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		foreach (Animator animator in wickedWitchLargeSymbols)
		{
			if (animator.GetCurrentAnimatorStateInfo(0).IsName(LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		foreach (Animator animator in dorothyLargeSymbols)
		{
			if (animator.GetCurrentAnimatorStateInfo(0).IsName(LARGE_SYMBOL_REVEAL_ANIMATION_NAME))
			{
				// one of the overlays is still revealing!
				return true;
			}
		}

		return false;
	}

	/// Swap the large symbol overlays with symbols on the reels that can animate
	public static void swapOverlaysForSymbolInstance(ReelGame reelGame, Animator[] glindaLargeSymbols, Animator[] wickedWitchLargeSymbols, Animator[] dorothyLargeSymbols, bool isFreeSpins)
	{
		Osa01.swapOverlaysForSymbolInstanceOnReel(reelGame, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, isFreeSpins, 0);

		// only convert the right most reel if the first reel triggered a feature
		ReelFeatureEnum reelOneTriggeredFeature = Osa01.getReelStopFeature(reelGame, 0);
		ReelFeatureEnum reelFiveTriggeredFeature = Osa01.getReelStopFeature(reelGame, 4);
		if (reelOneTriggeredFeature == reelFiveTriggeredFeature)
		{
			Osa01.swapOverlaysForSymbolInstanceOnReel(reelGame, glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols, isFreeSpins, 4);
		}
	}

	/// Check and swap the overaly symbol on a specific reel if it qualified for a feature (may handle cases where only the first reel qualified)
	public static void swapOverlaysForSymbolInstanceOnReel(ReelGame reelGame, Animator[] glindaLargeSymbols, Animator[] wickedWitchLargeSymbols, Animator[] dorothyLargeSymbols, bool isFreeSpins, int reelIndex)
	{
		ReelFeatureEnum reelTriggeredFeature = Osa01.getReelStopFeature(reelGame, reelIndex);

		if (reelTriggeredFeature != ReelFeatureEnum.RegularSpin)
		{
			string featureSymbolName = "";
			switch (reelTriggeredFeature)
			{
				case ReelFeatureEnum.GlindaBubbleRespinsM1:
					featureSymbolName = "M1";
					break;
				case ReelFeatureEnum.WickedWitchFireBallWildsM2:
					featureSymbolName = "M2";
					break;
				case ReelFeatureEnum.RubySlippersLinkedReelsM3:
					featureSymbolName = "M3";
					break;
			}

			SlotReel[] reelArray = reelGame.engine.getReelArray();

			if (isFreeSpins)
			{
				reelArray[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "-3A");
			}
			else
			{
				reelArray[reelIndex].visibleSymbols[0].mutateTo(featureSymbolName + "-4A");
			}

			turnOffLargeSymbols(glindaLargeSymbols, wickedWitchLargeSymbols, dorothyLargeSymbols);
		}
	}

	/// Check if we need to, and if so, play the sounds and effects for a feature of this reel
	public static IEnumerator checkAndPlayReelFeature(ReelGame reelGame, SlotReel stoppedReel, Animator[] glindaLargeSymbols, Animator[] wickedWitchLargeSymbols, Animator[] dorothyLargeSymbols, bool isFreeSpins)
	{
		int reelId = stoppedReel.reelID;
		int reelIndex = reelId - 1;

		// this game only has special features on the 1st and 5th reels
		if (reelId == 1)
		{
			ReelFeatureEnum triggeredFeature = Osa01.getReelStopFeature(reelGame, reelIndex);

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

				switch (triggeredFeature)
				{
					case ReelFeatureEnum.GlindaBubbleRespinsM1:
						largeSymbolAnimator = glindaLargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
					case ReelFeatureEnum.WickedWitchFireBallWildsM2:
						largeSymbolAnimator = wickedWitchLargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
					case ReelFeatureEnum.RubySlippersLinkedReelsM3:
						largeSymbolAnimator = dorothyLargeSymbols[(int)LargeSymbolLocEnum.Left];
						break;
				}

				if (largeSymbolAnimator != null)
				{
					largeSymbolAnimator.gameObject.SetActive(true);
					largeSymbolAnimator.Play(LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
				}
			}
		}
		else if (reelId == 5)
		{
			// need to make sure that the reel feature is the same as the triggered one
			ReelFeatureEnum reelOnetriggeredFeature = Osa01.getReelStopFeature(reelGame, 0);
			ReelFeatureEnum triggeredFeature = Osa01.getReelStopFeature(reelGame, reelIndex);

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

				switch (triggeredFeature)
				{
					case ReelFeatureEnum.GlindaBubbleRespinsM1:
						largeSymbolAnimator = glindaLargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = GLINDA_REEL_5_TRIGGER_SOUND_NAME;
						break;
					case ReelFeatureEnum.WickedWitchFireBallWildsM2:
						largeSymbolAnimator = wickedWitchLargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = WITCH_REEL_5_TRIGGER_SOUND_NAME;
						break;
					case ReelFeatureEnum.RubySlippersLinkedReelsM3:
						largeSymbolAnimator = dorothyLargeSymbols[(int)LargeSymbolLocEnum.Right];
						featureSound = DOROTHY_REEL_5_TRIGGER_SOUND_NAME;
						break;
				}

				if (largeSymbolAnimator != null)
				{
					largeSymbolAnimator.gameObject.SetActive(true);
					largeSymbolAnimator.Play(LARGE_SYMBOL_REVEAL_ANIMATION_NAME);
				}

				// Wait before playing the next sound
				yield return new TIWaitForSeconds(FEATURE_SOUND_INTRO_DELAY_TIME);

				if (triggeredFeature == ReelFeatureEnum.GlindaBubbleRespinsM1)
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
	public static IEnumerator playFeatureTextAnimation(GameObject featureText, Animation animation)
	{
		featureText.SetActive(true);

		animation.Play();

		while (animation.isPlaying)
		{
			yield return null;
		}

		featureText.SetActive(false);
	}
}
