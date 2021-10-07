
public class BundleSaleExperiment : EosExperiment
{
    public string bundleId
    {
        get;
        private set;
    }

    public BundleSaleExperiment(string name) : base(name)
    {
    }

    protected override void init(JSON data)
    {
        bundleId = getEosVarWithDefault(data, "bundle_sale_id", "");
    }

    public override void reset()
    {
        bundleId = "";
    }
}
