using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Mm01DancerPickemGame : GenericWheelOutcomePickemGame 
{
	[SerializeField] private Mm01StuffPickemGame stuffPickemGame;

	List<string> revealAnimNames = new List<string>();

	private static readonly string[] DANCER_ANIM_NAMES =
	{
		"",
		"pickM1",
		"pickM2",
		"pickM3",
		"pickM4",
		"pickM5",
	};

	// Should be gray anims, but we don't have gray anims yet.
	private static readonly string[] REVEAL_ANIM_NAMES =
	{
		"",
		"revealM1",
		"revealM2",
		"revealM3",
		"revealM4",
		"revealM5",
	};

	// Voice over per character played when a character icon is found
	public static readonly string[] REVEAL_CHARACTER_VO_SOUND_LIST = 
	{
		"",
		"MMHorribleKnowMCSavannah",
		"KNSoThankfulAllTogetherLastRide",
		"RCNoUniverseNotDoingFireman",
		"TZAsGoodARunAsAnybody",
		"TTIfReasonCallFadedMeant2B" 
	};
	
	protected override bool hasMorePicksThisGame()
	{
		return true;  // no pick can end this game, always advance.
	}		
	

	protected override IEnumerator singlePickAdvanceButtonPressedCoroutine(GameObject pickButton)
	{
		WheelPick wheelPick = outcome.getNextEntry();

		switch (wheelPick.bonusGame)
		{
			case "mm01_mike_pickem":
				stuffPickemGame.dancerEnum = Mm01StuffPickemGame.DancerEnum.Mike;
				break;

			case "mm01_ken_pickem":
				stuffPickemGame.dancerEnum = Mm01StuffPickemGame.DancerEnum.Ken;
				break;

			case "mm01_richie_pickem":
				stuffPickemGame.dancerEnum = Mm01StuffPickemGame.DancerEnum.Richie;
				break;

			case "mm01_tarzan_pickem":
				stuffPickemGame.dancerEnum = Mm01StuffPickemGame.DancerEnum.Tarzan;
				break;

			case "mm01_tito_pickem":
				stuffPickemGame.dancerEnum = Mm01StuffPickemGame.DancerEnum.Tito;
				break;
		}

		int dancerIndex = (int)stuffPickemGame.dancerEnum;
		Audio.play(REVEAL_CHARACTER_VO_SOUND_LIST[dancerIndex]);
		animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME = DANCER_ANIM_NAMES[dancerIndex];

		for (int iReveal = 1; iReveal < REVEAL_ANIM_NAMES.Length; iReveal++)
		{
			if (iReveal != dancerIndex)
			{
				revealAnimNames.Add(REVEAL_ANIM_NAMES[iReveal]);
			}
		}
		CommonDataStructures.shuffleList<string>(revealAnimNames);
		
		yield return StartCoroutine(base.singlePickAdvanceButtonPressedCoroutine(pickButton));
		yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);

		gameObject.SetActive(false);
		stuffPickemGame.gameObject.SetActive(true);

		SlotOutcome pickemGame = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame);
		pickemOutcome = new PickemOutcome(pickemGame);

		stuffPickemGame.init(pickemOutcome);
	}
	
	protected override void singlePickAdvanceRevealRemainingPick(PickGameButtonData pick)
	{
		// The server doesn't send any reveals,
		// so randomly reveal the remaining dancers.

		pick.animator.Play(revealAnimNames[0]);
		playNotChosenAudio();
		revealAnimNames.RemoveAt(0);
	}	
}
