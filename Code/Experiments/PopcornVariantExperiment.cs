using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopcornVariantExperiment : EosExperiment 
{
	public string template { get; private set; }
	public string theme { get; private set; }
	public PopcornVariantExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		template = data.getString("template", "");
		theme = data.getString("theme", "");
	}

	public override void reset()
	{
		base.reset();
		template = "";
		theme = "";
	}
}
