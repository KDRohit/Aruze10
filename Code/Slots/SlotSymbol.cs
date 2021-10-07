using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
Symbols change constantly while reels are spinning.
This class maps symbols to their visual GameObjects.
It also provides animation calls to those GameObjects.
*/
[System.Serializable]
public class SlotSymbol
{
	public delegate void AnimateDoneDelegate(SlotSymbol sender);

	private string _name; // Must use accessors to get/set name
	public string debugName; // Name used for server debugging.
	public string debug; // Used for display on the test GUI display
	public SymbolAnimator animator { get; private set;}
	public SlotSymbol parentSymbol {get; private set;}	// The parent of a subsymbol. Null if none exists
	public SlotSymbol subsymbol {get; private set;}		// The child of the current symbol. Null if none exists.
	public int index { get; private set; }
	public SlotReel reel { get; private set; }
	private SymbolAnimator.AnimatingDelegate animatingCallback = null;
	private SlotSymbolCacheItem slotSymbolCacheItem = emptySlotSymbolCacheItem;

	// Name accessor
	public string name
	{
		get
		{ 
			return _name; 
		}
		set 
		{ 
			// Anytime we update SlotSymbol name, we update it's slotSymbolCacheItem
			if (_name != value)
			{
				_name = value;
				slotSymbolCacheItem = getOrCreateCacheItem(_name);
			}
		}
	}

	// An optimal way of changing a symbolName if you already have a SlotSymbolCacheItem to go with it
	protected void setNameAndCacheItem(string newName, SlotSymbolCacheItem cacheItem)
	{
		Debug.Assert( newName == cacheItem.name, "name doesn't match cacheItem!");

		this._name = newName;
		this.slotSymbolCacheItem = cacheItem;
	}
		
	public int visibleSymbolIndex
	{
		get 
		{
			if (reel != null)
			{
				return index - reel.numberOfTopBufferSymbols;
			}
			else
			{
				Debug.LogError("SlotSymbol.visibleSymbolIndex - Reel is null so no way to know _bufferSymbols size!  Returning 0.");
				return 0;
			}
		}
	}

	// Get the symbol index counting from the bottom of the reel to the top.
	// This matches up with the way symbol indices come down in server data.
	public int visibleSymbolIndexBottomUp
	{
		get 
		{
			if (reel != null)
			{
				return reel.visibleSymbols.Length - visibleSymbolIndex;
			}
			else
			{
				Debug.LogError("SlotSymbol.visibleSymbolIndex - Reel is null so no way to know _bufferSymbols size!  Returning 0.");
				return 0;
			}
		}
	}

	// Fastest to use static regex objects (they get parsed once) without any capturing groups
	static Regex rgxTallSymbol = new Regex("^[^-]+[-][1-9][A-Z]$");
	static Regex rgxMegaSymbol = new Regex("^[^-]+[-][1-9][A-Z][-][1-9][A-Z]$");
	public const string symbolVariantRegex = @"_Variant[1-9][0-9]*";

	public const string FLATTENED_SYMBOL_POSTFIX = "_Flattened";	// Post fix added to flatened symbols
	public const string OUTCOME_SYMBOL_POSTFIX = "_Outcome";		// Symbol name part denoting that this is a special animated outcome symbol
	public const string NO_ANIMATOR_SYMBOL_POSTFIX = "_NoAnimator"; // Old version of unanimated symbols, perfer _Flattened whenever possible
	public const string WILD_SYMBOL_VERSION_POSTFIX = "_WILD";		// Way some games have specified WILD versions of symbols
	public const string ACQUIRED_SYMBOL_POSTFIX = "_Acquired";		// Old version of animated verisons of symbols, use _Outcome instead
	public const string LAND_SYMBOL_POSTFIX = "_Land";				// Used in some games for landing specific animaitons, which are different from outcome animations
	public const string LOOP_SYMBOL_POSTIFIX = "_Loop";             // Used to indicate a looping animation for a symbol
	public const string VALUE_SYMBOL_POSTFIX = "_Value";            // Used to indicate a version of a symbol that has a setable value on label on it
	public const string AWARD_SYMBOL_POSTFIX = "_Award";			// Used in games which have Land, Lock, and Award animation Land/Lock can use the symbol name and the _Outcome version, but we need another version for the award
	public const string SYMBOL_VARIANT_POSTFIX = "_Variant";		// Used in skin changer games for different variants of the same symbol.  Will also include a number, for instance you could have M1 and M1_Variant1
	public const string TW_POSTFIX = "-TW";							// -TW postfix at the end of symbols, denoting it should do something for the TWs.
	public const string R_PREFIX = "R-";							// Random symbol, the feature will have this symbol spin into what ever is after the prefix.
	public const char   SUBSYMBOL_DELIMITER = ':';					// <SymbolName><SUBSYMBOL_DELIMITER><SubsymbolName>
	public const string BINGO_SYMBOL_BASE_NAME = "SL";				// Base name for bingo symbols that are usually in the format of SL##

	/// true if symbol 'should' be showing
	/// anyPart true:this or sibling cells is visible false:only this cell
	/// relativeToEngine true:engine says if visible false:reel says if visible
	public bool isVisible(bool anyPart, bool relativeToEngine)
	{
		if (relativeToEngine)
		{
			if(CommonDataStructures.arrayContains(_reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1, reel.layer), this))
			{
				return true;
			}
		}
		else
		{
			if(CommonDataStructures.arrayContains(reel.visibleSymbols, this))
			{
				return true;
			}
		}

		if (anyPart && isOnScreenPartial(relativeToEngine))
		{
			return true;
		}

