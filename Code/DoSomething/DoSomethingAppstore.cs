using UnityEngine;
using System.Collections;

public class DoSomethingAppstore : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		Application.OpenURL(Glb.clientAppstoreURL);
	}
}
