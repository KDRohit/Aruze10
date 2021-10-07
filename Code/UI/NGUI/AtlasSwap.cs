using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AtlasSwap : TICoroutineMonoBehaviour 
{	
	public GameResolution overrideForEditor = GameResolution.High;
	public bool useOverrideInEditor = false;
	public AtlasReplacement[] atlasReplacements;
	public FontReplacement[] fontReplacements;
	
	private static UIFont smallFont = null;		// Used where dynamically setting a font is needed.
	private static UIFont mediumFont = null;	// Used where dynamically setting a font is needed.
	private static UIFont largeFont = null;		// Used where dynamically setting a font is needed.
	private static UIFont titleFont = null;		// Used where dynamically setting a font is needed.
	
	public enum GameResolution
	{
		High = 0,
		Low
	};
		
	void Awake() 
	{
		GameResolution resolutionToUse = GameResolution.High;
		
		if (MobileUIUtil.shouldUseLowRes)
		{
			resolutionToUse = GameResolution.Low;
		}
		
#if UNITY_EDITOR
		// In editor, we may use a specified override.
		if (useOverrideInEditor)
		{
			resolutionToUse = overrideForEditor;
		}
#endif

		swapAtlases(resolutionToUse);
	}
	
	// This is a separate function so it can be called by editor scripts too.
	public void swapAtlases(GameResolution resolutionToUse)
	{		
		// Replace atlases.
		foreach (AtlasReplacement atlas in atlasReplacements)
		{
			UIAtlas replacement = null;
			UIAtlas toReplace = null;
			
			switch (resolutionToUse)
			{
				case GameResolution.Low:
					replacement = atlas.lowReplacement;
					toReplace = atlas.highReplacement;
					atlas.highReplacement = null;
					break;
				case GameResolution.High:
					replacement = atlas.highReplacement;
					toReplace = atlas.lowReplacement;
					atlas.lowReplacement = null;
					break;
			}
			
			if (replacement != null)
			{
				atlas.reference.replacement = replacement;
			}
			
			if (toReplace != null)
			{
				Material aMaterial = toReplace.spriteMaterial;
				if(aMaterial != null)
				{
#if !UNITY_EDITOR	
					Texture aTex = toReplace.spriteMaterial.mainTexture;
					toReplace.spriteMaterial.mainTexture = null;
					toReplace.spriteMaterial = null;
					Resources.UnloadAsset(aTex);
					Resources.UnloadAsset(aMaterial);
#endif	
				}
			}
			
#if UNITY_EDITOR
			if (replacement != null && toReplace != null)
			{
				// When in the editor, we might want to know that atlases are doing replacement
				//Debug.Log(string.Format("Replacing atlas for {0}: {1} -> {2}", gameObject.name, toReplace.gameObject.name, replacement.gameObject.name));
			}
#endif
		}

		// Replace fonts.
		foreach (FontReplacement font in fontReplacements)
		{
			switch (font.reference.name.ToLower())
			{
				case "small":
					smallFont = font.reference;
					break;
				case "medium":
					mediumFont = font.reference;
					break;
				case "large":
					largeFont = font.reference;
					break;
				case "title":
					titleFont = font.reference;
					break;
			}
			
			UIFont replacement = null;
			UIFont toReplace = null;
			
			switch (resolutionToUse)
			{
				case GameResolution.Low:
					replacement = font.lowReplacement;
					toReplace = font.highReplacement;
					font.highReplacement = null;
					break;
				case GameResolution.High:
					replacement = font.highReplacement;
					toReplace = font.lowReplacement;
					font.lowReplacement = null;
					break;
			}
			
			if (replacement != null)
			{
				font.reference.replacement = replacement;
			}
			
			if (toReplace != null)
			{
				Material aMaterial = toReplace.material;
				if (aMaterial != null)
				{
#if !UNITY_EDITOR	

					Texture aTex = toReplace.material.mainTexture;
					toReplace.material.mainTexture = null;
					toReplace.material = null;
					Resources.UnloadAsset(aTex);
					Resources.UnloadAsset(aMaterial);
#endif
				}				
			}
		}
	}
	
	[System.Serializable]
	public class AtlasReplacement
	{
		public UIAtlas reference;
		public UIAtlas highReplacement;
		public UIAtlas lowReplacement;
	}

	[System.Serializable]
	public class FontReplacement
	{
		public UIFont reference;
		public UIFont highReplacement;
		public UIFont lowReplacement;
	}
	
	// Returns a reference font of the given name, for use with things that dynamically set fonts.
	public static UIFont getFont(string name)
	{
		if (Application.isPlaying)
		{
			switch (name.ToLower())
			{
				case "small":
					return smallFont;
				case "medium":
					return mediumFont;
				case "large":
					return largeFont;
				case "title":
					return titleFont;
			}
		}
#if UNITY_EDITOR
		else
		{
			// When in edit mode, it's a bit tricker since the above references aren't defined.
			string path = string.Format("Assets/Data/Common/NGUI/Reference Fonts/{0}.prefab", Localize.toTitle(name));
			GameObject fontObj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
			if (fontObj != null)
			{
				return fontObj.GetComponent<UIFont>();
			}
		}
#endif
		return null;
	}
}
