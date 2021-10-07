using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadGameFTUEExperiment : EosExperiment
{
	public string gameKey { get; private set; }

	public LoadGameFTUEExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		gameKey = data.getString("gameKey", "wonka01");
	}

	public override void reset()
	{
		base.reset();
		gameKey = "wonka01";
	}
}
