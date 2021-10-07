using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RoyalRushLobbyOptionDecorator : LobbyOptionDecorator
{
	public static GameObject overlayPrefab;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Royal Rush Lobby Option Decorator";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(RoyalRushLobbyOptionDecorator), false, ".prefab");
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
