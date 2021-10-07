using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyAssetData
{	
	// =============================
	// PUBLIC
	// =============================
	public GameObject lobbyPrefab;
	public GameObject optionPrefab;
	public GameObject portalPrefab;
	public GameObject jackpotPrefab;
	public GameObject sideBarPrefab;
	public GameObject mainLobbyOptionPrefab;

	public virtual string lobbyPrefabPath     { get; set; }
	public virtual string optionPrefabPath    { get; set; }
	public virtual string portalPrefabPath    { get; set; }
	public virtual string jackpotPrefabPath   { get; set; }
	public virtual string sideBarPrefabPath   { get; set; }
	public virtual string mainLobbyOptionPath { get; set; } // In case we want to surface a lobby option in addition to the room drawer.

	public string themeName = "";
	public Dictionary<string, string> audioMap = null;
	
	public string campaignName { get; protected set; }
	public string bundleName { get; protected set; }
	public bool succesfulDownload { get; private set; }
	
	// =============================
	// CONST PUBLIC
	// =============================
	// audio map keys
	public const string MUSIC = "music";
	public const string TRANSITION = "transition";
	public const string DIALOG_OPEN = "dialog_open";
	public const string DIALOG_CLOSE = "dialog_close";
	public const string OBJECTIVE_TICK = "objective_tick";
	public const string OBJECTIVE_FADE = "objective_fade";
	public const string OBJECTIVE_COMPLETE = "objective_complete";
	public const string ALL_OBJECTIVES_COMPLETE = "all_objectives_complete";
	public const string UNLOCK_NEW_GAME = "unlock_new_game";
	public const string COLLECT_NEW_GAME = "collect_new_game";
	public const string JACKPOT_ROLLUP = "jackpot_rollup";
	public const string JACKPOT_TERM = "jackpot_term";

	// Non standard sounds
	public const string COLLECT_JACKPOT = "collect_jackpot";
	public const string CLICK_PLAY = "click_play"; // When you go into a game from a challenge lobby
	public const string ON_MISSION_COMPLETE = "mission_complete"; // Played by the dialog when the mission finishes
	public const string MOTD_OPEN = "motd_open";

	public LobbyAssetData(string campaignName, string bundleName)
	{
		succesfulDownload = true;
		this.campaignName = campaignName;
		this.bundleName = bundleName;
	}

	// Used by LobbyLoader to preload asset bundle.
	public void bundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (assetPath == lobbyPrefabPath)
		{
			lobbyPrefab = obj as GameObject;
		}

		if (assetPath == optionPrefabPath)
		{
			optionPrefab = obj as GameObject;
		}

		if (assetPath == portalPrefabPath)
		{
			portalPrefab = obj as GameObject;
		}

		if (assetPath == jackpotPrefabPath)
		{
			jackpotPrefab = obj as GameObject;
		}

		if (assetPath == sideBarPrefabPath)
		{
			sideBarPrefab = obj as GameObject;
		}

		if (assetPath == mainLobbyOptionPath)
		{
			mainLobbyOptionPrefab = obj as GameObject;
		}
	}

	public string getAudioByKey(string key)
	{
		if (audioMap.ContainsKey(key))
		{
			return audioMap[key];
		}
		return null;
	}

	// Used by LobbyLoader to preload asset bundle.
	public void bundleLoadFailure(string assetPath, Dict data = null)
	{
		succesfulDownload = false;
		Debug.LogError(string.Format("Failed to download {0}: {1}\n{0} lobby option will not appear.", campaignName, assetPath));
	}
}