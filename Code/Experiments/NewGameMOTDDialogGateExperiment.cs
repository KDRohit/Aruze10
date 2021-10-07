using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGameMOTDDialogGateExperiment : EosExperiment
{
	public int unlockLevel { get; private set; }
	public NewGameMOTDDialogGateExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		unlockLevel = getEosVarWithDefault(data, "level_unlock", 0);
	}

	public override void reset()
	{
		unlockLevel = 0;
	}
}
