public class FirstPurchaseOfferData
{
	public string packageName = "";
	public int bonusPercent = 0;
	public int salePercent = 0;
	public bool isBestValue = false;
	public bool isMostPopular = false;
	public bool isW2eBestValue = false;
	public bool isW2eMostPopular = false;	

	public FirstPurchaseOfferData(string name, int bonus, int sale, int bestValueIndex,int mostPopularIndex,int w2eBestValueIndex,int w2eMostPopularIndex,int packageIndex)
	{
		packageName = name;
		bonusPercent = bonus;
		salePercent = sale;
		isBestValue = (packageIndex == bestValueIndex);
		isMostPopular = (packageIndex == mostPopularIndex);
		isW2eBestValue = (packageIndex == w2eBestValueIndex);
		isW2eMostPopular = (packageIndex == w2eMostPopularIndex);
	}
}
