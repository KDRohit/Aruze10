using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

public class DevGUIMenuActionHistory : DevGUIMenu
{
	private Vector2 scrollPosition = Vector2.zero;
	private bool reverseOrder = true;
	private bool showSentActions = false;
	private bool showRecievedEvents = false;
	private string sentFilter = "";
	private string recievedFilter = "";

	private string _actionHistoryLength = "";
	private string actionHistoryLength
	{
		get
		{
			if (string.IsNullOrEmpty(_actionHistoryLength))
			{
				_actionHistoryLength = SlotsPlayer.getPreferences().GetInt(Prefs.MAX_ACTION_HISTORY_COUNT, 0).ToString();
			}
			return _actionHistoryLength;
		}
		set
		{
			if (value != _actionHistoryLength)
			{
				_actionHistoryLength = value;
			}
		}
	}

	private const float BUTTON_WIDTH = 300f;

	public override void drawGuts()
	{
		#if ZYNGA_PRODUCTION
		// This shouldn't ever really happen.
		GUILayout.Label("This is not enabled on production, try again later.");
		return;
		#endif		
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Action History Length: ");
		actionHistoryLength = GUILayout.TextField(actionHistoryLength);
		if (GUILayout.Button("Save"))
		{
			int valAsInt = 0;
			try
			{
				int.TryParse(actionHistoryLength, out valAsInt);
				SlotsPlayer.getPreferences().SetInt(Prefs.MAX_ACTION_HISTORY_COUNT, valAsInt);
			}
			catch(System.Exception e)
			{
				Debug.LogErrorFormat("DevGUIMenuActionHistory.cs -- Null Function -- failed with exception: {0}", e.ToString());
			}

		}
		GUILayout.EndHorizontal();
		reverseOrder = GUILayout.Toggle(reverseOrder, "Show newest first");
		if (showHideButton(showRecievedEvents, "Incoming Events", out showRecievedEvents))
		{
			getInputFilter("Filter by type: ", recievedFilter, out recievedFilter);
			showList(ActionHistory.recentReceivedEvents, recievedFilter, reverseOrder);
		}

		if (showHideButton(showSentActions, "Outgoing Actions", out showSentActions))
		{
			getInputFilter("Filter by type: ", sentFilter, out sentFilter);
			showList(ActionHistory.recentSendActions, sentFilter, reverseOrder);
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}

	private void showList(List<KeyValuePair<string, JSON>> list, string filter, bool isReversed)
	{
		string key = "";
		int i = isReversed ? (list.Count - 1) : 0;
		while (i >= 0 && i < list.Count)
		{
			key = list[i].Key;
			if (string.IsNullOrEmpty(filter) || key.Contains(filter))
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(key);
				if (GUILayout.Button("Copy Data", GUILayout.Width(BUTTON_WIDTH)))
				{
					string val = list[i].Value.ToString();
					shareString(val);
					Debug.LogFormat("ActionHistoryViewer.cs -- showList -- data for key {0} was {1}", key, val);
				}
				GUILayout.EndHorizontal();
			}
			i = isReversed ? i - 1 : i + 1;
		}
	}

	private void shareString(string stringToShare)
	{
		#if !UNITY_EDITOR
		NativeBindings.ShareContent(
			subject:"Action History JSON",
			body:stringToShare,
			imagePath:"",
			url:"");
		#else
		UnityEditor.EditorGUIUtility.systemCopyBuffer = stringToShare;
		#endif
	}

	// Show a green/red button with Hide or Show appeneded to the end of the label.
	// By default value is assumed to be positive when showing content.
	protected bool showHideButton(bool value, string label, out bool newValue, bool swapValueMeaning = false)
	{
		bool result = value;
		Color originalColor = GUI.backgroundColor;
		bool tweakedValue = swapValueMeaning ? !value : value;
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.normal.textColor = Color.white;
		GUI.backgroundColor = tweakedValue ? Color.red : Color.green;
		string showHide = tweakedValue ? "Hide" : "Show";
		string buttonLabel = string.Format(label + " : " + showHide);
		if (GUILayout.Button(buttonLabel, buttonStyle))
		{
			result = !value;
		}
		GUI.backgroundColor = originalColor;
		newValue = result;
		return newValue;
	}

	protected string getInputFilter(string label, string currentValue, out string outputValue)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(label);
		string result = GUILayout.TextField(currentValue);
		GUILayout.EndHorizontal();
		outputValue = result;
		return outputValue;
		//return result;
	}	
}
