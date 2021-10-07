using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyV3Experiment : EosExperiment
{

	public bool isFtueEnabled 
	{
		get; private set;
	}

	public LobbyV3Experiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		isFtueEnabled = getEosVarWithDefault(data, "ftue_enabled", false);
	}

	public override void reset()
	{
		base.reset();
		isFtueEnabled = false;
	}

	public void forceEnabled(bool enable)
	{
		isEnabled = enable;
	}
}