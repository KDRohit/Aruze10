using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using CustomLog;
using TMPro;

/**
Controls the look and behavior of the sin city lobby
*/
public class SinCityLobby : ChallengeLobby
{
	new public static LobbyAssetData assetData = null;

	public const string BUNDLE = "sin_city_strip";

	// =============================
	// CONST
	// =============================
	public const string LOBBY_PREFAB_PATH = "Features/Sin City/Lobby Prefabs/Lobby Sin City Panel";
	public const string OPTION_PREFAB_PATH = "Features/Sin City/Lobby Prefabs/Lobby Option Sin City";
	public const string OPTION_PREFAB_PORTAL_PATH = "Features/Sin City/Lobby Prefabs/Lobby Option Sin City Portal";
	public const string OPTION_PREFAB_JACKPOT_PATH = "Features/Sin City/Lobby Prefabs/Lobby Option Sin City Jackpot";
	public const string INGAME_SIDEBAR_PREFAB_PATH = "Features/Sin City/Misc Prefabs/Sin City Challenges";

	public static bool isBeingLazilyLoaded
	{
		get { return AssetBundleManager.shouldLazyLoadBundle(SinCityLobby.BUNDLE); }
	}

	protected override void setLobbyData()
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.SIN_CITY;
	 	lobbyInfo = LobbyInfo.find(LobbyInfo.Type.SIN_CITY);

	    if (lobbyInfo == null)
	    {
		    Debug.LogError("SinCityLobby - Missing lobby info");
	    }
	    
		ChallengeLobby.sideBarUI = assetData.sideBarPrefab;
	}

	protected override void setChallengeCampaign()
	{
		ChallengeLobbyCampaign.currentCampaign = CampaignDirector.find(assetData.campaignName) as ChallengeLobbyCampaign;

		if (lobbyText != null)
		{
			lobbyText.text = Localize.text("scs_lobby_text");

			if (ChallengeLobbyCampaign.currentCampaign != null && ChallengeLobbyCampaign.currentCampaign.isComplete)
			{
				lobbyText.text = Localize.text("scs_lobby_text_complete");
			}
		}
	}
	
	// add this return to ChallengeLobby.lobbyAssetDataList
	public static LobbyAssetData setAssetData()
	{
		assetData = new LobbyAssetData("challenge_sin_city_strip", "sin_city_strip");
		assetData.themeName = "SCS";
		assetData.lobbyPrefabPath = LOBBY_PREFAB_PATH;
		assetData.optionPrefabPath = OPTION_PREFAB_PATH;
		assetData.portalPrefabPath = OPTION_PREFAB_PORTAL_PATH;
		assetData.jackpotPrefabPath = OPTION_PREFAB_JACKPOT_PATH;
		assetData.sideBarPrefabPath = INGAME_SIDEBAR_PREFAB_PATH;

		// audio maps
		assetData.audioMap = new Dictionary<string, string>()
		{
			{ LobbyAssetData.TRANSITION, 				"TransitionSCS" },
			{ LobbyAssetData.MUSIC, 					"LobbyBgSCS" },
			{ LobbyAssetData.DIALOG_OPEN, 				"minimenuopen0" },
			{ LobbyAssetData.DIALOG_CLOSE, 				"minimenuclose0" },
			{ LobbyAssetData.OBJECTIVE_TICK, 			"ObjectiveTickUpSCS" },
			{ LobbyAssetData.OBJECTIVE_FADE, 			"ObjectivesAllClearedFinalSCS" },
			{ LobbyAssetData.OBJECTIVE_COMPLETE, 		"ObjectiveClearSingleSCS" },
			{ LobbyAssetData.ALL_OBJECTIVES_COMPLETE, 	"ObjectivesAllCleared" },
			{ LobbyAssetData.UNLOCK_NEW_GAME, 			"UnlockNewGameFanfareSCS" },
			{ LobbyAssetData.COLLECT_NEW_GAME, 			"UnlockNewGameCollectSCS" },
			{ LobbyAssetData.JACKPOT_ROLLUP, 			"RollupCollectJackpotSCS" },
			{ LobbyAssetData.JACKPOT_TERM, 				"RollupTermCollectJackpotSCS" }
		};
		
		return assetData;
	}

	public override LobbyAssetData lobbyAssetData
	{
		get
		{
			return assetData;
		}
	}

	public static void onLoadBundleRequest()
	{
		AssetBundleManager.downloadAndCacheBundle(assetData.bundleName, true, true, blockingLoadingScreen:false);
	}

	public static void onReload(Dict args = null)
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.SIN_CITY;
	}

	new public static void resetStaticClassData(){}
}
