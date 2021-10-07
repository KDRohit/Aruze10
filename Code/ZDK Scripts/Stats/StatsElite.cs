using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StatsElite
{
	public static void logDialogView(string klass, string milestone, long val)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: "",
			klass: klass,
			genus: "view",
			milestone: milestone,
			val: val
			
		);	
	}

	public static void logOpenTab(string tabName)
	{
		string milestone = "";
		int val = 0;
		
		if (EliteManager.hasActivePass)
		{
			milestone = "active";
			val = EliteManager.passes;
		}
		else
		{
			milestone = "deactivated";	
		}

		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: tabName,
			family: EliteManager.points.ToString(),
			genus: "click",
			milestone: milestone,
			val: val
		);	
	}
	
	public static void logRewardListItemClicked(string klass)
	{
		string milestone = EliteManager.hasActivePass ? "active" : "deactivated";
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: "elite_rewards",
			klass: klass,
			family: EliteManager.points.ToString(),
			genus: "click",
			milestone: milestone,
			val: EliteManager.passes
		);	
	}
	
	public static void logPointsListItemClicked(string klass)
	{
		string milestone = EliteManager.hasActivePass ? "active" : "deactivated";
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: "elite_points",
			klass: klass,
			family: EliteManager.points.ToString(),
			genus: "click",
			milestone: milestone,
			val: EliteManager.passes
		);	
	}

	public static void logEliteButtonClicked()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "bottom_nav",
			kingdom: "elite",
			phylum: "dialog",
			family: EliteManager.points.ToString(),
			genus: "click"
		);	
	}

	public static void logAccessExpired()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: "times_up",
			genus: "click"
		);	
	}
	
	public static void logAccessRejoin()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: "rejoin",
			genus: "click"
		);	
	}
	
	public static void logViewVideo(string source)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "elite",
			phylum: source,
			klass: "watch_video",
			genus: "click"
		);	
	}

	public static void logMilestone(string milestone, int val)
	{
		StatsManager.Instance.LogMileStone(milestone, val);
	}

}
