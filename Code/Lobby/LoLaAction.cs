﻿using UnityEngine;
using System.Collections;

public class LoLaAction
{
	public string action = "";
	public string imagePath = "";
	
	public static void populateAll(JSON data)
	{
		foreach (JSON json in data.getJsonArray("actions"))
		{
			new LoLaAction(json);
		}
	}
	
	private LoLaAction(JSON json)
	{
		action = json.getString("action", "");
		string imagePath = json.getString("image", "");
		if (!string.IsNullOrWhiteSpace(imagePath))
		{
			this.imagePath = "lobby_options" + imagePath;
		}

		if (action.Contains(','))
		{
			string[] actionsList = action.Split(',');
			action = actionsList[0];
			for (int i = 1; i < actionsList.Length; i++)
			{
				new LoLaAction(json, actionsList[i]);
			}
		}
		
		foreach (JSON lobbyJson in json.getJsonArray("lobby_display"))
		{
			new LoLaLobbyDisplay(lobbyJson, this);
		}
	}
	
	private LoLaAction(JSON json, string lolaAction)
	{
		action = lolaAction;

		string imagePath = json.getString("image", "");
		if (!string.IsNullOrWhiteSpace(imagePath))
		{
			this.imagePath = "lobby_options" + imagePath;
		}
		
		foreach (JSON lobbyJson in json.getJsonArray("lobby_display"))
		{
			new LoLaLobbyDisplay(lobbyJson, this);
		}
	}
	
}
