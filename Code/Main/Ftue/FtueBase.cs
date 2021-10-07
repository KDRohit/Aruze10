using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class FtueBase: MonoBehaviour
{
	// Shroud that covers the entire game
	public GameObject shroud;

	// Arrow that points to an element
	public UISprite arrow;

	//Text that appears for the FTUE
	public TextMeshPro ftueText;

	// Button to skip or show someother part of the ftue
	public ImageButtonHandler buttonHandler;

	// Tab the needs to be clicked
	public ClickHandler ftueTabClick;

	protected GenericDelegate _shroudDelegate = null;

	public virtual void Awake()
	{
		Initialize();
	}

	public virtual void Start()
	{
	}
		
	/// <summary>
	/// Method to check where the shroud is active or not
	/// </summary>
	/// <returns>bool value 
	/// true: shroud active
	/// false: shroud inactive
	/// </returns>
	public bool IsShroudActive()
	{
		return Overlay.instance != null && Overlay.instance.shroud != null && Overlay.instance.shroud.activeSelf;
	}

	/// <summary>
	/// Show the shroud. Shroud is a layer that blocks out all the UI elements
	/// </summary>
	/// <param name="clickDelegate"></param>
	/// <param name="z"></param>
	public void ShowShroud(GenericDelegate clickDelegate, float z = -1100f)
	{
		Vector3 pos = shroud.transform.localPosition;
		shroud.transform.localPosition = new Vector3(pos.x, pos.y, z);
		shroud.SetActive(true);
		_shroudDelegate = clickDelegate;
	}

	/// <summary>
	/// This method hides the shroud. If the delegate is not null then it executes what ever the delegate is.
	/// </summary>
	public void HideShroud()
	{
		if (_shroudDelegate != null)
		{
			_shroudDelegate();
		}

		_shroudDelegate = null;
		if (shroud != null)
		{
			// Need to nullcheck this since this function is called without knowing whether a shroud actually exists.
			// It doesn't exist on the old Overlay, which has the VIP button instead of Charms.
			shroud.SetActive(false);
		}
	}

	/// <summary>
	/// Callback for what happens when you click the Ftue Tab
	/// </summary>
	/// <param name="args"></param>
	public virtual void TabClick(Dict args = null)
	{
		Debug.Log("====GIRISH:Tab click===");
	}

	public virtual void positionFtue()
	{

	}


	/// <summary>
	/// The skip button click.
	/// </summary>
	/// <param name="args">Arguments.</param>
	public virtual void ButtonClick(Dict args = null)
	{
		Debug.Log("=====GIRISH:Skip button clicked");
	}


	/// <summary>
	/// Initialize all the values
	/// </summary>
	public virtual void Initialize()
	{
		// Initializing the click handlers
		if (ftueTabClick != null) 
		{
			ftueTabClick.registerEventDelegate (TabClick);
		}
		if (buttonHandler != null) 
		{
			buttonHandler.registerEventDelegate (ButtonClick);
		}
	}

}

