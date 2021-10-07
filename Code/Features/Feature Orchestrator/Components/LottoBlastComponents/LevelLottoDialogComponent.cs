
namespace FeatureOrchestrator
{
	public class LevelLottoDialogComponent : ShowDialogComponent
	{
		private GameTimerRange durationTimer;
		
		public LevelLottoDialogComponent(string keyName, JSON json) : base(keyName, json)
		{
		}

		protected override void setupDialogArgs()
		{
			base.setupDialogArgs();
			
			long jackpotAmount = jsonData.getLong("jackpotLabelAmount", 0);
			string revampDialogDescriptionText = jsonData.getString("descriptionTextRevamp", "");
			string offerOverlayTitleText = jsonData.getString("offerOverlayTitleText", "");
			string jackpotAmountString = CreditsEconomy.convertCredits(jackpotAmount);
			
			Timer timer = jsonData.jsonDict["durationKeyname"] as Timer;
			GameTimerRange durationTimer = null;
			if (timer != null)
			{
				durationTimer = timer.durationTimer;
			}
			XPProgressCounter progress = jsonData.jsonDict["xpProgressData"] as XPProgressCounter;
			args.merge(D.TIME, durationTimer, D.VALUE, progress, D.AMOUNT, jackpotAmountString, D.OPTION,
				revampDialogDescriptionText, D.OPTION1, offerOverlayTitleText);
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new LevelLottoDialogComponent(keyname, json);
		}
	}
}

