using Com.HitItRich.EUE;
using System.Collections.Generic;

public class DoSomethingDynamicMOTDV2 : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		List<string> readyTemplates = DynamicMOTDFeature.instance.getReadyDialogs();
		if (readyTemplates.Count > 0)
		{
			if (readyTemplates.Contains(parameter))
			{
				DynamicMOTD.showDialog("", parameter);
			}
			else if (string.IsNullOrEmpty(parameter)) //TEMP for old carousel setup to still work until server code is released. The tool won't allow this parameter to be empty.
			{
				DynamicMOTD.showDialog("", readyTemplates[0]);
			}
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("DoSomethingDynamicMOTDV2 - Tried to show dynamic MOTD without a valid template!");
		}
	}


	public override bool getIsValidToSurface(string parameter)
	{
		List<string> readyTemplates = DynamicMOTDFeature.instance.getReadyDialogs();
		return readyTemplates.Count > 0 && (readyTemplates.Contains(parameter) || string.IsNullOrEmpty(parameter));
	}
}
