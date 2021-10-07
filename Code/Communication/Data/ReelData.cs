using System;
using System.Collections.Generic;

/**
ReelData contains the global data info needed for a particular reel.
Note that visibleSymbols in global data is outside the ReelStrip definition, so this
class contains data from both the reel set definition and includes the reelStrip handle.
*/

public class ReelData
{
	public string reelStripKeyName;
	public ReelStrip reelStrip;
	public int visibleSymbols;
	// Independent end reels use a matrix
	public int position = 0;
	public int reelID = -1;
	
	public ReelData (JSON reelData)
	{
		reelStripKeyName = reelData.getString("key_name", "");
		reelStrip = ReelStrip.find(reelStripKeyName);				// TODO - defer this search until needed?
		
		visibleSymbols = reelData.getInt("visible_symbols", 0);
		position = reelData.getInt("position", 0);
		reelID = reelData.getInt("reel", -1);
		if (position != -1 && reelID != -1 && visibleSymbols == 0) // This is an independent reel and it's visible symbol size is 1.
		{
			visibleSymbols = 1;
		}
	}

	public ReelData(string reelStripKeyName, int visibleSymbols)
	{
		this.reelStripKeyName = reelStripKeyName;
		reelStrip = ReelStrip.find(reelStripKeyName);
		this.visibleSymbols = visibleSymbols;
	}
}