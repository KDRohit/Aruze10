using UnityEngine;
using System.Collections;

public class Mm01StuffPickemGame : GenericPickemOutcomePickemGame 
{
	[SerializeField] private Animator characterAnimator;

	[SerializeField] private GameObject turntableAnchor;
	[SerializeField] private GameObject turntablePrefab;
	[SerializeField] private Animator jackpotAnimator;

	private GameObject turntableObject = null;

	public enum DancerEnum
	{
		None = -1,
		Dancer = 0,

		Mike = 1,
		Ken = 2,
		Richie = 3,
		Tarzan = 4,
		Tito = 5,

		Turntable = 6
	};

	private static readonly string[] DANCER_ANIM_NAMES =
	{
		"",
		"M1",
		"M2",
		"M3",
		"M4",
		"M5",
	};

	private static readonly string[] PICKED_JACKPOT_ANIM_NAMES =
	{
		"",
		"revealM1",
		"revealM2",
		"revealM3",
		"revealM4",
		"revealM5",
	};

	private static readonly string[] UNPICKED_JACKPOT_ANIM_NAMES =
	{
		"",
		"revealM1Gray",
		"revealM2Gray",
		"revealM3Gray",
		"revealM4Gray",
		"revealM5Gray",
	};

	private const string ADVANCE_JACKPOT_SOUND = "ConventionAdvanceJackpotSparklyXXL";
	private const string JACKPOT_UPDATE_ANIMATION = "updateNumber";

	public DancerEnum dancerEnum = DancerEnum.None;
	private long currentJackpotAmount;
	private long creditsFromJackpot;
	
	protected override bool hasMorePicksThisGame()
	{
			return !hasGameEnded;
	}		

	public override void init(PickemOutcome passedOutcome)
	{
		currentStage = (int)dancerEnum;
		outcomeType = BonusOutcomeTypeEnum.PickemOutcomeType;

		defaultPickmeAnimName = pickMeAnimName;

		base.init(passedOutcome);
		initStage();

		gameObject.SetActive(true);
		characterAnimator.Play(DANCER_ANIM_NAMES[currentStage]);

		currentJackpotAmount = jackpotCredits;

		animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME = PICKED_JACKPOT_ANIM_NAMES[currentStage];
		animationNamesByRound[currentStage].REVEAL_UNPICKED_SPECIAL1_ANIM_NAME = UNPICKED_JACKPOT_ANIM_NAMES[currentStage];

		inputEnabled = true;
	}

	// We assume any credits used here, have been PRE-multiplied
	public void updateJackpotValue(long creditsToAdd)
	{
		StartCoroutine(addCredits(creditsFromJackpot));
		currentJackpotAmount += creditsToAdd;
		jackpotCredits = currentJackpotAmount;

		if (jackpotAnimator != null)
		{
			jackpotAnimator.Play(JACKPOT_UPDATE_ANIMATION);
		}

		foreach (UILabel jackpotLabel in jackpotLabels)
		{
			jackpotLabel.text = CreditsEconomy.convertCredits(currentJackpotAmount);
		}


		Audio.play(ADVANCE_JACKPOT_SOUND);
	}

	public override void continueToNextStage()
	{
		if ((int)DancerEnum.Dancer < currentStage && currentStage < (int)DancerEnum.Turntable)
		{
			// Deactivating the stuff pickem doesn't work.
			//    gameObject.SetActive(false);
			// When you reactivate it, it doesn't return to its previous state.
			// Move it out of the way, instead, to preserve its current state.

			// TODO - If this isn't deactivated, then the pickem sound needs to get disabled while this scren doesn't have focus.
			// transform.localPosition += new Vector3(0.0f, 5000.0f, 0.0f);

			turntableObject = CommonGameObject.instantiate(turntablePrefab) as GameObject;
			turntableObject.transform.parent = turntableAnchor.transform;

			turntableObject.transform.localPosition = Vector3.zero;
			turntableObject.transform.localScale = Vector3.one;

			Mm01TurntablePickemGame turntablePickemGame =
				turntableObject.GetComponent<Mm01TurntablePickemGame>();

			turntablePickemGame.stuffPickemGame = this;
			bool isEnd = (getNumEntries() == 0);
			turntablePickemGame.init(isEnd);
		}
	}

	// Only minor differences in the SAVE outcome, but needed to copy the whole function.
	protected override IEnumerator creditsOrAdvanceOrJackpotEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		pickData = getNextEntry();
		PickemPick currentPickem = pickData as PickemPick;
		long credits = pickData.credits;

		if (currentPickem.groupId == "JACKPOT")
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			if (currentStage < jackpotWinEffects.Length)
			{
				if (jackpotWinEffects[currentStage] != null)
				{
					jackpotWinEffects[currentStage].SetActive(true);
				}
			}
			
			//if (currentStage < sparkleTrailDefinitionsByRound.Length)
			//{
			//	yield return StartCoroutine(doSparkleTrail());
			//	StartCoroutine(doPostSparkleTrailEffects());
			//}
			yield return StartCoroutine(addCredits(jackpotCredits));
			yield return StartCoroutine(revealRemainingPicks());
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			
			hasGameEnded = true;
			yield return StartCoroutine(revealRemainingPicks());
		}
		else if (currentPickem.groupId == "SAVE")
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL2_ANIM_NAME);
			
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_SPECIAL2_SOUND_DELAY);
				
			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_SPECIAL_2_VO_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_SPECIAL2_VO_SOUND_DELAY);
				
			playSceneSounds(SoundDefinition.PlayType.Advance);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);
			//if (credits > 0)
			//{
			//	yield return StartCoroutine(addCredits(credits));
			//}
			creditsFromJackpot = credits; // Need to keep this so we can roll up when we come out.

			continueToNextStage();
			//initStage();
		}
		else if (credits > 0)
		{
			pick.revealNumberLabel.text = CreditsEconomy.convertCredits(credits);
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_CREDITS_ANIM_NAME);

			Audio.play(
				getSoundMappingByRound(PICKEM_PICK_CREDITS_MAPPING_PREFIX),
				1.0f, 0.0f,
				soundDelaysByRound[currentStage].REVEAL_CREDITS_SOUND_DELAY);
			
			playSceneSounds(SoundDefinition.PlayType.Credits);
			
			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(addCredits(credits));

			if (getNumEntries() > 0)
			{
				inputEnabled = true;
			}
			else
			{
				hasGameEnded = true;
			}
		}
	}

	public void onReturnFromTurntable()
	{
		inputEnabled = true;
	}

	public PickemOutcome getPickemOutcome()
	{
		return pickemOutcome;
	}
	
}
