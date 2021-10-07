using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsPostPurchaseChallenge
{
	public static void logDialogView(string klass)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "post_purchase",
			phylum: "dialog",
			klass: klass,
			genus: "view"
		);	
	}
	
	public static void logDialogClick(string family)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "post_purchase",
			phylum: "dialog",
			family: family,
			genus: "click"
		);	
	}

	public static void logMilestone(string milestone, int val)
	{
		StatsManager.Instance.LogMileStone(milestone, val);
	}
	
}
