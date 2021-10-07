using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class WeeklyRaceUnitTests
{
	private static WeeklyRace validRace = null;
	
	private static WeeklyRace generateWeeklyRace()
	{
		if (validRace == null)
		{
			JSON data = new JSON("{\"race_key\":\"WeeklyRace10Minutes_14286\",\"score\":\"250000\",\"division\":\"11\",\"wheel_bonus\":\"400\",\"time_left\":\"286\",\"state\":\"active\",\"rankings\":[{\"competition_rank\":\"0\",\"id\":\"39411032375\",\"rank\":\"0\",\"score\":\"250000\",\"fb_id\":\"1429933090645200\",\"first_name\":\"Bennett\",\"name\":\"Bennett K.\",\"photo_url\":\"https://graph.facebook.com/1429933090645200/picture?height=100&width=100\",\"vip_level\":\"3\",\"achievement_score\":\"2500\"}],\"competition_rank\":\"0\",\"promotion\":\"10\",\"relegation\":\"80\",\"prizes\":[{\"start_rank\":\"3\",\"end_rank\":\"9\",\"chest_id\":\"2\",\"chest_name\":\"Bronze\"},{\"start_rank\":\"2\",\"end_rank\":\"2\",\"chest_id\":\"3\",\"chest_name\":\"Silver\"},{\"start_rank\":\"1\",\"end_rank\":\"1\",\"chest_id\":\"4\",\"chest_name\":\"Gold\"},{\"start_rank\":\"0\",\"end_rank\":\"0\",\"chest_id\":\"5\",\"chest_name\":\"Epic\"}],\"divisions\":[{\"div_id\":\"0\",\"div_name\":\"Beginner\",\"wheel_bonus\":\"5\",\"icon_url\":\"\"},{\"div_id\":\"1\",\"div_name\":\"Rookie 1\",\"wheel_bonus\":\"10\",\"icon_url\":\"\"},{\"div_id\":\"2\",\"div_name\":\"Rookie 2\",\"wheel_bonus\":\"15\",\"icon_url\":\"\"},{\"div_id\":\"3\",\"div_name\":\"Rookie 3\",\"wheel_bonus\":\"20\",\"icon_url\":\"\"},{\"div_id\":\"4\",\"div_name\":\"Professional 1\",\"wheel_bonus\":\"25\",\"icon_url\":\"\"},{\"div_id\":\"5\",\"div_name\":\"Professional 2\",\"wheel_bonus\":\"35\",\"icon_url\":\"\"},{\"div_id\":\"6\",\"div_name\":\"Professional 3\",\"wheel_bonus\":\"50\",\"icon_url\":\"\"},{\"div_id\":\"7\",\"div_name\":\"Master 1\",\"wheel_bonus\":\"100\",\"icon_url\":\"\"},{\"div_id\":\"8\",\"div_name\":\"Master 2\",\"wheel_bonus\":\"150\",\"icon_url\":\"\"},{\"div_id\":\"9\",\"div_name\":\"Master 3\",\"wheel_bonus\":\"200\",\"icon_url\":\"\"},{\"div_id\":\"10\",\"div_name\":\"Grand Master 1\",\"wheel_bonus\":\"300\",\"icon_url\":\"\"},{\"div_id\":\"11\",\"div_name\":\"Grand Master 2\",\"wheel_bonus\":\"400\",\"icon_url\":\"\"},{\"div_id\":\"12\",\"div_name\":\"Grand Master 3\",\"wheel_bonus\":\"500\",\"icon_url\":\"\"}]}");
			// Currently broken
			// validRace = new WeeklyRace(data);
		}

		return validRace;
	}

	// [Test]
	// public static void hasValidChest()
	// {
	// 	generateWeeklyRace();
	// 	Assert.AreNotEqual(validRace.getChestForRank(1), -1);
	// }

	[Test]
	public static void hasValidDivisionNames()
	{
		Assert.AreEqual(WeeklyRace.getDivisionName(0), "Beginner");
		Assert.AreEqual(WeeklyRace.getDivisionName(1), "Rookie");
		Assert.AreEqual(WeeklyRace.getDivisionName(2), "Rookie");
		Assert.AreEqual(WeeklyRace.getDivisionName(3), "Rookie");
		Assert.AreEqual(WeeklyRace.getDivisionName(4), "Professional");
		Assert.AreEqual(WeeklyRace.getDivisionName(5), "Professional");
		Assert.AreEqual(WeeklyRace.getDivisionName(6), "Professional");
		Assert.AreEqual(WeeklyRace.getDivisionName(7), "Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(8), "Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(9), "Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(10), "Grand Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(11), "Grand Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(12), "Grand Master");
		Assert.AreEqual(WeeklyRace.getDivisionName(13), "Champion");
		Assert.AreEqual(WeeklyRace.getDivisionName(14), "Champion");
		Assert.AreEqual(WeeklyRace.getDivisionName(15), "Champion");
		Assert.AreEqual(WeeklyRace.getDivisionName(16), "Grand Champion");
		Assert.AreEqual(WeeklyRace.getDivisionName(17), "Grand Champion");
		Assert.AreEqual(WeeklyRace.getDivisionName(18), "Grand Champion");
	}

	[Test]
	public static void hasValidDivisionGroups()
	{
		Assert.AreEqual(WeeklyRace.getDivisionGroup(0), 0);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(1), 1);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(2), 1);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(3), 1);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(4), 2);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(5), 2);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(6), 2);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(7), 3);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(8), 3);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(9), 3);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(10), 4);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(11), 4);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(12), 4);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(13), 5);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(14), 5);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(15), 5);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(16), 6);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(17), 6);
		Assert.AreEqual(WeeklyRace.getDivisionGroup(18), 6);
	}

	[Test]
	public static void hasValidTiers()
	{
		Assert.AreEqual(WeeklyRace.getTier(0), 0);
		Assert.AreEqual(WeeklyRace.getTier(1), 1);
		Assert.AreEqual(WeeklyRace.getTier(2), 2);
		Assert.AreEqual(WeeklyRace.getTier(3), 3);
		Assert.AreEqual(WeeklyRace.getTier(4), 1);
		Assert.AreEqual(WeeklyRace.getTier(5), 2);
		Assert.AreEqual(WeeklyRace.getTier(6), 3);
		Assert.AreEqual(WeeklyRace.getTier(7), 1);
		Assert.AreEqual(WeeklyRace.getTier(8), 2);
		Assert.AreEqual(WeeklyRace.getTier(9), 3);
		Assert.AreEqual(WeeklyRace.getTier(10), 1);
		Assert.AreEqual(WeeklyRace.getTier(11), 2);
		Assert.AreEqual(WeeklyRace.getTier(12), 3);
		Assert.AreEqual(WeeklyRace.getTier(13), 1);
		Assert.AreEqual(WeeklyRace.getTier(14), 2);
		Assert.AreEqual(WeeklyRace.getTier(15), 3);
		Assert.AreEqual(WeeklyRace.getTier(16), 0);
		Assert.AreEqual(WeeklyRace.getTier(17), 0);
		Assert.AreEqual(WeeklyRace.getTier(18), 0);
	}

	// [Test]
	// public static void isInPromotion()
	// {
	// 	generateWeeklyRace();
	// 	Assert.AreEqual(validRace.isInPromotion, true);
	// }

	// [Test]
	// public static void isInRelegation()
	// {
	// 	generateWeeklyRace();
	// 	Assert.AreEqual(validRace.isInRelegation, false);
	// }

	// [Test]
	// public static void isRankWithinPromotion()
	// {
	// 	generateWeeklyRace();
	// 	Assert.AreEqual(validRace.isRankWithinPromotion(2), true);
	// 	Assert.AreEqual(validRace.isRankWithinPromotion(11), false);
	// }

	// [Test]
	// public static void isRankWithinRelegation()
	// {
	// 	generateWeeklyRace();
	// 	Assert.AreEqual(validRace.isRankWithinRelegation(2), false);
	// 	Assert.AreEqual(validRace.isRankWithinRelegation(81), true);
	// }

	// [Test]
	// public static void validRacerNames()
	// {
	// 	generateWeeklyRace();
	// 	List<WeeklyRaceRacer> racers = validRace.getRacersByRank;
	// 	for (int i = 0; i < racers.Count; ++i)
	// 	{
	// 		Assert.AreNotEqual(racers[i].name, "notyet");
	// 	}
	// }
}
