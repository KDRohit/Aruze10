using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChallengeGameAnimateOnSpecificRoundStart : ChallengeGameModule
{
	[SerializeField] private List<RoundIntroAnimationInfo> introAnimationNamesByVariant = new List<RoundIntroAnimationInfo>();

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		foreach (RoundIntroAnimationInfo roundInfo in introAnimationNamesByVariant)
		{
			if (roundInfo.ROUND_NAME == roundVariantParent.getVariantGameDataName())
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(roundInfo.animationInformation));
			}
		}
		yield return StartCoroutine(base.executeOnRoundStart());
	}

	[System.Serializable]
	protected class RoundIntroAnimationInfo
	{
		public string ROUND_NAME = ""; //Only plays these animations/sounds for this specific round
		public AnimationListController.AnimationInformationList animationInformation;
	}
}
