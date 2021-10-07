using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module intended to allow for control of the delay when advancing to another round
vs advancing to the end game and summary screen (for instance in games that want
to move to the next round more quickly, but delay a bit when the game ends).
NOTE: Should probably set DELAY_BEFORE_ADVANCE_ROUND on the Variant to 0 in order
to ensure that you aren't doing two delays

Creation Date: 2/22/2018
Original Author: Scott Lepthien
*/
public class ChallengeGameDifferentAdvanceRoundAndEndGameTimesModule : ChallengeGameModule 
{
	[SerializeField] private float DELAY_BEFORE_ADVANCE_TO_ANOTHER_ROUND = 0.0f;
	[SerializeField] private float DELAY_BEFORE_END_GAME = 0.0f;

	// executeOnRoundEnd() section
	// executes right when a round starts or finishes initing.
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return true;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame)
		{
			if (DELAY_BEFORE_END_GAME > 0.0f)
			{
				yield return new TIWaitForSeconds(DELAY_BEFORE_END_GAME);
			}
		}
		else
		{
			if (DELAY_BEFORE_ADVANCE_TO_ANOTHER_ROUND > 0.0f)
			{
				yield return new TIWaitForSeconds(DELAY_BEFORE_ADVANCE_TO_ANOTHER_ROUND);
			}
		}
	}
}
