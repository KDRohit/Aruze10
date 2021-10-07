using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Com.States;

/// <summary>
///   Basic data structure and state handling for any weekly race instance
/// </summary>
public class WeeklyRace : FeatureBase
{
	// =============================
	// PRIVATE
	// =============================
	public long playersScore 		{ get; private set; }	// individual players score
	public int division 			{ get; private set; }	// users current division (beginner through grand master or something, grand champion? grand...grand something...)
	public int competitionRank 		{ get; private set; }	// users current rank within their division
	public int rank 				{ get; private set; }	// users current rank within their division
	public int promotionCutoff 		{ get; private set; }	// the minimum rank required for promotion in this tier
	public int relegationCutoff 	{ get; private set; }	// the maximum rank before being relegated
	public int newDivision			{ get; private set; }	// when the race completes, the division (if any) the user moves to
	public int previousRank			{ get; private set; }	// users rank prior to the last update
	public int rivalsRank			{ get; private set; }	// rank of the current user's rival
	public int rivalsPreviousRank	{ get; private set; }	// rivals rank prior to the last update
	public int timeleft 			{ get; private set; } 	// time remaining for the weekly race from data
	public int chestRewardId		{ get; private set; }
	public string raceName 			{ get; private set; }	// name of this particular weekly race
	public bool	hasIncomingRival 	{ get; private set; }
	public bool rivalsPairingEnded  { get; private set; }
	public bool hasRivalComplete  	{ get; private set; }
	public bool isScoreInflated 	{ get; private set; } = true; // Default True

	private List<WeeklyRacePrize> prizes 	 	= new List<WeeklyRacePrize>();
	private List<WeeklyRaceRacer> racers 		= new List<WeeklyRaceRacer>();
	private List<WeeklyRaceDivision> divisions	= new List<WeeklyRaceDivision>();
	private StateMachine stateMachine 			= null;

	// =============================
	// PUBLIC
	// =============================
	public WeeklyRaceRacer playersRacerInstance = null; // this players "WeeklyRaceRacer" instance
	public WeeklyRaceRacer rivalsRacerInstance  = null;
	public GameTimerRange timer 				= null; // timer for the race
	public GameTimerRange rivalTimer 			= null; // timer for the daily rival
	public GameTimerRange cooldownTimer 		= null; // this a timer that runs when the race has ended and results are pending

	// =============================
	// CONST
	// =============================
	public const int TIERS 						= 3; 	// these are tiers per division. e.g. Rookie I, Rookie II, Rookie III etc.
	public const int NUM_DIVISIONS				= 17;
	public const int COOLDOWN_TIME				= 900;	// time to allot server for processing results
	public const string PREFAB_PATH 			= "Features/Weekly Race/Prefabs";
	public const string ACTIVE 					= "active";
	public const string PENDING_REWARD 			= "pending_reward";
	public const string ON_RACE_UPDATED			= "on_race_updated";
	public const string ON_RACE_RESULTS			= "on_race_results";
	public const string ON_RACE_REWARDS			= "on_race_rewards";
	public const int RACE_ENDING_MIN_TIME_RANGE	= 300;
	public const int RACE_ENDING_MAX_TIME_RANGE	= 900;
	public const string WEEKLY_RACE_LEADERBOARD = "weekly_race_leaderboard";

	// Delegate to refresh the leaderboard
	public static event GenericDelegate refreshLeaderBoardEvent;

	// mapping of division to a division name
	public static Dictionary<int, string> divisionNames = new Dictionary<int, string>()
	{
		{ 0, "Beginner" },
		{ 1, "Rookie" },
		{ 2, "Professional" },
		{ 3, "Master" },
		{ 4, "Grand Master" }, 
		{ 5, "Champion" },
		{ 6, "Grand Champion" }
	};

	// chest names are 1 based, im so sorry. at this point making server go back and do another adjustment so it matched
	// the rank, competition rank, and division, just wasn't worth it
	public static Dictionary<int, string> chestNames = new Dictionary<int, string>()
	{
		{ 1, "Common" },
		{ 2, "Bronze" },
		{ 3, "Silver" }, 
		{ 4, "Gold" },
		{ 5, "Epic" }
	};
	
