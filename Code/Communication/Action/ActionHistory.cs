using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ActionHistory
{
	private static int _cachedMaxCount = -1;
	public static int cachedMaxCount
	{
		get
		{
			if (_cachedMaxCount < 0)
			{
				_cachedMaxCount = PlayerPrefsCache.GetInt(Prefs.MAX_ACTION_HISTORY_COUNT, 0);
			}
			return _cachedMaxCount;
		}
	}

	public static bool isRecording
	{
		get
		{
			return cachedMaxCount > 0;
		}
	}

	private static List<KeyValuePair<string, JSON>> _recentReceivedEvents;
	public static List<KeyValuePair<string, JSON>> recentReceivedEvents
	{
		get
		{
			if (_recentReceivedEvents == null)
			{
				_recentReceivedEvents = new List<KeyValuePair<string, JSON>>();
			}
			return _recentReceivedEvents;
		}
	}

	private static List<KeyValuePair<string, JSON>> _recentSendActions;
	public static List<KeyValuePair<string, JSON>> recentSendActions
	{
		get
		{
			if (_recentSendActions == null)
			{
				_recentSendActions = new List<KeyValuePair<string, JSON>>();
			}
			return _recentSendActions;
		}
	}

	public static void addRecievedEvent(string key, JSON data)
	{
		if (!isRecording)
		{
			return;
		}
		if (recentReceivedEvents.Count > cachedMaxCount)
		{
			recentReceivedEvents.RemoveAt(0);
		}
		recentReceivedEvents.Add(new KeyValuePair<string, JSON>(key, data));
	}

	public static void addSentAction(string key, JSON data)
	{
		if (!isRecording)
		{
			return;
		}
		if (recentSendActions.Count > cachedMaxCount)
		{
			recentSendActions.RemoveAt(0);
		}
		recentSendActions.Add(new KeyValuePair<string, JSON>(key, data));
	}
}
