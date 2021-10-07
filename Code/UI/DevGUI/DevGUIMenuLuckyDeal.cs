using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Identities dev panel.
*/
using Zynga.Core.Util;

public class DevGUIMenuLuckyDeal : DevGUIMenu
{				
	public static bool useFakeServerPurchase = false;
	public static string fakePurchaseJSON = "";
	public static float[] reelTimes = new float[2];
	public static float[] reelLoops = new float[2];
	public static bool overrideReels = false;

	public  static IEnumerator launchDialog()
	{
		yield return new WaitForSeconds(.2f);

		LuckyDealDialog.showDialog();
	}		

	public override void drawGuts()
	{
		GUILayout.Label("Lucky Deal active at log in : " + LuckyDealDialog.wasWheelDealActiveAtLogIn);	
		GUILayout.Label("Variant : " + ExperimentWrapper.WheelDeal.keyName);	
		GUILayout.Label("Waiting for event data : " + LuckyDealDialog.waitingForEventData);	
		GUILayout.Label("Event expires : " + LuckyDealDialog.eventTimer.endDateFormatted);
		GUILayout.Label("Errors : " + LuckyDealDialog.errMessage);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Has player seen spin for this event : " + PlayerPrefsCache.GetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0));
		if (GUILayout.Button("Reset to False"))
		{
			PlayerPrefsCache.SetInt(Prefs.HAS_SPUN_LUCKY_DEAL, 0);			
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (LuckyDealDialog.eventData != null && GUILayout.Button("Show Intro Dialog using last event outcome"))
		{
			RoutineRunner.instance.StartCoroutine(launchDialog());

			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Show Collect Dialog"))
		{
			Dict args = Dict.create(D.DATA, "COLLECT");
			LuckyDealDialog.showDialog("", args);				
			DevGUI.isActive = false;
		}		
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Reset local data and get event from server"))
		{
			// useful if you reset event in server admin panel
			LuckyDealDialog.getEventAction();		
		}		

		renderFakePurchaseEdit();

		string eventJson = "Data is null";
		if (LuckyDealDialog.eventData != null)
		{
			eventJson = LuckyDealDialog.eventData.ToString();
		}
		GUILayout.Label(" ");	
		GUILayout.Label("Event JSON");	
		GUILayout.TextArea(eventJson);


		overrideReels = GUILayout.Toggle(overrideReels, "Override Reel Settings. If checked settings below will be used on spin.\n");

		for (int i = 0; i < 2; i++)
		{
			GUILayout.Label("Reel " + i);	
			GUILayout.BeginHorizontal();
			GUILayout.Label("Duratipn :");	
			GUILayout.Label(string.Format("{0:0.000}", reelTimes[i]), GUILayout.Width(isHiRes ? 100 : 50));
			reelTimes[i] = GUILayout.HorizontalSlider(reelTimes[i], 0.1f, 10.0f, GUILayout.Width(isHiRes ? 300 : 150));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Num Loops :");	
			GUILayout.Label(reelLoops[i].ToString(), GUILayout.Width(isHiRes ? 100 : 50));
			reelLoops[i] = (int)GUILayout.HorizontalSlider(reelLoops[i], 1.0f, 50.0f, GUILayout.Width(isHiRes ? 300 : 150));
			GUILayout.EndHorizontal();
		}

	}

	private void renderFakePurchaseEdit()
	{
		useFakeServerPurchase = GUILayout.Toggle(useFakeServerPurchase, "Fake server purchase.");
		GUILayout.BeginHorizontal();
		fakePurchaseJSON =	GUILayout.TextField(fakePurchaseJSON);
		GUILayout.EndHorizontal();		
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}	
}
