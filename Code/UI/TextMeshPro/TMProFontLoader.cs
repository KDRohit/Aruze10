using UnityEngine;
using System.Collections;
using TMPro;

/*
Contains functions to extend TextMeshPro without modifying the TMPro source code.
*/

public class TMProFontLoader : MonoBehaviour
{
	// If adding fonts here, also add them to CarouselDesignTool.cs
	public TMP_FontAsset openSans;
	public TMP_FontAsset monofontoNumbers;
	public TMP_FontAsset masala;
	// If adding fonts here, also add them to CarouselDesignTool.cs
	
	private static TMProFontLoader instance = null;
	
	void Awake()
	{
		instance = this;
	}
	
	// Returns a reference font of the given name, for use with things that dynamically set fonts.
	// Input string must match the font asset names.
	public static TMP_FontAsset getFont(string name)
	{
		if (Application.isPlaying)
		{
			switch (name)
			{
				case "OpenSans-Bold SDF":
					return instance.openSans;
				case "monofonto numbers SDF":
					return instance.monofontoNumbers;
				case "MasalaPro-Bold SDF":
					return instance.masala;
				case "PollerOne SDF":
					Debug.LogError("Attempt to use PollerOne SDF as app font, open sans returned.");
					return instance.openSans;
				case "Teko-Bold SDF":
					Debug.LogError("Attempt to use Teko-Bold SDF as app font, open sans returned.");				
					return instance.openSans;
			}
		}
#if UNITY_EDITOR
		else
		{
			// When in edit mode, it's a bit tricker since the above references aren't defined.
			string path = string.Format("Assets/Data/Common/TextMeshPro Fonts/Resources/Fonts & Materials/{0}.asset", name);
			TMP_FontAsset fontObj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(TMP_FontAsset)) as TMP_FontAsset;

			if (fontObj != null)
			{
				return fontObj;
			}
		}
#endif
		
		return null;
	}

#if UNITY_EDITOR
	// Returns a general font material of the given name, for use with things that dynamically set fonts.
	// Input string must match the font material names located in the "Label Styles TMPro General" folder in common assets.
	// This is only intended to be used in the editor as part of an editor script.
	public static Material getGeneralMaterial(string name)
	{
		// When in edit mode, it's a bit tricker since the above references aren't defined.
		string path = string.Format("Assets/Data/Common/Label Styles TMPro General/{0}.mat", name);
		return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
	}
#endif

}
