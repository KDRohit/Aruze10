using UnityEngine;
using System.Collections;

/*
 Invoke this to be able to alpha a particular object when on a small device size according to the parameters set in the script
 */
public class SmallDeviceSpriteAlpher : MonoBehaviour
{
	public float alpha = 1.0f;
	
	/// We want to execute the alpha once when spawning the prefab.  
	void Awake ()
	{
		//Check current device size, and if we are in small device mode, set the alpha
		if (MobileUIUtil.isSmallMobile)
		{
			CommonGameObject.alphaUIGameObject(this.gameObject,alpha);
		}
		Destroy (this);
	}
}
