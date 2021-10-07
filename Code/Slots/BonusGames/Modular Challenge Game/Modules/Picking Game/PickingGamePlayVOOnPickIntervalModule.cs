using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickingGamePlayVOOnPickIntervalModule : PickingGameModule
{
	[SerializeField] private string REVEAL_VO_AUDIO = "pickem_credits_vo_pick";
	[SerializeField] private float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] protected int pickInterval = 0;
	[SerializeField] protected bool countNonCreditPicks = false;

	private int runningPickCount = 0;

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		if(currentPick != null && shouldCount(currentPick))
		{
			runningPickCount++;

			if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO) && runningPickCount == pickInterval)
			{
				// play the associated audio voiceover
				Audio.playWithDelay(Audio.soundMap(REVEAL_VO_AUDIO), REVEAL_VO_DELAY);
				runningPickCount = 0;
			}
		}	
		yield return null;
	}

	//This is for readability.  Since the check to determine if a pick is "credits" only gets a bit lengthy
	private bool shouldCount(ModularChallengeGameOutcomeEntry pickData)
	{
		//If we don't care about what kind of pick move along.
		if (countNonCreditPicks)
		{
			return true;
		}
		else
		{
			//Make sure we have a credits only pick, we aren't an advance round pick, we aren't additional picks, and we aren't the game over pick
			if ((pickData.credits > 0) && (pickData.pickemGroupId == "") && !pickData.canAdvance && (pickData.additonalPicks == 0) && !pickData.isGameOver)
			{
				return true;
			}

			return false;
		}
	}
}
