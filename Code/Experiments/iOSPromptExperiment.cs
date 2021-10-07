using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class iOSPromptExperiment : EosExperiment
{
	public bool pollInClient { get; private set; }

	public iOSPromptExperiment(string name) : base(name)
	{
	}
	
	protected override void init(JSON data)
	{
		pollInClient = getEosVarWithDefault(data, "poll_in_client", false);
	}
}
