using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class osa02HatPickem : PickingGame<PickemOutcome> {

	private const string PICKEM_BG_MUSIC = "BonusBgOSA02";
	private const string PICKEM_INTRO_VO = "HPIntroVO";
	private const string CROW_FLIES_IN_SOUND = "TransitionToBonusFlapCawDiploma";
	
	public float INSTRUCTIONS_START_Y_POS = 830.0f;
	public float INSTRUCTIONS_WAIT_FOR_INTRO = 0.0f;
	public float INSTRUCTIONS_INTRO_DUR = 1.0f;
	
	public float HAT_INTRO_GROW_DUR = 1.0f;
	public float HAT_INTRO_GROW_TO_NEXT_DUR = 0.8f;
	
	public float HAT_INTRO_TO_MULTIPLIER_DUR = 1.0f;
	public float HAT_MULTIPLIER_TO_MUSIC_DUR = 1.0f;
	
	private const string PICK_ME_SOUND = "HPPickMe";
	public float PICK_ME_SHAKE_TO_STILL_DUR = 1.0f;
	
	private const string PICK_SOUND = "HPPickHat";
	public float HAT_PICK_TO_SCROLL_DUR = 1.0f;
	public float HAT_SCROLL_TO_MULTIPLIER_DUR = 1.0f;
	private const string MULTIPLIER_SOUND = "HPRevealMultipler";
	public float HAT_MULTIPLIER_TO_NEXT_DUR = 1.0f;
	
	public float HAT_LAST_TO_FLY_DUR = 1.0f;
	private const string CROW_FLIES_OUT_SOUND = "HPRevealCrowFlies";
	public float HAT_FLY_TO_TA_DA_DUR = 1.0f;	
	private const string HAT_TA_DA = "HPMultiplierLandsTaDa";
	public float HAT_TA_DA_TO_FINAL_SCORE_DUR = 2.0f;
	public float HAT_MULTIPLIER_DY = 100.0f;
	public float HAT_MULTIPLIER_FADE_DUR = 1.0f;
	public float HAT_FINAL_SCORE_TO_FINISH = 1.0f;
	
	public Color HAT_REVEAL_GRAY_COLOR = new Color(0.5f, 0.5f, 0.5f, 1.0f);
	public float HAT_GRAY_TO_REVEAL_DUR = 0.5f;
	public const string HAT_REVEAL_SOUND = "reveal_not_chosen";
	public float HAT_REVEAL_TO_NEXT_DUR = 0.25f;
	public float HATS_TO_END_DUR = 1.0f;
	
	public GameObject instructionGo;
	public Animator scrollAnimator;
	public UILabel multiplierLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	public GameObject floatingMultiplier;
	public UILabel floatingMultiplierLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent floatingMultiplierLabelWrapperComponent;

	public LabelWrapper floatingMultiplierLabelWrapper
	{
		get
		{
			if (_floatingMultiplierLabelWrapper == null)
			{
				if (floatingMultiplierLabelWrapperComponent != null)
				{
					_floatingMultiplierLabelWrapper = floatingMultiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_floatingMultiplierLabelWrapper = new LabelWrapper(floatingMultiplierLabel);
				}
			}
			return _floatingMultiplierLabelWrapper;
		}
	}
	private LabelWrapper _floatingMultiplierLabelWrapper = null;
	

	protected PickemPick pickemPick;
	
	// The server does not send random multipliers for each pick,
	// so we have to make them up on the client.
	protected List<int> randomMultipliers;
	
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/
	
	public override void init()
	{
		base.init();
		
		Audio.play(PICKEM_INTRO_VO);
		Audio.play(CROW_FLIES_IN_SOUND);
		
		for (int hatIndex = 0; hatIndex < getButtonLengthInRound(); hatIndex++)
		{
			PickGameButtonData hatPick = getPickGameButton(hatIndex);
			
			hatPick.go.SetActive(false);
			hatPick.go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
		}
		
		pickemPick = outcome.getNextEntry();
		
		// Make up random multipliers.  The list is actually in order,
		// but we will randomly choose (and remove) a multiplier each time.
		// Except for the final multiplier, remove that because we'll show it last.
		
		randomMultipliers = new List<int>();
		for (int i = 0; i <= getButtonLengthInRound(); i++ )
		{
			randomMultipliers.Add(i+1);
		}
		if (randomMultipliers.Contains(outcome.finalMultiplier))
		{
			randomMultipliers.Remove(outcome.finalMultiplier);
		}
		CommonDataStructures.shuffleList(randomMultipliers);

		StartCoroutine(ScrollIntro());
		StartCoroutine(TitleIntro());
		StartCoroutine(HatIntro());
		
		_didInit = true;
}
	
	private IEnumerator ScrollIntro()
	{
		// I have to wait to set the multiplier text until the labels are activated
		// (because the multilabel doesn't know it has UI Label components in children).
		
		yield return new TIWaitForSeconds(HAT_INTRO_TO_MULTIPLIER_DUR);
		
		currentMultiplier = randomMultipliers[0];
		randomMultipliers.RemoveAt(0);
		
		multiplierLabelWrapper.text = CommonText.formatNumber(currentMultiplier);
		yield return new TIWaitForSeconds(HAT_MULTIPLIER_TO_MUSIC_DUR);
	}
	
	private IEnumerator TitleIntro()
	{
		yield return new TIWaitForSeconds(INSTRUCTIONS_WAIT_FOR_INTRO);
		
		iTween.MoveFrom(
			instructionGo.gameObject,
			iTween.Hash(
				"position", new Vector3(0.0f, INSTRUCTIONS_START_Y_POS, 0.0f),
				"islocal", true,
				"time", INSTRUCTIONS_INTRO_DUR,
				"easetype", iTween.EaseType.linear));
		
		yield return new TIWaitForSeconds(INSTRUCTIONS_INTRO_DUR);
	}
	
	private IEnumerator HatIntro()
	{
		for (int hatIndex = 0; hatIndex < getButtonLengthInRound(); hatIndex++)
		{
			PickGameButtonData hatPick = getPickGameButton(hatIndex);
			
			hatPick.go.SetActive(true);
			iTween.ScaleTo(hatPick.go, Vector3.one, HAT_INTRO_GROW_DUR);
			
			yield return new TIWaitForSeconds(HAT_INTRO_GROW_TO_NEXT_DUR);
		}

		Audio.playMusic(PICKEM_BG_MUSIC);
		Audio.switchMusicKey(PICKEM_BG_MUSIC);
	}
	
/*==========================================================================================================*\
	Pick Me
\*==========================================================================================================*/

	protected override IEnumerator pickMeAnimCallback()
	{
		PickGameButtonData hatPickMe = getRandomPickMe();
		
		if (hatPickMe != null)
		{
			hatPickMe.animator.Play("osa02_PickBonus_PickingObject_PickMe");
			Audio.play(PICK_ME_SOUND);
			
			yield return new TIWaitForSeconds(PICK_ME_SHAKE_TO_STILL_DUR);
			
			if (isButtonAvailableToSelect(hatPickMe))
			{
				hatPickMe.animator.Play("osa02_PickBonus_PickingObject_Still");
			}
		}
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject hatButton)
	{
		inputEnabled = false;
		
		int hatIndex = getButtonIndex(hatButton);
		removeButtonFromSelectableList(hatButton);
		PickGameButtonData hatPick = getPickGameButton(hatIndex);

		long credits = pickemPick.credits;
		hatPick.setText(CreditsEconomy.convertCredits(credits));

		if (currentMultiplier != outcome.finalMultiplier)
		{
			// Take off the hat.
			
			hatPick.animator.Play("osa02_PickBonus_PickingObject_Reveal_Number");
			Audio.play(PICK_SOUND);
			
			yield return new TIWaitForSeconds(HAT_PICK_TO_SCROLL_DUR);
			
			// Roll-up the score.
			
			StartCoroutine(
				animateScore(
				BonusGamePresenter.instance.currentPayout,
				BonusGamePresenter.instance.currentPayout + credits));
			
			BonusGamePresenter.instance.currentPayout += credits;
			
			// Close and open the scroll with the new multiplier.
			
			scrollAnimator.Play("osa02_PickBonus_multiplier_Scroll");
			Audio.play(MULTIPLIER_SOUND);
			
			yield return new TIWaitForSeconds(HAT_SCROLL_TO_MULTIPLIER_DUR);
			
			pickemPick = outcome.getNextEntry();
			
			currentMultiplier = pickemPick.multiplier;
			if (currentMultiplier == 0)
			{
				currentMultiplier = randomMultipliers[0];
				randomMultipliers.RemoveAt(0);
			}
			multiplierLabelWrapper.text = CommonText.formatNumber(currentMultiplier);
			
			yield return new TIWaitForSeconds(HAT_MULTIPLIER_TO_NEXT_DUR);
			
			scrollAnimator.Play("osa02_PickBonus_multiplier_Still");
			inputEnabled = true;
		}
		else
		{
			// This is the last pick, show the crow and reveal the leftover hats.
			
			hatPick.animator.Play("osa02_PickBonus_PickingObject_Reveal_Crow");
			Audio.play(PICK_SOUND);

			StartCoroutine(revealHats());
			
			yield return new TIWaitForSeconds(HAT_PICK_TO_SCROLL_DUR);

			// Roll-up the score.
			
			StartCoroutine(
				animateScore(
				BonusGamePresenter.instance.currentPayout,
				BonusGamePresenter.instance.currentPayout + credits));
			
			BonusGamePresenter.instance.currentPayout += credits;
			
			yield return new TIWaitForSeconds(HAT_LAST_TO_FLY_DUR);
			
			// Crow flies to the win box.
			
			scrollAnimator.Play("osa02_PickBonus_multiplier_end");
			Audio.play(CROW_FLIES_OUT_SOUND);
			
			yield return new TIWaitForSeconds(HAT_FLY_TO_TA_DA_DUR);
			Audio.play(HAT_TA_DA);
			yield return new TIWaitForSeconds(HAT_TA_DA_TO_FINAL_SCORE_DUR);
			
			// Show the multiplier.

			floatingMultiplier.SetActive(true);
			floatingMultiplierLabelWrapper.text = CommonText.formatNumber(currentMultiplier);
			
			// Fade-out the multiplier and roll-up the final score.
			
			iTween.MoveTo(
				floatingMultiplier,
				iTween.Hash(
				"y", floatingMultiplier.transform.localPosition.y + HAT_MULTIPLIER_DY,
				"isLocal", true,
				"time", HAT_MULTIPLIER_FADE_DUR));
			
			iTween.ValueTo(
				gameObject,
				iTween.Hash(
				"from", 1.0f,
				"to", 0.0f,
				"time", HAT_MULTIPLIER_FADE_DUR,
				"onupdate", "updateFloatingMultiplierMultiLabelAlpha"));
			
			yield return StartCoroutine(
				animateScore(
				BonusGamePresenter.instance.currentPayout,
				currentMultiplier * BonusGamePresenter.instance.currentPayout));
			
			BonusGamePresenter.instance.currentPayout *= currentMultiplier;
			
			yield return new TIWaitForSeconds(HAT_FINAL_SCORE_TO_FINISH);
			
			// Game over.
			
			BonusGamePresenter.instance.gameEnded();
		}
	}

	protected IEnumerator revealHats()
	{
		PickemPick pickemReveal = outcome.getNextReveal();
		
		// Gray out remaining hats.
		
		for (int pickIndex = 0; pickIndex < this.getButtonLengthInRound(); pickIndex++ )
		{
			PickGameButtonData pickGameButton = getPickGameButton(pickIndex);
			
			if (this.isButtonAvailableToSelect(pickGameButton))
			{
				CommonGameObject.colorUIGameObject(pickGameButton.go, HAT_REVEAL_GRAY_COLOR);
			}
		}

		yield return new TIWaitForSeconds(this.HAT_GRAY_TO_REVEAL_DUR);
		
		while (pickemReveal != null)
		{
			revealHat(pickemReveal);
			
			yield return StartCoroutine(revealWait.wait(HAT_REVEAL_TO_NEXT_DUR));
			pickemReveal = outcome.getNextReveal();
		}
		
		yield return new TIWaitForSeconds(HATS_TO_END_DUR);
	}
	
	protected void revealHat(PickemPick pickemReveal)
	{
		int hatIndex = getButtonIndex(grabNextButtonAndRemoveIt());
		PickGameButtonData hatPick = getPickGameButton(hatIndex);
		
		long credits = pickemReveal.credits;
		hatPick.setText(CreditsEconomy.convertCredits(credits));
		grayOutRevealText(hatIndex);
		
		hatPick.animator.Play("osa02_PickBonus_PickingObject_Reveal_Number");
		if (!revealWait.isSkipping) 
		{
			Audio.play (Audio.soundMap (HAT_REVEAL_SOUND));
		}
	}

	private void updateFloatingMultiplierMultiLabelAlpha(float alpha)
	{
		CommonGameObject.alphaUIGameObject(floatingMultiplier, alpha);
	}
	
/*==========================================================================================================*/

}


