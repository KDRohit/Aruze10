using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HyperEconomyExperiment : EosExperiment
{
	public bool isIntroEnabled { get; private set; }
	public bool isShowingRepricedVisuals { get; private set; }

	public HyperEconomyExperiment(string name) : base (name)
	{

	}

	protected override void init(JSON data)
	{
		isIntroEnabled = isEnabled && getEosVarWithDefault(data, "show_intro", false);
		isShowingRepricedVisuals = getEosVarWithDefault(data, "buy_page_visual_calculation_reprice_2018", false);
	}

	public override void reset()
	{
		isIntroEnabled = false;
		isShowingRepricedVisuals = false;
	}
}
