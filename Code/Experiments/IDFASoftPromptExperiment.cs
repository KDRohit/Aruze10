public class IDFASoftPromptExperiment : EosExperiment
{
	public bool showSoftPrompt { get; private set; }
	public bool showLocationEntry { get; private set; }
	public bool showLocationW2E { get; private set; }
	public int softPromptMaxViews { get; private set; }
	public int showEntryCoolDown { get; private set; }
	public int showW2ECoolDown { get; private set; }

	public IDFASoftPromptExperiment(string name) : base(name)
	{
	}
	
	protected override void init(JSON data)
	{
		showSoftPrompt = getEosVarWithDefault(data, "show_soft_prompt", false);
		showLocationEntry = getEosVarWithDefault(data, "show_location_entry", false);
		showLocationW2E = getEosVarWithDefault(data, "show_location_w2e", false);
		softPromptMaxViews = getEosVarWithDefault(data, "soft_prompt_max_views", 0);
		showEntryCoolDown = getEosVarWithDefault(data, "show_entry_cooldown", 0);
		showW2ECoolDown = getEosVarWithDefault(data, "show_w2e_cooldown", 0);
	}
}
