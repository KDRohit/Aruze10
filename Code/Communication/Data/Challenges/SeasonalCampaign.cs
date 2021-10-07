using Com.Scheduler;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

//Campaign that can have periodic as well as seasonal challenges
public class SeasonalCampaign : ChallengeCampaign
{
	public enum ChallengeType
	{
		SEASONAL,
		PERIODIC,
		NONE
	}
	public SortedDictionary<int, List<Mission>> seasonMissions { get; private set; }
	public SortedDictionary<int, List<Mission>> periodicMissions { get; private set; }
	public SortedDictionary<int, int> lockedSeasonMissions { get; private set; }

	private HashSet<Objective> periodicObjectives;
	private HashSet<Objective> seasonObjectives;
	private HashSet<int> missionIds;

	public int periodicStartTime { get; private set; }
	public GameTimerRange periodChallengesEnd { get; private set; } //Request new periodic challenge once this timer expires
	private int periodicChallengeDuration = 0;

	protected override void populateMissions(JSON data)
    {
	    missions = new List<Mission>();
	    seasonMissions = new SortedDictionary<int, List<Mission>>();
	    periodicMissions = new SortedDictionary<int, List<Mission>>();
	    lockedSeasonMissions = new SortedDictionary<int, int>();
	    
	    periodicObjectives = new HashSet<Objective>();
	    seasonObjectives = new HashSet<Objective>();
	    missionIds = new HashSet<int>();
        JSON periodic = data.getJSON("periodic_challenges");
        populatePeriodicChallenges(periodic);
        
        JSON seasonal = data.getJSON("seasonal_challenges");
        populateSeasonalChallenges(seasonal);

        JSON futureUnlockDates = data.getJSON("unlock_dates");
        populateSeasonalUnlockDates(futureUnlockDates);
    }

    private void populatePeriodicChallenges(JSON data)
    {
	    if (data == null)
	    {
		    return;
	    }

	    JSON[] challengeGroups = data.getJsonArray("challenge_groups");
	    for (int i = 0; i < challengeGroups.Length; i++)
	    {
		    addPeriodicChallenge(challengeGroups[i]);
	    }
    }

    protected virtual bool addPeriodicChallenge(JSON data)
    {
	    if (data == null)
	    {
		    Debug.LogError("Invalid challenge json");
		    return false;
	    }
	    
	    int id = data.getInt("id", -1);
	    if (missionIds.Contains(id) || id == -1)
	    {
		    return false;
	    }
	    
	    missionIds.Add(id);
	    
	    int unlockTime = data.getInt("start_time", System.Int32.MaxValue);
	    periodicStartTime = unlockTime;
	    periodChallengesEnd = new GameTimerRange(periodicStartTime, periodicStartTime+periodicChallengeDuration); //Restart the time to the new periodic challenge

	    addChallenge(data, periodicMissions, periodicObjectives);
	    return true;
    }

    public bool addSeasonalChallenge(JSON data)
    {
	    int id = data.getInt("id", -1);
	    if (missionIds.Contains(id) || id == -1)
	    {
		    return false;
	    }
	    
	    missionIds.Add(id);
	    addChallenge(data, seasonMissions, seasonObjectives);
	    return true;
    }

    private void addChallenge(JSON data, SortedDictionary<int, List<Mission>> missionDictionary, HashSet<Objective> objectiveHash)
    {
	    if (data == null)
	    {
		    Debug.LogError("Invalid challenge json");
		    return;
	    }
		    
	    Mission mission = createMission(data);
	    List<Mission> missionList = null;
	    int unlockTime = data.getInt("start_time", 0);
	    if (!missionDictionary.TryGetValue(unlockTime, out missionList))
	    {
		    missionList = new List<Mission>();
		    missionDictionary.Add(unlockTime, missionList);
	    }

	    if (mission != null)
	    {
		    foreach (Objective objective in mission.objectives)
		    {
			    if (objective == null)
			    {
				    Debug.LogError("null objective");
				    continue;
			    }
			    if (!string.IsNullOrEmpty(objective.game))
			    {
				    registerGame(objective.game);
			    }
			    
			    objectiveHash.Add(objective);
		    }
		    
		    missionList.Add(mission);
		    missions.Add(mission);
	    }
    }

    protected virtual void registerGame(string gameKey)
    {
    }

    private void populateSeasonalChallenges(JSON data)
    {
	    if (data == null)
	    {
		    return;
	    }

	    JSON[] challengeGroups = data.getJsonArray("challenge_groups");
	    
	    for (int i = 0; i < challengeGroups.Length; i++)
	    {
		    addSeasonalChallenge(challengeGroups[i]);
	    }
    }

    private void populateSeasonalUnlockDates(JSON data)
    {
	    if (data == null)
	    {
		    return;
	    }

	    List<string> unlockDateKeys = data.getKeyList();
	    for (int i = 0; i < unlockDateKeys.Count; i++)
	    {
		    int dateTimestamp = 0;
		    if (!int.TryParse(unlockDateKeys[i], out dateTimestamp))
		    {
			    Debug.LogWarning("Invalid unlock time");
			    continue;
		    }
		    lockedSeasonMissions.Add(dateTimestamp, data.getInt(unlockDateKeys[i], 0));
	    }
    }

