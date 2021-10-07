using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zynga.Core.Util;

public class DevGUIMenuServerEvents : DevGUIMenu, IResetGame
{
	private static string eventText;
	private static Vector2 scrollPosition = Vector2.zero;
	private static List<JSON> queuedEvents = new List<JSON>();

	private static GUILayoutOption[] textAreaOptions = new GUILayoutOption[]
	{
		GUILayout.ExpandWidth(true),
		GUILayout.Height(150)
	};
	
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		scrollPosition = GUILayout.BeginScrollView(scrollPosition, textAreaOptions);
		{
			// Readonly TextArea with working scrollbars
			eventText = GUILayout.TextArea(eventText, GUILayout.ExpandHeight(true));
		}
		GUILayout.EndScrollView();
		GUILayout.Label("Number of queued events: " + queuedEvents.Count);
		
		if (GUILayout.Button("Add Event to queue"))
		{
			try
			{
				JSON json = new JSON(eventText);
				if (json.isValid)
				{
					queuedEvents.Add(json);    
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("Cannot construct event: " + System.Environment.NewLine + e.ToString());

			}
		}

		if (GUILayout.Button("Clear queued events"))
		{
			queuedEvents.Clear();
		}

		if (queuedEvents.Count > 0)
		{
			if (GUILayout.Button("Trigger events now"))
			{
				Server.queueJSONMessages(queuedEvents, false);
			}
			if (GUILayout.Button("Reset game and trigger events before server event registration"))
			{
				PreferencesBase prefs = SlotsPlayer.getPreferences();
				string messages = buildJSONString();
				prefs.SetString(DebugPrefs.FAKE_SERVER_EVENT_QUEUE, messages);
				Glb.resetGame("Dev menu");
			}    
		}
		
		GUILayout.EndVertical();
		
	}

	private static string buildJSONString()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("{");
		sb.AppendLine("\t\"events\": [");

		for (int i = 0; i < queuedEvents.Count; i++)
		{
			sb.AppendLine(queuedEvents[i].ToString());
		}
		
		sb.AppendLine("\t]");
		sb.AppendLine("}");
		return sb.ToString();
	}

	public static void resetStaticClassData()
	{
		eventText = "";
		scrollPosition = Vector2.zero;
		queuedEvents.Clear();
	}
}
