using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/*
This tool finds all texture dependencies used by selected bundle(s), and generates a per-bundle report 
showing how much memory would be used by each texture, including for ETC2-expanded textures, and their totals.
This should help identify the highest texture-memory games, since compressed AssetBundle sizes dont reflect that.
*/

public class TextureReporter
{
	
	// Find all assets & dependencies of a bundle (ignoring unused material texture references) & generate a size report.
	// 
	// This works by looking at each actual imported texture asset (rather than texture importer settings)
	// So it has the resulting size, format, etc. based on the platform-specific importer rules.
	// To look at Android results, one must be running the Unity editor in the Android mode.
	// To look at "Standard Def" variant estimates, one must first apply the "Standard Def" overrides in place

	[MenuItem ("Zynga/Asset Reports/Generate Texture Report for selected bundles")]
	static void generateTextureReportForSelectedBundles()
	{
		string unusedCsvReport;
		var bundleNames = AssetBundleTagger.findAllBundleTagsInSelection();
		var fullReport = generateTextureReportForBundles(bundleNames, out unusedCsvReport);

		// Put in copy-paste buffer so user can just 'paste' into a text editor
		EditorGUIUtility.systemCopyBuffer = fullReport;
		Debug.Log("Report copied to system clipboard; you can <paste> it to an editor...");
	}

	public static string generateTextureReportForBundlesInPath(string path, out string csvReport)
	{
		var bundleNames = AssetBundleTagger.findAllBundleTagsInPath(path);
		var fullReport = generateTextureReportForBundles(bundleNames, out csvReport);
		return fullReport;
	}

	static string generateTextureReportForBundles(string[] bundleNames, out string csvReport)
	{
		Debug.Log("Creating Texture Reports for  " + bundleNames.Length + " bundles: " + string.Join(", ", bundleNames));
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// SpritePacking must at least be enabled for "build time"
		if (UnityEditor.EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
		{
			UnityEditor.EditorSettings.spritePackerMode = SpritePackerMode.BuildTimeOnly;
		}
		UnityEditor.Sprites.Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget, true);


		string allReports = "";
		string allSummaryLines = "";
		string allCsvSummaryLines = "";
		foreach (var bundleName in bundleNames)
		{
			string summaryLine;
			string csvSummaryLine;
			string report = generateTextureReportForBundle(bundleName, out summaryLine, out csvSummaryLine);

			allReports += "==========\n".PadLeft(104, '=');
			allReports += report;

			allSummaryLines += summaryLine;
			allCsvSummaryLines += csvSummaryLine;
		}

		Debug.Log("Done creating texture reports in " + stopwatch.Elapsed);

		// return csvReport
		csvReport = "Bundle, Size, NoETC2\n" + allCsvSummaryLines;

		return string.Format("Per-Bundle Totals:{0,-60} Size  /   NoETC2 \n", "") + allSummaryLines + "\n\n" + allReports;
	}

	static string generateTextureReportForBundle(string bundleName, out string summaryLine, out string csvSummaryLine)
	{
		string[] topLevelAssets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
		var texturePaths = getAllTextureDependenciesSkipUnusedMaterialTextures( topLevelAssets );

		string report = "Bundle: '" + bundleName + "'  contains:  " + texturePaths.Length + " texture paths\n\n";
		report += "   TextureName                                         Size    Mip    Format        Size  /   NoETC2     .\n";
	
		int totalTexSize = 0;         //in KB
		int totalTexSizeExpanded = 0; //in KB

		var spriteAtlasNamesSeen = new HashSet<string>();

		foreach(var texturePath in texturePaths)
		{
			var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

			if (texture == null)
			{
				Debug.LogWarning("cant load texture2d: " + texturePath);
				continue;
			}

			// Do we have android overrides? (important - we can't make SD variants otherwise)
			var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
			var androidSettings = importer.GetPlatformTextureSettings("Android");
			string otherInfo = androidSettings.overridden ? "." : "NoAndroidOverride";

			// Check if this texture gets packed into an atlas, if so, use the atlas info instead
			Texture2D spriteAtlasTexture = getSpriteAtlasTextureFromTextureImporter(importer);
			if (spriteAtlasTexture != null)
			{
				if (spriteAtlasNamesSeen.Contains(spriteAtlasTexture.name))
				{
					// only show each atlas once
					continue;
				}
				spriteAtlasNamesSeen.Add(spriteAtlasTexture.name);
				texture = spriteAtlasTexture;
			}

			int texSize = estimateTextureSize(texture, false) / 1024;
			int texSizeExpandedETC2 = estimateTextureSize(texture, true) / 1024;

			report += string.Format(
				"{0,-50} {1,4} x {2,4}  {3,2}  {4,-10}   {5,5} KB / {6,5} KB   {7,3}  \n",
				texture.name.Replace("SpriteAtlasTexture-", ""), 
				texture.width, 
				texture.height, 
				texture.mipmapCount, 
				texture.format, 
				texSize,
				texSizeExpandedETC2,
				otherInfo
			);

			totalTexSize += texSize;
			totalTexSizeExpanded += texSizeExpandedETC2;
		}

		string totals = string.Format(
			"TOTAL:{0,5} MB / {1,5} MB ",
			totalTexSize / 1024,
			totalTexSizeExpanded / 1024
		);

		report += string.Format("{0,-75}{1}\n", "", totals);
		summaryLine = string.Format("{0,-70}{1}\n", bundleName, totals);

		csvSummaryLine = bundleName + ", " + (totalTexSize / 1024) + ", " + (totalTexSizeExpanded / 1024) + "\n";

		return report;
	}

