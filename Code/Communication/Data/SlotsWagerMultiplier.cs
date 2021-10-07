using System.Collections.Generic;

// SlotsWagerMultiplier class - stores the bet multipliers and the level required to use each.
public class SlotsWagerMultiplier : IResetGame
{
	public long multiplier = 1;
	public int levelRequired = 1;
	
	public SlotsWagerMultiplier(JSON data)
	{
		multiplier = data.getLong("multiplier", 0L);
		levelRequired = data.getInt("level_required", 0);
		all.Add(this);
	}
	
	private static List<SlotsWagerMultiplier> all = new List<SlotsWagerMultiplier>();
	
	public static void populateAll(JSON[] multiplierJsonArray)
	{
		foreach (JSON data in multiplierJsonArray)
		{
			new SlotsWagerMultiplier(data);
		}
		all.Sort(sortByMultiplier);
	}
	
	public static List<long> getMultipliersAtLevel(int level)
	{
		List<long> returnVal = new List<long>();
		
		foreach (SlotsWagerMultiplier mult in all)
		{
			if (mult.levelRequired > level)
			{
				break;
			}
			
			returnVal.Add(mult.multiplier);
		}
		
		return returnVal;
	}
	
	public static int sortByMultiplier(SlotsWagerMultiplier a, SlotsWagerMultiplier b)
	{
		return a.multiplier.CompareTo(b.multiplier);
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new List<SlotsWagerMultiplier>();
	}
}
