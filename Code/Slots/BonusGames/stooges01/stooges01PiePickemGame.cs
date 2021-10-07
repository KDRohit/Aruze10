using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class stooges01PiePickemGame : PickingGame<WheelOutcome>
{
	// Tunables
	
	[SerializeField] private float WAIT_TO_PLAY_INTRO_VO_DUR = 1.0f;
	
	[SerializeField] private float WAIT_FOR_REVEAL_PLATTER_SOUND_DUR = 0.5f;
	[SerializeField] private float WAIT_FOR_PLATTER_PICK_DUR = 1.0f;
	[SerializeField] private float WAIT_FROM_PLATTERS_TO_PIES_DUR = 1.0f;

	[SerializeField] private float WAIT_FOR_REVEAL_PIE_DUR = 0.5f;
	[SerializeField] private float WAIT_FOR_PIE_PICK_DUR = 1.0f;
	
	[SerializeField] private float WAIT_FOR_CHAR_PICK_DUR = 1.0f;
	[SerializeField] private float WAIT_TO_THROW_PIE_DUR = 1.0f;
	[SerializeField] private float WAIT_FOR_PIE_THROW_DUR = 1.0f;
	[SerializeField] private float WAIT_FOR_PIE_SPLAT_DUR = 1.0f;
	[SerializeField] private float WAIT_FOR_PIE_MESSY_DUR = 1.0f;
	[SerializeField] private float WAIT_FOR_JACKPOT_DUR = 1.0f;

	[SerializeField] private float WAIT_FOR_UNTHROWN_PIE_DUR = 1.0f;	
	[SerializeField] private float WAIT_TO_END_GAME = 1.0f;
	
	// Game Objects
	
	[SerializeField] private UILabel jackpotAmountLabel = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotAmountLabelWrapperComponent = null;

	public LabelWrapper jackpotAmountLabelWrapper
	{
		get
		{
			if (_jackpotAmountLabelWrapper == null)
			{
				if (jackpotAmountLabelWrapperComponent != null)
				{
					_jackpotAmountLabelWrapper = jackpotAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotAmountLabelWrapper = new LabelWrapper(jackpotAmountLabel);
				}
			}
			return _jackpotAmountLabelWrapper;
		}
	}
	private LabelWrapper _jackpotAmountLabelWrapper = null;
	
	[SerializeField] private UILabel numPicksLabel = null;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent numPicksLabelWrapperComponent = null;

	public LabelWrapper numPicksLabelWrapper
	{
		get
		{
			if (_numPicksLabelWrapper == null)
			{
				if (numPicksLabelWrapperComponent != null)
				{
					_numPicksLabelWrapper = numPicksLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_numPicksLabelWrapper = new LabelWrapper(numPicksLabel);
				}
			}
			return _numPicksLabelWrapper;
		}
	}
	private LabelWrapper _numPicksLabelWrapper = null;
	
	[SerializeField] private Animator[] pieThrows;
	
	// Variables
	
	private List<int> possibleNumPicksList = new List<int>();
	private List<CharacterEnum> characterList = new List<CharacterEnum>();

	private PickemOutcome pickemOutcome;

	private long jackpotAmount = 0;
	private int numPicks = 0;
	
	// Constants
	
	private const string INTRO_VO_NAME = "BonusIntroVOStooges";
	private const string PLATTER_MUSIC_NAME = "BonusIdleStooges";

	private const string PLATTER_PICK_ME_ANIM_NAME = "pickme";
	private const string PLATTER_PICK_ME_SOUND_NAME = "PiePickMe";
	private const string PLATTER_REVEAL_PICK_ANIM_NAME = "reveal";
	private const string PLATTER_REVEAL_PICK_SOUND_NAME = "PickAPie";
	private const string PLATTER_REVEAL_NUM_PIES_SOUND_NAME = "PieRevealNumPicks";
	private const string PLATTER_REVEAL_REMAINING_SOUND_NAME = "reveal_not_chosen";
	
	private const string PIE_MUSIC_NAME = "BonusBgStooges";
	private const string PLATTER_REVEAL_REMAINING_ANIM_NAME = "revealGray";
	
	private const string PIE_PICK_ME_ANIM_NAME = "pickme";
	private const string PIE_PICK_ME_SOUND_NAME = "PiePickMe";
	
	private const string PIE_REVEAL_PICK_ANIM_NAME = "revealCredit";
	private const string PIE_REVEAL_PICK_SOUND_NAME = "PickAPie";
	private const string PIE_REVEAL_CREDITS_SOUND_NAME = "PiePickRevealCredit";
	
	private static readonly string[] CHARACTER_ANIM_NAMES =
	{
		"revealMoe",
		"revealLarry",
		"revealCurly"
	};
	private const string PIE_REVEAL_CHARACTER_SOUND_NAME = "PiePickRevealFaceBuildup";
	private static readonly string[] CHARACTER_VO_NAMES =
	{
		"MHImThePresident",
		"LHLaugh",
		"CHCmonIDareYa"
	};
	
	private const string PIE_THROW_ANIM_NAME = "throw";
	private const string PIE_THROW_SOUND_NAME = "PiePickWhoosh";
	private const string PIE_SPLAT_SOUND_NAME = "PiePickSplat";
	private const string PIE_MESSY_SOUND_NAME = "PieLightCharacter";
	const string JACKPOT_SOUND_NAME = "PieCharacterJackpot";
	
	private const string PIE_REVEAL_REMAINING_SOUND_NAME = "reveal_not_chosen";
	
	private enum StageEnum
	{
		Platters = 0,
		Pies = 1,
	};
	
	private enum CharacterEnum
	{
		Moe = 0,
		Larry = 1,
		Curly = 2
	};

	private static readonly int[] POSSIBLE_NUM_PICKS_ARRAY =
	{
		8,
		9,
		10
	};
	
	private const string UNTHROWN_PIE_ANIM_NAME = "revealCreditGray";	
	private static readonly string[] UNTHROWN_CHARACTER_ANIM_NAMES =
	{
		"revealMoeGray",
		"revealLarryGray",
		"revealCurlyGray"
	};
	
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/
	
	public override void init()
	{
		base.init();
				
		Audio.play(INTRO_VO_NAME, 1.0f, 0.0f, WAIT_TO_PLAY_INTRO_VO_DUR);
		Audio.switchMusicKeyImmediate(Audio.soundMap(PLATTER_MUSIC_NAME));

		foreach (int possibleNumPicks in POSSIBLE_NUM_PICKS_ARRAY)
		{
			possibleNumPicksList.Add(possibleNumPicks);
		}
		CommonDataStructures.shuffleList<int>(possibleNumPicksList);
		
		characterList.Add(CharacterEnum.Moe);
		characterList.Add(CharacterEnum.Larry);
		characterList.Add(CharacterEnum.Curly);
		CommonDataStructures.shuffleList<CharacterEnum>(characterList);
	}
	
/*==========================================================================================================*\
	Pick Me Anim callback
\*==========================================================================================================*/
	
	protected override IEnumerator pickMeAnimCallback()
	{
		if (currentStage == (int)StageEnum.Platters)
		{
			yield return StartCoroutine(platterPickMeAnimCallback());
			
		}
		else
		{
			yield return StartCoroutine(piePickMeAnimCallback());
		}
	}		

/*==========================================================================================================*\
	Pickem Button Pressed Coroutine
\*==========================================================================================================*/

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		if (currentStage == (int)StageEnum.Platters)
		{
			yield return StartCoroutine(platterButtonPressedCoroutine(button));
			
		}
		else
		{
			yield return StartCoroutine(pieButtonPressedCoroutine(button));
		}
	}

