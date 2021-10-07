using System;
using UnityEngine;
using Com.HitItRich.Feature.TimedBonus;
using Com.Scheduler;
using Zynga.Core.Util;

/*
A dev panel.
*/

public class DevGUIMenuDailyBonus : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Daily Bonus");
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		string bonusString = "";
		if (ExperimentWrapper.NewDailyBonus.isInExperiment)
		{
			bonusString = ExperimentWrapper.NewDailyBonus.bonusKeyName;
		}
		for (int bonusDayWanted = 1; bonusDayWanted <= 8; bonusDayWanted++)
		{
			if (GUILayout.Button(string.Format("Day {0}", bonusDayWanted)))
			{
				if (bonusDayWanted != SlotsPlayer.instance.dailyBonusTimer.day)
				{
					SlotsPlayer.instance.dailyBonusTimer.day = bonusDayWanted;
					CreditAction.setTimerClaimDay(bonusDayWanted, bonusString);

					DevGUI.isActive = false;
					GenericDialog.showDialog(
						Dict.create(
							D.TITLE, "SENT",
							D.MESSAGE, "The action has been sent. The game will restart to receive the benefits.",
							D.REASON, "dev-gui-daily-bonus-day-changed",
							D.CALLBACK, new DialogBase.AnswerDelegate( (args) => { Glb.resetGame("Changed daily bonus day on dev panel."); })
						),
						SchedulerPriority.PriorityType.IMMEDIATE
					);
				}
			}
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Daily Triple Wheel"))
		{
			string testDataPath = "Test Data/DailyBonusGames/DailyTripleWheel";
			if (ExperimentWrapper.NewDailyBonus.isInExperiment)
			{
				testDataPath = "Test Data/DailyBonusGames/NewDailyBonusWheel";
			}
			TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));

			string text = textAsset.text;
			JSON json = new JSON(text);
			
			if (ExperimentWrapper.NewDailyBonus.isInExperiment)
			{
				NewDailyBonusDialog.showDebugSpin(json);
			}
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		
		long resetTimeRemaining = SlotsPlayer.instance.dailyBonusTimer.resetProgressionTimerTimeRemaining;
		long resetTimeSeconds = TimeUtil.CurrentTimestamp() + resetTimeRemaining;	                        
		DateTime resetTime = TimeUtil.TimestampToDateTime(resetTimeSeconds);
		GUILayout.Label("Time remaining seconds " + resetTimeRemaining);
		GUILayout.Label("Reset time seconds " + resetTimeSeconds);
		GUILayout.Label("Reset Time " + resetTime);		
		GUILayout.Label("Suggested notif offset seconds " + (SlotsPlayer.instance.dailyBonusTimer.resetProgressionTimerTimeRemaining - 600));
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
