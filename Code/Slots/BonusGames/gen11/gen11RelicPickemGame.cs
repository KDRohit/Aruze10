using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gen11RelicPickemGame : PickingGame<WheelOutcome>
{
	// Tunable Constants

	public float WAIT_FOR_FOREST_DUR = 1.0f;
	
	public float WAIT_FOR_REVEAL_RELIC = 1.0f;
	public  float WAIT_FOR_REVEAL_TIGER = 1.0f;
	
	public float WAIT_TO_ADVANCE_DUR = 1.0f;
	public float WAIT_TO_END_GAME_DUR = 1.0f;

	public float PICK_WIN_ALL_TIGER_SOUND_DELAY = 0.3f;
	
	// Tunable Objects
	
	public Animator forestAnimator;
	public UILabel[] titleLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] titleLabelsWrapperComponent;

	public List<LabelWrapper> titleLabelsWrapper
	{
		get
		{
			if (_titleLabelsWrapper == null)
			{
				_titleLabelsWrapper = new List<LabelWrapper>();

				if (titleLabelsWrapperComponent != null && titleLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in titleLabelsWrapperComponent)
					{
						_titleLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in titleLabels)
					{
						_titleLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _titleLabelsWrapper;
		}
	}
	private List<LabelWrapper> _titleLabelsWrapper = null;	
	
	public GameObject relicUI;
	public GameObject tigerUI;
	public UILabel tigerWinAmountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent tigerWinAmountLabelWrapperComponent;

	public LabelWrapper tigerWinAmountLabelWrapper
	{
		get
		{
			if (_tigerWinAmountLabelWrapper == null)
			{
				if (tigerWinAmountLabelWrapperComponent != null)
				{
					_tigerWinAmountLabelWrapper = tigerWinAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_tigerWinAmountLabelWrapper = new LabelWrapper(tigerWinAmountLabel);
				}
			}
			return _tigerWinAmountLabelWrapper;
		}
	}
	private LabelWrapper _tigerWinAmountLabelWrapper = null;
	
	public Animator tigerWinBoxAnimator;
	
	// Variables
	
	private WheelPick wheelPick;
	private long winAllAmount;
	private bool isWaiting;
	
	// Constants
	
	private const string INTRO_MUSIC_NAME = "TempleIntro";	
	private const string FOREST_ADVANCE_SOUND_NAME = "TempleIntroAnimation";
	private const string FOREST_TEMPLE_SOUND_NAME = "TransitionToFinalBonusTiger";
	
	private const string PICK_ME_SOUND_NAME = "TemplePickMe";
	private const string PICK_WIN_ALL_SOUND_NAME = "TempleRevealWinAll";
	private const string PICK_WIN_ALL_TIGER_SOUND_NAME = "M2RoarTiger";
	private const string PICK_END_BONUS_SOUND_NAME = "TempleRevealBad";
	private const string REVEAL_RELIC_SOUND_NAME = "TempleRevealOthers";
	
	private const string PICK_TIGER_CREDITS_SOUND_NAME = "TempleTigerRevealCredit";
	private const string PICK_TIGER_MULTIPLIER_SOUND_NAME = "TempleTigerRevealMultiplier";
	
	private const string FOREST_IDLE_ANIM_NAME = "1234_still";
	private const string FOREST_ADVANCE_ANIM_NAME = "advance1234_playTwice";
	private const string FOREST_TEMPLE_ANIM_NAME = "advance5";
	
	private const string IDLE_ANIM_NAME = "idle";
	private const string PICK_ME_ANIM_NAME = "pickme";
	
	private const string PICK_WIN_ALL_ANIM_NAME = "winAll";
	private const string PICK_CREDITS_ANIM_NAME = "number";
	private const string PICK_END_BONUS_ANIM_NAME = "endsBonus";
	private const string PICK_MULTIPLIER_ANIM_NAME = "2x";

	private const string GRAY_OUT_ANIM_NAME = "gray_idle";
		
	private const string REVEAL_WIN_ALL_ANIM_NAME = "winAll_gray";
	private const string REVEAL_CREDITS_ANIM_NAME = "number_gray";
	private const string REVEAL_END_BONUS_ANIM_NAME = "endsBonus_gray";
	private const string REVEAL_MULTIPLIER_ANIM_NAME = "2x_gray";

	private const string TIGER_WIN_BOX_GLOW_ANIM_NAME = "idle";
	private const string TIGER_WIN_BOX_IDLE_ANIM_NAME = "glow";
	
	private enum StageEnum
	{
		Scrolls = 0,
		Rings = 1,
		Idols = 2,
		Keys = 3,
		Tigers = 4
	};
	
	private static readonly string[] BG_MUSIC_NAMES =
	{
		"BonusBgTiger1",
		"BonusBgTiger2",
		"BonusBgTiger3",
		"BonusBgTiger4",
		"TemplePickTigerBg"
	};

	private static readonly string[] INTRO_VO_NAMES =
	{	
		"TTPickAScroll",
		"TTPickARing",
		"TTPickAnIdol",
		"TTPickAKey",
		"TTPickATiger"
	};
	
	private static readonly string[] PICK_CREDITS_SOUND_NAMES =
	{
		"TempleRevealCredit1",
		"TempleRevealCredit2",
		"TempleRevealCredit3",
		"TempleRevealCredit4",
		"TempleTigerRevealCredit"
	};
	
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/

	public override void init()
	{
		base.init();
		
		StartCoroutine(playBgMusic());
		Audio.play(INTRO_VO_NAMES[currentStage]);
		
		for (int iTitle = 0; iTitle < titleLabelsWrapper.Count; iTitle++)
		{
			LabelWrapper titleLabel = titleLabelsWrapper[iTitle];
			
			if (titleLabel != null)
			{
				int round = iTitle + 1;
				
				titleLabel.text = string.Format(
					"{0}\n{1}",
					Localize.textUpper("round_{0}", round),
					Localize.textUpper(string.Format("gen11_challenge_round_{0}_title", round)));
			}
		}
	}
	
	protected IEnumerator playBgMusic()
	{
		Audio.switchMusicKeyImmediate(Audio.soundMap(INTRO_MUSIC_NAME));
		yield return null;
		
		Audio.switchMusicKey(Audio.soundMap(BG_MUSIC_NAMES[currentStage]));
	}
	
/*==========================================================================================================*\
	Pick Me Anim callback
\*==========================================================================================================*/
	
	protected override IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled)
		{
			PickGameButtonData pickMe = getRandomPickMe();
			
			if (pickMe != null)
			{
				pickMe.animator.Play(PICK_ME_ANIM_NAME);
				Audio.play(PICK_ME_SOUND_NAME);
				
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pickMe.animator));
				
				if (inputEnabled && isButtonAvailableToSelect(pickMe))
				{
					pickMe.animator.Play(IDLE_ANIM_NAME);
				}
			}
		}
	}

