using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuLiveData : DevGUIMenu
{
	private string searchFilter = "";
	private string expandedKey = "";

	private string keyToModify = "";
	private string valueToSend = "";

	
	public override void drawGuts()
	{

		if (!string.IsNullOrEmpty(keyToModify))
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Modifying Key: " + keyToModify);
			valueToSend = GUILayout.TextArea(valueToSend);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set Override"))
			{
				TestingSetupManager.instance.setLiveDataOverride(keyToModify, valueToSend);
			}
			if (GUILayout.Button("Cancel"))
			{
				keyToModify = "";
				valueToSend = "";
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
		else
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Search For: ");
			searchFilter = GUILayout.TextField(searchFilter, GUILayout.Width(isHiRes ? 500 : 300));
			if (GUILayout.Button("X", GUILayout.Width(isHiRes ? 100 : 50)))
			{
				searchFilter = "";
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("ZID: " + SlotsPlayer.instance.socialMember.zId);
			GUILayout.Label("SNID: " + SlotsPlayer.instance.socialMember.id);
			GUILayout.EndHorizontal();

			GUILayout.BeginVertical();

			for (int i = 0; i < Data.liveData.keys.Count; i++)
			{
				string key = Data.liveData.keys[i];

				// Make everything lower case and then check filtering.
				if (searchFilter != "" && !key.ToLower().Contains(searchFilter.ToLower()))
				{
					continue;
				}

				if (key == expandedKey)
				{
					GUILayout.BeginVertical();
					// If we are in the expanded view, then display it vertically to make it easier to read.
					GUILayout.BeginHorizontal();
					GUILayout.Label(key);
					if (GUILayout.Button("V"))
					{
						expandedKey = "";
					}
					GUILayout.EndHorizontal();

					string value = Data.liveData.getString(key, "?");
					GUILayout.TextArea(value);

					if (GUILayout.Button("Modify"))
					{
						keyToModify = key;
						valueToSend = value;
					}
					GUILayout.EndVertical();
				}
				else
				{
					// Otherwise draw on one line.
					GUILayout.BeginHorizontal();
					GUILayout.Label(key, GUILayout.Width(isHiRes ? 500 : 300));
					GUILayout.Label(Data.liveData.getString(key, "?"), GUILayout.Width(isHiRes ? 500 : 300));
					if (GUILayout.Button("<"))
					{
						expandedKey = key;
					}
					GUILayout.EndHorizontal();
				}

			}

			GUILayout.EndVertical();
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
