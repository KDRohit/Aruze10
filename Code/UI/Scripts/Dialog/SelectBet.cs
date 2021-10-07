using UnityEngine;
using System.Collections;

public class SelectBet : DialogBase
{
	// =============================
	// PROTECTED
	// =============================
	protected long[] buttonValues = null;
	protected LobbyGame gameInfo;

	// =============================
	// PUBLIC
	// =============================
	public GameObject[] betButtons;	// Must be defined from low to high bet value so it matches the multipliers array.

	public override void init()
	{
		//
	}

	public override void close()
	{
		//
	}

	protected bool isQualifyingBet(long betValue)
	{
		if (gameInfo != null)
		{
			return betValue >= gameInfo.specialGameMinQualifyingAmount;
		}
		return false;
	}
}