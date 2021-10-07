using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class creates a 3D curve based on provided keyframes.
It can be used with 3D coords, RGB colors, or any other kind
of data that needs gradual interpolation with optional smooth start/finish.

NOTE: Unity has a native curve function similar to this, except it only handles 1 data point at a time instead of 3.


Example usage:

Create a curve with keyframes:
	var zoomSpline:Spline = new Spline();
	zoomSpline.addKeyframe(0,1,0,Vector3(0,0,-15));
	zoomSpline.addKeyframe(10,0,0,Vector3(0,0,-5));
	zoomSpline.addKeyframe(20,1,0,Vector3(0,0,-15));
	zoomSpline.update();	// or updateLinear() to ignore bias and tension values.
						// This precalculates all values along the curve to improve performance later.

Use values along the curve:
	var coord:Vector3 = zoomSpline.getValue(Time.time);
*/
public class Spline
{
	private Dictionary<int, SplineKeyframe> keyframes;	///< A collection that contains all keyframes that will be interpolated along the curve
	
	private Vector3[] calcVal;				///< Contains Vector3 objects
	
	public int firstFrame { get; private set; }		///< Index of the first frame
	public int lastFrame { get; private set; }		///< Index of the last frame
	
	/// Returns an array of the values of all keyframes, only for debugging purposes (hopefully).
	public Vector3[] allKeyframeValues
	{
		get
		{
			if (_allKeyframeValues == null)
			{
				_allKeyframeValues = new Vector3[keyframes.Count];
				int i = 0;
				for (int frame = firstFrame; i <= lastFrame; i++)
				{
					if (keyframes.ContainsKey(frame))
					{
						_allKeyframeValues[i] = keyframes[frame].coord;
						i++;
					}
				}
			}
			
			return _allKeyframeValues;
		}
	}
	private Vector3[] _allKeyframeValues = null;
	
	
	
	public Spline()
	{
		keyframes = new Dictionary<int, SplineKeyframe>();
		firstFrame = int.MaxValue;
		lastFrame = int.MinValue;
	}
		
	/// Adds a SplineKeyframe to the list of keyframes at input frame.
	/// If there exists a key at that frame, it is replaced.
	public void addKeyframe(int frame, float tension, float bias, Vector3 coord)
	{
		// Clear out the cached _allKeyframeValues
		_allKeyframeValues = null;
		
		SplineKeyframe keyframe = new SplineKeyframe(frame, tension, bias, coord);
		
		if (keyframes.ContainsKey(frame))
		{
			// Replace an already existing keyframe
			keyframes[frame] = keyframe;
		}
		else
		{
			// Add a new keyframe
			keyframes.Add(frame, keyframe);
			
			// Update the status of the first/last frames
			if (firstFrame > frame)
			{
				firstFrame = frame;
			}
			if (lastFrame < frame)
			{
				lastFrame = frame;
			}
		}
	}
	
	/// Returns a Vector3 point in space along the curve given a frame within the curve
	public Vector3 getValue(float frame)
	{
		if (keyframes.Count == 0)
		{
			Debug.LogWarning("Spline.getValue() cannot be called on a curve with no keyframes.");
			return Vector3.zero;
		}
		else if (keyframes.Count == 1)
		{
			return calcVal[0];
		}

		float between;
		
		// If the current frame is out of range for the curve's keyframes, use the closest one.
		frame = Mathf.Clamp(frame, firstFrame, lastFrame);
		
		// Allow for fractional frames.
		between = frame - Mathf.Floor(frame);	// get only the fractional part of the value

		Vector3 prev = calcVal[Mathf.FloorToInt(frame)];
		Vector3 next = calcVal[Mathf.CeilToInt(frame)];
		
		float x = prev.x + (next.x - prev.x) * between;
		float y = prev.y + (next.y - prev.y) * between;
		float z = prev.z + (next.z - prev.z) * between;

		return new Vector3(x, y, z);
	}
	
	/// Returns a Vector3 point in space along the curve given a normalized point within the curve
	public Vector3 getValueNormalized(float normalizedPoint)
	{
		return getValue(normalizedPoint * (lastFrame - firstFrame) + firstFrame);
	}
	
