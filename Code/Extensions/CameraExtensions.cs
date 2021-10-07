using UnityEngine;

public static class CameraExtensions
{
	/// Returns Bounds that represents where the screen bounds of a camera are in world space.
	public static Bounds ScreenToWorldBounds(this Camera camera, float distanceFromCamera = 0.0f)
	{
		Vector3 bottomLeft = new Vector3(camera.rect.xMin * Screen.width, camera.rect.yMin * Screen.height, 0f);
		Vector3 topRight   = new Vector3(camera.rect.xMax * Screen.width, camera.rect.yMax * Screen.height, 0f);
		bottomLeft = camera.ScreenToWorldPoint(bottomLeft);
		topRight = camera.ScreenToWorldPoint(topRight);
		Vector3 boundSize = topRight - bottomLeft;
		return new Bounds(Vector3.zero, boundSize);
	}
}
