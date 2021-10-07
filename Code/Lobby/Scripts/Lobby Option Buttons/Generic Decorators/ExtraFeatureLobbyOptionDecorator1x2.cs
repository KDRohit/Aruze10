using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ExtraFeatureLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	
	public static GameObject overlayPrefab = null;
	
	private const string LOBBY_PREFAB_PATH_1X2 = "Lobby Option Decorators/Extra Feature Lobby Option Decorator 1x2";
	
	[SerializeField] public ObjectSwapper swapper;
	[SerializeField] public TextMeshPro jackpotTMPro;
	
	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH_1X2, typeof(ExtraFeatureLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefab = null;
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		overlayPrefab = prefabFromAssetBundle;
	}

	private static string getState(ExtraFeatureType extraFeatureType)
	{
		switch (extraFeatureType)
		{
			case ExtraFeatureType.SUPER_FAST_SPINS:
				return "super_spin";
            
			case ExtraFeatureType.PROGRESSIVE_FREE_SPINS:
				return "free_spin_fury";
            
			case ExtraFeatureType.CASH_CHAIN:
				return "cash_connect";
            
			default:
				return "super_spin";
		}
	}
    
	protected override void setup()
	{
		swapper.setState(getState(parentOption.option.game.extraFeatureType));
        
		if (MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.masker.addObjectToList(jackpotTMPro);
		}

		registerProgressiveJackpotLabel(jackpotTMPro);
	}
}
