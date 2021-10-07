using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using CustomLog;
using TMPro;

/**
Controls the look and behavior of the Land of Oz lobby.
*/

public class LOZLobby : ChallengeLobby
{
	public const string BUNDLE = "feature_land_of_oz";
	public const string LOBBY_PREFAB_PATH = "Features/Land of Oz/Lobby Prefabs/Lobby LOZ Panel";
	public const string OPTION_PREFAB_PATH = "Features/Land of Oz/Lobby Prefabs/Lobby Option LOZ";
	public const string OPTION_PREFAB_PORTAL_PATH = "Features/Land of Oz/Lobby Prefabs/Lobby Option LOZ Portal";
	public const string OPTION_PREFAB_JACKPOT_PATH = "Features/Land of Oz/Lobby Prefabs/Lobby Option LOZ Jackpot";
	public const string INGAME_SIDEBAR_PREFAB_PATH = "Features/Land of Oz/Misc Prefabs/Land of Oz Objectives";

	public static bool isBeingLazilyLoaded
	{
		get { return AssetBundleManager.shouldLazyLoadBundle(LOZLobby.BUNDLE); }
	}

	protected override void setLobbyData()
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.LOZ;
		lobbyInfo = LobbyInfo.find(LobbyInfo.Type.LOZ);
		
		if (lobbyInfo == null)
		{
			Debug.LogError("No LOZ lobby data found.");
		}

		ChallengeLobby.sideBarUI = assetData.sideBarPrefab;
	}

	protected override void setChallengeCampaign()
	{
		ChallengeLobbyCampaign.currentCampaign = CampaignDirector.find(assetData.campaignName) as ChallengeLobbyCampaign;
	}

	// add this return to ChallengeLobby.lobbyAssetDataList
	public static LobbyAssetData setAssetData()
	{
		assetData = new LobbyAssetData("challenge_loz", "feature_land_of_oz");
		assetData.lobbyPrefabPath = LOBBY_PREFAB_PATH;
		assetData.optionPrefabPath = OPTION_PREFAB_PATH;
		assetData.portalPrefabPath = OPTION_PREFAB_PORTAL_PATH;
		assetData.jackpotPrefabPath = OPTION_PREFAB_JACKPOT_PATH;
		assetData.sideBarPrefabPath = INGAME_SIDEBAR_PREFAB_PATH;

		// audio maps
		assetData.audioMap = new Dictionary<string, string>()
		{
			{ LobbyAssetData.TRANSITION, 				"TransitionLOOZ" },
			{ LobbyAssetData.MUSIC, 					"LobbyLOOZBg" }
		};
		
		return assetData;
	}

	/*=========================================================================================
	ANIMATING
	=========================================================================================*/
	protected override IEnumerator finishTransition()
	{
		ChallengeLobbyCampaign campaign = CampaignDirector.find(lobbyAssetData.campaignName) as ChallengeLobbyCampaign;
		
		float currentScroll = 0.0f;
		
		yield return null;	// Give the ListScroller a frame to initialize before calling scrollToItem().
		
		// First set to the current option.
		// If the first time here in this session, we will jump to the last option then scroll back to this.
		int currentOption = getLastUnlockedGameIndex(campaign);
		
		if (campaign.isComplete)
		{
			// We know we're resetting to the first one if showing the celebration.
			currentOption = 0;
		}
		
		scroller.scrollToItem(itemMap[currentOption]);

		if (isFirstTime || doShowCelebrationLozLobby)
		{
			currentScroll = scroller.normalizedScroll;
			// Jump to the last option.
			scroller.scrollToItem(itemMap[itemMap.Count - 1]);
		}

		yield return StartCoroutine(BlackFaderScript.instance.fadeTo(0.0f));
		
		iTween.ValueTo(gameObject,
			iTween.Hash(
				"from", 1.0f,
				"to", BACKGROUND_TINT,
				"time", BACKGROUND_TINT_TIME,
				"onupdate", "updateBackgroundColor"
			)
		);

		if (doShowCelebrationLozLobby && !campaign.isComplete)
		{
			yield return doRollup();
		}
		
		if (doShowCelebrationLozLobby || isFirstTime)
		{
			yield return animateScroller(currentScroll);
		}

		didFinishIntro = true;
		isFirstTime = false;

		CustomPlayerData.setValue(CustomPlayerData.LOZ_LOBBY_COMPLETE_SEEN, true);
	}

	protected override IEnumerator animateScroller(float currentScroll)
	{
		if (currentScroll != scroller.normalizedScroll)
		{
			yield return new WaitForSeconds(0.5f);
			StartCoroutine(scroller.animateScroll(currentScroll, SCROLL_TIME));
			while (scroller.isAnimatingScroll)
			{
				yield return null;
			}
		}

		ChallengeLobbyCampaign campaign = CampaignDirector.find(lobbyAssetData.campaignName) as ChallengeLobbyCampaign;
		// show objectives update if there are currently remaining missions to be completed
		if (doShowCelebrationLozLobby && !campaign.isComplete)
		{
			LandOfOzAchievementsUpdatedDialog.showDialog();
		}
	}

	protected override IEnumerator doRollup()
	{
		if (doShowCelebrationLozLobby)
		{
			ChallengeLobbyCampaign campaign = CampaignDirector.find(lobbyAssetData.campaignName) as ChallengeLobbyCampaign;
		
			Audio.play("NextTierIncrementLOOZ");
			
			// Show the celebration before finishing the scroll to the current lobby option.
			if (jackpotOptionButton != null)
			{
				yield return new WaitForSeconds(1);
				jackpotOptionButton.jackpotAnimator.SetTrigger("Celebrate");
				// We can't yield on SlotUtils.rollup() here because it uses RoutineRunner (TICoroutineMonoBehaviour) internally, but this lobby script is MonoBehaviour.
				StartCoroutine
				(
					SlotUtils.rollup
					(
						  0L
						, campaign.currentJackpot
						, jackpotOptionButton.jackpotLabel
						, true
						, JACKPOT_OPTION_ROLLUP_TIME
						, true
						, true
						, "NextTierIncrementLoopLOOZ"
						, "NextTierIncrementTermLOOZ"						
					)
				);
			}
			yield return new WaitForSeconds(JACKPOT_OPTION_ROLLUP_TIME);
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public override int getLastUnlockedGameIndex(ChallengeLobbyCampaign campaign)
	{
		for (int i = campaign.missions.Count - (LOZCampaign.NUM_TIERS - 1); --i >= 0; )
		{
			string gameKey = campaign.missions[i].objectives[0].game;
			if (LobbyGame.find(gameKey).isUnlocked)
			{
				return i;
			}
		}
		return 0;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public static bool doShowCelebrationLozLobby
	{
		get
		{
			return !CustomPlayerData.getBool(CustomPlayerData.LOZ_LOBBY_COMPLETE_SEEN, true);
		}
	}

	public override LobbyAssetData lobbyAssetData
	{
		get
		{
			return assetData;
		}
	}

	public static void onReload(Dict args = null)
	{
		LobbyLoader.lastLobby = LobbyInfo.Type.LOZ;
	}

	public static void onLoadBundleRequest()
	{
		AssetBundleManager.downloadAndCacheBundle(assetData.bundleName, true, true, blockingLoadingScreen:false);
	}

	new public static void resetStaticClassData(){}
}
 
