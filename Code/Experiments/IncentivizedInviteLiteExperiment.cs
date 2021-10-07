using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncentivizedInviteLiteExperiment : EosExperiment
{
	public int baseIncentiveAmount { get; private set; }
	public int rewardSchedule { get; private set; }
	public int maxCollects { get; private set; }
	public int maxEscalations { get; private set; }
	public int installsUntilNextEscalation { get; private set; }
	public IncentivizedInviteLiteExperiment(string name) : base (name)
	{

	}

	protected override void init(JSON data)
	{
		baseIncentiveAmount = getEosVarWithDefault(data, "base_incentive_amount", 0);
		rewardSchedule = getEosVarWithDefault(data, "reward_schedule", 0);
		maxCollects = getEosVarWithDefault(data, "max_collects", 0);
		maxEscalations = getEosVarWithDefault(data, "max_escalations", 0);
		installsUntilNextEscalation = getEosVarWithDefault(data, "installs_until_next_escalation", 0);
	}
}
