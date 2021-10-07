using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WheelSliceController 
{
	public List<WheelSlice> allSlices;
	public Dictionary<string, List<WheelSlice>> wheelSliceGroups;
	public Dictionary<string, int> sliceTxtColors;
	
	public WheelSliceController()
	{
		allSlices = new List<WheelSlice>();
		wheelSliceGroups = new Dictionary<string, List<WheelSlice>>();
		sliceTxtColors = new Dictionary<string, int>();
	}
	
	public void addWheelSlice(LabelWrapper label, long value = -1, string groupID = null, string bonusGameID = null)
	{
		WheelSlice slice = new WheelSlice(label);
		slice.creditBaseValue = value;
		slice.group = groupID;
		slice.bonusGameID = bonusGameID;
		
		if (value > 0)
		{
			setWheelTextField(slice, value.ToString());
		}
		
		allSlices.Add(slice);
	}
	
	public void setWheelTextField(WheelSlice wheelSlice, string txtString, bool isRevealTxt = false)
	{
		wheelSlice.slice.text = CommonText.makeVertical(txtString.Trim());
	}
}
