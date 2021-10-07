using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the SATC wheel bonus game.
*/

public class SATCChallengeWheel : ChallengeGame
{
	private enum CharacterEnum
	{
		None = -1,
		Miranda = 0,
		Charlotte,
		Samantha,
		Carrie,
		MrBig
	};

	/* Wheel Game Items */
	private const int NUM_SLICES = 10;							// Number of slices on the wheel
	private const float DEGREES_PER_SLICE = 360 / NUM_SLICES;	// The number of degrees per slice on the wheel

	public GameObject wheel;
	public UILabel[] wheelTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelTextsWrapperComponent;

	public List<LabelWrapper> wheelTextsWrapper
	{
		get
		{
			if (_wheelTextsWrapper == null)
			{
				_wheelTextsWrapper = new List<LabelWrapper>();

				if (wheelTextsWrapperComponent != null && wheelTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelTextsWrapperComponent)
					{
						_wheelTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelTexts)
					{
						_wheelTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelTextsWrapper;
		}
	}
	private List<LabelWrapper> _wheelTextsWrapper = null;	
	
	public GameObject spinButton;
	public UILabel winText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winTextWrapperComponent;

	public LabelWrapper winTextWrapper
	{
		get
		{
			if (_winTextWrapper == null)
			{
				if (winTextWrapperComponent != null)
				{
					_winTextWrapper = winTextWrapperComponent.labelWrapper;
				}
				else
				{
					_winTextWrapper = new LabelWrapper(winText);
				}
			}
			return _winTextWrapper;
		}
	}
	private LabelWrapper _winTextWrapper = null;
	
	public GameObject winBox;
	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	public UILabel[] progressivePools;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] progressivePoolsWrapperComponent;

	public List<LabelWrapper> progressivePoolsWrapper
	{
		get
		{
			if (_progressivePoolsWrapper == null)
			{
				_progressivePoolsWrapper = new List<LabelWrapper>();

				if (progressivePoolsWrapperComponent != null && progressivePoolsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in progressivePoolsWrapperComponent)
					{
						_progressivePoolsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in progressivePools)
					{
						_progressivePoolsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _progressivePoolsWrapper;
		}
	}
	private List<LabelWrapper> _progressivePoolsWrapper = null;	
	
	public GameObject[] progressivePoolEffects;
	public GameObject wheelGameParent;
	public GameObject wheelStartAnimationPrefab;
	public GameObject wheelWinBoxAnimation;
	public UISprite spinBoxWhiteTexture;
	public GameObject winSliceAnimation;
	public GameObject topPointerAnimation;
	public UISprite wheelSprite; 					// Used to get the size for the swipeToSpin Feature.
	
	/* Pickem Game Items */
	private static readonly string[] CHARACTER_ICONS = { "miranda_icon_m", "charlotte_icon_m", "samantha_icon_m", "carrie_icon_m", "MrBig_icon_m" };
	private static readonly string[] PROGRESSIVE_ITEM_ARRAY = { "miranda_item_m", "charlotte_item_m", "samantha_item_m", "carrie_item_m", "mrBig_item_m" };
	private const int TOTAL_NUM_TRIES = 3; // The number of tries the player gets during the pickem part of this game

	public GameObject pickemParent;
	public Animation progressiveRoot;
	public UISprite progressiveIcon;
	public UISprite[] progressiveItemIcons;
	public UILabel progressiveWinLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent progressiveWinLabelWrapperComponent;

	public LabelWrapper progressiveWinLabelWrapper
	{
		get
		{
			if (_progressiveWinLabelWrapper == null)
			{
				if (progressiveWinLabelWrapperComponent != null)
				{
					_progressiveWinLabelWrapper = progressiveWinLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_progressiveWinLabelWrapper = new LabelWrapper(progressiveWinLabel);
				}
			}
			return _progressiveWinLabelWrapper;
		}
	}
	private LabelWrapper _progressiveWinLabelWrapper = null;
	
	public UILabel progressiveAmountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent progressiveAmountLabelWrapperComponent;

	public LabelWrapper progressiveAmountLabelWrapper
	{
		get
		{
			if (_progressiveAmountLabelWrapper == null)
			{
				if (progressiveAmountLabelWrapperComponent != null)
				{
					_progressiveAmountLabelWrapper = progressiveAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_progressiveAmountLabelWrapper = new LabelWrapper(progressiveAmountLabel);
				}
			}
			return _progressiveAmountLabelWrapper;
		}
	}
	private LabelWrapper _progressiveAmountLabelWrapper = null;
	
	public UILabel picksRemainingLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent picksRemainingLabelWrapperComponent;

	public LabelWrapper picksRemainingLabelWrapper
	{
		get
		{
			if (_picksRemainingLabelWrapper == null)
			{
				if (picksRemainingLabelWrapperComponent != null)
				{
					_picksRemainingLabelWrapper = picksRemainingLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_picksRemainingLabelWrapper = new LabelWrapper(picksRemainingLabel);
				}
			}
			return _picksRemainingLabelWrapper;
		}
	}
	private LabelWrapper _picksRemainingLabelWrapper = null;
	
	public GameObject[] picks;
	public UILabel[] reveals;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealsWrapperComponent;

	public List<LabelWrapper> revealsWrapper
	{
		get
		{
			if (_revealsWrapper == null)
			{
				_revealsWrapper = new List<LabelWrapper>();

				if (revealsWrapperComponent != null && revealsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealsWrapperComponent)
					{
						_revealsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in reveals)
					{
						_revealsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealsWrapper;
		}
	}
	private List<LabelWrapper> _revealsWrapper = null;	
	
	public UISprite[] characterIconReveals;				// Icons for characters that can be turned on when the user finds a character pick
	public GameObject progressiveWinAnimation;
	public GameObject pickemWinBoxAnimation;

	private int numPicksMade = 0;						// The number of picks that the user has made up to TOTAL_NUM_TRIES
	
	private WheelOutcome wheelOutcome;					// Outcome data sent from the server
	private PickemOutcome pickemOutcome = null;			// Pick outcome extracted from the outcome from the server
	private WheelSpinner wheelSpinner;					// Visual controller for the wheel spinning					
	private WheelPick wheelPick;						// A wheel event taken from the WheelOutcome
	
	private CharacterEnum selectedCharacter = CharacterEnum.None;	// The character which the pointer on the wheel selected

	private long[] progPool = new long[5];						// Stores out the win values of the progressives
	private int lastRnd = -1;									// Tracks what the last random object to wiggle was so that we don't wiggle the same one twice

	private const float MIN_TIME_BETWEEN_SHAKES = 2.0f;			// Minimum time between shakes
	private const float MAX_TIME_BETWEEN_SHAKES = 3.0f;			// Maximum time between shakes
	private float shakeTimer = 0;				// Used to track tha time till a shake is next played
	private bool isRevealingPicks = false;		// Tracks if this pick is revealing, in which case shaking should be stopped
	private List<GameObject> unrevealedPicks = new List<GameObject>();	// The list of picks which haven't yet been revealed, used to determine what to shake

	private bool pickClickedLock = false;	// Controls when the user 

	private static readonly string[] BASE_PAYOUT_PAYTABLES = {
		"satc_100_common_pickem_1",
		"satc_100_common_pickem_2",
		"satc_100_common_pickem_3",
		"satc_100_common_pickem_4",
		"satc_100_common_pickem_5" };

	public override void init() 
	{
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		
		wheelPick = wheelOutcome.getNextEntry();
		
		int wheelIdx = 0;
		for (int j = 0; j < wheelPick.wins.Count; j++)
		{
			long credits = wheelPick.wins[j].credits;
			if (credits == 0)
			{
				continue;
			}
			wheelTextsWrapper[wheelIdx].text = CommonText.makeVertical(CreditsEconomy.convertCredits(credits, false));
			wheelIdx++;
		}

		winBox.SetActive(false);
		wheelWinBoxAnimation.SetActive(false);
		
		JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.

		for (int i = 0; i < progPool.Length; i++)
		{
			if(progressivePoolsJSON != null && progressivePoolsJSON.Length > 0)
			{
				progPool[i] = SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[i].getString("key_name", ""), SlotBaseGame.instance.multiplier, false);
			}
			else
			{
				progPool[i] = BonusGamePaytable.getBasePayoutCreditsForPaytable("pickem", BASE_PAYOUT_PAYTABLES[i], "1") * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier;
			}
			progressivePoolsWrapper[i].text = CreditsEconomy.convertCredits(progPool[i]);

		}
		
		for (int i = 0; i < revealsWrapper.Count; i++)
		{
			revealsWrapper[i].alpha = 0;
		}

		// copy out all of hte picks into unreaveledPicks
		for (int i = 0; i < picks.Length; i++)
		{
			unrevealedPicks.Add(picks[i]);
		}
		
		FadeIn();
		
		Audio.switchMusicKey(Audio.soundMap("bonus_idle_bg"));
		Audio.stopMusic();

		shakeTimer = Random.Range(MIN_TIME_BETWEEN_SHAKES, MAX_TIME_BETWEEN_SHAKES);

		_didInit = true;
	}

	protected override void startGame()
	{
		base.startGame();
		showSpinButton(); // Needs to go in the Start functuion for Swipe to spin to work b/c camera may not be set in awake.
	}
	
	private void FadeIn()
	{
		 iTween.ValueTo(spinBoxWhiteTexture.gameObject, iTween.Hash("from", 0.3f, 
												"to", 0.0f,
            									"time", 0.5f, 
												"easetype", "linear",
            									"onupdate", "setAlpha",
            									"onupdatetarget", gameObject,
												"oncomplete", "FadeOut",
												"oncompletetarget", gameObject));
	}
	
	private void FadeOut()
	{
		 iTween.ValueTo(spinBoxWhiteTexture.gameObject, iTween.Hash("from", 0f, 
														"to", 0.3f,
            											"time", 0.5f, 
														"easetype", "linear",
            											"onupdate", "setAlpha",
            											"onupdatetarget", gameObject,
														"oncomplete", "FadeIn",
														"oncompletetarget", gameObject));
	}
	
	private void setAlpha(float alpha)
	{
		spinBoxWhiteTexture.alpha = alpha;
	}
	
	public void showSpinButton()
	{
		spinButton.SetActive(true);
		enableSwipeToSpin();
	}
	
	protected override void Update()
	{
		base.Update();

		if (!_didInit)
		{
			return;
		}

		if (pickemParent.activeInHierarchy && !isRevealingPicks)
		{
			shakeTimer -= Time.deltaTime;

			if (shakeTimer <= 0)
			{
				int rnd = Random.Range(0, unrevealedPicks.Count - 1);

				// ensure the same one doesn't happen twice in a row
				if(rnd == lastRnd)
				{
					rnd += 2;
					rnd = rnd % unrevealedPicks.Count;
				}

				iTween.ShakeRotation(unrevealedPicks[rnd], iTween.Hash("amount", new Vector3(0, 0, 5), "time", 0.5f));
				lastRnd = rnd;

				// get a new time till next shake
				shakeTimer = Random.Range(MIN_TIME_BETWEEN_SHAKES, MAX_TIME_BETWEEN_SHAKES);
			}
		}
		
		if (wheelSpinner != null)
		{
			wheelSpinner.updateWheel();
		}
	}
	
	public void onWheelSpinComplete()
	{
		StartCoroutine(rollupAndEnd());
	}
	
	// When rollup ends if the outcome is payout show payout
	// Otherwise start the pickem game
	private IEnumerator rollupAndEnd()
	{
		winSliceAnimation.SetActive(true);
		yield return new TIWaitForSeconds(2.0f);
		winSliceAnimation.SetActive(false);
		long _payout = wheelPick.wins[wheelPick.winIndex].credits;
		if (_payout > 0)
		{
			wheelWinBoxAnimation.SetActive(true);
			NGUITools.SetActive(winLabelWrapper.gameObject, true);
			NGUITools.SetActive(winTextWrapper.gameObject, true);
			BonusGamePresenter.instance.currentPayout = _payout;
			winTextWrapper.gameObject.SetActive(true);
			yield return StartCoroutine(SlotUtils.rollup(0, BonusGamePresenter.instance.currentPayout, winTextWrapper));
			
			yield return new WaitForSeconds(0.5f);
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame);
			pickemOutcome = new PickemOutcome(pickemGame);
			
			// the mapping between win index and progressive array index is as follows
			// miranda   1 -> 0
			// mr big    3 -> 4
			// carrie    5 -> 3
			// samantha  7 -> 2
			// charlotte 9 -> 1
			
			Audio.play("BonusTaDaSATC");
			switch (wheelPick.winIndex)
			{
				case 1:
					selectedCharacter = CharacterEnum.Miranda;
					Audio.play("WheelBonusCharacterMiranda1", 1, 0, 0.5f);
					break;
				case 3:
					selectedCharacter = CharacterEnum.MrBig;
					Audio.play("WheelBonusCharacterBig1", 1, 0, 0.5f);
					break;
				case 5:
					selectedCharacter = CharacterEnum.Carrie;
					Audio.play("WheelBonusCharacterCarrie1", 1, 0, 0.5f);
					break;
				case 7:
					selectedCharacter = CharacterEnum.Samantha;
					Audio.play("WheelBonusCharacterSam1", 1, 0, 0.5f);
					break;
				case 9:
					selectedCharacter = CharacterEnum.Charlotte;
					Audio.play("WheelBonusCharacterCharlotte1", 1, 0, 0.5f);
					break;
			}
			CommonGameObject.parentsFirstSetActive(progressivePoolEffects[(int)selectedCharacter].gameObject, true);
			yield return new WaitForSeconds(1.0f);
			showPickemGame();
		}
	}
	
	private void showPickemGame()
	{
		topPointerAnimation.SetActive(false);
		BonusGamePresenter.instance.useMultiplier = false;
		pickemParent.SetActive(true);
		wheelWinBoxAnimation.SetActive(false);
		foreach (Transform t in progressivePoolsWrapper[(int)selectedCharacter].transform)
		{
			t.gameObject.SetActive(false);
		}
		
		
		progressiveIcon.spriteName = CHARACTER_ICONS[(int)selectedCharacter];
		for (int i = 0; i < progressiveItemIcons.Length; i++)
		{
			progressiveItemIcons[i].spriteName = PROGRESSIVE_ITEM_ARRAY[(int)selectedCharacter];
		}
		progressiveAmountLabelWrapper.text = progressivePoolsWrapper[(int)selectedCharacter].text;
		
		picksRemainingLabelWrapper.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);
		progressiveWinLabelWrapper.text = CreditsEconomy.convertCredits(0);
		
		Audio.switchMusicKey("BonusBgPt2SATC");
		Audio.stopMusic();
	}

	public void onPickSelectedScaleAnimComplete(GameObject button)
	{
		StartCoroutine(revealItem(button));
	}
	
	public IEnumerator revealItem(GameObject button)
	{
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		NGUITools.SetActive(button, false);
		button.GetComponent<Collider>().enabled = false;
		int index = System.Array.IndexOf(picks, button);

		// pick is being revealed so remove it
		unrevealedPicks.Remove(picks[index]);
		
		PickemPick _pick = pickemOutcome.getNextEntry();
		pickemWinBoxAnimation.SetActive(true);

		UISprite ut = characterIconReveals[index];
		if (_pick.groupId.Length > 0)
		{
			progressiveRoot.Play("wheel_pick_progressive_win");
			progressiveWinAnimation.SetActive(true);
			//NGUITools.SetActive(button, true);
			revealsWrapper[index].alpha = 1;
			revealsWrapper[index].text = "";
			BonusGamePresenter.instance.currentPayout += progPool[(int)selectedCharacter];
			
			if (ut)
			{
				ut.gameObject.SetActive(true);
				ut.spriteName = CHARACTER_ICONS[(int)selectedCharacter];
				ut.MarkAsChanged();
				ut.MakePixelPerfect();
			}

			picksRemainingLabelWrapper.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);

			// ensure that a pick click doesn't cause the rollup to skip
			yield return null;
			
			yield return StartCoroutine(SlotUtils.rollup(
				initialCredits,
				BonusGamePresenter.instance.currentPayout,
				progressiveWinLabelWrapper,
				new RollupDelegate(ProgressiveRollUpCallback)
			));
		}
		else
		{
			revealsWrapper[index].text = CreditsEconomy.convertCredits(_pick.credits * BonusGameManager.instance.currentMultiplier);
			revealsWrapper[index].alpha = 1;
			BonusGamePresenter.instance.currentPayout  += _pick.credits * BonusGameManager.instance.currentMultiplier;

			picksRemainingLabelWrapper.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);

			// ensure that a pick click doesn't cause the rollup to skip
			yield return null;
			
			// wait for the rollup
			yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, progressiveWinLabelWrapper));

