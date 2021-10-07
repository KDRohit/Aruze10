using UnityEngine;
using System.Collections;
/**
Use this class to adjust an anchor.  This should pretty much just be used in cases where most of a dialog is positioned
differently and the location of a button needs to be changed to match a comp.
 */
[RequireComponent(typeof(UIAnchor))]
public class SmallDeviceAnchorAdjuster : MonoBehaviour
{
	public float newRelativeX;
	public float newRelativeY;
	public int newPixelOffsetX;
	public int newPixelOffsetY;

	/// Perform the adjustment when initializing the prefab of attachment
	void Awake ()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			UIAnchor anchorToAdjust = gameObject.GetComponent<UIAnchor>();
			if(anchorToAdjust != null)
			{
				Vector2 newRelativeOffset = anchorToAdjust.relativeOffset;
				Vector2 newPixelOffset = anchorToAdjust.pixelOffset;
				newRelativeOffset.x = newRelativeX;
				newRelativeOffset.y = newRelativeY;
				newPixelOffset.x = newPixelOffsetX;
				newPixelOffset.y = newPixelOffsetY;
				anchorToAdjust.relativeOffset = newRelativeOffset;
				anchorToAdjust.pixelOffset = newPixelOffset;
			}
			else
			{
				Debug.LogWarning("SmallDeviceAnchorAdjuster.cs attempting to UIAnchor that doesnt exist");
			}

		}
		Destroy (this);
	}
}

