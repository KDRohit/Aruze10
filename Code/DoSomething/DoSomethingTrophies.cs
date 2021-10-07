using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;

public class DoSomethingTrophies : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (!string.IsNullOrEmpty(parameter))
		{
			Achievement achievement = NetworkAchievements.getAchievement(parameter);
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, achievement, NetworkProfileDialog.MODE_TROPHIES);
		}
		else
		{
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, SchedulerPriority.PriorityType.IMMEDIATE, null, NetworkProfileDialog.MODE_TROPHIES);
		}
	}
}