    public override void drawInDevGUI()
    {
	    GUILayout.BeginVertical();
	    GUILayout.Label(string.Format("Campaign ID: {0}", campaignID));
	    GUILayout.Label(string.Format("isEnabled: {0}", isEnabled));
	    GUILayout.Label(string.Format("Error string: {0}", campaignErrorString));
	    GUILayout.Label(string.Format("isForceDisabled: {0}", isForceDisabled));
	    GUILayout.Label(string.Format("Total Missions: {0}", missions.Count));
	    GUILayout.Label(string.Format("Num Periodic Challenge Groups: {0}", periodicMissions.Count));
	    GUILayout.Label(string.Format("Num Seasonal Challenge Groups: {0}", seasonMissions.Count));
	    GUILayout.Label(string.Format("State: {0}", state));
	    GUILayout.Label(string.Format("Range Active: {0}", timerRange.isActive));
	    GUILayout.Label(string.Format("Range Left: {0}", timerRange.timeRemainingFormatted));
	    GUILayout.EndVertical();
    }

    public void onSeasonalRewardsComplete(int[] awardIndecies)
    {
	    
    }

    public void onPerodicRewardsComplete(int[] awardIndecies)
    {
	    
    }

    public List<Mission> getActiveMissionsForGame(string gameKey)
    {
	    List<Mission> activeMissions = new List<Mission>();

	    foreach (int time in seasonMissions.Keys)
	    {

		    if (time > GameTimer.currentTime)
		    {
			    break;
		    }

		    List<Mission> allMissions = seasonMissions[time];
		    for (int i = 0; i < allMissions.Count; i++)
		    {
			    if (allMissions[i].containsGame(gameKey) || allMissions[i].hasObjectivesWithoutAGame)
			    {
				    activeMissions.Add(allMissions[i]);
			    }
		    }
	    }
	    
	    foreach (int time in periodicMissions.Keys)
	    {

		    if (time > GameTimer.currentTime)
		    {
			    break;
		    }

		    List<Mission> allMissions = periodicMissions[time];
		    for (int i = 0; i < allMissions.Count; i++)
		    {
			    if (allMissions[i].containsGame(gameKey) || allMissions[i].hasObjectivesWithoutAGame)
			    {
				    activeMissions.Add(allMissions[i]);
			    }
		    }
	    }
	    return activeMissions;
    }

    public override void onProgressReset(JSON response)
    {
	    ChallengeType type = parseTypeString(response.getString("challenge_type", ""));
	    int startTime = response.getInt("challenge_start_time", -1);
	    int groupId = response.getInt("group_id", -1);
	    int id = response.getInt("id", -1);
	    List<Mission> missions = null;
	    switch (type)
	    {
		    case ChallengeType.PERIODIC:
			    if (!periodicMissions.TryGetValue(startTime, out missions))
			    {
					 Debug.LogError("Missions can't be found");
					 return;
			    }
			    break;
		    
		    case ChallengeType.SEASONAL:
			    if (!seasonMissions.TryGetValue(startTime, out missions))
			    {
				    Debug.LogError("Missions can't be found");
				    return;
			    }
			    break;
	    }

	    if (missions == null || missions.Count == 0)
	    {
		    Debug.LogError("Missions entry exists, but has no data");
		    return;
	    }


	    for (int i = 0; i < missions.Count; i++)
	    {
		    SeasonMission seasonMission = missions[i] as SeasonMission;
		    if (seasonMission == null)
		    {
			    continue;
		    }

		    if (seasonMission.id == groupId && seasonMission.objectives != null)
		    {
			    for (int objectiveIndex = 0; objectiveIndex < seasonMission.objectives.Count; objectiveIndex++)
			    {
				    if (seasonMission.objectives[objectiveIndex].id == id)
				    {
					    seasonMission.objectives[objectiveIndex].resetProgress(replayGoalRatio);
					    break;
				    }
			    }
		    }
	    }
    }

    public ChallengeType getChallengeType(Objective objective)
    {
	    if (periodicObjectives.Contains(objective))
	    {
		    return ChallengeType.PERIODIC;
	    }

	    if (seasonObjectives.Contains(objective))
	    {
		    return ChallengeType.SEASONAL;
	    }

	    Debug.LogWarning("Can't find objective type");
	    return ChallengeType.NONE;
    }

    public virtual void fullReset()
    {
	    resetMissions(periodicMissions, true);
	    resetMissions(seasonMissions, true);
    }

