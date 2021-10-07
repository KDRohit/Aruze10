using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FBAuthDialogExperiment : EosExperiment
{
	public int autopopLoginCount { get; private set; }
	public int sessionFrequency { get; private set; }

	public FBAuthDialogExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		autopopLoginCount = getEosVarWithDefault(data, "AUTH_LOBBY_VISIT", 1);
		sessionFrequency = getEosVarWithDefault(data, "AUTH_SESSION_FREQUENCY", 1);
	}

	public override void reset()
	{
		base.reset();
		autopopLoginCount = 1;
		sessionFrequency = 1;
	}
}
