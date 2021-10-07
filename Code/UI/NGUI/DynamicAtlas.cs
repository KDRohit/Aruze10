using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// Class for the creation and handling of a dynamic NGUI Atlas.

public class DynamicAtlas
{
	// A cached atlas, useful for if you need to cache one and instantiate it later
	public UIAtlas atlasHandle;

	// prefabToAttachTo is ideally something that'll get cleaned up with whatever assets are going to use this atlas
	public UIAtlas createAtlas(Texture2D[] textures, GameObject prefabToAttachTo, int maxAtlasSize = 2048, string atlasTextureName = "") 	{
		UIAtlas atlasToMake = prefabToAttachTo.AddComponent<UIAtlas>();
 		Dictionary<string, Texture2D> texDic = new Dictionary<string, Texture2D>();
		Rect[] coords;
		Texture2D newAtlasTexture = new Texture2D(1, 1);
		if (!atlasTextureName.IsNullOrWhiteSpace())
		{
			newAtlasTexture.name = atlasTextureName; //Used for debugging purposes
		}
		coords = newAtlasTexture.PackTextures(textures, 1, maxAtlasSize, true);
		atlasToMake.spriteMaterial = new Material(ShaderCache.find("Unlit/Transparent Colored"));
		atlasToMake.spriteMaterial.SetTexture("_MainTex", newAtlasTexture);
		atlasToMake.spriteList = new List<UIAtlas.Sprite>();
		Texture2D tex;

		for (int i = 0; i < textures.Length; i++)
		{
			tex = textures[i];
			if (!texDic.ContainsKey(tex.name))
			{
				texDic.Add(tex.name, tex);
				Rect coordinate = coords[i];
				UIAtlas.Sprite sprite = new UIAtlas.Sprite { name = tex.name, inner = coordinate, outer = coordinate };
				atlasToMake.spriteList.Add(sprite);
			}
		}

		return atlasToMake; 	}

	public static UIAtlas createAndAttachAtlas(Texture2D[] textures, GameObject prefabToAttachTo, string atlasName = "", int maxTextureSize = 1024)
	{
		UIAtlas atlasToMake = prefabToAttachTo.AddComponent<UIAtlas>();

		Dictionary<string, Texture2D> texDic = new Dictionary<string, Texture2D>();
		Rect[] coords;
		Texture2D newAtlasTexture = new Texture2D(1, 1);
		if (!atlasName.IsNullOrWhiteSpace())
		{
			newAtlasTexture.name = atlasName; //Used for debugging purposes
		}
		coords = newAtlasTexture.PackTextures(textures, 1, maxTextureSize, true);
		atlasToMake.spriteMaterial = new Material(ShaderCache.find("Unlit/Transparent Colored"));
		atlasToMake.spriteMaterial.SetTexture("_MainTex", newAtlasTexture);
		atlasToMake.spriteList = new List<UIAtlas.Sprite>();
		Texture2D tex;

		for (int i = 0; i < textures.Length; i++)
		{
			tex = textures[i];
			if (!texDic.ContainsKey(tex.name))
			{
				texDic.Add(tex.name, tex);
				Rect coordinate = coords[i];
				UIAtlas.Sprite sprite = new UIAtlas.Sprite { name = tex.name, inner = coordinate, outer = coordinate };
				atlasToMake.spriteList.Add(sprite);
			}
		}

		return atlasToMake;
	}

	public static void remakeAtlasWithImages(UIAtlas atlasToRemake, Texture2D[] texturesToUse)
	{
		Dictionary<string, Texture2D> texDic = new Dictionary<string, Texture2D>();
		Rect[] coords;
		Texture2D newAtlasTexture = new Texture2D(1, 1);
		coords = newAtlasTexture.PackTextures(texturesToUse, 1, 4096);
		atlasToRemake.spriteMaterial = new Material(ShaderCache.find("Unlit/Transparent Colored"));
		atlasToRemake.spriteMaterial.SetTexture("_MainTex", newAtlasTexture);
		atlasToRemake.spriteList = new List<UIAtlas.Sprite>();
		Texture2D tex;

		for (int i = 0; i < texturesToUse.Length; i++)
		{
			tex = texturesToUse[i];
			if (!texDic.ContainsKey(tex.name))
			{
				texDic.Add(tex.name,tex);
 				Rect coordinate = coords[i];
				UIAtlas.Sprite sprite = new UIAtlas.Sprite { name = tex.name, inner = coordinate, outer = coordinate };
				atlasToRemake.spriteList.Add(sprite);
			}
		}
	}
}
