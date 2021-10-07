using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ToggleHandler))]
public class ToggleHandlerInspector : Editor
{
	UISprite mSprite;
	ToggleHandler mToggle;
	
	public override void OnInspectorGUI()
	{
		mToggle = target as ToggleHandler;
		// Now allow them to change it if they want.
		mSprite = EditorGUILayout.ObjectField("Sprite", mToggle.targetSprite, typeof(UISprite), true) as UISprite;

		if (mSprite != null)
		{
			NGUIEditorTools.SpriteField("On", mSprite.atlas, mToggle.toggleOnSprite, toggleOnSprite);
			NGUIEditorTools.SpriteField("Off", mSprite.atlas, mToggle.toggleOffSprite, toggleOffSprite);
		}

		mToggle.targetSprite = mSprite;
		mToggle.targetLabel = EditorGUILayout.ObjectField("Label", mToggle.targetLabel, typeof(TMPro.TextMeshPro), true) as TMPro.TextMeshPro;
		if (mToggle.targetLabel != null)
		{
			mToggle.stylingMethod = (ToggleHandler.TextStyling)EditorGUILayout.EnumPopup("Label Styling", mToggle.stylingMethod);
			switch (mToggle.stylingMethod)
			{
				case ToggleHandler.TextStyling.STYLE:
					mToggle.toggleOnFontStyle = EditorGUILayout.ObjectField("Toggled On Font Style", mToggle.toggleOnFontStyle, typeof(Material), true) as Material;
					mToggle.toggleOffFontStyle = EditorGUILayout.ObjectField("Toggled Off Font Style", mToggle.toggleOffFontStyle, typeof(Material), true) as Material;		
					break;
				case ToggleHandler.TextStyling.COLOR:
					mToggle.toggleOnFontColor = EditorGUILayout.ColorField("On Color", mToggle.toggleOnFontColor);
					mToggle.toggleOffFontColor = EditorGUILayout.ColorField("Off Color", mToggle.toggleOffFontColor);
					break;
			}
		}
	}

	private void toggleOnSprite(string spriteName)
	{
		mToggle.toggleOnSprite = spriteName;
	}
	
	private void toggleOffSprite(string spriteName)
	{
		mToggle.toggleOffSprite = spriteName;
	}
}