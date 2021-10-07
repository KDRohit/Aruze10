using Com.Rewardables;
using System.Collections.Generic;

public class RewardableRewardBundle : Rewardable
{
	public List<Rewardable> rewardables = new List<Rewardable>();
	public const string TYPE = "rewards_bundle";
	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		JSON[] rewardablesJson = data.getJsonArray("rewardables");
		for (int i = 0; i < rewardablesJson.Length; i++)
		{
			string type = rewardablesJson[i].getString("reward_type", "");
			Rewardable rewardable = RewardablesManager.createRewardFromType(type, rewardablesJson[i]);
			rewardables.Add(rewardable);
		}
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}
