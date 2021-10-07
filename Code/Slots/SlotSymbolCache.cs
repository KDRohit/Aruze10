using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/**
Helper class for ReelGame which stores and manages cached SlotSymbols which can be reused
*/
public class SlotSymbolCache
{
	private Dictionary<int, SlotSymbolCacheEntry> cache = new Dictionary<int, SlotSymbolCacheEntry>();
	private int currentCacheSize = 0;
	private int symbolCacheMax = -1;                        // -1 is uncapped
	private GameObject symbolCacheGameObject = null;        // Object from the ReelGame where the cached symbols will be stored while cached
	private GameObject freespinsCacheGameObject = null;     // Object to house the freespins override symbols so they are kept apart from the other caches

	private const int MAX_SINGLE_SYMBOL_STACK_SIZE = 30;
	private const int EDITOR_WINDOW_SYMBOLS_DISPLAYED_PER_COLUMN = 10;

	private bool isDoingSymbolInfoValidation = true; // Enable this to detect if there is a case where SymbolInfo.getSlotSymbolCacheHash() is not mapping to a valid entry in the cache

	public SlotSymbolCache(int symbolCacheMax, int symbolCacheMaxLowEndAndroid, GameObject symbolCacheGameObject)
	{
		this.symbolCacheMax = symbolCacheMax;

#if ZYNGA_KINDLE
		if ((SystemInfo.deviceModel == "Amazon KFOT" || SystemInfo.deviceModel == "Amazon KFTT") && symbolCacheMaxLowEndAndroid != -1)
		{
			this.symbolCacheMax = symbolCacheMaxLowEndAndroid;
		}
#endif
		this.symbolCacheGameObject = symbolCacheGameObject;
	}

	// Create the entries in the SlotSymbolCache which will be used to lookup cached symbols
	public void createEntriesInSymbolCache(List<SymbolInfo> symbolTemplates, bool isFreeSpinTemplates)
	{
		foreach (SymbolInfo info in symbolTemplates)
		{
			createEntryInSymbolCache(info, isFreeSpinTemplates);
		}
	}

	// Attempt to create a cache entry for the passed in SymbolInfo.  If a matching
	// entry is already in the cache, we will just associate the names for the pased
	// SymbolInfo with that cache entry.
	private void createEntryInSymbolCache(SymbolInfo info, bool isFreeSpinTemplates)
	{
		// Double check if we can actually get a valid hash from this SymbolInfo.
		// If not it doesn't have any way to visually be displayed so we'll log an
		// error in Editor and someone should look at the symbol and determine if
		// it should be deleted, because it shouldn't be used.
		if (info.hasVisualElementDefined())
		{
			int cacheKey = info.getSlotSymbolCacheHash();

			if (cache.ContainsKey(cacheKey))
			{
				SlotSymbolCacheEntry entry = cache[cacheKey];
				if (isDoingSymbolInfoValidation)
				{
					if (!entry.isSymbolInfoValidMatch(info))
					{
						Debug.LogError("SlotSymbolCache.createEntryInSymbolCache() - info with names: " + info.getNameArrayAsString() + "; matched another entry with key: " + cacheKey + "; but the visual elements don't seem to match!");
					}
				}

				// We already have an entry that this SymbolInfo can use, so link it
				cache[cacheKey].addInfoUsedByEntry(info, isFreeSpinTemplates);
			}
			else
			{
				// We don't have a matching entry, so create one
				cache.Add(cacheKey, new SlotSymbolCacheEntry(info, isFreeSpinTemplates));
			}
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogError("SlotSymbolCache.createEntryInSymbolCache() - No visual elements are set on this SymbolInfo with names: " + info.getNameArrayAsString());
#endif
		}
	}
	
	// Tells if the cache has an entry to store the passed in SymbolInfo
	// if not then this symbol is not being cached
	public bool isCachingSymbol(SymbolInfo info, bool isFreeSpinGame)
	{
		int cacheKey = info.getSlotSymbolCacheHash();
		
		// if a freespins symbol isn't valid or isn't found
		// then we'll check the base cache
		if (cache.ContainsKey(cacheKey))
		{
			return true;
		}

		return false;
	}
	
