using UnityEngine;
using System.Collections;
using TMPro;

public class TabSelector : MonoBehaviour
{

	public enum SelectionType
	{
		SwapSprite,
		ToggleSprite,
		ToggleDifferentSprites
	}

	public SelectionType selectionType;
	public UISprite targetSprite;
	public UISprite targetOffSprite;
	public TextMeshPro targetLabel;
	public GameObject content;
	public Animator animator;

	// Swap Sprite variables
	public string selectedSprite = "";
	public string unselectedSprite = "";

	// ToggleSprite Variables
	public bool showWhenSelected = false;
	public ClickHandler clickHandler;

	// Animator Variable
	public bool shouldControlAnimation;
	public string animationString = "";

	// Font Variables
	public enum TextStyling
	{
		STYLE,
		COLOR,
		HIDE,
		NONE
	}

	public TextStyling stylingMethod;
	public Material selectedFontStyle;
	public Material unselectedFontStyle;
	public Color selectedFontColor;
	public Color unselectedFontColor;

	// Private variables
	private bool _isSelected = false;
	public int index = 0;
	private TabManager manager;

	public bool selected
	{
		set
		{
			_isSelected = value;
			if (_isSelected)
			{
				if (animator == null)
				{
					SafeSet.gameObjectActive(content, true);
				}
				else if (shouldControlAnimation)
				{
					animator.Play(animationString);
				}
			}
			else
			{
				if (animator == null)
				{
					SafeSet.gameObjectActive(content, false);
				}
			}
			handleSpriteSelection();
			handleLabelToggling();
		}
		get
		{
			return _isSelected;
		}
	}

	private void handleSpriteSelection()
	{
		if (_isSelected)
		{
			switch (selectionType)
			{
			case SelectionType.SwapSprite:
				// Do selection stuff.
				if (!string.IsNullOrEmpty(selectedSprite))
				{
					targetSprite.spriteName = selectedSprite;
				}
				break;
			case SelectionType.ToggleSprite:
				targetSprite.gameObject.SetActive(showWhenSelected);
				break;
		    case SelectionType.ToggleDifferentSprites:
				targetSprite.gameObject.SetActive(true);
				targetOffSprite.gameObject.SetActive(false);
				break;
			default:
				// Do nothing.
				break;
			}
		}
		else
		{
			switch (selectionType)
			{
				case SelectionType.SwapSprite:
					// Do unselection stuff.
					if (!string.IsNullOrEmpty(unselectedSprite))
					{
						targetSprite.spriteName = unselectedSprite;
					}
				break;
				case SelectionType.ToggleSprite:
					targetSprite.gameObject.SetActive(!showWhenSelected);
					break;
				case SelectionType.ToggleDifferentSprites:
					targetSprite.gameObject.SetActive(false);
					targetOffSprite.gameObject.SetActive(true);
					break;
				default:
					// Do nothing.
					break;
			}
		}
	}

	private void handleLabelToggling()
	{
		if (targetLabel == null)
		{
			return;
		}
		switch (stylingMethod)
		{
			case TextStyling.STYLE:
				
				if (_isSelected && selectedFontStyle != null)
				{
					targetLabel.fontMaterial = selectedFontStyle;
				}
				else if (!_isSelected && unselectedFontStyle != null)
				{
					targetLabel.fontMaterial = unselectedFontStyle;
				}
				break;
			case TextStyling.COLOR:
				targetLabel.color = _isSelected ? selectedFontColor : unselectedFontColor;
				break;
			case TextStyling.HIDE:
				targetLabel.gameObject.SetActive(_isSelected);
				break;
		}
	}

	public void init(int index, TabManager manager)
	{
		this.index = index;
		clickHandler.registerEventDelegate(click);
		this.manager = manager;
	}

	public void click(Dict args = null)
	{
		if (args == null)
		{
			Debug.LogErrorFormat("TabSelector.cs -- click -- args were null, something went wrong.");
			manager.selectTab(null);
		}
		manager.selectTab(this);
	}
}
