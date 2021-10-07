using UnityEngine;
using System.Collections;

/*
Enforces the location of a sprite for when we are on small devices.  
 */
public class SmallDeviceLocationAdjuster : MonoBehaviour
{
	public enum Direction
	{
		X,
		Y,
		BOTH
	}

	public float newXLocation;
	public float newYLocation;
	public Direction direction = Direction.BOTH;

	/// On first run, fix the location of the background image set to the new size.
	void Awake()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			Vector3 newPosition = this.gameObject.transform.localPosition;
			if (direction == Direction.X || direction == Direction.BOTH)
			{
				newPosition.x = newXLocation;
			}
			if (direction == Direction.Y || direction == Direction.BOTH)
			{
				newPosition.y = newYLocation;
			}
			this.gameObject.transform.localPosition = newPosition;
		}
		Destroy(this);
	}
}

