using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevGUIMenuAudio2 : DevGUIMenu
{
	private static KeyValuePair<string, List<AudioInfo>> bundleCurrent = new KeyValuePair<string, List<AudioInfo>>();
	private static AudioInfo audioCurrent;
	private static Vector2 scrollPosLeft = Vector2.zero;
	private static Vector2 scrollPosRight = Vector2.zero;
	private static bool resetCounts;
	private static string searchString = "";

	public override void drawGuts()
	{
		Dictionary<string, List<AudioInfo>> bundles = new Dictionary<string, List<AudioInfo>>();

		foreach (KeyValuePair<string, AudioInfo> entry in AudioInfo.all)
		{
			if (!bundles.ContainsKey(entry.Value.bundleKey))
			{
				bundles.Add(entry.Value.bundleKey, new List<AudioInfo>());
			}

			bundles[entry.Value.bundleKey].Add(entry.Value);
		}

		//
		// Top area when an audioInfo is selected 
		//
		if (audioCurrent != null)
		{
			GUILayout.BeginHorizontal();

			GUILayout.BeginVertical();
			if (drawButton("play " + audioCurrent.keyName + " cnt" + audioCurrent.playedCount, "Click to play", true))
			{
				Audio.listenerVolume = 1.0f; // in case @ idle volume
				Audio.play(audioCurrent.keyName);
			}
			GUILayout.Label(audioCurrent.clipKey);
			GUILayout.Label("vol:" + audioCurrent.volume);
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("channel(s)");
			if (audioCurrent.channelTags.Count > 0)
			{				
				foreach (AudioChannel channel in audioCurrent.channelTags)
				{
					GUILayout.Label(channel.keyName);					
				}	
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("abort");
			if (audioCurrent.abortChannels.Count > 0)
			{				
				foreach (AudioAbortInfo channel in audioCurrent.abortChannels)
				{
					GUILayout.Label(channel.channel.keyName);
				}
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("blocking");
			if (audioCurrent.blockingChannels.Count > 0)
			{
				foreach (AudioChannel channel in audioCurrent.blockingChannels)
				{					
					GUILayout.Label(channel.keyName);					
				}
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("ducking");
			if (audioCurrent.duckChannels.Count > 0)
			{				
				foreach (AudioDuckInfo channel in audioCurrent.duckChannels)
				{
					GUILayout.Label(channel.channel.keyName);
				}
			}
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Label("event");
			if (audioCurrent.audioEvents.Count > 0)
			{
				foreach (AudioEvent audEvent  in audioCurrent.audioEvents)
				{
					GUILayout.Label(audEvent.message);
				}
			}
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
		}

		GUILayout.BeginHorizontal();

		//
		// COL LEFT, list of selectable bundles
		//
		scrollPosLeft = GUILayout.BeginScrollView(scrollPosLeft);
		int bundleCount = 0;
		
		foreach (KeyValuePair<string, List<AudioInfo>> bundle in bundles)
		{
			bundleCount++;
			GUILayout.BeginHorizontal();
			if (drawButton(bundleCount + ")" + bundle.Key + " cnt:" + bundle.Value.Count, "Click to choose", bundle.Key == bundleCurrent.Key))
			{
				bundleCurrent = bundle;
				audioCurrent = null;
				scrollPosRight.y = 0;
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();

		//
		// COL RIGHT, list of selectable audioInfo
		//
		if (bundleCurrent.Value != null)
		{
			scrollPosRight = GUILayout.BeginScrollView(scrollPosRight);

			GUILayout.BeginHorizontal();
			if (drawButton("Reset Counts", "Will reset all playedCount for all audioInfo"))
			{
				resetCounts = true;
			}

			if (drawButton("Clear Search", "Clears search text field"))
			{
				searchString = "";
			}
			GUILayout.EndHorizontal();

			searchString = GUILayout.TextField(searchString, 30);


			int audioCnt = 0;
			foreach (AudioInfo audio in bundleCurrent.Value)
			{
				if (resetCounts)
				{
					audio.playedCount = 0;
				}

				audioCnt++;

				if (searchString == "" || audio.keyName.ToLower().IndexOf(searchString.ToLower()) > -1)
				{
					GUILayout.BeginHorizontal();
					if (drawButton(audioCnt + ")" + audio.keyName + " cnt" + audio.playedCount, "Click to select", audio.Equals(audioCurrent)))
					{
						audioCurrent = audio;
					}
					GUILayout.EndHorizontal();
				}
			}

			resetCounts = false;

			GUILayout.EndScrollView();
		}

		GUILayout.EndHorizontal();
	}

	new public static void resetStaticClassData()
	{
		bundleCurrent = new KeyValuePair<string, List<AudioInfo>>();
		audioCurrent = null;
		scrollPosLeft = Vector2.zero;
		scrollPosRight = Vector2.zero;
		resetCounts = false;
		searchString = "";
	}
}
