using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/*
A dev panel.
*/

public class DevGUIMenuScheduler : DevGUIMenu
{
	public override void drawGuts()
	{
		// Define style for horizontal line separator.
		GUIStyle hlStyle = new GUIStyle(GUI.skin.box);
		hlStyle.stretchWidth = true;
		hlStyle.fixedHeight = 2;

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label(string.Format("Dialog.isOpening: {0}", Dialog.instance.isOpening));
			GUILayout.Label(string.Format("Dialog.isDownloadingTexture: {0}", Dialog.instance.isDownloadingTexture));
			GUILayout.Label(string.Format("Dialog.isClosing: {0}", Dialog.instance.isClosing));
			GUILayout.EndHorizontal();
		}

		// Horizontal line separator.
		GUILayout.Box("", hlStyle);

		// Display queue info.
		GUILayout.Label(string.Format("Scheduler has task? : {0}", Scheduler.hasTask));

		for (int i = 0; i < Scheduler.tasks.Count; ++i)
		{
			GUILayout.BeginHorizontal();

			GUILayout.Label(Scheduler.tasks[i].ToString());

			GUILayout.EndHorizontal();
		}

		// Horizontal line separator.
		GUILayout.Box("", hlStyle);

		// Display delayed dialog info.
		Dialog.drawDebugInfo();

		// Horizontal line separator.
		GUILayout.Box("", hlStyle);

		if (GUILayout.Button("Force Scheduler run"))
		{
			Scheduler.run();
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}