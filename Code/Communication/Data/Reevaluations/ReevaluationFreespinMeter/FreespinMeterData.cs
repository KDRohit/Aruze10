public class FreespinMeterData
{
	public string tier;
	public string symbol;
	public int reel;
	public int pos;
	public int freeSpinsOld;
	public int freeSpinsNew;
	public int wagerMultiplierOld;
	public int wagerMultiplierNew;

	public FreespinMeterData(JSON meterJSON)
	{
		if (meterJSON == null)
		{
			return;
		}

		tier = meterJSON.getString("tier", "");
		symbol = meterJSON.getString("symbol", "");
		reel = meterJSON.getInt("reel", 0);
		pos = meterJSON.getInt("pos", 0);
		freeSpinsOld = meterJSON.getInt("free_spins_old", 0);
		freeSpinsNew = meterJSON.getInt("free_spins_new", 0);
		wagerMultiplierOld = meterJSON.getInt("wager_multiplier_old", 0);
		wagerMultiplierNew = meterJSON.getInt("wager_multiplier_new", 0);
	}
}