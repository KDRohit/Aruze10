using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/**
This is a derived controller for expanding reel symbols, allows for one texture to be used for 1x1, 1x2, 1x3, 3x3, etc..
All of these sizes supported by this symbol are pre-built by art and contained in the same prefab 
(reason for this may be that frame pieces aren't easily handled in code)
*/
public class PreBuiltExpandingReelSymbol : ExpandingReelSymbolBase
{
	[SerializeField] private List<SupportedSizesData> symbolData = new List<SupportedSizesData>();	// A List of configurations for the different sizes of the symbol.
	private Dictionary<string, SupportedSizesData> symbolDataLookup;   								// Configuration lookup, base on key "widthxheight"
	private GameObject currentSizeObj = null;														// The current size which will be used when this expanding symbol is shown

	protected override void OnDisable()
	{
		base.OnDisable();

		if (currentSizeObj != null)
		{
			currentSizeObj.SetActive(false);
		}
	}																		

    /// Init function, ensures the dictionary has been set up properly
    public override void init()
    {
		symbolDataLookup = new Dictionary<string, SupportedSizesData>();
        foreach (SupportedSizesData data in this.symbolData)
        {
            symbolDataLookup.Add(data.cellsWide + "x" + data.cellsHigh, data);
        }

		base.init();
	} 

    /// Sets the size for the overlay
	public override void setSize(ReelGame reelGame, int cellsWide, int cellsHigh)
	{
		playExpandingSymbolSound(cellsWide, reelGame.isFreeSpinGame());

		string key = cellsWide + "x" + cellsHigh;

		if (symbolDataLookup.ContainsKey(key))
		{
			if (currentSizeObj != null)
			{
				currentSizeObj.SetActive(false);
			}

			currentSizeObj = symbolDataLookup[key].symbolObject;
			currentSizeObj.SetActive(true);          
		}
		else
		{
			Debug.LogException(new KeyNotFoundException("Data for a cell size of [" + cellsWide + ", " + cellsHigh + "] not found!"), this);
		}
	}

	/// Get a list of the supported sizes for this symbol
	protected override List<ExpandingReelSymbolBase.SupportedSize> getSupportedSizeList()
	{
		List<ExpandingReelSymbolBase.SupportedSize> supportedSizeList = new List<ExpandingReelSymbolBase.SupportedSize>();

		foreach (SupportedSizesData data in symbolData)
        {
        	supportedSizeList.Add(new SupportedSize(data.cellsWide, data.cellsHigh, data.cellsWide + "x" + data.cellsHigh));
        }

        return supportedSizeList;
	}

	/// Set the alpha for the symbol
	protected override void setSymbolAlpha(float alpha)
	{
		if (currentSizeObj != null)
		{
			CommonGameObject.alphaGameObject(currentSizeObj, alpha);
		}
		else
		{
			Debug.LogException(new NullReferenceException("currentSizeObj was null for some reason!"));
		}
	}

	/// This will be used to hold data for the supported sizes of this expanding symbol
	[System.Serializable] public class SupportedSizesData : System.Object 
	{	
		public int cellsWide; // Defines the number of cells wide this data is authored for
		public int cellsHigh; // Defines the number of cells high this data is authored for
		public GameObject symbolObject = null;	// Reference to the game object for this symbol size
	}
}
