using UnityEngine;
using System.Collections;

public class ElitePassExperiment : EosVideoExperiment
{
	public bool showSpinsInToaster = true;
	public ElitePassExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		base.init(data);
		showSpinsInToaster = getEosVarWithDefault(data, "show_elite_spins_in_toaster", true);
	}
}