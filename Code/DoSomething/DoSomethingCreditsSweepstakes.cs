
public class DoSomethingCreditsSweepstakes : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		CreditSweepstakesMOTD.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		//show if feature is active and we aren't running a sale
		return CreditSweepstakes.isActive && !PurchaseFeatureData.isSaleActive && !ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
	}

	public override GameTimer getTimer(string parameter)
	{
		if (CreditSweepstakes.timeRange != null)
		{
			return CreditSweepstakes.timeRange.endTimer;
		}

		return null;
	}
}