/*==========================================================================================================*\
	Pickem Button Pressed Coroutine
\*==========================================================================================================*/

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		if (currentStage < (int)StageEnum.Tigers)
		{
			yield return StartCoroutine(relicButtonPressedCoroutine(button));
			
		}
		else
		{
			yield return StartCoroutine(tigerButtonPressedCoroutine(button));
		}
	}
	
/*==========================================================================================================*\
	Relic Button Pressed Coroutine
\*==========================================================================================================*/
	
	protected IEnumerator relicButtonPressedCoroutine(GameObject relicButton)
	{
		inputEnabled = false;
		
		int relicIndex = getButtonIndex(relicButton);
		removeButtonFromSelectableList(relicButton);
		PickGameButtonData relicPick = getPickGameButton(relicIndex);
		
		for (int iOther = 0; iOther < getButtonLengthInRound(); iOther++)
		{
			if (iOther != relicIndex)
			{
				PickGameButtonData otherPick = getPickGameButton(iOther);
				otherPick.animator.Play(GRAY_OUT_ANIM_NAME);
			}
		}
		
		wheelPick = outcome.getNextEntry();
		IdentifyWinAll();
		
		long credits = wheelPick.credits;
		
		if (wheelPick.canContinue)
		{
			relicPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			
			if (credits == winAllAmount)
			{
				relicPick.animator.Play(PICK_WIN_ALL_ANIM_NAME);
				Audio.play(PICK_WIN_ALL_SOUND_NAME);
				Audio.play(PICK_WIN_ALL_TIGER_SOUND_NAME, 1, 0, PICK_WIN_ALL_TIGER_SOUND_DELAY);
			}
			else
			{
				relicPick.animator.Play(PICK_CREDITS_ANIM_NAME);
				Audio.play(PICK_CREDITS_SOUND_NAMES[currentStage]);
			}
		}
		else
		{
			relicPick.animator.Play(PICK_END_BONUS_ANIM_NAME);
			Audio.play(PICK_END_BONUS_SOUND_NAME);
		}
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(relicPick.animator));
		
		isWaiting = true;
		StartCoroutine(revealRemainingRelics());

		yield return StartCoroutine(
			animateScore(
			BonusGamePresenter.instance.currentPayout,
			BonusGamePresenter.instance.currentPayout + credits));
		BonusGamePresenter.instance.currentPayout += credits;
		
		yield return StartCoroutine(Wait());
		yield return new WaitForSeconds(WAIT_TO_ADVANCE_DUR);
		
		if (wheelPick.canContinue)
		{
			stageObjects[currentStage].SetActive(false);
			
			if (currentStage+1 < (int)StageEnum.Tigers)
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap(FOREST_ADVANCE_SOUND_NAME));
				
				forestAnimator.Play(FOREST_ADVANCE_ANIM_NAME);
				yield return new WaitForSeconds(WAIT_FOR_FOREST_DUR);
				
				forestAnimator.Play(FOREST_IDLE_ANIM_NAME);
				yield return null;
				
				forestAnimator.Play(FOREST_ADVANCE_ANIM_NAME);
				yield return new WaitForSeconds(WAIT_FOR_FOREST_DUR);
		
				forestAnimator.Play(FOREST_IDLE_ANIM_NAME);
				yield return null;
			}
			else
			{
				forestAnimator.Play(FOREST_TEMPLE_ANIM_NAME);
				Audio.switchMusicKeyImmediate(Audio.soundMap(FOREST_TEMPLE_SOUND_NAME));
				
				yield return new WaitForSeconds(WAIT_FOR_FOREST_DUR);
			}				
			
			continueToNextStage();
		
			Audio.play(INTRO_VO_NAMES[currentStage]);
			Audio.switchMusicKeyImmediate(Audio.soundMap(BG_MUSIC_NAMES[currentStage]));
			
			if (currentStage == (int)StageEnum.Tigers)
			{
				relicUI.SetActive(false);
				tigerUI.SetActive(true);
				
				currentWinAmountTextWrapperNew = tigerWinAmountLabelWrapper;
				currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}
			
			inputEnabled = true;
		}
		else
		{
			yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);			
			BonusGamePresenter.instance.gameEnded();
		}
	}
	
	// You have to manually identify the win all pick.
	// If it's there, then it's the sum of all the other picks.
	protected void IdentifyWinAll()
	{
		winAllAmount = 0;
		
		// Find the max credits.
		
		long max = 0;
		for (int i = 0; i < wheelPick.wins.Count; i++)
		{
			long credits = wheelPick.wins[i].credits;
			
			if (credits > max)
			{
				max = credits;
			}
		}
		
		// Sum all the credits (except the max credits).
		
		long sum = 0;
		for (int i = 0; i < wheelPick.wins.Count; i++ )
		{
			long credits = wheelPick.wins[i].credits;
			
			if (credits != max)
			{
				sum += credits;
			}
		}
		
		// If the sum of all the other credits equals the max credits,
		// then we identified the win all amount.
		
		if (sum == max)
		{
			winAllAmount = sum;
		}
	}
	
	protected IEnumerator Wait()
	{
		while (isWaiting)
		{
			yield return null;
		}
	}
	
