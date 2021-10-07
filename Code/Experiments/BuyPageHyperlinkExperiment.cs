
public class BuyPageHyperlinkExperiment : EosExperiment
{
	public string popcornLink { get; private set; }
	public string buyLink { get; private set; }
	public string popcornText { get; private set; }
	public string buyText { get; private set; }

	public BuyPageHyperlinkExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		popcornLink = getEosVarWithDefault(data, "popcorn_page_link", "");
		buyLink = getEosVarWithDefault(data, "buy_page_link", "");
		popcornText = getEosVarWithDefault(data, "popcorn_text", "");
		buyText = getEosVarWithDefault(data, "buy_page_text", "");
	}

	public override void reset()
	{
		base.reset();
		popcornLink = "";
		buyLink = "";
		popcornText = "";
		buyText = "";
	}
}
