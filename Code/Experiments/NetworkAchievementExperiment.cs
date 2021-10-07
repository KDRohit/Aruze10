using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkAchievementExperiment : EueActiveDiscoveryExperiment 
{
	public int hirTrophyVersion { get; private set; }
	public int bdcTrophyVersion { get; private set; }
	public int wonkaTrophyVersion { get; private set; }
	public int wozTrophyVersion { get; private set; }
	public int networkTrophyVersion { get; private set; }
	public bool enableTrophyV15 { get; private set; }
	public NetworkAchievementExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		base.init(data);
		int trophyVersion = getEosVarWithDefault(data, "trophy_version", 0);
		int specificTrophyVersion = getEosVarWithDefault(data, "hir_trophy_version", 0);
		hirTrophyVersion = Mathf.Max(trophyVersion, specificTrophyVersion);
		wonkaTrophyVersion = getEosVarWithDefault(data, "wonka_trophy_version", 0);
		wozTrophyVersion = getEosVarWithDefault(data, "woz_trophy_version", 0);
		bdcTrophyVersion = getEosVarWithDefault(data, "bdc_trophy_version", 0);
		networkTrophyVersion = getEosVarWithDefault(data, "network_trophy_version", 0);
		enableTrophyV15 = getEosVarWithDefault(data, "enable_trophy_v1.5", false);
					
	}

	public override void reset()
	{
		base.reset();
		hirTrophyVersion = 0;
		bdcTrophyVersion = 0;
		wonkaTrophyVersion = 0;
		wozTrophyVersion = 0;
		networkTrophyVersion = 0;
		enableTrophyV15 = false;
	}
}
