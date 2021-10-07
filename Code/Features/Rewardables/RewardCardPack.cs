using UnityEngine;
using System.Collections;
using Com.Rewardables;

public class RewardCardPack : Rewardable
{
	public string packKey { get; private set; }
	public const string TYPE = "collectible_pack";

	/// <inheritdoc/>
	public override void init(JSON data)
	{
		base.init(data);
		packKey = data.getString("pack_key", "");
	}

	/// <inheritdoc/>
	public override string type
	{
		get { return TYPE; }
	}
}