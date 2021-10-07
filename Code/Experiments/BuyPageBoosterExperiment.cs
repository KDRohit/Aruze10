using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyPageBoosterExperiment : EosExperiment
{
	// Start Time of the experiment
	public int startTimeInSecs { get; private set; }

	// End Time of the experiment
	public int endTimeInSecs { get; private set; }

	public BuyPageBoosterExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		string startStr = getEosVarWithDefault(data, "buy_page_event_start_time", "");
		string endStr = getEosVarWithDefault(data, "buy_page_event_end_time", "");
		if (!string.IsNullOrEmpty(startStr) && !string.IsNullOrEmpty(endStr))
		{
			startTimeInSecs = GameTimer.convertDateTimeStringToSecondsFromNow(startStr) + GameTimer.currentTime;
			endTimeInSecs = GameTimer.convertDateTimeStringToSecondsFromNow(endStr) + GameTimer.currentTime;
		}
		else
		{
			if (startStr == null)
			{
				Debug.LogWarning("BuyPageBoosterExperiment: null eos variable buy_page_event_start_time");
			}
			if (endStr == null)
			{
				Debug.LogWarning("BuyPageBoosterExperiment: null eos variable buy_page_event_end_time");
			}
		}
	}

	public override void reset()
	{
		startTimeInSecs = 0;
		endTimeInSecs = 0;
	}
}
