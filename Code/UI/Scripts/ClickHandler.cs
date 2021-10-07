using UnityEngine;
using System.Collections;


/*
  Class: ClickHandler
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Handles button delegates and allows you to pass in arguments to it.
*/


[RequireComponent(typeof(BoxCollider))]
public class ClickHandler : MonoBehaviour
{
	public enum MouseEvent
	{
		OnClick,
		OnMouseDown,
		OnMouseUp,
		OnDoubleClick,
		OnHold
	}

	public delegate void onClickDelegate(Dict args);
	public event onClickDelegate OnClicked;
	public MouseEvent registeredEvent = MouseEvent.OnClick;
	public AudioListController.AudioInformationList clickedSounds;

	private Dict args; // Arguments for the callback function.


	private bool hasCheckedForCollider = false;
    private BoxCollider _boxCollider;
	public BoxCollider boxCollider
	{
		get
		{
			if (_boxCollider == null && !hasCheckedForCollider)
			{
				_boxCollider = gameObject.GetComponent<BoxCollider>();
				hasCheckedForCollider = true;
			}
			return _boxCollider;
		}
		set
		{
			_boxCollider = value;
		}
	}
	
	[SerializeField]private bool _isEnabled = true;
    public bool isEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
		    boxCollider.enabled = value;
		}
	}

	// Clear all delegates AND callbacks.
	public void clearAllDelegates()
	{
		if (OnClicked == null)
		{
			// Nothing to do here.
			return;
		}
		OnClicked = null;
		args = null;
	}
	
	// Unregisters the delegate
	public void unregisterEventDelegate(onClickDelegate callback, bool removeAll = true)
	{
		if (removeAll)
		{
			// If we want to remove everything then just clear all.
			clearAllDelegates();
		}
		else
		{
			// No real way to know what args are associated with this particular callback so we will just merge them.
			OnClicked -= callback;
		}
	}

	// Wrapper function to make it nicer to look at to show/hide.
	public void show(bool shouldShow)
	{
		gameObject.SetActive(shouldShow);
	}
	
	// Removed EVERYTHING from the delegate
	public void clearDelegate()
	{
		OnClicked = null;
	}

	// Register the callback delegate, as well as the arguments you want passed to it.
	public void registerEventDelegate(onClickDelegate callback, Dict callbackArgs = null)
	{
		if (args != null)
		{
			if (!args.merge(callbackArgs))
			{
				// If there is already an existing argument list, then merge the two.
				Debug.LogErrorFormat("ButtonHandler.cs -- registerEventDelegate -- failed to merge arguments.");
			}
		}
		else
		{
			// Otherwise, use the new one.
			args = callbackArgs;
		}

		OnClicked -= callback;
		OnClicked += callback;

	}

	public void handleMouseEvent(MouseEvent mouseEvent)
	{
		if (this == null || gameObject == null)
		{
			return;
		}
		
		if (mouseEvent == registeredEvent && OnClicked != null)
		{
			// Set the calling object.
			if (args == null)
			{
				args = Dict.create(D.CALLING_OBJECT, gameObject);
			}
			else
			{
				args[D.CALLING_OBJECT] = gameObject;
			}
			
			if (isEnabled)
			{
				// Allow us to disable the callbacks
				StartCoroutine(AudioListController.playListOfAudioInformation(clickedSounds));
				OnClicked(args);
			}
		}
	}

	public void OnClick()
	{
		handleMouseEvent(MouseEvent.OnClick);
	}

	public virtual void OnPress(bool isPressed)
	{
		if (isPressed)
		{
			handleMouseEvent(MouseEvent.OnMouseDown);
		}
		else
		{
			handleMouseEvent(MouseEvent.OnMouseUp);
		}
	}

	public void OnDoubleClick()
	{
		handleMouseEvent(MouseEvent.OnDoubleClick);
	}

	public void OnDestroy()
	{
		clearAllDelegates();
	}	
}