	/// Update the curve with full smoothness
	public void update()
	{
		updateSpline(false);
	}
	
	/// Update the curve as if it were linear movement between points, faster than update()
	public void updateLinear()
	{
		updateSpline(true);
	}

	/// Precalculate each frame's position on the curve so it is faster when actually using it.
	public void updateSpline(bool linear)
	{
		Vector3 first;
		Vector3 last;
		Vector3 val1;
		Vector3 val2;
		float between;
		SplineKeyframe keyframe = null;
		SplineKeyframe prev = null;
		SplineKeyframe next = null;
		
		// Check for weird edge cases
		switch (keyframes.Count)
		{
			case 0:
				Debug.LogWarning("Spline.updateSpline() shouldn't be called for a curve with zero keyframes.  It makes no damn sense.");
				return;
			
			case 1:
				// This shouldn't happen, but here is the result if it does
				calcVal = new Vector3[1];
				calcVal[0] = keyframes[firstFrame].coord;
				return;
				
			case 2:
				// Short-circuit to linear for 2-point paths
				linear = true;
				break;
		}
		
		// Do a pass to assign back/next associations
		for (int frame = 0; frame <= lastFrame; frame++)
		{
			if (keyframes.ContainsKey(frame))
			{
				keyframe = keyframes[frame];
				
				// Assign previous keyframes next value
				if (prev != null)
				{
					prev.next = keyframe;
				}
				
				keyframe.prev = prev;
				prev = keyframe;
			}
		}
		
		
		// First sort the keyframes so they're in order, just in case some numbnuts didn't add them in chronological order.
		//keyframes.Sort(new SplineKeyframe.Comparer());

		calcVal = new Vector3[lastFrame - firstFrame + 1];

		for (int frame = firstFrame; frame <= lastFrame; frame++)
		{
			Vector3 values = Vector3.zero;

			// First see if the current frame is a keyframe.
			if (keyframes.ContainsKey(frame))
			{
				// If the current frame is a keyframe, then simply set the values to the keyframe's values without calculating anything.
				keyframe = keyframes[frame];
				values.x = keyframe.coord.x;
				values.y = keyframe.coord.y;
				values.z = keyframe.coord.z;
			}
			else
			{
				next = keyframe.next;
				prev = keyframe.prev;

				between = ((float)(frame - keyframe.frame)) / ((float)(next.frame - keyframe.frame));

				if (linear)
				{
					val1.x = keyframe.coord.x;
					val1.y = keyframe.coord.y;
					val1.z = keyframe.coord.z;

					val2.x = next.coord.x;
					val2.y = next.coord.y;
					val2.z = next.coord.z;
				}
				else
				{
					// Calculate the coords

					// Get values from the previous keyframe.
					if (prev == null)
					{
						// First keyframe.
						// Since the Hermite curve requires a leading point for the curve, calculate one here
						first.x = keyframe.coord.x + (keyframe.coord.x - next.coord.x);
						first.y = keyframe.coord.y + (keyframe.coord.y - next.coord.y);
						first.z = keyframe.coord.z + (keyframe.coord.z - next.coord.z);
					}
					else
					{
						first.x = prev.coord.x;
						first.y = prev.coord.y;
						first.z = prev.coord.z;
					}
					// Get values from the next keyframe.
					if (next.next == null)
					{
						// Last keyframe to calculate, which is actually second from last because we calculate from the start of a point to the next point.
						// Since the Hermite curve requires a trailing point for the curve, calculate one here
						last.x = next.coord.x + (next.coord.x - keyframe.coord.x);
						last.y = next.coord.y + (next.coord.y - keyframe.coord.y);
						last.z = next.coord.z + (next.coord.z - keyframe.coord.z);
					}
					else
					{
						last.x = next.next.coord.x;
						last.y = next.next.coord.y;
						last.z = next.next.coord.z;
					}

					// Calculate the position based on the first keyframe's tension and bias.
					val1.x = hermiteInterpolate(first.x, keyframe.coord.x, next.coord.x, last.x, between, keyframe.tension, keyframe.bias);
					val1.y = hermiteInterpolate(first.y, keyframe.coord.y, next.coord.y, last.y, between, keyframe.tension, keyframe.bias);
					val1.z = hermiteInterpolate(first.z, keyframe.coord.z, next.coord.z, last.z, between, keyframe.tension, keyframe.bias);

					// Calculate the position based on the second keyframe's tension and bias.
					val2.x = hermiteInterpolate(first.x, keyframe.coord.x, next.coord.x, last.x, between, next.tension, next.bias);
					val2.y = hermiteInterpolate(first.y, keyframe.coord.y, next.coord.y, last.y, between, next.tension, next.bias);
					val2.z = hermiteInterpolate(first.z, keyframe.coord.z, next.coord.z, last.z, between, next.tension, next.bias);					
				}

				// Average the two calculations based on the position between the keyframes.			
				values.x = val1.x + (val2.x - val1.x) * between;
				values.y = val1.y + (val2.y - val1.y) * between;
				values.z = val1.z + (val2.z - val1.z) * between;
			}
			
			calcVal[frame - firstFrame] = values;
		}
	}
	
