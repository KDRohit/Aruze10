using UnityEngine;

/*
  Class: AnimatedButton
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: A simple class to setup a button to our coding standards. Adds the required scripts and
  sets up their values properly. If there is more than one sprite beneath the parent object you put this on, 
  you may need to change that link manually.

*/
[RequireComponent(typeof(UIButtonScale))]
[RequireComponent(typeof(UIButton))]
[RequireComponent(typeof(UIButtonMessage))]
[RequireComponent(typeof(BoxCollider))]
[ExecuteInEditMode]
public class AnimatedButton : MonoBehaviour
{
	public bool resetNow = false;
	public bool relinkNow = false;
	public UIButtonScale buttonScale;
	public UIButton button;
	public BoxCollider spriteCollider;
	public UISprite sprite;
	
	void Awake()
	{
		if (!Application.isPlaying)
		{
			setupLinks();
			setToDefaults();
		}
	}

	private void setupLinks()
	{
		buttonScale = GetComponent<UIButtonScale>();
		button = GetComponent<UIButton>();
		spriteCollider = GetComponent<BoxCollider>();
		sprite = GetComponent<UISprite>();
		if (sprite == null)
		{
			// if this isnt all on one gameobject, then find a child sprite.
			sprite = GetComponentInChildren<UISprite>();
			if (sprite == null)
			{
				Debug.LogError("AnimatedButton -- You have no sprite in the hierarchy, please fix this.");
			}
		}
	}
	
	private void setToDefaults()
	{	
		buttonScale.duration = 0.05f;
		buttonScale.pressed = new Vector3(0.95f, 0.95f, 0.95f);
		buttonScale.hover = Vector3.one;

		button.hover = Color.white;
		float rgbValue = 153f/255f;
		button.pressed = new Color(rgbValue, rgbValue, rgbValue, 1f);
		button.duration = 0.05f;

		if (sprite != null)
		{
			// if we found a child, then use this to size the collider.
			Vector3 spriteSize = sprite.transform.localScale;
			Vector3 colliderSize = new Vector3(spriteSize.x * 1.5f, spriteSize.y * 1.5f, 1);
			spriteCollider.size = colliderSize;

			// If we have a child sprite then use it for the other elements as well.
			button.tweenTarget = sprite.gameObject;
			buttonScale.tweenTarget = sprite.transform;
		}
		else
		{
			button.tweenTarget = gameObject;
			buttonScale.tweenTarget = transform;
			spriteCollider.size = Vector3.one;
		}
	}
	
	void Update()
	{
		if (!Application.isPlaying && resetNow)
		{
			setToDefaults();
			resetNow = false;
		}
		if (!Application.isPlaying && relinkNow)
		{
			setupLinks();
			relinkNow = false;
		}
	}
}