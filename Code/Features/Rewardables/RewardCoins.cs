using UnityEngine;
using System.Collections;
using Com.Rewardables;

public class RewardCoins : Rewardable
{
	public long amount { get; protected set; }
	public const string TYPE = "coin";
	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		amount = data.getLong("value", 0);
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}