using UnityEngine;
using System.Collections;

public class DoSomethingDynamicMOTD : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (MOTDDialogDataDynamic.instance != null)
		{
			MOTDDialogDataDynamic.instance.show();
		}
	}


	public override bool getIsValidToSurface(string parameter)
	{
		// Hopefully this is ok, since any experiment checks or whatever happens when populating the data.
		return ExperimentWrapper.SegmentedDynamicMOTD.isInExperiment && MOTDDialogDataDynamic.instance != null;
	}
}
