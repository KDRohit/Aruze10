using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VIPPhoneDialogSurfacingExperiment : EosExperiment
{

	public int coinRewardAmount { get; private set; }
	public VIPPhoneDialogSurfacingExperiment(string name) : base(name)
	{
	}
	
	protected override void init(JSON data)
	{
		coinRewardAmount = getEosVarWithDefault(data, "coin_reward_amount", 0);
	}

	public override void reset()
	{
		base.reset();
		coinRewardAmount = 0;
	}
}
