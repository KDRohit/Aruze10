using UnityEngine;
using System.Collections;
using Com.Rewardables;

public class RewardElitePass : Rewardable
{
	public int duration { get; protected set; }
	public int cost { get; protected set; }
	public int oldPassCount { get; protected set; }
	public int newPassCount { get; protected set; }
	public int expiration { get; protected set; }

	/// <inheritdoc/>
	public override void init(JSON data)
	{
		this.data = data;

		duration = data.getInt("duration", 0);
		cost = data.getInt("cost", 0);
		oldPassCount = data.getInt("old_num_passes", 0);
		newPassCount = data.getInt("new_num_passes", 0);
		expiration = data.getInt("expiration", 0);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return "elite_pass"; }
	}
}