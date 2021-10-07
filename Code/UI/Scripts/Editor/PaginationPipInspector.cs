using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PaginationPip))]
public class PaginationPipInspector : Editor
{
	UISprite mSprite;
	PaginationPip mPip;
	
	public override void OnInspectorGUI()
	{
		mPip = target as PaginationPip;
		mPip.clickHandler = EditorGUILayout.ObjectField("Click Handler", mPip.clickHandler, typeof(ClickHandler), true) as ClickHandler;
		mPip.type = (PaginationPip.PipType)EditorGUILayout.EnumPopup("Pip Type", mPip.type);
		switch (mPip.type)
		{
			case PaginationPip.PipType.SPRITE_SWAP:
				// Now allow them to change it if they want.
				mSprite = EditorGUILayout.ObjectField("Sprite", mPip.pipSprite, typeof(UISprite), true) as UISprite;

				if (mSprite != null)
				{
					NGUIEditorTools.SpriteField("On", mSprite.atlas, mPip.onSpriteName, onSprite);
					NGUIEditorTools.SpriteField("Off", mSprite.atlas, mPip.offSpriteName, offSprite);
				}
				mPip.pipSprite = mSprite;
				break;
			case PaginationPip.PipType.OBJECT_TOGGLE:
				mPip.onObject = EditorGUILayout.ObjectField("On Object", mPip.onObject, typeof(GameObject), true) as GameObject;
				mPip.offObject = EditorGUILayout.ObjectField("Off Object", mPip.offObject, typeof(GameObject), true) as GameObject;
				break;
		}
	}

	private void onSprite(string spriteName)
	{
		mPip.onSpriteName = spriteName;
	}
	
	private void offSprite(string spriteName)
	{
		mPip.offSpriteName = spriteName;
	}
}
