using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevGUIMenuEvents : DevGUIMenu
{

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();

		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
