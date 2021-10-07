using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
Class to run an editor window for showing the usage of the SlotSymbolCache used
by the current ReelGame.

Original Author: Scott Lepthien
Creation Date: 10/11/2018
*/
public class SlotSymbolCacheEditorWindow : EditorWindow 
{
	private Vector2 scrollPosition;

	[MenuItem("Zynga/Slot Symbol Cache Info")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(SlotSymbolCacheEditorWindow));
	}

	public void OnGUI()
	{
		if (EditorApplication.isPlaying)
		{
			GUILayout.Label("Game is running");
			if (GameState.hasGameStack)
			{
				if (ReelGame.activeGame != null)
				{
					scrollPosition = GUILayout.BeginScrollView(scrollPosition);
					ReelGame.activeGame.drawOnGuiSlotSymbolCacheInfo();
					GUILayout.EndScrollView();
				}
				else
				{
					GUILayout.Label("Game not loaded yet.");
				}
			}
			else
			{
				GUILayout.Label("In lobby");
			}
		}
		else
		{
			GUILayout.Label("Game is not running");
		}
	}
	
	public void OnInspectorUpdate()
	{
		if (EditorApplication.isPlaying && GameState.hasGameStack)
		{
			Repaint();
		}
	}
}
