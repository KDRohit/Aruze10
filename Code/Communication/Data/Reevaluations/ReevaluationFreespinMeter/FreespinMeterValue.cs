//
// Class to deserialize and store freespin meter values contained in free_spin_meter reevaluation.
// This can be used to specify the value of the freespin meters when a game loads.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Sep 9th 2019
//
// games : bettie02 - wheel
//
public class FreespinMeterValue
{
	public string tier;
	public int freeSpins;
	public int wagerMultiplier;

	public FreespinMeterValue(JSON meterValueJSON)
	{
		tier = meterValueJSON.getString("tier", "");
		freeSpins = meterValueJSON.getInt("free_spins", 0);
		wagerMultiplier = meterValueJSON.getInt("wager_multiplier", 0);
	}
}

