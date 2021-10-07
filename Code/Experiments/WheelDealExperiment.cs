using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelDealExperiment : EosExperiment 
{
	public string keyName { get; private set; }
	public WheelDealExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		keyName = getEosVarWithDefault(data, "key_name", "");
	}

	public override void reset()
	{
		base.reset();
		keyName = "";
	}
}