	public static Dictionary<int, string> chestSpriteNames = new Dictionary<int, string>()
	{
		{ 1, "Chest Icon 00 Common" },
		{ 2, "Chest Icon 01 Bronze" },
		{ 3, "Chest Icon 02 Silver" },
		{ 4, "Chest Icon 03 Gold" },
		{ 5, "Chest Icon 04 Crystal" }
	};

	/*=========================================================================================
	INIT/DATA HANDLING
	=========================================================================================*/
    public WeeklyRace(JSON data)
    {
		raceName = data.getString("race_key", "Weekly Race");        
		stateMachine = new StateMachine( "statemachine_" + raceName );
		stateMachine.addState( State.READY );
		stateMachine.addState( State.COMPLETE );
		stateMachine.addState( PENDING_REWARD );
		stateMachine.addState( ACTIVE, new StateOptions( new List<string>(){ State.READY, State.IN_PROGRESS } ));
		stateMachine.updateState( State.READY );
		initializeWithData( data );
		/* HIR-91922: Removing toaster notifications
		 WeeklyRaceAlertDirector.reset();
		 */
    }

	protected override void initializeWithData(JSON data)
	{		
		//HIR-84017 We want to hide the alerts on startup
		updateData(data, false);
	}

	/// <summary>
	///   This is the main bulk of the class functionality, it actually gets called at startup, and during update events.
	/// </summary>
	private void updateData(JSON data, bool showAlerts = true)
    {
		previousRank = competitionRank;
		
		if (data != null)
		{
			raceName = data.getString("race_key", raceName);
			playersScore = data.getLong("score", playersScore);
			division = data.getInt("division", division);
			timeleft = data.getInt("time_left", timeleft);
			competitionRank = data.getInt("competition_rank", competitionRank);
			promotionCutoff = data.getInt("promotion", promotionCutoff);
			relegationCutoff = data.getInt("relegation", relegationCutoff);
			isScoreInflated = data.getBool("is_score_inflated", isScoreInflated);

			updateRacers(data);
			updatePlayersRacerData();
			parseDivisions(data);
			parsePrizes(data);
			updateRival(data);
			updateState(data, showAlerts);

			if (timer == null)
			{
				timer = GameTimerRange.createWithTimeRemaining(timeleft);
				cooldownTimer = GameTimerRange.createWithTimeRemaining(timeleft + COOLDOWN_TIME);
			}

			refreshLeaderboard();
		}
    }

	public void refreshLeaderboard()
	{
		if (refreshLeaderBoardEvent != null)
		{
			refreshLeaderBoardEvent();
		}
	}

	private void updateRival(JSON data)
	{
		JSON rivalData = data.getJSON("daily_rivals");
		if (rivalData != null)
		{
			int startTime = rivalData.getInt("start_time", 0);
			int endTime = rivalData.getInt("end_time", 0);

			hasIncomingRival = rivalData.getBool("impending_run", false);
			rivalsPairingEnded = rivalData.getBool("pairing_closed", false);

			rivalTimer = new GameTimerRange(startTime, endTime);

			if (!rivalTimer.isExpired)
			{
				rivalTimer.registerFunction(onRivalTimerExpired);
			}

			string rivalZid = rivalData.getString("rival_zid", "");
			if (rivalsRacerInstance != null)
			{
				if (rivalZid != rivalsRacerInstance.id)
				{
					rivalsRacerInstance.isRival = false;
					rivalsRacerInstance = null;
				}
				else
				{
					rivalsPreviousRank = rivalsRacerInstance.rank;
				}
			}

			rivalsRacerInstance = getRacerByZid(rivalData.getString("rival_zid", ""));

			if (rivalsRacerInstance != null && rivalsRacerInstance.member != null)
			{
				rivalsRank = rivalsRacerInstance.rank;
				rivalsRacerInstance.isRival = true;

				if (!rivalTimer.isExpired)
				{
					if (!CustomPlayerData.getBool(CustomPlayerData.HAS_SEEN_RIVAL_PAIRING, false))
					{
						DailyRivalsDialog.showDialog(this);
					}
					/* HIR-91922: Removing toaster notifications
					else
					{
						WeeklyRaceAlertDirector.showRivalPairing();
					}
					*/
				}
			}
		}

		if (rivalTimer == null || rivalTimer.isExpired)
		{
			hasRivalComplete = rivalData == null || rivalsRacerInstance != null;
		}
	}

