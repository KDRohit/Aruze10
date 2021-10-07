using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel for testing the video dialog.
Enter any url to test or leave it blank to test the DynamicVideo EOS experiment set up
*/

public class DevGUIMenuVideoDialog : DevGUIMenu
{
	private static string videoPath = "";
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Video Path");	
		videoPath = GUILayout.TextField(videoPath).Trim();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Show Video"))
		{
			if (string.IsNullOrEmpty(videoPath))
			{
				if (ExperimentWrapper.DynamicVideo.isInExperiment)
				{
					DoSomething.now("dynamic_video");
				}
			}
			else
			{
				VideoDialog.showDialog(videoPath);
			}
			DevGUI.isActive = false;
		}
		GUILayout.EndHorizontal();

	}

	private delegate void showDialogDelegate();
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
