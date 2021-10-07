using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoLaExperiment : EosExperiment 
{
    public string version { get; private set; }
    

    public LoLaExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		version = getEosVarWithDefault(data, "lola_version", "NONE");
	}

	public override void reset()
	{
		base.reset();
		version = "NONE";
	}

}
