using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FtueExperiment : EosExperiment 
{
	public enum FtueMode
	{
		NONE,
		AUTO_SELECT,
		ON_LOAD_SELECT,
		PLAY_THEN_SELECT
	}

	public FtueMode currentMode { get; private set; }

	public FtueExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		switch (getEosVarWithDefault(data, "experience", ""))
		{
			// Set the FTUE mode based on EOS.
			case "direct":
				currentMode = FtueMode.AUTO_SELECT;
				break;
			case "play_then_select":
				currentMode = FtueMode.PLAY_THEN_SELECT;
				break;
			case "select":
				currentMode = FtueMode.ON_LOAD_SELECT;
				break;
			default:
				currentMode = FtueMode.NONE;
				break;
		}
	}

	public override bool isInExperiment
	{
		get
		{
			// If we can find the credit package and the timer is active, then this is a valid starter pack sale.
			return currentMode != FtueMode.NONE;
		}
	}

	public override void reset()
	{
		currentMode = FtueMode.NONE;
	}
}
