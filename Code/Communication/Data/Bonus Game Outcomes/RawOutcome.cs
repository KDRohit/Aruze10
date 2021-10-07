using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure that holds raw outcome for games that haven't yet broken down their outcomes into smaller outcomes.
You should not use this class longterm!!
Break your outcome down into multiple, more focus outcome objects such as PickemOutcome, WheelOutcome, etc.
*/

public class RawOutcome : GenericBonusGameOutcome<SlotOutcome>
{
	public SlotOutcome outcome = null;
	
	public RawOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{
		outcome = baseOutcome;
	}
}
