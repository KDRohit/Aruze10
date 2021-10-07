using UnityEngine;
using System.Collections;

public class WheelSlice 
{
	public LabelWrapper slice;
	public string currentText;
	public string nextText;
	public long credits;
	public string group;
	public string bonusGameID;
	
	private long _creditBaseValue;
	
	public WheelSlice(LabelWrapper slice)
	{
		this.slice = slice;
	}
	
	public long creditBaseValue
	{
		get
		{
			return _creditBaseValue;
		}
		
		set
		{
			_creditBaseValue = value;
			credits = value;
		}
	}
}
