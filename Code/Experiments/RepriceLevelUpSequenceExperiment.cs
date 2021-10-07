
public class RepriceLevelUpSequenceExperiment : EosExperiment
{
	public int timeoutLength { get; private set; }
	
	// Toaster controls
	public bool useToaster { get; private set; }
	public int toasterTimeout { get; private set; }
	public RepriceLevelUpSequenceExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		timeoutLength = getEosVarWithDefault(data, "timeout_length", 0);
		useToaster = getEosVarWithDefault(data, "useToaster", false);
		toasterTimeout = getEosVarWithDefault(data, "toasterTimeout", 0);
	}
	
	public override void reset()
	{
		base.reset();
		timeoutLength = 0;
	}
}
