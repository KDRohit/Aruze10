using UnityEngine;
using System.Collections;

/** 
	This will swap a UITexture's main texture based on whether it is a "small device" or a large device.
*/
[RequireComponent(typeof(UITexture))]
public class SmallDeviceTextureSwap : MonoBehaviour {

	[SerializeField] private Texture2D smallDeviceTexture; // Texture2D we should use on small devices
	[SerializeField] private Texture2D largeDeviceTexture; // Texture2D we shoudl use on large devices
	[SerializeField] private UITexture gameTexture; // the UITexture that we want to swap. Opened to inpsector to avoid a GetComponent call

	public void Awake()
	{
		if(MobileUIUtil.isSmallMobile)
		{
			gameTexture.mainTexture = smallDeviceTexture;
			Resources.UnloadAsset(largeDeviceTexture);
			largeDeviceTexture = null; // possibly unnecessary?
		}
		else
		{
			gameTexture.mainTexture = largeDeviceTexture;
			Resources.UnloadAsset(smallDeviceTexture);
			smallDeviceTexture = null;
		}
		Destroy(this);
	}
}
