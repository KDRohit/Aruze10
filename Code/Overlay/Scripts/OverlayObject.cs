using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class OverlayObject : MonoBehaviour
{	
	// The sprite we should use for determining the size of the button.
	public UISprite sizingSprite;

	private Transform sizingTransform
	{
		get
		{
			if (sizingSprite != null)
			{
				// If they have given a sizing sprite, use that.
				return sizingSprite.transform;
			}
			else
			{
				// Otherwise just use whatever this parent object is.
				return transform;
			}
		}
	}
	
	private float _width;
	public float width
	{
		get
		{
			if (rightMostObject != null && leftMostObject != null)
			{
				// Use the RealPosition() here (world position adjusted for UIScale) to make sure that even if the parent is sized down we get the real size.
				return rightMostObject.transform.RealPosition().x - leftMostObject.transform.RealPosition().x;
			}
			else
			{
				return 0;
			}			
		}
		set
		{
			_width = value;
		}
	} 
	public int index = 0;

	public GameObject leftMostObject;
	public GameObject rightMostObject;

	public void setPosition(float x, bool isInitialized = false)
	{
		// We are using the left edge of the button as the position from the organizer.
		CommonTransform.setX(transform, x + (transform.RealPosition().x - leftMostObject.transform.RealPosition().x));
	}
}