			if (numPicksMade >= TOTAL_NUM_TRIES)
			{
				yield return StartCoroutine(revealRemainingPicks());
			}
		}

		pickClickedLock = false;
	}

	/**
	Handle the reveal for the picking part of this game
	*/
	private IEnumerator pickemRevealItem(PickemPick _pick, int index)
	{
		long initialCredits = BonusGamePresenter.instance.currentPayout;

		revealsWrapper[index].text = CreditsEconomy.convertCredits(_pick.credits * BonusGameManager.instance.currentMultiplier);
		revealsWrapper[index].alpha = 1;
		BonusGamePresenter.instance.currentPayout  += _pick.credits * BonusGameManager.instance.currentMultiplier;

		picksRemainingLabelWrapper.text = Localize.textUpper("{0}_picks_remaining", pickemOutcome.entryCount);

		// ensure that a pick click doesn't cause the rollup to skip
		yield return null;
		
		// wait for the rollup
		yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, progressiveWinLabelWrapper));

		if (numPicksMade >= TOTAL_NUM_TRIES)
		{
			StartCoroutine(revealRemainingPicks());
		}	
	}
	
	/**
	Rollup part of finding a character portrait during pickem
	*/
	private void ProgressiveRollUpCallback(long rollupValue)
	{
		if (rollupValue >= BonusGamePresenter.instance.currentPayout)
		{
			progressiveRoot.Play("wheel_pick_progressive_win_return");
			progressiveWinAnimation.SetActive(false);
			
			if (numPicksMade >= TOTAL_NUM_TRIES)
			{
				for (int i = 0; i < progressiveItemIcons.Length; i++)
				{
					progressiveItemIcons[i].gameObject.GetComponent<Collider>().enabled = false;
				}

				StartCoroutine(revealRemainingPicks());
			}
		}
	}
	
	/**
	Called when a pickem object is clicked on
	*/
	public void OnPickSelected(GameObject button)
	{
		if (!pickClickedLock)
		{
			pickClickedLock = true;
			numPicksMade++;

			if (numPicksMade >= TOTAL_NUM_TRIES)
			{
				for (int i = 0; i < progressiveItemIcons.Length; i++)
				{
					progressiveItemIcons[i].gameObject.GetComponent<Collider>().enabled = false;
				}
			}

			if (button.GetComponent<Collider>() != null)
			{
				button.GetComponent<Collider>().enabled = false;
			}
			
			Audio.play("BonusRegisterSATC01");

			iTween.ScaleBy(button, iTween.Hash("amount", new Vector3(0.01f, 0.01f, 0.01f),
												"time", 0.5f,
												"oncomplete", "onPickSelectedScaleAnimComplete",
												"oncompletetarget", gameObject,
												"oncompleteparams", button));
		}
	}
	
	/**
	Reveal the remaining objects that weren't picked
	*/
	private IEnumerator revealRemainingPicks()
	{
		// wait just a bit before starting the reveals
		yield return new TIWaitForSeconds(2.5f);

		isRevealingPicks = true;

		PickemPick _reveal = pickemOutcome.getNextReveal();

		while(_reveal != null)
		{
			// find a slot to display the reveal in
			for (int i = 0; i < revealsWrapper.Count; i++)
			{
				if (revealsWrapper[i].alpha == 0)
				{
					if (_reveal.groupId.Length > 0)
					{
						NGUITools.SetActive(revealsWrapper[i].transform.parent.gameObject, true);
						revealsWrapper[i].alpha = 1;
						revealsWrapper[i].text = "";
						revealsWrapper[i].color = Color.gray;

						picks[i].SetActive(false);
						UISprite ut = characterIconReveals[i];
						if (ut)
						{
							ut.gameObject.SetActive(true);
							ut.spriteName = CHARACTER_ICONS[(int)selectedCharacter];
							ut.color = Color.gray;
							ut.MarkAsChanged();
							ut.MakePixelPerfect();
						}
					}
					else
					{
						UISprite ut = revealsWrapper[i].transform.parent.GetComponentInChildren<UISprite>();
						if (ut)
						{
							ut.enabled = false;
						}
						revealsWrapper[i].text = CreditsEconomy.convertCredits(_reveal.credits * BonusGameManager.instance.currentMultiplier);
						revealsWrapper[i].alpha = 1;
						revealsWrapper[i].color = Color.gray;
					}

					Audio.play(Audio.soundMap("reveal_not_chosen"));
					
					// only wait if another reveal is going to happen
					if (i != revealsWrapper.Count - 1)
					{
						// wait a half second before revealing the next one
						yield return new TIWaitForSeconds(0.5f);
					}
					break;
				}
			}

			// get the next reveal
			_reveal = pickemOutcome.getNextReveal();
		}

		yield return new TIWaitForSeconds(1.0f);
		BonusGamePresenter.instance.gameEnded();
	}

	private void enableSwipeToSpin()
	{
		int winIndex = wheelPick.winIndex;
		wheel.AddComponent<SwipeableWheel>().init(wheel,(winIndex * DEGREES_PER_SLICE),onSwipeStarted, onWheelSpinComplete,wheelSprite);
	}

	/**
	Removes the swipeablewheel object from wheel. 
	*/
	private void disableSwipeToSpin()
	{
		//Remove the swipeable wheel because we shouldn't be able to move it anymore.
		SwipeableWheel swipeableWheel = wheel.GetComponent<SwipeableWheel>();
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}

	private void onSwipeStarted()
	{
		StartCoroutine(playSpinClickedAnimaiton());
	}

	/**
	Handles changing out what is visible after the spin button has been pressed
	*/
	private IEnumerator playSpinClickedAnimaiton()
	{
		Audio.play(Audio.soundMap("wheel_spin_animation"));
		GameObject btn = CommonGameObject.findChild(spinButton, "Spin Button");
		btn.GetComponent<Collider>().enabled = false;
		NGUITools.SetActive(spinButton, false);
		NGUITools.SetActive(winBox, true);
		NGUITools.SetActive(topPointerAnimation, true);
		yield return new TIWaitForSeconds(0.5f);
		NGUITools.SetActive(wheelStartAnimationPrefab, true);
		yield return new TIWaitForSeconds(1.5f);
	}
	
	/**
	Called when the spin button is clicked
	*/
	public IEnumerator spinClicked()
	{
		disableSwipeToSpin();
		yield return StartCoroutine(playSpinClickedAnimaiton());
		
		int winIndex = wheelPick.winIndex;
		wheelSpinner = new WheelSpinner(wheel, (winIndex * DEGREES_PER_SLICE), onWheelSpinComplete);
	}
}

