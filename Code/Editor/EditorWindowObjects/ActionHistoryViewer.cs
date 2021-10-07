using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
	Class: AtlasViewer
	Class to collect all the Atlases uses for the selected SKU in one place so you don't have to search through the project for them.
*/


public class ActionHistoryViewer : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Player Viewer")]
	public static void openActionHistoryViewer()
	{
		ActionHistoryViewer playerViewer = (ActionHistoryViewer)EditorWindow.GetWindow(typeof(ActionHistoryViewer));
		playerViewer.Show();
	}

	private ActionHistoryViewerObject playerViewerObject;

	public void OnGUI()
	{
		if (playerViewerObject == null)
		{
			playerViewerObject = new ActionHistoryViewerObject();
		}
		playerViewerObject.drawGUI(position);
	}
}

public class ActionHistoryViewerObject : EditorWindowObject
{
	protected override string getButtonLabel()
	{
		return "Action History Viewer";
	}

	protected override string getDescriptionLabel()
	{
		return "(RUNTIME ONLY) This will let you see your information at runtime and copy to the clipboard";
	}

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

	public override void drawGuts(Rect position)
	{
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
					EditorGUIUtility.systemCopyBuffer = val;
					Debug.LogFormat("ActionHistoryViewer.cs -- showList -- data for key {0} was {1}", key, val);
				}
				GUILayout.EndHorizontal();
			}
			i = isReversed ? i - 1 : i + 1;
		}
	}
}
