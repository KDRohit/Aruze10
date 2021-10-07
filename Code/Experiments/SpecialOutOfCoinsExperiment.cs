public class SpecialOutOfCoinsExperiment : EosExperiment
{	
	[Eos("spin_limit", 100)]
	public int spinLimit { get; private set; }
	
	[Eos("ooc_limit", 3)]
	public int oocLimit { get; private set; }
	
	[Eos("time_limit", 1)]
	public int timeLimit { get; private set; }
	
	[Eos("cta", "dollar_reward")]
	public string cta { get; private set; }
	
	[Eos("dollar_amount", 10)]
	public int dollarAmount { get; private set; }
	
	[Eos("background_image", "dynamic_dialogs/ooc_rebound_windowed.png")]
	public string backgroundImage { get; private set; }
	
	public SpecialOutOfCoinsExperiment(string name) : base(name)
	{	
	}
}
