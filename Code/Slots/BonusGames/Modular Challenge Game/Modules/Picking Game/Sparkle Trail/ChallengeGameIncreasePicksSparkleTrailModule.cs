/**
 * Module extension for displaying sparkle trails only on items that award increased picks
 */
public class ChallengeGameIncreasePicksSparkleTrailModule : ChallengeGameSparkleTrailModule
{

	private bool shouldHandle(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		if ((pickData != null) && (pickData.additonalPicks > 0))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return shouldHandle(pickData);
	}
}
