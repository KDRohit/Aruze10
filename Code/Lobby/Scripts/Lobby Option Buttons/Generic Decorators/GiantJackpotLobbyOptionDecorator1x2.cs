using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Giant Jackpot games of all kinds for 1x2 squares
*/

public class GiantJackpotLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	public TextMeshPro jackpotTMPro;

	public static GameObject overlayPrefab;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Giant Jackpot Lobby Option Decorator 1x2";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(GiantJackpotLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefab = null;
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
		overlayPrefab = prefabFromAssetBundle;
	}	
}
