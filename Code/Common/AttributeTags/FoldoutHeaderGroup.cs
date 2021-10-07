using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * Custom attribute tag that lets you define a named foldout section to group serialized fields
 * in the inspector into.  For instance breaking them up by department to make it easier for people
 * to find the variables they care about.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 8/12/2019
 */
namespace Zynga.Unity.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class FoldoutHeaderGroup : Attribute
	{
		public const string STANDARD_OPTIONS_GROUP = "Standard Options";
		public const string AUDIO_OPTIONS_GROUP = "Audio Options";
		public const string ADDITIONAL_OPTIONS_GROUP = "Additional Options";

		public readonly string groupName;

		public FoldoutHeaderGroup(string passedGroupName)
		{
			groupName = passedGroupName;
		}
	}
}
