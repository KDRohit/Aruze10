using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This class is for helping to determine the optimal position and size of a 3D mesh object
that is to be displayed in NGUI space like on a dialog.

Attach this script to an empty GameObject that is being used as the parent anchor for the 3D mesh object.
Adjust the width and height until the box fits the area you want the object to fit into.
Call attachObject() to attach the object to this anchor and it will scale to fit the area.
*/

public class NGUIMeshObjectAnchor : TICoroutineMonoBehaviour
{
	public Vector2 areaSize;
	public TextAnchor anchor = TextAnchor.LowerCenter;

	private GameObject scaleObject;	///< Link a game object to this to keep it fitting in the area size dynamically.
	private Vector2 lastSize = new Vector2(0, 0);		///< Cache the last size for optimization.

	void Update()
	{
		if (scaleObject != null)
		{
			if (areaSize.x != lastSize.x || areaSize.y != lastSize.y)
			{
				// The area size changed, so update the linked transform.
				fitObject();
			}
		}
	}

	public void attachObject(GameObject obj)
	{
		scaleObject = obj;
		scaleObject.transform.parent = transform;
		scaleObject.transform.localPosition = Vector3.zero;
		fitObject();
	}
	
	private void fitObject()
	{
		CommonGameObject.fitGameObjectToPanel(scaleObject, areaSize);
		lastSize.x = areaSize.x;
		lastSize.y = areaSize.y;
	}

	/// Draw the border of the area in the editor.
	void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		
		if (areaSize.x < 0)
		{
			areaSize.x = 0;
		}

		if (areaSize.y < 0)
		{
			areaSize.y = 0;
		}
		
		Vector3 worldScale = CommonTransform.getWorldScale(transform);

		Vector3 boxSize = new Vector3(
			areaSize.x * worldScale.x,
			areaSize.y * worldScale.y,
			0
		);

		float centerX = 0;
		float centerY = 0;
		
		switch (anchor)
		{
			case TextAnchor.UpperLeft:
			case TextAnchor.UpperCenter:
			case TextAnchor.UpperRight:
				centerY = -(boxSize.y / 2);
				break;

			case TextAnchor.LowerLeft:
			case TextAnchor.LowerCenter:
			case TextAnchor.LowerRight:
				centerY = (boxSize.y / 2);
				break;
		}

		switch (anchor)
		{
			case TextAnchor.UpperLeft:
			case TextAnchor.MiddleLeft:
			case TextAnchor.LowerLeft:
				centerX = (boxSize.x / 2);
				break;

			case TextAnchor.UpperRight:
			case TextAnchor.MiddleRight:
			case TextAnchor.LowerRight:
				centerX = -(boxSize.x / 2);
				break;
		}
				
		Vector3 cubePosition = transform.position + new Vector3(
			centerX,
			centerY,
			0
		);
				
		Gizmos.DrawWireCube(cubePosition, boxSize);
	}
}
