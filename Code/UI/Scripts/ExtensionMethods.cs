using UnityEngine;
using System.Collections;

public static class ExtensionMethods
{
	/* Returns the position of the transform adjusted by the UIRoot scale. Because this scale
	   is so small it messes with calculations on the positional values if you need the global
	   position.

	   **NOTE**
	   If you are going to access the x, y, z coordinates of this successively, then just
	   cache the Vector3 once and acces that, to avoid doing the calculation three times.
	*/
	public static Vector3 RealPosition(this Transform trans)
	{
		Vector3 result = trans.position;
		Vector3 scale = new Vector3(
			(1 / trans.root.localScale.x),
			(1 / trans.root.localScale.y),
			(1 / trans.root.localScale.z));
		result.Scale(scale);
		return result;
	}
	
	/* Returns a reproducible hash of a string 
	 * The built in has for string is non deterministic and can change every launch 
	 */
	public static int GetStableHashCode(this string str)
	{
		unchecked
		{
			int hash1 = 5381;
			int hash2 = hash1;
			for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
			{
				hash1 = ((hash1 << 5) + hash1) ^ str[i];
				if (i == str.Length - 1 || str[i + 1] == '\0')
					break;
				hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
			}

			return hash1 + (hash2 * 1566083941);
		}
	}
}

