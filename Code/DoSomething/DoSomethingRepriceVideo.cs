using UnityEngine;
using System.Collections;

public class DoSomethingRepriceVideo : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		VideoDialog.queueRepriceVideo(false);
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return ExperimentWrapper.RepriceVideo.isInExperiment;
	}
}
