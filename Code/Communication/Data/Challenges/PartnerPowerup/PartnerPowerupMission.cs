using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartnerPowerupMission : Mission, IResetGame
{
	public PartnerPowerupMission(JSON data) : base (data)
	{
		init(data);
	}

	public override void init(JSON data)
	{
		// only 1 objective, and no relevant data so we'll set it up the hard way
		objectives = new List<Objective>();
		Objective spinObjective = new Objective(CampaignDirector.partner);
		objectives.Add(spinObjective);

		// Only 1 reward, and it's COOOOIIINNNSS
		rewards = new List<MissionReward>();
		MissionReward reward = new MissionReward(ChallengeReward.RewardType.CREDITS, CampaignDirector.partner.reward);
		rewards.Add(reward);

		// Populate with a pair of <state (as string), dialog that we'd used with that state>
		dialogStateData = new Dictionary<string, DialogData>();
		// Grab data for dialogs for:
		// Complete
		// Incomplete
		// in progress?
		// etc.

		// partner powerup doesn't have game specific objectives
		//gameObjectives = new Dictionary<string, List<Objective>>();
	}

	public static void resetStaticClassData(){}
}