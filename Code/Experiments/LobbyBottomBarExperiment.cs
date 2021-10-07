
public class LobbyBottomBarExperiment : EosExperiment
{
	public int version { get; private set; }

	public LobbyBottomBarExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		version = getEosVarWithDefault(data, "version", 3);
	}

	public override void reset()
	{
		base.reset();
		version = 3;
	}
}
