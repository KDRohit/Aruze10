using UnityEngine;
using System.Collections;
using Com.Rewardables;

public class RewardPrizePopPicks : Rewardable
{
	public long extraPicks { get; protected set; }
	public const string TYPE = "prize_pop_extra_picks";
	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		extraPicks = data.getInt("value", 0);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}