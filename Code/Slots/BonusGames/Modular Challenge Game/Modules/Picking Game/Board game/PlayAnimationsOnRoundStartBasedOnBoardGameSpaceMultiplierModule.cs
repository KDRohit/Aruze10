using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayAnimationsOnRoundStartBasedOnBoardGameSpaceMultiplierModule : ChallengeGameModule
{
	[SerializeField] private List<RoundIntroAnimationInfo> introAnimationNamesByMultiplier = new List<RoundIntroAnimationInfo>();
	[SerializeField] private ModularBoardGameVariant boardGame;

	private BoardGameSpace currentSpace = null;
	public override bool needsToExecuteOnRoundStart()
	{
		currentSpace = boardGame.currentSpaceIndex < boardGame.boardSpaces.Length ? boardGame.boardSpaces[boardGame.currentSpaceIndex] : null;
		return currentSpace != null && currentSpace.miniSlot != null;
	}

	public override IEnumerator executeOnRoundStart()
	{
		foreach (RoundIntroAnimationInfo roundInfo in introAnimationNamesByMultiplier)
		{
			if (roundInfo.multiplier == currentSpace.miniSlot.currentMultiplier)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(roundInfo.animationInformation));
			}
		}
	}
	
	[System.Serializable]
	protected class RoundIntroAnimationInfo
	{
		public int multiplier;
		public AnimationListController.AnimationInformationList animationInformation;
	}
}

