using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperStreakExperiment : EosExperiment
{
	public float multiplier { get; private set; }

	public SuperStreakExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		multiplier = getEosVarWithDefault(data, "super_streak_multiplier", 1.0f);
	}

	public override void reset()
	{
		base.reset();
		multiplier = 1;
	}

}
