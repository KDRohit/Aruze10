using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This decorator is to indicate Personalized Content games of all kinds for 1x2 squares
*/

public class PersonalizedContentLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	public static GameObject overlayPrefab;
	public static string gameKey = "";

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Personalized Content Lobby Option Decorator 1x2";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(PersonalizedContentLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefab = null;
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		overlayPrefab = prefabFromAssetBundle;
	}	
}
