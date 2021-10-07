using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPurchaseOfferExperiment : EosExperiment 
{

	public List<FirstPurchaseOfferData> firstPurchaseOffersList { get; private set; }

	public int bestValueIndex { get; private set; }
	public int mostPopularIndex { get; private set; }
	public int w2eBestValueIndex { get; private set; }
	public int w2eMostPopularIndex { get; private set; }

	public int bestSalePercent
	{
		get
		{
			if (isInExperiment)
			{
				if (bestValueIndex - 1 >= 0 && bestValueIndex <= firstPurchaseOffersList.Count)
				{
					return firstPurchaseOffersList[bestValueIndex-1].salePercent;
				}
			}

			return 0;
		}
	}

	public FirstPurchaseOfferExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		bestValueIndex = getEosVarWithDefault(data, "best_value_index", 0);
		mostPopularIndex = getEosVarWithDefault(data, "most_popular_index", 0);
		w2eBestValueIndex = getEosVarWithDefault(data, "w2e_best_value_index", 0);
		w2eMostPopularIndex = getEosVarWithDefault(data, "w2e_most_popular_index", 0);

		if (firstPurchaseOffersList == null)
		{
			firstPurchaseOffersList = new List<FirstPurchaseOfferData>(6);
		}
		else
		{
			firstPurchaseOffersList.Clear();
		}

		for (int i = 1; i <= 6; i++)
		{
			string name = getEosVarWithDefault(data, "buy_page_package_"+ i, "");
			int bonus = EosExperiment.getEosVarWithDefault(data, "bonus_pct_"+ i, 0);
			int sale = EosExperiment.getEosVarWithDefault(data, "sale_pct_"+ i, 0);
			FirstPurchaseOfferData offerData = new FirstPurchaseOfferData(name, bonus, sale, bestValueIndex,mostPopularIndex,w2eBestValueIndex,w2eMostPopularIndex,i);
			firstPurchaseOffersList.Add(offerData);
		}
	}

	public  override bool isInExperiment
	{
		get
		{
			return base.isInExperiment && !SlotsPlayer.instance.isPayerMobile && !SlotsPlayer.instance.isPayerWeb;
		}
	}

	public override void reset()
	{
		base.reset();
		if (firstPurchaseOffersList != null)
		{
			firstPurchaseOffersList.Clear();
		}
	}
}