	static int estimateTextureSize(Texture2D texture, bool expandETC2)
	{
		int estimatedSize = estimateTextureSize(texture.width, texture.height, texture.mipmapCount, texture.format, expandETC2);

		// sanity test: for non-expanded estimates, we should be close to the raw texture size 
		if (!expandETC2)
		{
			int actualSize = texture.GetRawTextureData().Length;
			float estimateVsActualRatio = (float)estimatedSize / actualSize;
			Debug.Assert(actualSize < 1024 || (estimateVsActualRatio > 0.95f && estimateVsActualRatio < 1.05f), 
				"TextureEstimate Error: Est=" + estimatedSize + "  raw=" + actualSize);
		}

		return estimatedSize;
	}

	static int estimateTextureSize(int width, int height, int mipLevels, TextureFormat format, bool expandETC2)
	{
		if (expandETC2)
		{
			// Some devices don't support ETC2, and decompress the texture to RGBA32
			format = isTextureFormatETC2(format) ? TextureFormat.RGBA32 : format;
		}

		// width * height * formatSize * 1.33 (if mipmapped)
		return (int)(width * height * AlphaSplitTextureInspector.getBitsPerPixel(format) / 8 * (mipLevels > 1 ? 1.33 : 1.0) );
	}

	static bool isTextureFormatETC2(TextureFormat format)
	{
		return (format == TextureFormat.ETC2_RGB ||
				format == TextureFormat.ETC2_RGBA1 ||
				format == TextureFormat.ETC2_RGBA8);
	}

	// Returns a list of all recursive texture dependencies, starting at rootPaths, but ignoring unused material texture references
	private static string[] getAllTextureDependenciesSkipUnusedMaterialTextures( string[] rootPaths )
	{
		var assetResults = new List<string>();
		var assetsSeen = new HashSet<string>();

		foreach (var path in rootPaths)
		{
			getAllTextureDependenciesRecurse(path, assetResults, assetsSeen);
		}

		return assetResults.Distinct().OrderBy(path => path).ToArray();
	}

	private static void getAllTextureDependenciesRecurse( string path, List<string> assetResults, HashSet<string> assetsSeen )
	{
		if (!assetsSeen.Contains(path)) // skip duplicate items
		{
			assetsSeen.Add(path);

			// Add only textures to our list
			if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
			{
				assetResults.Add(path);
			}
			else
			{
				// Recurse through immediate children (avoiding unused material texture references)
				var childrenAssets = CommonEditor.getDependenciesFixed(path);
				foreach (var child in childrenAssets)
				{
					getAllTextureDependenciesRecurse(child, assetResults, assetsSeen);
				}
			}
		}
	}

	private static Texture2D getSpriteAtlasTextureFromTextureImporter(TextureImporter importer)
	{
		if (importer.textureType == TextureImporterType.Sprite  && !string.IsNullOrEmpty(importer.spritePackingTag))
		{
			// Get first sub-sprite so we can get atlas info
			var sprite = AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(Sprite) ) as Sprite;
			if (sprite != null && sprite.packed)
			{
				Texture2D atlasTex = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, true);
				return atlasTex;
			}
			else
			{
				Debug.LogWarning("couldn't get packed sprite!");
			}
		}

		// Not a packed sprite
		return null;
	}

}
