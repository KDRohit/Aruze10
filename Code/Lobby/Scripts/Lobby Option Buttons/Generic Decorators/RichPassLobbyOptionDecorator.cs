using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassLobbyOptionDecorator : LobbyOptionDecorator
{

	private const string LOCKED_STATE = "locked";
	private const string UNLOCKED_STATE = "unlocked";
    
	static Dictionary<string, GameObject> overlayPrefabs = new Dictionary<string, GameObject>();

	[SerializeField] private ObjectSwapper objectSwap;

	private const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Rich Pass Lobby Option Decorator";

	public static void loadPrefab(GameObject parentObject, LobbyOptionButtonGeneric option, string sizePostfix = null)
	{
		string prefabPath = LOBBY_PREFAB_PATH + (string.IsNullOrWhiteSpace(sizePostfix) ? "" : " " + sizePostfix);
		if (!overlayPrefabs.ContainsKey(prefabPath))
		{
			overlayPrefabs.Add(prefabPath, null);
		}
		prepPrefabForLoading(overlayPrefabs[prefabPath], parentObject, option, prefabPath, typeof(RichPassLobbyOptionDecorator));
	}
	
	public static void cleanup()
	{
		overlayPrefabs.Clear();
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
		overlayPrefabs[assetPath] = prefabFromAssetBundle;
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
