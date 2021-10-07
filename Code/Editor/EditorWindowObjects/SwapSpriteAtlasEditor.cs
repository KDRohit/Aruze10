using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SwapSpriteAtlasEditor : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/SwapSpriteAtlasEditor")]
	public static void openSwapSpriteAtlasEditor()
	{
		SwapSpriteAtlasEditor swapAtlas = (SwapSpriteAtlasEditor)EditorWindow.GetWindow(typeof(SwapSpriteAtlasEditor));
		swapAtlas.Show();
	}

	private SwapSpriteAtlasEditorObject swapAtlasObject;
	public void OnGUI()
	{
		if (swapAtlasObject == null)
		{
			swapAtlasObject = new SwapSpriteAtlasEditorObject();
		}
		swapAtlasObject.drawGUI(position);
	}
}


public class SwapSpriteAtlasEditorObject : EditorWindowObject
{
	private GameObject targetObject;
	private UIAtlas oldAtlas;
	private UIAtlas newAtlas;
	private bool doRecursive = true;
	private bool onlySwapExistingSprites = false;

	protected override string getButtonLabel()
	{
		return "Swap Atlases";
	}

	protected override string getDescriptionLabel()
	{
		return "Finds sprites that use the Old Atlas in the given target Object. If you set the New Atlas variable then it will swap the sprites to be from the new atlas. You can choose to go through all the child elements, as well as whether you want to only swap sprites that exist in the new atlas and leave any others still pointing to the old atlas.";
	}
		
	public override void drawGuts(Rect position)
	{
			
		targetObject = EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true) as GameObject;
		oldAtlas = EditorGUILayout.ObjectField("Old Atlas", oldAtlas, typeof(UIAtlas), true) as UIAtlas;
		newAtlas = EditorGUILayout.ObjectField("New Atlas", newAtlas, typeof(UIAtlas), true) as UIAtlas;

		doRecursive = EditorGUILayout.Toggle("Do Recursive?", doRecursive);
		onlySwapExistingSprites = EditorGUILayout.Toggle("Only Swap Existing?", onlySwapExistingSprites);

		if (GUILayout.Button("Swap Sprites!"))
		{
			swapSprites();
		}
	}

	private void swapSprites()
	{
		if (targetObject == null)
		{
			Debug.LogErrorFormat("SwapSpriteAtlasEditor.cs -- swapSprites -- targetObject was null, aborting");
			return;
		}

		if (oldAtlas == null)
		{
			Debug.LogErrorFormat("SwapSpriteAtlasEditor.cs -- swapSprites -- oldAtlas was null, aborting");
			return;
		}
		
		oldAtlas = SpriteFinder.getFinalAtlas(oldAtlas);
			
		List<UISprite> sprites = new List<UISprite>();
		if (doRecursive)
		{
			sprites.AddRange(targetObject.GetComponentsInChildren<UISprite>(true));
		}
		else
		{
			sprites.Add(targetObject.GetComponent<UISprite>());
		}

		int findCount = 0;
		int fixCount = 0;

		List<string> newAtlasSprites = new List<string>();
		if (newAtlas != null)
		{
			List<UIAtlas.Sprite> atlasSprites = newAtlas.spriteList;
			for (int i = 0; i < atlasSprites.Count; i++)
			{
				newAtlasSprites.Add(atlasSprites[i].name);
			}
		}
		newAtlasSprites.Sort();
		
		for (int i = 0; i < sprites.Count; i++)
		{
			UISprite sprite = sprites[i];
			if (SpriteFinder.getFinalAtlas(sprite.atlas) == oldAtlas)
			{
				findCount++;
				if (newAtlas != null)
				{
					bool shouldSwap = onlySwapExistingSprites ? newAtlasSprites.Contains(sprite.spriteName) : true;
					if (shouldSwap)
					{
						// If we aren't checking for existing sprites before switching, or if we are and it exists in both, then swap them.
						sprite.atlas = newAtlas;
						EditorUtility.SetDirty(sprite.gameObject);
						EditorUtility.SetDirty(sprite);
						fixCount++;
					}
				}
			}
		}
				
		if (newAtlas == null)
		{
			Debug.LogFormat("SwapSpriteAtlasEditor.cs -- swapSprites -- Found {0} sprites.", findCount);
		}
		else
		{
			Debug.LogFormat("SwapSpriteAtlasEditor.cs -- swapSprites -- Found and fixed {0} sprites.", fixCount);
		}
	}
}
