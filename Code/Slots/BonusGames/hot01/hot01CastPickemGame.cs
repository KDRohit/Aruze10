using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

public class hot01CastPickemGame : PickingGame<NewBaseBonusGameOutcome>
{
	// Tunables
	
	public float WAIT_TO_REVEAL_DUR = 1.0f;
	
	public float WAIT_TO_ADVANCE_DUR = 1.0f;
	public float WAIT_TO_END_GAME_DUR = 1.0f;
	
	// Game Objects
	
	public GameObject castUI;
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
	
	public UILabel jackpotLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent jackpotLabelWrapperComponent;

	public LabelWrapper jackpotLabelWrapper
	{
		get
		{
			if (_jackpotLabelWrapper == null)
			{
				if (jackpotLabelWrapperComponent != null)
				{
					_jackpotLabelWrapper = jackpotLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotLabelWrapper = new LabelWrapper(jackpotLabel);
				}
			}
			return _jackpotLabelWrapper;
		}
	}
	private LabelWrapper _jackpotLabelWrapper = null;
	

	public GameObject trackUI;
	public UILabel trackWinLabel;		// To be removed when prefabs are updated.
	public LabelWrapperComponent trackWinLabelWrapperComponent;	

	public LabelWrapper trackWinLabelWrapper
	{
		get
		{
			if (_trackWinLabelWrapper == null)
			{
				if (trackWinLabelWrapperComponent != null)
				{
					_trackWinLabelWrapper = trackWinLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_trackWinLabelWrapper = new LabelWrapper(trackWinLabel);
				}
			}
			return _trackWinLabelWrapper;
		}
	}
	private LabelWrapper _trackWinLabelWrapper = null;
	
	
	// Variables
	
	private RoundPicks roundPicks;
	private BasePick basePick;
	private long jackpotCredits;
	
	// Constants
	
	private enum StageEnum
	{
		Mirrors = 0,
		Shoes = 1,
		Glasses = 2,
		TrackSuits = 3
	};
	
	private static readonly string[] CHARACTER_NAMES =
	{
		"victoria",
		"melanie",
		"joy"
	};
	
	private static readonly string[] PICK_ME_ANIM_NAMES =
	{
		"picking object_pick me",
		"picking object_pick me",
		"picking object_pick me",
		"betty_picking object_pick me"
	};
	
	private const string PICK_CHARACTER_ANIM_NAME = "picking object_reveal_character";
	private const string PICK_CREDITS_ANIM_NAME = "picking object_reveal_number";
	private const string PICK_END_ANIM_NAME = "picking object_reveal_end";
	private const string PICK_TRACK_MULTIPLIER_ANIM_NAME = "betty_picking object_betty";
	private const string PICK_TRACK_CREDITS_ANIM_NAME = "betty_picking object_number";
	
	private const string REVEAL_CHARACTER_ANIM_NAME = "picking object_notSelected_character";
	private const string REVEAL_CREDITS_ANIM_NAME = "picking object_notSelected_number";
	private const string REVEAL_END_ANIM_NAME = "picking object_notSelected_end";
	private const string REVEAL_TRACK_MULTIPLIER_ANIM_NAME = "betty_picking object_notSelectBetty";
	private const string REVEAL_TRACK_CREDITS_ANIM_NAME = "betty_picking object_notSelectNumber";
	
	private static readonly string[] BACKGROUND_MUSIC_NAMES =
	{
		"BonusBg1HIC",
		"BonusBg2HIC",
		"BonusBg3HIC",
		"BonusBg4HIC"
	};	
	private const string INTRO_VO_NAME = "JYWillBeFun";
	
	private static readonly string[] PICK_ME_SOUND_NAMES =
	{
		"PickMeRound1",
		"PickMeRound2",
		"PickMeRound3",
		"PickMeRound4"
	};

	private static readonly string[] PICK_CHARACTER_SOUND_NAMES =
	{
		"PickVictoria",
		"PickMelanie",
		"PickJoy"
	};
	private static readonly string[] PICK_CHARACTER_VO_NAMES =
	{
		"BonusPickVictoriaVOHIC",
		"BonusPickMelanieVOHIC",
		"BonusPickJoyVOHIC"
	};
	private static readonly string[] PICK_CREDITS_SOUND_NAMES =
	{
		"PickemRound1Credits",
		"PickemRound2Credits",
		"PickemRound3Credits"
	};
	public const string PICK_END_SOUND_NAME = "PickemBrokenHeart";
	public const string PICK_END_VO_NAME = "BonusOverVOHIC";
		
