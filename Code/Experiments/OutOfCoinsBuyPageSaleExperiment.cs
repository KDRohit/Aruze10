using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfCoinsBuyPageExperiment : EosExperiment
{
    public bool showSale = false;
    public bool showIntermediaryDialog = false;
    public OutOfCoinsBuyPageExperiment(string name) : base(name)
    {

    }

    protected override void init(JSON data)
    {
        // grab if we're in a sale
        showSale = getEosVarWithDefault(data, "show_sale", false);
        showIntermediaryDialog = getEosVarWithDefault(data, "dialog", false);
    }

    public override void reset()
    {
        base.reset();
    }
}