using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Identities dev panel.
*/
using Zynga.Core.Util;

public class DevGUIMenuTicketTumbler : DevGUIMenu
{
	public static bool pauseEvents;
	private static JSON completeStashData;
	private static JSON stashData;
	private static 	System.DateTime timeConverter = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);

	public  static IEnumerator launchDialog()
	{
		yield return new WaitForSeconds(.2f);

		TicketTumblerDialog.showDialog("", null, true);
	}

	public  static IEnumerator launchTicketingDialog(JSON data)
	{
		if (data != null)
		{

			yield return new WaitForSeconds(.2f);
			TicketTumblerFeature.instance.lastTicketingKey = 0;
			TicketTumblerFeature.instance.completedLotteryKey = 0;
			bool prevPauseEvents = pauseEvents;
			pauseEvents = false;
			TicketTumblerFeature.instance.handleLotteryCompleteEvent(data);
			pauseEvents = prevPauseEvents;
		}

		DevGUI.isActive = false;
			
	}

	public  static IEnumerator launchWinnerDialog(JSON data)
	{
		if (data != null)
		{
			yield return new WaitForSeconds(.2f);
			TicketTumblerFeature.instance.lastTicketingKey = 0;
			TicketTumblerFeature.instance.winningLotteryKey = 0;
			bool prevPauseEvents = pauseEvents;
			pauseEvents = false;			
			TicketTumblerFeature.instance.handleLotteryWinnerEvent(data);
			pauseEvents = prevPauseEvents;
		}

		DevGUI.isActive = false;
	}

	public override void drawGuts()
	{
		GUILayout.Label("LOTTERY_DAY_START_TIME : " + CommonText.formatDateTime(Common.convertFromUnixTimestampSeconds(Data.liveData.getInt("LOTTERY_DAY_START_TIME", 0))));
		GUILayout.Label("LOTTERY_DAY_END_TIME : " + CommonText.formatDateTime(Common.convertFromUnixTimestampSeconds(Data.liveData.getInt("LOTTERY_DAY_END_TIME", 0))));

		GUILayout.Label("Bundle is loaded : " + AssetBundleManager.isBundleCached("ticket_tumbler"));	
		GUILayout.Label("Ticket Tumbler active at log in : " + TicketTumblerFeature.instance.wasLotteryActiveAtLogIn);	
		GUILayout.Label("Variant : " + ExperimentWrapper.LotteryDayTuning.keyName + " in experiment " + ExperimentWrapper.LotteryDayTuning.isInExperiment);	
		GUILayout.Label("Waiting for event data : " + TicketTumblerFeature.instance.waitingForEventData);	
		if (TicketTumblerFeature.instance.featureTimer != null)
		{
			GUILayout.Label("Entire event expires : " + TicketTumblerFeature.instance.featureTimer.endDateFormatted);
		}
		else
		{
			GUILayout.Label("Event timer is null!");			
		}
		
		if (TicketTumblerFeature.instance.roundEventTimer != null)
		{
			GUILayout.Label("Drawing expires : " + TicketTumblerFeature.instance.roundEventTimer.endDateFormatted);
		}
		else
		{
			GUILayout.Label("roundEventTimer is null!");			
		}
			
		GUILayout.Label("Tickets : " + TicketTumblerFeature.instance.ticketCount);
		GUILayout.Label("Meter Progress : " + TicketTumblerFeature.instance.meterProgress);
		GUILayout.Label("Errors : " + TicketTumblerDialog.errMessage);

		string pauseString = pauseEvents ? "Unpause Events" : "Pause Events";
		if (GUILayout.Button(pauseString))
		{
			pauseEvents = !pauseEvents;
		}

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Send event in clipboard to dialog"))
		{
			string clip = GUIUtility.systemCopyBuffer;
			if (clip != null)
			{
				JSON cheatData = null;
				try 
				{
					cheatData = new JSON(clip);
				}
				catch (System.Exception ex)
				{
					Debug.LogError("Unable to parse clipboard data: " + ex.ToString());
				}
				if (cheatData != null)
				{
					string eventType = cheatData.getString("type", "");

					if (eventType.Equals("lottery_complete"))
					{
						Debug.LogError("launching json cheat data " + cheatData.ToString());
						RoutineRunner.instance.StartCoroutine(launchTicketingDialog(cheatData));
					}
					if (eventType.Equals("lottery_winner"))
					{
						Debug.LogError("launching json cheat data " + cheatData.ToString());
						RoutineRunner.instance.StartCoroutine(launchWinnerDialog(cheatData));
					}
				}
				else
				{
					Debug.LogError("cheat data is null");
				}
			}
		}		
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Request info event with key : " + TicketTumblerFeature.instance.lotteryKey))
		{
			// useful if you reset event in server admin panel
			TicketTumblerFeature.instance.postGetInfoAction();		
		}		

		jsonDataField(TicketTumblerFeature.instance.logInLotteryData, "Log In Data");
		jsonDataField(TicketTumblerFeature.instance.eventData, "Info Event Data");
		jsonDataField(TicketTumblerFeature.instance.completeData, "Complete Event Data");
		jsonDataField(TicketTumblerFeature.instance.winnerData, "Winner Event Data");
		jsonDataField(TicketTumblerFeature.instance.ticketData, "Ticket Awarded Data");
		jsonDataField(TicketTumblerFeature.instance.progressData, "Meter Progress Data");						
	}

	private static void jsonDataField(JSON data, string labelText)
	{
		string eventJson = "Data is null";
		if (data != null)
		{
			eventJson = data.ToString();
		}		
		GUILayout.BeginHorizontal();
		GUILayout.Label(labelText);

		if (labelText.Equals("Complete Event Data") || labelText.Equals("Winner Event Data"))
		{
			if (GUILayout.Button("Play it again. Will cause desync."))
			{
				if (labelText.Equals("Complete Event Data"))
				{
					RoutineRunner.instance.StartCoroutine(launchTicketingDialog(data));
				}
				if (labelText.Equals("Winner Event Data"))
				{
					RoutineRunner.instance.StartCoroutine(launchWinnerDialog(data));
				}		
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.TextArea(eventJson);		
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