	public const string PICK_TRACK_MULTIPLIER_SOUND_NAME = "FinalPickemMultiplier";
	public const string PICK_TRACK_CREDITS_SOUND_NAME = "PickemRound4Credits";
	
	public const string REVEAL_SOUND_NAME = "reveal_not_chosen";

	// See HIR-17162
	// private const string BONUS_SUMMARY_VO_SOUND_KEY = "bonus_summary_vo";
	
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/

	public override void init()
	{
		base.init();

		// force load the wings, since they don't switch automatically
		if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.forceShowChallengeWings(true);
		}

		initStage();
		
		Audio.play(INTRO_VO_NAME);
	}
	
	protected void initStage()
	{
		roundPicks = outcome.roundPicks[currentStage];
		
		if (currentStage < (int)StageEnum.TrackSuits)
		{
			if (currentStage < titleLabelsWrapper.Count)
			{
				titleLabelsWrapper[currentStage].text = Localize.textUpper("find_{0}", CHARACTER_NAMES[currentStage]);
			}
		
			jackpotCredits = 0;
			for (int iPick = 0; iPick < roundPicks.entries.Count; iPick++)
			{
				long credits = roundPicks.entries[iPick].credits;
				if (credits > jackpotCredits)
				{
					jackpotCredits = credits;
				}
			}
			for (int iPick=0; iPick < roundPicks.reveals.Count; iPick++)
			{
				long credits = roundPicks.reveals[iPick].credits;
				if (credits > jackpotCredits)
				{
					jackpotCredits = credits;
				}
			}
			jackpotLabelWrapper.text = CreditsEconomy.convertCredits(jackpotCredits);
		}
		else
		{
			// force show the second set of challenge wings
			if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
			{
				BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(true);
			}

			castUI.SetActive(false);
			trackUI.SetActive(true);
			
			trackWinLabelWrapper.text = currentWinAmountTextWrapperNew.text;
			currentWinAmountTextWrapperNew = trackWinLabelWrapper;
		}
				
		pickMeAnimName = PICK_ME_ANIM_NAMES[currentStage];
		pickMeSoundName = PICK_ME_SOUND_NAMES[currentStage];
		
		Audio.switchMusicKeyImmediate(Audio.soundMap(BACKGROUND_MUSIC_NAMES[currentStage]));
		inputEnabled = true;
	}
	
/*==========================================================================================================*\
	Pickem Button Pressed Coroutine
\*==========================================================================================================*/

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		if (currentStage < (int)StageEnum.TrackSuits)
		{
			yield return StartCoroutine(castButtonPressedCoroutine(pickButton));
		}
		else
		{
			yield return StartCoroutine(trackButtonPressedCoroutine(pickButton));
		}
	}

