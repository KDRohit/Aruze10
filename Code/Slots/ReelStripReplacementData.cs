using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Updated way of defining reel strip replacement data that works for any type of game
 * (i.e. standard, independent reel, and layered games can all be represented and
 * handled using this format).
 *
 * Creation Date: 2/11/2020
 * Original Author: Scott Lepthien
 */
public class ReelStripReplacementData
{
	private string _reelStripKeyName = "";
	public string reelStripKeyName
	{
		get { return _reelStripKeyName;  }
	}
	
	private int _reelIndex = 0;
	public int reelIndex
	{
		get { return _reelIndex; }
	}
	
	private int _position = 0;
	public int position
	{
		get { return _position; }
	}

	private int _layer = 0;
	public int layer
	{
		get { return _layer; }
	}

	private int _visibleSymbols = 1;
	public int visibleSymbols
	{
		get { return _visibleSymbols; }
	}

	public ReelStripReplacementData(string reelStripKeyName, int reelIndex, int position, int layer, int visibleSymbols)
	{
		this._reelStripKeyName = reelStripKeyName;
		this._reelIndex = reelIndex;
		this._position = position;
		this._layer = layer;
		this._visibleSymbols = visibleSymbols;
	}
}
