using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TabSelector))]
public class TabSelectorInspector : Editor
{
	UISprite mSprite;
	TabSelector mTabSelector;
	
	public override void OnInspectorGUI()
	{
		mTabSelector = target as TabSelector;
		mSprite = EditorGUILayout.ObjectField("Sprite", mTabSelector.targetSprite, typeof(UISprite), true) as UISprite;
		mTabSelector.targetSprite = mSprite;
		mTabSelector.selectionType = (TabSelector.SelectionType)EditorGUILayout.EnumPopup("Selection Type", mTabSelector.selectionType);
		
		if (mSprite != null)
		{
			switch (mTabSelector.selectionType)
			{
			case TabSelector.SelectionType.SwapSprite:
				NGUIEditorTools.SpriteField("Selected", mSprite.atlas, mTabSelector.selectedSprite, OnSelected);
				NGUIEditorTools.SpriteField("Unselected", mSprite.atlas, mTabSelector.unselectedSprite, OnUnselected);
				break;
			case TabSelector.SelectionType.ToggleSprite:
				mTabSelector.showWhenSelected = GUILayout.Toggle(mTabSelector.showWhenSelected, "Show When Selected?");
				break;
				case TabSelector.SelectionType.ToggleDifferentSprites:
					mTabSelector.targetOffSprite = EditorGUILayout.ObjectField("Off Sprite", mTabSelector.targetOffSprite, typeof(UISprite), true) as UISprite;
					break;
			default:
				// Do nothing.
				break;
			}
		}
		

		mTabSelector.targetLabel = EditorGUILayout.ObjectField("Label", mTabSelector.targetLabel, typeof(TMPro.TextMeshPro), true) as TMPro.TextMeshPro;
		if (mTabSelector.targetLabel != null)
		{
			mTabSelector.stylingMethod = (TabSelector.TextStyling)EditorGUILayout.EnumPopup("Label Styling", mTabSelector.stylingMethod);
			switch (mTabSelector.stylingMethod)
			{
				case TabSelector.TextStyling.STYLE:
					mTabSelector.selectedFontStyle = EditorGUILayout.ObjectField("Toggled On Font Style", mTabSelector.selectedFontStyle, typeof(Material), true) as Material;
					mTabSelector.unselectedFontStyle = EditorGUILayout.ObjectField("Toggled Off Font Style", mTabSelector.unselectedFontStyle, typeof(Material), true) as Material;		
					break;
				case TabSelector.TextStyling.COLOR:
					mTabSelector.selectedFontColor = EditorGUILayout.ColorField("On Color", mTabSelector.selectedFontColor);
					mTabSelector.unselectedFontColor = EditorGUILayout.ColorField("Off Color", mTabSelector.unselectedFontColor);
					break;
			}
		}

		mTabSelector.content = EditorGUILayout.ObjectField("Content", mTabSelector.content, typeof(GameObject), true) as GameObject;
		mTabSelector.clickHandler = EditorGUILayout.ObjectField("clickHandler", mTabSelector.clickHandler, typeof(ClickHandler), true) as ClickHandler;

		mTabSelector.animator = EditorGUILayout.ObjectField("Animtor", mTabSelector.animator, typeof(Animator), true) as Animator;
		mTabSelector.shouldControlAnimation = EditorGUILayout.Toggle("Control Animation?", mTabSelector.shouldControlAnimation);
		if (mTabSelector.shouldControlAnimation)
		{
			mTabSelector.animationString = EditorGUILayout.TextField("Animation Name", mTabSelector.animationString);
		}
	}

	void OnSelected (string spriteName)
	{
		mTabSelector.selectedSprite = spriteName;
	}

	void OnUnselected (string spriteName)
	{
		mTabSelector.unselectedSprite = spriteName;
	}
}
