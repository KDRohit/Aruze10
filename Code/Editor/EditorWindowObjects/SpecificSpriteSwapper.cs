using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/*
	Class Name: SpecificSpriteSwapper.cs
	Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
	Description: An editor window script for swapping sprites on an object between two different atlases (rather than having the same name and swapping just the atlas component). Designing this is such a way as as it can be called from other EditorWindows (in case we want to make a comprehensive sprite finder/swapper/etcc script)
*/
public class SpecificSpriteSwapper : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/SpecificSpriteSwapper")]
	public static void openSpecificSpriteSwapper()
	{
		SpecificSpriteSwapper spriteSwapper = (SpecificSpriteSwapper)EditorWindow.GetWindow(typeof(SpecificSpriteSwapper));
		spriteSwapper.Show();
	}

	private SpriteSwapperObject swapperObject;
	public void OnGUI()
	{
		if (swapperObject == null)
		{
			swapperObject = new SpriteSwapperObject();
		}
		swapperObject.drawGUI(position);
	}
}

public class SpriteSwapperObject : EditorWindowObject
{
	public UIAtlas sourceAtlas;
	public UIAtlas populatedSourceAtlas;
	public string[] sourceSpriteNameArray;
	public int selectedSourceSpriteIndex = 0;

	public UIAtlas targetAtlas;
	public UIAtlas populatedTargetAtlas;
	public string[] targetSpriteNameArray;
	public int selectedTargetSpriteIndex = 0;

	public GameObject targetObject;

	protected override string getButtonLabel()
	{
		return "Sprite Swapper";
	}

	protected override string getDescriptionLabel()
	{
		return "Very similar to the normal atlas swapper, but you can manually set which sprites you which to swap from a dropdown.";
	}
	
	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();
		targetObject = EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), allowSceneObjects:true) as GameObject;
		sourceAtlas = EditorGUILayout.ObjectField("Source Atlas", sourceAtlas, typeof(UIAtlas), allowSceneObjects:false) as UIAtlas;
		if (sourceAtlas != null )
		{
			if (sourceAtlas != populatedSourceAtlas)
			{
				// Populate the string enum dropdown.
				populatedSourceAtlas = sourceAtlas;
				sourceSpriteNameArray = sourceAtlas.GetListOfSprites().ToArray();
			}

			selectedSourceSpriteIndex = EditorGUILayout.Popup("Sprites: ", selectedSourceSpriteIndex, sourceSpriteNameArray);
		}

		targetAtlas = EditorGUILayout.ObjectField("Target Atlas", targetAtlas, typeof(UIAtlas), allowSceneObjects:false) as UIAtlas;
		if (targetAtlas != null )
		{
			if (targetAtlas != populatedTargetAtlas)
			{
				// Populate the string enum dropdown.
				populatedTargetAtlas = targetAtlas;
				targetSpriteNameArray = targetAtlas.GetListOfSprites().ToArray();
			}

			selectedTargetSpriteIndex = EditorGUILayout.Popup("Sprites: ", selectedTargetSpriteIndex, targetSpriteNameArray);
		}

		if (GUILayout.Button("Swap Sprites!"))
		{
			swapSprites(sourceAtlas, sourceSpriteNameArray[selectedSourceSpriteIndex], targetAtlas, targetSpriteNameArray[selectedTargetSpriteIndex], targetObject);
		}
		GUILayout.EndVertical();
	}

	// Making this a standalone-function for ease of copying it around.
	public void swapSprites(UIAtlas sourceAtlas, string sourceSpriteName, UIAtlas targetAtlas, string targetSpriteName, GameObject parentObject)
	{
		if (parentObject == null)
		{
			Debug.LogErrorFormat("SpecificSpriteSwapper.cs -- swapSprites -- called swapSprites with a null target gameobject!");
			return;
		}
		List<UISprite> spritesToSwap = new List<UISprite>(parentObject.GetComponentsInChildren<UISprite>());
		for (int i = 0; i < spritesToSwap.Count; i++)
		{
			UISprite sprite = spritesToSwap[i];
			if (sprite.atlas == sourceAtlas && sprite.name == sourceSpriteName)
			{
				// Then Swap to the new atlas/sprite
				sprite.atlas = targetAtlas;
				sprite.name = targetSpriteName;

				Debug.LogFormat("SpecificSpriteSwapper.cs -- swapSprites -- Swapping the sprite on object: ", sprite.gameObject.name);
			}
		}
	}
}