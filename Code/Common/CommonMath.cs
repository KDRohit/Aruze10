using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is a purely static class of generic useful functions that relate to math.
*/
public static class CommonMath
{

	/// There is no native function for rounding to a certain decimal place, so here's mine.
	/// If rounding to int, consider using Mathf.RoundToInt() instead.
	public static float round(float val,int decimalPlaces)
	{
	
		float mult = Mathf.Pow(10,decimalPlaces);
		return Mathf.Round(val * mult) / mult;
	}
	
	// Rounds a float to a long. There is no built-in functionality for this, surprisingly.
	public static long roundToLong(float val)
	{
		return (long)(val + 0.5f);
	}

	/// Returns the next whole value closest to 0.
	/// Example: -1.5 would return -1, 1.5 would return 1.
	public static float signedFloor(float val)
	{
		return Mathf.Floor(Mathf.Abs(val)) * Mathf.Sign(val);
	}
	
	/// Returns the next whole value farthest from 0.
	/// Example: -1.5 would return -2, 1.5 would return 2.
	public static float signedCeil(float val)
	{
		return Mathf.Ceil(Mathf.Abs(val)) * Mathf.Sign(val);
	}
	
	// Returns the sign of the value, or 0 if the value is 0.
	// Unity's version returns 1 if the value is 0.
	public static int sign(int value)
	{
		return sign((float)value);
	}

	public static int sign(float value)
	{
		if (value > 0)
		{
			return 1;
		}
		else if (value < 0)
		{
			return -1;
		}
		return 0;
	}
	
	/// Returns the average of two surface normals between two triangles.
	public static Vector3 calculateRectSurfaceNormal(Vector3[] vertices)
	{
		// Returns the closest possible normal of a rectangle, since the rectangle could be non-flat.
		Vector3 normal1 = calculateSurfaceNormal(new Vector3[] { vertices[0], vertices[2], vertices[1] });
		Vector3 normal2 = calculateSurfaceNormal(new Vector3[] { vertices[2], vertices[3], vertices[1] });
		
		return (normal1 + normal2) / 2;
	}

	/// Returns the surface area of a triangle.
	public static float triangleArea(Vector2 a, Vector2 b, Vector2 c)
	{
		return ((a.x - c.x) * (b.y - c.y) - (a.y - c.y) * (b.x - c.x)) / 2;
		
	}

	/// Returns a string version of the given int value passed in and appends plus sign if needed.
	public static string signedInt(int value)
	{
		string plus = (value > 0 ? "+" : "");
		return plus + value.ToString();
	}

	/// Convert angle degrees to radians, so it can be used with functions that require radians.
	public static float degreesToRadians(float degrees)
	{
		return degrees * (Mathf.PI / 180);
	}
	
	/// Convert angle radians to degrees, so it can be used with functions that require degrees.
	public static float radiansToDegrees(float radians)
	{
		return radians * (180/Mathf.PI);
	}

	/// Returns the non-normalized surface normal given 3 vertices.
	public static Vector3 calculateSurfaceNormal(Vector3[] vertices)
	{
		// Returns normal of a triangle.
		// Vertices must be in clockwise order in the array.
		Vector3 u = vertices[1] - vertices[0];
		Vector3 v = vertices[2] - vertices[0];
		
		Vector3 normal = new Vector3(u.y * v.z - u.z * v.y, u.z * v.x - u.x * v.z, u.x * v.y - u.y * v.x);
		
		return normal;
	}
	
	/// This sqrt function only works with integer input and output.
	/// It is an adaptation of Newton's method.
	public static int fastIntSqrt(int x)
	{
		if (x < 2)
		{
			return x;
		}
		
		int a;
		int b;
		
		a = x >> 1;
		b = (a + (x / a)) >> 1;
		
		while (b < a)
		{
			a = b;
			b = (a + (x / a)) >> 1;
		}
		
		return a;
	}

	/// Returns either 1 or -1 randomly.
	public static int randomSign
	{
		get
		{
			if (Random.Range(0, 2) == 1)
			{
				return 1;
			}
			return -1;
		}
	}

