
public class SpinTimeExperiment : EosExperiment
{
	public int rollupTimePercentage { get; private set; }     //% of original defined time will be use, 100 is unmodulated time, < 100 is faster
	public int reelStopTimePercentage { get; private set; }  //% originally defined time will be use, 100 is unmodulated time, < 100 is faster
	public float payoutRatioModifier { get; private set; }
	public float rollupTimeModifier { get; private set; }

	public SpinTimeExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		rollupTimePercentage = getEosVarWithDefault(data, "rollup_time_percentage", 100);
		reelStopTimePercentage = getEosVarWithDefault(data, "reel_stop_time_percentage", 100);
		payoutRatioModifier = getEosVarWithDefault(data, "payoutRatioModifier", 0.0f);
		rollupTimeModifier = getEosVarWithDefault(data, "rollupTimeModifier", 0.0f);
	}

	public override void reset()
	{
		base.reset();
		rollupTimePercentage = 100;
		reelStopTimePercentage = 100;
		payoutRatioModifier = 0;
		rollupTimeModifier = 0;
	}
}