/*==========================================================================================================*\
	Reveal Remaining Relics
\*==========================================================================================================*/
	
	protected IEnumerator revealRemainingRelics()
	{
		revealWait.reset();
		
		for (int iRelic = 0; iRelic < wheelPick.wins.Count; iRelic++)
		{
			if (iRelic != wheelPick.winIndex)
			{
				StartCoroutine(revealRemainingRelic(iRelic));
				yield return StartCoroutine(revealWait.wait(WAIT_FOR_REVEAL_RELIC));
			}
		}
		
		isWaiting = false;
	}
	
	protected IEnumerator revealRemainingRelic(int iRelic)
	{
		PickGameButtonData relicPick = removeNextPickGameButton();
		
		if (relicPick != null)
		{
			if (wheelPick.wins[iRelic].canContinue)
			{
				long credits = wheelPick.wins[iRelic].credits;
				
				if (credits == winAllAmount)
				{			
					relicPick.animator.Play(REVEAL_WIN_ALL_ANIM_NAME);
				}
				else
				{
					if (wheelPick.credits == winAllAmount)
					{
						relicPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
						relicPick.animator.Play(PICK_CREDITS_ANIM_NAME);
					}
					else
					{
						relicPick.extraLabel.text = CreditsEconomy.convertCredits(credits);
						relicPick.animator.Play(REVEAL_CREDITS_ANIM_NAME);
					}
				}
			}
			else
			{
				relicPick.animator.Play(REVEAL_END_BONUS_ANIM_NAME);
			}

			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_RELIC_SOUND_NAME);
			}
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(relicPick.animator));
		}
	}

