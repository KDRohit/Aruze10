using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaginationPip : MonoBehaviour
{
	public enum PipType
	{
		SPRITE_SWAP,
		OBJECT_TOGGLE
	}

	public PipType type = PipType.OBJECT_TOGGLE; // Default to Object Toggle.
	
	// Sprite Sweapping
	public UISprite pipSprite;
	public string onSpriteName;
	public string offSpriteName;

	// Object Toggling
	public GameObject onObject;
	public GameObject offObject;

	// To Allow clicking register a click handler
	public ClickHandler clickHandler;
	private PaginationController controller;
	
	public void init(int index, PaginationController controller)
	{
		this.controller = controller;
		if (clickHandler != null)
		{
			clickHandler.registerEventDelegate(onPipClicked, Dict.create(D.INDEX, index));
		}
	}
	
	public void toggle(bool isOn)
	{
		switch (type)
		{
			case PipType.SPRITE_SWAP:
				pipSprite.spriteName = isOn ? onSpriteName : offSpriteName;
				break;
			case PipType.OBJECT_TOGGLE:
			default: // Make object toggle the pip type since it can work without names.
				onObject.SetActive(isOn);
				offObject.SetActive(!isOn);
				break;
		}
	}

	private void onPipClicked(Dict args)
	{
		if (controller != null)
		{
			int index = (int)args.getWithDefault(D.INDEX, 0);
			controller.onPipClicked(index);
		}
	}
}
