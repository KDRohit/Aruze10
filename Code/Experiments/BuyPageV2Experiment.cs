using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyPageV2Experiment : EosExperiment
{

	public bool shouldShowNewArt { get; private set; }
	public bool shouldShowNewNumbers { get; private set; }
	public BuyPageV2Experiment(string name) : base(name)
	{
	}
	protected override void init(JSON data)
	{
		shouldShowNewArt = getEosVarWithDefault(data, "new_art", false);
		shouldShowNewNumbers = getEosVarWithDefault(data, "new_numbers", false);
	}
	public override void reset()
	{
		shouldShowNewArt = false;
		shouldShowNewNumbers = false;
	}
}
