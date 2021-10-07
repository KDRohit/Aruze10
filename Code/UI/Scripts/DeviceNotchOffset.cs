using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to a gameObject with a UIAnchor component which needs to be adjusted for the Iphone X/XR/XS notch
/// so that the widget isn't hidden by the notch.
/// </summary>
[RequireComponent(typeof(UIAnchor))]
public class DeviceNotchOffset : MonoBehaviour
{
	//The location of the UI widget on the screen.	
	public enum ScreenLocation
	{
		Left = 0,
		Right,
		Top,
		Bottom
	}
	
	[SerializeField] private UIAnchor anchor;
	public ScreenLocation myLocation = ScreenLocation.Left;	
	//This used to emulate the device orientation change in the Unity Editor.
	public ScreenOrientation DebugScreenOrientation = ScreenOrientation.Unknown;

	//Save the last orientation of the device so that it is only updated when the orientation changes
	private ScreenOrientation lastOrientation;
	//The startingOffset is used to reset the position of the UI widget if the widget is no longer
	//on the same side as the device notch.
	private Vector2 startingOffset = Vector2.zero;	
	//Offset amount in pixels 
	private int offsetAmount;

	private bool firstPass = true;
	
	void Start()
	{		
		if (anchor == null)
		{
			Debug.LogError("Could not find component UIAnchor on GameObject " + gameObject.name + " - Destroying script.");
			Destroy(this);			
		}
		
		//Check if the anchor is referencing some other UIAnchor and not the one attached to this gameobject 
		if (anchor != GetComponent<UIAnchor>())
		{
			Debug.LogError("anchor is not referencing component UIAnchor attached to the GameObject " + gameObject.name + " - Destroying script.");
			Destroy(this);
		}
		
		startingOffset = anchor.pixelOffset;
		offsetAmount = getOffsetAmount();		
	}
	
	void Update()
	{						
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			DebugScreenOrientation = GetNextDebugOrientation();
		}
#endif
		
		//Check if the device orientation has changed 
		if (getScreenOrientation() != lastOrientation || firstPass)
		{
			//Reset to the starting anchor offset 
			anchor.pixelOffset = startingOffset;			
			addOffset(offsetAmount, getScreenOrientation());			
			lastOrientation = getScreenOrientation();
			firstPass = false;
		}
	}

#if UNITY_EDITOR
	private ScreenOrientation GetNextDebugOrientation()
	{
		switch (DebugScreenOrientation)
		{
			case ScreenOrientation.Unknown:
			case ScreenOrientation.PortraitUpsideDown:
				return ScreenOrientation.LandscapeRight;
			case ScreenOrientation.LandscapeRight:
				return ScreenOrientation.LandscapeLeft;
			case ScreenOrientation.LandscapeLeft:			 
				return ScreenOrientation.Portrait;
			case ScreenOrientation.Portrait:
				return ScreenOrientation.PortraitUpsideDown;
			default:
				return ScreenOrientation.LandscapeRight;				
		}
	}
#endif
	
	/// <summary>
	/// Adds the offset in the correct direction depending on the device orientation and the location of the widget
	/// on screen.
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="orientation"></param>
	private void addOffset(float offset, ScreenOrientation orientation)
	{
		if (myLocation == ScreenLocation.Left && orientation == ScreenOrientation.LandscapeLeft)
		{
			//Move right
			anchor.pixelOffset = startingOffset + new Vector2(offset, 0);
		}
		else if (myLocation == ScreenLocation.Right && orientation == ScreenOrientation.LandscapeRight)
		{
			//Move left
			anchor.pixelOffset = startingOffset - new Vector2(offset, 0);
		}
		else if (myLocation == ScreenLocation.Top && orientation == ScreenOrientation.Portrait)
		{
			//Move down
			anchor.pixelOffset = startingOffset - new Vector2(0, offset);
		}
		else if (myLocation == ScreenLocation.Bottom && orientation == ScreenOrientation.PortraitUpsideDown)
		{
			//Move up
			anchor.pixelOffset = startingOffset + new Vector2(0, offset);
		}
		
		//Call UIAchor enabled with the new offset. This will reposition the anchor
		//Directly calling reposition may cause a null ref if the anchor is disabled
		anchor.enabled = true;
	}
	/// <summary>
	/// Returns the screen orientation on mobile devices
	/// Returns DebugScreenOrientation in the editor
	/// </summary>
	/// <returns></returns>
	private ScreenOrientation getScreenOrientation()
	{
		ScreenOrientation screenOrientation = UnityEngine.Screen.orientation;
			
#if UNITY_EDITOR
		if(DebugScreenOrientation != ScreenOrientation.Unknown)
		{
			screenOrientation = DebugScreenOrientation;
		}
#endif
		return screenOrientation;
	}

	/// <summary>
	/// Calculates the offset amount using the device safe area.
	/// Returns a fixed value for testing in the editor.
	/// </summary>
	/// <returns></returns>
	private int getOffsetAmount()
	{
#if UNITY_EDITOR
		//This value is the Iphone XS max safe area offset. Use this in editor to get an idea of how the offset would look
		//In the editor the safe area is same as the screen width no matter which aspect ration is chosen.  
		return 132;  
#endif
		return (int)((UnityEngine.Screen.width - UnityEngine.Screen.safeArea.width)/2.0f);
	}
}
