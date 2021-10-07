using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ExtraFeatureLobbyOptionDecorator : LobbyOptionDecorator
{
    public static GameObject overlayPrefab = null;
    
    private const string LOBBY_PREFAB_PATH= "Lobby Option Decorators/Extra Feature Lobby Option Decorator";
    

    [SerializeField] public TextMeshPro jackpotTMPro;
    [SerializeField] public ObjectSwapper swapper;

    public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
    {
        prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(ExtraFeatureLobbyOptionDecorator));
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
                
			case ExtraFeatureType.STICK_AND_WIN_NO_PROGRESSIVE:
				return "stick_and_win_no_progressive";
            
            default:
                return "super_spin";
        }
    }
    
    protected override void setup()
    {
        if (parentOption != null && parentOption.option != null && parentOption.option.game != null)
        {   
            swapper.setState(getState(parentOption.option.game.extraFeatureType));    
        }
        else
        {
            //disable this feature decorator
            SafeSet.gameObjectActive(this.gameObject, false);
            return;
        }
        
        
        if (MainLobby.hirV3 != null && MainLobby.hirV3.masker != null)
        {
            MainLobby.hirV3.masker.addObjectToList(jackpotTMPro);
        }

        registerProgressiveJackpotLabel(jackpotTMPro);
    }
}
