using UnityEngine;
using System.Collections;

public class WheelPointer
{
	public UISprite clip;
	public int pointerMask;
	public int multiplier;
	public LabelWrapper multiplierTextField;
	
	public bool active;
	public int sliceOffset;
	
	public WheelPointer(UISprite clip, int pointerMask, int sliceOffset, int multiplier = 0, LabelWrapper multiplierTextField = null, bool active = false)
	{
		this.clip = clip;
		this.pointerMask = pointerMask;
		this.multiplier = multiplier;
		this.multiplierTextField = multiplierTextField;
		this.active = active;
		this.sliceOffset = sliceOffset;
	}
}

