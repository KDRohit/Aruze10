using UnityEngine;
using System.Collections;

/**
 * Module to enable showing custom wings per-round
 */
public class ChallengeGameShowCustomRoundWings : ChallengeGameModule
{
	public bool wingsInForeground;
	public WingInformation.WingChallengeStage stage;

	public override bool needsToExecuteOnShowCustomWings()
	{		
		return true;
	}

	public override IEnumerator executeOnShowCustomWings()
	{
		switch (stage)
		{
			case WingInformation.WingChallengeStage.First:
				BonusGameManager.instance.wings.forceShowChallengeWings(wingsInForeground);
				break;
			case WingInformation.WingChallengeStage.Secondary:
				BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(wingsInForeground);
				break;
			case WingInformation.WingChallengeStage.Third:
				BonusGameManager.instance.wings.forceShowThirdChallengeWings(wingsInForeground);
				break;
			case WingInformation.WingChallengeStage.Fourth:
				BonusGameManager.instance.wings.forceShowFourthChallengeWings(wingsInForeground);
				break;
			case WingInformation.WingChallengeStage.None:
				BonusGameManager.instance.wings.hide();
				break;
		}
		return base.executeOnShowCustomWings();
	}
}
