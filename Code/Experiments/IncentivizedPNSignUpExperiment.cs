using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncentivizedPNSignUpExperiment : EosExperiment
{
	public int value { get; private set; }

	public IncentivizedPNSignUpExperiment(string name) : base (name)
	{

	}

	protected override void init(JSON data)
	{
		value = getEosVarWithDefault(data, "pn_incentive_amount", 0);
	}

	public override void reset()
	{
		base.reset();
		value = 0;
	}
}
