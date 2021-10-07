using UnityEngine;
using System.Collections;

/*
  Attach this script to an element that needs to be dynamically scaled.  It will support a range between aspect ratios 1:1 to 2:1.
  Please place this above the UIStretch value
*/
[RequireComponent(typeof(UIStretch))]
public class UIStretchAdjuster : MonoBehaviour
{
	void Start()
	{
		UIStretch thisThingToStretch = this.gameObject.GetComponent<UIStretch>();
		thisThingToStretch.style = UIStretch.Style.FillKeepingRatio;
		UITexture texture = this.gameObject.GetComponent<UITexture>();
		if (texture != null)
		{
			if (texture.mainTexture.width > texture.mainTexture.height)
			{
				thisThingToStretch.initialSize.x = (float)texture.mainTexture.width / (float)texture.mainTexture.height;
				thisThingToStretch.initialSize.y = 1.0f;
			}
			else
			{
				thisThingToStretch.initialSize.x = 1.0f;
				thisThingToStretch.initialSize.y = (float)texture.mainTexture.height / (float)texture.mainTexture.width;
			}
		} 
		/* else
		{
			if (Camera.main.aspect >= 1.0)
			{
				thisThingToStretch.relativeSize.x = Camera.main.aspect;
				thisThingToStretch.relativeSize.y = 1.0f;
			}
			else
			{
				thisThingToStretch.relativeSize.x = 1.0f;
				thisThingToStretch.relativeSize.y = Camera.main.aspect;
			}
		} */
		Destroy (this);
	}
}

