using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LOZChallengesExperiment : EosExperiment
{
	public int levelLock { get; private set; }

	public LOZChallengesExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		levelLock = getEosVarWithDefault(data, "level_lock", 0);
	}

	public override void reset()
	{
		base.reset();
		levelLock = 0;
	}

}
