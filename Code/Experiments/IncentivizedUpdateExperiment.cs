using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncentivizedUpdateExperiment : EosExperiment 
{
	public string minClient { get; private set; }
	public string iLink { get; private set; }
	public int coins { get; private set; }

	public IncentivizedUpdateExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		isEnabled = getEosVarWithDefault(data, "enabled", false);
		iLink = getEosVarWithDefault(data, "ilink", "");
		minClient = getEosVarWithDefault(data, "min_client", "");
		coins = getEosVarWithDefault(data, "coins", 0);
	}

	public override bool isInExperiment
	{
		get
		{
			return base.isInExperiment && !string.IsNullOrEmpty(iLink) && !string.IsNullOrEmpty(minClient);
		}
	}

	public override void reset()
	{
		base.reset();
		minClient = "";
		iLink = "";
		coins = 0;
	}
}