	/// Chooses a random index value from the list of chances.
	/// The values passed must be in something that implements IList,
	/// such as float[] or List<float>.
	/// For example, if chances contains { 20, 10, 30 }
	/// then the overall chance is 60, so the first option
	/// has a 20/60 chance, second has 10/60, third has 30/60.
	/// Example usage:
	/// string[] message = new string[]
	/// {
	/// 	"This message has a 20/60 chance.",
	/// 	"This message has a 10/60 chance.",
	/// 	"This message has a 30/60 chance."
	/// }
	/// float[] chances = new float[] { 20, 10, 30 };
	/// int choice = chooseRandomWeightedValue(chances);
	/// Debug.Log(message[choice]);
	public static int chooseRandomWeightedValue(IList<float> chances)
	{
		float totalChance = 0f;
		foreach (float chance in chances)
		{
			totalChance += chance;
		}
		
		if (totalChance < 0.000001f)
		{
			Debug.LogError("Total chances for Common.chooseRandomWeightedValue is < .0001, which is too small. Chances should be defined higher.");
			return -1;
		}
		
		float random = Random.Range(0.000001f, totalChance);	// Min value is never 0, to prevent choosing 0-chance options.
		
		totalChance = 0f;	// Reuse this variable in the loop below.
		
		for (int i = 0; i < chances.Count; i++)
		{
			totalChance += chances[i];
			if (random <= totalChance)
			{
				return i;
			}
		}
		return -1;	// Should never happen!
	}

	/// A basic 2D distance calculation function.
	/// Use this instead of Vector2.Distance if you have raw coords instead of Vector2 objects
	/// so we don't have to create any new Vector2 objects to find the distance.
	public static float distance(float x1, float y1, float x2, float y2)
	{
		return Mathf.Sqrt(sqrDistance(x1, y1, x2, y2));
	}
	
	/// A basic 2D squared distance calculation function.
	/// Use this instead of Vector2.Distance if you have raw coords instead of Vector2 objects
	/// so we don't have to create any new Vector2 objects to find the distance.
	/// Performance note: USE THIS IF AT ALL POSSIBLE OVER THE VERSION THAT NEEDS Mathf.Sqrt()
	public static float sqrDistance(float x1, float y1, float x2, float y2)
	{
		float diffX = (x1 - x2);
		float diffY = (y1 - y2);
		return diffX * diffX + diffY * diffY;
	}
	
	/// Returns whether the point falls within the rectangle. (int)
	public static bool rectContainsPoint(Rect rect, Vector2int point)
	{
		return rectContainsPoint(rect, point.x, point.y);
	}
	
	/// Returns whether the point falls within the rectangle. (float)
	public static bool rectContainsPoint(Rect rect, Vector2 point)
	{
		return rectContainsPoint(rect, point.x, point.y);
	}
	
	/// Returns whether the point falls within the rectangle. (float)
	public static bool rectContainsPoint(Rect rect, float pointX, float pointY)
	{
		return (
			pointX >= rect.xMin &&
			pointX <= rect.xMax &&
			pointY >= rect.yMin &&
			pointY <= rect.yMax
			);
	}
	
	/// Returns whether the two rectangles overlap.
	public static bool rectsOverlap(Rect rect1, Rect rect2)
	{
		return (
			rectContainsPoint(rect2, rect1.xMin, rect1.yMin) ||
			rectContainsPoint(rect2, rect1.xMax, rect1.yMin) ||
			rectContainsPoint(rect2, rect1.xMin, rect1.yMax) ||
			rectContainsPoint(rect2, rect1.xMax, rect1.yMax)
			);
	}

	/// Returns the Manhattan distance between two vectors, ignoring the Y.
	public static float manhattanDistance(Vector3 point1, Vector3 point2)
	{
		float xDiff = Mathf.Abs(point1.x - point2.x);
		float zDiff = Mathf.Abs(point1.z - point2.z);
		
		return Mathf.Max(xDiff, zDiff);
	}

	/// Returns the normalized value between 0 and 1 for the given value based on the given min and max.
	public static float normalizedValue(float min, float max, float value)
	{
		if (max == min)
		{
			// Avoid divide-by-zero errors.
			return 0.0f;
		}
		max = Mathf.Max(max, min);	// Make sure the max isn't lower than the min.
		max -= min;
		value -= min;
		return Mathf.Clamp01(value / max);
	}

	/// Return the rotation angle between two points in 2D space,
	/// where the angle would point from the first point to the second point.
	public static float angleBetweenPoints(Vector2int point1, Vector2int point2)
	{
		return angleBetweenPoints(new Vector2(point1.x, point1.y), new Vector2(point2.x, point2.y));
	}
	