	public void clearExpiredRival()
	{
		if (rivalTimer != null && rivalTimer.isExpired)
		{
			if (rivalsRacerInstance != null)
			{
				rivalsRacerInstance.isRival = false;
				rivalsRacerInstance = null;
			}
		}
	}

    private void updateState(JSON data, bool showAlerts = true)
    {
		if (timeleft > 0)
		{
			// the race should pretty much always be active, but it's good practice to check the machine
			if (stateMachine.can( ACTIVE ))
			{
				refreshCurrentRaceStandings();
				
				/* HIR-91922: Removing toaster notifications
				//HIR-84017 We want to hide the alerts on startup
				if (hasRankChange && showAlerts)
				{
					WeeklyRaceAlertDirector.handleRankChange();
				}
				if (timeleft >= RACE_ENDING_MIN_TIME_RANGE && timeleft <= RACE_ENDING_MAX_TIME_RANGE)
				{
					WeeklyRaceAlertDirector.showRaceEnding();
				}
				*/
			}
			// first time this function is called is during startup, which has the machine in the "ready" state
			else if (stateMachine.can( State.READY ))
			{
				stateMachine.updateState( ACTIVE );
			}
		}
		else if (!stateMachine.can( PENDING_REWARD ) || data.getString("state", "").Contains("pending"))
		{
			/* HIR-91922: Removing toaster notifications
			// only display this alert to actively playing users. a user who just entered the game
			// will not have been in the active state
			if (stateMachine.previousState == ACTIVE)
			{
				WeeklyRaceAlertDirector.showRaceEnding();
			}
			*/
			stateMachine.updateState( PENDING_REWARD );
		}
		/* HIR-91922: Removing toaster notifications
	    if (rivalTimer != null && rivalTimer.timeRemaining > 0)
	    {
		    if (rivalTimer.timeRemaining >= RACE_ENDING_MIN_TIME_RANGE && rivalTimer.timeRemaining <= RACE_ENDING_MAX_TIME_RANGE)
		    {
			    WeeklyRaceAlertDirector.showRivalEnding();
		    }
	    }
	    */
		Decs.completeEvent(ON_RACE_UPDATED);
    }

    private void parsePrizes(JSON data)
    {
		JSON[] prizeJson = data.getJsonArray("prizes");

		if (prizeJson.Length > 0)
		{
			prizes = new List<WeeklyRacePrize>();
		}

		for (int i = 0; i < prizeJson.Length; ++i)
		{
			WeeklyRacePrize prize = new WeeklyRacePrize(prizeJson[i]);
			prizes.Add(prize);
		}
    }

	private void parseDivisions(JSON data)
    {
		JSON[] divisionJson = data.getJsonArray("divisions");

		if (divisionJson.Length > 0)
		{
			divisions = new List<WeeklyRaceDivision>();
		}

		for (int i = 0; i < divisionJson.Length; ++i)
		{
			WeeklyRaceDivision div = new WeeklyRaceDivision(divisionJson[i]);
			divisions.Add(div);
		}
    }

	private void updateRacers(JSON data)
    {
	    if (data != null)
	    {
		    JSON[] racerJson = data.getJsonArray("rankings");
		    
		    if (racerJson.Length > 0)
		    {
			    for (int i = 0; i < racerJson.Length; ++i)
			    {
				    WeeklyRaceRacer r = new WeeklyRaceRacer(racerJson[i]);

				    if (!hasRacerByZid(r.id))
				    {
					    racers.Add(r);
				    }
				    else
				    {
					    WeeklyRaceRacer existingRacer = getRacerByZid(r.id);
					    existingRacer.updateData(racerJson[i]);
				    }
			    }
		    }
	    }
	    else
	    {
		    Debug.LogError(("WeeklyRace::updateRacers - Null data when updating weekly race racers"));
	    }
    }

