using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/*
Class: MultiClickHandler
Author: Mike Murphy <micmurphy@zynga.com>
Handles button delegates for multiple events and allows you to pass in arguments to it.
*/


[RequireComponent(typeof(BoxCollider))]
public class MultiClickHandler : MonoBehaviour 
{

	public float holdTime = 1.0f;

	public delegate void onClickDelegate(Dict args);
	public event onClickDelegate OnClicked;
	public event onClickDelegate OnMouseDown;
	public event onClickDelegate OnMouseUp;
	public event onClickDelegate OnDoubleClicked;
	public event onClickDelegate OnHold;


	private Dictionary<ClickHandler.MouseEvent, Dict> args; // Arguments for the callback function.


	private bool isHeld = false;
	private float timeHeld = 0.0f;
	private bool hasCheckedForCollider = false;
	private BoxCollider _boxCollider;
	private BoxCollider boxCollider
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
	public void clearAllDelegates(ClickHandler.MouseEvent mouseEvent)
	{
		if (args.ContainsKey(mouseEvent))
		{
			args[mouseEvent] = null;
		}

		switch(mouseEvent)
		{
			case ClickHandler.MouseEvent.OnClick:
				if (OnClicked == null)
				{
					// Nothing to do here.
					return;
				}
				OnClicked = null;
				break;

			case ClickHandler.MouseEvent.OnMouseDown:
				if (OnMouseDown == null)
				{
					// Nothing to do here.
					return;
				}
				OnMouseDown = null;
				break;

			case ClickHandler.MouseEvent.OnMouseUp:
				if (OnMouseUp == null)
				{
					return;
				}
				OnMouseUp = null;
				break;

			case ClickHandler.MouseEvent.OnDoubleClick:
				if (OnDoubleClicked == null)
				{
					return;
				}
				OnDoubleClicked = null;
				break;

			case ClickHandler.MouseEvent.OnHold:
				if (OnHold == null)
				{
					return;
				}
				OnHold = null;
				break;
		}

		



	}

	// Unregisters the delegate
	public void unregisterEventDelegate(ClickHandler.MouseEvent mouseEvent, onClickDelegate callback, bool removeAll = true)
	{
		if (removeAll)
		{
			// If we want to remove everything then just clear all.
			clearAllDelegates(mouseEvent);
		}
		else
		{
			switch(mouseEvent)
			{
				case ClickHandler.MouseEvent.OnClick:
					OnClicked -= callback;
					break;

				case ClickHandler.MouseEvent.OnMouseDown:
					OnMouseDown -= callback;
					break;

				case ClickHandler.MouseEvent.OnMouseUp:
					OnMouseUp -= callback;
					break;

				case ClickHandler.MouseEvent.OnDoubleClick:
					OnDoubleClicked -= callback;
					break;

				case ClickHandler.MouseEvent.OnHold:
					OnHold -= callback;
					break;
			}
			// No real way to know what args are associated with this particular callback so we will just merge them.

		}
	}

	// Wrapper function to make it nicer to look at to show/hide.
	public void show(bool shouldShow)
	{
		gameObject.SetActive(shouldShow);
	}


	// Register the callback delegate, as well as the arguments you want passed to it.
	public void registerEventDelegate(ClickHandler.MouseEvent mouseEvent, onClickDelegate callback, Dict callbackArgs = null)
	{
		if (args == null)
		{
			args = new Dictionary<ClickHandler.MouseEvent, Dict>();
		}
		if (args.ContainsKey(mouseEvent) && args[mouseEvent] != null)
		{
			if (!args[mouseEvent].merge(callbackArgs))
			{
				// If there is already an existing argument list, then merge the two.
				Debug.LogErrorFormat("ButtonHandler.cs -- registerEventDelegate -- failed to merge arguments.");
			}
		}
		else
		{
			// Otherwise, use the new one.
			args[mouseEvent] = callbackArgs;
		}

		switch(mouseEvent)
		{
			case ClickHandler.MouseEvent.OnClick:
				OnClicked -= callback;
				OnClicked += callback;
				break;

			case ClickHandler.MouseEvent.OnMouseDown:
				OnMouseDown -= callback;
				OnMouseDown += callback;
				break;

			case ClickHandler.MouseEvent.OnMouseUp:
				OnMouseUp -= callback;
				OnMouseUp += callback;
				break;

			case ClickHandler.MouseEvent.OnDoubleClick:
				OnDoubleClicked -= callback;
				OnDoubleClicked += callback;
				break;

			case ClickHandler.MouseEvent.OnHold:
				OnHold -= callback;
				OnHold += callback;
				break;
		}

	}

