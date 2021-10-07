using UnityEngine;
using System.Collections;

public class DoSomethingUrlTerms : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		Application.OpenURL(Glb.HELP_LINK_TERMS);
	}
}
