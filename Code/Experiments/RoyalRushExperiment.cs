using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoyalRushExperiment : EosExperiment 
{
	public bool isAutoSpinEnabled { get; private set; }
	public bool isPausingInCollections { get; private set; }
	public bool isPausingInLevelUps { get; private set; }
	public bool isPausingInQFC { get; private set; }
	public RoyalRushExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		isAutoSpinEnabled = getEosVarWithDefault(data, "auto_spin_enabled", false);
		isPausingInCollections = getEosVarWithDefault(data, "collectible_pause_enabled", false);
		isPausingInLevelUps = getEosVarWithDefault(data, "level_up_pause_enabled", false);
		isPausingInQFC = getEosVarWithDefault(data, "qfc_pause_enabled", false);
	}

	public override void reset()
	{
		base.reset();
		isAutoSpinEnabled = false;
		isPausingInCollections = false;
		isPausingInLevelUps = false;
		isPausingInQFC = false;
	}
}
