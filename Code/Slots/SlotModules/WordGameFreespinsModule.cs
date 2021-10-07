using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Module for handling a game where you get a word with letter values that increase by hitting symbols on the reels (originally used for zynga04 free spins)
Original Author: Scott Lepthien
*/
public class WordGameFreespinsModule : SlotModule 
{
	[SerializeField] private List<Animator> letterAnimators;
	[SerializeField] private List<Transform> letterTileCenters;
	[SerializeField] private string LETTER_OFF_ANIM_NAME = "off";
	[SerializeField] private string LETTER_ON_ANIM_NAME = "on";
	[SerializeField] private float DELAY_BETWEEN_REMOVE_OLD_LETTERS;
	[SerializeField] private float DELAY_BETWEEN_NEW_LETTERS;
	[SerializeField] private List<UILabel> letterLabelTexts;	// To be removed when prefabs are updated.
	[SerializeField] private List<LabelWrapperComponent> letterLabelTextsWrapperComponent;

	public List<LabelWrapper> letterLabelTextsWrapper
	{
		get
		{
			if (_letterLabelTextsWrapper == null)
			{
				_letterLabelTextsWrapper = new List<LabelWrapper>();

				if (letterLabelTextsWrapperComponent != null && letterLabelTextsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in letterLabelTextsWrapperComponent)
					{
						_letterLabelTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in letterLabelTexts)
					{
						_letterLabelTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _letterLabelTextsWrapper;
		}
	}
	private List<LabelWrapper> _letterLabelTextsWrapper = null;	
	
	[SerializeField] private List<UILabel> letterValueTexts;	// To be removed when prefabs are updated.
	[SerializeField] private List<LabelWrapperComponent> letterValueTextsWrapperComponent;

	public List<LabelWrapper> letterValueTextsWrapper
	{
		get
		{
			if (_letterValueTextsWrapper == null)
			{
				_letterValueTextsWrapper = new List<LabelWrapper>();

				if (letterValueTextsWrapperComponent != null && letterValueTextsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in letterValueTextsWrapperComponent)
					{
						_letterValueTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in letterValueTexts)
					{
						_letterValueTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _letterValueTextsWrapper;
		}
	}
	private List<LabelWrapper> _letterValueTextsWrapper = null;	
	
	[SerializeField] private List<Animator> letterEffectsAnimators;
	[SerializeField] private string LETTER_EFFECT_DL_ANIM_NAME = "DL";
	[SerializeField] private string LETTER_EFFECT_TL_ANIM_NAME = "TL";
	[SerializeField] private string LETTER_EFFECT_DW_ANIM_NAME = "DW";
	[SerializeField] private string LETTER_EFFECT_TW_ANIM_NAME = "TW";
	[SerializeField] private float UPDATE_LETTER_VALUE_DURING_EFFECT_DELAY_TIME;
	[SerializeField] protected LabelWrapperComponent jackpotLabelText;
	[SerializeField] private Animator playButtonAnimator;
	[SerializeField] protected Transform customSCFlyToTransform = null;
	[SerializeField] private string PLAY_BUTTON_OFF_ANIM_NAME = "play button off";
	[SerializeField] private string PLAY_BUTTON_PLAY_ANIM_NAME = "play button play";
	[SerializeField] private Animator barEffectAnimator;
	[SerializeField] private float UPDATE_JACKPOT_BAR_DURING_EFFECT_DELAY_TIME;
	[SerializeField] private string BAR_EFFECT_DL_ANIM_NAME = "bar effect DL";
	[SerializeField] private string BAR_EFFECT_TL_ANIM_NAME = "bar effect TL";
	[SerializeField] private string BAR_EFFECT_DW_ANIM_NAME = "bar effect DW";
	[SerializeField] private string BAR_EFFECT_TW_ANIM_NAME = "bar effect TW";
	[SerializeField] private Animator playBarCelebrationEffectAnimator;
	[SerializeField] private string PLAY_BAR_CELEB_EFFECT_ON_ANIM_NAME = "on";
	[SerializeField] private string PLAY_BAR_CELEB_EFFECT_OFF_ANIM_NAME = "off";
	[SerializeField] private GameObject doubleWordSparkleBurstEffect;
	[SerializeField] private GameObject tripleWordSparkleBurstEffect;
	[SerializeField] private float SPARKLE_BURST_WAIT_BEFORE_HIDING;
	[SerializeField] private float PLAY_BAR_CELEB_WAIT_BEFORE_HIDING;												// controls how long the bar celebration animation plays for getting the jackpot
	[SerializeField] protected Animator playBarTransitionAnimator = null;
	[SerializeField] protected GameObject activatedPlayButton = null;
	[SerializeField] protected string PLAY_BAR_INTRO_TRANSITION_ANIM_NAME = "";
	[SerializeField] protected bool isPlayBarIntroTransitionAnimBlocking = true;
	[SerializeField] protected string PLAY_BAR_OUTRO_TRANSITION_ANIM_NAME = "";
	[SerializeField] private bool playAnticipationAndOutcome = false;												// play both anticipation and outcome before symbol fly
	[SerializeField] private GameObject wd2FlyingSymbolPrefab;														// cacher for flying symbol effects
	[SerializeField] private GameObject wd3FlyingSymbolPrefab;														// cacher for flying symbol effects
	[SerializeField] private GameObject w2FlyingSymbolPrefab;														// cacher for flying symbol effects
	[SerializeField] private GameObject w3FlyingSymbolPrefab;														// cacher for flying symbol effects
	[SerializeField] private GameObject scFlyingSymbolPrefab;                                                       // cacher for flying symbol effects
	[SerializeField] private AnimationListController.AnimationInformationList scLandedAnimationList;				// animations to play after scFlyingSymbolPrefab lands
	[SerializeField] private WordJustificationEnum wordJustification = WordJustificationEnum.LEFT_JUSTIFIED;		// how the word is placed on the word bar
	[SerializeField] private GameObject flyingSymbolPoolObject;														// where the flying symbol cached objects are stored, auto created if not set
	[SerializeField] private float FLYING_SYMBOL_FLY_TIME = 1.0f;													// the time it takes for flying symbols to reach their targets
	[SerializeField] private Vector3 FLYING_LETTER_SYMBOL_END_SCALE;												// controls the final scale of the flying WD2/WD3 symbols
	[SerializeField] private Vector3 FLYING_WORD_SYMBOL_END_SCALE;													// controls the final scale of the flying W2/W3 symbols
	[SerializeField] private Vector3 FLYING_SC_SYMBOL_END_SCALE;													// controls the final scale of the flying sc symbols
	private string DOUBLE_LETTER_SYMBOL_NAME = "WD2";
	private string TRIPLE_LETTER_SYMBOL_NAME = "WD3";
	protected const string DOUBLE_WORD_SYMBOL_NAME = "W2";
	protected const string TRIPLE_WORD_SYMBOL_NAME = "W3";
	protected const string PLAY_WORD_SYMBOL_NAME = "SC";
	[SerializeField] private string REPLACE_SPECIAL_SYMBOL_WITH_SYMBOL_NAME = "WD";

	// Sounds are mostly not sound mapped for now, because this game may not be cloned much if ever
	[SerializeField] private float SYMBOL_INIT_VO_DELAY = 0f;
	[SerializeField] private string DOUBLE_LETTER_SYMBOL_INIT_SOUND; 
	[SerializeField] private string DOUBLE_LETTER_SYMBOL_VO_SOUND;
	[SerializeField] private string DOUBLE_LETTER_APPLIED_SOUND;
	[SerializeField] private string TRIPLE_LETTER_SYMBOL_INIT_SOUND;
	[SerializeField] private string TRIPLE_LETTER_SYMBOL_VO_SOUND; 
	[SerializeField] private string TRIPLE_LETTER_APPLIED_SOUND;
	[SerializeField] private string DOUBLE_WORD_SYMBOL_INIT_SOUND; 
	[SerializeField] private string DOUBLE_WORD_SYMBOL_VO_SOUND;
	[SerializeField] private string TRIPLE_WORD_SYMBOL_INIT_SOUND; 
	[SerializeField] private string TRIPLE_WORD_SYMBOL_VO_SOUND;
	[SerializeField] private string PLAY_SYMBOL_INIT_SOUND;
	[SerializeField] private string PLAY_SYMBOL_VO_SOUND; 
	[SerializeField] private string PLAY_WORD_APPLIED_SOUND;
	[SerializeField] private string SYMBOL_TRAVEL_TO_TOP_SOUND;
	[SerializeField] protected string SHOW_JACKPOT_VALUE_SOUND;
	[SerializeField] private string JACKPOT_INCREMENT_SOUND;
	[SerializeField] private string NEW_TILE_LOAD_SOUND;
	[SerializeField] private string ROLLUP_JACKPOT_SOUND;
	[SerializeField] private string ROLLUP_TERM_JACKPOT_SOUND;
	[SerializeField] private string TRANSITION_SOUND;

	private Dictionary<string, long> letterScores = new Dictionary<string, long>(); // track what the letters are worth, this only comes down in the first mutation of the game
	private List<long> currentLetterValues = new List<long>();		// Current letter values, not multiplied for now because it will cause an issue
	private string currentWord = "";								// Track the current word so we can figure out the offset depending on how the word is aligned
	protected long gameMultiplier = 1;								// Need to track this so we can correctly calculate the final jackpot value
	private long numLetterAnimsGoing = 0;							// Tracks a coroutine that plays animations on all the letters at once for a word multiplier
	private GameObjectCacher wd2FlyingSymbolCacher;					// Caches the flying symbol effect for WD2
	private GameObjectCacher wd3FlyingSymbolCacher;					// Caches the flying symbol effect for WD3
	private GameObjectCacher w2FlyingSymbolCacher;					// Caches the flying symbol effect for W2
	private GameObjectCacher w3FlyingSymbolCacher;					// Caches the flying symbol effect for W3
	private GameObjectCacher scFlyingSymbolCacher;					// Caches the flying symbol effect for SC
	private bool isSymbolAnimating = false;							// used to control coroutines that need to wait on symbol animations
	private bool isFlyingSymbolMoving = false;						// Tracking this to know when the two tweens on the flying symbol are done
	private bool isFlyingSymbolScaling = false;						// Tracking this to know when the two tweens on the flying symbol are done

	[SerializeField] protected string alternateMutationType = "jackpot_multiplier";

	public enum WordJustificationEnum
	{
		LEFT_JUSTIFIED = 0,
		RIGHT_JUSTIFIED,
		CENTERED				// Not supported yet
	};

	public override void Awake()
	{
		for (int i = 0; i < letterAnimators.Count; i++)
		{
			currentLetterValues.Add(0);
		}

		// account for a possible multiplier override such as the one used for cumulative bonuses like in zynga04
		if (BonusGameManager.instance.betMultiplierOverride != -1)
		{
			gameMultiplier = BonusGameManager.instance.betMultiplierOverride;
		}
		else
		{
			if (GameState.giftedBonus != null)
			{
				// Make sure we only use this on a gifted spins.
				gameMultiplier = GiftedSpinsVipMultiplier.playerMultiplier;
			}
			else
			{
				gameMultiplier = SlotBaseGame.instance.multiplier;
			}
		}

		wd2FlyingSymbolCacher = new GameObjectCacher(this.gameObject, wd2FlyingSymbolPrefab);
		wd3FlyingSymbolCacher = new GameObjectCacher(this.gameObject, wd3FlyingSymbolPrefab);
		w2FlyingSymbolCacher = new GameObjectCacher(this.gameObject, w2FlyingSymbolPrefab);
		w3FlyingSymbolCacher = new GameObjectCacher(this.gameObject, w3FlyingSymbolPrefab);
		scFlyingSymbolCacher = new GameObjectCacher(this.gameObject, scFlyingSymbolPrefab);

		// if this is null, auto create it
		if (flyingSymbolPoolObject == null)
		{
			flyingSymbolPoolObject = new GameObject();
			flyingSymbolPoolObject.name = "Flying Symbol Pool";
			flyingSymbolPoolObject.transform.parent = gameObject.transform;
			flyingSymbolPoolObject.transform.localPosition = Vector3.zero;
			flyingSymbolPoolObject.transform.localScale = Vector3.one;
		}

		base.Awake();
	} 

// executePreReelsStopSpinning() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels stop spinning (after the outcome has been set)
	public override bool needsToExecutePreReelsStopSpinning()
	{
		// only need to handle pre spin for the first spin to setup the first word and letter score data
		return letterScores.Count == 0;
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		// Check for the initial WordMutation so we can get the letter score values and setup the first word
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation is WordMutation)
			{
				WordMutation wordMuataion = mutation as WordMutation;
				if (wordMuataion.letterScores.Count > 0)
				{
					letterScores = wordMuataion.letterScores;
				}

				yield return StartCoroutine(setupWordOnBar(wordMuataion.currentWord));
			}
		}
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				int letterMultiplierIndex = 0;
				int wordMultiplierIndex = 0;

				if (symbol.serverName == DOUBLE_LETTER_SYMBOL_NAME || symbol.serverName == TRIPLE_LETTER_SYMBOL_NAME)
				{
					// double letter or triple letter triggered (in zynga04 this happens on reels 2, 3, 4)
					// we need to find the mutation that goes along with this since we need to know which letter is affected
					WordMutation.LetterMultiplier letterMultiplierData = getLetterMultiplierData(reel, symbol, letterMultiplierIndex);

					if (letterMultiplierData != null)
					{
						letterMultiplierIndex++;
						yield return StartCoroutine(doLetterMultiplierEffect(reel, symbol, letterMultiplierData));
					}
					else
					{
						Debug.LogError("Couldn't find matching LetterMultiplier mutation data for symbol: " + symbol.serverName);
					}
				}
				else if (symbol.serverName == DOUBLE_WORD_SYMBOL_NAME || symbol.serverName == TRIPLE_WORD_SYMBOL_NAME)
				{
					// double word or triple word triggered (in zynga04 this happens on reel 5 only)
					WordMutation.WordMultiplier wordMultiplierData = getWordMultiplierData(reel, symbol, wordMultiplierIndex);
					if (wordMultiplierData != null)
					{
						wordMultiplierIndex++;
						yield return StartCoroutine(doWordMultiplierEffect(reel, symbol, wordMultiplierData));
					}
					else 
					{
						Debug.LogError("Couldn't find matching WordMultiplier mutation data for symbol: " + symbol.serverName);
					}
				}
				else if (symbol.serverName == PLAY_WORD_SYMBOL_NAME)
				{
					// jackpot payout triggered and a new word needs to be loaded (in zynga04 SC lands on reel 5 only)
					WordMutation payoutMutation = getPayoutWordMutation();
					
					if (payoutMutation != null)
					{
						yield return StartCoroutine(doWordPayoutEffect(reel, symbol, payoutMutation, null, customSCFlyToTransform));
					}
					else
					{
						Debug.LogError("Couldn't find payout mutation for symbol: " + symbol.serverName);
					}
				}
			}
		}
	}

// executeOnPlayAnticipationSound() section
// Functions here are executed in SpinReel where SlotEngine.playAnticipationSound is called
	/*public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return anticipationSymbols != null && anticipationSymbols.Count > 0;
	}
	
	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		if (anticipationSymbols.ContainsKey(stoppedReel + 1))
		{
			string symbol = anticipationSymbols[stoppedReel + 1];
			Audio.play(getSymbolInitSoundForSymbol(symbol));
		}

		yield break;
	}*/

