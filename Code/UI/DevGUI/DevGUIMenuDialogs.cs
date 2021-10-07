using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.HitItRich.IDFA;

/*
A dev panel.
*/

public class DevGUIMenuDialogs : DevGUIMenu
{
	private static int testVIPLevel = 0;

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();

		string recentEarlyAccess = PlayerPrefsCache.GetString(Prefs.EARLY_ACCESS_RECENT, "(none)");
		if (GUILayout.Button("Clear recent Early Access: " + recentEarlyAccess))
		{
			PlayerPrefsCache.DeleteKey(Prefs.EARLY_ACCESS_RECENT);

			PlayerPrefsCache.Save();
		}

		if (GUILayout.Button("Clear VIP Progressive Jackpot"))
		{
			PlayerPrefsCache.DeleteKey(Prefs.SHOWN_VIP_PROGRESSIVES_JACKPOT_MOTD);
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		LobbyGame skuGameUnlock = LobbyGame.skuGameUnlock;

		if (skuGameUnlock == null)
		{
			GUILayout.Button("No SKU Game Unlock");
		}
		else
		{
			if (GUILayout.Button("SKU Game Unlock MotD"))
			{
				MOTDFramework.showMOTD(MOTDDialog.getSkuGameUnlockName());
				DevGUI.isActive = false;
			}

			if (GUILayout.Button("SKU Game Unlocked Dialog"))
			{
				GameUnlockedDialog.showDialog(skuGameUnlock, null);
				DevGUI.isActive = false;
			}
		}

		if (GUILayout.Button("IDFA Soft Prompt"))
		{
			IDFASoftPromptDialog.showDialog(IDFASoftPromptManager.SurfacePoint.GameEntry, null, null);
			DevGUI.isActive = false;
		}
		
		GUILayout.EndHorizontal();

		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("2X XP"))
		{
			XPMultiplierDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Ask for credits"))
		{
			MFSDialog.showDialog(MFSDialog.Mode.ASK);
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
	
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Buy Page Perk"))
		{
			BuyPagePerkMOTD.showDialog(isEventExpiryDialog: false);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Buy Another Perk"))
		{
			BuyPagePerkMOTD.showDialog(isEventExpiryDialog: true);
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		testVIPLevel = intInputField("VIP Dialog Level", testVIPLevel.ToString(), 1);
		testVIPLevel = Mathf.Clamp(testVIPLevel, 0, VIPLevel.maxLevel?.levelNumber ?? 0);

		if (GUILayout.Button("VIP New Level Up"))
		{
			VIPLevelUpDialog.showDialog(testVIPLevel);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Level Up bonus (requries active bons)"))
		{
			LevelUpBonusDialog.showDialog(10000, 1000, 10, 2);
		}

		if (GUILayout.Button("Normal Level up"))
		{
			int newLevel = 100;
			string levelData = "{\"level\":\"" + newLevel + "\",\"required_xp\":\"25000\",\"bonus_amount\":\"1000\",\"bonus_vip_points\":\"1\",\"max_bet\":\"500\"}";
			JSON levelJSON = new JSON(levelData);
			JSON[] levelArray = new JSON[] { levelJSON };
			ExperienceLevelData.populateAll(levelArray);
			Overlay.instance.topHIR.showLevelUpAnimation(100, 100, 100);
		}

		if (GUILayout.Button("Early Access MOTD"))
		{
			EarlyAccessDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Double Free Spins MOTD"))
		{
			DoubleFreeSpinsMOTD.showDialog("gen39");
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Daily Challenge MOTD"))
		{
			int startVal = Data.liveData.getInt("QUESTS_ACTIVE_QUEST_START_DATE", 0);
			int endVal = Data.liveData.getInt("QUESTS_ACTIVE_QUEST_END_DATE", 0);
			DailyChallenge.timerRange = new GameTimerRange(startVal, endVal, true);
			DailyChallenge.gameKey = "superman01";
			DailyChallengeMOTD.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Daily Challenge Over"))
		{
			DailyChallengeOver.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Daily Challenge Credit Grant"))
		{
			string testAwardJson = "{ \"reward_type\":\"xp\", \"event_type_id\":\"eventID333\", \"credits\":\"1\", \"xp_points\":\"44\", \"vip_points\":\"88\"}";

			/* awardJson looks like this:
			 *
			 *  'reward_type'        => ("xp"/"vip"/"credits")
	            'credits'            => $credits,
	            'event_type_id'	     => $eventType->getId(),
	            'xp_points'          => $xpPoints,
	            'vip_points'         => $vipPoints
			 */

			DailyChallengeCreditGrant.showDialog(new JSON(testAwardJson));
			DevGUI.isActive = false;
		}

		if (GameState.game != null && GameState.game.isProgressive)
		{
			if (GUILayout.Button("Prog Jackpot"))
			{
				string json = "{ \"credits\":\"1234\", \"jackpot_key\":\"" + GameState.game.progressiveJackpots[0].keyName + "\" }";

				ProgressiveJackpotDialog.showDialog(new JSON(json));
				DevGUI.isActive = false;
			}
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Mobile XPromo WOZ"))
		{
			MobileXPromoDialog.showDialog("woz", "xpromo/woz/DialogBG.png", MobileXpromo.SurfacingPoint.NONE, null);
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Buy Confirm"))
		{
			Dict args = Dict.create(
				D.BONUS_CREDITS, (long)1000,
				D.TOTAL_CREDITS, (long)1000,
				D.PACKAGE_KEY, "coin_package_10",
				D.VIP_POINTS, (int)1000,
				D.DATA, null,
				D.BASE_CREDITS, (long)1000,
				D.BONUS_PERCENT, (int)25,
				D.SALE_BONUS_PERCENT, (int)25,
				D.VIP_BONUS_PERCENT, (int)25,
				D.IS_JACKPOT_ELIGIBLE, false,
				D.TYPE, PurchaseFeatureData.Type.BUY_PAGE
			);
			BuyCreditsConfirmationDialog.showDialog(args);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Unlock Game"))
		{
			GameUnlockedDialog.showDialog(
				LobbyGame.find("oz00"),
				LobbyGame.find("com01")
			);

			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Need Credits"))
		{
			NeedCreditsSTUDDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (PurchaseFeatureData.OutOfCreditsThree != null)
		{
			if (GUILayout.Button("Need Credits 3"))
			{
				NeedCreditsMultiDialog.showDialog();
				DevGUI.isActive = false;
			}
		}
		else
		{
			if (GUILayout.Button("No Need Credits 3"))
			{
			}
		}

		if (GUILayout.Button("Age Gate"))
		{
			CustomPlayerData.setValue(CustomPlayerData.SHOW_AGE_GATE, 1);
			AgeGateDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("W2E Collect"))
		{
			WatchToEarnCollectDialog.showDialog(25000, "");
			DevGUI.isActive = false;
		}

		if (SelectGameUnlockDialog.shouldShowDialog("generic", ""))
		{
			if (GUILayout.Button("Select Game Unlock"))
			{
				SelectGameUnlockDialog.showDialog("generic", "");
				DevGUI.isActive = false;
			}
		}
		else
		{
			GUILayout.Button("No Select Game Unlock");
		}

		if (GUILayout.Button("Jackpot Unlock Game MOTD"))
		{
			JackpotUnlockGameMotd.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Increase Mystery Gift Chance MOTD"))
		{
			IncreaseMysteryGiftChanceMOTD.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Software Update"))
		{
			SoftwareUpdateDialog.showDialog();
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Credit Sweepstakes Win"))
		{
			CreditSweepstakesWinner.showDialog(CreditSweepstakes.payout, -1);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Credit Sweepstakes Lose"))
		{
			CreditSweepstakesLoser.showDialog(new List<string>());
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Credit Sweepstakes MOTD"))
		{
			CreditSweepstakesMOTD.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		GUILayout.EndHorizontal();
		if (GameState.game != null)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Paytables"))
			{
				PaytablesDialog.showDialog();
				DevGUI.isActive = false;
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Buy Progressive Win"))
		{
			BuyCreditsProgressiveWinDialog.showDialog(1000000);
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Linked VIP Status"))
		{
			LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Linked VIP Connect"))
		{
			LinkedVipConnectDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Linked VIP Program"))
		{
			LinkedVipProgramDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Linked VIP Pending"))
		{
			LinkedVIPPendingDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Linked VIP Congrats"))
		{
			LinkedVIPCongrats.showDialog("no email");
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("LOZ Updated"))
		{
			LandOfOzAchievementsUpdatedDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Incentive PN Dialog"))
		{
			IncentivizedSoftPromptDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Credit Rewards Dialog (Incentivized)"))
		{
			long creditsGifted = 2000;
			CreditRewardDialog.showDialog(creditsGifted);
			DevGUI.isActive = false;
		}

#if UNITY_WEBGL
		if (GUILayout.Button("WebGl form Flash Rewards Dialog (Incentivized)"))
		{
			long creditsGifted = 20000;
			FlashWebGLThankYouDialog.showDialog("bogusId", creditsGifted);
			DevGUI.isActive = false;
		}
#endif

		GUILayout.EndHorizontal();

		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("VIP Phone Collect"))
		{
			VIPPhoneCollectDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("VIP Phone Collect Reward"))
		{
			VIPPhoneCollectRewardDialog.showDialog(new JSON("{ \"coin_amt\":5000000 }"));
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Jackpot Days MOTD"))
		{
			JackpotDaysMOTD.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Jackpot Days Winner"))
		{
			JackpotDaysWinDialog.showDialog(100);
			DevGUI.isActive = false;
		}

		

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Canceled Purchase Dialog"))
		{
			CanceledPurchaseDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Network Rank Up"))
		{
			AchievementsRankUpDialog.showDialog(AchievementLevel.getLevel(2));
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Flash Sale"))
		{
			FlashSaleDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Streak Sale"))
		{
			StreakSaleDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("get_loot_box_reward"))
		{
			ServerAction action = new ServerAction(ActionPriority.IMMEDIATE, "get_loot_box_reward");

			ServerAction.processPendingActions(true);

            DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();

		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Missed bonus: Coins"))
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("\t\"slots_game_key\" : \":aerosmith01\",");
			sb.AppendLine("\t\"bonus_game_key\" : \":mystery_gift\",");
			sb.AppendLine("\t\"credits\" : 12000,");
			sb.AppendLine("\t\"grant\" : false");
			sb.AppendLine("}");
			BonusGameErrorDialog.handlePresentation(new JSON(sb.ToString()), true);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Missed bonus: Keys"))
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("\t\"slots_game_key\" : \":aerosmith01\",");
			sb.AppendLine("\t\"bonus_game_key\" : \":mystery_gift\",");
			sb.AppendLine("\t\"credits\" : 0,");
			sb.AppendLine("\t\"reward_keys\" : 5,");
			sb.AppendLine("\t\"grant\" : false");
			sb.AppendLine("}");
			BonusGameErrorDialog.handlePresentation(new JSON(sb.ToString()), true);
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Start Collecting Dialog"))
		{
			StartCollectingDialog.showDialog(true);
		}

		if (GUILayout.Button("Level Lotto Minigame"))
		{
			LottoBlastMinigameDialog.skipInit = true;
			LottoBlastMinigameDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("-force jackpot"))
		{
			LottoBlastMinigameDialog.skipInit = true;
			LottoBlastMinigameDialog.jackpotCheatOn = true;
			LottoBlastMinigameDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();

		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Zis SignOut Dialog"))
		{
			ZisSignOutDialog.showDialog(Dict.create(D.TITLE, ZisSignOutDialog.DISCONNECTED_HEADER_LOCALIZATION));
			DevGUI.isActive = false;
		}

		if (GUILayout.Button("Zis Manage Dialog"))
		{
			ZisManageAccountDialog.showDialog();
			DevGUI.isActive = false;
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Bundle Sale Dialog"))
		{
			BundleSaleDialog.showDialog();
			DevGUI.isActive = false;
		}
		GUILayout.EndHorizontal();
	}

	private delegate void showDialogDelegate();
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}

