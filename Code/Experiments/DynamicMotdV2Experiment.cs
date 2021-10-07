public class DynamicMotdV2Experiment : EosExperiment
{
	public string variant { get; private set; }
	public string config { get; private set; }

	public DynamicMotdV2Experiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		variant = getEosVarWithDefault(data, "variant", "");
		config = getEosVarWithDefault(data, "liveVersion", "");
	}

	public override void reset()
	{
		base.reset();
		variant = "";
		config = "";
	}
}
