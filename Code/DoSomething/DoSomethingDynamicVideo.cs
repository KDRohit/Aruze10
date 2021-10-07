using UnityEngine;
using System.Collections;

public class DoSomethingDynamicVideo : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		return !Data.liveData.getBool(VideoDialog.LIVE_DATA_DISABLE_KEY, false) && ExperimentWrapper.DynamicVideo.isInExperiment;
	}

	public override void doAction(string parameter)
	{
		VideoDialog.showDialog(
			ExperimentWrapper.DynamicVideo.url, 
			ExperimentWrapper.DynamicVideo.action, 
			ExperimentWrapper.DynamicVideo.buttonText, 
			ExperimentWrapper.DynamicVideo.statName, 
			ExperimentWrapper.DynamicVideo.closeButtonDelay,
			ExperimentWrapper.DynamicVideo.skipButtonDelay,
			"",
			ExperimentWrapper.DynamicVideo.imagePath,
			false
		);
	}
}
