using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mm01TurntablePickemGame : GenericPickemOutcomePickemGame 
{
	public Mm01StuffPickemGame stuffPickemGame;
	protected bool shouldEnd;

	private const string TRANSITION_SOUND = "ConventionRevealEncore";
	private const string RETREAT_VO_SOUND = "PickReturnToP2VOEL03";
	
	private readonly string[] GAME_OVER_VO_SOUNDS = 
	{
		"MMGodsWay2ndChanceMyrtle",
		"KNImStillPretty",
		"RCNoActualSexForFiveMonths",
		"TZSomebodyHookedUpTrustFndBby",
		"TTHealthyMobileBlockParty"
	};
	private const string REVEAL_OTHERS_SOUND = "pickem_pickme7"; // Just hard coding this in here since we don't have time to rework the way REVEAL_SOUND_NAME works. (Chris want it to match the pickme sound.)
	private const float VO_DELAY = 0.6f;

	public void init(bool isEnd)
	{
		shouldEnd = isEnd;

		currentStage = (int)Mm01StuffPickemGame.DancerEnum.Turntable;
		outcomeType = BonusOutcomeTypeEnum.PickemOutcomeType;
		
		base.init();
		initStage();
		pickemOutcome = stuffPickemGame.getPickemOutcome();
		pickData = stuffPickemGame.pickData;
		REVEAL_SOUND_NAME_PREFIX = REVEAL_OTHERS_SOUND;
		inputEnabled = true;
	}
	
	protected override bool hasMorePicksThisGame()
	{
		return !hasGameEnded;
	}		

	protected override IEnumerator retreatOrEndButtonPressedCoroutine(GameObject pickButton)
	{
		inputEnabled = false;
		
		int pickIndex = getButtonIndex(pickButton);
		removeButtonFromSelectableList(pickButton);
		PickGameButtonData pick = getPickGameButton(pickIndex);
		
		if (pickData.credits != 0)
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_SPECIAL1_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_SPECIAL_1_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_SPECIAL1_VO_SOUND_DELAY);
			
			Audio.play(TRANSITION_SOUND);

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			yield return StartCoroutine(revealRemainingPicks());
		}
		else
		{
			pick.animator.Play(animationNamesByRound[currentStage].REVEAL_PICKED_END_ANIM_NAME);
			
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_SOUND_DELAY);
			Audio.play(getSoundMappingByRound(PICKEM_PICK_BAD_VO_MAPPING_PREFIX), 1.0f, 0.0f, soundDelaysByRound[currentStage].REVEAL_BAD_VO_SOUND_DELAY);

			switch((int)stuffPickemGame.dancerEnum)
			{
				case (int)Mm01StuffPickemGame.DancerEnum.Mike: 
					Audio.play(GAME_OVER_VO_SOUNDS[0], 1.0f, 0.0f, VO_DELAY);
					break;
				case (int)Mm01StuffPickemGame.DancerEnum.Ken: 
					Audio.play(GAME_OVER_VO_SOUNDS[1], 1.0f, 0.0f, VO_DELAY);
					break;
				case (int)Mm01StuffPickemGame.DancerEnum.Richie: 
					Audio.play(GAME_OVER_VO_SOUNDS[2], 1.0f, 0.0f, VO_DELAY);
					break;
				case (int)Mm01StuffPickemGame.DancerEnum.Tarzan: 
					Audio.play(GAME_OVER_VO_SOUNDS[3], 1.0f, 0.0f, VO_DELAY);
					break;
				case (int)Mm01StuffPickemGame.DancerEnum.Tito: 
					Audio.play(GAME_OVER_VO_SOUNDS[4], 1.0f, 0.0f, VO_DELAY);
					break;
			}

			yield return StartCoroutine(CommonAnimation.waitForAnimDur(pick.animator));
			
			hasGameEnded = true;
			yield return StartCoroutine(revealRemainingPicks());
		}

		yield return new WaitForSeconds(revealDelaysByRound[currentStage].WAIT_TO_ADVANCE_DUR);

		if (!hasGameEnded)
		{
			Destroy(gameObject);

			stuffPickemGame.gameObject.SetActive(true);
			stuffPickemGame.updateJackpotValue((pickData as PickemPick).jackpotIncrease);
			stuffPickemGame.onReturnFromTurntable();
			//stuffPickemGame.transform.localPosition -= new Vector3(0.0f, 5000.0f, 0.0f);
			
			Audio.switchMusicKeyImmediate(getSoundMappingByRound(PICKEM_BG_MUSIC_MAPPING_PREFIX, 1));
			Audio.play(RETREAT_VO_SOUND);
			
			Audio.play(Mm01DancerPickemGame.REVEAL_CHARACTER_VO_SOUND_LIST[(int)stuffPickemGame.dancerEnum]);
		}
	}
			
}
