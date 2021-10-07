public class PremiumSlicePackage
{
    public PurchasablePackage purchasePackage { get; private set; }
	
    public PremiumSlicePackage(string packageName)
    {
        purchasePackage = PurchasablePackage.find(packageName);
    }
	
    public override string ToString()
    {
        return string.Format(
            "PremiumSlicePackage:[package_key:{0}]",
            purchasePackage.keyName.ToString());
    }
}
