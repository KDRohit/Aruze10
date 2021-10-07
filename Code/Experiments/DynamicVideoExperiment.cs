
public class DynamicVideoExperiment : EosExperiment
{
	public string url { get; private set; }
	public string buttonText { get; private set; }
	public string action { get; private set; }
	public string statName { get; private set; }
	public int closeButtonDelay { get; private set; }
	public int skipButtonDelay { get; private set; }
	public string imagePath { get; private set; }

	public DynamicVideoExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		url = getEosVarWithDefault(data, "video_path", "");
		buttonText = getEosVarWithDefault(data, "button_text", "");
		action = getEosVarWithDefault(data, "action", "");
		statName = getEosVarWithDefault(data, "stat_name", "");
		closeButtonDelay = getEosVarWithDefault(data, "close_button_delay", 0);
		skipButtonDelay = getEosVarWithDefault(data, "skip_button_delay", 0);
		imagePath = getEosVarWithDefault(data, "image_path", "");
	}

	public override void reset()
	{
		base.reset();
		url = "";
		buttonText = "";
		action = "";
		statName = "";
		closeButtonDelay = 0;
		skipButtonDelay = 0;
		imagePath = "";
	}
}
