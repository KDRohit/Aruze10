using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Oz01BasketButton : TICoroutineMonoBehaviour
{
	public oz01FreeSpins parent;
	public int id;

	public UISprite background;
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	
	public UISprite shine;

	public bool beenPicked;

	public UIButton button;
	public TweenColor shineElementColor;

	// Use this for initialization
	void Awake() 
	{
		labelWrapper.text = "";
		doBasketShine(false);
		toggleButton(false);
	}

	void OnClick() 
	{
		if (beenPicked)
		{
			return;
		}
		beenPicked = true;
		parent.basketPicked(id);
		resetShine();
	}
	
	public void showButton()
	{
		labelWrapper.gameObject.SetActive(true);
		if (beenPicked)
		{
			return;
		}
		TweenColor.Begin(background.gameObject, 0.25f, new Color(1, 1, 1, 1));
		shine.enabled = true;
	}

	public void hideButton() 
	{
		TweenColor.Begin(background.gameObject, 0.25f, new Color(1,1,1,0));
		shine.enabled = false;
		labelWrapper.gameObject.SetActive(false);
	}

	/// Reveal this unpicked basket's random fake prize,
	/// to show the player what he could not have ever possibly won.
	public void reveal(string text)
	{
		hideButton();
		labelWrapper.gameObject.SetActive(true);
		labelWrapper.text = text;
		labelWrapper.color = Color.gray;
		Audio.play("totoarf");
	}

	public void toggleButton(bool canClick) 
	{
		button.isEnabled = canClick && !beenPicked;
	}

	public void setParent(oz01FreeSpins parent, int id) 
	{
		this.parent = parent;
		this.id = id;
	}

	public void doBasketShine(bool isEnabled)
	{
		if (beenPicked)
		{
			return;
		}
		shine.enabled = isEnabled;
		shineElementColor.enabled = isEnabled;
	}

	private void resetShine() 
	{
		Color resetColor = shine.color;
		resetColor.a = 0;
	}
}
