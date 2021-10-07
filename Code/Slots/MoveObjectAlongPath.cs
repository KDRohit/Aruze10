using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Script that is attached to a gameobject that you want to move along a path
public class MoveObjectAlongPath : MonoBehaviour
{
	private Vector3[] path;
	private bool shouldReverse;
	private bool doAngle;
	private bool isAnimating = false;

	public void startObjectAlongPath(Vector3[] objectPath, float time, bool needsToReverse, bool needsAngleAdjustment = true)
	{
		path = objectPath;
		shouldReverse = needsToReverse;
		isAnimating = true;
		iTween.ValueTo(gameObject, iTween.Hash("onupdate", "moveObjectOnPath", "from", 0.0f, "to", 1.0f, "time", time, "oncompletetarget", this.gameObject, "oncomplete", "moveObjectComplete"));
		doAngle = needsAngleAdjustment;
	}

	// Using iTween to handle the movement of the hand so we can change the tweenType if we want.
	public void moveObjectOnPath(float percent)
	{
		// Put the hand on the right part of the path based off the percentage.
		iTween.PutOnPath(gameObject, path, percent);

		if (doAngle)
		{
			Vector3 pointOnPath = iTween.PointOnPath(path, percent); // What we want to look at.
			Vector3 nextPointOnPath = iTween.PointOnPath(path, percent + 0.00001f); // What we want to look at.
			nextPointOnPath.z = gameObject.transform.position.z;

			float angle = Vector3.Angle(Vector3.right, nextPointOnPath - pointOnPath);
			if (Vector3.Dot(Vector3.up, nextPointOnPath - pointOnPath) < 0)
			{
				angle *= -1;
			}
			if (shouldReverse)
			{
				angle += 180.0f;
			}

			Vector3 handAngle = gameObject.transform.localEulerAngles;
			handAngle.z = angle;
			gameObject.transform.localEulerAngles = handAngle;
		}
	}

	/// Hook to iTween completion so that this object can be waited on in a coroutine
	public void moveObjectComplete()
	{
		isAnimating = false;
	}
}
