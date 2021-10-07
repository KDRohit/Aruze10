using UnityEngine;
using System.Collections;
using System;
using Com.Scheduler;

/*
Handles Android-specific functionality.
*/

public class AndroidUtil : MonoBehaviour
{
	private const float BACK_SPAM_DELAY = 0.4f; 	// Sounds disgusting.
	
	private static float nextBackButtonTime = 0f;
	
	public static bool isBackEnabled = true;
	
	// Check for back button input and react accordingly.
	// Support the ButtonHandler registration format.
	public static void checkBackButton(ButtonHandler.onClickDelegate callback, string counterName = "", string kingdom = "", string phylum = "", string className = "", string family = "", string genus = "")
	{
		checkBackButton(callback, null, counterName, kingdom, phylum, className, family, genus);
	}

	// Check for back button input and react accordingly.
	// Support the generic format.
	public static void checkBackButton(System.Action callback, string counterName = "", string kingdom = "", string phylum = "", string className = "", string family = "", string genus = "")
	{
		checkBackButton(null, callback, counterName, kingdom, phylum, className, family, genus);
	}

	// Check for back button input and react accordingly.
	private static void checkBackButton(ButtonHandler.onClickDelegate argsCallback, System.Action callback, string counterName = "", string kingdom = "", string phylum = "", string className = "", string family = "", string genus = "")
	{
		if (!isBackEnabled || Time.realtimeSinceStartup <= nextBackButtonTime)
		{
			// Do not allow the back button to trigger multiple times within a a small timeframe.
			// This prevents multiple calls in the same frame, as well as multiple accidental rapid calls.
			return;
		}

		if (Input.GetKeyDown(KeyCode.Escape) && !Dialog.instance.isClosing && !Dialog.instance.isOpening)
		{
			// Only one of these should be non-null.
			if (argsCallback != null)
			{
				argsCallback(null);
			}
			else if (callback != null)
			{
				callback();
			}
			
			if (counterName != "")
			{
				StatsManager.Instance.LogCount(counterName, kingdom, phylum, className, family, genus);
				Bugsnag.LeaveBreadcrumb("Hitting the ESC key, this is the back button on Android -- called from: " + kingdom + " " + phylum);
			}
			else
			{
				Bugsnag.LeaveBreadcrumb("Hitting the ESC key, this is the back button on Android");
			}
			
			nextBackButtonTime = Time.realtimeSinceStartup + BACK_SPAM_DELAY;
		}
	}

	// Ask for confirmation about quitting.
	public static void androidQuit()
	{
#if UNITY_ANDROID || UNITY_EDITOR
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.text("quit_confirmation_title"),
				D.MESSAGE, Localize.text("quit_confirmation_message"),
				D.OPTION1, Localize.textUpper("yes"),
				D.OPTION2, Localize.textUpper("no"),
				D.REASON, "android-quit",
				D.CALLBACK, new DialogBase.AnswerDelegate(quitDialogCallback)
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		); 
#else
		Debug.LogWarning("Cannot call Application.Quit() if not in Android");
#endif
	}
	
	// Callback when quit confirmation dialog is closed.
	private static void quitDialogCallback(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "1")
		{
			// Simulate a pause to update the badge counter correctly upon exiting the app in android
			NotificationManager.Instance.pauseHandler(true);

#if UNITY_EDITOR
			// In the editor, do the closest thing we can to quitting, which is pausing the game.
			Debug.Break();
#else
			Common.QuitApp();
#endif
		}
	}
}
