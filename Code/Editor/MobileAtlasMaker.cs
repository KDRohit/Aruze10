using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Menu action to build low res atlases from an associated hi res atlas.
*/
public static class LowResAtlasMaker
{
	[MenuItem("Zynga/Editor Atlas/Make Cheap Low Res Atlas")] public static void menuMakeLowResAtlas()
	{
		GameObject hiAtlasObject = Selection.activeGameObject;

		if (hiAtlasObject == null || !hiAtlasObject.name.Contains("Hi"))
		{
			Debug.LogWarning("Please select a 'Hi' atlas prefab before running this menu command.");
			return;
		}

		UIAtlas hiAtlas = hiAtlasObject.GetComponent<UIAtlas>();

		if (hiAtlas == null)
		{
			Debug.LogWarning("The selected GameObject has no UIAtlas component.");
			return;
		}

		hiAtlas.coordinates = UIAtlas.Coordinates.TexCoords;

		Material hiMaterial = hiAtlas.spriteMaterial;
		Texture2D hiTexture = hiMaterial.mainTexture as Texture2D;

		// Build path strings
		string hiAtlasPath = AssetDatabase.GetAssetPath(hiAtlasObject);
		string hiMaterialPath = AssetDatabase.GetAssetPath(hiMaterial);
		string hiTexturePath = AssetDatabase.GetAssetPath(hiTexture);
		string lowAtlasPath = hiAtlasPath.Replace("Hi", "Low");
		string lowMaterialPath = hiMaterialPath.Replace("Hi", "Low");
		string lowTexturePath = hiTexturePath.Replace("Hi", "Low");

		GameObject lowAtlasObject = AssetDatabase.LoadAssetAtPath(lowAtlasPath, typeof(GameObject)) as GameObject;
		if (lowAtlasObject == null)
		{
			// TODO:UNITY2018:nestedprefabs:confirm//old
			// lowAtlasObject = PrefabUtility.CreatePrefab(lowAtlasPath, hiAtlasObject);
			// TODO:UNITY2018:nestedprefabs:confirm//new
			lowAtlasObject = PrefabUtility.SaveAsPrefabAsset(hiAtlasObject, lowAtlasPath);
		}
		else
		{
			EditorUtility.CopySerialized(hiAtlasObject, lowAtlasObject);
			EditorUtility.CopySerialized(hiAtlasObject.GetComponent<UIAtlas>(), lowAtlasObject.GetComponent<UIAtlas>());
		}

		UIAtlas lowAtlas = lowAtlasObject.GetComponent<UIAtlas>(); 
		if (lowAtlas == null)
		{
			Debug.LogError("Missing UIAtlas on " + lowAtlasPath);
			return;
		}

		Material lowMaterial = AssetDatabase.LoadAssetAtPath(lowMaterialPath, typeof(Material)) as Material;
		if (lowMaterial == null)
		{
			lowMaterial = new Material(ShaderCache.find("Unlit/Premultiplied Colored"));
			AssetDatabase.CreateAsset(lowMaterial, lowMaterialPath);
		}
		
		lowAtlas.spriteMaterial = lowMaterial;

		// Make a copy of the hi texture
		AssetDatabase.DeleteAsset(lowTexturePath);
		AssetDatabase.CopyAsset(hiTexturePath, lowTexturePath);
					
		// Set import settings
		TextureImporter hiImporter = TextureImporter.GetAtPath(hiTexturePath) as TextureImporter;	// Get this to copy the textureFormat for the low version.
		TextureImporter lowImporter = TextureImporter.GetAtPath(lowTexturePath) as TextureImporter;

		//Get the texture import formats for both iphone and android; we will use this when setting overrides
		TextureImporterFormat androidFormat = TextureImporterFormat.Automatic;
		TextureImporterFormat iosFormat = TextureImporterFormat.Automatic;
		int size = 0;
		hiImporter.GetPlatformTextureSettings("Android",  out size,  out androidFormat);
		hiImporter.GetPlatformTextureSettings("iPhone",  out size,  out iosFormat);

		lowImporter.maxTextureSize = hiTexture.width / 2;	//Mathf.NextPowerOfTwo(Mathf.Max(hiTexture.width / 2, hiTexture.height / 2) - 1);
		lowImporter.mipmapEnabled = false;
		lowImporter.wrapMode = TextureWrapMode.Clamp;
		lowImporter.filterMode = FilterMode.Bilinear;
		lowImporter.anisoLevel = 0;

		//Set the platform texture settings for both of the devices
		CommonEditor.setTextureImporterOverrides(lowImporter, "iPhone", lowImporter.maxTextureSize, iosFormat, 100, false);
		CommonEditor.setTextureImporterOverrides(lowImporter, "Android", lowImporter.maxTextureSize, androidFormat, 50, false);
		AssetDatabase.ImportAsset(lowTexturePath, ImportAssetOptions.ForceUpdate);

		Texture2D lowTexture = AssetDatabase.LoadAssetAtPath(lowTexturePath, typeof(Texture2D)) as Texture2D;
		
		lowMaterial.mainTexture = lowTexture;
		lowAtlas.spriteMaterial = lowMaterial;
		lowAtlas.pixelSize = hiAtlas.pixelSize * 2f;
		lowAtlas.coordinates = UIAtlas.Coordinates.Pixels;

		EditorUtility.SetDirty(lowTexture);
		EditorUtility.SetDirty(lowMaterial);
		lowAtlas.MarkAsDirty();
		EditorUtility.SetDirty(lowAtlasObject);

		hiAtlas.coordinates = UIAtlas.Coordinates.Pixels;
		hiAtlas.MarkAsDirty();

		AssetDatabase.SaveAssets();

		Debug.Log("Low res atlas has been created or updated: " + lowAtlasPath);
	}
}	
