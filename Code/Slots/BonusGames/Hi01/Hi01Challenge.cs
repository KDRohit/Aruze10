using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// hi01 bonus wheel. Handles the wheel game for hi01.
/// </summary>
public class Hi01Challenge : ChallengeGame 
{
	public GameObject bonusWheel;											// The wheel parent part that will be spinning.
	public Transform spinningWheelSize;										// Transform with a scale set to the size of the spinning wheel.
	public UILabel instructionText; 										// The text at the top of the screen. -  To be removed when prefabs are updated.
	public LabelWrapperComponent instructionTextWrapperComponent; 										// The text at the top of the screen.

	public LabelWrapper instructionTextWrapper
	{
		get
		{
			if (_instructionTextWrapper == null)
			{
				if (instructionTextWrapperComponent != null)
				{
					_instructionTextWrapper = instructionTextWrapperComponent.labelWrapper;
				}
				else
				{
					_instructionTextWrapper = new LabelWrapper(instructionText);
				}
			}
			return _instructionTextWrapper;
		}
	}
	private LabelWrapper _instructionTextWrapper = null;
	
	public UILabel winAmount;												// Label holding the win amount. -  To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountWrapperComponent;												// Label holding the win amount.

	public LabelWrapper winAmountWrapper
	{
		get
		{
			if (_winAmountWrapper == null)
			{
				if (winAmountWrapperComponent != null)
				{
					_winAmountWrapper = winAmountWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountWrapper = new LabelWrapper(winAmount);
				}
			}
			return _winAmountWrapper;
		}
	}
	private LabelWrapper _winAmountWrapper = null;
	
	public UILabel multiplierAmount;										// Label holding the win amount. -  To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierAmountWrapperComponent;										// Label holding the win amount.

	public LabelWrapper multiplierAmountWrapper
	{
		get
		{
			if (_multiplierAmountWrapper == null)
			{
				if (multiplierAmountWrapperComponent != null)
				{
					_multiplierAmountWrapper = multiplierAmountWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierAmountWrapper = new LabelWrapper(multiplierAmount);
				}
			}
			return _multiplierAmountWrapper;
		}
	}
	private LabelWrapper _multiplierAmountWrapper = null;
	
	public GameObject gameScaler;											// The object that is used to scale in the whole game.
	public GameObject bigWinEffect;											// The effect played when you win one of the bigger slices.
	public GameObject smallWinEffect;										// The effect played when you win one of the the smaller slices.
	public GameObject startSpinEffect;										// The effect played when you start the spin
	public Animator pointerAnimator;										// Animator controlling the pointers animations (sheen / glow / fade out.)
	public Animator spinAnimator; 											// Animator controlling the spin button.
	public CoinScript spinningCoin;											// The spinning coin that moves down into the win box.

	public UILabel[] wheelValues; 											// The labels on the wheel itself -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelValuesWrapperComponent; 											// The labels on the wheel itself