	// Get the stack used to store symbol instance for the passed in name
	// correctly checks both the freespins overrides and the regular cache
	private SlotSymbolCacheEntry getCacheEntryForSymbolInfo(SymbolInfo info, bool isFreeSpinGame)
	{
		int cacheKey = info.getSlotSymbolCacheHash();
		
		// if a freespins symbol isn't valid or isn't found
		// then we'll check the base cache
		if (cache.ContainsKey(cacheKey))
		{
			return cache[cacheKey];
		}

		Debug.LogWarning("SlotSymbolCache.getCacheStackForSymbol() - names = " + info.getNameArrayAsString() + "; isFreeSpinGame = " + isFreeSpinGame + "; unable to find entry in either cache!  Returning null");
		return null;
	}
	
	// Check if the cache contains an instance of the passed symbol name
	public bool isCachedInstanceAvailable(SymbolInfo info, bool isFlattened)
	{
		int cacheKey = info.getSlotSymbolCacheHash();
		
		// if a freespins symbol isn't valid or isn't found
		// then we'll check the base cache
		if (cache.ContainsKey(cacheKey))
		{
			return cache[cacheKey].hasElements(isFlattened);
		}

		// didn't find anything cached
		return false;
	}
	
	// Get a SymbolAnimator instance from the cache
	public SymbolAnimator getCachedInstance(SymbolInfo info, bool isFlattened)
	{
		int cacheKey = info.getSlotSymbolCacheHash();
	
		if (isCachedInstanceAvailable(info, isFlattened))
		{
			// if a freespins symbol isn't valid or isn't found
			// then we'll check the base cache
			currentCacheSize--;
			return cache[cacheKey].pop(isFlattened);
		}
		else
		{
			return null;
		}
	}
	
	// Determines a way to reduce the cache size in order to attempt to make room
	// to continue to cache
	public void handleCacheLimitExceeded(bool isFreeSpinGame)
	{
		// @todo : Determine if we want to do something with the freespins symbols when cache limit is exceeded

		// @todo : Determine some good ways outside of random to handle this.
		// Things to consider are that once we exceed we will keep exceeding
		// which might not be great (although regardless we'll be deleting a symbol
		// to handle the exceed or because we are just skipping caching to handle the
		// exceed).  Best case would probably be to keep symbols that are used most
		// often so as to prevent needing to deal with the limit being exceeded as much
		// but this is a bit more complicated problem, since the most used could suddenly
		// swing to being a different symbol, and we'd ideally want to be able to handle
		// that rather than holding onto a large cached list of some symbol that was
		// most used previously but is no longer (and then not caching any of the new
		// most used sybmol).
		int[] keys = new int[cache.Keys.Count];
		cache.Keys.CopyTo(keys, 0);
		// We need to remove something from the cache and add this symbol in.
		// What should we remove lets do random.
		int randomStartingIndex = Random.Range(0, cache.Count);
		for (int i = randomStartingIndex; i < cache.Count + randomStartingIndex; i++)
		{
			int index = i % cache.Count;
			int currentKey = keys[index];
			if (cache[currentKey] != null && cache[currentKey].totalCount > 0)
			{
				// We want to remove this element from the list.
				SymbolAnimator cachedSymbol = cache[currentKey].popEitherStack();
				Object.Destroy(cachedSymbol.gameObject);
				currentCacheSize--;
				break;
			}
		}
	}
	
	// Release a symbol instance back into the cache
	// return value tells if the object was placed into the cache or not
	public bool releaseSymbolToCache(SymbolAnimator symbol, bool isFlattend, bool isFreeSpinGame)
	{
		if (symbolCacheMax > -1 && currentCacheSize >= symbolCacheMax)
		{
			// try to make room
			handleCacheLimitExceeded(isFreeSpinGame);
		}
		
		if (symbolCacheMax == -1 || currentCacheSize < symbolCacheMax)
		{
			currentCacheSize++;
			SlotSymbolCacheEntry entry = getCacheEntryForSymbolInfo(symbol.info, isFreeSpinGame);
			entry.add(symbol, isFlattend);
			symbol.transform.parent = symbolCacheGameObject.transform;
			return true;
		}
		else
		{
			// Symbol was not cached, let the caller know so they can handle it.
			// Almost always by destroying the symbol.
			return false;
		}
	}

