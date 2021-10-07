using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LotteryDayTuningExperiment : EosExperiment 
{
	public int levelLock { get; private set; }
	public string keyName { get; private set; }
	public string scaleFactor { get; private set; }

	public LotteryDayTuningExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		levelLock = getEosVarWithDefault(data, "level_lock", 0);
		keyName = getEosVarWithDefault(data, "key_name", "");
		scaleFactor = getEosVarWithDefault(data, "scaling_factor", "");
	}

	public override void reset()
	{
		base.reset();
		scaleFactor = "";
		keyName = "";
		levelLock = 0;
	}
}
