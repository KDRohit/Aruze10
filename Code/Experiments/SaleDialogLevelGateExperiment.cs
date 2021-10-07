using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaleDialogLevelGateExperiment : EosExperiment
{
	public bool playerMeetsLevelRequirement 
	{ 
		get
		{
			return SlotsPlayer.instance != null &&
				SlotsPlayer.instance.socialMember != null &&
				SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
		}
	}
	private int unlockLevel = 0;

	public SaleDialogLevelGateExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		unlockLevel = getEosVarWithDefault(data, "level_unlock", 0);
	}

	public override void reset()
	{
		base.reset();
		unlockLevel = 0;
	}
}