/*==========================================================================================================*\
	Platters Pickem
\*==========================================================================================================*/
	
	protected IEnumerator platterPickMeAnimCallback()
	{
		if (inputEnabled)
		{
			PickGameButtonData platterPick = getRandomPickMe();
			
			if (platterPick != null)
			{
				platterPick.animator.Play(PLATTER_PICK_ME_ANIM_NAME);
				Audio.play(PLATTER_PICK_ME_SOUND_NAME);
				
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(platterPick.animator));				
			}
		}
	}

	private IEnumerator platterButtonPressedCoroutine(GameObject platterButton)
	{
		inputEnabled = false;
		
		int platterIndex = getButtonIndex(platterButton);
		removeButtonFromSelectableList(platterButton);
		PickGameButtonData platterPick = getPickGameButton(platterIndex);
		
		WheelPick wheelPick = outcome.getNextEntry();
		
		pickemOutcome = new PickemOutcome(
			SlotOutcome.getBonusGameOutcome(
				BonusGameManager.currentBonusGameOutcome,
				wheelPick.bonusGame));
		
		foreach (JSON paytableGroup in pickemOutcome.paytableGroups)
		{
			if (paytableGroup.getString("group_code", "") == "GIRL")
			{
				jackpotAmount = paytableGroup.getLong("credits", 0L);
				jackpotAmount *= GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				jackpotAmountLabelWrapper.text = CreditsEconomy.convertCredits(jackpotAmount);
			}
		}
		
		numPicks = pickemOutcome.entryCount;
		possibleNumPicksList.Remove(numPicks);
		
		yield return StartCoroutine(revealPlatter(platterPick));
		yield return StartCoroutine(revealRemainingPlatters());
		
		numPicksLabelWrapper.text = CommonText.formatNumber(numPicks);
	}
	
	private IEnumerator revealPlatter(PickGameButtonData platterPick)
	{
		platterPick.revealNumberLabel.text = CommonText.formatNumber(numPicks);
		
		platterPick.animator.Play(PLATTER_REVEAL_PICK_ANIM_NAME);
		Audio.play(PLATTER_REVEAL_PICK_SOUND_NAME);
		yield return new WaitForSeconds(WAIT_FOR_REVEAL_PLATTER_SOUND_DUR);
		
		Audio.play(PLATTER_REVEAL_NUM_PIES_SOUND_NAME);
		yield return new WaitForSeconds(WAIT_FOR_PLATTER_PICK_DUR);
	}

	private IEnumerator revealRemainingPlatters()
	{
		while (getNumPickMes() > 0)
		{
			PickGameButtonData platterPick = removeNextPickGameButton();
			StartCoroutine(revealRemainingPlatter(platterPick));
			
			yield return new WaitForSeconds(WAIT_FOR_PLATTER_PICK_DUR);
		}
		
		yield return new TIWaitForSeconds(WAIT_FROM_PLATTERS_TO_PIES_DUR);

		Audio.switchMusicKeyImmediate(PIE_MUSIC_NAME);
		continueToNextStage();
		
		inputEnabled = true;
	}
	
	protected IEnumerator revealRemainingPlatter(PickGameButtonData platterPick)
	{
		int possibleNumPicks = possibleNumPicksList[0];
		possibleNumPicksList.RemoveAt(0);
		
		platterPick.revealNumberLabel.text = CommonText.formatNumber(possibleNumPicks);
		
		platterPick.animator.Play(PLATTER_REVEAL_REMAINING_ANIM_NAME);
		Audio.play(Audio.soundMap(PLATTER_REVEAL_REMAINING_SOUND_NAME));
		
		const float PLATTER_WAIT_FOR_REVEAL_REMAINING_DUR = 1.0f;
		yield return new TIWaitForSeconds(PLATTER_WAIT_FOR_REVEAL_REMAINING_DUR);
	}
	
