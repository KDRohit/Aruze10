using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ee01Pickem : PickingGame<PickemOutcome>
{
	public GameObject wheel; // The parent wheel object
	public UITexture wheelSprite;
	public UILabel[] wheelLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] wheelLabelsWrapperComponent;

	public List<LabelWrapper> wheelLabelsWrapper
	{
		get
		{
			if (_wheelLabelsWrapper == null)
			{
				_wheelLabelsWrapper = new List<LabelWrapper>();

				if (wheelLabelsWrapperComponent != null && wheelLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in wheelLabelsWrapperComponent)
					{
						_wheelLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in wheelLabels)
					{
						_wheelLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _wheelLabelsWrapper;
		}
	}
	private List<LabelWrapper> _wheelLabelsWrapper = null;	
	
	public UILabel instructionText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent instructionTextWrapperComponent;

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
	
	
	public TweenColor spinButtonTween;
	public UIImageButton spinButtonImage;

	public Animator[] burnVFX; // Burn effects
	public Animator[] pickXVFX; // X button effects
	public Animator wheelSpinVFX; // Wheel glow when starting the spin
	public Animator pieSliceVFX; // Slice glow when ending the spin.

	private WheelSpinner spinner;
	private WheelPick wheelResult;

	private Color disabledColor = new Color(0.25f, 0.25f, 0.25f);		//The value used to gray out unpicked items/text

	// Constant Variables
	private const float DEGREES_PER_SLICE = 45.0f;
	private const float INTRO_VO_VOLUME = 1.0f;
	private const float INTRO_VO_PITCH = 0.0f;
	private const float INTRO_VO_DELAY = 0.6f;
	private const float LABEL_OFFSET = -67.5f;							//The labels themselves are offset by that much
	private const float REVEAL_PICKS_WAIT = 1.0f;
	private const float WHEEL_VFX_WAIT = 1.2f;
	private const float WHEEL_END_WAIT = 1.0f;
	private const float CREDITS_WAIT = 1.5f;
	private const float HIDE_X_WAIT = 0.8f;
	private const float POST_REVEAL_WAIT = 0.55f;

	// Sound Names
	private const string INTRO_VO = "BonusWelcomeVOPirates";			// Name of the sound played when the game starts.
	private const string PICK_SOUND = "PickMeBonusPirates";				// Name of sound played when a pick is selected.
	private const string REVEAL_ADVANCE_X = "ShovelXSpot";				// Name of the sound played when the Flame is played on a bonus or a game over.
	private const string REVEAL_OTHER_X = "FireballRevealX";			// Name of the sound played when the flame is played on a credit value
	private const string REVEAL_BONUS = "RevealChest";					// Name of the sound played when a chest is revealed.
	private const string REVEAL_GAME_OVER = "RevealSkull";				// Name of the sound played when a skull is revealed.
	private const string WHEEL_BG_MUSIC = "WheelPrespinPirates";		// Name of the music played when stage 2 starts.
	private const string SUMMARY_VO = "BonusSummaryVOPirates";			// Name of VO played at the end of the game so it's heard on the summary screen.

	public override void init() 
	{
		base.init();

		foreach (GameObject pick in newPickGameButtonRounds[0].pickGameObjects)
		{
			PickGameButton pickButton = pick.GetComponent<PickGameButton>();
			if(pickButton != null)
			{
				pickButton.revealNumberLabel.text = "";
				pickButton.imageReveal.enabled = false;
			}
		}

		foreach (Animator burnAnim in burnVFX)
		{
			burnAnim.enabled = false;
		}
		Audio.play(INTRO_VO, INTRO_VO_VOLUME, INTRO_VO_PITCH, INTRO_VO_DELAY);
	}

	/// If the wheel is spinning, show it!
	protected override void Update()
	{
		base.Update();

		if (spinner != null)
		{
			spinner.updateWheel();
		}
	}
	
	protected override IEnumerator pickemButtonPressedCoroutine (GameObject pirateButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pirateButton);
		removeButtonFromSelectableList(pirateButton);
		PickGameButtonData piratePick = getPickGameButton(pickIndex);

		PickemPick pick = outcome.getNextEntry();

		burnVFX[pickIndex].enabled = true;

		// Shovel if it's not a number value, fireball if it is.
		if (pick.isBonus || pick.isGameOver)
		{
			Audio.play(REVEAL_ADVANCE_X);
		}
		else
		{
			Audio.play(REVEAL_OTHER_X);
		}
		
		// Added in this section so the reveal could happen during the animation, instead of at the end.
		yield return new TIWaitForSeconds(HIDE_X_WAIT);
		UISprite XImage = pirateButton.GetComponent<UISprite>();
		if(XImage != null)
		{
			XImage.enabled = false;
		}
		showRevealItem(pick, null, pickIndex);
		
		yield return new TIWaitForSeconds(POST_REVEAL_WAIT);

		if (pick.isBonus)
		{
			instructionTextWrapper.text = Localize.text("congratulations_ex");
			SlotOutcome challengeBonus = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, pick.bonusGame);
			WheelOutcome wheelOutcome = new WheelOutcome(challengeBonus);
			wheelResult = wheelOutcome.getNextEntry();
			Audio.play(REVEAL_BONUS);
			StartCoroutine(revealChests(true));
		}
		else if (pick.isGameOver)
		{
			instructionTextWrapper.text = Localize.text("game_over");
			Audio.play(REVEAL_GAME_OVER);
			StartCoroutine(revealChests(false));
		}
		else
		{	
			piratePick.revealNumberLabel.text = CreditsEconomy.convertCredits(pick.credits);
			currentWinAmountTextWrapperNew = currentWinAmountTextsWrapper[0];
			yield return StartCoroutine(addCredits(pick.credits));
			inputEnabled = true;
		}
	}

	private void enableSwipeToSpin()
	{
		wheel.AddComponent<SwipeableWheel>().init(wheel,(DEGREES_PER_SLICE * wheelResult.winIndex) + LABEL_OFFSET, 
			onSwipeStart,onWheelSpinComplete,wheelSprite);
	}

	private void disableSwipeToSpin()
	{
		SwipeableWheel swipeableWheel = wheel.GetComponent<SwipeableWheel>();
		if (swipeableWheel != null)
		{
			Destroy(swipeableWheel);
		}
	}
	
	// Handles the visual changes as needed
	private void showRevealItem(PickemPick pick, UISprite xImage, int indexArray)
	{
		PickGameButton pickButton = newPickGameButtonRounds[0].pickGameObjects[indexArray].GetComponent<PickGameButton>();
		if(pickButton != null)
		{
			if (xImage != null)
			{
				xImage.enabled = false;
				pickButton.revealNumberLabel.color = disabledColor;
				pickButton.imageReveal.color = disabledColor;
			}

			pickButton.animator.gameObject.SetActive(false);
						
			if (pick.isBonus)
			{
				pickButton.imageReveal.enabled = true;
				pickButton.imageReveal.spriteName = "treasure_pick_m";
			}
			else if (pick.isGameOver)
			{
				pickButton.imageReveal.enabled = true;
				pickButton.imageReveal.spriteName = "ends_pick_m";
			}
			else
			{
				pickButton.imageReveal.enabled = false;
				pickButton.revealNumberLabel.text = CreditsEconomy.convertCredits(pick.credits);
			}
		}
	}
	
	// Similar to burnX, but for reveals after the game has ended or before we move onto the wheel.
	public IEnumerator revealChests(bool wheelGame)
	{
		yield return new TIWaitForSeconds(REVEAL_PICKS_WAIT);
		revealWait.reset();

		int revealCount = outcome.revealCount;
		for (int i = 0; i < revealCount; i++)
		{
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap("reveal_not_chosen"));
			}
			PickemPick pickReveal = outcome.getNextReveal();
			foreach (GameObject pick in newPickGameButtonRounds[0].pickGameObjects)
			{
				GameObject chest = pick.GetComponent<PickGameButton>().button;
				if(chest != null)
				{
					UISprite XImage = chest.GetComponent<UISprite>();
				
					if (XImage != null && XImage.enabled == true)
					{										
						showRevealItem(pickReveal, XImage, System.Array.IndexOf(newPickGameButtonRounds[0].pickGameObjects, pick));
						break;
					}
				}
			}
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
		}

		yield return new TIWaitForSeconds(REVEAL_PICKS_WAIT);
		
		if (wheelGame)
		{
			beginWheelGame();
		}
		else
		{
			endGame();
		}
	}

