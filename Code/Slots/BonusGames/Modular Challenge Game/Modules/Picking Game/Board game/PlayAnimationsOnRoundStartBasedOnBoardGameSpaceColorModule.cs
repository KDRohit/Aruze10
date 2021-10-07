using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayAnimationsOnRoundStartBasedOnBoardGameSpaceColorModule : ChallengeGameModule
{
	[SerializeField] private List<RoundIntroAnimationInfo> introAnimationNamesByColor = new List<RoundIntroAnimationInfo>();
	[SerializeField] private ModularBoardGameVariant boardGame;

	private BoardGameSpace currentSpace = null;
	public override bool needsToExecuteOnRoundStart()
	{
		currentSpace = boardGame.currentSpaceIndex < boardGame.boardSpaces.Length ? boardGame.boardSpaces[boardGame.currentSpaceIndex] : null;
		return currentSpace != null && currentSpace.miniSlot != null;
	}

	public override IEnumerator executeOnRoundStart()
	{
		foreach (RoundIntroAnimationInfo roundInfo in introAnimationNamesByColor)
		{
			if (roundInfo.color == currentSpace.miniSlot.color)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(roundInfo.animationInformation));
			}
		}
	}
	
	[System.Serializable]
	protected class RoundIntroAnimationInfo
	{
		public BoardGameSpaceMiniSlot.ColorType color;
		public AnimationListController.AnimationInformationList animationInformation;
	}
}

