using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MegaWheelOutcome : GenericBonusGameOutcome<MegaWheelPick>
{
	private int[] _picks;
	private JSON[] _winningSlices;// Winning slices
	private int _currPick;// Current pick to be selected or revealed.
	private int _sliceToStop;//Stop slice
	private List<int> _creditValues;

	public MegaWheelOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{
		JSON paytable = baseOutcome.getBonusGamePayTable();
		JSON[] outcomes = baseOutcome.getJsonSubOutcomes();
		JSON picksArr = outcomes[0];
		int[] picksToSelect;// Array of keys for the picks that will be chosen by the player. Keys are applied to entries.
		int[] picksToReveal;// Array of keys for picks to be revealed.
		
		if (paytable.getJsonArray("picks").Length != 0 && entries == null) 
		{
			entries = new List<MegaWheelPick>();
			for (int i= 0; i < paytable.getJsonArray("picks").Length; i++) 
			{
				MegaWheelPick wheelPick = new MegaWheelPick(paytable.getJsonArray("picks")[i]);
				entries.Add(wheelPick);
			}
		}
		
		picksToSelect = new int[0];
		picksToReveal = new int[0];
			
		if (picksArr.getIntArray("picks").Length != 0) 
		{
			picksToSelect = picksArr.getIntArray("picks");
		}
		if (picksArr.getIntArray("reveals").Length != 0) 
		{
			picksToReveal = picksArr.getIntArray("reveals");
		}
			
		ArrayList al = new ArrayList();
		al.AddRange(picksToSelect);
		al.Sort();
		al.AddRange(picksToReveal);
		_picks = al.ToArray(typeof(int)) as int[];
			
		// Set array of winning slices.
		JSON[] outcomesV2 = new JSON[outcomes.Length - 1];
		for (int x = 0; x < outcomesV2.Length; x++)
		{
			outcomesV2[x] = outcomes[x + 1];
		}
		_winningSlices = outcomesV2;
			
		// Set credits and slice to stop.
		_creditValues = new List<int>();
			
		
		if (paytable.getJsonArray("rounds").Length != 0) 
		{
			JSON round = paytable.getJsonArray("rounds")[0];
			if (round.getJsonArray("wins").Length != 0) 
			{
				JSON[] wins = round.getJsonArray("wins");
				
				for (int j = 0; j < wins.Length; j++) 
				{
					if (wins[j].getInt("credits", 0) != 0) 
					{
						int credits = wins[j].getInt("credits", 0);
						_creditValues.Add(credits);
					}
						
					if (wins[j].getInt("id", -1) != -1 && wins[j].getInt("id", -1) == baseOutcome.getRoundStop(1))
					{
						_sliceToStop = j;
					}
				}
			}
		}
		
			
		// Set the current pick to 0;
		_currPick = 0;
	}

	/// Returns the next bonus game value (pick, wheel pick, SlotOutcome) and removes it from the list.
	public override MegaWheelPick getNextEntry()
	{
		int pickId = _picks[_currPick];
		MegaWheelPick wheelPick = null;
			
		for (int i = 0; i < entries.Count; i++) 
		{
			if (entries[i].id == pickId) 
			{
				wheelPick = entries[i];
			}
		}
			
		_currPick++;
			
		return wheelPick;
	}
		
	public JSON[] winningSlices
	{
		get
		{
			return _winningSlices;
		}
	}
		
	public int sliceToStop
	{
		get
		{
			return _sliceToStop;
		}
	}
		
	public List<int> creditValues 
	{
		get
		{
			return _creditValues;
		}
	}
}

/**
Simple data structure used by MegaWheelOutcome and WheelOutcome.
*/
public class MegaWheelPick 
{
	public int id;
	public int multiplier;
	public string group;
	public int pointerMask;
	public bool isSpinNow;

	public MegaWheelPick(JSON pick)
	{
		if (pick.getInt("id", -1) != -1)
		{
			id = pick.getInt("id", -1);
		}
		
		if (pick.getInt("multiplier", -1) != -1)
		{
			multiplier = pick.getInt("multiplier", -1);
		}
		
		if (pick.getString("group", "") != "")
		{
			group = pick.getString("group", "");
		}
		
		if (pick.getInt("pointer_mask", -1) != -1)
		{
			pointerMask = pick.getInt("pointer_mask", -1);
		}
		
		isSpinNow = pick.getBool("is_spin_now", false);
	}
}
