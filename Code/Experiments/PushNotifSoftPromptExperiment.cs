public class PushNotifSoftPromptExperiment : EosExperiment
{
	public bool incentivizedPromptEnabled { get; private set; }
	public float incentiveAmount { get; private set; }
	public int cooldown { get; private set; }
	public int maxViews { get; private set; }

	public PushNotifSoftPromptExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		incentivizedPromptEnabled = getEosVarWithDefault(data, "incent_enabled", false);
		incentiveAmount = getEosVarWithDefault(data, "incent_amount", 0f);
		cooldown = getEosVarWithDefault(data, "cooldown", System.Int32.MaxValue);
		maxViews = getEosVarWithDefault(data, "max_views", 0);
	}
}