    public ChallengeType parseTypeString(string type)
    {
	    switch (type)
	    {
		    case "periodic":
			    return ChallengeType.PERIODIC;
		    
		    default:
			    Debug.LogWarning("Invalid challenge type -- using default season type");
			    goto case "seasonal";
			    
		    case "seasonal":
			    return ChallengeType.SEASONAL;
	    }
    }
    
    
    public override void onProgressUpdate(JSON response)
    {   
	    ChallengeType type = parseTypeString(response.getString("challenge_type", ""));
	    int id = response.getInt("id", -1);
	    int group_id = response.getInt("group_id", -1);
	    int challengeStartTime = response.getInt("challenge_start_time", 0);
	    SortedDictionary<int, List<Mission>> missions = null;
	    switch (type)
	    {
		    case ChallengeType.PERIODIC:
			    missions = periodicMissions;
			    break;
		    
		    case ChallengeType.SEASONAL:
			    missions = seasonMissions;
			    break;
	    }

	    if (missions == null)
	    {
		    Debug.LogError("Invalid challenge type");
		    return;
	    }
	    
	    JSON data = response.getJSON("progress_data");
	    updateMissions(missions, group_id, id, challengeStartTime, data);	    
	    scheduleUIUpdate();
    }

    private static void updateMissions(SortedDictionary<int, List<Mission>> missionDict, int group_id, int id, int startTime, JSON data)
    {
	    if (data == null)
	    {
		    Debug.LogError("No update data");
		    return;
	    }

	    List<Mission> missions = null;
	    if (!missionDict.TryGetValue(startTime, out missions))
	    {
		    Debug.LogWarning("start time not found");
		    return;
	    }

	    for (int i = 0; i < missions.Count; i++)
	    {
		    SeasonMission seasonMission = missions[i] as SeasonMission;
		    if (seasonMission != null && seasonMission.id == group_id)
		    {
			    if (seasonMission.objectives != null)
			    {
				    for (int objIndex = 0; objIndex < seasonMission.objectives.Count; objIndex++)
				    {
					    if (seasonMission.objectives[objIndex] == null)
					    {
						    Debug.LogWarning("Null objective");
						    continue;
					    }
					    if (seasonMission.objectives[objIndex].id == id)
					    {
						    updateObjective(seasonMission.objectives[objIndex], data);
						    break;
					    }
				    }
				    seasonMission.checkCompletedObjectives();
			    }
			    break;
		    }
	    }
    }

    private static void updateObjective(Objective objective, JSON data)
    {
	    objective.init(data);
    }

    private static void resetMissions(SortedDictionary<int, List<Mission>> missionDict, bool destroy = false)
    {
	    if (destroy)
	    {
		    missionDict.Clear();
	    }
	    else
	    {
		    foreach(List<Mission> missions in missionDict.Values)
		    {
			    for (int i = 0; i < missions.Count; i++)
			    {
				    if (missions[i] == null)
				    {
					    continue;
				    }

				    missions[i].resetProgress(1,1);

			    }
		    }   
	    }
    }

    public int[] getSeasonalUnlockDates()
    {
	    return seasonMissions.Keys.ToArrayNoLinq();
    }

    private List<Mission> getCurrentPeriodicMissions()
    {
	    List<Mission> missionList = null;
	    if (!periodicMissions.TryGetValue(periodicStartTime, out missionList))
	    {
		    foreach (KeyValuePair<int, List<Mission>> kvp in periodicMissions)
		    {
			    missionList = kvp.Value;
			    break;
		    }
	    }

	    return missionList;
    }

    public virtual Objective getCurrentPeriodicObjective()
    {
	    Objective objectiveToDisplay = null;
	    List<Mission> missionList = getCurrentPeriodicMissions();
	    
	    //currently we only get one periodic mission from the server, so take the fist one
	    if (missionList != null && missionList.Count > 0)
	    {
		    for (int missionIndex = 0; missionIndex < missionList.Count; missionIndex++)
		    {
			    Mission missionToDisplay = missionList[missionIndex];
			    if (missionToDisplay != null && missionToDisplay.objectives != null && !missionToDisplay.isComplete)
			    {
				    for (int objectiveIndex = 0; objectiveIndex < missionToDisplay.objectives.Count; objectiveIndex++)
				    {
					    if (!missionToDisplay.objectives[objectiveIndex].isComplete)
					    {
						    objectiveToDisplay = missionToDisplay.objectives[objectiveIndex];
						    break;
					    }
				    }
			    }
			    if (objectiveToDisplay != null)
			    {
				    break;
			    }
		    }
	    }

	    return objectiveToDisplay;
    }
    
    public override void init(JSON data)
    {
	    base.init(data);
	    
	    //set enabled flag based on timer range because enabled field does not come down
	    //can't use isActive field of timer range becuase timer range takes a frame to setup
	    int startDate = data.getInt("start_time", 0); 
	    int endDate	  = data.getInt("end_time", 0);
	    isEnabled = startDate <= GameTimer.currentTime && endDate >= GameTimer.currentTime;
	    periodicChallengeDuration = data.getInt("periodic_challenge_duration", 0);
	    
	    periodChallengesEnd = new GameTimerRange(periodicStartTime, periodicStartTime+periodicChallengeDuration);
    }

    protected override Mission createMission(JSON data)
    {
	    return new SeasonMission(data);
    }
    

}
