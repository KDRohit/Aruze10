using UnityEngine;
using System.Collections;

public class DoSomethingNothing : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		// Literally do nothing. Mainly for carousel slides that just appear but don't react to touching.
		// We need a valid action, otherwise the slide won't show up, so "" is now a valid action.
	}
}