	/// Handle the symbol flying up to a target location on the word bar and leaving a WD symbol in its place
	private IEnumerator doSymbolFlyUp(SlotSymbol symbol, Transform targetTransform, Vector3 finalScale)
	{
		string symbolName = symbol.serverName; //Saving this name now so we can use it after its been mutated
		isSymbolAnimating = true;

		Audio.play(getSymbolInitSoundForSymbol(symbol.serverName));

		string symbolVOSound = getSymbolVOSoundForSymbol(symbol.serverName);
		if (symbolVOSound != "")
		{
			Audio.play(symbolVOSound, 1, 0, SYMBOL_INIT_VO_DELAY);
		}

		// choreography for gen52 
		if (playAnticipationAndOutcome)
		{
			yield return StartCoroutine(symbol.playAndWaitForAnimateAnticipation());
		}

		symbol.animateOutcome(onSlotSymbolAnimDone);

		while (isSymbolAnimating)
		{
			yield return null;
		}

		GameObjectCacher flyingSymbolCacher = getCacherForFlyingSymbol(symbolName);

		GameObject flyingSymbol = flyingSymbolCacher.getInstance();
		flyingSymbol.transform.parent = flyingSymbolPoolObject.transform;
		flyingSymbol.transform.localScale = Vector3.one;
		flyingSymbol.transform.position = symbol.gameObject.transform.position;
		flyingSymbol.SetActive(true);

		// swap the symbol out with a WD symbol now that it should be covered
		symbol.mutateTo(REPLACE_SPECIAL_SYMBOL_WITH_SYMBOL_NAME, null, true, true);

		if (SYMBOL_TRAVEL_TO_TOP_SOUND != "")
		{
			Audio.play(SYMBOL_TRAVEL_TO_TOP_SOUND);
		}

		isFlyingSymbolMoving = true;
		iTween.MoveTo(flyingSymbol, iTween.Hash("position", targetTransform.position, "time", FLYING_SYMBOL_FLY_TIME, "easetype", iTween.EaseType.easeInOutQuad, "oncompletetarget", this.gameObject, "oncomplete", "onFlyingSymbolMoveComplete", "oncompleteparams", symbolName));

		isFlyingSymbolScaling = true;
		iTween.ScaleTo(flyingSymbol, iTween.Hash("scale", finalScale, "time", FLYING_SYMBOL_FLY_TIME, "islocal", true, "easetype", iTween.EaseType.linear, "oncompletetarget", this.gameObject, "oncomplete", "onFlyingSymbolScaleComplete"));

		// wait for the tweens to finish
		while (isFlyingSymbolMoving || isFlyingSymbolScaling)
		{
			yield return null;
		}

		flyingSymbol.SetActive(false);
		flyingSymbolCacher.releaseInstance(flyingSymbol);
	}

