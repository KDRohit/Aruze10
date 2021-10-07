using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LayeredMultiSlotBaseGame : MultiSlotBaseGame
{
	
	public override void setReelSet(string defaultKey, JSON data)
	{
		List<string> listOfExcludedReelSetTypes = new List<string>() //List of reel set types we want to ignore in the basegame
		{
			"freespin_background",
		};
		reelSetDataJson = data;
		setReelInfo();
		setModifiers();
		JSON[] someReelInfo = data.getJsonArray("reel_info");
		for(int i = 0; i < someReelInfo.Length; i++)
		{
			string type = someReelInfo[i].getString("type", "");
			if (!listOfExcludedReelSetTypes.Contains(type))
			{
				string reelSetName = someReelInfo[i].getString("reel_set", "");
				reelLayers[i].reelGame = this;
				ReelSetData layerReelSetData = slotGameData.findReelSet(reelSetName);
				reelLayers[i].reelSetData = layerReelSetData;
			}
		}
		
		handleSetReelSet(defaultKey);
	}
}