	private void updatePlayersRacerData()
    {
		if (playersRacerInstance == null && racers.Count > 0)
		{
			for (int i = 0; i < racers.Count; ++i)
			{
				WeeklyRaceRacer r = racers[i];
				if (r.id == SlotsPlayer.instance.socialMember.zId || r.fbid == SlotsPlayer.instance.socialMember.id)
				{
					playersRacerInstance = r;
				}
			}
		}

		if (playersRacerInstance != null)
		{
			playersRacerInstance.division = division;
			playersRacerInstance.score = playersScore;
			rank = playersRacerInstance.rank;
		}
		else if (racers.Count > 0)
		{
			Debug.LogError("WeeklyRace: Player is not included in the race data!?");
		}
    }

	/*=========================================================================================
	EVENT HANDLING
	=========================================================================================*/
	public void onEventUpdated(JSON data)
	{
		updateData(data);
	}

	public void onRaceComplete(JSON data)
	{
		updateData(data);
		newDivision = data.getInt("new_division", division);
		stateMachine.updateState(PENDING_REWARD);
		int prevRank = data.getInt("competition_rank", -1);
		bool hasChest = getChestForRank(prevRank) != -1;
		WeeklyRaceResults.showDialog(hasChest, Dict.create(D.OBJECT, this));
		stateMachine.updateState(State.COMPLETE);
		Decs.completeEvent(ON_RACE_RESULTS);
	}

	public void onClaimReward(JSON data)
	{
		WeeklyRaceAction.claimReward(data.getString("event", ""));
		chestRewardId = data.getInt("chest_id", 0);
		Decs.completeEvent(ON_RACE_REWARDS);
	}

	private void onRivalTimerExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		WeeklyRaceDirector.getUpdatedRaceData();
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public bool isActive
	{
		get
		{
			return !stateMachine.can( State.COMPLETE );
		}
	}

	public bool hasPendingReward
	{
		get
		{
			return stateMachine.can( PENDING_REWARD );
		}
	}

	/// <summary>
	///   returns true if the user's new division is higher than their current one
	/// </summary>
	public bool hasPromotion
	{
		get
		{
			return newDivision > division;
		}
	}

	/// <summary>
	///   returns true if the user's new division is lower than their current one
	/// </summary>
	public bool hasRelegation
	{
		get
		{
			return newDivision < division;
		}
	}

	/// <summary>
	///   Returns true is user is within the promotion cutoff
	/// </summary>
	public bool isInPromotion
	{
		get
		{
			return competitionRank <= promotionCutoff && promotionCutoff >= 0;
		}
	}

	/// <summary>
	///   Returns true is user is within the relegation cuttoff
	/// </summary>
	public bool isInRelegation
	{
		get
		{
			return competitionRank >= relegationCutoff && relegationCutoff > 0;
		}
	}

	public bool hasRankChange
	{
		get
		{
			return competitionRank != previousRank;
		}
	}

	public List<WeeklyRaceRacer> getRacersByRank
	{
		get
		{
			racers.Sort(sortByRank);
			return racers;
		}
	}

	public List<SocialMember> getFriendsByDivision
	{
		get
		{
			List<SocialMember> friendsByDivision = new List<SocialMember>(SocialMember.allFriends);
			friendsByDivision.Sort(sortByDivision);
			return friendsByDivision;	
		}
	}

	public int timeRemaining
	{
		get
		{
			if (timer != null)
			{
				return timer.timeRemaining;
			}
			return 0;
		}
	}

	public int cooldownTimeRemaining
	{
		get
		{
			if (cooldownTimer != null)
			{
				return cooldownTimer.timeRemaining;
			}
			return 0;
		}
	}

