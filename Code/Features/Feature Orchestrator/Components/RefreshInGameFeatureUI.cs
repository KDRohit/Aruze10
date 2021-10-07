using System.Collections.Generic;

namespace FeatureOrchestrator
{
	public class RefreshInGameFeatureUI : BaseComponent
	{
		public RefreshInGameFeatureUI(string keyName, JSON json) : base(keyName, json)
		{
		}
		
		public override Dictionary<string, object> perform(Dictionary<string, object> payload, bool shouldLog = false)
		{
			Dictionary<string, object> result = base.perform(payload, shouldLog);
			this.payload = payload;
			InGameFeatureContainer.refreshDisplay(featureName, Dict.create(D.DATA, jsonData));
			return result;
		}

		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new RefreshInGameFeatureUI(keyname, json);
		}
	}
}