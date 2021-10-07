using UnityEngine;
using System.Collections;

/*
A way to be able to change what direction the anchor is facing for the purposes of small devices.
This is very rare to get used; in fact, it likely will only really have a use for the spin panel, where we want a button to appear on the left of one button,
as opposed to above it for ipad.
 */
[RequireComponent(typeof(UIAnchor))]
public class SmallDeviceAnchorSideChanger : MonoBehaviour
{
	public UIAnchor.Side newSide;

	/// If we are on a small device, change the side of the anchor, and we're done.
	void Awake()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			this.gameObject.GetComponent<UIAnchor>().side = newSide;
		}
		Destroy (this);
	}
}

