

public class PremiumSliceExperiment : EosExperiment
{
	public int cooldownHours { get; private set; }
	public int cooldownDailySpins { get; private set; }

	public bool showPriceUnderCTAButton { get; private set; }

	public PremiumSliceExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		cooldownHours = getEosVarWithDefault(data, "cooldown_hours", -1);
		cooldownDailySpins = getEosVarWithDefault(data, "cooldown_daily_bonus_spins", -1);
		showPriceUnderCTAButton = getEosVarWithDefault(data, "show_price_under_cta_button", false);
	}

	public override void reset()
	{
		base.reset();
		cooldownHours = -1;
		cooldownDailySpins = -1;
	}
}