/*==========================================================================================================*\
	Tiger Button Pressed Coroutine
\*==========================================================================================================*/

	protected IEnumerator tigerButtonPressedCoroutine(GameObject tigerButton)
	{
		inputEnabled = false;
		
		int tigerIndex = getButtonIndex(tigerButton);
		removeButtonFromSelectableList(tigerButton);
		PickGameButtonData tigerPick = getPickGameButton(tigerIndex);

		for (int iOther = 0; iOther < getButtonLengthInRound(); iOther++)
		{
			if (iOther != tigerIndex)
			{
				PickGameButtonData otherPick = getPickGameButton(iOther);
				otherPick.animator.Play(GRAY_OUT_ANIM_NAME);
			}
		}
		
		wheelPick = outcome.getNextEntry();
		
		long credits = wheelPick.credits;
		int multiplier = wheelPick.multiplier;
		
		if (credits > 0)
		{
			tigerPick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			
			tigerPick.animator.Play(PICK_CREDITS_ANIM_NAME);
			Audio.play(PICK_TIGER_CREDITS_SOUND_NAME);
		}
		else if (multiplier > 0)
		{
			tigerPick.animator.Play(PICK_MULTIPLIER_ANIM_NAME);
			Audio.play(PICK_TIGER_MULTIPLIER_SOUND_NAME);
			
			if (credits == 0)
			{
				credits = BonusGamePresenter.instance.currentPayout;
			}
		}
		
		yield return StartCoroutine(CommonAnimation.waitForAnimDur(tigerPick.animator));
		
		isWaiting = true;
		StartCoroutine(revealRemainingTigers());
	
		tigerWinBoxAnimator.Play(TIGER_WIN_BOX_GLOW_ANIM_NAME);
			
		yield return StartCoroutine(
			animateScore(
			BonusGamePresenter.instance.currentPayout,
			BonusGamePresenter.instance.currentPayout + credits));
		BonusGamePresenter.instance.currentPayout += credits;
		
		yield return StartCoroutine(Wait());
		yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);
		
		tigerWinBoxAnimator.Play(TIGER_WIN_BOX_IDLE_ANIM_NAME);
		BonusGamePresenter.instance.gameEnded();
	}

/*==========================================================================================================*\
	Reveal Remaining Tigers
\*==========================================================================================================*/
	
	protected IEnumerator revealRemainingTigers()
	{
		revealWait.reset();
		
		for (int iTiger = 0; iTiger < wheelPick.wins.Count; iTiger++)
		{
			if (iTiger != wheelPick.winIndex)
			{
				StartCoroutine(revealRemainingTiger(iTiger));
				yield return StartCoroutine(revealWait.wait(WAIT_FOR_REVEAL_TIGER));
			}
		}
		
		isWaiting = false;
	}
	
	protected IEnumerator revealRemainingTiger(int iTiger)
	{
		PickGameButtonData tigerPick = removeNextPickGameButton();
		
		if (tigerPick != null)
		{
			long credits = wheelPick.wins[iTiger].credits;
			int multiplier = wheelPick.wins[iTiger].multiplier;
				
			if (credits > 0)
			{			
				tigerPick.extraLabel.text = CreditsEconomy.convertCredits(credits);
				tigerPick.animator.Play(REVEAL_CREDITS_ANIM_NAME);
			}
			else if (multiplier > 0)
			{
				tigerPick.animator.Play(REVEAL_MULTIPLIER_ANIM_NAME);
			}

			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_RELIC_SOUND_NAME);
			}
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(tigerPick.animator));
		}
	}
	
/*==========================================================================================================*/

}

