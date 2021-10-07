using UnityEngine;
using System.Collections;

public class StatsSlotventures
{
	public static void logEUEClick(string genus = "click_home")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "eue_sv",
			phylum: "sv_lobby_dialog",
			klass: "",
			genus: genus
		);
	}
}