	public void handleMouseEvent(ClickHandler.MouseEvent mouseEvent)
	{
		if (!isEnabled)
		{
			return;
		}

		switch(mouseEvent)
		{
			case ClickHandler.MouseEvent.OnClick:
				if (OnClicked != null)
				{
					OnClicked(getArgsForEvent(mouseEvent));
				}
				break;

			case ClickHandler.MouseEvent.OnMouseDown:
				if (OnMouseDown != null)
				{
					OnMouseDown(getArgsForEvent(mouseEvent));
				}
				break;

			case ClickHandler.MouseEvent.OnMouseUp:
				if(OnMouseUp != null)
				{
					OnMouseUp(getArgsForEvent(mouseEvent));
				}
				break;

			case ClickHandler.MouseEvent.OnDoubleClick:
				if (OnDoubleClicked != null)
				{
					OnDoubleClicked(getArgsForEvent(mouseEvent));
				}
				break;

			case ClickHandler.MouseEvent.OnHold:
				if (OnHold != null)
				{
					OnHold(getArgsForEvent(mouseEvent));
				}
				break;
		}
	}

	private Dict getArgsForEvent(ClickHandler.MouseEvent mouseEvent)
	{
		if (args == null)
		{
			args = new Dictionary<ClickHandler.MouseEvent, Dict>();
		}
		// Set the calling object.
		if (!args.ContainsKey(mouseEvent) || args[mouseEvent] == null)
		{
			args[mouseEvent] = Dict.create(D.CALLING_OBJECT, gameObject);
		}
		else
		{
			args[mouseEvent][D.CALLING_OBJECT] = gameObject;
		}

		return args[mouseEvent];
	}

	public void OnClick()
	{
		handleMouseEvent(ClickHandler.MouseEvent.OnClick);
	}

	public void OnPress(bool isPressed)
	{
		if (isPressed)
		{
			timeHeld = 0.0f;
			isHeld = true;
			handleMouseEvent(ClickHandler.MouseEvent.OnMouseDown);
		}
		else
		{
			isHeld = false;
			handleMouseEvent(ClickHandler.MouseEvent.OnMouseUp);
		}
	}

	public void OnDoubleClick()
	{
		handleMouseEvent(ClickHandler.MouseEvent.OnDoubleClick);
	}

	public void OnDestroy()
	{
		if (Application.isPlaying) 
		{
			if (OnClicked != null)
			{
				// Cleanup the handler and remove any remaining delegates
				cleanUpDelegates(OnClicked.GetInvocationList());
			}

			if (OnMouseDown != null)
			{
				cleanUpDelegates(OnMouseDown.GetInvocationList());
			}

			if (OnMouseUp != null)
			{
				cleanUpDelegates(OnMouseUp.GetInvocationList());
			}

			if (OnDoubleClicked != null)
			{
				cleanUpDelegates(OnDoubleClicked.GetInvocationList());
			}

			if (OnHold != null)
			{
				cleanUpDelegates(OnHold.GetInvocationList());
			}

		}		
	}

	private void cleanUpDelegates(System.Delegate[] delegates)
	{
		for (int i = 0; i < delegates.Length; i++)
		{
			System.Delegate.RemoveAll(OnClicked, delegates[i]);
		}
	}

	private void Update()
	{
		if (isHeld)
		{
			if (timeHeld >= holdTime)
			{
				isHeld = false;
				handleMouseEvent(ClickHandler.MouseEvent.OnHold);
			}
			else
			{
				timeHeld += Time.deltaTime;
			}
		}
	}
}
