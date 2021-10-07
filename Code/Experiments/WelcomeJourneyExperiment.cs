using System.Collections.Generic;

public class WelcomeJourneyExperiment : EosExperiment
{
	public List<int> dailyRewards { get; private set; }
	public bool isLapsed = false;

	public WelcomeJourneyExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		if (dailyRewards == null)
		{
			dailyRewards = new List<int>(7);
		}
		else
		{
			dailyRewards.Clear();
		}

		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_1", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_2", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_3", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_4", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_5", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_day_6", 0));
		dailyRewards.Add(getEosVarWithDefault(data, "reward_treasure", 0));

		isLapsed = getEosVarWithDefault(data, "is_relapsed", false);
	}

	public override void reset()
	{
		base.reset();
		if (dailyRewards != null)
		{
			dailyRewards.Clear();
		}
	}
}
