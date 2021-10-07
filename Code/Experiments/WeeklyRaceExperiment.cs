public class WeeklyRaceExperiment : EueActiveDiscoveryExperiment
{

	public bool disableTextMask { get; private set; }
	public bool dailyRivalShowInGame { get; private set; }
	public bool hasDailyRivals { get; private set; }

	public WeeklyRaceExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		base.init(data);
		disableTextMask = getEosVarWithDefault(data, "disable_text_mask", false);
		dailyRivalShowInGame = getEosVarWithDefault(data, "daily_rival_toaster_on_slot", false);
		hasDailyRivals = getEosVarWithDefault(data, "enabled_with_rivals", false);
	}

	public override void reset()
	{
		base.reset();
		disableTextMask = false;
		dailyRivalShowInGame = false;
		hasDailyRivals = false;

	}
}
