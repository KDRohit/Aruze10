using UnityEngine;
using UnityEngine.Rendering;

public static class CommonTransform {

	/// Returns the world scale of the given transform.
	/// I wrote this since the Transform class only has localScale for some reason.
	public static Vector3 getWorldScale(Transform transform)
	{
		return localScaleToParent(transform, null);
	}
	
	/// Takes the world scale and converts it to a localScale for the given transform.
	/// I wrote this since the Transform class only has localScale for some reason.
	public static void setWorldScale(Transform transform, Vector3 scale)
	{
		Transform parent = transform.parent;
		
		while (parent != null)
		{
			scale = new Vector3(scale.x / parent.localScale.x, scale.y / parent.localScale.y, scale.z / parent.localScale.z);
			parent = parent.parent;
		}
		
		transform.localScale = scale;
	}

	/// Returns the child's local scale as it relates to the given parent,
	/// which may not be a direct parent.
	public static Vector3 localScaleToParent(Transform child, Transform parent)
	{
		Vector3 scale = Vector3.one;
		
		while (child != null && child != parent)
		{
			scale.x *= child.localScale.x;
			scale.y *= child.localScale.y;
			scale.z *= child.localScale.z;
			
			child = child.parent;
		}
		
		return scale;
	}
	
	/// Returns the child's local positon as it relates to the given parent,
	/// which may not be a direct parent.
	public static Vector3 localPositionToParent(Transform child, Transform parent)
	{
		Vector3 pos = Vector3.zero;
		
		while (child != null && child != parent)
		{
			pos += child.localPosition;
			child = child.parent;
		}
		return pos;
	}

	// Translates a local distance to world distance, based on the given transform as the local transform.
	public static Vector3 localToWorldDistance(Transform localTransform, Vector3 localDistance)
	{
		Vector3 scale = getWorldScale(localTransform);
		localDistance.x *= scale.x;
		localDistance.y *= scale.y;
		localDistance.z *= scale.z;
		return localDistance;
	}

	/// Orient the transform toward the point while keeping it oriented straight up
	public static void lookAtPoint(Transform transform, Vector3 point, bool correctY = true)
	{
		Vector3 direction = point - transform.position;
		if (correctY)
		{
			direction.y = 0f;
		}
		if (direction != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(direction);
		}
	}
	
	/// Sets only the X value of a transform's position.
	public static void setX(Transform transform, float x, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}
		
		pos.x = x;
		
		if (space == Space.Self)
		{
			transform.localPosition = pos;
		}
		else
		{
			transform.position = pos;
		}
	}
	
	/// Sets only the width value of a transform's scale.
	public static void setWidth(Transform transform, float value)
	{
		Vector3 scale = transform.localScale;
		scale.x = value;
		transform.localScale = scale;
	}

	/// Sets only the width value of a transform's scale.
	public static void addWidth(Transform transform, float value)
	{
		Vector3 scale = transform.localScale;
		scale.x += value;
		transform.localScale = scale;
	}	
	
	/// Sets only the height value of a transform's scale.
	public static void setHeight(Transform transform, float value)
	{
		Vector3 scale = transform.localScale;
		scale.y = value;
		transform.localScale = scale;
	}
	
	/// Sets only the depth value of a transform's scale.
	public static void setDepth(Transform transform, float value)
	{
		Vector3 scale = transform.localScale;
		scale.z = value;
		transform.localScale = scale;
	}

	/// Sets only the Y value of a transform's position.
	public static void setY(Transform transform, float y, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}
		
		pos.y = y;
		
		if (space == Space.Self)
		{
			transform.localPosition = pos;
		}
		else
		{
			transform.position = pos;
		}
	}
	
	/// Sets only the Z value of a transform's position.
	public static void setZ(Transform transform, float z, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}
		
		pos.z = z;
		
		if (space == Space.Self)
		{
			transform.localPosition = pos;
		}
		else
		{
			transform.position = pos;
		}
	}

	public static void addX(Transform transform, float x, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}

		setX(transform, pos.x + x, space);
	}

	public static void addY(Transform transform, float y, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}

		setY(transform, pos.y + y, space);
	}

	public static void addZ(Transform transform, float z, Space space = Space.Self)
	{
		Vector3 pos;
		
		if (space == Space.Self)
		{
			pos = transform.localPosition;
		}
		else
		{
			pos = transform.position;
		}

		setZ(transform, pos.z + z, space);
	}	

	public static Vector3 setObjectLocationWithJSON(JSON objectJSON, Vector3 vectorToModify)
	{
		vectorToModify.x = objectJSON.getInt("x", 0);
		vectorToModify.y = objectJSON.getInt("y", 0);
		vectorToModify.z = objectJSON.getInt("z", 0);

		return vectorToModify;
	}

	public static Vector3 setObjectScaleWithJSON(JSON objectJSON, Vector3 vectorToModify)
	{
		vectorToModify.x = objectJSON.getInt("width", 0);
		vectorToModify.y = objectJSON.getInt("height", 0);
		vectorToModify.z = 1;

		return vectorToModify;
	}

	// This has only been tested on objects that are drawn by the UIRoot Cameras (Dialog Camera, Overlay, etc)
	// If there are other cameras involved then this may not work as designed. User beware.
	public static void matchScreenPosition(Transform objectToMove, Transform objectToMatch)
	{
		// First find the Cameras that are drawing these.
	    Camera moveObjectCamera = NGUIExt.getObjectCamera(objectToMove.gameObject);
	    Camera matchObjectCamera = NGUIExt.getObjectCamera(objectToMatch.gameObject);;

		Vector3 moveObjectRelativePos = moveObjectCamera.transform.InverseTransformPoint(objectToMove.transform.position);
		Vector3 matchObjectRelativePos = matchObjectCamera.transform.InverseTransformPoint(objectToMatch.transform.position);
		Vector3 amountToMove = matchObjectRelativePos - moveObjectRelativePos;

		CommonTransform.addX(objectToMove, amountToMove.x);
		CommonTransform.addY(objectToMove, amountToMove.y);
	}
}
