using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Zap.Automation;

#if UNITY_EDITOR && !ZYNGA_PRODUCTION
[CustomEditor(typeof(ZapButton))]
public class ZapButtonEditor : Editor
{
	Dictionary<string, bool> buttonIdMap = new Dictionary<string, bool>();
	string[] buttonIds;
	int buttonIdIndex = 0;

	void OnEnable()
	{
		getButtonIds();
	}

	public override void OnInspectorGUI()
	{
		drawButtonIds();
	}

	// create a dropdown with all the zap button ids
	private void drawButtonIds()
	{
		if(buttonIds != null && buttonIds.Length > 0)
		{
			buttonIdIndex = EditorGUILayout.Popup("buttonId", buttonIdIndex, buttonIds);
			ZapButton zapButton = target as ZapButton;
			if (buttonIdIndex < buttonIds.Length)
			{
				zapButton.buttonId = buttonIds[buttonIdIndex];
			}
		}
	}

	// read in the Actions.json file and extract all the buttonIds so we can display
	// them in a dropdown list. We also get the index of the current selection here
	// so we can populate the drop down properly.
	private void getButtonIds()
	{
		//setup the variables we'll need
		int index = 0;
		ZapButton zapButton = target as ZapButton;
		Dictionary<string, JObject> values = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(@"Assets/Code/ZAP/Actions.json"));
		Dictionary<string, Action> actions = new Dictionary<string, Action>();

		//go through all the actions and find zap button ids
		foreach (KeyValuePair<string, JObject> entry in values)
		{
			if (entry.Value["zapButtonId"] != null)
			{
				if (!buttonIdMap.ContainsKey(entry.Value["zapButtonId"].ToString()))
				{
					string buttonId = entry.Value["zapButtonId"].ToString();

					if (buttonId == zapButton.buttonId)
					{
						buttonIdIndex = index;
					}
					buttonIdMap.Add(buttonId, true);
					++index;
				}
			}
		}

		// convert our uniq buttonIdMap into an array that can be used to for the dropdown
		if(buttonIdMap.Keys.Count > 0)
		{
			buttonIds = new string[buttonIdMap.Keys.Count];
			buttonIdMap.Keys.CopyTo(buttonIds, 0);
		}
	}
}
#endif
