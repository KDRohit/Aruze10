
using UnityEngine;

public class RichPassMultiJackpotLobbyOptionDecorator1x2 : MultiJackpotLobbyOptionDecorator1x2
{
    private const string LOCKED_STATE = "locked";
    private const string UNLOCKED_STATE = "unlocked";

    [SerializeField] private ObjectSwapper objectSwap;

    public static new GameObject overlayPrefab;
    
    public new const float TEXTURE_RELATIVE_Y = 0.5f;
    public new const float TEXTURE_ANCHOR_PIXEL_OFFSET_Y = -125.0f;

    private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Rich Pass Multi Jackpot Lobby Option Decorator 1x2";

    protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
    {
        overlayPrefab = prefabFromAssetBundle;
    }
    
    public static void loadPrefab(GameObject parentObjectect, LobbyOptionButtonGeneric option)
    {
        prepPrefabForLoading(overlayPrefab, parentObjectect, option, LOBBY_PREFAB_PATH, typeof(RichPassMultiJackpotLobbyOptionDecorator1x2));
    }
    
    protected override void setup()
    {
        if (MainLobby.hirV3 != null)
        {
            MainLobby.hirV3.masker.addObjectArrayToList(jackpotLabels);
        }

        if (parentOption != null && parentOption.option != null && parentOption.option.game != null)
        {
            parentOption.option.game.registerMultiProgressiveLabels(jackpotLabels, true);

            // this overlay uses a 1x1 texture in the top half, so we need to resize the anchor/stretchy settings for the texture which defualted to 1x2
            UIStretch stretcher = parentOption.gameImageStretch;
            if (stretcher != null)
            {
                stretcher.relativeSize.y = TEXTURE_RELATIVE_Y;
            }

            UIAnchor anchor = parentOption.gameImageAnchor;
            if (anchor != null)
            {
                anchor.pixelOffset.y = TEXTURE_ANCHOR_PIXEL_OFFSET_Y;
            }

            parentOption.refresh();	
        }
        
        bool isUnlocked = CampaignDirector.richPass != null && CampaignDirector.richPass.isActive &&
                          CampaignDirector.richPass.passType == "gold";
        objectSwap.setState(isUnlocked ? UNLOCKED_STATE : LOCKED_STATE);
        
        if (CampaignDirector.richPass != null)
        {
            CampaignDirector.richPass.onPassTypeChanged -= onPassTypeChanged;
            CampaignDirector.richPass.onPassTypeChanged += onPassTypeChanged;
        }
    }
    
    public static void cleanup()
    {
        overlayPrefab = null;
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
