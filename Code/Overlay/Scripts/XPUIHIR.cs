using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles HIR overrides for XPUI.
*/

public class XPUIHIR : XPUI
{
	protected override float tweenDuration
	{
		get
		{
			return Mathf.Abs(xpMeter.transform.localScale.x - newXPMeterWidth) * 0.02f;
		}
	}
}
