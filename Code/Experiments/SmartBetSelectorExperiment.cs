using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartBetSelectorExperiment : EosExperiment
{

	public int jackpotModifier { get; private set; }
	public int bigSliceModifier { get; private set; }
	public int mysteryModifier { get; private set; }
	public int nonTopperModifier { get; private set; }
	public int[] jackpotIncrements { get; private set; }
	public int[] mysteryIncrements { get; private set; }
	public int[] bigSliceIncrements { get; private set;}

	public SmartBetSelectorExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		jackpotModifier = getEosVarWithDefault(data, "jackpot_modifier", 1);
		bigSliceModifier = getEosVarWithDefault(data, "bigslice_modifier", 1);
		mysteryModifier = getEosVarWithDefault(data, "mystery_modifier", 1);
		nonTopperModifier = getEosVarWithDefault(data, "non_topper_modifier", 1);

		string jackpots = getEosVarWithDefault(data, "jackpot_inc", "1,1,1");
		string bigSlice = getEosVarWithDefault(data, "bigslice_inc", "1,1,1");
		string mystery = getEosVarWithDefault(data, "mystery_inc", "1,1,1");

		jackpotIncrements = System.Array.ConvertAll<string, int>( jackpots.Split(','), safeParse);
		mysteryIncrements = System.Array.ConvertAll<string, int>( mystery.Split(','), safeParse);
		bigSliceIncrements = System.Array.ConvertAll<string, int>( bigSlice.Split(','), safeParse);
	}

	private int safeParse(string text)
	{
		int number = 1; //default to non zero value

		if (!int.TryParse(text, out number))
		{
			Debug.LogWarning("Could not parse smart bet value");
		}

		return number;
	}

	public override void reset()
	{
		base.reset();
		jackpotModifier = 1;
		mysteryModifier = 1;
		bigSliceModifier = 1;
		jackpotIncrements = new int[] { 1, 1, 1 };
		mysteryIncrements = new int[] { 1, 1, 1 };
		bigSliceIncrements = new int[] { 1, 1, 1 };
	}
}
