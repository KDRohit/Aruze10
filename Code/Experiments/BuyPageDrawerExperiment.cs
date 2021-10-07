using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyPageDrawerExperiment : EosExperiment
{
    public List<int> delaysList { get; private set; } //Possibly make a list we just expand
    public string[] priorityOrderList { get; private set; }
    public int maxItemsToRotate { get; private set; }

    public BuyPageDrawerExperiment(string name) : base(name)
    {
    }

    protected override void init(JSON data)
    {
        string delayString = getEosVarWithDefault (data, "delays", "");
        delaysList = Common.getIntListFromCommaSeperatedString(delayString);
        
        string priorityOrderString = getEosVarWithDefault (data, "priority_order", "");
        priorityOrderList = priorityOrderString.Split(',');
        
        maxItemsToRotate = getEosVarWithDefault(data, "max_item_to_rotate", 0);
    }
}
