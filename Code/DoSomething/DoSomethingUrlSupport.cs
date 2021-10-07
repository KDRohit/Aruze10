using UnityEngine;
using System.Collections;

public class DoSomethingUrlSupport : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		Application.OpenURL(Glb.HELP_LINK_SUPPORT);
	}
}