	// Gets a collection of all SymbolAnimators
	public IEnumerable<SymbolAnimator> getAllCachedSymbolAnimators()
	{
		List<SymbolAnimator> animators = new List<SymbolAnimator>();
		foreach (SlotSymbolCacheEntry entry in cache.Values)
		{
			animators.AddRange(entry.getAllCachedSymbolAnimators());
		}

		return animators;
	}
	
	// Clears the symbol cache
	public void clearSymbolCache()
	{
		IEnumerable<SymbolAnimator> allCachedSymbolAnimators = getAllCachedSymbolAnimators();
		if (allCachedSymbolAnimators != null)
		{
			foreach (SymbolAnimator symbolAnimator in allCachedSymbolAnimators)
			{
				if (symbolAnimator != null && !symbolAnimator.isDoingSomething)
				{
					Object.Destroy(symbolAnimator.gameObject);
				}
			}
		}
		
		foreach (SlotSymbolCacheEntry entry in cache.Values)
		{
			entry.clear();
		}

		cache.Clear();

		currentCacheSize = 0;
	}

	// Limit each symbol to the limiter MAX_SINGLE_SYMBOL_STACK_SIZE
	// should ideally be run after a Freespin game is played to ensure
	// the cache isn't being blown out with a lot of extra symbols which
	// aren't used very often
	public void limitIndividualSymbolStackSizes()
	{
		foreach (SlotSymbolCacheEntry entry in cache.Values)
		{
			currentCacheSize -= entry.reduceToSize(MAX_SINGLE_SYMBOL_STACK_SIZE);
		}
	}
	
	// Logs info about the symbol cache
	public void logCacheInfo()
	{
		string cacheLog = "baseCache.Count = " + cache.Count + "\n{";
		int totalSymbols = 0;

		foreach (KeyValuePair<int, SlotSymbolCacheEntry> cacheEntryPair in cache)
		{
			totalSymbols += cacheEntryPair.Value.totalCount;
			cacheLog += "   " + cacheEntryPair.Value.getSymbolNamesForEntry(false) + " Animated Count = " + cacheEntryPair.Value.animatedCount + ",\n";
			cacheLog += "   " + cacheEntryPair.Value.getSymbolNamesForEntry(false) + " Flattened Count = " + cacheEntryPair.Value.animatedCount + ",\n";
		}

		cacheLog += "}\nTotal Symbols = " + totalSymbols;

		Debug.Log(cacheLog);
	}
	
	private void renderGuiLabel(string labelText, int symbolEntryNumber, GUIStyle style)
	{
		GUILayout.Label(labelText, style);
			
		symbolEntryNumber++;
		if (symbolEntryNumber % EDITOR_WINDOW_SYMBOLS_DISPLAYED_PER_COLUMN == 0)
		{
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
		}
	}

