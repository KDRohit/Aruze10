using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyChallengeQuest2Experiment : EosExperiment
{
	public string bigWinMotdText { get; private set; } //for awardType == "big_win"
	public string bigWinMotdFooterText { get; private set; }
	public DailyChallengeQuest2Experiment(string name) : base (name)
	{

	}

	protected override void init(JSON data)
	{
		bigWinMotdText = getEosVarWithDefault(data, "biggest_win_text", "");
		bigWinMotdFooterText = getEosVarWithDefault(data, "event_motd_footer", "");
	}

	public override void reset()
	{
		bigWinMotdFooterText = "";
		bigWinMotdText = "";
	}
}
