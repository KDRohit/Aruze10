using UnityEngine;
using System.Collections;

/**
 Monobehaviour for resizing all of the children UI Sprites of a dialog subcomponent when playing on a device with different resolution
 than our standard assets.  There is an assumption that everything in the dialogues that descend from this adjuster are anchored.
*/ 
public class ChildSizeAdjuster : MonoBehaviour
{
	void Start ()
	{
		// Check to see if our main aspect ratio is not being used, and if so, resize all of the children.  
		// We also shouldn't do anything if our aspect ratio is lower than 1, ie 3x4, and also we need to ignore sizes beyond 2:1
		if (Camera.main.aspect != (4.0f / 3.0f))
		{
			//Get a list of all of the UI sprites that are in this object's descendants.
			
			if (Camera.main.aspect >= 1.0f && Camera.main.aspect <= 2.0f)
			{
				//Figure out the scaling factor required for both x and y
				scaleChildren(Camera.main.aspect / (4.0f / 3.0f));
			}
			// If we are outside of our bounds, we should clip as close as our bounds allow us to go 
			else if (Camera.main.aspect < 1.0f)
			{
				scaleChildren(0.75f);
			}
			else
			{
				scaleChildren(1.5f);
			}
		}
	}


	///All of the resizing is in the end done here
	private void scaleChildren(float multiplierToUse)
	{
		UISprite[] allSprites = this.gameObject.GetComponentsInChildren<UISprite>();
		foreach (UISprite sprite in allSprites)
		{
			sprite.gameObject.transform.localScale = new Vector3(sprite.gameObject.transform.localScale.x * multiplierToUse, sprite.gameObject.transform.localScale.y, 1);
		}
	}
}

