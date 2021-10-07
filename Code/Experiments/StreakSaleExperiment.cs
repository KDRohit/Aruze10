using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreakSaleExperiment : EosExperiment
{
	public string configKey { get; private set; }
	public bool enabled { get; private set; }
	public int startTime { get; private set; }
	public int endTime { get; private set; }
	public string bottomText { get; private set; }
	public string bgImagePath { get; private set; }

	public StreakSaleExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		configKey = getEosVarWithDefault(data, "config_key", "");
		enabled = getEosVarWithDefault(data, "enabled", false);
		startTime = getEosVarWithDefault(data, "start_time", 0);
		endTime = getEosVarWithDefault(data, "end_time", 0);
		bottomText = getEosVarWithDefault(data, "bottom_text", "");
		bgImagePath = getEosVarWithDefault(data, "bg_image_path", "");
	}

	public override void reset()
	{
		base.reset();
		configKey = "config_key";
	}
}
