using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure to process and dish out outcomes tailor-made for free spins bonus games.
*/

public class FreeSpinsOutcome : GenericBonusGameOutcome<SlotOutcome>
{
	private SlotOutcome baseOutcome = null;
	public JSON paytable = null;
	public Dictionary<int, string> freeSpinInitialReelSet = null;
	public JSON[] reelInfo = null;
	public int numFreespinsOverride = 0;

	public FreeSpinsOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{
		this.baseOutcome = baseOutcome;

		entries = new List<SlotOutcome>();
		
		if (baseOutcome.getBonusGamePayTableName() != "")
		{
			paytable = BonusGamePaytable.findPaytable("free_spin", baseOutcome.getBonusGamePayTableName());
		}

		foreach (SlotOutcome bonusOutcome in baseOutcome.getSubOutcomesReadOnly())
		{
			entries.Add(bonusOutcome);
		}

		freeSpinInitialReelSet = baseOutcome.getFreeSpinInitialReelSet();
		reelInfo = baseOutcome.getReelInfo();

		// gen84 and bettie02 can override the number of freespins by sending down an extra parameter
		// this is sent down along side the paytable
		numFreespinsOverride = getNumFreespinsOverride();
	}

	private int getNumFreespinsOverride()
	{
		int numFreespins = baseOutcome.getParameter();

		if (numFreespins > 0)
		{
			return numFreespins;
		}

		int numberOfFreespinsOverride = baseOutcome.getNumberOfFreespinsOverride();

		if (numberOfFreespinsOverride > 0)
		{
			return numberOfFreespinsOverride;
		}

		return 0;
	}

	public string getPaytableSetId()
	{
		return baseOutcome.getPaytableSetId();
	}

	public string getBonusGamePayTableName()
	{
		return baseOutcome.getBonusGamePayTableName();
	}

	public long getCarryOverWin()
	{
		JSON carryOverWinJson = baseOutcome.getJsonObject().getJSON("carry_over_win");
		if (carryOverWinJson != null)
		{
			return carryOverWinJson.getLong("credits", 0L);
		}

		return 0L;
	}

	// Used by games like gen97 Cash Tower which display multipliers at the top of the
	// game which unlock/increase as the game is played.  Since we need to display them
	// when the game loads in, we need to have them in the root of the Freespin outcome.
	public int[] getTopMultiplierInitValues()
	{
		return baseOutcome.getJsonObject().getIntArray("top_multipliers_init");
	}
}
