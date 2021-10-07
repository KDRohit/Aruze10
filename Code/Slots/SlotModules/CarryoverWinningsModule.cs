//
// This class implements the basics of providing a carry over value from the basegame
// to a bonus game. Module should inherit this and provide the calculation that sets
// the carryoverWinnings.
//
// BonusGamePresenter checks the basegame cached slotmodules for needsToGetCarryoverWinnings
// and assigns the value to the current payout. Other modules such as
// CarryOverWinningsFreespinModule can then use this value to populate the win label on startup.
//
public abstract class CarryoverWinningsModule : SlotModule
{
	protected long carryoverWinnings = 0;
	
	public override bool needsToGetCarryoverWinnings()
	{
		return true;
	}
	
	public override long executeGetCarryoverWinnings()
	{
		return carryoverWinnings;
	}
}
