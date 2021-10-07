using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicBuyPageSurfacingExperiment : EosExperiment 
{
	public int cooldown { get; private set; }
	public DynamicBuyPageSurfacingExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		cooldown = getEosVarWithDefault(data, "cooldown", 0);
	}

	public override void reset()
	{
		base.reset();
		cooldown = 0;
	}
}
