namespace FeatureOrchestrator
{
	/*
	 * Proton data object used for keeping track of progress data in a feature
	 */
	public class ProgressCounter : BaseDataObject
	{
		private const string STARTING_VALUE_KEY = "startingValue";
		private const string CURRENT_VALUE_KEY = "currentValue";
		private const string COMPLETE_VALUE_KEY = "completeValue";
		
		public long startingValue { get; private set; }
		public long currentValue { get; private set; }
		public long completeValue { get; private set; }

		public ProgressCounter(string keyName, JSON json) : base(keyName, json)
		{
			if (json == null)
			{
				return;
			}
			
			startingValue = json.getLong(STARTING_VALUE_KEY, 0);
			currentValue = json.getLong(CURRENT_VALUE_KEY, 0);
			completeValue = json.getLong(COMPLETE_VALUE_KEY, 0);
		}

		public override void updateValue(JSON json)
		{
			if (json == null)
			{
				return;
			}

			startingValue = json.getLong(STARTING_VALUE_KEY, 0);
			currentValue = json.getLong(CURRENT_VALUE_KEY, 0);
			completeValue = json.getLong(COMPLETE_VALUE_KEY, 0);

			if (Data.debugMode)
			{
				jsonData = json;
			}
		}
		
		public static ProvidableObject createInstance(string keyname, JSON json)
		{
			return new ProgressCounter(keyname, json);
		}
	}
}
