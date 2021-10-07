using UnityEngine;
using System.Collections;

/**
See this page for documentation, the code comes straight from here:
http://www.unifycommunity.com/wiki/index.php?title=Mathfx

Normalized animation functions similar to Lerp but with curves:
	Hermite: eases in and out.
	Sinerp: eases out.
	Coserp: eases in.
	Berp: slight overshoot and then recovery at end.
	
Bounce: returns a value between 0 and 1 that follows a decaying bounce.
Approx: returns if a value is within a specified range of another value (floats and vectors).
NearestPoint and NearestPointStrict: given a point, returns the nearest point on a line to that given point.

Todd Gillissie:
	I added versions of some of these curves to handle Vector3's instead of a single float,
	to make it easier to interpolate transforms in 3D space.
	I also added SinerpBump, HermiteBump and LerpBump, to create a curve that rises and falls symmetrically
	within its lifecycle, instead of only rising.
*/
public static class Mathz
{
	/// Returns a float after applying Hermite effect along 2 float points
    public static float Hermite(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
    }
   
	/// Returns a Vector3 after applying Hermite effect along 2 Vector3 points
	public static Vector3 Hermite(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, Hermite(0f, 1f, value));
	}
	
	/// Returns a Quaternion after applying Hermite effect
	public static Quaternion Hermite(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, Hermite(0f, 1f, value));
	}
	
	/// Returns a float from creating a Lerp bump as the curve.
	public static float LerpBump(float start, float end, float value)
	{
		if (value < .5f)
		{
			return Mathf.Lerp(start, end, value * 2);
		}
		else
		{
			return Mathf.Lerp(start, end, 1f - ((value - .5f) * 2));			
		}
	}

	/// Returns a Vector3 from creating a Lerp bump as the curve.
	public static Vector3 LerpBump(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, LerpBump(0f, 1f, value));		
	}

	/// Returns a Quaternion from creating a Lerp bump as the curve.
	public static Quaternion LerpBump(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, LerpBump(0f, 1f, value));		
	}
	
	/// Returns a Color from creating a Lerp bump as the curve.
    public static Color LerpBump(Color start, Color end, float value)
    {
        return Color.Lerp(start, end, LerpBump(0f, 1f, value));        
    }

	/// Returns a float from creating a Sinerp bump as the curve.
	public static float SinerpBump(float start, float end, float value)
	{
		if (value < .5f)
		{
			return Sinerp(start, end, value * 2);
		}
		else
		{
			return Sinerp(start, end, 1f - ((value - .5f) * 2));			
		}
	}

	/// Returns a Vector3 from creating a Sinerp bump as the curve.
	public static Vector3 SinerpBump(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, SinerpBump(0f, 1f, value));		
	}

	/// Returns a Quaternion from creating a Sinerp bump as the curve.
	public static Quaternion SinerpBump(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, SinerpBump(0f, 1f, value));		
	}
	
	/// Returns a float from creating a Coserp bump as the curve.
	public static float HermiteBump(float start, float end, float value)
	{
		if (value < .5f)
		{
			return Hermite(start, end, value * 2);
		}
		else
		{
			return Hermite(start, end, 1f - ((value - .5f) * 2));			
		}
	}
	
	/// Returns a Vector3 from creating a Hermite bump as the curve.
	public static Vector3 HermiteBump(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, HermiteBump(0f, 1f, value));		
	}

	/// Returns a Quaternion from creating a Sinerp bump as the curve.
	public static Quaternion HermiteBump(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, HermiteBump(0f, 1f, value));		
	}

	/// Returns a float after applying Ocilating curve to the input value
	public static float Sinerp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
    }
	
	/// Returns a Vector3 after applying Ocilating curve to the input value 
	public static Vector3 Sinerp(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, Sinerp(0f, 1f, value));
	}
	
	/// Returns a Quaternion after applying Ocilating curve to the input value 
	public static Quaternion Sinerp(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, Sinerp(0f, 1f, value));
	}
	
	/// Returns a float after applying Ocilating curve to the input value
    public static float Coserp(float start, float end, float value)
    {
        return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
    }
 
	/// Returns a Vector3 after applying Ocilating curve to the input value
	public static Vector3 Coserp(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, Coserp(0f, 1f, value));
	}
	
	/// Returns a Quaternion after applying Ocilating curve to the input value
	public static Quaternion Coserp(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, Coserp(0f, 1f, value));
	}
	
	/// Returns a float after applying Berp curve to the input value
    public static float Berp(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
        return start + (end - start) * value;
    }
	
	/// Returns a Vector3 after applying Berp curve to the input value
	public static Vector3 Berp(Vector3 start, Vector3 end, float value)
	{
		return Vector3.Lerp(start, end, Berp(0f, 1f, value));
	}
	
	/// Returns a Quaternion after applying Berp curve to the input value
	public static Quaternion Berp(Quaternion start, Quaternion end, float value)
	{
		return Quaternion.Lerp(start, end, Berp(0f, 1f, value));
	}
	
	/// Returns a float after applying a SmoothStep
    public static float SmoothStep (float x, float min, float max)
    {
        x = Mathf.Clamp (x, min, max);
        float v1 = (x-min)/(max-min);
        float v2 = (x-min)/(max-min);
        return -2*v1 * v1 *v1 + 3*v2 * v2;
    }
 
	/// Returns a Vector3 that is the nearest point
    public static Vector3 NearestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = Vector3.Normalize(lineEnd-lineStart);
        float closestPoint = Vector3.Dot((point-lineStart),lineDirection)/Vector3.Dot(lineDirection,lineDirection);
        return lineStart+(closestPoint*lineDirection);
    }
 
	/// Returns a Vector3 that is the nearest point
    public static Vector3 NearestPointStrict(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 fullDirection = lineEnd-lineStart;
        Vector3 lineDirection = Vector3.Normalize(fullDirection);
        float closestPoint = Vector3.Dot((point-lineStart),lineDirection)/Vector3.Dot(lineDirection,lineDirection);
        return lineStart+(Mathf.Clamp(closestPoint,0.0f,Vector3.Magnitude(fullDirection))*lineDirection);
    }
	
	/// Returns a float after applying a diminishing Ocilating curve to the input value
    public static float Bounce(float x) 
	{
        return Mathf.Abs(Mathf.Sin(6.28f*(x+1f)*(x+1f)) * (1f-x));
    }
   
    // test for value that is near specified float (due to floating point inprecision)
    // all thanks to Opless for this!
    public static bool Approx(float val, float about, float range) 
	{
        return ( ( Mathf.Abs(val - about) < range) );
    }

    // test if a Vector3 is close to another Vector3 (due to floating point inprecision)
    // compares the square of the distance to the square of the range as this
    // avoids calculating a square root which is much slower than squaring the range
    public static bool Approx(Vector3 val, Vector3 about, float range) 
	{
        return ( (val - about).sqrMagnitude < range*range);
    }

   /*
     * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
     * This is useful when interpolating eulerAngles and the object
     * crosses the 0/360 boundary.  The standard Lerp function causes the object
     * to rotate in the wrong direction and looks stupid. Clerp fixes that.
     */
    public static float Clerp(float start , float end, float value)
	{
        float min = 0.0f;
        float max = 360.0f;
        float half = Mathf.Abs((max - min)/2.0f);//half the distance between min and max
        float retval = 0.0f;
        float diff = 0.0f;
   
        if ((end - start) < -half){
			diff = ((max - start)+end)*value;
			retval =  start+diff;
		}
		else if ((end - start) > half){
			diff = -((max - end)+start)*value;
			retval =  start+diff;
		}
		else retval =  start+(end-start)*value;

		// Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
		return retval;
	}

}