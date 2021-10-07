using System;
using System.Collections.ObjectModel;
using UnityEngine;

/// Class used to store basic data about a symbol
[System.Serializable] public class SymbolInfo
{
	// General Symbol Infomation
	[SerializeField] private bool isUsingNameArray = false;
	[SerializeField] private string name;
	[SerializeField] private string[] nameArray; // Allows for definition of multiple symbol names that will use this info.
	public Vector3 scaling = Vector3.one;
	public Vector3 positioning = Vector3.zero;
	[HideInInspector] public int cellsHigh = 1;				// Defines how many reel cells this symbol occupies.  Default is 1, but some symbol use 2 or more cells.
	public int symbolLayer = 0;				// Controls how symbols on the reels layer, higher means it will be on top, will work with isLayeringOverlappingSymbols and is applied after that is handled
	public float layerByDepthAdjust = 0;	// Represents number of DEPTH_ADJUSTMENT off normal to use, used in combination with ReelGame.isLayeringSymbolsByDepth to adjust a symbols depth to push it over or under other symbols, because RenderQueue is proving to not be a great solution in combination with depth sorting
	public float layerByReelAdjust = 0;
	public float layerByCumulativeAdjust = 1f; // Scales the DEPTH_ADJUSTMENT for Cumulative layering when isLayeringSymbolsCumulative is enabled
	// 2D specific Symbol Information
	[HideInInspector] [SerializeField] private Texture2D baseTexture; // Hidden because it's depricated
	[HideInInspector] [SerializeField] private Material uvMappedMaterial; // Hidden because it's depricated
	[HideInInspector] public string shaderName = "Unlit/GUI Texture"; // Hidden because it's depricated
	[HideInInspector] public GameObject vfxPrefab; // Hidden because it's depricated
	[HideInInspector] public Texture2D wildTexture; // Hidden because it's depricated
	public GameObject wildOverlayGameObject = null;					// GameObject version of the overlaying WILD
	public bool wildHidesSymbol = false;							// Flag to tell if the wild overlay texture should hide the symbol behind it to prevent strange overlapping
	public bool disableWildOverlayGameObject = false;				// We don't always want to put a wild overlay gameobject over symbols. (Mainly if there is already a wild animation.)
	[HideInInspector] public ExpandingReelSymbolBase expandingSymbolOverlay = null;	// Define an ExpandingReelSymbol to be used to overlay this symbol when it has multiple same cell symbols grouped together  // Hidden because it's depricated
	public SymbolAnimationType anticipationAnimation;				// Animation for when a bonus symbol that is triggering anticipation shows up
	public SymbolAnimationType outcomeAnimation;
	public SymbolAnimationType mutateFromAnimation;
	public SymbolAnimationType mutateToAnimation;
	public bool keepObjectLayeringOnMutateTo;                       // Use the layers in the symbol prefab.
	
	// Bigger custom symbol things
	public GameObject symbolPrefab;
	public GameObject flattenedSymbolPrefab;
	public bool isSymbolSplitable = true;							// Tells if a GameObject prefab symbol with custom animations can be split into 1x1's
	public bool boxFullSymbol = false;								// If true, draw a payline around an entire large symbol, else split up into 1x1 boxes
	public float customAnimationDurationOverride = 0.0f;			// Allows for control of how long the custom animations play, required for symbols that have only Animators on them.  If 0.0f assumed to not be used!
	public bool endAnimatorAtNormalizedTime1 = false;				// Lets us decide if a SymbolAnimator should set the normalized time to 1 (rather than 0) when animation finishes

	// 3D specific Symbol Information
	[HideInInspector] public GameObject symbol3d; // Hidden because it's depricated
	
	private bool isNameAddedToNameArray = false;

	// Tells if this SymbolInfo has a visual element defined.  Can be used prior to calling
	// getSlotSymbolCacheHash to ensure that an actual valid hash can be generated for
	// this SymbolInfo.
	public bool hasVisualElementDefined()
	{
		return symbolPrefab != null || symbol3d != null || uvMappedMaterial != null || getTexture() != null;
	}

	public int getSlotSymbolCacheHash()
	{
		if (symbolPrefab != null)
		{
			return symbolPrefab.GetInstanceID();
		}
		else if (symbol3d != null)
		{
			return symbol3d.GetInstanceID();
		}
		else if (uvMappedMaterial != null)
		{
			return uvMappedMaterial.GetInstanceID();
		}
		else if (getTexture() != null)
		{
			return getTexture().GetInstanceID();
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogError("SymbolInfo.getSlotSymbolCacheHash() - No visual elements are set on this SymbolInfo, returning 0 for hash for SymbolInfo with names = " + getNameArrayAsString());
#endif
			return 0;
		}
	}

	public string getNameArrayAsString()
	{
		ReadOnlyCollection<string> names = getNameArrayReadOnly();
		string outputStr = "[";
		string seperator = ", ";
		foreach (string name in names)
		{
			outputStr += name + seperator;
		}

		int location = outputStr.LastIndexOf(seperator);
		if (location == -1)
		{
			outputStr += "]";
		}
		else
		{
			outputStr = outputStr.Remove(location, seperator.Length).Insert(location, "]");
		}

		return outputStr;
	}

	public ReadOnlyCollection<string> getNameArrayReadOnly()
	{
		if (!isNameAddedToNameArray)
		{
			if (!string.IsNullOrEmpty(name))
			{
				// Double check that the name isn't already in the array (although
				// this shouldn't be possible as SymbolInfoDrawer should clear it
				// when swapping between the array or single name value).
				bool isAlreadyInArray = System.Array.Exists(nameArray, isMatchForName);
				if (!isAlreadyInArray)
				{
					// The name isn't already in the array, so we need to insert it
					string[] newNameArray = new string[nameArray.Length + 1];

					// Insert the name at the front and then copy the existing values afterwards
					newNameArray[0] = name;
					for (int i = 0; i < nameArray.Length; i++)
					{
						newNameArray[i + 1] = nameArray[i];
					}

					// Make the newly created array the actual nameArray
					nameArray = newNameArray;
				}
			}

			isNameAddedToNameArray = true;
		}

		return Array.AsReadOnly(nameArray);
	}

	// Get the first name in the name array, used for instances where we just need to use some name
	// based on a name in this list.  For instance when making a flattened symbol where we need only
	// make one flattened symbol for the entire SymbolInfo.
	public string getFirstElementInNameArray()
	{
		ReadOnlyCollection<string> possibleSymbolNames = getNameArrayReadOnly();
		if (possibleSymbolNames.Count > 0)
		{
			return possibleSymbolNames[0];
		}
		else
		{
			return "";
		}
	}

	private bool isMatchForName(string passedName)
	{
		return passedName == name;
	}

	public bool isUVMappedMaterial()
	{
		return uvMappedMaterial != null;
	}
	
	/// Create a shallow copy
	public SymbolInfo createShallowCopy()
	{
		return this.MemberwiseClone() as SymbolInfo;
	}

	/// Allow the base texture to be set
	public void setBaseTexture(Texture2D texture)
	{
		baseTexture = texture;
	}

	/// Allows a user to pass in a material to apply a texture to, including uv mapping if it is being used
	public void applyTextureToMaterial(Material material)
	{
		if (baseTexture != null)
		{
			material.SetTexture("_MainTex", baseTexture);
		}
		else if (isUVMappedMaterial())
		{
			material.SetTexture("_MainTex", uvMappedMaterial.mainTexture as Texture2D);
			material.mainTextureScale = uvMappedMaterial.mainTextureScale;
			material.mainTextureOffset = uvMappedMaterial.mainTextureOffset;
		}
		else
		{
			Debug.LogError(name + " - No baseTexture or uvMappedMaterial, so nothing is being applied to the material!");
		}
	}
	
	public Material getUvMappedMaterial()
	{
		return uvMappedMaterial;
	} 

	/// Get the texture for this symbol either from the baseTexture or a texture stored in the uvMappedMaterial
	/// NOTE: If this is a uv mapping texture stored in the uvMappedMaterial this will return the full texture without the uv mapping info which is tied to the material
	public Texture2D getTexture()
	{
		if (baseTexture != null)
		{
			return baseTexture;
		}
		else if (isUVMappedMaterial())
		{
			return uvMappedMaterial.mainTexture as Texture2D;
		}
		else
		{
			return null;
		}
	}
}

