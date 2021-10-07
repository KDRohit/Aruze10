using UnityEngine;
using System.Collections;

/**
Simply a class to hold two int values.
*/
[System.Serializable] public class Vector2int
{
	public int x;
	public int y;
	
	/// Property to return a Vector2(0, 0)
	public static Vector2int zero
	{
		get
		{
		  	return new Vector2int(0, 0);
		}
	}

	public static Vector2int one
	{
		get
		{
		  	return new Vector2int(1, 1);
		}
	}
	
	public Vector2int()
	{
		x = 0;
		y = 0;
	}
	
	/// Copy constructor
	public Vector2int(Vector2int inVec)
	{
		if (inVec == null)
		{
			x = 0;
			y = 0;
			return;
		}
		x = inVec.x;
		y = inVec.y;
	}
	
	public Vector2int(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	
	/// Operator for adding two Vector2ints
	public static Vector2int operator +(Vector2int v1, Vector2int v2)
	{
		return new Vector2int(v1.x + v2.x, v1.y + v2.y);
	}
	
	/// Operator for subtracting two Vector2ints
	public static Vector2int operator -(Vector2int v1, Vector2int v2)
	{
		return new Vector2int(v1.x - v2.x, v1.y - v2.y);
	}
	
	/// Operator for checking equality between two Vector2ints
	// public static bool operator ==(Vector2int v1, Vector2int v2)
	// {
	// 	// No idea if v1 can be null with how this equality 
	// 	// operator works but checking for it anyway
	// 	// UPDATE: Checking equality of v1 causes an infinite loop of calling this operator method.
	// 	if (v2 == null)
	// 	{
	// 		return false;
	// 	}
	// 	
	// 	return (v1.x == v2.x) && (v1.y == v2.y);
	// }
	
	/// Operator for checking non-equality between two Vector2ints
	// public static bool operator !=(Vector2int v1, Vector2int v2)
	// {
	// 	return !(v1.Equals(v2));
	// }
	
	/// Operator for checking equality between two Vector2ints
	public override bool Equals(System.Object obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // If parameter cannot be cast to Point return false.
        Vector2int p = obj as Vector2int;
        if ((System.Object)p == null)
        {
            return false;
        }

        // Return true if the fields match:
        return (x == p.x) && (y == p.y);
    }
	
	/// Checks equality between two Vector2ints
    public bool Equals(Vector2int p)
    {
        // If parameter is null return false:
        if ((object)p == null)
        {
            return false;
        }

        // Return true if the fields match:
        return (x == p.x) && (y == p.y);
    }
	
	/// Data hash for checking equality
	public override int GetHashCode()
    {
        return x ^ y;
    }
	
	/// Returns the distance between two points
	public float distanceTo(Vector2int other)
	{
		Vector2 v1 = new Vector2(x,y);
		Vector2 v2 = new Vector2(other.x,other.y);
		return Vector2.Distance(v1,v2);
	}
	
	/// Convenient formatting for printing Vector2int
	public override string ToString()
	{
		return "(" + x + ", " + y + ")";
	}
}
