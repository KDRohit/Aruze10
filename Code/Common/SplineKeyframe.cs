using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class is used by the Spline class to store keyframe information for a curve.
*/
public class SplineKeyframe
{
	public int frame;		///< The frame number
	public float tension;	///< Tension controls how much slack this keyframe has
	public float bias;		///< Bias controls how much pull this keyframe has
	public Vector3 coord;	///< The value at this keyframe
	
	public SplineKeyframe prev;
	public SplineKeyframe next;
	
	/// Constructs SplineKeyframe
	public SplineKeyframe(int frame, float tension, float bias, Vector3 coord)
	{
		this.frame = frame;
		this.tension = tension;
		this.bias = bias;
		this.coord = coord;
	}
}
