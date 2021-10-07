using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Jackpot games of all kinds for 1x1 squares
*/

public class JackpotLobbyOptionDecorator : LobbyOptionDecorator
{
	public enum JackpotTypeEnum
	{
		Default = 0,
		StickAndWin
	}
	
	public TextMeshPro jackpotTMPro;
	
	public static Dictionary<string, GameObject> overlayPrefabsDict = new Dictionary<string, GameObject>();

	private const string LOBBY_DEFAULT_PREFAB_PATH = "Lobby Option Decorators/Jackpot Lobby Option Decorator";
	public const string LOBBY_STICK_AND_WIN_PREFAB_PATH = "Lobby Option Decorators/Stick And Win Jackpot Lobby Option Decorator";

	// Converts a string (most likely from slot_resource_map) into an enum type
	public static JackpotTypeEnum getTypeEnumFromString(string typeStr)
	{
		JackpotTypeEnum jackpotType;

		if (!System.Enum.IsDefined(typeof(JackpotTypeEnum), typeStr))
		{
			Debug.LogWarning("JackpotLobbyOptionDecorator.getTypeEnumFromString() - Unable to find enum value for typeStr = " + typeStr + "; returning Default type.");
			return JackpotTypeEnum.Default;
		}
		else
		{
			return (JackpotTypeEnum) System.Enum.Parse(typeof(JackpotTypeEnum), typeStr);
		}
	}
	
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

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option, JackpotTypeEnum jackpotType)
	{
		string lobbyPrefabPath = getLobbyPrefabPathForType(jackpotType);
		GameObject overlayPrefab = getOverlayPrefabForType(jackpotType);
		prepPrefabForLoading(overlayPrefab, parentObject, option, lobbyPrefabPath, typeof(JackpotLobbyOptionDecorator));
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
