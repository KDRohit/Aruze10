using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrchestratorFeatureExperiment : EosExperiment
{
    public string version { get; private set; }
    
    public OrchestratorFeatureExperiment(string name) : base(name)
    {
    }

    protected override void init(JSON data)
    {
        version = data.getString("version", "");
    }

    public override void reset()
    {
        base.reset();
        version = "";
    }
}
