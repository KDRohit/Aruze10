using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecommendedLobbyOptionDecorator1x2: LobbyOptionDecorator
{
	static Dictionary<string, GameObject> overlayPrefabs = new Dictionary<string, GameObject>();

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Recommended Lobby Option Decorator 1x2";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		if (!overlayPrefabs.ContainsKey(LOBBY_PREFAB_PATH))
		{
			overlayPrefabs.Add(LOBBY_PREFAB_PATH, null);
		}
		prepPrefabForLoading(overlayPrefabs[LOBBY_PREFAB_PATH], parentObject, option, LOBBY_PREFAB_PATH, typeof(RecommendedLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefabs.Clear();
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		overlayPrefabs[assetPath] = prefabFromAssetBundle;
	}
}