	// Render out the info from SlotSymbolCache into SlotSymbolCacheEditorWindow
	public void drawOnGuiSlotSymbolCacheInfo(bool isGameUsingOptimizedFlattenedSymbols)
	{
		// Base cache section
		GUILayout.BeginHorizontal();
		GUILayout.Label("______________________________________");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Slot Symbol Cache:");
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		int symbolEntryNumber = 0;
		
		GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
		guiStyle.richText = true;
		
		foreach (KeyValuePair<int, SlotSymbolCacheEntry> cacheEntryPair in cache)
		{
			renderGuiLabel(cacheEntryPair.Value.animatedCount + ": " + cacheEntryPair.Value.getSymbolNamesForEntry(true), symbolEntryNumber, guiStyle);
			symbolEntryNumber++;

			if (isGameUsingOptimizedFlattenedSymbols && cacheEntryPair.Value.isTrackingFlattenedSymbols)
			{
				renderGuiLabel(cacheEntryPair.Value.flattenedCount + ": <color=green>[FLAT]</color> " + cacheEntryPair.Value.getSymbolNamesForEntry(true), symbolEntryNumber, guiStyle);
				symbolEntryNumber++;
			}
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("______________________________________");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Cache Total Size: " + currentCacheSize + " symbols");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("______________________________________");
		GUILayout.EndHorizontal();
	}

	private class SlotSymbolCacheEntry
	{
		// Tracking the original SymbolInfo used to create this entry so that we can add
		// validation checks that can be turned on to ensure that the cache is linking
		// SymbolInfo correctly via our hashing method
		private SymbolInfo originalInfo = null;
		
		private HashSet<string> baseSymbolNames = new HashSet<string>();
		private HashSet<string> freespinSymbolNames = new HashSet<string>();
	
		private Stack<SymbolAnimator> animatedSymbolStack = new Stack<SymbolAnimator>();
		private Stack<SymbolAnimator> flattenedSymbolStack = new Stack<SymbolAnimator>();
		
		private string cachedSymbolNamesForEntryWithRichText = "";
		private string cachedSymbolNamesForEntry = "";

		public SlotSymbolCacheEntry(SymbolInfo originalInfo, bool isFreespins)
		{
			this.originalInfo = originalInfo;
			isTrackingFlattenedSymbols = originalInfo.flattenedSymbolPrefab != null;
			addNewSymbolNames(originalInfo.getNameArrayReadOnly(), isFreespins);
		}

		public bool isTrackingFlattenedSymbols
		{
			get;
			private set;
		}

		public int totalCount
		{
			get { return animatedSymbolStack.Count + flattenedSymbolStack.Count; }
		}

		public int animatedCount
		{
			get { return animatedSymbolStack.Count; }
		}

		public int flattenedCount
		{
			get { return flattenedSymbolStack.Count; }
		}
		
		// This function can be used for validating that an Entry that was gotten
		// using SymbolInfo.getSlotSymbolCacheHash() lookup is a valid match
		// for the originalInfo used when creating this entry.
		public bool isSymbolInfoValidMatch(SymbolInfo infoToCheck)
		{
			return (infoToCheck.symbolPrefab == originalInfo.symbolPrefab
					&& infoToCheck.getTexture() == originalInfo.getTexture()
					&& infoToCheck.getUvMappedMaterial() == originalInfo.getUvMappedMaterial());

		}

		// Adds any new names to this entry.  These names are used for display
		// purposes to show what is in the cache when utilizing our tools which
		// display the cache contents.  Names never need to be removed, just added
		// if new symbols are detected as using this entry.
		private void addNewSymbolNames(ReadOnlyCollection<string> names, bool isFreespins)
		{
			if (isFreespins)
			{
				foreach (string name in names)
				{
					if (!freespinSymbolNames.Contains(name))
					{
						freespinSymbolNames.Add(name);
					}
				}
			}
			else
			{
				foreach (string name in names)
				{
					if (!baseSymbolNames.Contains(name))
					{
						baseSymbolNames.Add(name);
					}
				}
			}
			
			updateCachedSymbolNamesForEntry();
		}

		// Adds the names for the passed info which will now be associated with
		// this entry.
		public void addInfoUsedByEntry(SymbolInfo info, bool isFreespins)
		{
			addNewSymbolNames(info.getNameArrayReadOnly(), isFreespins);
		}

		// Clears the stack and returns how many elements were cleared from the stack
		public int clear()
		{
			int totalCleared = animatedSymbolStack.Count + flattenedSymbolStack.Count;
			animatedSymbolStack.Clear();
			flattenedSymbolStack.Clear();
			return totalCleared;
		}

		// Adds an element onto the cached stack
		public void add(SymbolAnimator animator, bool isFlattened)
		{
			if (isFlattened)
			{
				flattenedSymbolStack.Push(animator);
			}
			else
			{
				animatedSymbolStack.Push(animator);
			}
		}

		// Checks if the cache stack has an element to get
		public bool hasElements(bool isFlattened)
		{
			if (isFlattened)
			{
				return flattenedSymbolStack.Count > 0;
			}
			else
			{
				return animatedSymbolStack.Count > 0;
			}
		}

		// Pop an element off the cache stack
		public SymbolAnimator pop(bool isFlattened)
		{
			if (!hasElements(isFlattened))
			{
				Debug.LogError("SlotSymbolCacheEntry.pop() - Attempting to pop stack with no elements! isFlattened = " + isFlattened);
				return null;
			}

			if (isFlattened)
			{
				return flattenedSymbolStack.Pop();
			}
			else
			{
				return animatedSymbolStack.Pop();
			}
		}
		
		// Function used exclusively if we need to clear space in the cache.  Shouldn't
		// be used often since we aren't really using cache limits right now.
		public SymbolAnimator popEitherStack()
		{
			if (totalCount == 0)
			{
				Debug.LogError("SlotSymbolCacheEntry.popEitherStack() - Attempting to pop stack with no elements!");
				return null;
			}

			if (animatedSymbolStack.Count > 0)
			{
				return animatedSymbolStack.Pop();
			}
			else
			{
				return flattenedSymbolStack.Pop();
			}
		}

		// Static method to reduce the size of a passed in stack to be under the passed limit
		private static int reduceStackToSize(Stack<SymbolAnimator> stack, int limit)
		{
			if (limit <= 0)
			{
				Debug.LogError("SlotSymbolCache.reduceStackToSize() - Was passed a limit that was less than or equal to zero! limit = " + limit);
				return 0;
			}
		
			List<SymbolAnimator> symbolsToReAdd = new List<SymbolAnimator>();
			int numberRemoved = 0;

			while (stack.Count > limit)
			{
				// Always pop so we can keep clearing elements, and we will add
				// the elements from symbolsToReAdd when we are done if there
				// were symbols which were still doing something that we don't
				// want to destroy yet
				SymbolAnimator symbolToDelete = stack.Pop();
				if (symbolToDelete != null)
				{
					// Only destroy if the symbol isn't doing anything
					// on the off chance the symbol was animating or something.
					// This shouldn't really be happening to a symbol in the cache
					// though.
					if (symbolToDelete.isDoingSomething)
					{
						// This symbol is still doing something so we will add it back in
						// once we are done trying to reduce the size of this stack
						symbolsToReAdd.Add(symbolToDelete);
					}
					else
					{
						Object.Destroy(symbolToDelete.gameObject);
						numberRemoved++;
					}
				}
			}

			// Now put back in any symbols that couldn't be dealt with because they were
			// doing something
			for (int i = 0; i < symbolsToReAdd.Count; i++)
			{
				stack.Push(symbolsToReAdd[i]);
			}

			return numberRemoved;
		}

		// Reduces both Stacks to the passed in limit.  Will return the number of elements
		// that were removed from the cache.
		public int reduceToSize(int limit)
		{
			int totalNumberRemoved = 0;
			totalNumberRemoved += reduceStackToSize(animatedSymbolStack, limit);
			totalNumberRemoved += reduceStackToSize(flattenedSymbolStack, limit);
			return totalNumberRemoved;
		}
		
		// Get all the cached symbol animators stored within this entry
		public IEnumerable<SymbolAnimator> getAllCachedSymbolAnimators()
		{
			List<SymbolAnimator> animators = new List<SymbolAnimator>();
			animators.AddRange(animatedSymbolStack);
			animators.AddRange(flattenedSymbolStack);
			return animators;
		}
		
		private void updateCachedSymbolNamesForEntry()
		{
			cachedSymbolNamesForEntry = buildSymbolNamesForEntryString(false);
			cachedSymbolNamesForEntryWithRichText = buildSymbolNamesForEntryString(true);
		}

		private string buildSymbolNamesForEntryString(bool includeRichText)
		{
			string seperator = ", ";
			string outputStr = "[";
			foreach (string name in baseSymbolNames)
			{
				outputStr += name + seperator;
			}

			if (baseSymbolNames.Count > 0)
			{
				if (freespinSymbolNames.Count == 0)
				{
					outputStr = outputStr.Remove(outputStr.LastIndexOf(seperator), seperator.Length);
				}
			}
			
			foreach (string name in freespinSymbolNames)
			{
				if (includeRichText)
				{
					outputStr += "<color=teal>(FS)</color>";
				}
				else
				{
					outputStr += "(FS)";
				}
			
				outputStr += name + seperator;
			}
			
			if (freespinSymbolNames.Count > 0)
			{
				outputStr = outputStr.Remove(outputStr.LastIndexOf(seperator), seperator.Length);
			}
			
			outputStr += "]";

			return outputStr;
		}

		public string getSymbolNamesForEntry(bool includeRichText)
		{
			if (includeRichText)
			{
				return cachedSymbolNamesForEntryWithRichText;
			}
			else
			{
				return cachedSymbolNamesForEntry;
			}
		}
	}
}
