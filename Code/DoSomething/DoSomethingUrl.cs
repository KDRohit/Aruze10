using UnityEngine;
using System.Collections;

public class DoSomethingUrl : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (!string.IsNullOrEmpty(parameter))
		{
			Common.openUrlWebGLCompatible(parameter);
		}
	}
}
