using UnityEngine;
using System;
using System.Collections.Generic;

// ReelStrip class instance contains the immutable definition for each member of "reel_strips" from GlobalData.  Note that it is distinct from
// ReelData in that ReelData encapsulates a more game-usage friendly encapsulation of global data from different sources.
public class ReelStrip : IResetGame
{
	public readonly string keyName = "";
	public readonly string[] symbols;
	public readonly int reelSyncedTo;
	public readonly int bufferSymbolSize = 1;	// The number of buffer symbols to use for this strip

	// Used when the entire reel strip is the same tall symbol. In such a case, we don't want to splice the reel and clobber broken symbols.
	// When stopping the reels, we manually override the stop position based on the number of visible symbols (also the symbol height) and the reelID so that it visually looks like a normal reelstop.
	// Used in madmen01
	public readonly bool avoidSplicing = false;

	private static Dictionary<string, ReelStrip> _all = new Dictionary<string, ReelStrip>();
	
	public ReelStrip (string keyName, JSON data)
	{
		this.keyName = keyName;
		symbols = data.getStringArray("symbols");

		// find the number of buffer symbols needed for this reel strip
		foreach (string symbol in symbols)
		{
			int currentSymbolHeight = (int)SlotSymbol.getWidthAndHeightOfSymbolFromName(symbol).y;
			bufferSymbolSize = Math.Max(bufferSymbolSize, currentSymbolHeight);
		}

		// buffer symbol size needs to be the (max_symbol_size - 1) * 2
		// and then +1 to ensure we account for the area where the symbols are allowed to slide 
		// to avoid issues where tall/mega symbol overlaps will cause
		// ghost/overlaps on the visible symbols
		if (bufferSymbolSize > 1)
		{
			bufferSymbolSize = ((bufferSymbolSize - 1) * 2) + 1;
		}

		reelSyncedTo = data.getInt("link", -1);
		avoidSplicing = canAvoidSplicing();
	}
	
	public static void populateAll(JSON[] reelStrips)
	{
		foreach (JSON data in reelStrips)
		{
			string keyName = data.getString("key_name", "");
			
			if (keyName == "")
			{
				Debug.LogError("Cannot process empty reel strip key");
				continue;
			}
			else if (_all.ContainsKey(keyName))
			{
				// This could happen under normal conditions, since multiple games
				// may include some of the same reel_strips data in game-specific data.
				continue;
			}
			
			_all[keyName] = new ReelStrip(keyName, data);
		}
	}
	
	public static ReelStrip find(string keyName)
	{
		ReelStrip result = null;
		
		if (!_all.TryGetValue(keyName, out result))
		{
			Debug.LogError("Failed to find ReelStrip for key " + keyName);
		}
		
		return result;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,ReelStrip>();
	}

	// Used when the entire reel strip is the same tall symbol. In such a case, we don't want to splice the reel and clobber broken symbols.
	private bool canAvoidSplicing()
	{
		// Adding this additional check since some cheats may want to use fully blank reels, but this
		// causes weird stuff to happen if those reels get detected for this, since they don't follow the rules
		// intended for what this is being used for.
		bool isEverySymbolBlank = true;
	
		for (int i = 0; i < symbols.Length; i ++)
		{
			if (SlotSymbol.getShortNameFromName(symbols[i]) != SlotSymbol.getShortNameFromName(symbols[0])
				|| SlotSymbol.getWidthAndHeightOfSymbolFromName(symbols[i]) != SlotSymbol.getWidthAndHeightOfSymbolFromName(symbols[0]))				
			{
				return false;
			}

			if (!SlotSymbol.isBlankSymbolFromName(symbols[i]))
			{
				isEverySymbolBlank = false;
			}
		}

		if (isEverySymbolBlank)
		{
			return false;
		}
		else
		{
			return true;
		}
	}
}
