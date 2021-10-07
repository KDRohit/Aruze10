using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate high limit games of all kinds for 1x1 squares
*/

public class HighLimitLobbyOptionDecorator : LobbyOptionDecorator
{
	public static GameObject overlayPrefab;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/High Limit Lobby Option Decorator";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(HighLimitLobbyOptionDecorator));
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
