
using UnityEngine;

[System.Obsolete]
public class RichPassLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
    private const string LOCKED_STATE = "locked";
    private const string UNLOCKED_STATE = "unlocked";
    
    public static GameObject overlayPrefab;

    [SerializeField] private ObjectSwapper objectSwap;

    private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Rich Pass Lobby Option Decorator 1x2";

    public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option)
    {
        prepPrefabForLoading(overlayPrefab, parentObject, option, LOBBY_PREFAB_PATH, typeof(RichPassLobbyOptionDecorator1x2));
    }
	
    public static void cleanup()
    {
        overlayPrefab = null;
    }
    
    protected override void setup()
    {
        bool isUnlocked = CampaignDirector.richPass != null && CampaignDirector.richPass.isActive &&
                          CampaignDirector.richPass.isPurchased();
        objectSwap.setState(isUnlocked ? UNLOCKED_STATE : LOCKED_STATE);

        if (CampaignDirector.richPass != null)
        {
            CampaignDirector.richPass.onPassTypeChanged -= onPassTypeChanged;
            CampaignDirector.richPass.onPassTypeChanged += onPassTypeChanged;
        }
    }

    protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
    {
        overlayPrefab = prefabFromAssetBundle;
    }

    private void onPassTypeChanged(string newType)
    {
        switch (newType)
        {
            case "gold":
                objectSwap.setState(UNLOCKED_STATE);
                break;
            
            default:
                objectSwap.setState(LOCKED_STATE);
                break;
        }
    }

    private void OnDestroy()
    {
        if (CampaignDirector.richPass != null)
        {
            CampaignDirector.richPass.onPassTypeChanged -= onPassTypeChanged;
        }
    }
}