	public List<LabelWrapper> wheelValuesWrapper
	{
		get
		{
			if (_wheelValuesWrapper == null)
			{
				_wheelValuesWrapper = new List<LabelWrapper>();

				if (wheelValuesWrapperComponent != null && wheelValuesWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelValuesWrapperComponent)
					{
						_wheelValuesWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelValues)
					{
						_wheelValuesWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelValuesWrapper;
		}
	}
	private List<LabelWrapper> _wheelValuesWrapper = null;	
	
	public UILabel[] wheelShadows; 											// The labels on the wheel itself -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelShadowsWrapperComponent; 											// The labels on the wheel itself

	public List<LabelWrapper> wheelShadowsWrapper
	{
		get
		{
			if (_wheelShadowsWrapper == null)
			{
				_wheelShadowsWrapper = new List<LabelWrapper>();

				if (wheelShadowsWrapperComponent != null && wheelShadowsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelShadowsWrapperComponent)
					{
						_wheelShadowsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelShadows)
					{
						_wheelShadowsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelShadowsWrapper;
		}
	}
	private List<LabelWrapper> _wheelShadowsWrapper = null;	
	
	public Animator[] bigAnimators;

	private WheelSpinner wheelSpinner;				 						// Wheelspinner

	private WheelOutcome wheelOutcome; 										// portal wheel game outcome
	private WheelPick pick; 												// the pick object form the portal wheel game
	private float restingAngle; 											// angle the wheel should come to rest at after the spin
	private bool spinStarted = false;										// Has the spin started?
	private CoroutineRepeater pointerSheenRepeater;							// Makes sure that the sheen on the pointer keeps going.
	private long multiplier = 1;											// The multipler for this game.

	private string[] bigAnimationNames = {"HL7_SpinItRich_WheelBonus_LargeSliceFinalVer02_white_Animation", "HL7_SpinItRich_WheelBonus_LargeSliceFinalVer02_Green_Animation", "HL7_SpinItRich_WheelBonus_LargeSliceFinalVer02_Red_Animation", "HL7_SpinItRich_WheelBonus_LargeSliceFinalVer02_blue_Animation"};

	// Constants
	private readonly int[] BIG_SLICE_INDEXES = {0, 4, 8, 12};
	private const int NUMBER_OF_SLICES = 20;
	private const float DEGREES_PER_SLICE = 360.0f / NUMBER_OF_SLICES;
	private const float TIME_EXPAND_GAME = 1.5f;
	private const float TIME_AFTER_GAME_ENDS = 1.0f;
	private const float MIN_TIME_POINTER_SHEEN = 2.0f;						// Minimum time an animation might take to play next
	private const float MAX_TIME_POINTER_SHEEN = 7.0f;						// Maximum time an animation might take to play next
	private const float MULTILIER_TRAVEL_TIME = 1.0f;
	private const float SPIN_EFFECT_TIME = 1.5f;
	// Sound Names
	private const string INTRO_SOUND = "HL7TransitionToWheel";
	private const string LAND_MILLION = "WheelStopBigAssHiLimit7";
	private const string LAND_NORMAL = "WheelStopHiLimit7";
	
	public override void init()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_idle_bg"));
		Audio.play(INTRO_SOUND);
		//Begin the process to parse the results from the json provided by the server		
		wheelOutcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
		pick = wheelOutcome.getNextEntry();

		// Set all of the values in the wheel.
		float scaleMin = float.MaxValue;
		for (int i = 0; i < pick.wins.Count; i++)
		{
			long credits = pick.wins[i].credits;
			if (i < wheelValuesWrapper.Count)
			{
				string creditsText = CreditsEconomy.convertCredits(credits, false);
				wheelValuesWrapper[i].text = CommonText.makeVertical(creditsText);
				scaleMin = Mathf.Min(wheelValuesWrapper[i].transform.localScale.x, scaleMin);
				if (i % BIG_SLICE_INDEXES.Length == 0)
				{
					wheelShadowsWrapper[i / BIG_SLICE_INDEXES.Length].text = CommonText.makeVertical(creditsText);
				}
			}
			else
			{
				Debug.LogError("Trying to add more values than there are slices in the wheel.");
			}
		}
		if (pick.wins.Count != wheelValuesWrapper.Count)
		{
			Debug.LogError("There were not enough values to fill up all of the slices in the wheel.");
		}
		// Make all of the text the same size.
		for (int i = 1; i < wheelValuesWrapper.Count; i++)
		{	
			wheelValuesWrapper[i].transform.localScale = new Vector3(scaleMin, scaleMin, 1.0f);
		}
		for (int i = 1; i < wheelShadowsWrapper.Count; i++)
		{	
			wheelShadowsWrapper[i].transform.localScale = new Vector3(scaleMin, scaleMin, 1.0f);
		}

		// Cheat into Million Outcome.
		// pick.winIndex = 0;
		// pick.credits = 1000000;

		restingAngle = pick.winIndex * DEGREES_PER_SLICE + (pick.winIndex / BIG_SLICE_INDEXES.Length) * DEGREES_PER_SLICE;
		if (pick.winIndex % BIG_SLICE_INDEXES.Length != 0)
		{
			restingAngle += DEGREES_PER_SLICE / 2;
		}

		pointerSheenRepeater = new CoroutineRepeater(MIN_TIME_POINTER_SHEEN, MAX_TIME_POINTER_SHEEN, pointerSheenCallback);

		// Setup the wheel to be swipeable
		bonusWheel.AddComponent<SwipeableWheel>().init(bonusWheel, restingAngle, onSwipeStart, onSwipeEnd, spinningWheelSize);
		winAmountWrapper.text = "";
		if (SlotBaseGame.instance != null)
		{
			multiplier = BonusGameManager.instance.currentMultiplier;
		}
		multiplierAmountWrapper.text = CommonText.formatNumber(multiplier);

		Color[] colorsToSwapBetween = {Color.white, Color.green, Color.red, Color.blue, Color.magenta};
		CommonEffects.addOscillateTextColorEffect(instructionTextWrapper, colorsToSwapBetween, 0.01f);
		StartCoroutine(expandGame());
		_didInit = true;
	}

	private IEnumerator pointerSheenCallback()
	{
		if (pointerAnimator != null && !spinStarted)
		{
			pointerAnimator.Play("HL7_SpinItRich_WheelBonus_Wheel Pointer_sheen_Animation");
			yield return new TIWaitForSeconds(0.5f);
			pointerAnimator.Play ("HL7_SpinItRich_WheelBonus_Wheel Pointer_Still_Animation");
		}
	}

	private IEnumerator expandGame()
	{
		if (gameScaler != null)
		{
			iTween.ScaleFrom(gameScaler, Vector3.zero, TIME_EXPAND_GAME);
			yield return new TIWaitForSeconds(TIME_EXPAND_GAME);
		}
		else
		{
			Debug.LogWarning("Skipping scale in effect.");
		}
		_didInit = true;
	}
	
	private void disableSwipeToSpin()
	{
		SwipeableWheel swipeableWheel = bonusWheel.GetComponent<SwipeableWheel>();
		
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}

	private void onSwipeStart()
	{
		spinStarted = true;
		StartCoroutine(prespinHandler());

	}

	private void onSwipeEnd()
	{
		StartCoroutine(endMainGame());
	}

	private IEnumerator clickSpinButtonEnumerator()
	{
		disableSwipeToSpin();
		yield return StartCoroutine(prespinHandler());
		// Start the wheel spinner
		wheelSpinner = new WheelSpinner
		(
			bonusWheel, 
			restingAngle,
			delegate() 
			{
				//Left wheel only ends if there is no right wheel spins.
				StartCoroutine(endMainGame());
			}
		);
		
		startSpinEffect.SetActive(true);
		yield return new TIWaitForSeconds(SPIN_EFFECT_TIME);
		startSpinEffect.SetActive(false);

		spinStarted = true;
	}

	public void clickSpinButton()
	{
		Debug.Log("Clicked Spin Button.");
		if (!spinStarted)
		{
			StartCoroutine(clickSpinButtonEnumerator());
		}
	}

	public IEnumerator prespinHandler()
	{
		// Get the pointer glowing while the wheel spins.
		if (pointerAnimator != null)
		{
			pointerAnimator.Play("HL7_SpinItRich_WheelBonus_Wheel Pointer_glow_Animation");
		}
		// Change the spin area into the win box.
		if (spinAnimator != null)
		{
			spinAnimator.Play("HL7_SpinItRich_WheelBonus_SpinBtn_Spin_Animation");
		}
		yield return null;
	}

	private IEnumerator playPointerFade()
	{
		if (pointerAnimator != null)
		{
			pointerAnimator.Play("HL7_SpinItRich_WheelBonus_Wheel Pointer_Slice win_Animation");
			while (!pointerAnimator.GetCurrentAnimatorStateInfo(0).IsName("HL7_SpinItRich_WheelBonus_Wheel Pointer_Slice win_Animation"))
			{
				yield return null;
			}
			// Wait for the animation to stop.
			while (pointerAnimator.GetCurrentAnimatorStateInfo(0).IsName("HL7_SpinItRich_WheelBonus_Wheel Pointer_Slice win_Animation"))
			{
				yield return null;
			}
			//pointerAnimator.gameObject.SetActive(false);
		}
	}

	private IEnumerator endMainGame()
	{
		// Draw the box around the winning value.
		if (pick.winIndex % BIG_SLICE_INDEXES.Length == 0)
		{
			// Draw the big box.
			if (bigWinEffect != null)
			{
				bigWinEffect.SetActive(true);
			}
			else
			{
				Debug.LogWarning("No Big Win Effect");
			}
			// Stop the pointer from glowing, fade it out.
			int indexOfAnimator = pick.winIndex / BIG_SLICE_INDEXES.Length;
			if (indexOfAnimator < bigAnimators.Length)
			{
				if (bigAnimators[indexOfAnimator] != null)
				{
					bigAnimators[indexOfAnimator].gameObject.SetActive(true);
					bigAnimators[indexOfAnimator].enabled = true;
					bigAnimators[indexOfAnimator].Play(bigAnimationNames[indexOfAnimator]);
					bigAnimators[indexOfAnimator].gameObject.transform.Find("large Slice/Particle_01").gameObject.SetActive(true);
					bigAnimators[indexOfAnimator].gameObject.transform.Find("large Slice/Particle_02").gameObject.SetActive(true);
				}
				if (bigWinEffect != null)
				{
					bigWinEffect.transform.parent = wheelValuesWrapper[pick.winIndex].transform;
				}
			}
			StartCoroutine(playPointerFade());
		}
		else
		{
			// Draw the small box.
			if (smallWinEffect != null)
			{
				smallWinEffect.SetActive(true);
			}
			else
			{
				Debug.LogWarning("No Small Win Effect");
			}
			if (pointerAnimator != null)
			{
				pointerAnimator.Play("HL7_SpinItRich_WheelBonus_Wheel Pointer_Still_Animation");
			}
		}

		// Wow! What a champion.
		if (pick.credits == 1000000)
		{
			Audio.play(LAND_MILLION);
		}
		else
		{
			Audio.play(LAND_NORMAL);
		}
		yield return StartCoroutine(SlotUtils.rollup(0, pick.credits, winAmountWrapper));

		// Move down multiplier onto the main credits area

		if (spinningCoin != null)
		{
			multiplierAmountWrapper.gameObject.SetActive(false);
			spinningCoin.gameObject.SetActive(true);
			spinningCoin.spin();
			iTween.MoveTo(spinningCoin.gameObject, winAmountWrapper.transform.position, MULTILIER_TRAVEL_TIME);
			yield return new TIWaitForSeconds(MULTILIER_TRAVEL_TIME);
			GameObject.Destroy(spinningCoin.gameObject);
			if (spinAnimator != null)
			{
				spinAnimator.Play("HL7_SpinItRich_WheelBonus_SpinBtn_Spin_still");
			}
		}
		// Roll up the rest of the number with the multipler.
		yield return StartCoroutine(SlotUtils.rollup(pick.credits, pick.credits * multiplier, winAmountWrapper));

		// Let it all sink in.
		yield return new TIWaitForSeconds(TIME_AFTER_GAME_ENDS);

		BonusGamePresenter.instance.currentPayout = pick.credits;
		BonusGamePresenter.instance.gameEnded();
	}

	protected override void Update()
	{
		base.Update();
		if (!spinStarted && _didInit)
		{
			pointerSheenRepeater.update();
		}
		else if (wheelSpinner != null)
		{
			wheelSpinner.updateWheel();
		}
	}
}