/*======================================================================================*\
 	Spinning Wheel
/*======================================================================================*/

	// Set the layers because at the start of the game, there's a bit of overlay.
	public void beginWheelGame()
	{
		continueToNextStage();

		currentWinAmountTextWrapperNew = currentWinAmountTextsWrapper[1];
		currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);

		float scaleMin = float.MaxValue;
		for (int i = 0; i < wheelResult.wins.Count; i++)
		{
			long creditLine = wheelResult.wins[i].credits;
			if (creditLine != 0)
			{
				wheelLabelsWrapper[i].text = CommonText.makeVertical(CreditsEconomy.convertCredits(creditLine, false));
				scaleMin = Mathf.Min(wheelLabelsWrapper[i].transform.localScale.x, scaleMin);
			}
		}
		
		// Let's make the scale uniform across all the labels now.
		for (int i = 0; i < wheelResult.wins.Count; i++)
		{	
			wheelLabelsWrapper[i].transform.localScale = new Vector3(scaleMin, scaleMin, 1);
		}

		Audio.switchMusicKeyImmediate(WHEEL_BG_MUSIC);
		CommonGameObject.setLayerRecursively(wheelSpinVFX.gameObject, Layers.ID_NGUI);
		CommonGameObject.setLayerRecursively(pieSliceVFX.gameObject, Layers.ID_NGUI);
		wheelSpinVFX.enabled = false;
		pieSliceVFX.enabled = false;
		enableSwipeToSpin();
	}

	public void spinClicked()
	{
		spinner = new WheelSpinner(wheel, (DEGREES_PER_SLICE * wheelResult.winIndex) + LABEL_OFFSET, onWheelSpinComplete);
		disableSwipeToSpin();
		onSwipeStart();
	}
	
	private void onSwipeStart()
	{
		StartCoroutine(beginWheelVFX());
		spinButtonTween.Reset();
		spinButtonTween.enabled = false;
		spinButtonImage.isEnabled = false;
	}
	
	// Unset the layers at the end as a safety measure.
	private IEnumerator beginWheelVFX()
	{
		CommonGameObject.setLayerRecursively(wheelSpinVFX.gameObject, Layers.ID_NGUI_OVERLAY);
		wheelSpinVFX.enabled = true;
		wheelSpinVFX.Play("wheelStart");
		yield return new TIWaitForSeconds(WHEEL_VFX_WAIT);
		wheelSpinVFX.enabled = false;
		CommonGameObject.setLayerRecursively(wheelSpinVFX.gameObject, Layers.ID_NGUI);
	}
	
	private void onWheelSpinComplete()
	{
		//Rollup further and then end the game.
		StartCoroutine(endWheelGame());
	}
	
	// Unset the layers at the end as a safety measure, and start the FINAL COUNTDOWN
	private IEnumerator endWheelGame()
	{
		CommonGameObject.setLayerRecursively(pieSliceVFX.gameObject, Layers.ID_NGUI_OVERLAY);
		pieSliceVFX.enabled = true;
		yield return new TIWaitForSeconds(WHEEL_END_WAIT);
		pieSliceVFX.enabled = false;
		CommonGameObject.setLayerRecursively(pieSliceVFX.gameObject, Layers.ID_NGUI);

		yield return StartCoroutine(addCredits(wheelResult.credits));
		yield return new TIWaitForSeconds(CREDITS_WAIT);
		endGame();
	}

	private void endGame()
	{
		Audio.play(SUMMARY_VO);
		BonusGamePresenter.instance.gameEnded();
	}
}