	/// Returns true if there is a key in the input index frame
	public bool keyframeExists(int frame)
	{
		return keyframes.ContainsKey(frame);
	}
	
	/// Basic spline interpolation in 1-dimension
	private float hermiteInterpolate(float y0, float y1, float y2, float y3, float mu, float tension, float bias)
	{
		float m0;
		float m1;
		float mu2;
		float mu3;
		float a0;
		float a1;
		float a2;
		float a3;
		mu2 = mu * mu;
		mu3 = mu2 * mu;
		m0 = (y1 - y0) * (1f + bias) * (1f - tension) * 0.5f;
		m0 += (y2 - y1) * (1f - bias) * (1f - tension) * 0.5f;
		m1 = (y2 - y1) * (1f + bias) * (1f - tension) / 2;
		m1 += (y3 - y2) * (1f - bias) * (1f - tension) / 2;
		a0 = 2f * mu3 - 3f * mu2 + 1;
		a1 = mu3 - 2f * mu2 + mu;
		a2 = mu3 - mu2;
		a3 = -2f * mu3 + 3f * mu2;
		return (a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2);
	}
	
	/// Adds multiple keyframes from the given list of vectors, where the key numbers are based on distance from the last value.
	/// Uses 0 for tension and bias for all keys.
	public void addKeyframeList(List<Vector3> nodeLocations, bool useSlowerAccurateMethod = false)
	{
		int key;
		Vector3 lastLocation;
		
		if (keyframes.ContainsKey(lastFrame))
		{
			// Continue from the last point, plus one to make sure we don't clobber a critical end frame
			key = lastFrame + 1;
			lastLocation = keyframes[lastFrame].coord;
		}
		else
		{
			// Start a new path
			key = 0;
			lastLocation = nodeLocations[0];
		}
		
		foreach (Vector3 nodeLocation in nodeLocations)
		{
			// Use the distance between tiles as the keyframe, so movement speed between the tiles is consistent.
			if (!useSlowerAccurateMethod)
			{
				// This is much faster and should be the default.
				key += CommonMath.fastIntSqrt(Mathf.CeilToInt((lastLocation - nodeLocation).sqrMagnitude));
			}
			else
			{
				key += Mathf.RoundToInt(Vector3.Distance(lastLocation, nodeLocation));
			}
			
			addKeyframe(key, 0, 0, nodeLocation);

			lastLocation = nodeLocation;
		}
	}
	
	/// Returns a new curve that has the same keyframes as the original.
	/// Does not update the curve, so the caller is responsible for that after doing whatever with it.
	public Spline copy()
	{
		Spline spline = new Spline();
		
		foreach (SplineKeyframe keyframe in keyframes.Values)
		{
			spline.addKeyframe(keyframe.frame, keyframe.tension, keyframe.bias, keyframe.coord);
		}
		
		return spline;
	}
}

