using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconTripleXPBuff : PurchasePerksIcon
{
	[SerializeField] private LabelWrapperComponent tripleXPText;

	private const string TEXT_LOCALIZATION = "triple_xp_purchase_perk";
	public override void init(CreditPackage package, int index, bool isPurchase, RewardPurchaseOffer offer)
	{
		bool showLottoBlast = ExperimentWrapper.LevelLotto.isInExperiment 
		                      && FeatureOrchestrator.Orchestrator.activeFeaturesToDisplay.Contains(ExperimentWrapper.LevelLotto.experimentName);
		if (!showLottoBlast)
		{
			return;
		}

		int hours = ExperimentWrapper.LevelLotto.tripleXPDuration / Common.SECONDS_PER_HOUR;
		
		tripleXPText.text = Localize.text(TEXT_LOCALIZATION, hours);
		iconDimmer.cacheTextColors(tripleXPText.tmProLabel);
	}
}