	public string formattedTimeleft
	{
		get
		{
			System.TimeSpan t = System.TimeSpan.FromSeconds(timer.timeRemaining > 0 ? timer.timeRemaining : cooldownTimer.timeRemaining);
		
			if (t.Days > 0)
			{
				return string.Format("{0}d {1}h {2:00}m", t.Days, t.Hours, t.Minutes);
			}
			else if (t.Hours > 0)
			{
				// (e.g. 5:01:08)
				return string.Format("{0}h {1:00}m {2:00}s", t.Hours, t.Minutes, t.Seconds);
			}
		
			return string.Format("{0:00}m {1:00}s", t.Minutes, t.Seconds);
		}
	}

	public int currentNumberOfDivisions
	{
		get
		{
			if (divisions != null && divisions.Count > 0)
			{
				return divisions.Count;
			}

			return NUM_DIVISIONS;
		}
	}

	public bool hasRival
	{
		get { return rivalsRacerInstance != null; }
	}

	public bool isRivalsActive
	{
		get { return hasRival || hasIncomingRival; }
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public static int sortByRank(WeeklyRaceRacer a, WeeklyRaceRacer b)
	{
		return a.rank - b.rank;
	}

	public static int sortByDivision(SocialMember a, SocialMember b)
	{
		return b.weeklyRaceDivision - a.weeklyRaceDivision;
	}

	/// <summary>
	///   Returns the increased daily bonus amount (percent) per the division
	/// </summary>
	public int getDailyBonusForDivision(int division)
	{
		for (int i = 0; i < divisions.Count; ++i)
		{
			if (divisions[i].divisionId == division)
			{
				return divisions[i].wheelBonus;
			}
		}

		// the champ ranks are the same as grand master 3 for some stuff, so everything defaults to that being the max
		if (divisions.Count > 0 && division > 0)
		{
			return divisions[divisions.Count-1].wheelBonus;
		}
		return 0;
	}

	/// <summary>
	///   Returns the chest awarded to the specified rank for this race
	/// </summary>
	public int getChestForRank(int rank)
	{
		for (int i = 0; i < prizes.Count; ++i)
		{
			if (rank >= prizes[i].rankMin && rank <= prizes[i].rankMax)
			{
				return prizes[i].chestId;
			}
		}
	
		return -1;
	}

	/// <summary>
	/// 	Returns true if the race contains the specified zid of a racer
	/// </summary>
	public bool hasRacerByZid(string zid)
	{
		for (int i = 0; i < racers.Count; ++i)
		{
			if (racers[i].id == zid)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 	Returns racer by zid
	/// </summary>
	public WeeklyRaceRacer getRacerByZid(string zid)
	{
		if (string.IsNullOrEmpty(zid))
		{
			return null;
		}

		for (int i = 0; i < racers.Count; ++i)
		{
			if (racers[i].id == zid)
			{
				return racers[i];
			}
		}

		return null;
	}

	public bool isRankWithinPromotion(int rank)
	{
		return rank <= promotionCutoff && promotionCutoff >= 0;
	}

	public bool isRankWithinRelegation(int rank)
	{
		return rank >= relegationCutoff && relegationCutoff > 0;
	}

	// Weekly race has some very specific choreography with many dialogs, this function aids
	// in removing the leaderboard so other dialogs can be shown without use of the showImmediately flag
	// which causes many issues when traversing games/bonus games/dialogs, etc.
	public static bool clearLeaderboardFromDialogs(bool forceClose = false)
	{
		bool wasRemoved = false;

		if (forceClose && Dialog.isSpecifiedDialogShowing("weekly_race_leaderboard"))
		{
			Scheduler.removeDialog(DialogType.find("weekly_race_leaderboard"));
			Dialog.close(Dialog.instance.currentDialog);
			wasRemoved = true;
		}
		else if (!Dialog.isSpecifiedDialogShowing("weekly_race_leaderboard") && Scheduler.hasTaskWith("weekly_race_leaderboard"))
		{
			Scheduler.removeDialog(DialogType.find("weekly_race_leaderboard"));
			wasRemoved = true;
		}

		return wasRemoved;
	}

	/*=========================================================================================
	STATIC
	=========================================================================================*/
	/// <summary>
	/// Method refresh the current race standings in the leaderboard
	///  </summary>
	public void refreshCurrentRaceStandings()
	{		
		if (racers != null && racers.Count > 0 && racers.Contains(playersRacerInstance))
		{
			racers.Sort(sortByRank);
			bool hasRankMove = false;
			for (int i = 0; i < racers.Count; ++i)
			{
				WeeklyRaceRacer racer = racers[i];
				if (racer != playersRacerInstance && (hasRankMove || racer.competitionRank == competitionRank))
				{
					// stale data checks, we will force the racer to the correct rank as we see fit. this gets resolved later
					if (racer.score > playersScore)
					{
						hasRankMove = true;

						if (competitionRank <= racer.competitionRank)
						{
							competitionRank++;
							rank++;
						}
						if (racer.competitionRank > 0)
						{
							racer.competitionRank--;
							racer.rank--;
						}
					}
					else if (racer.score < playersScore)
					{
						hasRankMove = true;
						racer.competitionRank++;
						racer.rank++;
					}
				}
			}

			playersRacerInstance.rank = rank;
			playersRacerInstance.competitionRank = competitionRank;
		}
	}

	/// <summary>
	///   Returns the division name based on the player's current division (0-20)
	/// </summary>
	public static string getDivisionName(int division)
	{
		if (division <= 0)
		{
			return divisionNames[0];
		}
		// Grand champ
		else if (division >= NUM_DIVISIONS - 1)
		{
			return divisionNames[6];
		}
		
		// normal calculation
		int index = Mathf.CeilToInt((float)division/(float)TIERS);
		return divisionNames[index];
	}

	/// <summary>
	///   Returns the division group (0-6) based on the division passed in. division is a 0 based number 0-17
	///	  representing beginner > grand champion. rookie > grand master has tiers I, II, III. the number returned represents
	///   only the group which that division belongs to. E.g. division 3 would be rookie, so the group id would be 1.
	/// </summary>
	public static int getDivisionGroup(int division)
	{
		if (division <= 0)
		{
			return 0;
		}
		// Grand champ
		else if (division >= NUM_DIVISIONS - 1)
		{
			return 6;
		}
		
		// normal calculation
		int index = Mathf.CeilToInt((float)division/(float)TIERS);
		return index;
	}

	/// <summary>
	///   Returns the players tier based on their division. The tiers are basically a division, and
	///	  then rank I, II, or III within that division
	/// </summary>
	public static int getTier(int division)
	{
		// no tiers for Beginner and Grand Champion
		if (division <= 0 || division >= NUM_DIVISIONS - 1)
		{
			return 0;
		}
		
		int index = division%TIERS; // 0 based
		if (index == 0)
		{
			return 3;
		}
		return index;
	}

	/// <summary>
	///   Returns the players tier (as a string) based on their division. The tiers are basically a division, and
	///	  then rank I, II, or III within that division
	/// </summary>
	public static string getTierNumeral(int division)
	{
		// no tiers for Beginner and Grand Champion
		if (division <= 0 || division >= NUM_DIVISIONS - 1)
		{
			return "";
		}
		
		int index = division%TIERS; // 0 based
		if (index == 0)
		{
			return "III";
		}
		else if (index == 2)
		{
			return "II";
		}
		return "I";
	}

	/// <summary>
	///   Returns the division name based on the player's current division (0-20)
	/// </summary>
	public static string getChestSpriteName(int chestId)
	{
		if (chestId <= 0)
		{
			return "";
		}
		else if (chestId > chestSpriteNames.Count)
		{
			// you cheater...
			return chestSpriteNames[chestNames.Count];
		}
		
		return chestSpriteNames[chestId];
	}

	/// <summary>
	///   Returns the correct division badge sprite name based on the passed in division
	/// </summary>
	public static string getBadgeSprite(int division)
	{
		string divisionName = getDivisionName(division);
		int divisionGroup = getDivisionGroup(division);
		return string.Format("Badge 0{0} {1}", divisionGroup.ToString(), divisionName);
	}

	/// <summary>
	///   Returns the correct division tier sprite name based on the passed in division
	/// </summary>
	public static string getDivisionTierSprite(int division)
	{
		string divisionName = getDivisionName(division);
		string divisionTier = getTierNumeral(division);
		int divisionGroup = getDivisionGroup(division);
		return string.Format("Badge 0{0} {1} {2}", divisionGroup.ToString(), divisionName, divisionTier);
	}

	public static string getFullDivisionName(int division)
	{
		string divisionName = getDivisionName(division);
		string divisionTier = getTierNumeral(division);

		return divisionName + " " + divisionTier;
	}
}

/*========================================================================================
MINOR DATA CLASSES
=========================================================================================*/
public class WeeklyRacePrize
{
	public int rankMin; // the lowest number rank. So rank 1 - 12 this would be 1.
	public int rankMax; // The highest number ranked. 1-12 this would be 12
	public int chestId;

	public WeeklyRacePrize(JSON prizeInfo)
	{
		// These are base 0, so add 1.
		rankMin = prizeInfo.getInt("start_rank", 0);
		rankMax = prizeInfo.getInt("end_rank", 0);
		chestId = prizeInfo.getInt("chest_id", 0);
	}
}

public class WeeklyRaceRacer
{
	public int competitionRank = 0; // users rank in the leaderboard
	public int rank = 0;			// users position within their division
	public int division = 0;		// division the user is in
	public long score = 0;			// users score
	public string id = "";			// users zid
	public string fbid = "";		// may have to remove this
	public string firstName = "";
	public int achievementScore = 0;
	public bool isPlayer { get; private set; }
	public bool isRival { get; set; }

	public SocialMember member = null;

	public WeeklyRaceRacer(JSON data)
	{
		// We don't bail here since we'll probably want to display a blank racer, otherwise we may not know what's going on.
		if (data != null)
		{
			id 					= data.getString("id", "");
			fbid 				= data.getString("fb_id", "");
			rank 				= int.Parse(data.getString("rank", "0"));
			name 				= data.getString("name", "");
			score 				= long.Parse(data.getString("score", "0"));
			division			= int.Parse(data.getString("division", "0"));
			firstName 			= data.getString("first_name", "");
			competitionRank 	= int.Parse(data.getString("competition_rank", "0"));
			achievementScore 	= int.Parse(data.getString("achievement_score", "0"));

			string photo = data.getString("photo_url", "");
			member = CommonSocial.findOrCreate(fbid, id, "", firstName, "", achievementScore, 0, photo);
		}
		isPlayer = id == SlotsPlayer.instance.socialMember.zId;
	}

	public void updateData(JSON data)
	{
		if (data != null)
		{
			rank 				= int.Parse(data.getString("rank", "0"));
			score 				= long.Parse(data.getString("score", "0"));
			competitionRank 	= int.Parse(data.getString("competition_rank", "0"));
		}
	}

	private string _name = "";
	public string name
	{
		get
		{
			if (string.IsNullOrEmpty(firstName))
			{
				if (id == SlotsPlayer.instance.socialMember.zId)
				{
					return "You";
				}
			}

			if (string.IsNullOrEmpty(_name))
			{
				_name = "Anonymous Racer";
			}
			return _name;
		}
		set
		{
			_name = value;
		}
	}
}

public class WeeklyRaceDivision
{
	public int divisionId;
	public string divisionName;
	public int wheelBonus;
	public string iconUrl;

	public WeeklyRaceDivision(JSON data)
	{
		divisionId 		= int.Parse(data.getString("div_id", "0"));
		divisionName 	= data.getString("div_name", "");
		wheelBonus 		= int.Parse(data.getString("wheel_bonus", "0"));
		iconUrl 		= data.getString("icon_url", "");
	}
}
