using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Mystery Gift games of all kinds for 1x1 squares
*/

public class MysteryGiftLobbyOptionDecorator : LobbyOptionDecorator
{
	[SerializeField] private GameObject hotStreak;
	
	public static GameObject overlayPrefab;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Mystery Gift Lobby Option Decorator";

	void Awake()
	{
		toggleHotStreak(MysteryGift.isIncreasedMysteryGiftChance);
	}
	
	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(MysteryGiftLobbyOptionDecorator));
	}
	
	public static void cleanup()
	{
		overlayPrefab = null;
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		overlayPrefab = prefabFromAssetBundle;
	}

	public void toggleHotStreak(bool enabled)
	{
		if (hotStreak != null)
		{
			hotStreak.SetActive(enabled);
		}
	}
}
