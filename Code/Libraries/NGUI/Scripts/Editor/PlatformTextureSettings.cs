using UnityEngine;
using UnityEditor;
using System.Collections;

public class PlatformTextureSettings
{
	public string platform = "";
	public int maxTextureSize = 0;
	public TextureImporterFormat textureFormat = TextureImporterFormat.ARGB32;
	public int compressionQuality = 100;

	public PlatformTextureSettings(string platform, int maxTextureSize = 0, TextureImporterFormat textureFormat = TextureImporterFormat.ARGB32, int compressionQuality = 100)
	{
		this.platform = platform;
		this.maxTextureSize = maxTextureSize;
		this.textureFormat = textureFormat;
		/*this.compressionQuality = compressionQuality; // commenting this out for now as I don't think there's a case where we wouldn't want quality 100*/
	}
}
