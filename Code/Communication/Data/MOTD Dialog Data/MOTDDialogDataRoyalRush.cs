﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 using Com.Scheduler;

/*
Override for special behavior.
*/

public class MOTDDialogDataRoyalRush : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
			ExperimentWrapper.RoyalRush.isInExperiment &&
			SlotsPlayer.instance.socialMember.experienceLevel >= RoyalRushEvent.minLevel &&
			LobbyOptionButtonRoyalRush.ftuePage != -1 &&
			LobbyOptionButtonRoyalRush.ftueButton.rushInfo != null &&
			LobbyOptionButtonRoyalRush.ftueButton.rushInfo.inWithinRegistrationTime() &&
			LobbyOptionButtonRoyalRush.ftueButton.rushInfo.currentState == RoyalRushInfo.STATE.AVAILABLE &&
			!MainLobby.isTransitioning;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;

 			if (!ExperimentWrapper.RoyalRush.isInExperiment)
			{
				result += "Royal Rush was not in enabled via experiment";
			}
			else if (SlotsPlayer.instance.socialMember.experienceLevel < RoyalRushEvent.minLevel)
			{
				result += "User Level too low for Royal Rush";
			}
			else if (LobbyOptionButtonRoyalRush.ftuePage == -1)
			{
				result += "Royal Rush FTUE page was not set";
			}
			else if (LobbyOptionButtonRoyalRush.ftueButton == null)
			{
				result += "Royal Rush FTUE button was not set";
			}
			else if (MainLobby.isTransitioning)
			{
				result += "Main lobby was transitioning";
			}

			return result;
		}
	}

	public override bool show()
	{
		// As long as we don't have something coming up that'll take us out of the lobby, we can run the MOTD
		if (!Scheduler.hasTaskWith<SchedulerDelegate>(MOTDFramework.callToAction))
		{
			Scheduler.addTask(new RoyalRushFTUETask(RoyalRushEvent.instance.playRoyalRushFTUE));
			return true;
		}
		else
		{
			return false;
		}
	}

	new public static void resetStaticClassData()
	{
	}

}