		return false;
	}

	[System.NonSerialized] public int debugSymbolInsertionIndex;
	public const int SYMBOL_INSERTION_INDEX_ADDED = -1;
	public const int SYMBOL_INSERTION_INDEX_CLOBBERED = -2;

	/// Returns true if a symbol is part of a mega symbol (tall and wide)
	public bool isMegaSymbolPart
	{
		get
		{
			return slotSymbolCacheItem.isMegaSymbolPart;
		}
	}

	public static bool isMegaSymbolPartFromName(string name)
	{
		return getOrCreateCacheItem(name).isMegaSymbolPart;
	}

	private static bool isMegaSymbolPartFromNameActual(string name)
	{
		string baseName = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		return !string.IsNullOrEmpty(baseName) && rgxMegaSymbol.IsMatch(baseName);
	}

	// Tells if this symbol is a blank, i.e. is "BL"
	public bool isBlankSymbol
	{
		get
		{
			return slotSymbolCacheItem.isBlankSymbol;
		}
	}

	public static bool isBlankSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isBlankSymbol;
	}
		
	/// Returns true if a symbol is part of a tall symbol
	public bool isTallSymbolPart
	{
		get
		{
			return slotSymbolCacheItem.isTallSymbolPart;
		}
	}

	public static bool isTallSymbolPartFromName(string name)
	{
		return getOrCreateCacheItem(name).isTallSymbolPart;
	}

	private static bool isTallSymbolPartFromNameActual(string name)
	{
		string baseName = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		return !string.IsNullOrEmpty(baseName) && rgxTallSymbol.IsMatch(baseName);
	}

	public bool isLargeSymbolPart
	{
		get
		{
			return (slotSymbolCacheItem.isTallSymbolPart || slotSymbolCacheItem.isMegaSymbolPart);
		}
	}

	public static bool isLargeSymbolPartFromName(string name)
	{
		var item = getOrCreateCacheItem(name);
		return (item.isTallSymbolPart || item.isMegaSymbolPart);
	}

	private static bool isLargeSymbolPartFromNameActual(string name)
	{
		return SlotSymbol.isTallSymbolPartFromNameActual(name) || SlotSymbol.isMegaSymbolPartFromNameActual(name);
	}

	/// Tells if this is a flattened optimized symbol
	public bool isFlattenedSymbol
	{
		get
		{
			return slotSymbolCacheItem.isFlattenedSymbol;
		}
	}

	/// Static version that tells if this is a flattened optimized symbol
	public static bool isFlattenedSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isFlattenedSymbol;
	}
	
	// Tells if this is a _Loop symbol version
	public bool isLoopSymbol
	{
		get
		{
			return slotSymbolCacheItem.isLoopSymbol;
		}
	}
	
	// Tells if this is a _Award symbol version
	public bool isAwardSymbol
	{
		get
		{
			return slotSymbolCacheItem.isAwardSymbol;
		}
	}
	
	/// Tells if this is a special animated outcome symbol
	public bool isOutcomeSymbol
	{
		get
		{
			return slotSymbolCacheItem.isOutcomeSymbol;
		}
	}

	/// Static version that tells if this is a special animated outcome symbol
	public static bool isOutcomeSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isOutcomeSymbol;
	}

	public bool isBingoSymbol
	{
		get
		{
			return slotSymbolCacheItem.isBingoSymbol;
		}
	}

	private static bool isBingoSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isBingoSymbol;
	}

	// Get the SymbolLayerReorganizer if one exists, mostly used in cases where the reorganization has to be controlled
	public SymbolLayerReorganizer symbolReorganizer
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.symbolReorganizer;
			}
			else
			{
				return null;
			}
		}
	}

	/// Function so you no longer need to access an animator directly to get the gameObject
	public GameObject gameObject
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.gameObject;
			}
			else
			{
				return null;
			}
		}
	}

	/// Function so you no longer need to access an animator to get the transform.
	public Transform transform
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.transform;
			}
			else
			{
				return null;
			}
		}
	}
	
	// Gather all the symbol parts for a tall/mega symbol, if this is a 1x1 symbol it will just return that symbol
	// alone in the list.
	// NOTE: Since this generates a new list every time it is called, you may not want to use this in loops that will execute a lot (like every frame)
	// since that will generate a lot of lists.
	public List<SlotSymbol> getAllSymbolParts()
	{
		List<SlotSymbol> outputList = new List<SlotSymbol>();

		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null)
		{
			return outputList;
		}

		Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();
		if (size.x == 1 && size.y == 1)
		{
			// This symbol isn't tall or mega, so just return it as all the parts
			outputList.Add(this);
			return outputList;
		}

		if (animatorSymbol.animator == null)
		{
			Debug.LogWarning("This symbol doesn't seem to have a valid animator!");
			outputList.Add(this);
			return outputList;
		}

		int startingReelIndex = -1;
		SlotReel[]reelArray = _reelGame.engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			if (reelArray[i] == animatorSymbol.reel)
			{
				startingReelIndex = i;
				break;
			}
		}

		if (startingReelIndex == -1)
		{
			Debug.LogWarning("Trying to get parts of " + animatorSymbol.name + " symbol that isn't on a visibleReel. Just returning the original part.");
			outputList.Add(this);
			return outputList;
		}
		
		for (int reelIndex = startingReelIndex; reelIndex <  startingReelIndex + size.x; reelIndex++)
		{
			if (reelIndex < reelArray.Length)
			{
				SlotReel newReel = reelArray[reelIndex];
				for (int symbolIndex = animatorSymbol.index; symbolIndex < animatorSymbol.index + size.y; symbolIndex++)
				{
					if (symbolIndex < newReel.symbolList.Count)
					{
						outputList.Add(newReel.symbolList[symbolIndex]);
					}
					else
					{
						Debug.LogWarning(animatorSymbol.name + " is trying to gather all symbol parts but doesn't fit on all the reels." + symbolIndex + " vs " + newReel.symbolList.Count);
					}
				}
			}
			else
			{
				Debug.LogWarning(animatorSymbol.name + " is seemingly place incorrectly on the reels");
			}
		}
		
		return outputList;
	}

	/// Function so you no longer need to access an animator to get the symbolAnimationRoot.transform.
	public Transform transformAnimatorRoot
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null && animatorSymbol.animator.symbolAnimationRoot != null)
			{
				return animatorSymbol.animator.symbolAnimationRoot.transform;
			}
			else
			{
				return null;
			}
		}
	}

	/// Function so you no longer need to access an animator to get the transform.
	public GameObject scalingSymbolPart
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.scalingSymbolPart;
			}
			else
			{
				return null;
			}
		}
	}

	// This member-accessor is slightly different that the static accessor; this one appends a : subsubsymbol shortserver name,
	// which can be changed at anytime (and hence, is not cached)
	public string shortServerName
	{
		get
		{
			string shortServerName = slotSymbolCacheItem.staticShortServerName;
			if (subsymbol != null)
			{
				shortServerName += SUBSYMBOL_DELIMITER + subsymbol.shortServerName;
			}
			return shortServerName;
		}
	}

	public static string getShortServerNameFromName(string name)
	{
		return getOrCreateCacheItem(name).staticShortServerName;
	}

	private static string getShortServerNameFromNameActual(string name)
	{
		return SlotSymbol.getShortNameFromNameActual(SlotSymbol.getServerNameFromNameActual(name, isRemovingVariant:true));
	}
	
	public string shortServerNameWithVariant
	{
		get
		{
			string shortServerNameWithVariant = slotSymbolCacheItem.staticShortServerNameWithVariant;
			if (subsymbol != null)
			{
				shortServerNameWithVariant += SUBSYMBOL_DELIMITER + subsymbol.shortServerNameWithVariant;
			}
			return shortServerNameWithVariant;
		}
	}

	public static string getShortServerNameWithVariant(string name)
	{
		return getOrCreateCacheItem(name).staticShortServerNameWithVariant;
	}
	
	private static string getShortServerNameWithVariantFromNameActual(string name)
	{
		return SlotSymbol.getShortNameFromNameActual(SlotSymbol.getServerNameFromNameActual(name, isRemovingVariant:false));
	}

	/// Get a shortened name if this is a large symbol
	public string shortName
	{
		get
		{
			return slotSymbolCacheItem.shortName;
		}
	}

	public static string getShortNameFromName(string name)
	{
		return getOrCreateCacheItem(name).shortName;
	}

	private static string getShortNameFromNameActual(string name)
	{
		string nameWithoutsubSymbol = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		string baseName = SlotSymbol.getBaseNameFromNameActual(nameWithoutsubSymbol);
		string shortName = baseName;
		if (!string.IsNullOrEmpty(baseName) && SlotSymbol.isLargeSymbolPartFromNameActual(baseName))
		{
			shortName = baseName.Substring(0, baseName.IndexOf('-'));
		}
		if (SlotSymbol.hasSubsymbolFromNameActual(name))
		{
			shortName += SUBSYMBOL_DELIMITER + SlotSymbol.getShortServerNameFromNameActual(SlotSymbol.getSubsymbolFromNameActual(name));
		}
		return shortName;
	}

	// Get the server name, which is basically name but without animation extensions
	public string serverName
	{
		get
		{
			return slotSymbolCacheItem.serverName;
		}
	}

	public static string getServerNameFromName(string name)
	{
		return getOrCreateCacheItem(name).serverName;
	}

	private static string getServerNameFromNameActual(string name, bool isRemovingVariant)
	{
		string serverName = name;
		string baseName = SlotSymbol.getBaseNameFromNameActual(name);
		if (!string.IsNullOrEmpty(baseName))
		{
			serverName = serverName.Replace(OUTCOME_SYMBOL_POSTFIX, "");
			serverName = serverName.Replace(FLATTENED_SYMBOL_POSTFIX, "");
			serverName = serverName.Replace(NO_ANIMATOR_SYMBOL_POSTFIX, "");
			serverName = serverName.Replace(WILD_SYMBOL_VERSION_POSTFIX, "");
			serverName = serverName.Replace(ACQUIRED_SYMBOL_POSTFIX, "");
			serverName = serverName.Replace(LOOP_SYMBOL_POSTIFIX, "");
			serverName = serverName.Replace(VALUE_SYMBOL_POSTFIX, "");
			serverName = serverName.Replace(AWARD_SYMBOL_POSTFIX, "");

			if (isRemovingVariant)
			{
				serverName = Regex.Replace(serverName, symbolVariantRegex, "");
			}
		}
		return serverName;
	}
	
	public string serverNameWithVariant
	{
		get
		{
			return slotSymbolCacheItem.serverNameWithVariant;
		}
	}

	// Get the server name, which is basically name but without animation extensions
	public string subsymbolName
	{
		get
		{
			string subsymbolName = slotSymbolCacheItem.subsymbolName;

#if UNITY_EDITOR
			string verificationSubsymbolName = null;
			if (subsymbol != null)
			{
				verificationSubsymbolName = subsymbol.name;
			}
			if (subsymbolName != verificationSubsymbolName)
			{
				Debug.LogError("Subsymbol name " + verificationSubsymbolName + " Does not match parents subsymbol Name " + subsymbolName);
			}
#endif
			
			return subsymbolName;
		}
	}

	public static string getSubsymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).subsymbolName;
	}

	private static string getSubsymbolFromNameActual(string name)
	{
		if (SlotSymbol.hasSubsymbolFromNameActual(name))
		{
			return name.Substring(name.LastIndexOf(SUBSYMBOL_DELIMITER) + 1);
		}
		else
		{
			return null;
		}
	}

	public static string getNameWithoutSubsymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).nameWithoutSubsymbol;
	}

	private static string getNameWithoutSubsymbolFromNameActual(string name)
	{
		if (SlotSymbol.hasSubsymbolFromNameActual(name))
		{
			return name.Substring(0, name.LastIndexOf(SUBSYMBOL_DELIMITER));
		}
		else
		{
			return name;
		}
	}

	// Returns the symbols name without and of the -TW or R- attached.
	public string baseName
	{
		get
		{
			return slotSymbolCacheItem.baseName;
		}
	}

	// Returns the symbols name without extensions -TW or R- attached.
	public static string getBaseNameFromName(string name)
	{
		return getOrCreateCacheItem(name).baseName;
	}

	// Returns the symbols name without extensions -TW or R- attached.
	private static string getBaseNameFromNameActual(string name)
	{
		string baseName = name;
		if (!string.IsNullOrEmpty(baseName))
		{
			baseName = baseName.Replace("R-", "");
			baseName = baseName.Replace("-TW", "");
		}
		return baseName;
	}

	public bool isSubsymbol
	{
		get
		{
			return parentSymbol != null;
		}
	}

	public bool hasSubsymbol
	{
		get
		{
			return !string.IsNullOrEmpty( slotSymbolCacheItem.subsymbolName );
		}
	}

	public static bool hasSubsymbolFromName(string name)
	{
		return !string.IsNullOrEmpty( getOrCreateCacheItem(name).subsymbolName );
	}

	private static bool hasSubsymbolFromNameActual(string name)
	{
		if (!string.IsNullOrEmpty(name) && name.Contains(SUBSYMBOL_DELIMITER))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public bool hasTWPostfix
	{
		get
		{
			return SlotSymbol.hasTWPostfixFromName(name);
		}
	}

	public static bool hasTWPostfixFromName(string name)
	{
		return name.Contains(TW_POSTFIX);
	}

	public bool hasRPrefix
	{
		get
		{
			return SlotSymbol.hasRPrefixFromName(name);
		}
	}

	// Returns the symbols name without extensions -TW or R- attached.
	public static bool hasRPrefixFromName(string name)
	{
		return name.Contains(R_PREFIX);
	}

	public bool isWhollyOnScreen
	{
		get
		{
			if (parentSymbol != null)
			{
				return parentSymbol.isWhollyOnScreen;
			}

			SlotSymbol animatorSymbol = getAnimatorSymbol();
			if (animatorSymbol == null)
			{
				return false;
			}

			if (animatorSymbol.reel == null)
			{
				// special case in gen75 where we want to play tall symbol animations
				// without being attached to a reel.
				return true;
			}

			Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();
			for (int i = 0; i < animatorSymbol.reel.visibleSymbols.Length; i++)
			{
				if (animatorSymbol.reel.visibleSymbols[i] == animatorSymbol)
				{
					return i + size.y - 1 < animatorSymbol.reel.visibleSymbols.Length;
				}
			}
			return false;
		}
	}

	/// Helper for checking this property of the animator
	public bool isWildShowing 
	{ 
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.isWildShowing;
			}
			else
			{
				return false;
			}
		}
	}

	/// Function to get the symbolInfo from the symbol and not from the animator.
	public SymbolInfo info
	{
		get
		{
			SlotSymbol animatorSymbol = getAnimatorSymbol();

			if (animatorSymbol != null && animatorSymbol.animator != null)
			{
				return animatorSymbol.animator.info;
			}
			else
			{
				return null;
			}
		}
	}

	public bool isMajor
	{
		get
		{
			return slotSymbolCacheItem.isMajor;
		}
	}

	public static bool isMajorFromName(string name)
	{
		return getOrCreateCacheItem(name).isMajor;
	}

	public bool isMinor
	{
		get
		{
			return slotSymbolCacheItem.isMinor;
		}
	}

	public static bool isMinorFromName(string name)
	{
		return getOrCreateCacheItem(name).isMinor;
	}

	/// Tells if a symbol is a bonus symbol, in which case it only does an outcome animations when a full set of bonus symbols are obtained 
	public bool isBonusSymbol
	{
		get
		{
			return slotSymbolCacheItem.isBonusSymbol;
		}
	}

	public static bool isBonusSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isBonusSymbol;
	}

	/// Tells if the symbol is a wild symbol
	public bool isWildSymbol
	{
		get
		{
			return slotSymbolCacheItem.isWildSymbol;
		}
	}

	public static bool isWildSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isWildSymbol;
	}

	/// Tells if the symbol is a scatter symbol
	public bool isScatterSymbol
	{
		get
		{
			return slotSymbolCacheItem.isScatterSymbol;
		}
	}

	public static bool isScatterSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isScatterSymbol;
	}
			
	/// Tells if the symbol is a Jackpot symbol
	public bool isJackpotSymbol
	{
		get
		{
			return slotSymbolCacheItem.isJackpotSymbol;
		}
	}

	public static bool isJackpotSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isJackpotSymbol;
	}

	// Tells if the symbol is a replacement symbol
	// which will be replaced via a mapping form the SlotOutcome
	public bool isReplacementSymbol
	{
		get
		{
			return slotSymbolCacheItem.isReplacementSymbol;
		}
	}

	public static bool isReplacementSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).isReplacementSymbol;
	}

	public static string getReplacedSymbolName(string name, Dictionary<string, string> normalReplacementSymbolMap = null, Dictionary<string, string> megaReplacementSymbolMap = null)
	{
		string subsymbolName = SlotSymbol.getSubsymbolFromName(name);
		name = SlotSymbol.getNameWithoutSubsymbolFromName(name);
		
		if (normalReplacementSymbolMap != null)
		{
			foreach(KeyValuePair<string, string> replaceInfo in normalReplacementSymbolMap)
			{
				if (SlotSymbol.getShortNameFromName(name) == replaceInfo.Key)
				{
					if (name.Contains("R-"))
					{
						name = "R-" + replaceInfo.Value;
					}
					else
					{
						name = replaceInfo.Value;
					}
				}
			}
		}

		if (megaReplacementSymbolMap != null)
		{
			foreach(KeyValuePair<string, string> replaceInfo in megaReplacementSymbolMap)
			{
				if (SlotSymbol.getShortNameFromName(name) == replaceInfo.Key)
				{
					name = name.Replace(replaceInfo.Key, replaceInfo.Value);
				}
			}
		}

		if (!string.IsNullOrEmpty(subsymbolName))
		{
			name += SUBSYMBOL_DELIMITER + SlotSymbol.getReplacedSymbolName(subsymbolName, normalReplacementSymbolMap, megaReplacementSymbolMap);
		}

		return name;
	}

	/// Need a guaranteed way to split large symbols even if they say they can't be split
	/// That is what this funciton will do, by splitting the symbol into random 1x1 symbols for the reel this symbol lives on
	/// ---
	/// NOTE : This function can cause desyncs and wrong looking outcomes if used before outcomes 
	/// are displayed because it might put a 1x1 symbol that would changes outcomes. So only use this function to correct visuals and not before outcomes display.
	/// ---
	/// @todo (06/06/2017 Kelly Zhang) Should eventually take in allowFlattenedSymbolSwap like splitSymbol()
	public void splitSymbolToRandomSymbols()
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null)
		{
			return;
		}

		Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();
		if (size.x == 1 && size.y == 1)
		{
			Debug.LogWarning("Trying to split a symbol that is already 1x1.");
			return;
		}

		if (animatorSymbol.animator == null)
		{
			Debug.LogWarning("Trying to split a symbol which doesn't seem to be able to split!");
			// symbol cannot be split
			return;
		}

		int reelID = -1;
		SlotReel[]reelArray = _reelGame.engine.getReelArray();

		for (int i = 0; i < reelArray.Length; i++)
		{
			if (reelArray[i] == animatorSymbol.reel)
			{
				reelID = i;
				break;
			}
		}

		if (reelID == -1)
		{
			Debug.LogWarning("Trying to split " + animatorSymbol.name + " symbol that isn't on a visibleReel.");
		}
		Debug.Log(name + ": size = " + size.x + " , " + size.y);
		for (int reelIndex = reelID; reelIndex <  reelID + size.x; reelIndex++)
		{
			if (reelIndex >= reelArray.Length)
			{
				Debug.LogWarning(animatorSymbol.name + " is trying to be split up but doesn't fit on all the reels.");
				return;
			}
			SlotReel newReel = reelArray[reelIndex];
			for (int symbolIndex = animatorSymbol.index; symbolIndex < animatorSymbol.index + size.y; symbolIndex++)
			{
				if (symbolIndex >= newReel.symbolList.Count)
				{
					Debug.LogWarning(animatorSymbol.name + " is trying to be split up but doesn't fit on all the reels." + symbolIndex + " vs " + newReel.symbolList.Count);
					return;
				}
				string oldName = newReel.symbolList[symbolIndex].name;
				newReel.symbolList[symbolIndex].cleanUp();

				// create a random symbol name
				string randomReplacementName = newReel.getRandomClobberReplacementSymbol();

				newReel.symbolList[symbolIndex].setupSymbol(randomReplacementName, symbolIndex, newReel);
				newReel.symbolList[symbolIndex].refreshSymbol(0); // Refresh the symbol if it got changed.
				newReel.symbolList[symbolIndex].debug = "Split " + oldName;
			}
		}
	}

	// Takes a symbol and splits it up into the smaller version of the symbol.
	// Making a new instance for each symbol. Flattens symbol if true is passed in.
	public void splitSymbol(bool allowFlattenedSymbolSwap = false)
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null)
		{
			return;
		}

		Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();
		if (size.x == 1 && size.y == 1)
		{
			Debug.LogWarningFormat("Trying to split {0} symbol that is already 1x1.", animatorSymbol.name);
			return;
		}

		if (animatorSymbol.animator == null)
		{
			Debug.LogWarningFormat("Trying to split {0} symbol but the animator is null!", animatorSymbol.name);
			return;
		}

		if (!animatorSymbol.animator.info.isSymbolSplitable)
		{
			Debug.LogWarningFormat("Trying to split {0} symbol but the flag isSymbolSplitable is false!", animatorSymbol.name);
			return;
		}

		string smallSymbolName = getShortNameFromName(serverName);
		int reelID = -1;

		if (animatorSymbol.reel != null)
		{
			reelID = animatorSymbol.reel.reelID - 1;
		}

		if (reelID == -1)
		{
			Debug.LogWarningFormat("Trying to split {0} symbol that isn't on a visibleReel. CurrentReel is: {1} -- length of thing: {2}",
				animatorSymbol.name, animatorSymbol.reel.getReelGameObject().name, _reelGame.engine.getReelArray().Length);
		}
		
		//Debug.Log(name + ": size = " + size.x + " , " + size.y);
		for (int reelIndex = reelID; reelIndex < reelID + size.x; reelIndex++)
		{
			SlotReel newReel = _reelGame.engine.getSlotReelAt(reelIndex, -1, animatorSymbol.reel.layer);

			if (newReel == null)
			{
				Debug.LogWarning("SlotSymbol.splitSymbol() - Unable to find reel for: reelIndex = " + reelIndex + "; row = -1; layer = " + animatorSymbol.reel.layer + "; Aborting split.");
				return;
			}

			for (int symbolIndex = animatorSymbol.index; symbolIndex < animatorSymbol.index + size.y; symbolIndex++)
			{
				//Splits symbol only onto valid reel positions, otherwise would attempt to overflow past buffer.
				if (symbolIndex < newReel.symbolList.Count) 
				{
					
					string oldName = newReel.symbolList [symbolIndex].name;
					newReel.symbolList [symbolIndex].cleanUp ();
					newReel.symbolList [symbolIndex].setupSymbol (smallSymbolName, symbolIndex, newReel, allowFlattenedSymbolSwap: allowFlattenedSymbolSwap);
					newReel.symbolList [symbolIndex].refreshSymbol (0); // Refresh the symbol if it got changed.
					newReel.symbolList [symbolIndex].debug = "Split " + oldName;
					foreach (SlotModule module in _reelGame.cachedAttachedSlotModules) 
					{
						if (module.needsToExecuteAfterSymbolSplit ()) 
						{
							module.executeAfterSymbolSplit (newReel.symbolList [symbolIndex]);
						}
					}
				}
			}
		}
	}

	/// Tells if a call to splitSymbol() will have any effect, normally you should check this before calling splitSymbol()
	public bool canBeSplit()
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null)
		{
			// Top piece wasn't located
			return false;
		}

		Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();
		if (size.x == 1 && size.y == 1)
		{
			// this symbols is already a 1x1
			return false;
		}

		if (animatorSymbol.animator == null || !animatorSymbol.animator.info.isSymbolSplitable)
		{
			// symbol has been marked as not being able to be split
			return false;
		}

		return true;
	}

	// DEPRECATED:: Use the couroutine, or the immediate versions.
	public void fadeOutSymbol(float lengthOfFade)
	{
		animator.startFadeOut(lengthOfFade);
	}

	public void fadeSymbolOutImmediate()
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();

		if (animatorSymbol != null && animatorSymbol.animator != null)
		{
			// flatten the symbol before fading if needed
			if (_reelGame.isGameUsingOptimizedFlattenedSymbols && !animatorSymbol.isFlattenedSymbol)
			{
				animatorSymbol.mutateToFlattenedVersion();
			}
			
			animatorSymbol.animator.fadeSymbolOutImmediate();	
		}
	}

	public IEnumerator fadeOutSymbolCoroutine(float duration)
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();

		if (animatorSymbol != null && animatorSymbol.animator != null)
		{
			// flatten the symbol before fading if needed
			if (_reelGame.isGameUsingOptimizedFlattenedSymbols && !animatorSymbol.isFlattenedSymbol)
			{
				animatorSymbol.mutateToFlattenedVersion();
			}

			// Attach the coroutine to the animator so it's on the SlotSymbols object.
			yield return animatorSymbol.animator.fadingCoroutine = animatorSymbol.animator.StartCoroutine(animatorSymbol.animator.fadeSymbolOutOverTime(duration));
		}
	}

	public void fadeSymbolInImmediate()
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();

		if (animatorSymbol != null && animatorSymbol.animator != null)
		{
			animatorSymbol.animator.fadeSymbolInImmediate();	
		}
	}

	public IEnumerator fadeInSymbolCoroutine(float duration)
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();

		if (animatorSymbol != null && animatorSymbol.animator != null)
		{
			// Attach the coroutine to the animator so it's on the SlotSymbols object.
			yield return animatorSymbol.animator.StartCoroutine(animatorSymbol.animator.fadeSymbolInOverTime(duration));
		}
	}

	// Helper to get the position without the symbol offset 
	// (useful for when you need the positioning of the symbol location
	// and not just the symbol with the offset included)
	public Vector3 getPositionWithoutSymbolInfoOffset(bool isLocal)
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null)
		{
			return animatorSymbol.animator.getPositionWithoutSymbolInfoOffset(isLocal);
		}
		else
		{
			Debug.LogError("SlotSymbol.getPositionWithoutSymbolInfoOffset() - name = " + name + "; isLocal = " + isLocal + "; unable to get animatorSymbol!  Returning Vector3.zero.");
			return Vector3.zero;
		}
	}

	/// Generates a Vector3 ID composed of the reelID, layer, and index on that reel of this symbol
	/// This ID can be used to determine if two mega symbol pieces are part of the same mega symbol
	public Vector3 getSymbolPositionId()
	{
		SlotSymbol animatorSymbol = getAnimatorSymbol();
		if (animatorSymbol == null || animatorSymbol.reel == null)
		{
			Debug.LogError("Something went wrong inside of getSymbolPositionID, aborting with a -1 Vector. This might be a broken mega symbol");
			return new Vector3(-1.0f, -1.0f, -1.0f);
		}
		return new Vector3(animatorSymbol.reel.reelID, animatorSymbol.reel.layer, animatorSymbol.index);
	}

	// This makes the assumption that the symbol is rectangular.
	// And it handles Wide and Tall symbols.
	// [width, height]
	public Vector2 getWidthAndHeightOfSymbol()
	{
		return slotSymbolCacheItem.widthAndHeight;
	}

	// This makes the assumption that the symbol is rectangular.
	// And it handles Wide and Tall symbols.
	// [width, height]
	public static Vector2 getWidthAndHeightOfSymbolFromName(string name)
	{
		return getOrCreateCacheItem(name).widthAndHeight;
	}

	private static Vector2 getWidthAndHeightOfSymbolFromNameActual(string name)
	{
		int width = 1;
		int height = 1;
		string baseName = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		if (SlotSymbol.isTallSymbolPartFromNameActual(baseName)) 
		{
			// M1-YA, the height is the first number here.
			char number = baseName.Substring(baseName.IndexOf('-'))[1];
			height = (int)char.GetNumericValue(number);
		}
		else if (SlotSymbol.isMegaSymbolPartFromNameActual(baseName))
		{
			// M1-YA-XA
			// Get the first number
			char number = baseName.Substring(baseName.IndexOf('-'))[1];
			height = (int)char.GetNumericValue(number);
			// Get the second number
			number = baseName.Substring(baseName.LastIndexOf('-'))[1];
			width = (int)char.GetNumericValue(number);
		}
		else if (string.IsNullOrEmpty(baseName))
		{
			// Trying to get the size of a null string.
			return new Vector2(0.0f, 0.0f);
		}

		// It's a 1x1 symbols
		return new Vector2(width, height);
	}

	// Returns the column that a multi wide symbol is on.
	// returns 1 if it's not wide.
	public int getColumn()
	{
		return slotSymbolCacheItem.column;
	}

	public static int getColumnFromName(string name)
	{
		return getOrCreateCacheItem(name).column;
	}

	// Returns the row that a multi wide symbol is on.
	// returns 1 if it's not wide.
	private static int getColumnFromNameActual(string name)
	{
		int col = 1;
		string baseName = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		if (SlotSymbol.isMegaSymbolPartFromNameActual(baseName))
		{
			// M1-YA-XA
			// Get the second number
			char number = baseName.Substring(baseName.LastIndexOf('-'))[2];
			col = (int) (number - 'A' + 1);
		}
		else if (string.IsNullOrEmpty(baseName))
		{
			// Trying to get the size of a null string.
			return 0;
		}
		return col;
	}

	// Returns the row that a multi tall symbol is on.
	// returns 1 if it's not tall.
	public int getRow()
	{
		return slotSymbolCacheItem.row;
	}

	public static int getRowFromName(string name)
	{
		return getOrCreateCacheItem(name).row;
	}

	// Returns the column that a multi tall symbol is on.
	// returns 1 if it's not tall.
	private static int getRowFromNameActual(string name)
	{
		int row = 1;
		string baseName = SlotSymbol.getNameWithoutSubsymbolFromNameActual(name);
		if (SlotSymbol.isLargeSymbolPartFromNameActual(baseName))
		{
			// M1-YA or M1-YA-XA
			// Get the first number
			char number = baseName.Substring(baseName.IndexOf('-'))[2];
			row = (int) (number - 'A' + 1);
		}
		else if (string.IsNullOrEmpty(baseName))
		{
			// Trying to get the size of a null string.
			return 0;
		}
		return row;
	}

	public bool isBottom
	{
		get
		{
			return ((int)slotSymbolCacheItem.row == (int)slotSymbolCacheItem.widthAndHeight.y);
		}
	}

	public static bool isBottomFromName(string name)
	{
		var item = getOrCreateCacheItem(name);
		return ((int)item.row == (int)item.widthAndHeight.y);
	}

	public bool isTop
	{
		get
		{
			return (slotSymbolCacheItem.row == 1);
		}
	}

	public static bool isTopFromName(string name)
	{
		return (getOrCreateCacheItem(name).row == 1);
	}

	public bool isRight
	{
		get
		{
			return ((int)slotSymbolCacheItem.column == (int)slotSymbolCacheItem.widthAndHeight.x);
		}
	}

	public static bool isRightFromName(string name)
	{
		var item = getOrCreateCacheItem(name);
		return ((int)item.column == (int)item.widthAndHeight.x);
	}

	public bool isLeft
	{
		get
		{
			return (slotSymbolCacheItem.column == 1);
		}
	}

	public static bool isLeftFromName(string name)
	{
		return (getOrCreateCacheItem(name).column == 1);
	}

	public static string constructNameFromDimensions(string name, int width, int height, int row = 1, int column = 1)
	{
		string shortName = SlotSymbol.getShortNameFromName(name);
		string shortServerName = SlotSymbol.getShortServerNameFromName(name);
		string shortServerNameWithTWandR = shortServerName;
		if (hasRPrefixFromName(name))
		{
			shortServerNameWithTWandR = R_PREFIX + shortServerNameWithTWandR;
		}
		if (hasTWPostfixFromName(name))
		{
			shortServerNameWithTWandR = shortServerNameWithTWandR + TW_POSTFIX;
		}
		string constructedShortName = shortName.Replace(shortServerName, shortServerNameWithTWandR);
		if (constructedShortName != name)
		{
			Debug.LogError("Constructing a name with a malformed name.");
		}
		if (height < 1 || width < 1)
		{
			Debug.LogError("Invalid Height or Width");
			return constructedShortName;
		}
		if (height == 1 && width == 1)
		{
			return constructedShortName;
		}

		name = string.Format("{0}-{1}{2}", constructedShortName, height, (char)('A' + (row - 1)));
		if (width > 1)
		{
			name = string.Format("{0}-{1}{2}", name, width, (char)('A' + (column - 1)));
		}
		return name;
	}

	private void setAnimatingDelegate(SymbolAnimator.AnimatingDelegate callback)
	{
		animatingCallback = callback;
	}

	public IEnumerator tumbleDown(int targetSymbolIndex, float speed = 10.0f, iTween.EaseType type = iTween.EaseType.easeOutBounce, float y = -6.0f, SymbolAnimator.OnSymbolTweenFinishDelegate onFinish = null)
	{
		if (animator != null && animator.gameObject != null)
		{			
			if (onFinish != null)
			{
				animator.setTweenFinishDelegateAndTargetSymbol(onFinish, this);
			}

			// retain symbol depth positioning if using that option
			if (_reelGame.isLayeringSymbolsByDepth)
			{
				float layerByDepthAdjust = (info != null) ? info.layerByDepthAdjust : 0.0f;
				int invertedSymbolIndex = (reel.reelData.visibleSymbols - 1) - targetSymbolIndex;
				animator.gameObject.transform.position = animator.gameObject.transform.position - new Vector3(0.0f, 0.0f, animator.gameObject.transform.position.z) + new Vector3(0.0f, 0.0f, ((invertedSymbolIndex - reel.numberOfTopBufferSymbols) * SlotReel.DEPTH_ADJUSTMENT) + (layerByDepthAdjust * SlotReel.DEPTH_ADJUSTMENT));
				float templatePositionZAdjust = (info != null) ? info.positioning.z : 0.0f;
				animator.gameObject.transform.localPosition = animator.gameObject.transform.localPosition + new Vector3(0.0f, 0.0f, templatePositionZAdjust);
			}

			iTween.MoveBy(animator.gameObject, iTween.Hash("y", y, "islocal", true, "speed", speed, "easetype", type, "onComplete", "onSymbolTweenComplete"));
			yield return new TIWaitForSeconds (.1f);
		}
	}

	public IEnumerator doTumbleSquashAndSquish()
	{
		float landScaleAmountX = .5f;
		float landScaleAmountY = 1.5f;
		float landScaleAmountZ = 1.5f;
		float landScaleTime1 = .1f;

		float landScaleAmount2X = 1.5f;
		float landScaleAmount2Y = .5f;
		float landScaleAmount2Z = .5f;
		float landScaleTime2 = .2f;

		float landScaleTime3 = .15f;
		iTween.EaseType landEaseType1 = iTween.EaseType.easeInSine;
		iTween.EaseType landEaseType2 = iTween.EaseType.easeInSine;
		iTween.EaseType landEaseType3 = iTween.EaseType.easeInSine;
		
		if (gameObject != null && animator.gameObject != null)
		{
			animator.isTumbleSquashAndStretching = true;
			GameObject scalePivot = animator.gameObject;
			Vector3 originalScale = scalePivot.transform.localScale;
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", new Vector3(originalScale.x * landScaleAmountX, originalScale.y * landScaleAmountY, originalScale.z * landScaleAmountZ), "time", landScaleTime1, "easetype", landEaseType1));
			yield return new TIWaitForSeconds(landScaleTime1);
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", new Vector3(originalScale.x * landScaleAmount2X, originalScale.y * landScaleAmount2Y, originalScale.z * landScaleAmount2Z), "time", landScaleTime2, "easetype", landEaseType2));
			yield return new TIWaitForSeconds(landScaleTime2);
			iTween.ScaleTo (scalePivot, iTween.Hash("scale", originalScale, "time", landScaleTime3, "easetype", landEaseType3));
			yield return new TIWaitForSeconds(landScaleTime3);
		}
		animator.isTumbleSquashAndStretching = false;
	}

	public IEnumerator fallDown(int targetSymbolIndex, float speed = 10.0f, iTween.EaseType type = iTween.EaseType.easeOutBounce, float y = -6.0f, SymbolAnimator.OnSymbolTweenFinishDelegate onFinish = null)
	{
		if (animator != null && animator.gameObject != null)
		{
			if (onFinish != null)
			{
				animator.setTweenFinishDelegateAndTargetSymbol(onFinish, this);
			}

			// retain symbol depth positioning if using that option
			if (_reelGame.isLayeringSymbolsByDepth)
			{
				float layerByDepthAdjust = (info != null) ? info.layerByDepthAdjust : 0.0f;
				int invertedSymbolIndex = (reel.reelData.visibleSymbols - 1) - targetSymbolIndex;
				animator.gameObject.transform.position = animator.gameObject.transform.position - new Vector3(0.0f, 0.0f, animator.gameObject.transform.position.z) + new Vector3(0.0f, 0.0f, ((invertedSymbolIndex - reel.numberOfTopBufferSymbols) * SlotReel.DEPTH_ADJUSTMENT) + (layerByDepthAdjust * SlotReel.DEPTH_ADJUSTMENT));
				float templatePositionZAdjust = (info != null) ? info.positioning.z : 0.0f;
				animator.gameObject.transform.localPosition = animator.gameObject.transform.localPosition + new Vector3(0.0f, 0.0f, templatePositionZAdjust);
			}

			iTween.MoveTo(animator.gameObject, iTween.Hash("y", y, "islocal", true, "speed", speed, "easetype", type, "onComplete", "onSymbolTweenComplete"));
			yield return new TIWaitForSeconds (.1f);
		}
	}

	// Tween the animator object to a new local z position (most likely +10)
	public IEnumerator plopDown(float seconds = 1.0f, iTween.EaseType type = iTween.EaseType.linear, float z = 10.0f, SymbolAnimator.OnSymbolTweenFinishDelegate onFinish = null)
	{
		if (animator != null && animator.gameObject != null)
		{
			if (onFinish != null)
			{
				animator.setTweenFinishDelegateAndTargetSymbol(onFinish, this);
			}
			iTween.MoveTo(animator.gameObject, iTween.Hash("z", z, "islocal", true, "time", seconds, "easetype", type));
			yield return new TIWaitForSeconds(.1f);

			GameObject scalePivot = animator.gameObject.transform.Find("ScalePivot").gameObject;
			if (scalePivot != null)
			{
				iTween.MoveTo(scalePivot, iTween.Hash("y", -.2f, "islocal", true, "time", seconds, "eastype", type, "onComplete", "onSymbolTweenComplete"));
			}
			else
			{
				Debug.LogWarning("Cannot MoveTo to null ScalePivot");
			}
		}

		// If it's a farmville symbol that we're plopping, make sure to update the rotation of it as we plop it
		if (animator is FarmVilleSymbolAnimator)
		{
			float startTime = Time.time;

			while (Time.time - startTime < seconds)
			{
				(animator as FarmVilleSymbolAnimator).updateRotation();
				yield return null;
			}

			(animator as FarmVilleSymbolAnimator).markAsPlopped();
		}
		else
		{
			yield return new WaitForSeconds(seconds);
		}
	}

	// Tween the animator object to a new local z position (most likely 0) - same as plopDown except for default z value
	public IEnumerator plopUp(float seconds = 1.0f, iTween.EaseType type = iTween.EaseType.linear, float z = 0.0f)
	{
		iTween.MoveTo(animator.gameObject, iTween.Hash("z", z, "islocal", true, "time", seconds, "easetype", type));
		yield return new WaitForSeconds(seconds);
	}

	public IEnumerator raiseUp(float seconds = 1.0f, iTween.EaseType type = iTween.EaseType.linear, float z = 0.0f)
	{
		iTween.MoveBy(animator.gameObject, iTween.Hash("z", z, "islocal", true, "time", seconds, "easetype", type));
		yield return new WaitForSeconds(seconds);
	}

	/// Does this symbol have a responsible animator?
	public bool hasAnimator
	{
		get 
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			if (animateSymbol == null || animateSymbol.animator == null)
			{
				return false;
			}
			return true;
		}
	}
	
	/// Is the SymbolAnimator responsible for this symbol's animation doing anything right now?
	public bool isAnimatorDoingSomething
	{
		get
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			if (animateSymbol == null || animateSymbol.animator == null)
			{
				return false;
			}
			return animateSymbol.animator.isDoingSomething;
		}
	}
	
	/// Is the SymbolAnimator responsible for this symbol's animation mutating right now?
	public bool isAnimatorMutating
	{
		get
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			if (animateSymbol == null || animateSymbol.animator == null)
			{
				return false;
			}
			return animateSymbol.animator.isMutating;
		}
	}

	private ReelGame _reelGame = null;

	private List<AnimateDoneDelegate> animationCallbacks = new List<AnimateDoneDelegate>();

	public SlotSymbol(ReelGame reelGame)
	{
		_reelGame = reelGame;
		setAnimatingDelegate(_reelGame.symbolAnimatingCallback);
	}

	/// Is this symbol animating?
	public bool isAnimating
	{
		get
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			if (animateSymbol == null || animateSymbol.animator == null)
			{
				return false;
			}
			return animateSymbol.animator.isAnimating;
		}
	}

	private bool needsRefresh = false;
	private bool needsNewInstance = false;

	/// Setup a symbol with a new identity
	public void setupSymbol(string newName, int newIndex, SlotReel newReel, Dictionary<string, string> normalReplacementSymbolMap = null, Dictionary<string, string> megaReplacementSymbolMap = null, bool allowFlattenedSymbolSwap = false, bool hasReplacementSymbolDataReady = true)
	{
		Profiler.BeginSample("setupSymbol");

		debug = "";
		needsRefresh = true;
		needsNewInstance = true;
		debugName = getServerNameFromName(newName);

		string subsymbolName = getSubsymbolFromName(newName);
		name = newName.Replace(SUBSYMBOL_DELIMITER + subsymbolName, "");

		bool replacedSymbol = false;

		name = getReplacedSymbolName(name, normalReplacementSymbolMap, megaReplacementSymbolMap);

		if (hasReplacementSymbolDataReady && name.Contains("RP"))
		{
			Debug.LogWarning("SlotSymbol.setupSymbol() - RP symbol wasn't replaced for " + name + "; newReel.layer = " + newReel.layer + "; newReel.reelID = " + newReel.reelID);
		}

		if (replacedSymbol)
		{
			// Put the old name into the debug info so we know where it came from
			debug = debugName;
		}

		// check to see if the top of this symbol is unflattened, and if so mimic that
		// this will ensure that if additional parts are added for a mega/tall symbol
		// they take into account if that tall/mega was already unflattened
		bool isFullSymbolFlattened = true;
		if (isLargeSymbolPart && reel != null && reel.symbolList != null)
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			if (animateSymbol != null && animateSymbol != this)
			{
				if (!animateSymbol.isFlattenedSymbol)
				{
					isFullSymbolFlattened = false;
				}
			}
		}

		if (allowFlattenedSymbolSwap && isFullSymbolFlattened)
		{
			// Check if we can swap out the symbol for an optimized flattened symbol
			Vector2 symbolSize = getWidthAndHeightOfSymbol();
			string originalPrefix = "";
			string originalPostfix = "";
			if (name.Contains("R-"))
			{
				originalPrefix = "R-";
			}
			if (name.Contains("-TW"))
			{
				originalPostfix = "-TW";
			}

			string symbolNameWithFlattenedExtension = constructNameFromDimensions(shortServerName + FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
			SymbolInfo info = _reelGame.findSymbolInfo(symbolNameWithFlattenedExtension);
			if (info != null)
			{
				name = constructNameFromDimensions(originalPrefix + shortServerName + originalPostfix + FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y, getRow(), getColumn());
			}
		}

		index = newIndex;
		reel = newReel;
		refreshSymbol(0);

		if (hasReplacementSymbolDataReady && name.Contains("RP"))
		{
			Debug.LogError("RP symbol wasn't replaced for " + name);
		}

		if (!string.IsNullOrEmpty(subsymbolName))
		{
			setUpSubsymbol(subsymbolName, newIndex, newReel, normalReplacementSymbolMap, megaReplacementSymbolMap, allowFlattenedSymbolSwap);
			name += SUBSYMBOL_DELIMITER + subsymbol.name;
		}

		if (_reelGame.engine is LayeredSlotEngine || _reelGame.engine.reelSetData.isIndependentReels)
		{
			// In these types of games we need to ensure that the symbol matches the layer of the reel it is placed on
			setSymbolLayerToParentReelLayer();
		}

		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteAfterSymbolSetup(this))
			{
				module.executeAfterSymbolSetup(this);
			}
		}

		Profiler.EndSample();
	}

	// Sets a sybmol's layer to match the layer of the parent reel it is on
	// NOTE: If you plan to restore the layer you'll need to save it out before calling this
	public void setSymbolLayerToParentReelLayer()
	{
		if (gameObject != null && gameObject.transform.parent != null)
		{
			GameObject animatorParent = gameObject.transform.parent.gameObject;
			CommonGameObject.setLayerRecursively(gameObject, animatorParent.layer);
		}
	}

	// Helper method to call setup Symbol on subsymbols, and set some basic information for them so it's easier to see what's happening in the scene.
	private void setUpSubsymbol(string subsymbolName, int newIndex, SlotReel newReel, Dictionary<string, string> normalReplacementSymbolMap = null, Dictionary<string, string> megaReplacementSymbolMap = null, bool allowFlattenedSymbolSwap = false)
	{
		if (!string.IsNullOrEmpty(subsymbolName))
		{
			// Only set up the symbol if there's a subsymbol to make.
			subsymbol = new SlotSymbol(_reelGame);
			subsymbol.setupSymbol(subsymbolName, newIndex, newReel, normalReplacementSymbolMap, megaReplacementSymbolMap, allowFlattenedSymbolSwap);
			subsymbol.parentSymbol = this;
		}

		if (hasAnimator && subsymbol != null && subsymbol.hasAnimator)
		{
			subsymbol.transform.parent = transform;
			subsymbol.gameObject.name = "Subsymbol " + subsymbol.name;
		}
	}

	public void transferSymbol(SlotSymbol newSymbol, int newIndex, SlotReel newReel)
	{
		needsRefresh = true;
		setNameAndCacheItem(newSymbol.name, newSymbol.slotSymbolCacheItem);
		this.debugName = newSymbol.debugName;
		this.animator = newSymbol.animator;
		this.parentSymbol = newSymbol.parentSymbol;
		this.subsymbol = newSymbol.subsymbol;
		newSymbol.subsymbol = null;
		if (this.subsymbol != null)
		{
			this.subsymbol.parentSymbol = this;
			this.subsymbol.index = newIndex;
			this.subsymbol.reel = newReel;
		}
		this.index = newIndex;
		this.reel = newReel;
		if (reel == null)
		{
			Debug.LogError("No reel set for symbol!");
		}
		this.debug = newSymbol.debug;
		this.debugSymbolInsertionIndex = newSymbol.debugSymbolInsertionIndex;
	}

	/// Move a symbol to a new reel, with a new index. But don't change the name of the symbol so we don't have to get a new instance.
	public void changeSymbolReel(int newIndex, SlotReel newReel)
	{
		needsRefresh = true;
		index = newIndex;
		reel = newReel;
	}

	// For plopping, we replace the name and SymbolAnimator of a SlotSymbol after destroying the previous SymbolAnimator
	public void setNameAndAnimator(string newName, SymbolAnimator newAnimator)
	{
		name = newName;
		debugName = name;
		animator = newAnimator;
		if (animator != null)
		{
			animator.setAnimatingDelegate(animatingCallback);
		}
	}

	public void cleanUp()
	{
		debug = "";
		if (animator != null)
		{
			_reelGame.releaseSymbolInstance(animator);
			animator = null;
		}
		if (subsymbol != null)
		{
			subsymbol.cleanUp();
			subsymbol = null;
		}
		if (parentSymbol != null)
		{
			parentSymbol.subsymbol = null;
			parentSymbol = null;
		}

	}

	/// Costly instance change handling
	public void refreshSymbol(float offset, bool force = false)
	{
		Profiler.BeginSample("refreshSymbol");

		if (needsRefresh || force)
		{

			// If this symbol has a different name, get the new GameObject representing it
			if (needsNewInstance || force)
			{
				// In Dark Desires, the "R-" name is prefixed onto any mystery symbol. We're just forcing the
				// R symbol to get used initially, as the first visible reel. The regular name is still safe and referenced later.
				string instanceName;
				if (name.Contains("R-"))
				{
					SymbolInfo info = _reelGame.findSymbolInfo("R_Flattened");
					if (info != null) //Use the flattened version if it exists.
					{
						instanceName = "R_Flattened";
					}
					else
					{
						instanceName = "R";
					}
				}
				else if (name.Contains(BINGO_SYMBOL_BASE_NAME)) //If we're loading a Bingo number symbol then we grab the one common instance which is just the symbol name without the # postfix
				{
					instanceName = BINGO_SYMBOL_BASE_NAME;
				}
				// Same as above, but for Duck Dynasty
				else
				{
					instanceName = baseName; // We want to remove the -TW from the name, but keep everything else.
				}
				
				if (_reelGame != null)
				{
					if(reel == null)
					{
						animator = _reelGame.getSymbolAnimatorInstance(instanceName, -1);
					}
					else
					{
						animator = _reelGame.getSymbolAnimatorInstance(instanceName, reel.reelID);
					}
					
					if (animator != null)
					{
						animator.setAnimatingDelegate(animatingCallback);
					}
				}

				needsNewInstance = false;
			}
			needsRefresh = false;
		}

		// Always reposition
		if (reel != null)
		{
			reel.setSymbolPosition(this, offset);
		}

		Profiler.EndSample();
	}

	/// Halts the animation of this symbol.
	public void haltAnimation(bool force = false)
	{
		bool wasAnimating = false;

		if (animator != null)
		{
			wasAnimating = animator.isAnimating;
			animator.stopAnimation(force);
		}

		if (wasAnimating)
		{
			animationDone();
		}
	}

	/// Animate the anticipation animation for this symbol.
	public void animateAnticipation(AnimateDoneDelegate callback = null, float delay = 0.0f)
	{
		if (delay == 0.0f)
		{
			animateAnticipationNoDelay(callback);
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(animateAnticipationWithDelay(callback, delay));
		}
	}

	private void animateAnticipationNoDelay(AnimateDoneDelegate callback)
	{
		haltAnimation();

		SlotSymbol animateSymbol = getAnimatorSymbol();
		
		if (animateSymbol == null || animateSymbol.animator == null ||
			animateSymbol.animator.info.anticipationAnimation == SymbolAnimationType.NONE)
		{
			// There is no animation to play, carry on with the logic.
			if (callback != null)
			{
				callback(this);
			}
		}
		else if (animateSymbol == this)
		{
			// if this is a flattened symbol, we need to swap it out before the animation happens
			mutateToUnflattenedVersion();

			// This symbol actually plays the animation.
			if (callback != null)
			{
				animationCallbacks.Add(callback);
			}
			animator.playAnticipation(this);
		}
		else
		{
			// A different symbol houses the animation, so call animateAnticipation() on it instead.
			animateSymbol.animateAnticipation(callback);
		}

		if (subsymbol != null)
		{
			subsymbol.animateAnticipation(callback);
		}
	}

	/// A delayed playing of the anticipation animation, that is canceled if the user slam stops
	private IEnumerator animateAnticipationWithDelay(AnimateDoneDelegate callback = null, float delay = 0.0f)
	{
		float elapsedTime = 0;
		
		while (elapsedTime < delay && !_reelGame.engine.isSlamStopPressed)
		{
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		// only play the animation if a slam stop didn't happen while it was waiting to animate
		if (!_reelGame.engine.isSlamStopPressed)
		{
			animateAnticipationNoDelay(callback);
		}
	}

	// call animateAnticipation and then wait until the symbol says it isn't animating anymore
	public IEnumerator playAndWaitForAnimateAnticipation(AnimateDoneDelegate callback = null, float delay = 0.0f)
	{
		animateAnticipation(callback, delay);

		// need to mimic the delay code here (otherwise this may not end at the correct time)
		if (delay != 0.0f)
		{
			float elapsedTime = 0;
		
			while (elapsedTime < delay && !_reelGame.engine.isSlamStopPressed)
			{
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			// wait an additional frame to hopefully ensure the coroutine that will actually start the animation will go first
			yield return null;
		}

		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			// wait for the symbol to stop animating
			while (animateSymbol.animator.isAnimating)
			{
				yield return null;

				if (animateSymbol == null || animateSymbol.animator == null)
				{
					Debug.LogWarning("animator disappeared while animating symbol" + serverName);
					break;
				}
			}
		}
	}

	// call animateOutcome and then wait until the symbol says it isn't animating anymore
	public IEnumerator playAndWaitForAnimateOutcome(AnimateDoneDelegate callback = null)
	{
		animateOutcome(callback);

		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			// wait for the symbol to stop animating
			while (animateSymbol.animator != null && animateSymbol.animator.isAnimating)
			{
				yield return null;
			}
		}
	}

	/// Animate the outcome animation for this symbol.
	public void animateOutcome(AnimateDoneDelegate callback = null)
	{
		haltAnimation();

		if (_reelGame.outcomeDisplayController.isPlayingOutcomeAnimSoundsThisSpin &&
		    !_reelGame.outcomeDisplayController.hasPreWinShowOutcome())
		{
			if (_reelGame.needsToPlaySymbolSoundOnAnimateOutcome(this))
			{
				_reelGame.playPlaySymbolSoundOnAnimateOutcome(this);
			}
		}

		SlotSymbol animateSymbol = getAnimatorSymbol();
		
		if (animateSymbol == null || animateSymbol.animator == null ||
			animateSymbol.animator.info.outcomeAnimation == SymbolAnimationType.NONE)
		{
			// There is no animation to play, carry on with the logic.
			if (callback != null)
			{
				callback(this);
			}
		}
		else if (animateSymbol == this)
		{
			// Check if this symbol needs to swap to an _Outcome version
			// find out if we have a custom bonus _Outcome symbol to swap in and play
			Vector2 symbolSize = getWidthAndHeightOfSymbol();
			string symbolNameWithOutcomeExtension = constructNameFromDimensions(shortServerName + OUTCOME_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
			SymbolInfo info = _reelGame.findSymbolInfo(symbolNameWithOutcomeExtension);

			//Mutating symbol loses isSkippingAnimations flag so hold the value
			bool isAnimatorSkippingAnimation = animator.isSkippingAnimations;

			// Don't try to auto convert to _Outcome if this is a special type like _Award.  If
			// it is an _Award it will be up to non automatic code to handle swapping to _Outcome if that is desired.
			bool didConvertToOutcomeSymbol = false;
			if (!isAwardSymbol)
			{
				didConvertToOutcomeSymbol = tryConvertSymbolToOutcomeSymbol(true);
			}

			if (!didConvertToOutcomeSymbol)
			{
				// no outcome symbol, now check if this is a flattened version that isn't going to have animations on it, and swap it to a normal version if it is
				mutateToUnflattenedVersion();
			}

			//If symbol was supposed to skip animation, set the flag again to mutated symbol
			if (isAnimatorSkippingAnimation)
			{
				skipAnimationsThisOutcome();
			}
			
			// This symbol actually plays the animation.
			if (callback != null)
			{
				animationCallbacks.Add(callback);
			}

			animator.playOutcome(this);
		}
		else
		{
			// A different symbol houses the animation, so call animateOutcome() on it instead.
			animateSymbol.animateOutcome(callback);
		}

		if (subsymbol != null)
		{
			subsymbol.animateOutcome(callback);
		}
	}

	/// Tells the symbol to skip any animation calls that happen during an outcome, usually in response to being covered by a banner
	public void skipAnimationsThisOutcome()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			animateSymbol.animator.isSkippingAnimations = true;
		}
	}

	// do we need to call animateOutcome twice on this symbol (returns true if it's a wide & tall symbol that has not been broken up yet)
	public bool needsToRepeatAnimationCall()
	{
		if (!isWhollyOnScreen && isLargeSymbolPart)
		{
			SlotSymbol animateSymbol = getAnimatorSymbol();
			SymbolInfo info = _reelGame.findSymbolInfo(animateSymbol.name);
			return info.isSymbolSplitable;
		}
		return false; // is it a wide symbol that it partially off screen
	}

	/// Helper function to mutate to an unflattened version of the symbol
	public void mutateToUnflattenedVersion()
	{
		if (isFlattenedSymbol)
		{
			mutateTo(serverName, null, true, true);
		}
	}

	/// Ease of use function for mutating a symbol to a flattened version, if one exists, if it doesn't this call does nothing
	public void mutateToFlattenedVersion(AnimateDoneDelegate callback = null, bool playVfx = true, bool skipAnimation = false, bool canRandomSplit = false)
	{
		// Check if we can swap out the symbol for an optimized flattened symbol
		Vector2 symbolSize = getWidthAndHeightOfSymbol();
		string symbolNameWithFlattenedExtension = constructNameFromDimensions(shortServerName + FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		SymbolInfo info = _reelGame.findSymbolInfo(symbolNameWithFlattenedExtension);
		if (info != null)
		{
			mutateTo(symbolNameWithFlattenedExtension, callback, playVfx, skipAnimation, canRandomSplit);
		}
		else
		{
			Debug.LogWarning("SlotSymbol.mutateToFlattenedVersion() - Couldn't find flattened symbol templete: " + symbolNameWithFlattenedExtension);
		}
	}

	/// Call to start up the sequence of mutating a symbol.
	public void mutateTo(string newName, AnimateDoneDelegate callback = null, bool playVfx = true, bool skipAnimation = false, bool canRandomSplit = false)
	{
		if (name == null || newName == null)
		{
			if (newName == null)
			{
				Debug.LogWarning("Calling mutateTo with a null name");
			}
			else
			{
				Debug.LogWarning("Calling a mutateTo on a symbol that doesn't have a name.");
			}
			return;
		}

		if (name == newName)
		{
			Debug.LogWarning("Trying to mutate " + name + " to itself.");
			return;
		}

		haltAnimation();

		SlotSymbol myParent = parentSymbol;
		
		SlotSymbol animateSymbol = getAnimatorSymbol();

		int oldLayer = -1;
		string oldLabelText = ""; //If this symbol has a label attached that is being dynamically set then store it so we can use it on the new symbol

		if (animateSymbol != null && animateSymbol.animator != null && animateSymbol.animator.getDynamicLabel() != null)
		{
			oldLabelText = animateSymbol.animator.getDynamicLabel().text;
		}

		if (animateSymbol != null && animateSymbol.animator != null && animateSymbol.animator.gameObject != null)
		{
			oldLayer = animateSymbol.animator.gameObject.layer;
		}

		if (skipAnimation)
		{
			mutateSwitchOver(newName, playVfx, skipAnimation, canRandomSplit);
		}
		else
		{
			if (animateSymbol == null || animateSymbol.animator == null ||
				animateSymbol.animator.info.mutateFromAnimation == SymbolAnimationType.NONE ||
				animateSymbol.animator.info.mutateToAnimation == SymbolAnimationType.NONE)
			{
				// There is no animation to play, carry on with the logic.
				if (callback != null)
				{
					animationCallbacks.Add(callback);
				}
				mutateSwitchOver(newName, playVfx, false, canRandomSplit);
			}
			else if (animateSymbol == this)
			{
				// This symbol actually plays the animation.
				if (callback != null)
				{
					animationCallbacks.Add(callback);
				}
				animator.playMutateFrom(this, newName, playVfx);
			}
			else
			{
				// A different symbol houses the animation, so call mutateTo() on it instead.
				animateSymbol.mutateTo(newName, callback, playVfx);
			}
		}

		// Ensure that the mutated objects is put on the same layer as the symbol before it was mutated.
		if (oldLayer >= 0 && animateSymbol != null && animateSymbol.animator != null)
		{
			if (info != null && !info.keepObjectLayeringOnMutateTo)
			{
				CommonGameObject.setLayerRecursively(animateSymbol.animator.gameObject, oldLayer);
			}
		}

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			LabelWrapperComponent mutatingLabel = animateSymbol.animator.getDynamicLabel();
			if (!oldLabelText.IsNullOrWhiteSpace() && mutatingLabel != null) //Set the label text on the new symbol
			{
				mutatingLabel.text = oldLabelText;
				mutatingLabel.forceUpdate();
			}
		}

		if (myParent != null)
		{
			animateSymbol.parentSymbol = myParent;
			animateSymbol.parentSymbol.subsymbol = this;
		}
	}

	/// Call to force stop an animation on a symbol.
	/// ALWAYS prefer this to calling directly on the .animator as this will deal with multi-part symbols
	/// NOTE: You should always call halt aniamtion since that will handle the correct callbacks.
	private void stopAnimation()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			animateSymbol.animator.stopAnimation();
		}
		else
		{
			Debug.LogWarning("stopAnimation() was called on a symbol which didn't have an animator!");
		}
	}

	/// Call to convert a symbol to use the wild overlay or wild texture.
	/// ALWAYS prefer this to calling directly on the .animator as this will deal with multi-part symbols
	public void showWild()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		// ensure this symbol is unflattened because adding a wild overlay implies that we expect to animate this symbol
		mutateToUnflattenedVersion();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			animateSymbol.animator.showWild();
		}
		else
		{
			Debug.LogWarning("showWild() was called on a symbol which didn't have an animator!");
		}
	}

	/// Call to hide the wild overlay or wild texture.
	/// ALWAYS prefer this to calling directly on the .animator as this will deal with multi-part symbols
	public void hideWild()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null && animateSymbol.animator != null)
		{
			animateSymbol.animator.hideWild();
		}
		else
		{
			Debug.LogWarning("hideWild() was called on a symbol which didn't have an animator!");
		}
	}

	/// Called by SymbolAnimator when the mutate-from-this animation completes in
	/// order to switch the symbol and continue with the mutate-to-this animation.
	/// Mutating to something that's bigger than the current number of visible symbols for a reel is going to have undefined effects.
	public void mutateSwitchOver(string newName, bool playVfx = true, bool skipAnimation = false, bool canRandomSplit = false)
	{
		// Save old callback list and replace it with a blank list
		List<AnimateDoneDelegate> oldList = animationCallbacks;
		animationCallbacks = new List<AnimateDoneDelegate>();

		// track if this symbol was showing a wild overlay
		bool wasWildOverlayShowing = isWildShowing;
		
		// split the symbol ONLY if the new symbol is starting in the same spot and is smaller than the current symbol
		// i.e. we have a 3x3 which is going to a 2x2
		Vector2 currentSymbolSize = getWidthAndHeightOfSymbol();
		Vector2 newSymbolSize = SlotSymbol.getWidthAndHeightOfSymbolFromName(newName);
		int newSymbolRow = SlotSymbol.getRowFromName(newName);
		int newSymbolColumn = SlotSymbol.getColumnFromName(newName);
		if (getRow() != newSymbolRow || getColumn() != newSymbolColumn || newSymbolSize.x < currentSymbolSize.x || newSymbolSize.y < currentSymbolSize.y)
		{
			if (canBeSplit())
			{
				splitSymbol();
			}
		}

		// default value for blank symbols
		Vector3 savedPosition = Vector3.zero;

		// save the local position for later restoration on tumble mutations
		if (gameObject != null)
		{
			savedPosition = gameObject.transform.localPosition;
		}

		// cleanup the current symbol and swap it for the new one
		cleanUp();

		if (_reelGame.isLegacyTumbleGame)
		{
			// because setupSymbol will move the symbols in tumble games we need to move them back to where they tumbled from
			setupSymbol(newName, index, reel);
			transform.localPosition = savedPosition;
		}
		else
		{
			setupSymbol(newName, index, reel);
		}
		
		string oldShortName = shortName;
		debug = "Mutated";

		SlotSymbol animatorSymbol = getAnimatorSymbol();
		// now determine if this is a large symbol and needs to clean out the stuff below and to the left of it
		if (animatorSymbol != null)
		{
			Vector2 size = animatorSymbol.getWidthAndHeightOfSymbol();

			// Update the buffer symbols since we mutated a large symbol.
			if (!(ReelGame.activeGame is TumbleFreeSpinGame || ReelGame.activeGame is TumbleSlotBaseGame))
			{
				// We don't want to change the reel sizes for tumble or plop games.
				SlotReel animatorSymbolReel = animatorSymbol.reel;
				if (animatorSymbolReel != null)
				{
					int animatorSymbolReelId = animatorSymbolReel.reelID - 1;
					for (int i = animatorSymbolReelId; i < animatorSymbolReelId + size.x; i++)
					{
						SlotReel reelToAdjust = _reelGame.engine.getSlotReelAt(i, animatorSymbol.getColumn(), animatorSymbol.reel.layer);

						// If the buffer symbols are set to 0, we don't want to add buffer symbols while playing outcome animations
						if (reelToAdjust != null && reelToAdjust.numberOfTopBufferSymbols != 0)
						{
							// This reel doesn't exist for mega symbols in independent reel games.
							int maxBufferSymbol = Mathf.Max((int)size.y, reelToAdjust.numberOfTopBufferSymbols);
							if (reelToAdjust.numberOfTopBufferSymbols != maxBufferSymbol)
							{
								reelToAdjust.updateReelSize(reelToAdjust.reelData.reelStrip, reelToAdjust.reelData.visibleSymbols, maxBufferSymbol, reelToAdjust.numberOfBottomBufferSymbols);
							}
						}
					}
				}
			}

			// only do this handling for symbols that are large
			if (SlotSymbol.isLargeSymbolPartFromName(newName) && reel != null)
			{
				SlotReel[]reelArray = _reelGame.engine.getReelArray();

				for (int x = 0; x < size.x; x++)
				{
					int reelIndex = (reel.reelID - 1) + x;

					if (reelIndex < reelArray.Length)
					{
						SlotReel currentReel = _reelGame.engine.getSlotReelAt(reelIndex, animatorSymbol.getColumn(), reel.layer);

						for (int y = 0; y < size.y; y++)
						{
							// account for buffer symbol differeces with an adjustment
							int bufferSymbolAdjust = currentReel.numberOfTopBufferSymbols - reel.numberOfTopBufferSymbols;

							int symbolIndex = index + y + bufferSymbolAdjust;

							if (symbolIndex < currentReel.symbolList.Count)
							{
								SlotSymbol currentSymbol = currentReel.symbolList[symbolIndex];
								string newSymbolPartName = "";

								if (SlotSymbol.isTallSymbolPartFromName(newName))
								{
									newSymbolPartName = oldShortName + '-' + size.y + (char)('A' + y);
								}
								else if (SlotSymbol.isMegaSymbolPartFromName(newName))
								{
									newSymbolPartName = oldShortName + '-' + size.y + (char)('A' + y) + '-' + size.x + (char)('A' + x);
								}
								
								// need to try and split in case we are overlapping another large symbol, but only if this symbol isn't fully covered up
								if (!isOverlappedSymbolFullyCovered(newSymbolPartName, currentSymbol.name))
								{
									if (currentSymbol.canBeSplit())
									{
										currentSymbol.splitSymbol();
									}
									else if (canRandomSplit && isLargeSymbolPartFromName(currentSymbol.name))
									{
										// we have a problem, a large symbol needs to split but doesn't have 1x1 version to split to.
										// So as a fallback we will replace it with random symbols valid for the reel strip 
										// the symbol is on by calling splitSymbolToRandomSymbols()
										currentSymbol.splitSymbolToRandomSymbols();
									}
								}

								// apply the part name to this symbol
								currentSymbol.cleanUp();
								if (SlotSymbol.isTallSymbolPartFromName(newName) || SlotSymbol.isMegaSymbolPartFromName(newName))
								{
									if (_reelGame.isLegacyTumbleGame)
									{
										// because setupSymbol will move the symbols in tumble games we need to move them back to where they tumbled from
										savedPosition = currentSymbol.gameObject.transform.localPosition;
										currentSymbol.setupSymbol(newSymbolPartName, symbolIndex, currentReel);
										currentSymbol.gameObject.transform.localPosition = savedPosition;
									}
									else
									{
										currentSymbol.setupSymbol(newSymbolPartName, symbolIndex, currentReel);
									}
								}
								else
								{
									Debug.LogError("Trying to mutate multiple symbols from a symbol that isn't tall or wide: " + newName);
								}

								// refresh the part
								if (_reelGame.isLegacyTumbleGame)
								{
									// because refreshSymbol will move the symbols in tumble games we need to move them back to where they tumbled from
									savedPosition = currentSymbol.gameObject.transform.localPosition;
									currentSymbol.refreshSymbol(0f);
									currentSymbol.gameObject.transform.localPosition = savedPosition;
								}
								else
								{
									currentSymbol.refreshSymbol(0.0f);
								}
							}
						}
					}
				}
			}
		}

		if (_reelGame.isLegacyTumbleGame)
		{
			// because refreshSymbol will move the symbols in tumble games we need to move them back to where they tumbled from
			savedPosition = gameObject.transform.localPosition;
			refreshSymbol(0f);
			transform.localPosition = savedPosition;
		}
		else
		{
			refreshSymbol(0f);
		}

		if (animatorSymbol != null && animatorSymbol.animator != null && reel != null)
		{
			// readjust the depth, in case it was changed by mutating (this will ensure symbols that are pushed forward remain that way)
			SlotReel.adjustDepthOfSymbolAnimatorAtSymbolIndex(_reelGame, animatorSymbol.animator, info, index, this.reel.reelID - 1, _reelGame.isLayeringSymbolsByDepth, _reelGame.isLayeringSymbolsByReel, _reelGame.isLayeringSymbolsCumulative, this.reel.numberOfTopBufferSymbols);
		}

		// reapply the wild overlay if it was on
		if (wasWildOverlayShowing)
		{
			showWild();
		}

		// Restore old callbacks list
		animationCallbacks = oldList;

		if (animator == null || skipAnimation)
		{
			// Nothing to animate
			animationDone();
		}
		else
		{
			// Play animation
			animateMutateTo();

            if (playVfx)
            {
                // Play visual effect
                animator.playVfx();
            }
		}
	}

	// test if the symbolName will fully cover the passed in overlappedSymbolName (these are assumed to be at the same position on the reels), 
	// used to determine when splitting is needed for overlapping symbols during mutation
	private static bool isOverlappedSymbolFullyCovered(string symbolName, string overlappedSymbolName)
	{
		if (symbolName == "" || overlappedSymbolName == "")
		{
			return false;
		}

		Vector2 symbolSize = getWidthAndHeightOfSymbolFromName(symbolName);
		Vector2 overlappedSymbolSize = getWidthAndHeightOfSymbolFromName(overlappedSymbolName);

		if (overlappedSymbolSize.x > symbolSize.x || overlappedSymbolSize.y > symbolSize.y)
		{
			// overlapped symbol is larger than the symbol we are trying to put in, so there is no way it is fully covered
			return false;
		}
		else
		{
			// test how many more symbol parts each symbol has in each direction, 
			// if the symbol we are putting in has the same number or more than the overlapped symbol 
			// then the symbol we are putting in should fully cover the overlapped symbol
			int numSymbolPartsBelowSymbol = (int)(symbolSize.y - (getRowFromName(symbolName) - 1));
			int numSymbolPartsAboveSymbol = (int)(symbolSize.y - numSymbolPartsBelowSymbol);
			int numSymbolPartsToRightOfSymbol = (int)(symbolSize.x - (getColumnFromName(symbolName) - 1));
			int numSymbolPartsToLeftOfSymbol = (int)(symbolSize.x - numSymbolPartsToRightOfSymbol);

			int numSymbolPartsBelowOverlappedSymbol = (int)(overlappedSymbolSize.y - (getRowFromName(overlappedSymbolName) - 1));
			int numSymbolPartsAboveOverlappedSymbol = (int)(overlappedSymbolSize.y - numSymbolPartsBelowOverlappedSymbol);
			int numSymbolPartsToRightOfOverlappedSymbol = (int)(overlappedSymbolSize.x - (getColumnFromName(overlappedSymbolName) - 1));
			int numSymbolPartsToLeftOfOverlappedSymbol = (int)(overlappedSymbolSize.x - numSymbolPartsToRightOfOverlappedSymbol);

			if (numSymbolPartsBelowSymbol >= numSymbolPartsBelowOverlappedSymbol
				&& numSymbolPartsAboveSymbol >= numSymbolPartsAboveOverlappedSymbol  
				&& numSymbolPartsToRightOfSymbol >= numSymbolPartsToRightOfOverlappedSymbol
				&& numSymbolPartsToLeftOfSymbol >= numSymbolPartsToLeftOfOverlappedSymbol)
			{
				return true;
			}
		}

		return false;
	}

	/// Animate the outcome animation for this symbol.
	public void animateMutateTo(AnimateDoneDelegate callback = null)
	{
		haltAnimation();

		SlotSymbol animateSymbol = getAnimatorSymbol();
		
		if (animateSymbol == null || animateSymbol.animator == null ||
			animateSymbol.animator.info.mutateToAnimation == SymbolAnimationType.NONE)
		{
			// There is no animation to play, carry on with the logic.
			if (callback != null)
			{
				callback(this);
			}
		}
		else if (animateSymbol == this)
		{			
			// This symbol actually plays the animation.
			if (callback != null)
			{
				animationCallbacks.Add(callback);
			}

			animator.playMutateTo(this);
		}
		else
		{
			// A different symbol houses the animation, so call animateOutcome() on it instead.
			animateSymbol.animateMutateTo(callback);
		}

		if (subsymbol != null)
		{
			subsymbol.animateMutateTo(callback);
		}
	}

	/// Called by SymbolAnimator when all pending animations are done.
	public void animationDone()
	{
		List<AnimateDoneDelegate> oldList = animationCallbacks;
		animationCallbacks = new List<AnimateDoneDelegate>();

		foreach (AnimateDoneDelegate callback in oldList)
		{
			if (callback != null)
			{
				callback(this);
			}
		}
	}
	
	// Get the world position of the symbol's animator
	public Vector3 getSymbolWorldPosition()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null)
		{
			return animateSymbol.animator.transform.position;
		}
		else
		{
			Debug.LogWarning("SlotSymbol.getSymbolWorldPosition() - Unable to find animateSymbol for symbol.name = " + name);
			return Vector3.zero;
		}
	}

	// Allows public access to the animator, correctly grabbed using getAnimatorSymbol
	// @todo (10/21/2016 Scott Lepthien) : Maybe we should consider removing public acess to animator, and replace it with calls to this
	public SymbolAnimator getAnimator()
	{
		SlotSymbol animateSymbol = getAnimatorSymbol();

		if (animateSymbol != null)
		{
			return animateSymbol.animator;
		}
		else
		{
			return null;
		}
	}

	///  Helper to determine if top left or bottom left of oversized symbol is visible
	public bool isOnScreenPartial(bool relativeToEngine)
	{
		// since tall and mega symbols are always fully on their reels from left/right, 
		// but may land partially top/bottom, 
		// only need to check top and bottom left corners to determine if any part is visible 

		// top left
		SlotSymbol cornerSymbol = getSymbolPartTopLeft();
		if (cornerSymbol == null)
		{
			return false;
		}
		if (cornerSymbol.isVisible(false, relativeToEngine))
		{
			return true;
		}

		// bottom left
		if (isTallSymbolPart || isMegaSymbolPart)
		{
			cornerSymbol = getSymbolPartBottomLeft();
			if (cornerSymbol == null)
			{
				return false;
			}
			if (cornerSymbol.isVisible(false, relativeToEngine))
			{
				return true;
			}
		}

		return false;
	}

	public SlotSymbol getSymbolPartTopLeft()
	{
		return getAnimatorSymbol();
	}

	public SlotSymbol getSymbolPartBottomLeft()
	{
		if (isTallSymbolPart || isMegaSymbolPart)
		{
			SlotReel leftReel = reel;
			if (isMegaSymbolPart)
			{
				SlotSymbol topLeft = getSymbolPartTopLeft();
				leftReel = topLeft.reel;
			}

			Vector2 size = getWidthAndHeightOfSymbol();
			int symbolIndex = index + (int)size.y - getRow();
			return leftReel.symbolList[Mathf.Min(symbolIndex, leftReel.symbolList.Count - 1)];
		}
		
		return this;
	}

	/// Returns the part of the symbol that contains the animator, if it can be found
	/// 1x1 symbols contain it themselves
	/// For multi-cell 'mega' symbols this will be the top left corner piece
	public SlotSymbol getAnimatorSymbol()
	{
		// Check and see if we have a wide and tall symbol, or just a tall one.
		if (this.animator != null)
		{
			// this symbol has an animator attached to itself, so return that
			return this;
		}
		else
		{
			if (isTallSymbolPart)
			{
				// Tall symbol
				int symbolIndex = (index) - (getRow() - 1);
				if (symbolIndex >= 0)
				{
					SlotSymbol animatorSymbol = reel.symbolList[symbolIndex];
					if (animatorSymbol.getRow() == 1 && animatorSymbol.getColumn() == 1)
					{
						return animatorSymbol;
					}
				}
			}
			else if (isMegaSymbolPart)
			{
				// Mega symbol that has height and width 
				// Use the size information to move over and up the reels to where the SYM-XA-YA part is.
				int reelIndex = reel.reelID - getColumn();

				if (reelIndex >= 0)
				{
					SlotReel[]reelArray = _reelGame.engine.getReelArray();

					SlotReel reelToCheck = reelArray[reelIndex];

					int symbolIndexInReelToCheck = (index) - (getRow() - 1);

					// adjust for the number of top buffer symbols, this ensures that if the
					// top buffer symbols are different between the two reels we will still
					// be walking across them in a straight line
					if (reel.numberOfTopBufferSymbols != reelToCheck.numberOfTopBufferSymbols)
					{
						int symbolPositionOffset = reelToCheck.numberOfTopBufferSymbols - reel.numberOfTopBufferSymbols;
						symbolIndexInReelToCheck += symbolPositionOffset;
					}

					// adjust for how far the other reel has advanced (if it is moving)
					// this ensures we check the correct index (i.e. if another reel has
					// moved twice in a frameUpdate but the reel of the passed symbol
					// has only moved once so far)
					if (reel.numberOfTimesSymbolsAdvancedThisFrame != reelToCheck.numberOfTimesSymbolsAdvancedThisFrame)
					{
						int symbolAdvanceDifference = reelToCheck.numberOfTimesSymbolsAdvancedThisFrame - reel.numberOfTimesSymbolsAdvancedThisFrame;

						// take into account what direction the spin is going in, because this
						// affects which direction the animator symbol part is off in
						if (reelToCheck.spinDirection == SlotReel.ESpinDirection.Up)
						{
							symbolAdvanceDifference = -symbolAdvanceDifference;
						}
						
						symbolIndexInReelToCheck += symbolAdvanceDifference;
					}

					if (symbolIndexInReelToCheck >= 0 && symbolIndexInReelToCheck < reelToCheck.symbolList.Count)
					{
						SlotSymbol animatorSymbol = reelToCheck.symbolList[symbolIndexInReelToCheck];
						if (animatorSymbol.getRow() == 1 && animatorSymbol.getColumn() == 1)
						{
							return animatorSymbol;
						}
					}
				}
			}
			else
			{
				// Not a tall or mega symbol.
				return this;
			}
		}

		// mega or tall symbol which doesn't have a top part, or 1x1 that was missing an animator
		return null;
	}
	
	// Get the base render level of a symbol animator
	public int getBaseRenderLevel()
	{
		SymbolAnimator symbolAnim = getAnimator();
		if (!System.Object.ReferenceEquals(symbolAnim, null))
		{
			return symbolAnim.getBaseRenderLevel();
		}
		
		// As a fallback just return the default render queue
		return SymbolAnimator.BASE_SYMBOL_RENDER_QUEUE_VALUE;
	}

	// Handles changing the render queue for this symbol and any attached sub symbols
	// Goes through every MeshRenderer and sets the render queue to queue.
	public void changeRenderQueue(int queue)
	{
		SymbolAnimator symbolAnim = getAnimator();
		if (!System.Object.ReferenceEquals(symbolAnim, null))
		{
			symbolAnim.changeRenderQueue(queue);
		}
		
		if (subsymbol != null)
		{
			subsymbol.changeRenderQueue(queue);
		}
	}

	// Handles changing the render queue for this symbol and any attached sub symbols
	// Adds a value to the render queue of every MeshRenderer
	public void addRenderQueue(int amount)
	{
		SymbolAnimator symbolAnim = getAnimator();
		if (!System.Object.ReferenceEquals(symbolAnim, null))
		{
			symbolAnim.addRenderQueue(amount);
		}
		
		if (subsymbol != null)
		{
			subsymbol.addRenderQueue(amount);
		}
	}

	public LabelWrapperComponent getDynamicLabel()
	{
		SymbolAnimator symbolAnimator = getAnimator();
		if (symbolAnimator != null)
		{
			return symbolAnimator.getDynamicLabel();
		}

		return null;
	}

	// useful when doing things like changing alpha on all labels since getDynamicLabel() only returns one
	public LabelWrapperComponent[] getAllLabels()
	{
		SymbolAnimator symbolAnimator = getAnimator();
		if (symbolAnimator != null)
		{
			return symbolAnimator.getAllLabels();
		}

		return null;
	}
	
	// Function to convert symbols to _Award versions.  Returns true if the resulting symbol will be an award symbol,
	// false otherwise.
	public bool tryConvertSymbolToAwardSymbol()
	{
		if (isAwardSymbol)
		{
			// The symbol is already an award symbol
			return true;
		}
	
		Vector2 symbolSize = getWidthAndHeightOfSymbol();
		string symbolNameWithAwardExtension = SlotSymbol.constructNameFromDimensions(shortServerName + SlotSymbol.AWARD_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		SymbolInfo awardSymbolInfo = _reelGame.findSymbolInfo(symbolNameWithAwardExtension);
		
		// Make sure there is actually an award version of this symbol, otherwise leave it as is
		if (awardSymbolInfo != null)
		{
			mutateTo(symbolNameWithAwardExtension, null, false, true);
			return true;
		}

		return false;
	}
	
	// Function to convert symbols to _Loop versions.  Returns true if the resulting symbol will be a loop symbol,
	// false otherwise.
	public bool tryConvertSymbolToLoopSymbol()
	{
		if (isLoopSymbol)
		{
			// The symbol is already a loop version of the symbol
			return true;
		}
	
		Vector2 symbolSize = getWidthAndHeightOfSymbol();
		string symbolNameWithLoopExtension = SlotSymbol.constructNameFromDimensions(shortServerName + SlotSymbol.LOOP_SYMBOL_POSTIFIX, (int)symbolSize.x, (int)symbolSize.y);
		SymbolInfo lockLoopSymbolInfo = _reelGame.findSymbolInfo(symbolNameWithLoopExtension);
		
		// Make sure there is actually a Loop version of this symbol, otherwise leave it as is
		if (lockLoopSymbolInfo != null)
		{
			mutateTo(symbolNameWithLoopExtension, null, false, true);
			return true;
		}
		
		return false;
	}
	
	// Function to convert symbols to _Outcome versions.
	// This usually happens automatically, but some games might want to handle this manually if they
	// are using multiple special version animations for a symbol, like _Award and _Loop versions.
	public bool tryConvertSymbolToOutcomeSymbol(bool playVfx)
	{
		if (isOutcomeSymbol)
		{
			// The symbol is already an outcome symbol
			return true;
		}
		
		Vector2 symbolSize = getWidthAndHeightOfSymbol();
		string symbolNameWithOutcomeExtension = SlotSymbol.constructNameFromDimensions(shortServerName + SlotSymbol.OUTCOME_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
		SymbolInfo outcomeInfo = _reelGame.findSymbolInfo(symbolNameWithOutcomeExtension);
		
		// Make sure there is actually an award version of this symbol, otherwise leave it as is
		if (outcomeInfo != null)
		{
			mutateTo(symbolNameWithOutcomeExtension, null, playVfx, true);
			return true;
		}

		return false;
	}

	//=============================================================================
	// Cache of immutable info parsed from symbol names...

	static Dictionary<string, SlotSymbolCacheItem> slotSymbolCache = new Dictionary<string, SlotSymbolCacheItem>();
	static SlotSymbolCacheItem slotSymbolCacheMRU1Item = new SlotSymbolCacheItem("<dummy1>"); // the last cache item we requested
	static SlotSymbolCacheItem slotSymbolCacheMRU2Item = new SlotSymbolCacheItem("<dummy2>"); // the 2nd MRU item we've requested
	static SlotSymbolCacheItem emptySlotSymbolCacheItem = new SlotSymbolCacheItem("");

	// Diagnostic caching stats
	static int slotSymbolCacheMisses   = 0;  // How often we had to create new cache entry
	static int slotSymbolCacheMRU1Hits = 0;  // How often we requested same item as a previous request (fastest)
	static int slotSymbolCacheMRU2Hits = 0;  // How often we requested the same item as our 2nd previous request?
	static int slotSymbolCacheDictHits = 0;  // How often we found info cached in dictionary

	static public SlotSymbolCacheItem getOrCreateCacheItem(string name)
	{
		Profiler.BeginSample("SlotSymbol.getOrCreateCacheItem");
		SlotSymbolCacheItem item; 

		// handle null as an empty string, don't want to deal with extra null-checks
		name = name ?? string.Empty;

		// We often look up thousands of items per frame, and frequently (>30% of the time) it's the same item as the previous request
		// It's worth it to do a quick "is this the same most-recently-used item as before" check before a full dictionary lookup
		if (slotSymbolCacheMRU1Item.name == name)
		{
			item = slotSymbolCacheMRU1Item;
			slotSymbolCacheMRU1Hits++;
		}
		else if (slotSymbolCacheMRU2Item.name == name)
		{
			item = slotSymbolCacheMRU2Item;
			slotSymbolCacheMRU2Hits++;
		}
		else if (slotSymbolCache.TryGetValue(name, out item))
		{
			// Found it in the cache
			slotSymbolCacheDictHits++;

			// update MRU cache
			slotSymbolCacheMRU2Item = slotSymbolCacheMRU1Item;
			slotSymbolCacheMRU1Item = item;
		}
		else
		{
			// Not in cache; So create new item and add it to the cache
			item = new SlotSymbolCacheItem(name);
			slotSymbolCache[name] = item;
			slotSymbolCacheMisses++;

			// update MRU cache
			slotSymbolCacheMRU2Item = slotSymbolCacheMRU1Item;
			slotSymbolCacheMRU1Item = item;
		}

		Profiler.EndSample();
		return item;
	}

	// Diagnostic function that prints the cache stats & resets them
	//
	// Example wonka01 spin:   TotalHits: 23905 (MRU1Hits: 10315 MRU2Hits: 3923 DickHits: 9667)  Misses: 4 CacheSize: 1198
	// Example elvira01 spin:  TotalHits: 34056 (MRU1Hits: 5961 MRU2Hits: 4363 DickHits: 23732)  Misses: 0 CacheSize: 1190
	// Example t102 spin:      TotalHits: 32318 (MRU1Hits: 15111 MRU2Hits: 13735 DickHits: 3472)  Misses: 0 CacheSize: 1188
	//
	static public void showSlotSymbolCacheStats()
	{
		int totalCacheHits = slotSymbolCacheMRU1Hits + slotSymbolCacheMRU2Hits + slotSymbolCacheDictHits;
		Debug.Log(
			"TotalHits: " + totalCacheHits +
			" (MRU1Hits: " + slotSymbolCacheMRU1Hits +
			" MRU2Hits: " + slotSymbolCacheMRU2Hits +
			" DickHits: " + slotSymbolCacheDictHits + ") " +
			" Misses: " + slotSymbolCacheMisses +
			" CacheSize: " + slotSymbolCache.Count
		);

		// reset stats
		slotSymbolCacheMisses = 0;
		slotSymbolCacheDictHits = 0;
		slotSymbolCacheMRU1Hits = 0;
		slotSymbolCacheMRU2Hits = 0;
	}

	// Immutable info all derived from the initial "name" ...
	public class SlotSymbolCacheItem
	{
		public readonly string name;                  // original name that we use for cache key
		public readonly string baseName;              // name without "-TW" or "R-"
		public readonly string nameWithoutSubsymbol;  // the portion BEFORE the colon (else just name)
		public readonly string subsymbolName;         // the portion AFTER the colon (null if none)
		public readonly string shortName;             //
		public readonly string serverName;            //
		public readonly string serverNameWithVariant; // Same as serverName but includes an _Variant part that differentiates the variants of the same symbol 
		public readonly string staticShortServerName; // 
		public readonly string staticShortServerNameWithVariant;

		public readonly bool isTallSymbolPart;        // basename without subsymbols matches form: xx-3A
		public readonly bool isMegaSymbolPart;        // basename without subsymbols matches form: xx-3A-4C
		public readonly bool isFlattenedSymbol;       // name contains _Flattened
		public readonly bool isOutcomeSymbol;         // name contains _Outcome
		public readonly bool isAwardSymbol;           // name contains _Award
		public readonly bool isLoopSymbol;            // name contains _Loop
		public readonly bool isBingoSymbol;           // basename contains SL
		public readonly bool isMajor;                 // shortname contains "M"
		public readonly bool isMinor;                 // shortname contains "F"
		public readonly bool isBonusSymbol;           // shortname contains "BN"
		public readonly bool isWildSymbol;            // shortname contains "W"
		public readonly bool isJackpotSymbol;         // shortname contains "JP"
		public readonly bool isScatterSymbol;         // shortname contains "SC"
		public readonly bool isReplacementSymbol;     // shortname contains "RP"
		public readonly bool isBlankSymbol;           // shortname contains "BL"	

		public readonly sbyte row;                    //
		public readonly sbyte column;                 //
		public readonly Vector2 widthAndHeight;       // ideally this would be two bytes instead of two floats

		public SlotSymbolCacheItem(string name)
		{
			this.name            = name;
			baseName             = getBaseNameFromNameActual(name);
			subsymbolName        = getSubsymbolFromNameActual(name);
			nameWithoutSubsymbol = getNameWithoutSubsymbolFromNameActual(name);
			shortName            = getShortNameFromNameActual(name);
			serverName           = getServerNameFromNameActual(name, isRemovingVariant:true);
			serverNameWithVariant = getServerNameFromNameActual(name, isRemovingVariant:false);
			staticShortServerName = getShortServerNameFromNameActual(name);
			staticShortServerNameWithVariant = getShortServerNameWithVariantFromNameActual(name);

			string baseNoSubsymbol  = getNameWithoutSubsymbolFromNameActual(baseName);
			isTallSymbolPart     = rgxTallSymbol.IsMatch(baseNoSubsymbol);
			isMegaSymbolPart     = rgxMegaSymbol.IsMatch(baseNoSubsymbol);
			isFlattenedSymbol    = name.Contains(FLATTENED_SYMBOL_POSTFIX);
			isOutcomeSymbol      = name.Contains(OUTCOME_SYMBOL_POSTFIX);
			isAwardSymbol        = name.Contains(AWARD_SYMBOL_POSTFIX);
			isLoopSymbol         = name.Contains(LOOP_SYMBOL_POSTIFIX);
			isBingoSymbol        = baseName.Contains(BINGO_SYMBOL_BASE_NAME);
			isMajor              = CommonText.FastStartsWith(shortName, "M");
			isMinor              = CommonText.FastStartsWith(shortName, "F");
			isBonusSymbol        = shortName.Contains("BN");
			isWildSymbol         = shortName.Contains('W');
			isJackpotSymbol      = shortName.Contains("JP");
			isScatterSymbol      = shortName.Contains("SC");
			isReplacementSymbol  = shortName.Contains("RP");
			isBlankSymbol        = shortName.Contains("BL");

			row                  = (sbyte)getRowFromNameActual(name);
			column               = (sbyte)getColumnFromNameActual(name);
			widthAndHeight       = getWidthAndHeightOfSymbolFromNameActual(name);
		}
			
		public override string ToString()
		{
			return (
				"   name=" + name +
				"   baseName=" + baseName +
				"   subsymbolName=" + subsymbolName +
				"   nameWithoutSubsymbol=" + nameWithoutSubsymbol +
				"   shortName=" + shortName +
				"   serverName=" + serverName +
				"   shortServerName=" + staticShortServerName +
				"   isTallSymbolPart=" + isTallSymbolPart +
				"   isMegaSymbolPart=" + isMegaSymbolPart +
				"   isFlattenedSymbol=" + isFlattenedSymbol +
				"   isOutcomeSymbol=" + isOutcomeSymbol +
				"   isBingoSymbol=" + isBingoSymbol +
				"   isMajor=" + isMajor +
				"   isMinor=" + isMinor +
				"   isBonusSymbol=" + isBonusSymbol +
				"   isWildSymbol=" + isWildSymbol +
				"   isJackpotSymbol=" + isJackpotSymbol +
				"   isScatterSymbol=" + isScatterSymbol +
				"   isReplacementSymbol=" + isReplacementSymbol
			);
		}
	}
}

public delegate void AnimateDoneDelegate(SlotSymbol sender, string animName);
