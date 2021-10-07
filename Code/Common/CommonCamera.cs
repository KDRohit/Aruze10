using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommonCamera
{
	public static bool isPointInCamera(Vector3 point, Camera camera)
	{
		Vector3 screenPoint = camera.WorldToViewportPoint(point);
 		return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
	}
}
