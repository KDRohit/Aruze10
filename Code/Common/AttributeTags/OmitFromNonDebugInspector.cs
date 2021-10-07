using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * Custom attribute tag intended to allow for serialized fields to be omitted from being
 * displayed except if the Inspector window is turned to Debug.  This is a bit better in
 * some cases than using HideInInspector because with that you have to remove and recompile
 * the project in order to view what is hidden using that attribute.
 *
 * Original Creator: Scott Lepthien
 * Creation Date: 8/12/2019
 */
namespace Zynga.Unity.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class OmitFromNonDebugInspector : Attribute
	{
		// No code needed for now, will just look for this tag and omit variables tagged with it
	}
}
