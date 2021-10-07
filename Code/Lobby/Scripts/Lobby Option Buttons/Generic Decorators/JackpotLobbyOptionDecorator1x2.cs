using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Giant Jackpot games of all kinds for 1x2 squares
*/

public class JackpotLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	public TextMeshPro jackpotTMPro;

	public static Dictionary<string, GameObject> overlayPrefabsDict = new Dictionary<string, GameObject>();

	public const string LOBBY_DEFAULT_PREFAB_PATH = "Lobby Option Decorators/Jackpot Lobby Option Decorator 1x2";
	public const string LOBBY_STICK_AND_WIN_PREFAB_PATH = "Lobby Option Decorators/Stick And Win Jackpot Lobby Option Decorator 1x2";

	// For the specified frame type get the path to the prefab
	private static string getLobbyPrefabPathForType(JackpotLobbyOptionDecorator.JackpotTypeEnum jackpotType)
	{
		string lobbyPrefabPath;

		switch (jackpotType)
		{
			case JackpotLobbyOptionDecorator.JackpotTypeEnum.StickAndWin:
				lobbyPrefabPath = LOBBY_STICK_AND_WIN_PREFAB_PATH;
				break;
			default:
				lobbyPrefabPath = LOBBY_DEFAULT_PREFAB_PATH;
				break;
		}

		return lobbyPrefabPath;
	}

	// For the specified frame type, try and get an already cached version of the overlay prefab
	public static GameObject getOverlayPrefabForType(JackpotLobbyOptionDecorator.JackpotTypeEnum jackpotType)
	{
		string lobbyPrefabPath = getLobbyPrefabPathForType(jackpotType);
		
		GameObject overlayPrefab = null;
		if (overlayPrefabsDict.ContainsKey(lobbyPrefabPath))
		{
			overlayPrefab = overlayPrefabsDict[lobbyPrefabPath];
		}

		return overlayPrefab;
	}

	// Allows the prefab to be set for a type, in case something external loads the prefab and wants to set it
	public static void setOverlayPrefabForType(JackpotLobbyOptionDecorator.JackpotTypeEnum jackpotType, GameObject obj)
	{
		if (obj != null)
		{
			string lobbyPrefabPath = getLobbyPrefabPathForType(jackpotType);

			if (overlayPrefabsDict.ContainsKey(lobbyPrefabPath))
			{
				overlayPrefabsDict[lobbyPrefabPath] = obj;
			}
			else
			{
				overlayPrefabsDict.Add(lobbyPrefabPath, obj);
			}
		}
	}

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option, JackpotLobbyOptionDecorator.JackpotTypeEnum jackpotType)
	{
		string lobbyPrefabPath = getLobbyPrefabPathForType(jackpotType);
		GameObject overlayPrefab = getOverlayPrefabForType(jackpotType);
		prepPrefabForLoading(overlayPrefab, parentObject, option, lobbyPrefabPath, typeof(JackpotLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefabsDict.Clear();
	}

	protected override void setup()
	{
		if (MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.masker.addObjectToList(jackpotTMPro);
		}

		registerProgressiveJackpotLabel(jackpotTMPro);
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		if (overlayPrefabsDict.ContainsKey(assetPath))
		{
			overlayPrefabsDict[assetPath] = prefabFromAssetBundle;
		}
		else
		{
			overlayPrefabsDict.Add(assetPath, prefabFromAssetBundle);
		}
	}	
}
