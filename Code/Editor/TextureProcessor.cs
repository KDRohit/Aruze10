using UnityEngine;
using UnityEditor;

/// <summary>
/// This Texture Post Processor can do several (optional) texture operations,  based on importer userdata keywords.
/// It is initially being used to handle assetbundle variant texture dithering and conversion to RGBA4444. -KK
/// </summary>
public class TextureProcessor : AssetPostprocessor
{
	// Userdata Keywords
	public static string DITHER       = "DitherFS;";    // userdata keyword to enable this postprocess
	public static string MAKE4444     = "Make4444;";    // userdata keyword to enable this postprocess
	public static string TINTMIPMAPS  = "TintMips;";    // userdata keyword to enable this postprocess


	// When updating any texture, check for certain userdata keywords that cause post-processing steps to happen
	void OnPostprocessTexture(Texture2D target)
	{
		// When called as a post-process, get the assetpath from this AssetPostProcessor, as the intermediate texture object may not have a path
		var importer = (assetImporter as TextureImporter);

		// Optionally tint the top few mipmap levels (dev/debug feature to visualize which mips are in use)
		if (userDataHasKeyword(importer, TINTMIPMAPS))
		{
			//Debug.Log("TextureMipTint post-process on " + assetPath); 
			requireTrueColor(importer);
			tintMipMaps(target);
		}
			
		// Optionally dither to an effective RGBA4444 bit depth
		if (userDataHasKeyword(importer, DITHER))
		{
			//Debug.Log("Dither post-process on " + assetPath); 
			requireTrueColor(importer);
			ditherFloydSteinberg4444(target);
		}
			
		// Optionally convert texture to RGBA4444 format (overrides importer setting, without changing importer)
		if (userDataHasKeyword(assetImporter, MAKE4444))
		{
			//Debug.Log("Make4444 post-process on " + assetPath); 
			requireTrueColor(importer);
			EditorUtility.CompressTexture(target, TextureFormat.RGBA4444, 50);
		}
	}

	// Lower values run first; we want to run first...
	override public int GetPostprocessOrder()
	{
		return -100;
	}

	// Dither to (an effective) RGBA4444 fidelity, in preparation for actual conversion to 4444
	static void ditherFloydSteinberg4444(Texture2D texture)
	{
		const float paletteSize = 16.0f;
		const float k1Per15 = 1.0f / (paletteSize-1);

		const float k1Per16 = 1.0f / 16.0f;
		const float k3Per16 = 3.0f / 16.0f;
		const float k5Per16 = 5.0f / 16.0f;
		const float k7Per16 = 7.0f / 16.0f;
			
		for (int mipLevel = 0; mipLevel < texture.mipmapCount; mipLevel++)
		{
			var pixels = texture.GetPixels(mipLevel);
			var texw = texture.width >> mipLevel;
			var texh = texture.height >> mipLevel;
			var offs = 0;

			for (var y = 0; y < texh; y++) 
			{
				for (var x = 0; x < texw; x++) 
				{
					// read full precision pixel
					float a = pixels[offs].a;
					float r = pixels[offs].r;
					float g = pixels[offs].g;
					float b = pixels[offs].b;

					// quantize components to 4bpp precision
					var a2 = Mathf.Clamp01(Mathf.Floor(a * 16) * k1Per15);
					var r2 = Mathf.Clamp01(Mathf.Floor(r * 16) * k1Per15);
					var g2 = Mathf.Clamp01(Mathf.Floor(g * 16) * k1Per15);
					var b2 = Mathf.Clamp01(Mathf.Floor(b * 16) * k1Per15);

					// write back quantized pixel
					pixels[offs].a = a2;
					pixels[offs].r = r2;
					pixels[offs].g = g2;
					pixels[offs].b = b2;

					// calculate error
					var ae = a - a2;
					var re = r - r2;
					var ge = g - g2;
					var be = b - b2;

					// propogate error into neighboring pixels, with Floyd Steinberg weighting
					var n1 = offs + 1;
					var n2 = offs + texw - 1;
					var n3 = offs + texw;
					var n4 = offs + texw + 1;

					if (x < texw - 1) 
					{
						pixels[n1].a += ae * k7Per16;
						pixels[n1].r += re * k7Per16;
						pixels[n1].g += ge * k7Per16;
						pixels[n1].b += be * k7Per16;
					}

					if (y < texh - 1) 
					{
						pixels[n3].a += ae * k5Per16;
						pixels[n3].r += re * k5Per16;
						pixels[n3].g += ge * k5Per16;
						pixels[n3].b += be * k5Per16;

						if (x > 0) 
						{
							pixels[n2].a += ae * k3Per16;
							pixels[n2].r += re * k3Per16;
							pixels[n2].g += ge * k3Per16;
							pixels[n2].b += be * k3Per16;
						}

						if (x < texw - 1) 
						{
							pixels[n4].a += ae * k1Per16;
							pixels[n4].r += re * k1Per16;
							pixels[n4].g += ge * k1Per16;
							pixels[n4].b += be * k1Per16;
						}
					}

					offs++;
				}
			}

			texture.SetPixels(pixels, mipLevel);
		}
	}

