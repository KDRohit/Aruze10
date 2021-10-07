﻿﻿﻿using UnityEngine;
using System.Collections;

public class DoSomethingPersonalizedContent : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		LobbyGame gameToLaunch = LobbyGame.find(PersonalizedContentLobbyOptionDecorator1x2.gameKey);

		if (gameToLaunch != null)
		{
			// This gets toggeled once the game launches. We differentiate between accessing a game from here and normally, so this seemed like the most direct route without
			// refactoring too much.
			gameToLaunch.isRecomended = true;
			gameToLaunch.askInitialBetOrTryLaunch();
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("DoSomethingPersonalizedContent::doAction - Missing the game we want to launch. Game was " + PersonalizedContentLobbyOptionDecorator1x2.gameKey);	
		}
	
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// This is to avoid putting a blank spot in the lobby
		return PersonalizedContentLobbyOptionDecorator1x2.gameKey != "" && ExperimentWrapper.PersonalizedContent.isInExperiment;
	}

}