/*==========================================================================================================*\
	Pies Pickem
\*==========================================================================================================*/

	protected IEnumerator piePickMeAnimCallback()
	{
		if (inputEnabled)
		{
			PickGameButtonData piePick = getRandomPickMe();
			
			if (piePick != null)
			{
				piePick.animator.Play(PIE_PICK_ME_ANIM_NAME);
				Audio.play(PIE_PICK_ME_SOUND_NAME);

				yield return StartCoroutine(CommonAnimation.waitForAnimDur(piePick.animator));				
			}
		}
	}

	private IEnumerator pieButtonPressedCoroutine(GameObject pieButton)
	{
		inputEnabled = false;
		
		int pieIndex = getButtonIndex(pieButton);
		removeButtonFromSelectableList(pieButton);
		PickGameButtonData piePick = getPickGameButton(pieIndex);
		
		yield return StartCoroutine(revealPie(piePick));

		if (pickemOutcome.entryCount == 0)
		{
			yield return StartCoroutine(revealUnthrownPies());
			
			yield return new TIWaitForSeconds(WAIT_TO_END_GAME);
			BonusGamePresenter.instance.gameEnded();
			
			yield break;
		}
		
		inputEnabled = true;
	}
	
	private IEnumerator revealPie(PickGameButtonData piePick)
	{
		PickemPick pickemPick = pickemOutcome.getNextEntry();
		numPicksLabelWrapper.text = CommonText.formatNumber(pickemOutcome.entryCount);
		
		if (pickemPick.groupId == "GIRL")
		{
			CharacterEnum characterEnum = characterList[0];
			characterList.RemoveAt(0);
			
			Audio.play(PIE_REVEAL_CHARACTER_SOUND_NAME);
			
			piePick.animator.Play(CHARACTER_ANIM_NAMES[(int)characterEnum]);
			Audio.play(PIE_REVEAL_PICK_SOUND_NAME);
			yield return new WaitForSeconds(WAIT_FOR_CHAR_PICK_DUR);
			
			Audio.play(CHARACTER_VO_NAMES[(int)characterEnum]);
			yield return new WaitForSeconds(WAIT_TO_THROW_PIE_DUR);
			
			pieThrows[(int)characterEnum].Play(PIE_THROW_ANIM_NAME);
			Audio.play(PIE_THROW_SOUND_NAME);
			yield return new WaitForSeconds(WAIT_FOR_PIE_THROW_DUR);
			
			Audio.play(PIE_SPLAT_SOUND_NAME);
			yield return new WaitForSeconds(WAIT_FOR_PIE_SPLAT_DUR);

			Audio.play(PIE_MESSY_SOUND_NAME);
			
			
			if (characterList.Count == 0)
			{
				yield return new WaitForSeconds(WAIT_FOR_PIE_MESSY_DUR);
							
				jackpotWinEffects[currentStage].SetActive(true);
				Audio.play(JACKPOT_SOUND_NAME);

				yield return new WaitForSeconds(WAIT_FOR_JACKPOT_DUR);
				jackpotWinEffects[currentStage].SetActive(false);
				
				StartCoroutine(
					animateScore(
						BonusGamePresenter.instance.currentPayout,
						BonusGamePresenter.instance.currentPayout + jackpotAmount));
						
				BonusGamePresenter.instance.currentPayout += jackpotAmount;				
			}
		}
		else
		{
			piePick.revealNumberLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			
			piePick.animator.Play(PIE_REVEAL_PICK_ANIM_NAME);
			Audio.play(PIE_REVEAL_PICK_SOUND_NAME);
			yield return new WaitForSeconds(WAIT_FOR_REVEAL_PIE_DUR);
			
			Audio.play(PIE_REVEAL_CREDITS_SOUND_NAME);
			yield return new TIWaitForSeconds(WAIT_FOR_PIE_PICK_DUR);
			
			yield return StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + pickemPick.credits));
					
			BonusGamePresenter.instance.currentPayout += pickemPick.credits;
		}
	}

	private IEnumerator revealUnthrownPies()
	{
		while (getNumPickMes() > 0)
		{
			StartCoroutine(revealUnthrownPie());
			yield return StartCoroutine(revealWait.wait(WAIT_FOR_UNTHROWN_PIE_DUR));
		}
	}
	
	private IEnumerator revealUnthrownPie()
	{
		PickemPick pickemPick = pickemOutcome.getNextReveal();
		PickGameButtonData piePick = removeNextPickGameButton();
		
		if (pickemPick.groupId == "GIRL")
		{
			CharacterEnum characterEnum = characterList[0];
			characterList.RemoveAt(0);
						
			piePick.animator.Play(UNTHROWN_CHARACTER_ANIM_NAMES[(int)characterEnum]);
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(PIE_REVEAL_REMAINING_SOUND_NAME));
			}
		}
		else
		{
			piePick.revealNumberLabel.text = CreditsEconomy.convertCredits(pickemPick.credits);
			
			piePick.animator.Play(UNTHROWN_PIE_ANIM_NAME);
			if(!revealWait.isSkipping)
			{
			Audio.play(Audio.soundMap(PIE_REVEAL_REMAINING_SOUND_NAME));
			}
		}
		
		yield break;
	}
	
/*==========================================================================================================*/

}