	// Colorize the top few miplevels so we (devs) can visualize which ones actually get rendered
	static void tintMipMaps(Texture2D texture)
	{
		for (int mipLevel = 0; mipLevel < texture.mipmapCount; mipLevel++)
		{
			var pixels = texture.GetPixels(mipLevel); // float components are [0.0, 1.0]
			var texw = texture.width >> mipLevel;
			var texh = texture.height >> mipLevel;

			float scaleR = 1.0f;
			float scaleG = 1.0f;
			float scaleB = 1.0f;
			float offsetR = 0.0f;
			float offsetG = 0.0f;
			float offsetB = 0.0f;
			float offset = 0.20f;
			float scale = 0.75f;

			if (mipLevel == 0) //yellow
			{
				offsetR = offset;
				offsetG = offset;
				scaleB = scale;
			}

			if (mipLevel == 1) //red
			{
				offsetR = offset;
				scaleG = scale;
				scaleB = scale;
			}
			if (mipLevel == 2) //green
			{
				offsetG = offset;
				scaleR = scale;
				scaleB = scale;
			}
			if (mipLevel == 3) //blue
			{
				offsetB = offset;
				scaleR = scale;
				scaleG = scale;
			}

			var offs = 0;
			for (var y = 0; y < texh; y++) 
			{
				for (var x = 0; x < texw; x++) 
				{
					pixels[offs].r = pixels[offs].r * scaleR + offsetR;
					pixels[offs].g = pixels[offs].g * scaleG + offsetG;
					pixels[offs].b = pixels[offs].b * scaleB + offsetB;
					pixels[offs].a = pixels[offs].a;

					offs++;
				}
			}
			texture.SetPixels(pixels, mipLevel);
		}
	}


	void requireTrueColor(TextureImporter importer)
	{
		BuildTarget currentTarget = EditorUserBuildSettings.activeBuildTarget;
		string platformName = CommonEditor.getPlatformNameFromBuildTarget(currentTarget);

		// Get platform overrides (if they exist), else returns base/generic settings
		int importerMaxSize;
		int importerQuality;
		TextureImporterFormat importerFormat;
		importer.GetPlatformTextureSettings(platformName, out importerMaxSize, out importerFormat, out importerQuality);

		if (importerFormat != TextureImporterFormat.RGBA32 &&
			importerFormat != TextureImporterFormat.ARGB32)
		{
			fatalError("Fatal Error - TexureProcessor requires RGBA32/ARGB32 src format, bad asset: " + importer.assetPath + "   format: " + importerFormat);
		}
	
	}

	public static bool userDataHasKeyword(AssetImporter importer, string keyword)
	{
		return importer.userData.Contains(keyword);
	}

	// handle fatal error
	//
	// Unfortunately, throwing an exception during a Unity asset import/refresh does not stop the remaining refreshes
	// We want to stop processing, so as to not silently importer bad assets or upload them to server-cache
	//
	// So for now, lets try to Exit the editor app with an error code... (TODO: Verify jenkins sees fatal error)
	static void fatalError(string msg)
	{
		Debug.LogError(msg);

		// doesnt work as expected
		//throw new System.Exception(msg);

		if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
		{
			// Force unity to immediately bail with error code
			Debug.LogError("*************** TRYING TO ABORT ASSET IMPORT/REFRESH; FORCING APP EXIT ****************");
			EditorApplication.Exit(1);
		}
		else
		{
			// Maybe a pop-up dialog?  TODO: Use Complex to ignore-all?
			bool isOkay = EditorUtility.DisplayDialog("TextureProcessor Error", msg, "Ignore", "KillEditor");
			if (!isOkay)
			{
				EditorApplication.Exit(1);
			}
		}
	}

}
