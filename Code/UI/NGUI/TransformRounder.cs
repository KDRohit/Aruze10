using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach this to objects that are in the NGUI hierarchy but are not a NGUI widget.
This allows us to keep parent objects of NGUI widgets nice and neat in their positions and scale,
since Unity has a bad habit of making them off by a small fractional amount when duplicating.
Also helps when positioning these objects, to make sure they stay at whole number positions.
Widgets have the "Make Pixel Perfect" button to fix rounding issues.
This is disabled at runtime, since it's only useful during design time.
*/

[ExecuteInEditMode]
public class TransformRounder : TICoroutineMonoBehaviour
{
	public int scaleRoundingPrecision = 2;
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			enabled = false;
		}
	}
	
	void Update()
	{
		// We don't need to do any fancy optimizations here,
		// since Unity only calls Update on an object in the editor
		// when a value changes on the object, so Unity is alreay optimizing the editor.
		
		Vector3 pos = transform.localPosition;
		pos.x = Mathf.Round(pos.x);
		pos.y = Mathf.Round(pos.y);
		pos.z = Mathf.Round(pos.z);
		transform.localPosition = pos;
		
		Vector3 rot = transform.localEulerAngles;
		rot.x = Mathf.Round(rot.x);
		rot.y = Mathf.Round(rot.y);
		rot.z = Mathf.Round(rot.z);
		transform.localEulerAngles = rot;

		// Since we may actually want to use fractional scale in the editor,
		// we round scale to the nearest 100th.
		Vector3 scale = transform.localScale;
		scale.x = CommonMath.round(scale.x, scaleRoundingPrecision);
		scale.y = CommonMath.round(scale.y, scaleRoundingPrecision);
		scale.z = CommonMath.round(scale.z, scaleRoundingPrecision);
		transform.localScale = scale;
	}
}