	/// Called when the flying symbol move iTween completes
	private void onFlyingSymbolMoveComplete(string symbolName)
	{
		StartCoroutine(playSparkleBurst(symbolName));

		switch (symbolName)
		{
			case PLAY_WORD_SYMBOL_NAME:
				if (scLandedAnimationList != null)
				{
					StartCoroutine(playSCLandedAnimation());
				}
			break;

			default:
				isFlyingSymbolMoving = false;
			break;
		}		
	}

	///  Called to play animations once flying SC symbols have landed  
	protected IEnumerator playSCLandedAnimation()
	{
		if (scLandedAnimationList != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(scLandedAnimationList));
		}

		isFlyingSymbolMoving = false;
	}

	/// Called when the flying symbol scale iTween completes
	private void onFlyingSymbolScaleComplete()
	{
		isFlyingSymbolScaling = false;
	}

	/// Do the payout effects, rollup the jackpot value, and then load the next word
	protected IEnumerator doWordPayoutEffect(SlotReel reel, SlotSymbol symbol, WordMutation payoutMutation, StandardMutation standardMutation = null, Transform customFlyToTransform = null)
	{
		if (customFlyToTransform == null)
		{
			yield return StartCoroutine(doSymbolFlyUp(symbol, playButtonAnimator.transform, FLYING_SC_SYMBOL_END_SCALE));
		}
		else
		{
			yield return StartCoroutine(doSymbolFlyUp(symbol, customFlyToTransform, FLYING_SC_SYMBOL_END_SCALE));
		}

		if (PLAY_WORD_APPLIED_SOUND != "")
		{
			Audio.play(PLAY_WORD_APPLIED_SOUND);
		}

		if (activatedPlayButton != null)
		{
			activatedPlayButton.SetActive(true);
		}

		if (!string.IsNullOrEmpty(PLAY_BUTTON_PLAY_ANIM_NAME))
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(playButtonAnimator, PLAY_BUTTON_PLAY_ANIM_NAME));
		}

		yield return StartCoroutine(hideOldLetters());

		if (playBarCelebrationEffectAnimator != null)
		{
			playBarCelebrationEffectAnimator.gameObject.SetActive(true);
			playBarCelebrationEffectAnimator.Play(PLAY_BAR_CELEB_EFFECT_ON_ANIM_NAME);
		}

		long currentWinnings = BonusGamePresenter.instance.currentPayout;
		if (payoutMutation != null)
		{
			BonusGamePresenter.instance.currentPayout += payoutMutation.creditsAwarded;
		}
		else
		{
			BonusGamePresenter.instance.currentPayout += standardMutation.creditsAwarded * gameMultiplier;
		}

		FreeSpinGame.instance.setRunningPayoutRollupValue(BonusGamePresenter.instance.currentPayout);
		yield return StartCoroutine(SlotUtils.rollup(	currentWinnings, 
														BonusGamePresenter.instance.currentPayout, 
														BonusSpinPanel.instance.winningsAmountLabel, 
														true, 
														2.0f,
														true,
														true,
														ROLLUP_JACKPOT_SOUND,
														ROLLUP_TERM_JACKPOT_SOUND));

		if (PLAY_BAR_CELEB_WAIT_BEFORE_HIDING != 0.0f)
		{
			yield return new TIWaitForSeconds(PLAY_BAR_CELEB_WAIT_BEFORE_HIDING);
		}

		TICoroutine playBarTransitionIntroAnimCoroutine = null;

		if (playBarTransitionAnimator != null && !string.IsNullOrEmpty(PLAY_BAR_INTRO_TRANSITION_ANIM_NAME))
		{
			if (TRANSITION_SOUND != "")
			{
				Audio.playSoundMapOrSoundKey(TRANSITION_SOUND);
			}

			if (isPlayBarIntroTransitionAnimBlocking)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(playBarTransitionAnimator, PLAY_BAR_INTRO_TRANSITION_ANIM_NAME));
			}
			else
			{
				playBarTransitionIntroAnimCoroutine = StartCoroutine(CommonAnimation.playAnimAndWait(playBarTransitionAnimator, PLAY_BAR_INTRO_TRANSITION_ANIM_NAME));
			}
		}

		if (playBarCelebrationEffectAnimator != null)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(playBarCelebrationEffectAnimator, PLAY_BAR_CELEB_EFFECT_OFF_ANIM_NAME));
			playBarCelebrationEffectAnimator.gameObject.SetActive(false);
		}


		if (payoutMutation != null)
		{
			// next word should be in this mutation, so load that in
			if (payoutMutation.nextWord != "")
			{
				// first_word and next_word appeared together
				yield return StartCoroutine(setupWordOnBar(payoutMutation.nextWord));
			}
			else
			{
				// currentWord contains the next_word
				yield return StartCoroutine(setupWordOnBar(payoutMutation.currentWord));
			}
		}

		if (standardMutation != null)
		{
			if (standardMutation.reinitializedJackpotValue != 0)
			{
				jackpotLabelText.text = CreditsEconomy.convertCredits(standardMutation.reinitializedJackpotValue * gameMultiplier);
			}
			else
			{
				if (reelGame.peekNextOutcome() != null)
				{
					foreach (JSON mutation in reelGame.peekNextOutcome().getMutations())
					{
						if (mutation.getString("type", "") == alternateMutationType)
						{
							long initialJackpotValue = mutation.getLong("jackpot_reinitialized", 0);
							if (initialJackpotValue != 0)
							{
								jackpotLabelText.text = CreditsEconomy.convertCredits(initialJackpotValue * gameMultiplier);
							}
							else
							{
								Debug.LogWarning("WordGameFreespinsModule.doWordPayoutEffect() - No initial jackpot value found in current spin's outcome or next spin's outcome");
							}
						}
					}
				}
			}
		}

		// even if we aren't blocking for the play bar intro transition, we should still make sure that anim is done before we switch to the outro
		if (!isPlayBarIntroTransitionAnimBlocking && playBarTransitionIntroAnimCoroutine != null)
		{
			yield return playBarTransitionIntroAnimCoroutine;
		}

		if (playBarTransitionAnimator != null && !string.IsNullOrEmpty(PLAY_BAR_OUTRO_TRANSITION_ANIM_NAME))
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(playBarTransitionAnimator, PLAY_BAR_OUTRO_TRANSITION_ANIM_NAME));
		}

		if (activatedPlayButton != null)
		{
			activatedPlayButton.SetActive(false);
		}
	}

	/// Do the effects and change the value of the word jackpot
	protected IEnumerator doWordMultiplierEffect(SlotReel reel, SlotSymbol symbol, WordMutation.WordMultiplier wordMultiplierData, StandardMutation standardMutationData = null)
	{
		string symbolServerName = symbol.serverName;

		yield return StartCoroutine(doSymbolFlyUp(symbol, barEffectAnimator.transform, FLYING_WORD_SYMBOL_END_SCALE));

		// update the code values of the letters but not the UI yet, we need the data to calculate the new jackpot amount
		if (wordMultiplierData != null)
		{
			applyMultiplierToAllLetterValues(wordMultiplierData.multiplier);
		}
		else
		{
			applyMultiplierToAllLetterValues(standardMutationData.creditsMultiplier);
		}

		StartCoroutine(updateJackportBar(symbolServerName));

		yield return StartCoroutine(updateAllLetterValuesWithEffects(symbolServerName));

		if (JACKPOT_INCREMENT_SOUND != "")
		{
			Audio.play(JACKPOT_INCREMENT_SOUND);
		}
	}

	/// Do the effects and change the value of a letter on the top and update the jackpot amount
	private IEnumerator doLetterMultiplierEffect(SlotReel reel, SlotSymbol symbol, WordMutation.LetterMultiplier letterMultiplierData)
	{
		string symbolServerName = symbol.serverName;

		// take into account that the letters may not start at the first slot on the left
		int adjustedLetterIndex = letterMultiplierData.letterIndex + getLetterOffset();

		yield return StartCoroutine(doSymbolFlyUp(symbol, letterTileCenters[adjustedLetterIndex], FLYING_LETTER_SYMBOL_END_SCALE));

		string multiplierSound = getWordMultiplierSound(symbolServerName);

		if (multiplierSound != "")
		{
			Audio.play(multiplierSound);
		}

		// update the code values of the letters but not the UI yet, we need the data to calculate the new jackpot amount
		currentLetterValues[adjustedLetterIndex] *= letterMultiplierData.multiplier;
		StartCoroutine(updateJackportBar(symbolServerName));

		yield return StartCoroutine(updateLetterValueWithEffect(adjustedLetterIndex, symbolServerName));
	}

	/// Update the letter value while playing an effect
	private IEnumerator updateLetterValueWithEffect(int letterIndex, string symbolServerName)
	{
		letterEffectsAnimators[letterIndex].Play(getLetterEffectAnimNameForSymbol(symbolServerName));

		if (UPDATE_LETTER_VALUE_DURING_EFFECT_DELAY_TIME != 0)
		{
			yield return new TIWaitForSeconds(UPDATE_LETTER_VALUE_DURING_EFFECT_DELAY_TIME);
		}

		letterValueTextsWrapper[letterIndex].text = CreditsEconomy.convertCredits(currentLetterValues[letterIndex]);
	}

	/// Plays the jackpot bar animation and updates the value
	private IEnumerator updateJackportBar(string symbolServerName)
	{
		barEffectAnimator.Play(getBarEffectAnimNameForSymbol(symbolServerName));

		if (UPDATE_JACKPOT_BAR_DURING_EFFECT_DELAY_TIME != 0)
		{
			yield return new TIWaitForSeconds(UPDATE_JACKPOT_BAR_DURING_EFFECT_DELAY_TIME);
		}

		// increase the jackpot with the value added to the letter
		jackpotLabelText.text = CreditsEconomy.convertCredits(calculateCurrentJackpotValue());
	}

	/// Find the mutation that is paying out, will match up with the SC symbol in zynga04
	private WordMutation getPayoutWordMutation()
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation is WordMutation)
			{
				WordMutation wordMutation = mutation as WordMutation;
				if (wordMutation.creditsAwarded != 0)
				{
					return wordMutation;
				}
			}
		}

		return null;
	}

	/// Get the mutation data for the word multiplier so we know how to trigger the word multiplier
	private WordMutation.WordMultiplier getWordMultiplierData(SlotReel reel, SlotSymbol symbol, int wordMultiplierIndex)
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation is WordMutation)
			{
				WordMutation wordMutation = mutation as WordMutation;
				if (wordMutation.wordMultipliers.Count > 0)
				{
					int numWordMultipliersChecked = 0;
					foreach (WordMutation.WordMultiplier wordMulti in wordMutation.wordMultipliers)
					{
						if (wordMulti.reelID == reel.reelID)
						{
							if (numWordMultipliersChecked == wordMultiplierIndex)
							{
								return wordMulti;
							}
							else
							{
								numWordMultipliersChecked++;
							}
						}
					}
				}
			}
		}

		return null;
	}

	/// Get the mutation data for the letter multiplier so we know what letter is going to be affected by the multiplication
	private WordMutation.LetterMultiplier getLetterMultiplierData(SlotReel reel, SlotSymbol symbol, int letterMultiplierIndex)
	{
		foreach (MutationBase mutation in reelGame.mutationManager.mutations)
		{
			if (mutation is WordMutation)
			{
				WordMutation wordMutation = mutation as WordMutation;
				if (wordMutation.letterMultipliers.Count > 0)
				{
					int numLetterMultipliersChecked = 0;
					foreach (WordMutation.LetterMultiplier letterMulti in wordMutation.letterMultipliers)
					{
						if (letterMulti.reelID == reel.reelID && getMultiplierValueForSymbol(symbol.serverName) == letterMulti.multiplier)
						{
							if (numLetterMultipliersChecked == letterMultiplierIndex)
							{
								return letterMulti;
							}
							else
							{
								// there was more than one of this mutation, so we need to check more
								numLetterMultipliersChecked++;
							}
						}
					}
				}
			}
		}

		return null;
	}

	/// Return the cacher for the flying symbol effect of the passed in symbol
	private GameObjectCacher getCacherForFlyingSymbol(string serverName)
	{
		if (serverName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return wd2FlyingSymbolCacher;
		}
		else if (serverName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return wd3FlyingSymbolCacher;
		}
		else if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return w2FlyingSymbolCacher;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return w3FlyingSymbolCacher;
		}
		else if (serverName == PLAY_WORD_SYMBOL_NAME)
		{
			return scFlyingSymbolCacher;
		}
		else
		{
			Debug.LogWarning("getCacherForFlyingSymbol() - serverName = " + serverName + " doesn't have an effect defined!");
			return null;
		}
	}

	/// Get the animation name to play for the letter effect based on the symbol that was hit
	private string getLetterEffectAnimNameForSymbol(string serverName)
	{
		if (serverName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return LETTER_EFFECT_DL_ANIM_NAME;
		}
		else if (serverName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return LETTER_EFFECT_TL_ANIM_NAME;
		}
		else if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return LETTER_EFFECT_DW_ANIM_NAME;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return LETTER_EFFECT_TW_ANIM_NAME;
		}
		else
		{
			Debug.LogWarning("getLetterEffectAnimNameForSymbol() - serverName = " + serverName + " doesn't have an effect defined!");
			return "";
		}
	}

	/// Get the bar effect animation name using the symbol name
	private string getBarEffectAnimNameForSymbol(string serverName)
	{
		if (serverName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return BAR_EFFECT_DL_ANIM_NAME;
		}
		else if (serverName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return BAR_EFFECT_TL_ANIM_NAME;
		}
		else if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return BAR_EFFECT_DW_ANIM_NAME;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return BAR_EFFECT_TW_ANIM_NAME;
		}
		else
		{
			Debug.LogWarning("getBarEffectAnimNameForSymbol() - serverName = " + serverName + " doesn't have an effect defined!");
			return "";
		}
	}

	/// Handle getting the right init sound for the anticipation of a special symbol in this game
	private string getSymbolInitSoundForSymbol(string serverName)
	{
		if (serverName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return DOUBLE_LETTER_SYMBOL_INIT_SOUND;
		}
		else if (serverName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return TRIPLE_LETTER_SYMBOL_INIT_SOUND;
		}
		else if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return DOUBLE_WORD_SYMBOL_INIT_SOUND;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return TRIPLE_WORD_SYMBOL_INIT_SOUND;
		}
		else if (serverName == PLAY_WORD_SYMBOL_NAME)
		{
			return PLAY_SYMBOL_INIT_SOUND;
		}
		else
		{
			Debug.LogWarning("getSymbolInitSoundForSymbol() - serverName = " + serverName + " doesn't have anything defined!");
			return "";
		}
	}

	/// Get the VO for the symbols, should be played just a little after the init stound
	private string getSymbolVOSoundForSymbol(string serverName)
	{
		if (serverName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return DOUBLE_LETTER_SYMBOL_VO_SOUND;
		}
		else if (serverName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return TRIPLE_LETTER_SYMBOL_VO_SOUND;
		}
		else if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return DOUBLE_WORD_SYMBOL_VO_SOUND;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return TRIPLE_WORD_SYMBOL_VO_SOUND;
		}
		else if (serverName == PLAY_WORD_SYMBOL_NAME)
		{
			// no VO for the play symbol right now
			return PLAY_SYMBOL_VO_SOUND;
		}
		else
		{
			Debug.LogWarning("getSymbolInitSoundForSymbol() - serverName = " + serverName + " doesn't have anything defined!");
			return "";
		}
	}

	/// Get the target multiplier value for the passed symbol name, used to match a symbol with its mutation
	protected virtual long getMultiplierValueForSymbol(string symbolServerName)
	{
		if (symbolServerName == DOUBLE_LETTER_SYMBOL_NAME)
		{
			return 2;
		}
		else if (symbolServerName == TRIPLE_LETTER_SYMBOL_NAME)
		{
			return 3;
		}
		else if (symbolServerName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return 2;
		}
		else if (symbolServerName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return 3;
		}
		else
		{
			return 0;
		}
	}

	private GameObject getSparkleBurstObject(string serverName)
	{
		if (serverName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return doubleWordSparkleBurstEffect;
		}
		else if (serverName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return tripleWordSparkleBurstEffect;
		}
		else
		{
			// It's very possible that not everything uses a sparkle burst when it gets to it's object, and warning on this is just needless spam.
			//Debug.Log("getSparkleBurstObject() - serverName = " + serverName + " doesn't have anything defined!");
			return null;
		}
	}

	/// Get the offset to the start of the letters, affected by how the word is justified in the UI
	private int getLetterOffset()
	{
		switch (wordJustification)
		{
			case WordJustificationEnum.LEFT_JUSTIFIED:
				return 0;

			case WordJustificationEnum.RIGHT_JUSTIFIED:
				if (currentWord != null && currentWord != "")
				{
					return letterAnimators.Count - currentWord.Length;
				}
				else
				{
					return -1;
				}

			case WordJustificationEnum.CENTERED:
				Debug.LogError("Not yet supported!  Defaulting to LEFT_JUSTIFIED!");
				wordJustification = WordJustificationEnum.LEFT_JUSTIFIED;
				return 0;

			default:
				return 0;
		}
	}

	/// Apply multiplier to all letter values, doesn't update the UI though, that will be done when applyMultiplierToAllLettersInUI() is called
	private void applyMultiplierToAllLetterValues(long multiplier)
	{
		int letterOffset = getLetterOffset();
		if (letterOffset >= 0)
		{
			for (int i = letterOffset; i < currentLetterValues.Count; i++)
			{
				if (i - letterOffset < currentWord.Length)
				{
					currentLetterValues[i] *= multiplier;
				}
			}
		}
		else
		{
			Debug.LogError("Mutation word letter count: " + currentWord.Length + " was smaller than letter slots: " + letterAnimators.Count + " destroying module!");
			Destroy(this);
		}
	}

	/// Calculate the current jackpot value using the letter values and the currentWordMultiplier
	private IEnumerator updateAllLetterValuesWithEffects(string symbolServerName)
	{
		int letterOffset = getLetterOffset();
		if (letterOffset >= 0)
		{
			string multiplierSound = getWordMultiplierSound(symbolServerName);

			if (multiplierSound != "")
			{
				Audio.play(multiplierSound);
			}

			for (int i = letterOffset; i < currentLetterValues.Count; i++)
			{
				if (i - letterOffset < currentWord.Length)
				{
					numLetterAnimsGoing++;
					StartCoroutine(playWordMultiplierEffectOnLetter(i, symbolServerName));
				}
			}

			// wait for all the letters to be multiplied out
			while (numLetterAnimsGoing > 0)
			{
				yield return null;
			}
		}
		else
		{
			Debug.LogError("Mutation word letter count: " + currentWord.Length + " was smaller than letter slots: " + letterAnimators.Count + " destroying module!");
			Destroy(this);
		}
	}

	/// Play the full word multiplier effect for the letter, will play all at the same time with the other letters
	private IEnumerator playWordMultiplierEffectOnLetter(int dataIndex, string symbolServerName)
	{
		yield return StartCoroutine(updateLetterValueWithEffect(dataIndex, symbolServerName));
		numLetterAnimsGoing--;
	}

	/// Get the sound for a multiplier being applied to a symbol
	private string getWordMultiplierSound(string symbolServerName)
	{
		if (symbolServerName == DOUBLE_LETTER_SYMBOL_NAME || symbolServerName == DOUBLE_WORD_SYMBOL_NAME)
		{
			return DOUBLE_LETTER_APPLIED_SOUND;
		}
		else if (symbolServerName == TRIPLE_LETTER_SYMBOL_NAME || symbolServerName == TRIPLE_WORD_SYMBOL_NAME)
		{
			return TRIPLE_LETTER_APPLIED_SOUND;
		}
		else
		{
			return "";
		}
	}

	/// Calculate the current jackpot value using the letter values and the currentWordMultiplier
	protected virtual long calculateCurrentJackpotValue()
	{
		long jackpotValue = 0;

		int letterOffset = getLetterOffset();
		if (letterOffset >= 0)
		{
			for (int i = letterOffset; i < currentLetterValues.Count; i++)
			{
				jackpotValue += currentLetterValues[i];
			}
		}
		else
		{
			Debug.LogError("Mutation word letter count: " + currentWord.Length + " was smaller than letter slots: " + letterAnimators.Count + " destroying module!");
			Destroy(this);
		}
		return jackpotValue;
	}

	/// Hide the old letters
	private IEnumerator hideOldLetters()
	{
		for (int i = 0; i < letterAnimators.Count; ++i)
		{
			letterAnimators[i].Play(LETTER_OFF_ANIM_NAME);
			currentLetterValues[i] = 0;
			yield return new TIWaitForSeconds(DELAY_BETWEEN_REMOVE_OLD_LETTERS);
		}
	}

	/// Handle setting up a new word on the word bar
	private IEnumerator setupWordOnBar(string currentWord)
	{
		this.currentWord = currentWord;

		if (!string.IsNullOrEmpty(PLAY_BUTTON_OFF_ANIM_NAME))
		{
			playButtonAnimator.Play(PLAY_BUTTON_OFF_ANIM_NAME);
		}

		// Clear the previous jackpot value
		jackpotLabelText.text = "";

		// determine the letter offset, going to assume for now that words are right justified on the letter slots
		int letterOffset = getLetterOffset();

		if (NEW_TILE_LOAD_SOUND != "")
		{
			Audio.play(NEW_TILE_LOAD_SOUND);
		}

		if (letterOffset >= 0)
		{
			for (int i = letterOffset; i < letterAnimators.Count; i++)
			{
				// since this supports both left and right justification, better make sure we skip over the last slots if they aren't used
				if (i - letterOffset < currentWord.Length)
				{
					string letter = currentWord[i - letterOffset].ToString();
					letterLabelTextsWrapper[i].text = letter;
					if (letterScores.ContainsKey(letter))
					{
						long letterValue = letterScores[letter] * gameMultiplier;
						currentLetterValues[i] = letterValue;
						letterValueTextsWrapper[i].text = CreditsEconomy.convertCredits(letterValue);
					}

					letterAnimators[i].Play(LETTER_ON_ANIM_NAME);

					yield return new TIWaitForSeconds(DELAY_BETWEEN_NEW_LETTERS);
				}
			}
		}
		else
		{
			Debug.LogError("Mutation word letter count: " + currentWord.Length + " was smaller than letter slots: " + letterAnimators.Count + " destroying module!");
			Destroy(this);
		}

		if (SHOW_JACKPOT_VALUE_SOUND != "")
		{
			Audio.play(SHOW_JACKPOT_VALUE_SOUND);
		}
		jackpotLabelText.text = CreditsEconomy.convertCredits(calculateCurrentJackpotValue());
	}

	/// General callback for symbol animations, need this to control flow in coroutines
	private void onSlotSymbolAnimDone(SlotSymbol symbol)
	{
		isSymbolAnimating = false;
	}

	private IEnumerator playSparkleBurst(string symbolName)
	{
		GameObject sparkleBurst = getSparkleBurstObject(symbolName);
		if (sparkleBurst != null)
		{
			sparkleBurst.SetActive(true);
			yield return new TIWaitForSeconds(SPARKLE_BURST_WAIT_BEFORE_HIDING);
			sparkleBurst.SetActive(false);
		}
	}
}

