using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(UIImageButton))]
[ExecuteInEditMode]
public class ImageButtonHandler : ClickHandler
{
	public UIImageButton imageButton;
	public UIButtonScale buttonScale;
	public UIButton button;
	public BoxCollider spriteCollider;
	public UISprite sprite;
	public TextMeshPro label;

	private bool _enabled;
	public new bool enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			imageButton.isEnabled = _enabled;
			if (button != null)
			{
				button.isEnabled = _enabled;
			}
			spriteCollider.enabled = _enabled;
			base.isEnabled = value;
		}
	}

		// Sets/Gets the TMPro label text;
	public string text
	{
		get
		{
			return (label == null) ? "": label.text;
		}
		set
		{
			SafeSet.labelText(label, value);
		}
	}

	// Supporting this GameObject call to make conversion easier.
	public void SetActive(bool isEnabled)
	{
		enabled = isEnabled;
	}
}
