public class ServerActionExperiment : EosExperiment
{

	public string actions { get; private set; }
	public ServerActionExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		actions = getEosVarWithDefault(data, "override_actions", "");
	}

	public override void reset()
	{
		base.reset();
		actions = "";
	}
}
