public class AgeGateExperiment : EosExperiment
{
	public int ageRequirement
	{
		get;
		private set;
	}

	public AgeGateExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		ageRequirement = getEosVarWithDefault(data, "age_requirement", 21);
	}
	
	

	public override void reset()
	{
		base.reset();
		ageRequirement = 21;
	}
}
