using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EueActiveDiscoveryExperiment : EosExperiment
{
    public bool activeDiscoveryEnabled { get; private set; }
    public int activeDiscoveryLevel { get; private set; }
    
    public EueActiveDiscoveryExperiment(string name) : base(name)
    {
    }

    protected override void init(JSON data)
    {
        activeDiscoveryEnabled = data.getBool("active_discovery", false);
        activeDiscoveryLevel = data.getInt("discovery_level", 1);
    }

    public override void reset()
    {
        base.reset();
        activeDiscoveryEnabled = false;
        activeDiscoveryLevel = 1;
    }
}
