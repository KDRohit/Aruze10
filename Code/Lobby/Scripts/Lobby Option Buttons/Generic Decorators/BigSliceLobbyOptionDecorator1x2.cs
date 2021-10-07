using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Big Slice games of all kinds for 1x1 squares
*/

public class BigSliceLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	public static GameObject overlayPrefab;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Big Slice Lobby Option Decorator 1x2";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(BigSliceLobbyOptionDecorator1x2));
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
