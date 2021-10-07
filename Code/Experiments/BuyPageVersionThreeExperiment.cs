using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyPageVersionThreeExperiment : EosExperiment
{
	public string bannerImage { get; private set; }
	public BuyPageVersionThreeExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		bannerImage = getEosVarWithDefault(data, "banner_image", "default");
	}

	public override void reset()
	{
		bannerImage = "default";
	}
}
