using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCPAStatusAction : ServerAction
{
	private const string GET_EXPERIMENT = "get_ccpa_status";
	private const string ACTION_TYPE = "ccpa_status";

	private CCPAStatusAction(ActionPriority priority, string type) : base(priority, type)
	{

	}

	public static void getExperiment(EventDelegate callback)
	{
		if (callback != null)
		{
			Server.registerEventDelegate(ACTION_TYPE, callback);
		}
		new CCPAStatusAction(ActionPriority.IMMEDIATE, GET_EXPERIMENT);
	}
}
