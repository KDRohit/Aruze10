using UnityEngine;
using System.Collections;

public class StatsWeeklyRace
{
	public static void logLeaderboard(int division, string zoneInfo, string state, string rank, int score)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "my_division",
			klass: division.ToString(),
			family: zoneInfo,
			genus: state,
			val: score,
			milestone: rank
		);
	}

	public static void logViewFriends(int division, int numFriends)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "friends",
			klass: division.ToString(),
			family: "",
			genus: "view",
			val: numFriends
		);
	}

	public static void logMotd(int division, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "player_tip",
			klass: division.ToString(),
			family: "",
			genus: state
		);
	}

	public static void logFinalStandings(int division, int previousDivision, int rank, int score, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "final_standings_review",
			klass: division.ToString(),
			family: previousDivision.ToString(),
			genus: state,
			val: score,
			milestone: rank.ToString()
		);
	}

	public static void logPromotion(int division, int previousDivision, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "final_standings_promotion",
			klass: division.ToString(),
			family: previousDivision.ToString(),
			genus: state
		);
	}

	public static void logViewReward(int chestId, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "reward_open",
			klass: chestId.ToString(),
			genus: state
		);
	}

	public static void logCollectReward(int chestId, int coinAmount, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "reward_collect",
			klass: chestId.ToString(),
			family: "",
			genus: state,
			val: coinAmount
		);
	}

	public static void logBoostDialog(int division, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "boost",
			klass: division.ToString(),
			family: "",
			genus: state
		);
	}

	public static void logOverlayClick(int division, int rank)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "top_nav",
			kingdom: "weekly_race_icon",
			phylum: "final_standings_promotion",
			klass: division.ToString(),
			family: "",
			genus: "click",
			val: 0,
			milestone: rank.ToString()
		);
	}

	public static void logBottomOverlayClick(int division, int rank)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "weekly_race_icon",
			phylum: "final_standings_promotion",
			klass: division.ToString(),
			family: "",
			genus: "click",
			val: 0,
			milestone: rank.ToString()
		);
	}

	public static void logDailyRivalsResults(int division, long playerScore, long rivalScore, long amountWon, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "daily_rival",
			phylum: "daily_rival_results",
			klass: division.ToString(),
			family: playerScore.ToString(),
			genus: state,
			milestone: rivalScore.ToString(),
			val: amountWon
		);
	}

	public static void logDailyRivalsPairing(int division, string rivalsZid, string state)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "daily_rival",
			phylum: "daily_rival_pairing",
			klass: division.ToString(),
			family: rivalsZid,
			genus: state
		);
	}


	public static void logWeeklyRaceDivisions(int division)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "weekly_race",
			phylum: "division_list",
			klass: division.ToString(),
			family: "",
			genus: "view"
		);
	}

	public static void handleRaceState(WeeklyRace race)
	{
		if (race.isInPromotion)
		{
			logLeaderboard(race.division, "promotion", "view", race.competitionRank.ToString(), (int)race.playersScore);
		}
		else if (race.isInRelegation)
		{
			logLeaderboard(race.division, "relegation", "view", race.competitionRank.ToString(), (int)race.playersScore);
		}
		else
		{
			logLeaderboard(race.division, "neutral", "view", race.competitionRank.ToString(), (int)race.playersScore);
		}
	}
}
