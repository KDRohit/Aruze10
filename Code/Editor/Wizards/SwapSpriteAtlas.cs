using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SwapSpriteAtlas : ScriptableWizard
{
	[SerializeField] private GameObject gameObject;
	[SerializeField] private UIAtlas oldAtlas;
	[SerializeField] private UIAtlas newAtlas;
	[SerializeField] private bool doRecursive = true;
	[SerializeField] private bool onlySwapExistingSprites = false;

	[MenuItem("Zynga/Wizards/Find Stuff/Find and Swap Atlas Sprites")]
	private static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<SwapSpriteAtlas>("Find and Swap Atlas Sprites", "Close", "Swap");
	}

	private void OnWizardUpdate()
	{
		if (gameObject == null)
		{
			this.helpString = "Please select an object to find sprites with the old atlas." +
				"\nOptionally specify a new atlas to automatically swap.";
		}
		else if (oldAtlas == null)
		{
			this.helpString = "Please select an old atlas.";
		}
	}

	public void OnWizardCreate()
	{
		// Called when Close is clicked.
	}

	private void OnWizardOtherButton()
	{
		if (gameObject == null || oldAtlas == null)
		{
			EditorUtility.DisplayDialog("We have a problem.", this.helpString, "OK");
			return;
		}
		
		oldAtlas = SpriteFinder.getFinalAtlas(oldAtlas);
			
		List<UISprite> sprites = new List<UISprite>();
		if (doRecursive)
		{
			sprites.AddRange(gameObject.GetComponentsInChildren<UISprite>(true));
		}
		else
		{
			sprites.Add(gameObject.GetComponent<UISprite>());
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
						sprite.MakePixelPerfect();
						EditorUtility.SetDirty(sprite.gameObject);
						EditorUtility.SetDirty(sprite);
						fixCount++;
					}
				}
			}
		}
				
		if (newAtlas == null)
		{
			EditorUtility.DisplayDialog("Done Searching", string.Format("Found {0} sprites.", findCount), "OK");
		}
		else
		{
			EditorUtility.DisplayDialog("Done Swapping", string.Format("Found and fixed {0} sprites.", fixCount), "OK");
		}
	}
} 
