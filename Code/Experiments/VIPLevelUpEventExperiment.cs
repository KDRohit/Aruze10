using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VIPLevelUpEventExperiment : EosExperiment
{
	public int startTime { get; private set; }
	public int endTime { get; private set; }
	public int boostAmount { get; set; }
	public string featureList { get; private set; }
	public VIPLevelUpEventExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		startTime = getEosVarWithDefault(data, "start_time", -1);
		endTime = getEosVarWithDefault(data, "end_time", -1);
		featureList = getEosVarWithDefault(data, "features_affected", "");
		boostAmount = getEosVarWithDefault(data, "vip_level_boost", 0);
	}

	public override void reset()
	{
		base.reset();
		startTime = 0;
		endTime = 0;
		boostAmount = -1;
		featureList = null;
	}
}
