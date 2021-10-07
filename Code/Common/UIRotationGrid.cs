/* 
   Class: UIRotationGrid
   Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
   Description: An organization script similar to UI grid, but for child object that should all be
   rotated at different degrees rather than arranged around.
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class UIRotationGrid : MonoBehaviour
{
	public enum Axis {X, Y, Z};
	public Axis rotationAxis = Axis.X;
	public float totalAngle = 360.0f;
	public float startingOffset = 0.0f;
	public bool isClockwise = true;
	public bool repositionNow = false;

	public void Start()
	{
		reposition();
	}
	
	public void Update()
	{
		if (repositionNow)
		{
			reposition();
			repositionNow = false;
		}
	}

	private static int nameSortingFunction(Transform a, Transform b) { return a.name.CompareTo(b.name); }
	
	private void reposition()
	{
		List<Transform> kids = new List<Transform>();
		foreach (Transform child in transform)
		{
			kids.Add(child);
		}
		kids.Sort(nameSortingFunction);
		float angleIncrement = totalAngle / kids.Count;
		for (int i = 0; i < kids.Count; i++)
		{
			Transform kid = kids[i];
			Vector3 currentRotation = kid.localEulerAngles;

			float desiredAngle = (startingOffset + (i * angleIncrement));
			desiredAngle *= isClockwise ? -1 : 1;
			switch (rotationAxis)
			{
				case Axis.Y:
					currentRotation.y = desiredAngle;
					break;
				case Axis.Z:
					currentRotation.z = desiredAngle;
					break;
				case Axis.X:
					currentRotation.x = desiredAngle;
					break;
				default:
					// Do nothing.
					break;
				
			}
			kid.localEulerAngles = currentRotation;
		}
	}
}