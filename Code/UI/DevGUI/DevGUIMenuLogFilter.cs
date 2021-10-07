using UnityEngine;
using System;
using System.Reflection;

class DevGUIMenuLogFilter : DevGUIMenu
{
	private static string wordList = "";
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Specify comma separated list of words to filter logs with");

		wordList = GUILayout.TextField(wordList);

		if (GUILayout.Button("Save"))
		{
			if (!string.IsNullOrEmpty(wordList))
			{	
				string[] words = wordList.Split(',');
				for (int i = 0; i < words.Length; ++i)
				{
					SmartLog.addWordFilter(words[i].Trim());
				}
			}
			else
			{
				SmartLog.clearWordFilter();
			}
		}

		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}