namespace Zap.Automation
{
	public class ZAPPrefs
	{
		// Flag to control whether we are resuming ZAP or its a fresh start.
		public const string TEST_RESULTS_FOLDER_KEY = "TEST_RESULTS_IN_PROGRESS";
		public const string CURRENT_AUTOMATABLE_INDEX_KEY = "CURRENT_AUTOMATABLE_INDEX";
		public const string CURRENT_TEST_INDEX_KEY = "CURRENT_TEST_INDEX";

		public const string CURRENT_TEST_PLAN = "ZAP_CURRENT_TEST_PLAN";
		public const string TEST_PLAN_JSON = "TEST_PLAN_JSON";
		public const string ZAP_RESULTS_LOCATION = "ZAP_RESULTS_LOCATION";
		public const string ZAP_SAVE_LOCATION = "ZAP_SAVE_LOCATION";

		public const string SHOULD_AUTOMATE_ON_PLAY = "SHOULD_AUTOMATE_ON_PLAY";
		public const string SHOULD_RESUME = "SHOULD_RESUME_ON_PLAY";

		public const string IS_ALLOWING_EDITOR_PAUSE = "IS_ALLOWING_EDITOR_PAUSE";
		public const string IS_USING_RANDOM_WAGERS_FOR_SPINS = "IS_USING_RANDOM_WAGERS_FOR_SPINS";
	}
}
