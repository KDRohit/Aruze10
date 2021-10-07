using UnityEngine;
using System.Collections;

/*
Enforces the size of a sprite to be altered for when we are on small devices.  
 */
public class SmallDeviceBackgroundSizeEnforcer : MonoBehaviour
{
	public GameObject backgroundImageToEnforce = null;
	public int fixedSizeX;
	public int fixedSizeY;
	public bool adjustCollider = true;
	
	private Vector3 localScaleToEnforce;

	/// On first run, fix the scale of the background image set to the new size.
	void Awake()
	{
		localScaleToEnforce = new Vector3(fixedSizeX, fixedSizeY, 1);
		//If this script is attached specifically to a UIImageButton, we don't want the image in the background to makePixelPerfect every time they interact with it.
		//Turn off this resizing functionality.
		UIImageButton imageButton = this.gameObject.GetComponent<UIImageButton>();
		if (imageButton != null)
		{
			imageButton.overrideMakePixelPerfect = true;
		}
	}
	
	/// This update loop checks for a delta of image size, and if it is detected, resize the target appropriately.
	void Update()
	{
		if (MobileUIUtil.isSmallMobile &&
		    backgroundImageToEnforce && 
		    (backgroundImageToEnforce.transform.localScale.x != fixedSizeX || 
		    backgroundImageToEnforce.transform.localScale.y != fixedSizeY))
		{
			backgroundImageToEnforce.transform.localScale = localScaleToEnforce;
			
			if (adjustCollider && backgroundImageToEnforce != gameObject)
			{
				// If there is a box collider on this object, then adjust the size of that to match,
				// since it's probably a button that should match the background's size.
				BoxCollider box = GetComponent<BoxCollider>();
			
				if (box != null)
				{
					box.size = localScaleToEnforce;
				}
			}
		}
	}
}

