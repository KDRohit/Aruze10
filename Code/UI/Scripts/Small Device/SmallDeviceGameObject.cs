using UnityEngine;
using System.Collections;

/*
 Define a small gameobject for small devices.
 */
public class SmallDeviceGameObject : MonoBehaviour
{
	public GameObject smallGo;

	public static GameObject getSmallGo(GameObject go)
	{
		GameObject smallGo = null;		
		SmallDeviceGameObject smallDeviceGo = go.GetComponent<SmallDeviceGameObject>();

		if (smallDeviceGo != null)
		{
			smallGo = smallDeviceGo.smallGo;
		}
		
		return (smallGo);
	}

	// if it's a small device and the game object has a small version, then return the small version.
	// otherwise, return the game object itself.
	public static GameObject getGo(GameObject go)
	{
		if (MobileUIUtil.isSmallMobile)
		{
			GameObject smallGo = getSmallGo(go);
			
			if (smallGo != null)
			{
				return (smallGo);
			}
		}
		
		return (go);
	}
}
