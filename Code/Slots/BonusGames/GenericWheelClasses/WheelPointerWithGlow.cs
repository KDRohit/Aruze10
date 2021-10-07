using UnityEngine;
using System.Collections;

public class WheelPointerWithGlow : WheelPointer
{
	public GameObject glowObject;

	public WheelPointerWithGlow(UISprite clip, GameObject glowObject, int pointerMask, int sliceOffset, int multiplier = 0, LabelWrapper multiplierTextField = null, bool active = false)
		: base(clip, pointerMask, sliceOffset, multiplier, multiplierTextField, active)
	{
		this.glowObject = glowObject;
	}
}