/// Enum used to define symbol animation types for inspector selection.
/// A symbol animation is a combination of mesh animation, shader effects, and/or particles.
/// WARNING: Please use explicit int mappings, and DO NOT change them once set.
/// Changing the int values of the enums will impact all inspectors that use them,
/// effectively changing what enum value those inspectors are set to.
public enum SymbolAnimationType
{
	NONE = 0,
	ANTICIPATE_01 = 1,
	ANTICIPATE_02 = 2,
	ANTICIPATE_03 = 3,
	ANTICIPATE_04 = 4,
	ANTICIPATE_05 = 5,
	ANTICIPATE_06 = 6,
	ANTICIPATE_07 = 7,
	ANTICIPATE_08 = 8,
	ANTICIPATE_09 = 9,
	OUTCOME_01 = 10,
	OUTCOME_02 = 11,
	OUTCOME_03 = 12,
	OUTCOME_04 = 13,
	OUTCOME_05 = 14,
	OUTCOME_06 = 15,
	OUTCOME_07 = 16,
	OUTCOME_08 = 17,
	OUTCOME_09 = 18,
	MUTATE_01_A = 19,
	MUTATE_01_B = 20,
	MUTATE_02_A = 21,
	MUTATE_02_B = 22,
	CUSTOM = 23,
	
	MISSING = 1000
}
