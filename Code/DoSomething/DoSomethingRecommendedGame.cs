public class DoSomethingRecommendedGame : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		string gameName = ExperimentWrapper.PersonalizedContent.getRecommandedGameName(parameter);
		LobbyGame gameToLaunch = !string.IsNullOrEmpty(gameName) ? LobbyGame.find(gameName) : null;

		if (gameToLaunch != null)
		{
			gameToLaunch.askInitialBetOrTryLaunch();
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("DoSomethingRecommendedGame::doAction - Missing the game we want to launch. Game was " + gameName);	
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// This is to avoid putting a blank spot in the lobby
		if (!ExperimentWrapper.PersonalizedContent.isInExperiment)
		{
			return false;
		}
		
		string gameName = ExperimentWrapper.PersonalizedContent.getRecommandedGameName(parameter);
		LobbyGame gameToLaunch = !string.IsNullOrEmpty(gameName) ? LobbyGame.find(gameName) : null;
		
		return gameToLaunch != null;
	}

}