	/// Return the rotation angle between two points in 2D space,
	/// where the angle would point from the first point to the second point.
	public static float angleBetweenPoints(Vector2 point1, Vector2 point2)
	{
		Vector2 vec = point2 - point1;
		
		if (vec == Vector2.zero)
		{
			return 0;
		}
		
		float angle = -Vector2.Angle(Vector2.up, vec);
		
		if (vec.x < 0)
		{
			angle = -angle;
		}
		
		return angle;
	}
	public static Vector2 rotatePointAroundPoint(Vector2 offset, Vector2 point, float degrees, Vector2 scale)
	{
		float s = 0f;
		float c = 0f;
		
		if (Application.isPlaying)
		{
			s = Mathf.Sin(CommonMath.degreesToRadians(degrees));
			c = Mathf.Cos(CommonMath.degreesToRadians(degrees));
		}
		else
		{
			// This is used for RunsInEditMode scripts to avoid the one-time 
			// hit associated with generating the FastTrig lookup table.
			s = Mathf.Sin(CommonMath.degreesToRadians(degrees));
			c = Mathf.Cos(CommonMath.degreesToRadians(degrees));			
		}
		
		// rotate point
		float x = point.x * c + point.y * s;
		float y = -point.x * s + point.y * c;
		
		point.x = x;
		point.y = y;
		
		// Apply scaling.
		if (scale.x > 0.0f)
		{
			point.x /= scale.x;
		}
		if (scale.y > 0.0f)
		{
			point.y /= scale.y;
		}
		
		// Apply the pivot offset after scaling.
		point.x += offset.x;
		point.y += offset.y;
		
		return point;
	}

	/// Rotates a vector on the X axis.
	public static Vector3 vectorRotateX(Vector3 v, float angle)
	{
		float sin = Mathf.Sin(CommonMath.degreesToRadians(angle));
		float cos = Mathf.Cos(CommonMath.degreesToRadians(angle));
		
		float ty = v.y;
		float tz = v.z;
		v.y = (cos * ty) - (sin * tz);
		v.z = (cos * tz) + (sin * ty);
		
		return v;
	}
	
	/// Rotates a vector on the Y axis.
	public static Vector3 vectorRotateY(Vector3 v, float angle)
	{
		
		float sin = Mathf.Sin(CommonMath.degreesToRadians(angle));
		float cos = Mathf.Cos(CommonMath.degreesToRadians(angle));
		
		float tx = v.x;
		float tz = v.z;
		v.x = (cos * tx) + (sin * tz);
		v.z = (cos * tz) - (sin * tx);
		
		return v;
	}
	
	/// Rotates a vector on the Z axis.
	public static Vector3 vectorRotateZ(Vector3 v, float angle)
	{
		float sin = Mathf.Sin(CommonMath.degreesToRadians(angle));
		float cos = Mathf.Cos(CommonMath.degreesToRadians(angle));

		float tx = v.x;
		float ty = v.y;
		v.x = (cos * tx) - (sin * ty);
		v.y = (cos * ty) + (sin * tx);
		
		return v;
	}

	//copied from iTween
	public static float easeOutCubic(float start, float end, float t)
	{
		t--;
		end -= start;
		return end * (t * t * t + 1) + start;
	}

	/// <summary>
	///   umod always returns a positive mod value
	/// </summary>
	public static long umod(int n, int mod)
	{
		return (n%mod +mod)%mod;
	}

	// Get an interpolated value between the start and end value using t as the normalized value between the two
	public static float getInterpolatedFloatValue(float startValue, float endValue, float t, bool doEaseOutCubic)
	{
		// TODO: could add other iTween easeTypes as well, but I like easeOutCubic for fades
		if (doEaseOutCubic)
		{
			return CommonMath.easeOutCubic(startValue, endValue, t);
		}
		else
		{
			return Mathf.Lerp(startValue, endValue, t);
		}
	}

	// Get the absolute value for a long value (there isn't a built in version of this for Unity)
	public static long abs(long val)
	{
		if (val == long.MinValue)
		{
			Debug.LogError("CommonMath.abs() - Cannot negate long.MinValue and have it still fit within a long, so returning value as is.");
			return val;
		}
	
		if (val < 0)
		{
			return -val;
		}
		else
		{
			return val;
		}
	}

	public static long max(long one, long two)
	{
		// Mathf doesn't have a max function for long.
		return (one >= two) ? one : two;
	}
}
