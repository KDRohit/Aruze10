using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Represents the override symbol data that can come down as part of a SlotOutcome.  This
 * data tells the client about a specific reel and symbol index that should be overriden.
 *
 * Original Author: Scott Lepthine
 * Creation Date: 10/21/2020
 */
public class OverrideSymbolData
{
	public int reel = 0;
	public int position = 0;
	public int layer = 0;
	public int reelStripIndex = 0;
	public string fromSymbol = "";
	public string toSymbol = "";
}
