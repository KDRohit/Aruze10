/* HIR-91922: removing toasters. File will be deleted as part of this ticket HIR-92857
using UnityEngine;
using System;
using System.Reflection;
using Com.Scheduler;

class DevGUIMenuWeeklyRace : DevGUIMenu
{
	private const string FAKE_COMPLETE_DATA_LOSS = "{\"type\": \"daily_rivals_complete\",\"event\": \"CLfiheHLxVfDNxqkikcNL7dU9MWXjo1gMGmEyEF5sBhQK\",\"creation_time\": \"1558632624\",\"reward_amount\": \"0\",\"score\": \"0\",\"rival_zid\": \"99146309814\",\"rival_score\": \"13539\",\"rival_profile \": \" \"}";
	private const string FAKE_COMPLETE_DATA_WIN = "{\"type\": \"daily_rivals_complete\",\"event\": \"CLfiheHLxVfDNxqkikcNL7dU9MWXjo1gMGmEyEF5sBhQK\",\"creation_time\": \"1558632624\",\"reward_amount\": \"5000\",\"score\": \"18000\",\"rival_zid\": \"99146309814\",\"rival_score\": \"13539\",\"rival_profile \": \" \"}";

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();

		GUILayout.Label("Toaters/Alerts");
		if (GUILayout.Button("Promotion Entered"))
		{
			WeeklyRaceAlertDirector.showPromotionZone();
		}

#if !ZYNGA_PRODUCTION
		if (GUILayout.Button("Force show top bar button"))
		{
			if (Overlay.instance != null && Overlay.instance.topV2 != null)
			{
				Overlay.instance.topV2.devForceWeeklyRace();
			}
		}
#endif

		if (GUILayout.Button("Promotion Exit"))
		{
			WeeklyRaceAlertDirector.showPromotionZoneExit();
		}

		if (GUILayout.Button("Drop Zone Entered"))
		{
			WeeklyRaceAlertDirector.showDropZone();			
		}

		if (GUILayout.Button("Drop Zone Exit"))
		{
			WeeklyRaceAlertDirector.showDropZoneExit();
		}

		if (GUILayout.Button("Leader"))
		{
			WeeklyRaceAlertDirector.showLeaderAlert();
		}

		if (GUILayout.Button("Race Ending"))
		{
			WeeklyRaceAlertDirector.showRaceEnding();
		}

		if (GUILayout.Button("Rival Passed"))
		{
			WeeklyRaceAlertDirector.showRivalPassed();
		}

		if (GUILayout.Button("Rival Lead"))
		{
			WeeklyRaceAlertDirector.showRivalLead();
		}

		if (GUILayout.Button("Rival Ending"))
		{
			WeeklyRaceAlertDirector.showRivalEnding();
		}

		if (GUILayout.Button("Rival Paired"))
		{
			WeeklyRaceAlertDirector.showRivalPairing(true);
		}

		if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.hasRival)
		{
			if (GUILayout.Button("Rival Won"))
			{
				WeeklyRaceAlertDirector.showRivalWon(WeeklyRaceDirector.currentRace.rivalsRacerInstance.member, WeeklyRaceDirector.currentRace.rivalsRacerInstance.name);
			}
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		GUILayout.Label("Testing promotions is: " + (WeeklyRaceResults.LOCAL_TESTING_PROMOTION ? "enabled" : "disabled"));
		GUILayout.Label("Testing demotions is: " + (WeeklyRaceResults.LOCAL_TESTING_DEMOTION ? "enabled" : "disabled"));

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Promotion Testing: " + (WeeklyRaceResults.LOCAL_TESTING_PROMOTION ? "enabled" : "disabled")))
		{
			WeeklyRaceResults.LOCAL_TESTING_PROMOTION = !WeeklyRaceResults.LOCAL_TESTING_PROMOTION;
			WeeklyRaceResults.LOCAL_TESTING_DEMOTION = false;
		}

		if (GUILayout.Button("Demotion Testing: " + (WeeklyRaceResults.LOCAL_TESTING_DEMOTION ? "enabled" : "disabled")))
		{
			WeeklyRaceResults.LOCAL_TESTING_DEMOTION = !WeeklyRaceResults.LOCAL_TESTING_DEMOTION;
			WeeklyRaceResults.LOCAL_TESTING_PROMOTION = false;
		}

		if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.hasRival)
		{
			if (GUILayout.Button("Show Rival Pairing Dialog"))
			{
				DailyRivalsDialog.showDialog(WeeklyRaceDirector.currentRace, true);
			}

			if (GUILayout.Button("Show Rival - Player Lost Dialog"))
			{
				DailyRivalsCompleteDialog.showDialog(new JSON(FAKE_COMPLETE_DATA_LOSS));
			}

			if (GUILayout.Button("Show Rival - Player Won Dialog"))
			{
				DailyRivalsCompleteDialog.showDialog(new JSON(FAKE_COMPLETE_DATA_WIN));
			}
		}

		if (WeeklyRaceDirector.currentRace != null && WeeklyRaceDirector.currentRace.hasRival)
		{
			if (GUILayout.Button("Show Rival Pairing Dialog"))
			{
				DailyRivalsDialog.showDialog(WeeklyRaceDirector.currentRace, true);
			}
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Show Results Dialog"))
		{
			if (WeeklyRaceDirector.currentRace != null)
			{
				Scheduler.addDialog("weekly_race_results", Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
			}
		}


		if (GUILayout.Button("Show Rewards Dialog"))
		{
			if (WeeklyRaceDirector.currentRace != null)
			{
				//Scheduler.addDialog("weekly_race_rewards", Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace, D.AMOUNT, 1000L));
				string testData =
				"{ \"reward_data\": { \"feature_name\":\"weekly_race\",\"type\":\"rewards_bundle\","
				+ "\"granted_events\":["
				+ "{"
				+ "\"type\":\"coin_reward\","
				+ "\"feature_name\":\"weekly_race\","
				+ "\"added_value\":200,"
				+ "\"old_value\":3000,"
				+ "\"new_value\":3200"
				+ "},"
				+ "{"
				+ "\"event\":\"Aqvf6nV0PVwEiw7Sq2M4X5POkqcPbzYgyZYst0hwMJIuxSXD\","
				+ "\"type\":\"collectibles_pack_reward\","
				+ "\"feature_name\":\"weekly_race\","
				+ "\"pack_key\":\"spin_pack_hard_1_0\","
				+ "\"pack_dropped_events\":{"
				+ "\"movie_reels\":{"
				+ "\"type\":\"collectible_pack_dropped\","
				+ "\"album\":\"movie_reels\","
				+ "\"pack\":\"spin_pack_hard_1_0\","
				+ "\"rewards\":{"
				+ "\"sets\":{\"003_Grease_MovieReels_Set\":\"224000\"}"
				+ "},"
				+ "\"cards\":["
				+ "\"009_Superman_MovieReels\","
				+ "\"008_Superman_MovieReels\","
				+ "\"006_Superman_MovieReels\""
				+ "],"
				+ "\"source\":\"weekly_race\""
				+ "}"
				+ "}"
				+ "}"
				+ "]}}";
				JSON data = new JSON(testData);
				WeeklyRaceDirector.onRewardReceived(data);
			}
		}

		if (GUILayout.Button("Show Boost Dialog"))
		{
			WeeklyRaceBoost.showDialog
			(
				Dict.create
				(
					D.TIME,
					30,
					D.START_TIME,
					1543245275,
					D.END_TIME,
					1606403668
				)
			);
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Test Weekly Race Stacked Alerts"))
		{
			WeeklyRaceAlertDirector.showPromotionZone();
			WeeklyRaceAlertDirector.showPromotionZoneExit();
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showDropZoneExit();

			string expectedResult = "drop_zone_exit";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		if (GUILayout.Button("Test Weekly Race Stacked Alerts 2"))
		{
			WeeklyRaceAlertDirector.showPromotionZoneExit();
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showDropZoneExit();

			string expectedResult = "drop_zone_exit";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		if (GUILayout.Button("Test Weekly Race Stacked Alerts 3"))
		{
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showPromotionZone();
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showPromotionZone();

			string expectedResult = "promotion_zone";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		if (GUILayout.Button("Test Weekly Race Stacked Alerts 4"))
		{
			WeeklyRaceAlertDirector.showPromotionZoneExit();
			WeeklyRaceAlertDirector.showPromotionZone();
			WeeklyRaceAlertDirector.showPromotionZoneExit();
			WeeklyRaceAlertDirector.showPromotionZone();

			string expectedResult = "promotion_zone";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		if (GUILayout.Button("Test Weekly Race Stacked Alerts 5"))
		{
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showDropZoneExit();
			WeeklyRaceAlertDirector.showDropZoneExit();
			WeeklyRaceAlertDirector.showDropZone();

			string expectedResult = "drop_zone";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		if (GUILayout.Button("Test Weekly Race Stacked Alerts 6"))
		{
			WeeklyRaceAlertDirector.showDropZone();
			WeeklyRaceAlertDirector.showDropZoneExit();
			WeeklyRaceAlertDirector.showLeaderDownAlert();
			WeeklyRaceAlertDirector.showLeaderAlert();

			string expectedResult = "drop_zone_exit,leader";
			string queue = WeeklyRaceAlertDirector.currentAlertQueue;

			if (expectedResult != queue)
			{
				Debug.LogError("Failed to prune queue: " + queue);
			}
		}

		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}
*/
