using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazyLoadBundlesExperiment : EosExperiment
{
	public List<string> bundleList { get; private set;}
	public LazyLoadBundlesExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		string[] bundles = getEosVarWithDefault(data, "bundle_list", "").Split (',');
		if (bundleList == null)
		{
			bundleList = new List<string>();
		}
		bundleList.AddRange (bundles);
	}
	public bool hasBundle (string bundleName)
	{
		return bundleList != null && bundleList.Contains (bundleName);
	}
	public override void reset()
	{
		base.reset();
		bundleList = new List<string>();
	}
}