/*==========================================================================================================*\
	Cast Button Pressed Coroutine
\*==========================================================================================================*/
	
	protected IEnumerator castButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		basePick = roundPicks.getNextEntry();
		long credits = basePick.credits;

		if (credits > 0)
		{
			if (credits == jackpotCredits)
			{
				pick.animator.Play(PICK_CHARACTER_ANIM_NAME);
				
				Audio.play(PICK_CHARACTER_SOUND_NAMES[currentStage]);
				Audio.play(PICK_CHARACTER_VO_NAMES[currentStage]);

				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
				jackpotWinEffects[currentStage].SetActive(true);
				
				yield return StartCoroutine(addCredits(credits));
				yield return StartCoroutine(revealRemainingPicks());
				yield return new WaitForSeconds(WAIT_TO_ADVANCE_DUR);
				
				jackpotWinEffects[currentStage].SetActive(false);
				
				continueToNextStage();
				initStage();
			}
			else
			{
				pick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
				
				pick.animator.Play(PICK_CREDITS_ANIM_NAME);
				Audio.play(PICK_CREDITS_SOUND_NAMES[currentStage]);

				yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
				yield return StartCoroutine(addCredits(credits));

				inputEnabled = true;
			}
		}
		else
		if (credits == 0)
		{
			pick.animator.Play(PICK_END_ANIM_NAME);
			
			Audio.play(PICK_END_SOUND_NAME);
			Audio.play(PICK_END_VO_NAME);

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(revealRemainingPicks());
			
			yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);
			BonusGamePresenter.instance.gameEnded();
		}
	}
	
	protected IEnumerator revealRemainingPicks()
	{
		revealWait.reset();

		PickGameButtonData pick = removeNextPickGameButton();
		
		while (pick != null)
		{
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
			
			revealRemainingPick(pick);
			pick = removeNextPickGameButton();
		}			
	}
	
	protected void revealRemainingPick(PickGameButtonData pick)
	{
		basePick = roundPicks.getNextReveal();
		
		if (basePick != null)
		{
			long credits = basePick.credits;
			
			if (credits > 0)
			{
				if (credits == jackpotCredits)
				{
					pick.animator.Play(REVEAL_CHARACTER_ANIM_NAME);
					if(!revealWait.isSkipping)
					{
						Audio.play(Audio.soundMap(REVEAL_SOUND_NAME));
					}
				}
				else
				{
					pick.extraLabel.text = CreditsEconomy.convertCredits(credits);
					
					pick.animator.Play(REVEAL_CREDITS_ANIM_NAME);
					if(!revealWait.isSkipping)
					{
						Audio.play(Audio.soundMap(REVEAL_SOUND_NAME));
					}
				}
			}
			else
			if (credits == 0)
			{
				pick.animator.Play(REVEAL_END_ANIM_NAME);
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_SOUND_NAME));
				}
			}
		}
	}

/*==========================================================================================================*\
	Track Button Pressed Coroutine
\*==========================================================================================================*/
	
	protected IEnumerator trackButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		basePick = roundPicks.getNextEntry();
		int multiplier = basePick.multiplier;
		long credits = basePick.credits;
		
		if (multiplier > 0)
		{
			pick.animator.Play(PICK_TRACK_MULTIPLIER_ANIM_NAME);
			Audio.play(PICK_TRACK_MULTIPLIER_SOUND_NAME);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			jackpotWinEffects[currentStage].SetActive(true);
			yield return StartCoroutine(addCredits(BonusGamePresenter.instance.currentPayout));
		}
		else if (credits > 0)
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
				
			pick.animator.Play(PICK_TRACK_CREDITS_ANIM_NAME);
			Audio.play(PICK_TRACK_CREDITS_SOUND_NAME);
				
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(addCredits(credits));
		}
		
		yield return StartCoroutine(revealRemainingTrackSuits());
		yield return new WaitForSeconds(WAIT_TO_END_GAME_DUR);
		
		jackpotWinEffects[currentStage].SetActive(false);

		// See HIR-17162
		// Audio.play(Audio.soundMap(BONUS_SUMMARY_VO_SOUND_KEY));
		
		BonusGamePresenter.instance.gameEnded();
	}
	
	protected IEnumerator revealRemainingTrackSuits()
	{
		revealWait.reset();
		
		PickGameButtonData pick = removeNextPickGameButton();
		
		while (pick != null)
		{
			yield return StartCoroutine(revealWait.wait(revealWaitTime));

			
			revealRemainingTrackSuit(pick);
			pick = removeNextPickGameButton();
		}			
	}
	
	protected void revealRemainingTrackSuit(PickGameButtonData pick)
	{
		basePick = roundPicks.getNextReveal();
		
		if (basePick != null)
		{
			int multiplier = basePick.multiplier;
			long credits = basePick.credits;
			
			if (multiplier > 0)
			{
				pick.animator.Play(REVEAL_TRACK_MULTIPLIER_ANIM_NAME);
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_SOUND_NAME));
				}
			}
			else if (credits > 0)
			{
				pick.extraLabel.text = CreditsEconomy.convertCredits(credits);
					
				pick.animator.Play(REVEAL_TRACK_CREDITS_ANIM_NAME);
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(REVEAL_SOUND_NAME));
				}
			}
		}
	}
		
/*==========================================================================================================*/

}

